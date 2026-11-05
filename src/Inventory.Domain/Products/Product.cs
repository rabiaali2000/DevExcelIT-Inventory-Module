using Inventory.Domain.Common;

namespace Inventory.Domain.Products;

public sealed class Product
{
    private Product(DateTime createdUtc)
    {
        CreatedUtc = createdUtc;
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    // Cached sum(In) - sum(Out) of this product's movements. It changes only through RecordMovement;
    // persistence applies the same change with a guarded, atomic UPDATE.
    public long CurrentStock { get; private set; }

    // Optimistic-concurrency token for product edits. Stock movements deliberately leave it unchanged.
    public Guid Version { get; private set; }

    public bool IsLowStock => CurrentStock <= ReorderLevel;

    public bool IsOutOfStock => CurrentStock == 0;

    public static Result<Product> Create(string sku, string name, string? description, int reorderLevel, DateTime utcNow)
    {
        var product = new Product(utcNow);

        var result = product.SetDetails(sku, name, description, reorderLevel);
        if (result.IsFailure)
            return Result.Failure<Product>(result.Error);

        return Result.Success(product);
    }

    public Result Update(string sku, string name, string? description, int reorderLevel) =>
        SetDetails(sku, name, description, reorderLevel);

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkChanged();
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        MarkChanged();
    }

    public Result<StockMovement> RecordMovement(MovementType type, int quantity, string? note, DateTime utcNow)
    {
        var normalizedNote = NormalizeOptional(note);

        var error = ValidateMovement(type, quantity, normalizedNote);
        if (error is not null)
            return Result.Failure<StockMovement>(error);

        var movement = new StockMovement(Id, type, quantity, normalizedNote, utcNow);
        CurrentStock += movement.SignedQuantity;
        return Result.Success(movement);
    }

    private Result SetDetails(string sku, string name, string? description, int reorderLevel)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(name);

        var normalizedSku = sku.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();
        var normalizedDescription = NormalizeOptional(description);

        var error = ValidateDetails(normalizedSku, normalizedName, normalizedDescription, reorderLevel);
        if (error is not null)
            return Result.Failure(error);

        Sku = normalizedSku;
        Name = normalizedName;
        Description = normalizedDescription;
        ReorderLevel = reorderLevel;
        MarkChanged();
        return Result.Success();
    }

    private static Error? ValidateDetails(string sku, string name, string? description, int reorderLevel)
    {
        if (sku.Length == 0)
            return ProductErrors.SkuRequired;
        if (sku.Length > ProductRules.SkuMaxLength)
            return ProductErrors.SkuTooLong;
        if (name.Length == 0)
            return ProductErrors.NameRequired;
        if (name.Length > ProductRules.NameMaxLength)
            return ProductErrors.NameTooLong;
        if (description is { Length: > ProductRules.DescriptionMaxLength })
            return ProductErrors.DescriptionTooLong;
        if (reorderLevel < 0)
            return ProductErrors.ReorderLevelNegative;

        return null;
    }

    private Error? ValidateMovement(MovementType type, int quantity, string? note)
    {
        if (!IsActive)
            return ProductErrors.Inactive;
        if (!Enum.IsDefined(type))
            return ProductErrors.InvalidMovementType;
        if (quantity is < 1 or > ProductRules.MaxMovementQuantity)
            return ProductErrors.QuantityOutOfRange;
        if (note is { Length: > ProductRules.NoteMaxLength })
            return ProductErrors.NoteTooLong;
        if (type == MovementType.Out && quantity > CurrentStock)
            return ProductErrors.InsufficientStock(quantity, CurrentStock);

        return null;
    }

    private static string? NormalizeOptional(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private void MarkChanged() => Version = Guid.NewGuid();
}
