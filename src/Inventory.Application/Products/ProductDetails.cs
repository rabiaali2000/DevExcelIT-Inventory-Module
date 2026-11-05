namespace Inventory.Application.Products;

public sealed record ProductDetails(
    int Id,
    string Sku,
    string Name,
    string? Description,
    int ReorderLevel,
    bool IsActive,
    long CurrentStock,
    bool IsLowStock,
    DateTime CreatedUtc,
    Guid Version);
