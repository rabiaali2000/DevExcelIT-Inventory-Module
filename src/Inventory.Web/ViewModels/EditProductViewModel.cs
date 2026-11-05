using Inventory.Application.Products;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Web.ViewModels;

public sealed class EditProductViewModel : ProductFormViewModel
{
    // The version the user saw when the form was opened; saving with an outdated one is reported as a conflict.
    public Guid Version { get; set; }

    // Shown on the page, never taken from the post.
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

    // Refreshes the read-only part of the page; the user's typed values are left as they are.
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
