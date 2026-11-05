using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests;

// A throwaway SQLite database file with the real migrations applied. Each test creates its own
// and the file is deleted when the test ends. Pooling is off so no connection keeps the file open.
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly string _filePath;

    private SqliteTestDatabase(string filePath)
    {
        _filePath = filePath;
        ConnectionString = $"Data Source={filePath};Pooling=False";
    }

    public string ConnectionString { get; }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"inventory-tests-{Guid.NewGuid():N}.db");
        var database = new SqliteTestDatabase(filePath);

        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        return database;
    }

    public InventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(ConnectionString).Options);

    public ValueTask DisposeAsync()
    {
        File.Delete(_filePath);
        return ValueTask.CompletedTask;
    }
}
