using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    // Creates the database on first run, applies any pending migrations and, when there are no products yet,
    // adds sample data, so the app needs no manual setup.
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using (var scope = services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);

            if (await dbContext.Products.AnyAsync(cancellationToken))
                return;
        }

        await SampleData.SeedAsync(services, cancellationToken);
    }
}
