using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class ProductQueries(InventoryDbContext db) : IProductQueries
{
    private const string LikeEscape = "\\";

    public async Task<IReadOnlyList<ProductListItem>> SearchAsync(
        string? searchTerm, bool includeInactive, CancellationToken cancellationToken)
    {
        var products = db.Products.AsNoTracking();
        if (!includeInactive)
            products = products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLikeWildcards(searchTerm.Trim())}%";
            products = products.Where(p =>
                EF.Functions.Like(p.Sku, pattern, LikeEscape) || EF.Functions.Like(p.Name, pattern, LikeEscape));
        }

        return await products
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItem(
                p.Id, p.Sku, p.Name, p.CurrentStock, p.ReorderLevel, p.IsActive, p.IsLowStock, p.IsOutOfStock))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductDetails?> FindAsync(int id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDetails(
                p.Id, p.Sku, p.Name, p.Description, p.ReorderLevel, p.IsActive,
                p.CurrentStock, p.IsLowStock, p.IsOutOfStock, p.CreatedUtc, p.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MovementItem>> GetMovementsAsync(int productId, CancellationToken cancellationToken) =>
        await db.StockMovements.AsNoTracking()
            .Where(m => m.ProductId == productId)
            .OrderBy(m => m.CreatedUtc)
            .ThenBy(m => m.Id)
            .Select(m => new MovementItem(m.Id, m.Type, m.Quantity, m.Note, m.CreatedUtc, m.SignedQuantity))
            .ToListAsync(cancellationToken);

    // User input is matched literally: %, _ and the escape character itself lose their LIKE meaning.
    private static string EscapeLikeWildcards(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
