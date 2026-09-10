# 20260601-0002 DTMAPI 0.2.1 Y Console And Migrated Mod Polish

## Source Request / Goal

- Continue the existing DTMAPI workspace from the 0.2.0 debug-console implementation; do not rebuild from zero.
- Bump to 0.2.1.
- Productize the debug console as a Y-key console, add time skip and movement speed debug APIs, and keep fragile native logic in `DTMAPI.GameBridge.DolocTown`.
- Polish migrated ActionSpeed, FishBreedingAssistant, AnimalHusbandryProgress, and OneActionComplete behavior/config.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/DebugConsoleMod/*`
- `testmods/ActionSpeedMod/*`
- `testmods/AnimalHusbandryProgressMod/*`
- `testmods/FishBreedingAssistantMod/*`
- `testmods/OneActionCompleteMod/*`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Known Facts And Rejected Hypotheses

- Known: 0.2.0 T-console was functional, but T conflicts with gameplay input. 0.2.1 uses Y only.
- Known: the real movement-speed native method is `MotionAbility.SetMoveScaler(float)`, not `SetMoveSpeedScale(float)`. Retained smoke `GAME-SMOKE/20260601-132044` failed only on that method-name mismatch.
- Known: DirectExe launch with the official-local Y console can exit before save load through the game's official reload path with `Steamworks is not initialized`; retained at `GAME-SMOKE/20260601-131105`. Passing smoke evidence uses Steam launch.
- Known: `-AutoOpenAnimalPanel` leaves the official animal panel open; combining it with ActionSpeed/OneAction gameplay exercise waits forever for `NormalGameState`. Retained interrupted evidence `GAME-SMOKE/20260601-133839`; clean evidence is split into animal-only and action/one-action runs.
- Rejected: putting the ordinary debug console mod under `BepInEx/plugins`.
- Rejected: implementing time skip, movement speed, inventory, weather, teleport, or one-action energy by raw save/coordinate/stat edits.

## Implementation

- Bumped runtime/package version to 0.2.1.
- Renamed/packaged `DTMAPI.DebugConsoleMod` as the official-local `DTMAPI_YKeyConsole` package, with Chinese-first `official-info.json`, icon/preview, and official enablement-state support.
- Changed the debug console hotkey from T to Y and added a config-menu language setting (`Auto`, `schinese`, `english`).
- Reworked the console layout into a first-screen item grid on the left and time, movement speed, weather, and teleport controls on the right.
- Extended debug APIs:
  - `IDebugConsoleApi.SetLanguage`
  - `ITimeDebugApi`
  - `IMovementDebugApi`
  - inventory search metadata (`EnglishName`, `Tags`, `SearchText`)
- Implemented time skip through `ArchiveDataHandle.PassTimeNoControl` plus `DolocAPI.OnWakeUp(false,true,false)`.
- Implemented movement speed through native `MotionAbility.SetMoveScaler(multiplier - 1)` and reset on return-to-title.
- Simplified ActionSpeed config text and rechecked interaction slices.
- Changed FishBreedingAssistant to title-only decoration and removed details config from the player-facing menu.
- Changed AnimalHusbandryProgress rendering to a native-style produce line under mood text, without the `隐藏产物` label, and added color preset/custom hex UI.
- Changed OneActionComplete resource completion to pay native tool energy for extra hits before applying extra completion damage.

## Validation

- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release -SkipTests`
  - Passed with 0 warnings and 0 errors.
- Y console/API third-save smoke:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 320 -AutoExerciseDebugConsole -AutoExerciseDebugInventory -AutoExerciseDebugWeather -AutoExerciseDebugTeleport -AutoExerciseDebugTime -AutoExerciseDebugMovement -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260601-140915`.
- Animal UI smoke:
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260601-135144`.
- ActionSpeed and OneAction smoke:
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260601-135239`.
- Official Mod UI smoke:
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260601-135332`.
- Official disabled-state smoke:
  - Temporary state under `docs/debug/evidence/OFFICIAL-ENABLE/20260601-135520`.
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260601-135531`.
  - State restored to `Local.DTMAPI_YKeyConsole.enabled=true`.

## Evidence

- Y console/API: `GAME-SMOKE/20260601-140915/result.json` has `DebugConsoleOpenY1=true`, `DebugConsoleCloseEscape=true`, `DebugConsoleOpenY2=true`, `DebugConsoleCloseY=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTeleport=true`, `DebugTime=true`, `DebugMovement=true`, `SaveLoaded=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- Inventory: `Smoke exercise DebugInventory OK item=wood, display=木头, before=0, after=1, given=1`.
- Weather: `Smoke exercise DebugWeather OK options=7 ... before=THUNDERSTORM, after=CLOUDY, display=多云`.
- Teleport: `Smoke exercise DebugTeleport OK destination=农场 ... changedRoom=True, distance=138.882`.
- Time: this record originally captured the first 0.2.1 time smoke. The fixed weather-boundary sequence is superseded by `20260602-0001` and `GAME-SMOKE/20260602-021349`, which records `targetHour=18`, `targetHour=24`, and `targetHour=6`.
- Movement: `Smoke exercise DebugMovement OK levels=0.5x:6,1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`.
- Animal: `GAME-SMOKE/20260601-135144` logs `marker=produce-progress-bar` and `羊毛脂 <color=#FF942E>... 99/100`.
- ActionSpeed: `GAME-SMOKE/20260601-135239` logs `Smoke exercise ActionSpeedInteraction OK ... pending=none`.
- OneAction: `GAME-SMOKE/20260601-135239` logs `nativeDamage=4, paidExtraHits=2/2`.
- Official UI: `GAME-SMOKE/20260601-135332` logs `selected=Local.DTMAPI_YKeyConsole, title=Y键控制台`.
- Official disabled-state: `GAME-SMOKE/20260601-135531` logs `Skipping DTMAPI.DebugConsoleMod: 此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。`
- Exit checks for passing smokes say `No DolocTown.exe process found.`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `DEBUGCONSOLE-001`, `DEBUGITEMS-001`, `DEBUGWEATHER-001`, `DEBUGTELEPORT-001`, `DEBUGTIME-001`, `DEBUGMOVE-001`, `ACTIONSPEED-002`, `ONEACTION-001/002/003`, `FISHROE-001`, `ANIMAL-001`, `OFFICIAL-001`, `OFFICIAL-004`
- `docs/hook-map/README.md`: `UI.DebugConsoleHost`, `Debug.InventoryWeatherTeleportApis`, `Debug.TimeMovementApis`, and migrated mod hook entries
- `docs/api/public-api-matrix.md`: 0.2.1 experimental debug API additions
- `docs/debug/INDEX.md`: DirectExe Steamworks blocker and animal-panel smoke sequencing note

## Rollback Notes

- If Y console UI regresses, disable `DTMAPI_YKeyConsole` through the official Mod UI or `SAVE/mod_infos.json`; the core GameBridge APIs remain experimental and can stay registered.
- If `MotionAbility.SetMoveScaler` changes in a future build, keep movement speed failed/pending with result DTO evidence rather than writing player fields directly.
- If time skip shows crop/schedule side effects, mark `ITimeDebugApi` blocked/partial and narrow it to a smoke-only tool until longer day-transition evidence is available.

## Follow-Up

- Add a harness step that closes official animal panels before running gameplay smokes, so animal UI and ActionSpeed/OneAction evidence can safely combine.
- Add deeper time-skip validation for crop growth, schedules, and weather visuals across the next 18:00/24:00 boundary.
- Render real item sprites in the item grid after a focused Unity Sprite/Image reflection pass.
