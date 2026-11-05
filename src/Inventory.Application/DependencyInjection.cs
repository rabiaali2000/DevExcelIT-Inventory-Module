using Inventory.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddSingleton(TimeProvider.System)
            .AddScoped<ProductService>()
            .AddScoped<StockMovementService>();
}
