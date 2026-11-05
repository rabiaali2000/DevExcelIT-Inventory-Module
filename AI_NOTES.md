# AI notes

## Tools

**Claude Code** (Anthropic), in the Claude desktop app, throughout the exercise. I reviewed and ran every change in
VS Code.

## What I used it for

- **Planning and design discussion** — reading the brief, comparing design options for storing stock and
  enforcing the below-zero rule under concurrency, and writing down the trade-offs before any code.
- **Scaffolding** — the solution layout, project references, EF Core configuration and the migration.
- **Code** — domain model, services, repository, controllers and views, written slice by slice. I reviewed each
  slice and had every design choice explained before accepting it.
- **Tests and verification** — unit and integration tests, plus scripted HTTP checks of every form path.
- **Commit hygiene** — splitting the work into small commits that each build on their own.

## Where the AI was wrong and I corrected it

**Main example — how stock is stored and guarded.** The AI first proposed a cached `CurrentStock` column that
the code would check in C# and then save as a new value. I challenged it against the brief ("Computed as
sum(In) − sum(Out) per product… If you cache it for performance, it must stay correct"): read-check-save breaks
when two requests run at once. Two Outs of 5 against a stock of 7 both read 7, both pass the check and both save
2 — 10 units leave, but the stock drops by only 5. It then swung to computing stock on every read,
which had no database-level guarantee. Pushing on both led to the final design: the ledger stays the source of
truth, and the cache changes only through one guarded `UPDATE … WHERE CurrentStock >= @qty` in the same
transaction as the ledger insert, backed by a `CHECK` constraint. Integration tests with 2 and 20 simultaneous
requests prove it.

Smaller ones caught along the way:

- **"Greyed-out" inactive rows.** The AI added a `text-muted` class to inactive table rows and said they would look
  grey. In the browser they didn't — Bootstrap 5.3 sets the colour on each table cell, overriding the row. The
  useless class was removed; inactive products are shown by their badge.
- **Reloading after a failed form post.** Reloading re-posted the form and the red validation messages came back,
  which looks like a bug to a user. I asked for a fresh page on reload; the fix is a single
  `history.replaceState` line emitted only on pages that answer a POST.
- **A wrong SQLite assumption.** A test expected SQLite to report a `RESTRICT` foreign-key violation with error
  code 787; the real code is 1811, because SQLite enforces `RESTRICT` with an internal trigger. The test caught it.
- **Integer overflow.** The AI claimed a 1,000,000-per-movement cap kept `int` stock totals safe; about 2,148 such
  receipts would overflow, so stock became a 64-bit `long`.

## How I verified the output

- Built with warnings treated as errors, so nothing compiles with warnings.
- Read the generated SQL of the migration before running it.
- Ran the tests and the app after every slice, and tested each screen in the browser myself, including two tabs
  editing the same product and two tabs issuing stock at the same time.
- Trimmed the test suite at the end to what the brief asks for: the stock calculation and the below-zero rule,
  including the concurrent case.
