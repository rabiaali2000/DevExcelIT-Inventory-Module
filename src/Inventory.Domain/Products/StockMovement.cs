namespace Inventory.Domain.Products;

public sealed class StockMovement
{
    internal StockMovement(int productId, MovementType type, int quantity, string? note, DateTime createdUtc)
    {
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Note = note;
        CreatedUtc = createdUtc;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public int SignedQuantity => Type == MovementType.In ? Quantity : -Quantity;
}
