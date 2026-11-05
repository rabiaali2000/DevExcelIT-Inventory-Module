using Inventory.Application.Products;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Persistence;

internal static class SampleData
{
    private sealed record SampleProduct(
        string Sku,
        string Name,
        string Description,
        int ReorderLevel,
        (MovementType Type, int Quantity, string Note)[] Movements,
        bool IsActive = true);

    private static readonly SampleProduct[] Products =
    [
        new("PEN-BLU-07", "Blue ballpoint pen 0.7 mm", "Retractable, medium point.", 50,
            [(MovementType.In, 200, "Opening stock"), (MovementType.Out, 35, "Order #5531"), (MovementType.Out, 20, "Order #5547")]),
        new("NB-A5-RUL", "A5 ruled notebook", "80 sheets, 70 gsm.", 20,
            [(MovementType.In, 60, "Opening stock"), (MovementType.Out, 40, "Order #5539")]),
        new("STP-26-6", "Staples 26/6, box of 1000", "Fits most desktop staplers.", 15,
            [(MovementType.In, 30, "Opening stock"), (MovementType.Out, 22, "Order #5542")]),
        new("HL-YEL", "Yellow highlighter", "Chisel tip.", 10,
            [(MovementType.In, 25, "Opening stock"), (MovementType.Out, 25, "Order #5550")]),
        new("ENV-DL-100", "DL envelopes, pack of 100", "Self-seal, white.", 5,
            [(MovementType.In, 40, "PO-1042")]),
        new("TAPE-19", "Clear tape 19 mm", "33 m roll.", 12,
            [(MovementType.In, 50, "PO-1042"), (MovementType.Out, 5, "Office use")]),
        new("FAX-ROLL", "Thermal fax paper roll", "Discontinued.", 0,
            [(MovementType.In, 10, "Opening stock"), (MovementType.Out, 10, "Final clearance")], IsActive: false),
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        foreach (var sample in Products)
        {
            // One scope per product, like one web request: its own DbContext and services.
            await using var scope = services.CreateAsyncScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
            var movementService = scope.ServiceProvider.GetRequiredService<StockMovementService>();

            var productId = EnsureSuccess(await productService.CreateAsync(
                new CreateProductCommand(sample.Sku, sample.Name, sample.Description, sample.ReorderLevel), cancellationToken));

            foreach (var (type, quantity, note) in sample.Movements)
                EnsureSuccess(await movementService.RecordAsync(
                    new RecordMovementCommand(productId, type, quantity, note), cancellationToken));

            if (!sample.IsActive)
                EnsureSuccess(await productService.DeactivateAsync(productId, cancellationToken));
        }
    }

    private static T EnsureSuccess<T>(Result<T> result) => result.IsSuccess
        ? result.Value
        : throw new InvalidOperationException($"Seeding sample data failed: {result.Error.Message}");

    private static void EnsureSuccess(Result result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException($"Seeding sample data failed: {result.Error.Message}");
    }
}
