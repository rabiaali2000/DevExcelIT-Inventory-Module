using System.ComponentModel.DataAnnotations;
using Inventory.Application.Products;
using Inventory.Domain.Products;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.ViewModels;

public sealed class RecordMovementViewModel
{
    [Required(ErrorMessage = "Choose In or Out.")]
    public MovementType? Type { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, ProductRules.MaxMovementQuantity, ErrorMessage = "Quantity must be a whole number between {1} and {2}.")]
    public int? Quantity { get; set; }

    [StringLength(ProductRules.NoteMaxLength, ErrorMessage = "Note cannot be longer than {1} characters.")]
    public string? Note { get; set; }

    public static IEnumerable<SelectListItem> TypeOptions() =>
    [
        new("In — goods received", nameof(MovementType.In)),
        new("Out — goods issued", nameof(MovementType.Out)),
    ];

    // Call only after ModelState.IsValid, when [Required] guarantees Type and Quantity have values.
    public RecordMovementCommand ToCommand(int productId) => new(productId, Type!.Value, Quantity!.Value, Note);

    public string SuccessMessage() =>
        Type == MovementType.In ? $"Received {Quantity:N0}." : $"Issued {Quantity:N0}.";
}
