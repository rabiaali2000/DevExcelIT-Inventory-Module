using Inventory.Domain.Products;

namespace Inventory.UnitTests.Domain;

internal static class TestProducts
{
    public static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    public static Product New(int reorderLevel = 0) =>
        Product.Create("ABC-001", "Blue pen", "Ballpoint, 0.7 mm", reorderLevel, Now).Value;

    public static Product WithStock(int stock, int reorderLevel = 0)
    {
        var product = New(reorderLevel);
        if (stock == 0)
            return product;

        var received = product.RecordMovement(MovementType.In, stock, null, Now);
        Assert.True(received.IsSuccess);
        return product;
    }
}
