# 20260615-0012 ConfigMenu File Split Round 3

## Status

verified-static

## Area

config-menu/ui/maintainability

## Source Request / Goal

Second-level cleanup/refactor branch Round 3 maintenance work after the ConfigMenu transaction hardening and follow-up review.
The goal is to reduce `ConfigMenuRegistry.cs` from a mixed registry/page/item/transaction file into smaller ownership files without changing public API or title settings behavior.

## Changed Files

- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuItems.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuTransactions.cs`
- `docs/debug/INDEX.md`
- `docs/updates/INDEX.md`

## Summary

- Kept `ConfigMenuRegistry.cs` focused on public `IDtmConfigMenuApi` registration and internal `IConfigMenuRuntime` entry points.
- Moved page editing state, pending preview, save/reset/cancel rollback, and lock checks into `ConfigMenuPage.cs`.
- Moved all config item implementations into `ConfigMenuItems.cs`.
- Moved the callback wrapper/exception description helper into `ConfigMenuTransactions.cs`.
- No behavior, API, validation text, or UI rendering logic was intentionally changed.

## Validation

- `git diff --check`
  - Passed with line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release`
  - Passed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.

## Evidence

- Static/build evidence only.
- No game smoke was run for this file split because it is a mechanical ownership split after the previously smoke-tested transaction behavior.
- Validation was run while unrelated/parallel AudioOverride working-tree changes were present in `src/DTMAPI.GameBridge.DolocTown`; they are not part of this update.

## Related Records

- `docs/updates/2026/20260615-0009-config-menu-transaction-guard-round3.md`
- `docs/updates/2026/20260615-0011-config-menu-cancel-reset-followup.md`
- `docs/debug/INDEX.md`

## Rollback Notes

Revert this update by moving `ConfigMenuPage`, `ConfigMenuItemBase` and item types, and `ConfigMenuCallbackRunner` back into `ConfigMenuRegistry.cs`.
Because this is a file split, rollback should not require data migration or config reset.

## Follow-Up

- Continue Round 3 maintenance cuts only after keeping ConfigMenu transaction tests green.
- The separate manual QA issue where too many config options exceed the visible UI still requires a title settings scrolling/layout implementation; this file split does not address that UI capacity bug.
