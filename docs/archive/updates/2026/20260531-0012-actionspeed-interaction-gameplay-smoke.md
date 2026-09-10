# 20260531-0012 ActionSpeed Interaction Gameplay Smoke

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up after earlier title button, F6, instant-save, OneAction, and startup timing work.
- Move ActionSpeed from "shared hook exists" toward real game behavior evidence.
- Do not redo completed official local mod packaging, base Chinese localization, fish roe display, or base animal bell display.
- Do not copy or imitate DLKsmapi source.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/ActionSpeedMod/i18n/english.json`
- `testmods/ActionSpeedMod/i18n/schinese.json`
- `docs/debug/INDEX.md`
- `docs/debug/lessons.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0012-actionspeed-interaction-gameplay-smoke.md`

## Known Facts And Rejected Hypotheses

- Known: the third save can load into the indoor room `farm_大型集装箱...`, which is valid for ordinary transient equipment but not enough for tree-hosted decal equipment or vegetation evidence.
- Known: `resin_collector` is decal equipment and must be attached to a legal `IDecalHost` plus decal slot.
- Known: the failed interaction smoke at `GAME-SMOKE/20260531-144623`, collected logs `GAME-SMOKE/20260531-145122`, verified fuel, feeder, eat/drink, bottle fill, and planting, but failed resin creation with `建造贴纸设备需要传入decalHost和decalSlot参数`.
- Known: the expanded smoke at `GAME-SMOKE/20260531-153006`, collected logs `GAME-SMOKE/20260531-153525`, verified the in-water bottle-fill branch and plant-basin crop harvest but failed vegetation setup because the current farm lacked `DM_vegetation` and official `CreateVegetationNoRender` returned null.
- Rejected: treating the resin failure as an ActionSpeed hook failure. The hook was never reached because the game rejected ordinary decal creation.
- Rejected: treating `CurrentInteractableObject` alone as sufficient ActionSpeed classification evidence. Native vegetation interaction goes through `InteractableManagerEx` and its current target can be a `VegetationRenderer` wrapping the actual `Vegetation`.
- Rejected: leaving stale scanner/current equipment selections in smoke evidence. Scanner selection is cleared before water/vegetation samples so evidence does not report an unrelated `SimpleWell` or `ResinCollector`.

## Implementation

- ActionSpeed interaction smoke now requests the game's official main-farm transition when the loaded room is indoor, then retries while keeping the smoke pending instead of failing early.
- Resin smoke first looks for an existing rendered `ResinCollector`. If none is found, it creates one through the game's official decal path:
  - query `resin_collector` equipment proto,
  - inspect `FitSlots`,
  - ask `DM_terrain.GetCachedDecalHosts(slotType)`,
  - verify the target through the collector's native `HostFilter`,
  - compute the decal slot world position and anchor,
  - call `IEquipmentHost.CreateEquipment(..., decalHost, decalSlotIndex)`.
- If a test room lacks a usable tree host, the smoke can create a transient tree through `IDungeonResourceHost.CreateDungeonResource`, mature it through native growth helpers, use it as the decal host, then clean it up after verification.
- In-water bottle-fill smoke now exercises the native `InteractiveWater.IsInWater` branch, not just an `IWaterContainer` target.
- Plant-basin crop-harvest smoke matures a transient planted crop and verifies the native harvest path changes from harvestable to cleared.
- Wild vegetation smoke now tries official vegetation creation first and falls back to a direct transient `Vegetation` plus native `VegetationRenderer`/`IVegetationHost.RenderVegetation` path when the current map lacks a usable vegetation host.
- ActionSpeed classification now reads and unwraps the current native interactable from `DolocAPI.agent._Interact`/`InteractableManagerEx`, so vegetation renderer targets classify as `Harvest` instead of `Unknown`.
- The smoke clears stale room-scanner selection before water and vegetation samples.
- The ActionSpeed pending logger now records interaction readiness under `Smoke.ActionSpeedInteraction` instead of accidentally reusing `Smoke.ActionSpeedTool`.
- The ActionSpeed config page/status text now labels the core behavior row as verified in Chinese and English while keeping AutoFillBottle auto-trigger text experimental.

## Validation

- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
- First full interaction smoke after resin fix:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 260 -AutoExerciseActionSpeedInteraction -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-150138`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-150224`.
- Final recheck after correcting the pending status key:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 260 -AutoExerciseActionSpeedInteraction -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-150334`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-150416`.
- Expanded interaction smoke before vegetation fallback:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 280 -AutoExerciseActionSpeedInteraction -SkipBuild`
  - Failed only on vegetation setup: `docs/debug/evidence/GAME-SMOKE/20260531-153006`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-153525`; in-water bottle fill and plant-basin crop harvest were already verified in this run.
- Final clean expanded interaction smoke after direct transient vegetation fallback and scanner-selection clearing:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 280 -AutoExerciseActionSpeedInteraction -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-154400`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-154445`.
- Final config/status text build and title-menu visual smoke:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 120 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-155247`, collected logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-155436`.

## Evidence

- `result.json` in `GAME-SMOKE/20260531-154400` has `ActionSpeedInteraction=true`, `SaveLoaded=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`.
- `process-check.txt` in `GAME-SMOKE/20260531-154445` says `No DolocTown.exe process found.`
- Title-menu screenshot `docs/debug/evidence/GAME-SMOKE/20260531-155436/DTMAPI-evidence/UI-004/20260531-155324/title-settings-menu.png` shows the ActionSpeed row as `动作加速（核心行为已验证）`.
- The same title smoke result has `TitleSettingsMenuScreenshot=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, `ForcedClose=false`, and `process-check.txt` says `No DolocTown.exe process found.`
- Transition evidence: logs show the smoke requested official transition from `farm_大型集装箱...` to `farm_type1-平地`.
- Fuel-machine evidence: `wood` count `3->0`, ratio `0->0.5`, `actionSpeedDelta=1`, native `AgentStateInteract`.
- Feeder evidence: `roughage_feed` count `3->1`, ratio `0->1`, `actionSpeedDelta=1`, native `AgentStateInteract`.
- Eat/drink evidence: `AgentStateEat` plus `UseItemContinues` changed `dt=0.2->0.6`, count `3->2`.
- Bottle-fill evidence: `SimpleWell` water `100->95`, bottle count `3->2`, continuous-use `dt=0.2->0.6`.
- In-water bottle-fill evidence: `branch=InteractiveWater.IsInWater`, bottle count `3->2`, `actionSpeedDelta=2`, `continuousDelta=1`, scanner selection cleared to `equipment=none,interactable=none`.
- Planting evidence: `seed_endyam` count `3->2`, target `PlantBasinSimple`, `isPlanted=True`.
- Plant-basin crop-harvest evidence: mature `seed_endyam` crop changed `couldHarvest=True->False`, `afterCrop=null`, bridge classified the interaction as `Harvest`.
- Resin evidence: existing tree-hosted `ResinCollector` on `DungeonResourceTree` level 4, `currentValue=3->0`, `actionSpeedDelta=1`, native `AgentStateInteract`, bridge kind `Harvest`.
- Wild vegetation evidence: transient `endyam/DolocTown.VegetationCrop` was rendered through native `VegetationRenderer`, touched through `InteractableManagerEx`, harvested by native `Vegetation.OnInteract`, and bridge classified it as `Harvest`.
- Final summary evidence: `Smoke exercise ActionSpeedInteraction OK ... pending=none`, followed by `Smoke.ActionSpeedInteraction = verified`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `CONFIG-005`, `ACTIONSPEED-001`, `ACTIONSPEED-002`
- `docs/hook-map/README.md`: `ActionSpeed.ToolAnimation`
- `docs/debug/lessons.md`: `2026-05-31: Decal Equipment Needs The Official Host Path`
- Previous partial record: `docs/updates/2026/20260531-0011-runtime-behavior-013.md`

## Rollback Notes

- If future Doloc Town builds change decal host APIs, disable only the resin smoke creation fallback; the already verified fuel/feed, eat/drink, bottle fill, planting, crop harvest, and vegetation smoke samples use separate native paths.
- If future Doloc Town builds change vegetation host APIs, disable only the direct transient vegetation fallback and leave the harvested-vegetation status pending until a new native host path is found.
- If main-farm transition becomes unreliable, keep the interaction smoke pending and record the room state instead of falling back to direct EXE or fake resin collection.
- Do not remove the decal-host lesson; ordinary equipment creation is expected to fail for decals.

## Follow-Up

- No ActionSpeed interaction variant from the 0.1.13 follow-up remains pending after `GAME-SMOKE/20260531-154400` / `GAME-SMOKE/20260531-154445`.
- AutoFillBottle automatic triggering remains experimental UI/config behavior; the underlying IWaterContainer and in-water bottle-fill paths are verified.
- Startup 30-second abnormal comparison remains pending; the current successful runs are normal startup/interaction evidence, not a captured abnormal-launch sample.
