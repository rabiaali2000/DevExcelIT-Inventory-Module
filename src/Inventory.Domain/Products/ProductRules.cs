namespace Inventory.Domain.Products;

public static class ProductRules
{
    public const int SkuMaxLength = 32;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int NoteMaxLength = 500;
    public const int MaxMovementQuantity = 1_000_000;
}
