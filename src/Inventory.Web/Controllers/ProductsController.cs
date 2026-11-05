using Inventory.Application.Abstractions;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public sealed class ProductsController(IProductQueries queries) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool showInactive, CancellationToken cancellationToken)
    {
        var products = await queries.SearchAsync(search, showInactive, cancellationToken);
        return View(ProductListViewModel.Create(search, showInactive, products));
    }
}
