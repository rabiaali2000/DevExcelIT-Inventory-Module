using Inventory.Domain.Common;

namespace Inventory.Domain.Products;

public static class ProductErrors
{
    public static readonly Error SkuRequired =
        new("Product.SkuRequired", "SKU is required.");

    public static readonly Error SkuTooLong =
        new("Product.SkuTooLong", $"SKU cannot be longer than {ProductRules.SkuMaxLength} characters.");

    public static readonly Error NameRequired =
        new("Product.NameRequired", "Name is required.");

    public static readonly Error NameTooLong =
        new("Product.NameTooLong", $"Name cannot be longer than {ProductRules.NameMaxLength} characters.");

    public static readonly Error DescriptionTooLong =
        new("Product.DescriptionTooLong", $"Description cannot be longer than {ProductRules.DescriptionMaxLength} characters.");

    public static readonly Error ReorderLevelNegative =
        new("Product.ReorderLevelNegative", "Reorder level cannot be negative.");

    public static readonly Error Inactive =
        new("Product.Inactive", "This product is inactive. Reactivate it to record movements.");

    public static readonly Error InvalidMovementType =
        new("StockMovement.InvalidType", "Movement type must be In or Out.");

    public static readonly Error QuantityOutOfRange =
        new("StockMovement.QuantityOutOfRange",
            FormattableString.Invariant($"Quantity must be between 1 and {ProductRules.MaxMovementQuantity:N0}."));

    public static readonly Error NoteTooLong =
        new("StockMovement.NoteTooLong", $"Note cannot be longer than {ProductRules.NoteMaxLength} characters.");

    public static Error InsufficientStock(int requested, long available) =>
        new("Product.InsufficientStock", $"Cannot issue {requested}. Only {available} available.");
}
