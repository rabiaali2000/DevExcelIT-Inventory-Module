using Inventory.Domain.Products;

namespace Inventory.Application.Products;

// SignedQuantity is +Quantity for In and -Quantity for Out, taken from the domain.
public sealed record MovementItem(int Id, MovementType Type, int Quantity, string? Note, DateTime CreatedUtc, int SignedQuantity);
