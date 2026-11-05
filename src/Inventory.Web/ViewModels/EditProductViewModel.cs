using Inventory.Application.Products;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Web.ViewModels;

public sealed class EditProductViewModel : ProductFormViewModel
{
    public Guid Version { get; set; }

    [BindNever] public int Id { get; set; }
    [BindNever] public long CurrentStock { get; set; }
    [BindNever] public bool IsActive { get; set; }

    public static EditProductViewModel From(ProductDetails product)
    {
        var form = new EditProductViewModel
        {
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            ReorderLevel = product.ReorderLevel,
            Version = product.Version,
        };
        form.ShowCurrentState(product);
        return form;
    }

    public void ShowCurrentState(ProductDetails product)
    {
        Id = product.Id;
        CurrentStock = product.CurrentStock;
        IsActive = product.IsActive;
    }

    // Call only after ModelState.IsValid, when [Required] guarantees ReorderLevel has a value.
    public UpdateProductCommand ToUpdateCommand(int id) =>
        new(id, Sku, Name, Description, ReorderLevel!.Value, Version);
}
