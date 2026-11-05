namespace Inventory.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation);
