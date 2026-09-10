# 20260615-0011 Config Menu Cancel Reset Follow-Up

Date: 2026-06-15
Status: verified-static
Branch: `codex/refactor-four-step-cleanup`

## Source Request

Round 3 sub-agent review of `8e7415f Round 3 guard config menu transactions` found that `Cancel()` failures could still exit edit mode and that title settings page switching/closing could hide a failed restore. The user required sub-agent review and iterative fixes for each cleanup round.

## Summary

Fixed the ConfigMenu cancel/reset failure edges from the review. Failed cancel now keeps the page editing and blocks title settings page switch/close so the user still sees the pending state and item validation error. Failed reset now restores both real config values and pending UI values, avoiding partially refreshed pending state after a getter failure.

## Changed Files

- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/updates/2026/20260615-0011-config-menu-cancel-reset-followup.md`
- `docs/updates/INDEX.md`

## Details

- Kept `ConfigMenuPage.IsEditing=true` when cancel restore fails.
- Made title settings page switching stop on cancel failure instead of moving to the requested page.
- Made title settings close stop on cancel failure instead of closing the runtime UI.
- Added reset pending snapshots so reset callback/getter failures roll back both true config values and staged pending values.
- Added unit coverage for reset callback failure, reset getter failure, cancel restore failure, and throwing button callbacks.

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\test.ps1 -Configuration Release` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: `git diff --check` with line-ending warnings only.

Validation ran while an unrelated local AudioOverride working-tree change was present; that change is not part of this record or commit.

## Evidence And Related Records

- Sub-agent review: Epicurus, `019ecc1f-f673-7bd0-a803-db80aedca172`
- Prior ConfigMenu update: `docs/updates/2026/20260615-0009-config-menu-transaction-guard-round3.md`
- Runtime snapshot extraction in between: `docs/updates/2026/20260615-0010-runtime-snapshot-factory-round3.md`

## Rollback

Revert this commit together with `20260615-0009` if the ConfigMenu transaction guard must be removed. Reverting this follow-up alone restores the reviewed cancel/reset edge bug.

## Follow-Up

- Re-run title settings smoke after the unrelated AudioOverride working-tree changes are either committed or removed from the workspace.
- Mechanical ConfigMenu file splitting should wait until this follow-up is included.
