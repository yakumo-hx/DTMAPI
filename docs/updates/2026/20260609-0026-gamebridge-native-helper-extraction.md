# 20260609-0026 GameBridge Native Helper Extraction

## Metadata

- Update ID: 20260609-0026
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/refactor-gamebridge-native-helpers`.
- Owner: Codex

## Scope

- Extract shared GameBridge reflection/native access helpers behind an internal helper class.
- Preserve `DolocTownExperimentalBridgeApi` static compatibility wrappers for existing partial files.
- Make `ActionCompletionService` stop using `using static DolocTownExperimentalBridgeApi`.
- Keep ActionCompletion gameplay behavior, hook IDs, status text, result schema, and public API unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeHelpers.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionService.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0026-gamebridge-native-helper-extraction.md`

## Summary

- Added `GameBridgeNativeHelpers` as the internal owner for common native/reflection helpers, including type resolution, member reads, hierarchy method lookup, resource collider lookup, resource name/class reads, and formatting helpers used by OneAction completion.
- Changed the existing `DolocTownExperimentalBridgeApi` helper methods into thin wrappers over `GameBridgeNativeHelpers`, preserving existing partial compatibility.
- Updated `ActionCompletionService` to import `GameBridgeNativeHelpers` directly instead of statically importing `DolocTownExperimentalBridgeApi`.
- Kept `IActionCompletionApi`, `Actions.OneActionComplete`, `Actions.OneActionFuelFeed`, `Smoke.OneAction*`, and all smoke result fields unchanged.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SaveSlot 3 -TimeoutSeconds 360`

## Evidence

- OneAction game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-174124`
- Report zip pointer: `docs/debug/evidence/GAME-SMOKE/20260609-174124/latest-report.txt`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Log evidence: `Feature.ActionCompletion = ready`, unchanged `Actions.OneActionComplete = verified`, unchanged `Actions.OneActionFuelFeed = verified`, resource completion, wrong-tool matrix, fuel/feed completion, and vegetation exception verification.
- Exit evidence: `process-check.txt` reports no leftover `DolocTown.exe`; `fatal-window-check.txt` reports no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `GAMEBRIDGE-NATIVE-HELPERS-20260609`
- `docs/hook-map/README.md` section `Actions.OneActionComplete`
- `docs/api/public-api-matrix.md` row `IActionCompletionApi`
- `docs/updates/2026/20260609-0021-actioncompletion-feature-split.md`
- `docs/updates/2026/20260609-0025-shared-agentstate-lifecycle-hooks.md`

## Rollback Notes

- Point `ActionCompletionService` back to the `DolocTownExperimentalBridgeApi` static wrappers and remove `GameBridgeNativeHelpers`.
- Keep rollback limited to helper ownership; do not change OneAction native validation, hook callbacks, status IDs, or smoke schemas.

## Follow-Up

- Merge `codex/refactor-gamebridge-native-helpers` back to `Refactor`.
- Continue the `IManifest.EntryType` public API addition branch.
