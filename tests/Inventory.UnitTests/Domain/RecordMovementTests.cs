using Inventory.Domain.Products;

namespace Inventory.UnitTests.Domain;

public class RecordMovementTests
{
    [Fact]
    public void Stock_EqualsSumOfInsMinusSumOfOuts()
    {
        var product = TestProducts.New();
        (MovementType Type, int Quantity)[] steps =
        [
            (MovementType.In, 10),
            (MovementType.Out, 3),
            (MovementType.In, 5),
            (MovementType.Out, 4),
        ];

        var movements = new List<StockMovement>();
        foreach (var (type, quantity) in steps)
            movements.Add(product.RecordMovement(type, quantity, null, TestProducts.Now).Value);

        var sumIn = movements.Where(m => m.Type == MovementType.In).Sum(m => m.Quantity);
        var sumOut = movements.Where(m => m.Type == MovementType.Out).Sum(m => m.Quantity);
        Assert.Equal(sumIn - sumOut, product.CurrentStock);
        Assert.Equal(8, product.CurrentStock);
    }

    [Fact]
    public void RecordOut_MoreThanStock_FailsWithAvailableQuantityAndKeepsStock()
    {
        var product = TestProducts.WithStock(7);

        var result = product.RecordMovement(MovementType.Out, 20, null, TestProducts.Now);

        Assert.Equal(ProductErrors.InsufficientStock(requested: 20, available: 7), result.Error);
        Assert.Equal("Cannot issue 20. Only 7 available.", result.Error?.Message);
        Assert.Equal(7, product.CurrentStock);
    }

    [Fact]
    public void RecordOut_AllStock_LeavesZero()
    {
        var product = TestProducts.WithStock(7);

        var result = product.RecordMovement(MovementType.Out, 7, null, TestProducts.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, product.CurrentStock);
    }

    [Fact]
    public void RecordOut_WithNoStock_Fails()
    {
        var product = TestProducts.New();

        var result = product.RecordMovement(MovementType.Out, 1, null, TestProducts.Now);

        Assert.Equal(ProductErrors.InsufficientStock(requested: 1, available: 0), result.Error);
        Assert.Equal(0, product.CurrentStock);
    }

    [Theory]
    [InlineData(MovementType.In, 0)]
    [InlineData(MovementType.Out, -5)]
    public void RecordMovement_WithNonPositiveQuantity_Fails(MovementType type, int quantity)
    {
        var product = TestProducts.WithStock(10);

        var result = product.RecordMovement(type, quantity, null, TestProducts.Now);

        Assert.Equal(ProductErrors.QuantityOutOfRange, result.Error);
        Assert.Equal(10, product.CurrentStock);
    }
}
