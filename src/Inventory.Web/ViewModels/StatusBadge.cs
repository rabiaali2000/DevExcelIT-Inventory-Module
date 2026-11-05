namespace Inventory.Web.ViewModels;

// How a product's status is shown. The rules (low stock, out of stock) come from the domain;
// this only decides which badges a product gets and how they look, so views just render them.
public sealed record StatusBadge(string Text, string CssClass)
{
    public static readonly StatusBadge OutOfStock = new("Out of stock", "text-bg-danger");
    public static readonly StatusBadge LowStock = new("Low stock", "text-bg-warning");
    public static readonly StatusBadge Inactive = new("Inactive", "text-bg-secondary");

    public static IReadOnlyList<StatusBadge> For(bool isOutOfStock, bool isLowStock, bool isActive)
    {
        var badges = new List<StatusBadge>();

        // Out of stock is also low stock; show only the more urgent of the two.
        if (isOutOfStock)
            badges.Add(OutOfStock);
        else if (isLowStock)
            badges.Add(LowStock);

        if (!isActive)
            badges.Add(Inactive);

        return badges;
    }
}
