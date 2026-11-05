using Inventory.Application.Products;
using Inventory.Infrastructure.Persistence;

namespace Inventory.IntegrationTests;

// The real services and repository around one DbContext, like a single web request.
internal static class TestServices
{
    public static ProductService Products(InventoryDbContext context) =>
        new(new ProductRepository(context), TimeProvider.System);

    public static StockMovementService Movements(InventoryDbContext context) =>
        new(new ProductRepository(context), TimeProvider.System);
}
