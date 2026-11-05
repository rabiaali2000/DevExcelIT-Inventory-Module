using Inventory.Domain.Products;

namespace Inventory.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidDetails_CreatesActiveProductWithNoStock()
    {
        var result = Product.Create("ABC-001", "Blue pen", "Ballpoint", 5, TestProducts.Now);

        Assert.True(result.IsSuccess);
        var product = result.Value;
        Assert.Equal("ABC-001", product.Sku);
        Assert.Equal("Blue pen", product.Name);
        Assert.Equal("Ballpoint", product.Description);
        Assert.Equal(5, product.ReorderLevel);
        Assert.True(product.IsActive);
        Assert.Equal(0, product.CurrentStock);
        Assert.Equal(TestProducts.Now, product.CreatedUtc);
        Assert.NotEqual(Guid.Empty, product.Version);
    }

    [Fact]
    public void Create_TrimsInputAndUppercasesSku()
    {
        var product = Product.Create("  abc-001 ", "  Blue pen  ", "   ", 0, TestProducts.Now).Value;

        Assert.Equal("ABC-001", product.Sku);
        Assert.Equal("Blue pen", product.Name);
        Assert.Null(product.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankSku_Fails(string sku)
    {
        var result = Product.Create(sku, "Blue pen", null, 0, TestProducts.Now);

        Assert.Equal(ProductErrors.SkuRequired, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Fails(string name)
    {
        var result = Product.Create("ABC-001", name, null, 0, TestProducts.Now);

        Assert.Equal(ProductErrors.NameRequired, result.Error);
    }

    [Fact]
    public void Create_WithNullSkuOrName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Product.Create(null!, "Blue pen", null, 0, TestProducts.Now));
        Assert.Throws<ArgumentNullException>(() => Product.Create("ABC-001", null!, null, 0, TestProducts.Now));
    }

    [Fact]
    public void Create_WithTextAtMaxLength_Succeeds()
    {
        var result = Product.Create(
            new string('S', ProductRules.SkuMaxLength),
            new string('N', ProductRules.NameMaxLength),
            new string('D', ProductRules.DescriptionMaxLength),
            0,
            TestProducts.Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithTooLongSku_Fails()
    {
        var sku = new string('S', ProductRules.SkuMaxLength + 1);

        var result = Product.Create(sku, "Blue pen", null, 0, TestProducts.Now);

        Assert.Equal(ProductErrors.SkuTooLong, result.Error);
    }

    [Fact]
    public void Create_WithTooLongName_Fails()
    {
        var name = new string('N', ProductRules.NameMaxLength + 1);

        var result = Product.Create("ABC-001", name, null, 0, TestProducts.Now);

        Assert.Equal(ProductErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void Create_WithTooLongDescription_Fails()
    {
        var description = new string('D', ProductRules.DescriptionMaxLength + 1);

        var result = Product.Create("ABC-001", "Blue pen", description, 0, TestProducts.Now);

        Assert.Equal(ProductErrors.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void Create_WithNegativeReorderLevel_Fails()
    {
        var result = Product.Create("ABC-001", "Blue pen", null, -1, TestProducts.Now);

        Assert.Equal(ProductErrors.ReorderLevelNegative, result.Error);
    }

    [Fact]
    public void Update_WithValidDetails_ChangesDetailsAndVersion()
    {
        var product = TestProducts.New();
        var originalVersion = product.Version;

        var result = product.Update("xyz-002", "Red pen", null, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal("XYZ-002", product.Sku);
        Assert.Equal("Red pen", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(10, product.ReorderLevel);
        Assert.NotEqual(originalVersion, product.Version);
    }

    [Fact]
    public void Update_WithBlankSku_FailsAndLeavesProductUnchanged()
    {
        var product = TestProducts.New();
        var originalVersion = product.Version;

        var result = product.Update("   ", "Red pen", null, 10);

        Assert.Equal(ProductErrors.SkuRequired, result.Error);
        Assert.Equal("ABC-001", product.Sku);
        Assert.Equal("Blue pen", product.Name);
        Assert.Equal(originalVersion, product.Version);
    }

    [Fact]
    public void Update_WithBlankName_FailsAndLeavesProductUnchanged()
    {
        var product = TestProducts.New();
        var originalVersion = product.Version;

        var result = product.Update("XYZ-002", "", null, 10);

        Assert.Equal(ProductErrors.NameRequired, result.Error);
        Assert.Equal("ABC-001", product.Sku);
        Assert.Equal("Blue pen", product.Name);
        Assert.Equal(originalVersion, product.Version);
    }

    [Fact]
    public void Deactivate_MakesProductInactiveAndChangesVersion()
    {
        var product = TestProducts.New();
        var originalVersion = product.Version;

        product.Deactivate();

        Assert.False(product.IsActive);
        Assert.NotEqual(originalVersion, product.Version);
    }

    [Fact]
    public void Activate_MakesInactiveProductActiveAgain()
    {
        var product = TestProducts.New();
        product.Deactivate();

        product.Activate();

        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData(4, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, false)]
    [InlineData(0, 0, true)]
    public void IsLowStock_IsTrueAtOrBelowReorderLevel(int stock, int reorderLevel, bool expected)
    {
        var product = TestProducts.WithStock(stock, reorderLevel);

        Assert.Equal(expected, product.IsLowStock);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsOutOfStock_IsTrueOnlyWhenNothingIsLeft(int stock, bool expected)
    {
        var product = TestProducts.WithStock(stock);

        Assert.Equal(expected, product.IsOutOfStock);
    }
}
