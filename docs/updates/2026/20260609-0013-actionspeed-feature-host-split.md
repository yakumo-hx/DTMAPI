# 20260609-0013 ActionSpeed Feature Host Split

## Status

Verified.

## Source

- Request: open a secondary branch and split a second real feature through the feature-host route, preferring `ActionSpeedFeature` over Fishing because Fishing has more state and input/minigame risk.
- Branch: `codex/refactor-actionspeed-feature`
- Requirements:
  - `IActionSpeedApi` is no longer implemented by `DolocTownExperimentalBridgeApi`.
  - `ActionSpeedFeature` registers `IActionSpeedApi`.
  - Hook status definitions remain unchanged.
  - ActionSpeed smoke still passes.
  - Do not change Fishing, Vehicle, or Equipment.

## Known Facts And Rejected Directions

- The latest reverse metadata build is `references/doloc-town/reverse/builds/23465763_workshop_38581E`.
- ActionSpeed native touchpoints remain `AgentStateTool.OnEnter/OnExit`, `AgentStateInteract.OnEnter/OnExit`, `AgentStateEat.OnEnter`, `AgentControllerState.UseItemContinues(float dt)`, `AgentStateBase.OnExit`, and native `ItemBottle.UseAsItem` for no-key auto-fill.
- Rejected: splitting Fishing in this pass because its native state surface includes fishing phases, rod state, minigame state, and input cancellation.
- Rejected: changing Fishing, Vehicle, Equipment, public API shape, status IDs, smoke result fields, or ActionSpeed gameplay policy behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/DolocTownExperimentalBridgeApi.ActionSpeed.cs` removed by rename.
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`

## Summary

- Added `ActionSpeedFeature` as the second production feature hosted through `IGameBridgeFeature`, after Camera.
- Moved `IActionSpeedApi` implementation, policy state, ActionSpeed counters, auto-fill runtime update, and animator restore ownership into `ActionSpeedService`.
- Moved ActionSpeed hook installation and original hook status output into `ActionSpeedHookBridge`.
- Removed `IActionSpeedApi` from `DolocTownExperimentalBridgeApi`; kept narrow smoke compatibility accessors there so the existing smoke harness can read the same counters/summaries without owning the feature.
- Routed ActionSpeed hook callbacks through `DolocTownGameBridge.ActionSpeedService`.
- Kept `ActionSpeed.ToolAnimation`, `ActionSpeed.InteractionAnimation`, and smoke status text/meaning unchanged.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-120153`
  - `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Exit check: `process-check.txt` says no `DolocTown.exe` process was found.
  - Fatal popup check: `fatal-window-check.txt` says no fatal instance popup was found.
- Passed: no Fishing, Vehicle, or Equipment feature diff.
- Passed: `IActionSpeedApi` appears only on `ActionSpeedFeature` registration and `ActionSpeedService` implementation in `src/DTMAPI.GameBridge.DolocTown`.

## Evidence

- Smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260609-120153`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-120153.zip`
- Startup log: `docs/debug/evidence/GAME-SMOKE/20260609-120153/DTMAPI-latest.log`
- Hook/status lines:
  - `Feature.ActionSpeed = ready`
  - `ActionSpeed.ToolAnimation = verified`
  - `ActionSpeed.InteractionAnimation = experimental`
  - `Smoke.ActionSpeedTool = verified`
  - `Smoke.ActionSpeedConfigApply = verified`
  - `Smoke.ActionSpeedAutoFillBottle = verified`
  - `Smoke.ActionSpeedInteraction = verified`
- ActionSpeed gameplay lines:
  - `ActionSpeed SaveLoaded restore boundary OK slot=2`
  - `ActionSpeed tool animation speed applied by Yuuka.DTMAPI.ActionSpeed`
  - Config apply changes the smoke multiplier to `4`.
  - Interaction smoke verifies fuel, feeder, eatDrink, bottledWaterRightClick, bottleFill, bottleFillInWater, autoFillBottle, plant, cropHarvest, resin, and vegetationHarvest summaries.

## Related Records

- `docs/hook-map/README.md` entry `ActionSpeed.ToolAnimation`
- `docs/debug/regressions/smoke-matrix.md` row `ACTIONSPEED-FEATURE-HOST-20260609`
- `docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`
- `docs/updates/2026/20260609-0008-gamebridge-feature-host-hardening.md`
- `docs/updates/2026/20260608-0012-gamebridge-actionspeed-feature-split.md`

## Rollback Notes

- If this feature-host split regresses ActionSpeed, restore the previous `DolocTownExperimentalBridgeApi.ActionSpeed.cs` partial ownership and the old GameBridge hook install block, then rerun Release build/test and the same DirectExe ActionSpeed smoke.
- Do not roll back unrelated Camera feature-host or CameraPlayable smoke case changes.

## Follow-Up

- This pass demonstrates the feature host is not a Camera-only special case. The next splits should still choose low-risk feature surfaces before Fishing, Vehicle, or Equipment.
