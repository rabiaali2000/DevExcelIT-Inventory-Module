namespace Inventory.Application.Products;

// Version is the one the user saw when the edit form was opened.
public sealed record UpdateProductCommand(int Id, string Sku, string Name, string? Description, int ReorderLevel, Guid Version);
