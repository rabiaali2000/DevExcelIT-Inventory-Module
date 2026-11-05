using System.Globalization;
using Inventory.Application.Products;
using Inventory.Domain.Products;

namespace Inventory.Web.ViewModels;

public sealed record MovementRowViewModel(
    string When,
    string TypeText,
    string TypeCssClass,
    string QuantityText,
    string? Note,
    long BalanceAfter)
{
    // History is oldest first, so the running balance shows how the ledger adds up to the current stock.
    public static IReadOnlyList<MovementRowViewModel> WithRunningBalance(IReadOnlyList<MovementItem> movements)
    {
        var rows = new List<MovementRowViewModel>(movements.Count);
        long balance = 0;
        foreach (var movement in movements)
        {
            balance += movement.SignedQuantity;
            rows.Add(From(movement, balance));
        }

        return rows;
    }

    private static MovementRowViewModel From(MovementItem movement, long balanceAfter)
    {
        var isIn = movement.Type == MovementType.In;
        return new(
            DisplayFormats.Utc(movement.CreatedUtc),
            isIn ? "In" : "Out",
            isIn ? "text-bg-success" : "text-bg-secondary",
            movement.SignedQuantity.ToString("+#,0;-#,0", CultureInfo.InvariantCulture),
            movement.Note,
            balanceAfter);
    }
}
