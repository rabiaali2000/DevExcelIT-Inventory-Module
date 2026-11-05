using Inventory.Application.Products;

namespace Inventory.Web.ViewModels;

public sealed record ProductDetailViewModel(
    int Id,
    string Sku,
    string Name,
    string DescriptionText,
    int ReorderLevel,
    long CurrentStock,
    bool IsActive,
    string CreatedText,
    IReadOnlyList<StatusBadge> Badges,
    IReadOnlyList<MovementRowViewModel> Movements,
    RecordMovementViewModel Form)
{
    public bool HasMovements => Movements.Count > 0;

    public static ProductDetailViewModel Create(
        ProductDetails product, IReadOnlyList<MovementItem> movements, RecordMovementViewModel form) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description ?? "—",
        product.ReorderLevel,
        product.CurrentStock,
        product.IsActive,
        DisplayFormats.Utc(product.CreatedUtc),
        StatusBadge.For(product.IsOutOfStock, product.IsLowStock, product.IsActive),
        MovementRowViewModel.WithRunningBalance(movements),
        form);
}
