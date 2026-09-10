# 20260609-0029 FishRoeTooltip Feature Split

## Status

Verified on 2026-06-09.

## Source Request

User asked to execute the Refactor follow-up plan item `codex/refactor-fishroe-tooltip-feature`: split `FishRoeTooltipFeature` / service / hook bridge, stop having `DolocTownExperimentalBridgeApi` implement `IItemTooltipApi`, keep `Items.FishRoeTooltip` hook status and display-only semantics, and preserve FishBreedingAssistant behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/FishRoeTooltipFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/FishRoeTooltipHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/FishRoeTooltipService.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

Removed:

- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/DolocTownExperimentalBridgeApi.FishRoeTooltip.cs`

## Implementation Notes

- Added `FishRoeTooltipFeature` to the GameBridge feature host and registered `IItemTooltipApi` through `FishRoeTooltipService`.
- Moved fish roe provider state, lookup handling, display decoration, and provider error recording into `FishRoeTooltipService`.
- Moved `DolocTown.Item.get_title`, `DolocTown.Item.get_description`, and `DolocTown.Item.GetDetailInfo` hook ownership into `FishRoeTooltipHookBridge`.
- Kept the existing `Items.FishRoeTooltip` status key and status text.
- Updated item-display hook callbacks to call `Bridge.FishRoeTooltipService` instead of `Bridge.ExperimentalApi`.
- Removed `IItemTooltipApi` and fish roe provider state from `DolocTownExperimentalBridgeApi`.
- Added a smoke-only fallback provider inside `TryExerciseFishRoeTooltipForSmoke()` after first checking the player-facing provider path. This is needed because the public shareable `FishBreedingAssistantMod/Generated/FishBreedingLookup.g.cs` intentionally contains placeholder data instead of private lookup rows, and the local `Yuuka.DTMAPI.FishBreedingAssistant` config was disabled during validation.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed, 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed, 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with CRLF warnings only.
- DirectExe third-save smoke:

```powershell
tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseExperimentalHooks -SaveSlot 3 -TimeoutSeconds 240
```

Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-181533`

Result highlights:

- `RunStatus=Passed`
- `StartupLog=Passed`
- `GameLaunched=Passed`
- `SaveLoaded=Passed`
- `ExperimentalHooks=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`
- `ForcedClose=Passed`

Log evidence:

- `Items.FishRoeTooltip = verified. Patched item display paths for fish roe providers; verified by FISHROE-001.`
- `Feature.FishRoeTooltip = ready. Safe feature host dispatch completed InstallHooks for this GameBridge feature. Feature status: id=FishRoeTooltip, lastOperation=InstallHooks, success=True, failureCount=0, lastError=none.`
- `Smoke fish roe fallback provider registered for public placeholder lookup validation.`
- `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`
- `Smoke.FishRoeTooltip = verified. Generated fish roe item and observed decorated tooltip text.`
- `Smoke.AnimalViewerRendering = verified. Constructed animal viewer data and observed independent hidden-produce progress row.`
- `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.`

Process/fatal evidence:

- `docs/debug/evidence/GAME-SMOKE/20260609-181533/process-check.txt`: no `DolocTown.exe` process found.
- `docs/debug/evidence/GAME-SMOKE/20260609-181533/fatal-window-check.txt`: no fatal instance popup found.

Retained precondition smoke:

- `docs/debug/evidence/GAME-SMOKE/20260609-180405` reached `Items.FishRoeTooltip = verified` and `Feature.FishRoeTooltip = ready`, but failed `Smoke.FishRoeTooltip` because no enabled public provider lookup produced decoration before the smoke-only fallback path was added.

Note: an attempted parallel build/test validation hit an `obj` file lock while both scripts wrote `DTMAPI.GameBridge.DolocTown.dll`; the subsequent serial `test.ps1` validation passed cleanly.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`, row `IItemTooltipApi`
- Hook map: `docs/hook-map/README.md`, row `Items.FishRoeTooltip`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`, row `FISHROE-TOOLTIP-FEATURE-SPLIT-20260609`
- Earlier mechanical file split: `docs/updates/2026/20260608-0009-gamebridge-fishroe-feature-split.md`

## Rollback Notes

- Remove `FishRoeTooltipFeature`, `FishRoeTooltipService`, and `FishRoeTooltipHookBridge`.
- Restore `DolocTownExperimentalBridgeApi.FishRoeTooltip.cs`, re-add `IItemTooltipApi` to `DolocTownExperimentalBridgeApi`, and route item callbacks back to `Bridge.ExperimentalApi`.
- Re-add the old experimental bridge `Items.FishRoeTooltip` pending publication and hook-install block in `DolocTownGameBridge.InstallHarmonyHooks`.

## Follow-Up

- Keep provider arbitration and tooltip ordering as a separate API design task.
- Keep the public placeholder FishBreeding lookup boundary explicit in future audit packages; do not reintroduce private generated lookup rows into the shareable source package.
