using Inventory.Domain.Products;

namespace Inventory.Application.Products;

public sealed record RecordMovementCommand(int ProductId, MovementType Type, int Quantity, string? Note);
