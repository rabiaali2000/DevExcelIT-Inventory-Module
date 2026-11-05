using System.ComponentModel.DataAnnotations;
using Inventory.Application.Products;
using Inventory.Domain.Products;

namespace Inventory.Web.ViewModels;

// What the product form posts. The attributes give field-by-field feedback on the server (and in the
// browser as an extra); the domain checks the same rules again, so nothing can get past them.
public class ProductFormViewModel
{
    [Display(Name = "SKU")]
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(ProductRules.SkuMaxLength, ErrorMessage = "SKU cannot be longer than {1} characters.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(ProductRules.NameMaxLength, ErrorMessage = "Name cannot be longer than {1} characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(ProductRules.DescriptionMaxLength, ErrorMessage = "Description cannot be longer than {1} characters.")]
    public string? Description { get; set; }

    // Nullable on purpose: an empty or missing value must fail [Required] instead of silently becoming 0.
    [Display(Name = "Reorder level")]
    [Required(ErrorMessage = "Reorder level is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
    public int? ReorderLevel { get; set; }

    // Call only after ModelState.IsValid, when [Required] guarantees ReorderLevel has a value.
    public CreateProductCommand ToCreateCommand() => new(Sku, Name, Description, ReorderLevel!.Value);
}
