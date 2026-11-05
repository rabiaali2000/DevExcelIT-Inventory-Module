using Inventory.Domain.Products;

namespace Inventory.Application.Abstractions;

public interface IProductRepository
{
    // Tracked: changes made to the returned product are written by SaveChangesAsync.
    Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    // Not tracked: a read-only copy, used to validate a stock movement.
    Task<Product?> GetSnapshotAsync(int id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, int? excludingProductId, CancellationToken cancellationToken);

    void Add(Product product);

    Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);

    // Applies the movement's stock change and inserts the movement in one transaction.
    // Returns false and writes nothing when the product is inactive or, for an Out, its stock is no longer enough.
    Task<bool> TryRecordMovementAsync(StockMovement movement, CancellationToken cancellationToken);
}
