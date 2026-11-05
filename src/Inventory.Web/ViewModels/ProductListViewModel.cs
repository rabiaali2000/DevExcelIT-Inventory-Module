using Inventory.Application.Products;

namespace Inventory.Web.ViewModels;

public sealed record ProductListViewModel(string? Search, bool ShowInactive, IReadOnlyList<ProductRowViewModel> Products)
{
    public bool HasProducts => Products.Count > 0;

    public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || ShowInactive;

    public string EmptyMessage => string.IsNullOrWhiteSpace(Search)
        ? "No products to show."
        : $"No products match “{Search.Trim()}”.";

    public static ProductListViewModel Create(string? search, bool showInactive, IReadOnlyList<ProductListItem> products) =>
        new(search, showInactive, products.Select(ProductRowViewModel.From).ToList());
}
