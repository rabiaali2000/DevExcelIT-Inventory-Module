using Inventory.Domain.Products;

namespace Inventory.UnitTests.Domain;

public class RecordMovementTests
{
    [Fact]
    public void RecordIn_IncreasesStockAndReturnsMovement()
    {
        var product = TestProducts.New();

        var result = product.RecordMovement(MovementType.In, 10, "  PO-123 ", TestProducts.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, product.CurrentStock);
        var movement = result.Value;
        Assert.Equal(MovementType.In, movement.Type);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal("PO-123", movement.Note);
        Assert.Equal(TestProducts.Now, movement.CreatedUtc);
    }

    [Fact]
    public void RecordOut_DecreasesStock()
    {
        var product = TestProducts.WithStock(10);

        var result = product.RecordMovement(MovementType.Out, 3, null, TestProducts.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, product.CurrentStock);
    }

    [Fact]
    public void RecordMovements_StockEqualsSumOfInsMinusSumOfOuts()
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
    public void RecordOut_MoreThanStock_FailsWithAvailableQuantity()
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
    [InlineData(MovementType.In, ProductRules.MaxMovementQuantity + 1)]
    public void RecordMovement_WithQuantityOutOfRange_Fails(MovementType type, int quantity)
    {
        var product = TestProducts.WithStock(10);

        var result = product.RecordMovement(type, quantity, null, TestProducts.Now);

        Assert.Equal(ProductErrors.QuantityOutOfRange, result.Error);
        Assert.Equal(10, product.CurrentStock);
    }

    [Fact]
    public void RecordIn_AtMaxQuantity_Succeeds()
    {
        var product = TestProducts.New();

        var result = product.RecordMovement(MovementType.In, ProductRules.MaxMovementQuantity, null, TestProducts.Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RecordMovement_WithUndefinedType_Fails()
    {
        var product = TestProducts.WithStock(10);

        var result = product.RecordMovement((MovementType)99, 1, null, TestProducts.Now);

        Assert.Equal(ProductErrors.InvalidMovementType, result.Error);
    }

    [Fact]
    public void RecordMovement_WithTooLongNote_Fails()
    {
        var product = TestProducts.New();
        var note = new string('x', ProductRules.NoteMaxLength + 1);

        var result = product.RecordMovement(MovementType.In, 1, note, TestProducts.Now);

        Assert.Equal(ProductErrors.NoteTooLong, result.Error);
    }

    [Fact]
    public void RecordMovement_OnInactiveProduct_Fails()
    {
        var product = TestProducts.WithStock(10);
        product.Deactivate();

        var result = product.RecordMovement(MovementType.Out, 1, null, TestProducts.Now);

        Assert.Equal(ProductErrors.Inactive, result.Error);
        Assert.Equal(10, product.CurrentStock);
    }

    [Fact]
    public void RecordMovement_DoesNotChangeVersion()
    {
        var product = TestProducts.New();
        var originalVersion = product.Version;

        var result = product.RecordMovement(MovementType.In, 5, null, TestProducts.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalVersion, product.Version);
    }
}
