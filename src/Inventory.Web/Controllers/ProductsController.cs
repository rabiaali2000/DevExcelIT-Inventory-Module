using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public sealed class ProductsController(IProductQueries queries, ProductService products) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool showInactive, CancellationToken cancellationToken)
    {
        var items = await queries.SearchAsync(search, showInactive, cancellationToken);
        return View(ProductListViewModel.Create(search, showInactive, items));
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
        return RedirectToAction(nameof(Index));
    }
}
