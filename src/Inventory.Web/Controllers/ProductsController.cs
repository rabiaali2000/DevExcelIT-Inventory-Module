using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Domain.Common;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public sealed class ProductsController(
    IProductQueries queries, ProductService products, StockMovementService movements) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool showInactive, CancellationToken cancellationToken)
    {
        var items = await queries.SearchAsync(search, showInactive, cancellationToken);
        return View(ProductListViewModel.Create(search, showInactive, items));
    }

    [HttpGet]
    public Task<IActionResult> Details([FromRoute] int id, CancellationToken cancellationToken) =>
        DetailsViewAsync(id, new RecordMovementViewModel(), cancellationToken);

    [HttpPost("Products/{id:int}/Movements")]
    public async Task<IActionResult> RecordMovement(
        [FromRoute] int id, RecordMovementViewModel form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var recorded = await movements.RecordAsync(form.ToCommand(id), cancellationToken);
            if (recorded.IsSuccess)
            {
                TempData[StatusMessages.Success] = form.SuccessMessage();
                return RedirectToAction(nameof(Details), new { id });
            }

            if (recorded.Error.Type == ErrorType.NotFound)
                return NotFound();

            ModelState.AddError(recorded.Error);
        }

        return await DetailsViewAsync(id, form, cancellationToken);
    }

    [HttpGet]
    public IActionResult Create() => View(new ProductFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(ProductFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(form);

        var created = await products.CreateAsync(form.ToCreateCommand(), cancellationToken);
        if (created.IsFailure)
        {
            ModelState.AddError(created.Error);
            return View(form);
        }

        TempData[StatusMessages.Success] = $"Product “{form.Name.Trim()}” created.";
        return RedirectToAction(nameof(Details), new { id = created.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id, CancellationToken cancellationToken)
    {
        var product = await queries.FindAsync(id, cancellationToken);
        return product is null ? NotFound() : View(EditProductViewModel.From(product));
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromRoute] int id, EditProductViewModel form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var updated = await products.UpdateAsync(form.ToUpdateCommand(id), cancellationToken);
            if (updated.IsSuccess)
            {
                TempData[StatusMessages.Success] = $"Changes to “{form.Name.Trim()}” saved.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (updated.Error.Type == ErrorType.NotFound)
                return NotFound();

            ModelState.AddError(updated.Error);
        }

        return await RedisplayEditAsync(id, form, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate([FromRoute] int id, CancellationToken cancellationToken) =>
        AfterStatusChange(id, await products.DeactivateAsync(id, cancellationToken),
            "Product deactivated. It's hidden from the list (tick “Show inactive” to see it) and its history is kept.");

    [HttpPost]
    public async Task<IActionResult> Activate([FromRoute] int id, CancellationToken cancellationToken) =>
        AfterStatusChange(id, await products.ActivateAsync(id, cancellationToken), "Product reactivated.");

    // Used by the GET and by a failed movement POST, so both show the same, freshly read page.
    private async Task<IActionResult> DetailsViewAsync(int id, RecordMovementViewModel form, CancellationToken cancellationToken)
    {
        var product = await queries.FindAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        var history = await queries.GetMovementsAsync(id, cancellationToken);
        return View(nameof(Details), ProductDetailViewModel.Create(product, history, form));
    }

    private async Task<IActionResult> RedisplayEditAsync(int id, EditProductViewModel form, CancellationToken cancellationToken)
    {
        var product = await queries.FindAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        form.ShowCurrentState(product);
        return View(form);
    }

    private IActionResult AfterStatusChange(int id, Result result, string successMessage)
    {
        if (result.IsSuccess)
            TempData[StatusMessages.Success] = successMessage;
        else if (result.Error.Type == ErrorType.NotFound)
            return NotFound();
        else
            TempData[StatusMessages.Error] = result.Error.Message;

        return RedirectToAction(nameof(Edit), new { id });
    }
}
