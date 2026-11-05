using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Inventory.UnitTests.Domain;

namespace Inventory.UnitTests.Application;

public class ProductServiceTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly ProductService _service;

    public ProductServiceTests() =>
        _service = new ProductService(_repository, new FixedTimeProvider(TestProducts.Now));

    [Fact]
    public async Task Create_WithNewSku_AddsAndSavesTheProduct()
    {
        var result = await _service.CreateAsync(new CreateProductCommand("abc-001", "Blue pen", null, 5));

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_repository.AddedProducts);
        Assert.Equal("ABC-001", added.Sku);
        Assert.Equal(TestProducts.Now, added.CreatedUtc);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Create_WithExistingSku_FailsWithoutSaving()
    {
        _repository.ExistingSkus.Add("ABC-001");

        var result = await _service.CreateAsync(new CreateProductCommand("abc-001", "Blue pen", null, 5));

        Assert.Equal(ProductErrors.DuplicateSku("ABC-001"), result.Error);
        Assert.Empty(_repository.AddedProducts);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task Create_WhenTheDatabaseRejectsADuplicateSku_ReturnsTheSameError()
    {
        _repository.SaveOutcome = SaveOutcome.DuplicateSku;

        var result = await _service.CreateAsync(new CreateProductCommand("ABC-001", "Blue pen", null, 5));

        Assert.Equal(ProductErrors.DuplicateSku("ABC-001"), result.Error);
    }

    [Fact]
    public async Task Update_UnknownProduct_ReturnsNotFound()
    {
        var result = await _service.UpdateAsync(new UpdateProductCommand(42, "ABC-001", "Blue pen", null, 5, Guid.NewGuid()));

        Assert.Equal(ProductErrors.NotFound(42), result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task Update_WithStaleVersion_ReturnsConflictWithoutChangingOrSaving()
    {
        var product = TestProducts.New();
        _repository.ProductForUpdate = product;

        var result = await _service.UpdateAsync(new UpdateProductCommand(1, "ABC-001", "Renamed", null, 5, Guid.NewGuid()));

        Assert.Equal(ProductErrors.ConcurrencyConflict, result.Error);
        Assert.Equal("Blue pen", product.Name);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task Update_ToTheSkuOfAnotherProduct_ReturnsDuplicateSkuWithoutSaving()
    {
        var product = TestProducts.New();
        _repository.ProductForUpdate = product;
        _repository.ExistingSkus.Add("XYZ-002");

        var result = await _service.UpdateAsync(new UpdateProductCommand(1, "xyz-002", "Blue pen", null, 5, product.Version));

        Assert.Equal(ProductErrors.DuplicateSku("XYZ-002"), result.Error);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task Update_WhenSomeoneSavesFirst_ReturnsConflict()
    {
        var product = TestProducts.New();
        _repository.ProductForUpdate = product;
        _repository.SaveOutcome = SaveOutcome.ConcurrencyConflict;

        var result = await _service.UpdateAsync(new UpdateProductCommand(1, "ABC-001", "Renamed", null, 5, product.Version));

        Assert.Equal(ProductErrors.ConcurrencyConflict, result.Error);
    }
}
