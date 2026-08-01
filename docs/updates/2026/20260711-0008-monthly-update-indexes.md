# 20260711-0008 — Monthly Update indexes

## Metadata

- Update ID: `20260711-0008`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: user approved changing high-volume annual Update detail into monthly ledgers before the next real code review/test/fix task.

## Scope

- split 2026 Update rows into May, June, and July monthly ledgers without losing or rewriting historical rows;
- make `INDEX.md` a year router and `INDEX-2026.md` a month router;
- write each new Update row only once, in `INDEX-YYYY-MM.md`;
- upgrade consistency checks for monthly ownership, year routing, root routing, duplicates, links, and size limits.

## Changed Files

- `AGENTS.md`
- `docs/onboarding/current-state.md`
- `docs/updates/INDEX.md`, `docs/updates/INDEX-2026.md`
- `docs/updates/INDEX-2026-05.md`, `docs/updates/INDEX-2026-06.md`, `docs/updates/INDEX-2026-07.md`
- `docs/updates/README.md`
- `docs/workflows/document-governance.md`
- `tools/scripts/check-doc-governance.ps1`, `tools/scripts/README.md`

## Validation

- Mechanical split preserved all 418 Update rows: May 35, June 301, July 82.
- Update files: 418; total rows across monthly ledgers: 418.
- Root index is 659 bytes; 2026 year router is 509 bytes; monthly ledgers are 11,607 / 121,181 / 48,809 bytes.
- PowerShell parser check passed for `check-doc-governance.ps1`.
- `tools/scripts/check-doc-governance.ps1`: `Document governance: OK (3866 checks)`.
- Checked 430 Markdown links across changed/new Markdown files; 0 broken links.
- Full `tools/scripts/test.ps1 -Configuration Release`: all builds completed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`; integrated governance checks passed.
- `git diff --check`: passed with line-ending normalization warnings only.
- No game install, runtime lock, game launch, or smoke run occurred; runtime validation was not required.

## Rollback Notes

- Revert the final commit to restore the annual detail ledger and prior checker behavior.

## Follow-Up

- Create the next month ledger only when that month's first Update is added.
- If one month exceeds the enforced 192 KiB limit, split that month further rather than expanding root/year routers.
