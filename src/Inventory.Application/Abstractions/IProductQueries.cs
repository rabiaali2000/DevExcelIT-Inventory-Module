using Inventory.Application.Products;

namespace Inventory.Application.Abstractions;

public interface IProductQueries
{
    // Case-insensitive match on SKU or name; inactive products only when asked for.
    Task<IReadOnlyList<ProductListItem>> SearchAsync(string? searchTerm, bool includeInactive, CancellationToken cancellationToken);

    Task<ProductDetails?> FindAsync(int id, CancellationToken cancellationToken);

    // Oldest first.
    Task<IReadOnlyList<MovementItem>> GetMovementsAsync(int productId, CancellationToken cancellationToken);
}
