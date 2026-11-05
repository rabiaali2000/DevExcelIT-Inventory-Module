namespace Inventory.Application.Abstractions;

public enum SaveOutcome
{
    Saved,

    // The unique SKU index rejected the save: another product got the same SKU first.
    DuplicateSku,

    // The product's Version changed since it was loaded: someone else saved first.
    ConcurrencyConflict,
}
