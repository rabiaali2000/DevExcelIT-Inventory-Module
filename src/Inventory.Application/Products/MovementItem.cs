using Inventory.Domain.Products;

namespace Inventory.Application.Products;

public sealed record MovementItem(int Id, MovementType Type, int Quantity, string? Note, DateTime CreatedUtc);
