using Inventory.Application.Products;
using Inventory.Domain.Products;
using Inventory.Infrastructure.Persistence;

namespace Inventory.IntegrationTests;

public class ProductQueriesTests
{
    [Theory]
    [InlineData("abc", "ABC-001")]
    [InlineData("ABC-0", "ABC-001")]
    [InlineData("marker", "XYZ-002")]
    [InlineData("RED MARK", "XYZ-002")]
    public async Task Search_MatchesSkuOrNameIgnoringCase(string term, string expectedSku)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "ABC-001", "Blue pen");
        await CreateAsync(database, "XYZ-002", "Red marker");

        var results = await SearchAsync(database, term);

        Assert.Equal(expectedSku, Assert.Single(results).Sku);
    }

    [Theory]
    [InlineData("A_1", "A_1")]
    [InlineData("50%", "50% OFF")]
    public async Task Search_TreatsWildcardsAsPlainCharacters(string term, string expectedSku)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "A_1", "Underscore code");
        await CreateAsync(database, "AB1", "Plain code");
        await CreateAsync(database, "50% OFF", "Sale sticker");
        await CreateAsync(database, "500 OFF", "Other sticker");

        var results = await SearchAsync(database, term);

        Assert.Equal(expectedSku, Assert.Single(results).Sku);
    }

    [Fact]
    public async Task Search_HidesInactiveProductsUnlessAsked()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "ABC-001", "Blue pen");
        var retiredId = await CreateAsync(database, "OLD-001", "Retired pen");
        await DeactivateAsync(database, retiredId);

        var activeOnly = await SearchAsync(database, "pen");
        var all = await SearchAsync(database, "pen", includeInactive: true);

        Assert.Equal("ABC-001", Assert.Single(activeOnly).Sku);
        Assert.Equal(2, all.Count);
        Assert.False(all.Single(p => p.Sku == "OLD-001").IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithBlankTerm_ReturnsAllActiveProductsSortedByName(string? term)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateAsync(database, "B-1", "Zebra stapler");
        await CreateAsync(database, "A-1", "Apple sticker");

        var results = await SearchAsync(database, term);

        Assert.Equal(new[] { "Apple sticker", "Zebra stapler" }, results.Select(p => p.Name));
    }

    [Fact]
    public async Task Search_FlagsLowAndOutOfStockUsingTheDomainRules()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await CreateWithStockAsync(database, "ABOVE-1", reorderLevel: 5, stock: 6);
        await CreateWithStockAsync(database, "AT-1", reorderLevel: 5, stock: 5);
        await CreateWithStockAsync(database, "ZERO-1", reorderLevel: 5, stock: 0);

        var bySku = (await SearchAsync(database, null)).ToDictionary(p => p.Sku);

        Assert.False(bySku["ABOVE-1"].IsLowStock);
        Assert.True(bySku["AT-1"].IsLowStock);
        Assert.False(bySku["AT-1"].IsOutOfStock);
        Assert.True(bySku["ZERO-1"].IsLowStock);
        Assert.True(bySku["ZERO-1"].IsOutOfStock);
    }

    private static async Task<int> CreateAsync(SqliteTestDatabase database, string sku, string name, int reorderLevel = 0)
    {
        await using var context = database.CreateContext();
        var created = await TestServices.Products(context).CreateAsync(new CreateProductCommand(sku, name, null, reorderLevel));
        return created.Value;
    }

    private static async Task CreateWithStockAsync(SqliteTestDatabase database, string sku, int reorderLevel, int stock)
    {
        var productId = await CreateAsync(database, sku, $"Product {sku}", reorderLevel);
        if (stock == 0)
            return;

        await using var context = database.CreateContext();
        var received = await TestServices.Movements(context)
            .RecordAsync(new RecordMovementCommand(productId, MovementType.In, stock, null));
        Assert.True(received.IsSuccess);
    }

    private static async Task DeactivateAsync(SqliteTestDatabase database, int productId)
    {
        await using var context = database.CreateContext();
        var deactivated = await TestServices.Products(context).DeactivateAsync(productId);
        Assert.True(deactivated.IsSuccess);
    }

    private static async Task<IReadOnlyList<ProductListItem>> SearchAsync(
        SqliteTestDatabase database, string? term, bool includeInactive = false)
    {
        await using var context = database.CreateContext();
        return await new ProductQueries(context).SearchAsync(term, includeInactive, CancellationToken.None);
    }
}
