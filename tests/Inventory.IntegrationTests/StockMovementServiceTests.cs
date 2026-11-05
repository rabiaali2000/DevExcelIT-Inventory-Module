using Inventory.Application.Products;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests;

public class StockMovementServiceTests
{
    [Fact]
    public async Task RecordOut_MoreThanStock_FailsAndWritesNothing()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var productId = await CreateProductWithStockAsync(database, 7);

        var issued = await RecordAsync(database, productId, MovementType.Out, 20);

        Assert.Equal(ProductErrors.InsufficientStock(requested: 20, available: 7), issued.Error);
        await AssertStockAndOutsAsync(database, productId, expectedStock: 7, expectedOuts: 0);
    }

    [Fact]
    public async Task TwoSimultaneousOuts_OnlyOneSucceeds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var productId = await CreateProductWithStockAsync(database, 7);

        var results = await RunSimultaneouslyAsync(2, () => RecordAsync(database, productId, MovementType.Out, 5));

        Assert.Single(results, r => r.IsSuccess);
        Assert.Single(results, r => r.Error == ProductErrors.InsufficientStock(requested: 5, available: 2));
        await AssertStockAndOutsAsync(database, productId, expectedStock: 2, expectedOuts: 1);
    }

    [Fact]
    public async Task TwentySimultaneousOuts_IssueExactlyTheStockThatExists()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var productId = await CreateProductWithStockAsync(database, 50);

        var results = await RunSimultaneouslyAsync(20, () => RecordAsync(database, productId, MovementType.Out, 5));

        Assert.Equal(10, results.Count(r => r.IsSuccess));
        Assert.All(results.Where(r => r.IsFailure),
            r => Assert.Equal(ProductErrors.InsufficientStock(requested: 5, available: 0), r.Error));
        await AssertStockAndOutsAsync(database, productId, expectedStock: 0, expectedOuts: 10);
    }

    // Starts every operation at the same moment, each on its own thread with its own DbContext,
    // like near-simultaneous web requests.
    private static async Task<Result[]> RunSimultaneouslyAsync(int count, Func<Task<Result>> operation)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Run(async () =>
            {
                await start.Task;
                return await operation();
            }))
            .ToArray();

        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private static async Task<int> CreateProductWithStockAsync(SqliteTestDatabase database, int stock)
    {
        int productId;
        await using (var context = database.CreateContext())
        {
            var created = await TestServices.Products(context).CreateAsync(new CreateProductCommand("ABC-001", "Blue pen", null, 5));
            productId = created.Value;
        }

        var received = await RecordAsync(database, productId, MovementType.In, stock);
        Assert.True(received.IsSuccess);
        return productId;
    }

    private static async Task<Result> RecordAsync(SqliteTestDatabase database, int productId, MovementType type, int quantity)
    {
        await using var context = database.CreateContext();
        return await TestServices.Movements(context).RecordAsync(new RecordMovementCommand(productId, type, quantity, null));
    }

    private static async Task AssertStockAndOutsAsync(SqliteTestDatabase database, int productId, long expectedStock, int expectedOuts)
    {
        await using var context = database.CreateContext();
        Assert.Equal(expectedStock, (await context.Products.SingleAsync(p => p.Id == productId)).CurrentStock);
        Assert.Equal(expectedOuts, await context.StockMovements.CountAsync(m => m.ProductId == productId && m.Type == MovementType.Out));
        Assert.Equal(0, await LedgerCheck.CountStockMismatchesAsync(context));
    }
}
