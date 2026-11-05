using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Inventory")
            ?? throw new InvalidOperationException("Connection string 'Inventory' is missing from configuration.");

        return services
            .AddDbContext<InventoryDbContext>(options => options.UseSqlite(connectionString))
            .AddScoped<IProductRepository, ProductRepository>()
            .AddScoped<IProductQueries, ProductQueries>();
    }
}
