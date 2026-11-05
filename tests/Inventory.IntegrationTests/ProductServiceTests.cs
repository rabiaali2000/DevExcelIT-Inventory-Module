using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests;

public class ProductServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_WithSkuThatExistsInDifferentCase_ReturnsDuplicateSku()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "ABC-001");

        var duplicate = await CreateAsync(database, "abc-001");

        Assert.Equal(ProductErrors.DuplicateSku("ABC-001"), duplicate.Error);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Products.CountAsync());
    }

    [Fact]
    public async Task Save_WhenTheUniqueIndexRejectsTheSku_ReportsDuplicateSku()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "ABC-001");

        // Adding straight through the repository skips the service's SKU check, like losing a race.
        await using var context = database.CreateContext();
        var repository = new ProductRepository(context);
        repository.Add(Product.Create("ABC-001", "Copy", null, 0, Now).Value);

        Assert.Equal(SaveOutcome.DuplicateSku, await repository.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Update_WithStaleVersion_ReturnsConflictAndKeepsTheFirstEdit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var productId = (await CreateAsync(database, "ABC-001")).Value;
        var versionBothUsersSaw = await GetVersionAsync(database, productId);

        var first = await UpdateAsync(database, new UpdateProductCommand(productId, "ABC-001", "Blue pen 0.7 mm", null, 5, versionBothUsersSaw));
        var second = await UpdateAsync(database, new UpdateProductCommand(productId, "ABC-001", "Blue pen", null, 8, versionBothUsersSaw));

        Assert.True(first.IsSuccess);
        Assert.Equal(ProductErrors.ConcurrencyConflict, second.Error);
        await using var context = database.CreateContext();
        var saved = await context.Products.SingleAsync(p => p.Id == productId);
        Assert.Equal("Blue pen 0.7 mm", saved.Name);
        Assert.Equal(5, saved.ReorderLevel);
    }

    private static async Task<Result<int>> CreateAsync(SqliteTestDatabase database, string sku)
    {
        await using var context = database.CreateContext();
        return await TestServices.Products(context).CreateAsync(new CreateProductCommand(sku, "Blue pen", null, 5));
    }

    private static async Task<Result> UpdateAsync(SqliteTestDatabase database, UpdateProductCommand command)
    {
        await using var context = database.CreateContext();
        return await TestServices.Products(context).UpdateAsync(command);
    }

    private static async Task<Guid> GetVersionAsync(SqliteTestDatabase database, int productId)
    {
        await using var context = database.CreateContext();
        return (await context.Products.SingleAsync(p => p.Id == productId)).Version;
    }
}
