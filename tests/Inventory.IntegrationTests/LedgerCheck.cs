using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests;

internal static class LedgerCheck
{
    // The reconciliation query from the design: how many products have a cached stock
    // that differs from sum(In) - sum(Out) of their ledger. Must always be zero.
    public static Task<int> CountStockMismatchesAsync(InventoryDbContext context) =>
        context.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS Value FROM (
                SELECT p.Id
                FROM Products p
                LEFT JOIN StockMovements m ON m.ProductId = p.Id
                GROUP BY p.Id, p.CurrentStock
                HAVING p.CurrentStock <> COALESCE(SUM(CASE m.Type WHEN 'In' THEN m.Quantity ELSE -m.Quantity END), 0))
            """).SingleAsync();
}
