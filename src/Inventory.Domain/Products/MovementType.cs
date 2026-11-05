namespace Inventory.Domain.Products;

// Starts at 1 so an unset value (0) is never a valid movement type.
public enum MovementType
{
    In = 1,
    Out = 2,
}
