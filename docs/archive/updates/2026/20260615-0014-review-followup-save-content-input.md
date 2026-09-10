# 20260615-0014 Review Follow-Up Save Content Input

## Status

verified

## Area

gamebridge/hooks/core/content/input/review-followup

## Source Request / Goal

Second-level cleanup/refactor branch Round 4 strict code review follow-up.
A sub-agent review found three issues after the core helper semantics commit:

- Save/Load Harmony target signatures used `void` return type even though `DolocAPI.LoadGame(int)` and `DolocAPI.SaveGame(int)` return `bool` in the current reverse index.
- Default content asset/text queries still indexed disabled official-local package files even though item queries were changed to enabled-only.
- `IInputHelper.Suppress` documentation implied stronger same-dispatch/native suppression than the implementation provides.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260615-0013-core-helper-semantics-round4.md`
- `docs/updates/INDEX.md`

## Summary

- Corrected Save/Load Harmony target signatures from `void LoadGame(int)` / `void SaveGame(int)` to `bool LoadGame(int)` / `bool SaveGame(int)`.
- Made default content asset indexing skip disabled official-local packages, so `FindAssets` and `TryReadTextAsset` follow the same enabled-only boundary as default item queries.
- Kept all-content item diagnostics available for disabled package visibility.
- Tightened suppress wording: `Suppress` only blocks future same-frame DTMAPI `RecordInputPressed/Released` helper/event dispatch, cannot undo handlers already dispatching, and does not claim native Unity/Doloc Town input consumption.
- Added unit coverage for disabled official-local asset/text lookup and enabled package asset/text lookup.

## Validation

- `git diff --check`
  - Passed with working-copy line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release`
  - Passed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- `tools/scripts/run-game-smoke.ps1 -UseSteam -IncludeHookProbe -AutoExerciseInstantSave -SaveSlot 3 -TimeoutSeconds 240 -AutoExitAfterSecondsOverride 90`
  - Passed.

## Evidence

- Steam third-save smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260616-012415`.
- Result fields passed: `RunStatus`, `StartupLog`, `GameLaunched`, `HookProbe`, `SaveLoaded`, `InstantSave`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Key log evidence:
  - `DTMAPI runtime starting.`
  - `HookProbe GameLaunched OK`
  - `HookProbe HookStatusChanged OK Save.SaveSaving=experimental`
  - `HookProbe HookStatusChanged OK Save.SaveSaved=experimental`
  - `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
  - `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`
- Exit check: `process-check.txt` reports `No DolocTown.exe process found.`

## Related Records

- `docs/updates/2026/20260615-0008-harmony-target-signature-round3.md`
- `docs/updates/2026/20260615-0013-core-helper-semantics-round4.md`
- `docs/debug/regressions/smoke-matrix.md`
- Reverse index checked by review: `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/index/Save_Load-methods.csv`

## Rollback Notes

Rollback by restoring the prior Save/Load signature filters, allowing content asset indexing to include disabled official-local packages, and reverting the suppress wording/test refinements.
If rolled back, Save/Load hooks may fail exact-signature matching, disabled official-local package files may leak through default content asset helpers, and suppress semantics will again be easier to overclaim.

## Follow-Up

- Broader Harmony signature migration should remain feature-by-feature with matching game smoke evidence.
- Public input-helper docs should retain the narrow DTMAPI helper/event wording until native input consumption exists as a separate proven hook.
