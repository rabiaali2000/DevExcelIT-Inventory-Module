using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_SeedsSampleDataOnce_WithStockMatchingTheLedger()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var services = BuildServices(database.ConnectionString);

        await services.InitializeDatabaseAsync();
        await services.InitializeDatabaseAsync();   // a second start must not seed again

        await using var context = database.CreateContext();
        var stockBySku = await context.Products.ToDictionaryAsync(p => p.Sku, p => p.CurrentStock);
        Assert.Equal(7, stockBySku.Count);
        Assert.Equal(145, stockBySku["PEN-BLU-07"]);
        Assert.Equal(20, stockBySku["NB-A5-RUL"]);
        Assert.Equal(8, stockBySku["STP-26-6"]);
        Assert.Equal(0, stockBySku["HL-YEL"]);
        Assert.False(await context.Products.Where(p => p.Sku == "FAX-ROLL").Select(p => p.IsActive).SingleAsync());
        Assert.Equal(0, await LedgerCheck.CountStockMismatchesAsync(context));
    }

    // The same registrations the app uses, pointed at the test database.
    private static ServiceProvider BuildServices(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Inventory"] = connectionString })
            .Build();

        return new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
