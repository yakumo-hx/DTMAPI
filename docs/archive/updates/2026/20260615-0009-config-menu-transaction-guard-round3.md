# 20260615-0009 Config Menu Transaction Guard Round 3

Date: 2026-06-15
Status: verified
Branch: `codex/refactor-four-step-cleanup`

## Source Request

The user asked for the second-level cleanup branch to continue maintainability hardening after the SecondMotor/MotorVehicle removal. Round 3 sub-agent review identified ConfigMenu callback isolation as a high-value, low-risk cut before mechanical file splitting.

## Summary

Hardened the title settings/config menu path so bad mod config callbacks do not leave partially-applied settings or break the title UI. Saves now snapshot current values, apply pending values transactionally, roll back on setter or save-callback failure, and keep pending edits visible for retry. Preview and cancel restore paths are guarded so failing setters are shown as item validation errors instead of escaping the render/close path.

## Changed Files

- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/updates/2026/20260615-0009-config-menu-transaction-guard-round3.md`
- `docs/updates/INDEX.md`

## Details

- Added internal callback wrapping for config page `reset`, `save`, and button callbacks.
- Changed `Save()` to snapshot current values, apply pending values, call the save callback, and roll back to the snapshot if any setter or save callback throws.
- Preserved failed pending edits after rollback so the UI can show the failing item and the player can retry after changing values.
- Changed `Reset()` to roll back if the reset callback or subsequent getter refresh fails.
- Guarded preview application and preview restore so render-time pending preview failures set `ValidationError` instead of throwing through title UI rendering.
- Guarded direct `Cancel()` calls when switching pages or closing the title settings menu so restore failures cannot prevent navigation/close.
- Added unit coverage for setter failure rollback, save callback failure rollback, successful retry after failure, and preview setter failure without render-path exception.

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\test.ps1 -Configuration Release` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: `git diff --check` with line-ending warnings only.
- Passed: Steam title settings smoke `docs/debug/evidence/GAME-SMOKE/20260616-002528`:
  - `RunStatus=Passed`
  - `TitleSettingsButton=Passed`
  - `TitleSettingsMenu=Passed`
  - `TitleSettingsMenuScreenshot=Passed`
  - `ProcessExited=Passed`
  - `NoFatalInstanceWindow=Passed`
  - `process-check.txt`: `No DolocTown.exe process found.`

The smoke validates title settings integration and clean exit. It does not inject a deliberately broken live mod callback in-game; that path is covered by unit tests.

## Evidence And Related Records

- Smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260616-002528`
- Prior hook-signature cut: `docs/updates/2026/20260615-0008-harmony-target-signature-round3.md`
- Debug index: `docs/debug/INDEX.md`

## Rollback

Revert this commit to return ConfigMenu save/reset/cancel/preview behavior to direct callback execution. If reverting only part of it, keep `ReflectedTitleMenuSettingsUi` direct-cancel guards aligned with `ConfigMenuPage.Cancel()` semantics.

## Follow-Up

- Mechanically split `ConfigMenuRegistry.cs` into registry/page/items/transactions files after this behavior is stable.
- Add a dedicated smoke fixture only if future title UI tests need to prove bad callback behavior inside a real game run.
