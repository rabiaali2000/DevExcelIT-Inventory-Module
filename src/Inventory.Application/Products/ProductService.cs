using System.Diagnostics;
using Inventory.Application.Abstractions;
using Inventory.Domain.Common;
using Inventory.Domain.Products;

namespace Inventory.Application.Products;

public sealed class ProductService(IProductRepository products, TimeProvider timeProvider)
{
    public async Task<Result<int>> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var created = Product.Create(
            command.Sku, command.Name, command.Description, command.ReorderLevel, timeProvider.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return Result.Failure<int>(created.Error);

        var product = created.Value;
        if (await products.SkuExistsAsync(product.Sku, excludingProductId: null, cancellationToken))
            return Result.Failure<int>(ProductErrors.DuplicateSku(product.Sku));

        products.Add(product);
        var saved = ToResult(await products.SaveChangesAsync(cancellationToken), product);
        return saved.IsFailure ? Result.Failure<int>(saved.Error) : Result.Success(product.Id);
    }

    public async Task<Result> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await products.GetForUpdateAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound(command.Id));

        // The form carries the version the user saw; a different version means someone saved in between.
        if (product.Version != command.Version)
            return Result.Failure(ProductErrors.ConcurrencyConflict);

        var updated = product.Update(command.Sku, command.Name, command.Description, command.ReorderLevel);
        if (updated.IsFailure)
            return updated;

        if (await products.SkuExistsAsync(product.Sku, product.Id, cancellationToken))
            return Result.Failure(ProductErrors.DuplicateSku(product.Sku));

        return ToResult(await products.SaveChangesAsync(cancellationToken), product);
    }

    public Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, product => product.Deactivate(), cancellationToken);

    public Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, product => product.Activate(), cancellationToken);

    private async Task<Result> ChangeStatusAsync(int id, Action<Product> change, CancellationToken cancellationToken)
    {
        var product = await products.GetForUpdateAsync(id, cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound(id));

        change(product);
        return ToResult(await products.SaveChangesAsync(cancellationToken), product);
    }

    private static Result ToResult(SaveOutcome outcome, Product product) => outcome switch
    {
        SaveOutcome.Saved => Result.Success(),
        SaveOutcome.DuplicateSku => Result.Failure(ProductErrors.DuplicateSku(product.Sku)),
        SaveOutcome.ConcurrencyConflict => Result.Failure(ProductErrors.ConcurrencyConflict),
        _ => throw new UnreachableException($"Unknown save outcome '{outcome}'."),
    };
}
