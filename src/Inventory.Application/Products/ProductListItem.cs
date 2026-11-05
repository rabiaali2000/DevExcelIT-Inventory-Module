namespace Inventory.Application.Products;

public sealed record ProductListItem(
    int Id,
    string Sku,
    string Name,
    long CurrentStock,
    int ReorderLevel,
    bool IsActive,
    bool IsLowStock);
