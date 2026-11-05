using Inventory.Domain.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Web.Controllers;

internal static class ModelStateExtensions
{
    // Shows an error from the domain or a service next to the field it is about,
    // or at the top of the form when it is about the request as a whole.
    public static void AddError(this ModelStateDictionary modelState, Error error) =>
        modelState.AddModelError(error.Target ?? string.Empty, error.Message);
}
