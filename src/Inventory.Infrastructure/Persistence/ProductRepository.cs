using Inventory.Application.Abstractions;
using Inventory.Domain.Products;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class ProductRepository(InventoryDbContext db) : IProductRepository
{
    private const int SqliteUniqueConstraintFailed = 2067;

    public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetSnapshotAsync(int id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, int? excludingProductId, CancellationToken cancellationToken) =>
        db.Products.AnyAsync(p => p.Sku == sku && p.Id != excludingProductId, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);

    public async Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return SaveOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveOutcome.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (IsDuplicateSku(exception))
        {
            return SaveOutcome.DuplicateSku;
        }
    }

    public async Task<bool> TryRecordMovementAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // The guard: the stock check and the stock change are one UPDATE statement, so no other
        // request can change the stock in between. It runs first, so it takes the write lock first.
        var target = db.Products.Where(p => p.Id == movement.ProductId && p.IsActive);
        if (movement.Type == MovementType.Out)
            target = target.Where(p => p.CurrentStock >= movement.Quantity);

        var updatedRows = await target.ExecuteUpdateAsync(
            setters => setters.SetProperty(p => p.CurrentStock, p => p.CurrentStock + movement.SignedQuantity),
            cancellationToken);
        if (updatedRows == 0)
            return false;

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static bool IsDuplicateSku(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintFailed } sqliteError
        && sqliteError.Message.Contains("Products.Sku", StringComparison.Ordinal);
}
