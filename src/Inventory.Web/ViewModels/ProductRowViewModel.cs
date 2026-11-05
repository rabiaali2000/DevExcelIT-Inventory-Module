using Inventory.Application.Products;

namespace Inventory.Web.ViewModels;

public sealed record ProductRowViewModel(
    int Id,
    string Sku,
    string Name,
    long CurrentStock,
    int ReorderLevel,
    IReadOnlyList<StatusBadge> Badges)
{
    public static ProductRowViewModel From(ProductListItem product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.CurrentStock,
        product.ReorderLevel,
        StatusBadge.For(product.IsOutOfStock, product.IsLowStock, product.IsActive));
}
