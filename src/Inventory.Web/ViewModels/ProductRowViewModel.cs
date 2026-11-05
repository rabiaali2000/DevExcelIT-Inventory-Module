using Inventory.Application.Products;

namespace Inventory.Web.ViewModels;

public sealed record ProductRowViewModel(
    string Sku,
    string Name,
    long CurrentStock,
    int ReorderLevel,
    IReadOnlyList<StatusBadge> Badges)
{
    public static ProductRowViewModel From(ProductListItem product) => new(
        product.Sku,
        product.Name,
        product.CurrentStock,
        product.ReorderLevel,
        StatusBadge.For(product.IsOutOfStock, product.IsLowStock, product.IsActive));
}
