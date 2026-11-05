using Inventory.Application.Products;
using Inventory.Domain.Products;
using Inventory.UnitTests.Domain;

namespace Inventory.UnitTests.Application;

public class StockMovementServiceTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly StockMovementService _service;

    public StockMovementServiceTests() =>
        _service = new StockMovementService(_repository, new FixedTimeProvider(TestProducts.Now));

    [Fact]
    public async Task Record_WhenTheGuardAccepts_Succeeds()
    {
        _repository.Snapshots.Enqueue(TestProducts.WithStock(10));

        var result = await _service.RecordAsync(new RecordMovementCommand(1, MovementType.Out, 3, "Order #1"));

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(_repository.AttemptedMovements);
        Assert.Equal(MovementType.Out, movement.Type);
        Assert.Equal(3, movement.Quantity);
        Assert.Equal(TestProducts.Now, movement.CreatedUtc);
    }

    [Fact]
    public async Task Record_ForUnknownProduct_ReturnsNotFound()
    {
        _repository.Snapshots.Enqueue(null);

        var result = await _service.RecordAsync(new RecordMovementCommand(42, MovementType.In, 1, null));

        Assert.Equal(ProductErrors.NotFound(42), result.Error);
    }

    [Fact]
    public async Task Record_WhenTheDomainRejects_NeverReachesTheGuard()
    {
        _repository.Snapshots.Enqueue(TestProducts.WithStock(7));

        var result = await _service.RecordAsync(new RecordMovementCommand(1, MovementType.Out, 20, null));

        Assert.Equal(ProductErrors.InsufficientStock(requested: 20, available: 7), result.Error);
        Assert.Empty(_repository.AttemptedMovements);
    }

    [Fact]
    public async Task Record_WhenStockFellAfterTheSnapshot_ReportsTheFreshAvailableQuantity()
    {
        _repository.Snapshots.Enqueue(TestProducts.WithStock(7));   // first read: 7, enough for 5
        _repository.GuardResults.Enqueue(false);                     // someone issued stock in between
        _repository.Snapshots.Enqueue(TestProducts.WithStock(2));   // re-read: only 2 left

        var result = await _service.RecordAsync(new RecordMovementCommand(1, MovementType.Out, 5, null));

        Assert.Equal(ProductErrors.InsufficientStock(requested: 5, available: 2), result.Error);
        Assert.Single(_repository.AttemptedMovements);
    }

    [Fact]
    public async Task Record_WhenTheProductWasDeactivatedAfterTheSnapshot_ReportsInactive()
    {
        var deactivated = TestProducts.WithStock(7);
        deactivated.Deactivate();
        _repository.Snapshots.Enqueue(TestProducts.WithStock(7));
        _repository.GuardResults.Enqueue(false);
        _repository.Snapshots.Enqueue(deactivated);

        var result = await _service.RecordAsync(new RecordMovementCommand(1, MovementType.Out, 5, null));

        Assert.Equal(ProductErrors.Inactive, result.Error);
    }

    [Fact]
    public async Task Record_WhenTheGuardKeepsRejecting_GivesUpAfterThreeAttempts()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            _repository.Snapshots.Enqueue(TestProducts.WithStock(10));
            _repository.GuardResults.Enqueue(false);
        }

        var result = await _service.RecordAsync(new RecordMovementCommand(1, MovementType.Out, 5, null));

        Assert.Equal(ProductErrors.ConcurrencyConflict, result.Error);
        Assert.Equal(3, _repository.AttemptedMovements.Count);
    }
}
