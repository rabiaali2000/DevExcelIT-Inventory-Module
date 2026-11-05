using Inventory.Domain.Products;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests;

public class DatabaseGuaranteeTests
{
    private const int SqliteUniqueConstraintFailed = 2067;

    private static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Migrations_CreateTablesIndexesAndLedgerTriggers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var names = await context.Database
            .SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type IN ('table', 'index', 'trigger')")
            .ToListAsync();

        Assert.Contains("Products", names);
        Assert.Contains("StockMovements", names);
        Assert.Contains("IX_Products_Sku", names);
        Assert.Contains("IX_StockMovements_ProductId_CreatedUtc", names);
        Assert.Contains("TR_StockMovements_NoUpdate", names);
        Assert.Contains("TR_StockMovements_NoDelete", names);
    }

    [Fact]
    public async Task SavedProduct_IsReadBackWithUtcDateAndNoStock()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var original = await AddProductAsync(database);

        await using var context = database.CreateContext();
        var product = await context.Products.SingleAsync(p => p.Id == original.Id);

        Assert.Equal(Now, product.CreatedUtc);
        Assert.Equal(DateTimeKind.Utc, product.CreatedUtc.Kind);
        Assert.Equal(0, product.CurrentStock);
        Assert.Equal(original.Version, product.Version);
    }

    [Fact]
    public async Task DuplicateSku_IsRejectedByUniqueIndex_EvenInDifferentCase()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await AddProductAsync(database, "ABC-001");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AddProductAsync(database, "abc-001"));

        var sqliteError = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(SqliteUniqueConstraintFailed, sqliteError.SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task NegativeStock_IsRejectedByCheckConstraint()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var product = await AddProductAsync(database);

        var exception = await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteSqlAsync(database, $"UPDATE Products SET CurrentStock = -1 WHERE Id = {product.Id}"));

        Assert.Contains("CK_Products_CurrentStock_NonNegative", exception.Message);
    }

    [Theory]
    [InlineData("   ", "Blue pen", "CK_Products_Sku_NotBlank")]
    [InlineData("ABC-001", "", "CK_Products_Name_NotBlank")]
    public async Task BlankSkuOrName_IsRejectedByCheckConstraint(string sku, string name, string constraint)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(() => ExecuteSqlAsync(database,
            $"""
            INSERT INTO Products (Sku, Name, ReorderLevel, IsActive, CurrentStock, Version, CreatedUtc)
            VALUES ({sku}, {name}, 0, 1, 0, {Guid.NewGuid()}, {Now})
            """));

        Assert.Contains(constraint, exception.Message);
    }

    [Theory]
    [InlineData("In", 0, "CK_StockMovements_Quantity_Positive")]
    [InlineData("Out", -5, "CK_StockMovements_Quantity_Positive")]
    [InlineData("Adjust", 1, "CK_StockMovements_Type")]
    public async Task InvalidMovement_IsRejectedByCheckConstraint(string type, int quantity, string constraint)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var product = await AddProductAsync(database);

        var exception = await Assert.ThrowsAsync<SqliteException>(() =>
            InsertMovementAsync(database, product.Id, type, quantity));

        Assert.Contains(constraint, exception.Message);
    }

    [Theory]
    [InlineData("UPDATE StockMovements SET Quantity = 99")]
    [InlineData("DELETE FROM StockMovements")]
    public async Task ChangingLedgerHistory_IsRejectedByTriggers(string sql)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var product = await AddProductAsync(database);
        await InsertMovementAsync(database, product.Id, "In", 5);

        await using var context = database.CreateContext();
        var exception = await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(sql));

        Assert.Contains("LEDGER_IS_APPEND_ONLY", exception.Message);
    }

    [Fact]
    public async Task DeletingProductWithMovements_IsRejectedByForeignKey()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var product = await AddProductAsync(database);
        await InsertMovementAsync(database, product.Id, "In", 5);

        var exception = await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteSqlAsync(database, $"DELETE FROM Products WHERE Id = {product.Id}"));

        // SQLite enforces ON DELETE RESTRICT with an internal trigger, so the error code is
        // SQLITE_CONSTRAINT_TRIGGER (1811), not SQLITE_CONSTRAINT_FOREIGNKEY (787). The message is the reliable signal.
        Assert.Contains("FOREIGN KEY constraint failed", exception.Message);
        await using var context = database.CreateContext();
        Assert.True(await context.Products.AnyAsync(p => p.Id == product.Id));
    }

    [Fact]
    public async Task SavingEditWithStaleVersion_ThrowsAndKeepsTheOtherUsersChange()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var original = await AddProductAsync(database);

        await using (var sara = database.CreateContext())
        {
            var product = await sara.Products.SingleAsync(p => p.Id == original.Id);
            product.Update(product.Sku, "Blue pen 0.7 mm", product.Description, product.ReorderLevel);
            await sara.SaveChangesAsync();
        }

        await using (var ali = database.CreateContext())
        {
            var product = await ali.Products.SingleAsync(p => p.Id == original.Id);
            ali.Entry(product).Property(p => p.Version).OriginalValue = original.Version;
            product.Update(original.Sku, original.Name, original.Description, 8);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ali.SaveChangesAsync());
        }

        await using var check = database.CreateContext();
        var saved = await check.Products.SingleAsync(p => p.Id == original.Id);
        Assert.Equal("Blue pen 0.7 mm", saved.Name);
        Assert.Equal(original.ReorderLevel, saved.ReorderLevel);
    }

    [Fact]
    public async Task SavingStockChangeThroughChangeTracking_ThrowsAndSavesNothing()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var original = await AddProductAsync(database);

        await using (var context = database.CreateContext())
        {
            var product = await context.Products.SingleAsync(p => p.Id == original.Id);
            var received = product.RecordMovement(MovementType.In, 5, null, Now);
            context.StockMovements.Add(received.Value);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("CurrentStock", exception.Message);
        }

        await using var check = database.CreateContext();
        var saved = await check.Products.SingleAsync(p => p.Id == original.Id);
        Assert.Equal(0, saved.CurrentStock);
        Assert.False(await check.StockMovements.AnyAsync());
    }

    private static async Task<Product> AddProductAsync(SqliteTestDatabase database, string sku = "ABC-001")
    {
        await using var context = database.CreateContext();
        var product = Product.Create(sku, "Blue pen", null, 5, Now).Value;
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static Task InsertMovementAsync(SqliteTestDatabase database, int productId, string type, int quantity) =>
        ExecuteSqlAsync(database,
            $"INSERT INTO StockMovements (ProductId, Type, Quantity, CreatedUtc) VALUES ({productId}, {type}, {quantity}, {Now})");

    private static async Task ExecuteSqlAsync(SqliteTestDatabase database, FormattableString sql)
    {
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlAsync(sql);
    }
}
