# Inventory & Stock Movements

A small ASP.NET Core MVC module for tracking products and their stock. Stock is never edited directly: it is
derived from a ledger of stock movements (goods received **In**, goods issued **Out**), and an Out can never
take stock below zero — even when two requests arrive at the same moment.

## Run it

Prerequisite: the **.NET 10 SDK** (`global.json` pins 10.0.x).

```bash
dotnet run --project src/Inventory.Web
```

Open the URL printed in the console (e.g. `http://localhost:5299`). On first start the app creates the SQLite
database `src/Inventory.Web/inventory.db`, applies the EF Core migration and seeds seven sample products with
movements (normal, at / below reorder level, out of stock, and one inactive product). Nothing else to set up.

- **Reset the data:** stop the app and delete `src/Inventory.Web/inventory.db`; the next start recreates it.
- **Run the tests:** `dotnet test`
- **VS Code:** F5 builds and starts the app (`.vscode/launch.json`).

## Screens

| Screen | What it does |
|---|---|
| **Product list** (`/`) | SKU, name, current stock, reorder level; red *Out of stock*, yellow *Low stock* (at or below the reorder level) and grey *Inactive* badges; search by SKU or name (case-insensitive, `%` / `_` matched literally); "Show inactive". |
| **Product detail** | Product info and current stock; chronological movement history with a running balance; form to record an In / Out movement. "Cannot issue 20. Only 7 available." is shown under the quantity. |
| **Create / edit product** | Server-side validation (client-side is an extra); SKU, name and reorder level required; duplicate SKU shown under the field; stale edits rejected; deactivate / reactivate. |

## Architecture

Clean Architecture in four projects; dependencies point inward and the compiler enforces it.

```
Web (MVC) ──► Application ──► Domain
   └──► Infrastructure (EF Core + SQLite) ──► Application
```

- **Domain** — `Product`, `StockMovement` and every business rule, with no framework dependencies. Expected
  failures return `Result` / `Error` (with a type and the input it is about) instead of throwing.
- **Application** — `ProductService`, `StockMovementService`; one write interface (`IProductRepository`) and one
  read interface (`IProductQueries`). No MediatR, AutoMapper or generic repository — not needed at this size.
- **Infrastructure** — `InventoryDbContext`, Fluent configurations, the migration, the repository with the
  atomic stock guard, read queries and the seeder.
- **Web** — thin controllers, view models with data annotations, Razor views that only display.

## Key design decisions

**Stock: a ledger plus a guarded cache.** `StockMovements` is the source of truth (stock = sum(In) − sum(Out)).
`Products.CurrentStock` is a cached copy used for reads and the guard. It is never editable: it changes only in
the same transaction as a ledger insert, EF is configured to throw if code tries to save it through change
tracking, and the ledger is append-only (triggers block `UPDATE` / `DELETE` on movements).

**The below-zero rule, including concurrent requests.** The domain checks the rule first, to give the friendly
message. Persisting then runs one statement inside a transaction:

```sql
UPDATE Products SET CurrentStock = CurrentStock - @qty
WHERE Id = @id AND IsActive = 1 AND CurrentStock >= @qty
```

The check and the change are a single statement under the database's write lock, so no other request can slip
in between. If it updates 0 rows, nothing is written and the service re-reads the product so the message shows
the current figure. A `CHECK (CurrentStock >= 0)` constraint is the last line of defence. Integration tests fire
2 and 20 simultaneous Outs at a real SQLite database and assert that exactly the available stock is issued.

Alternatives considered: computing stock on every read (no database-level guarantee and a retry loop),
check-in-C#-then-save (loses updates under concurrency), a separate balance table (an extra table for no gain
here), and a cache maintained by database triggers (logic hidden in the database).

**Soft deactivate instead of delete.** A product's movements are its stock history and the source of truth for
its stock; deleting the product would destroy that history (or orphan it). Deactivating hides the product from
the list and blocks new movements, keeps all history, and is reversible. The foreign key is `ON DELETE RESTRICT`,
so the database refuses a hard delete of a product with history anyway.

**Edits by two people at once.** The edit form carries the product's `Version`; saving with an outdated one shows
"This product was changed by someone else" and writes nothing. Recording a movement does not change the version,
so it never causes a false edit conflict.

**Forms.** Every POST is protected by an anti-forgery token. Successful posts redirect (Post/Redirect/Get); a page
shown after a failed post reloads as a fresh page instead of re-posting.

## Assumptions

- SKUs are case-insensitive: they are trimmed and stored in upper case, and must be unique.
- Reorder level must be entered (the create form starts empty); the brief's "default 0" is the database default.
- One movement is at most 1,000,000 units (a typo guard); stock is stored as a 64-bit number.
- Inactive products cannot record movements until reactivated.
- Times are stored and shown in UTC.
- SQLite was chosen over EF In-Memory because In-Memory has no constraints, transactions or triggers — it would
  hide exactly the problems this exercise is about.

## Not done / what I'd do next

- **Friendly error and 404 pages:** unknown products return a plain 404 and unexpected errors use the template's
  error page; I'd add proper status-code pages and `{id:int}` route constraints.
- **Stretch goals:** one is done — optimistic concurrency on product edits (see *Edits by two people at once*).
  Pagination / sorting, CSV export, the dashboard, audit fields / structured logging and the JSON endpoint were
  deliberately left out to keep the core solid.
- **At scale:** rebuild the cached stock from the ledger periodically (or keep snapshots), move to SQL Server, and run migrations as a deployment step instead of at startup.

See [AI_NOTES.md](AI_NOTES.md) for how AI tools were used.
