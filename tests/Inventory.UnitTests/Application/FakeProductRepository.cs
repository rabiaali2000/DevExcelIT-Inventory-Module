using Inventory.Application.Abstractions;
using Inventory.Domain.Products;

namespace Inventory.UnitTests.Application;

// A scripted stand-in for the database, so each decision a service makes can be tested on its own.
internal sealed class FakeProductRepository : IProductRepository
{
    public Product? ProductForUpdate { get; set; }
    public Queue<Product?> Snapshots { get; } = new();
    public Queue<bool> GuardResults { get; } = new();
    public HashSet<string> ExistingSkus { get; } = [];
    public SaveOutcome SaveOutcome { get; set; } = SaveOutcome.Saved;

    public List<Product> AddedProducts { get; } = [];
    public List<StockMovement> AttemptedMovements { get; } = [];
    public int SaveCount { get; private set; }

    public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(ProductForUpdate);

    public Task<Product?> GetSnapshotAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Snapshots.Dequeue());

    public Task<bool> SkuExistsAsync(string sku, int? excludingProductId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingSkus.Contains(sku));

    public void Add(Product product) => AddedProducts.Add(product);

    public Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(SaveOutcome);
    }

    public Task<bool> TryRecordMovementAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        AttemptedMovements.Add(movement);
        return Task.FromResult(GuardResults.Count == 0 || GuardResults.Dequeue());
    }
}
