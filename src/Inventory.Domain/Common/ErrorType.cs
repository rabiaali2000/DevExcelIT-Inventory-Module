namespace Inventory.Domain.Common;

public enum ErrorType
{
    // The input itself is invalid, e.g. a blank SKU or a quantity of 0.
    Validation,

    // The product the request refers to does not exist.
    NotFound,

    // The input is valid but clashes with the current state, e.g. a duplicate SKU,
    // not enough stock, an inactive product or an edit based on an outdated version.
    Conflict,
}
