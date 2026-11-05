namespace Inventory.Domain.Common;

// Target names the input the error is about (e.g. "Sku"), so a form can show it next to that field.
// It is null when the error is about the request as a whole.
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation, string? Target = null);
