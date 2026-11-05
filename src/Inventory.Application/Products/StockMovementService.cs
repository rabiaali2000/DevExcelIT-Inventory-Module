using Inventory.Application.Abstractions;
using Inventory.Domain.Common;
using Inventory.Domain.Products;

namespace Inventory.Application.Products;

public sealed class StockMovementService(IProductRepository products, TimeProvider timeProvider)
{
    private const int MaxAttempts = 3;

    public async Task<Result> RecordAsync(RecordMovementCommand command, CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var product = await products.GetSnapshotAsync(command.ProductId, cancellationToken);
            if (product is null)
                return Result.Failure(ProductErrors.NotFound(command.ProductId));

            var movement = product.RecordMovement(command.Type, command.Quantity, command.Note, utcNow);
            if (movement.IsFailure)
                return movement;

            if (await products.TryRecordMovementAsync(movement.Value, cancellationToken))
                return Result.Success();

            // The guard rejected it: stock or status changed after the snapshot was read.
            // Read again so the domain decides with fresh numbers, e.g. "Only 2 available".
        }

        return Result.Failure(ProductErrors.ConcurrencyConflict);
    }
}
