# 20260602-0001 - Y Console, Animal UI, ActionSpeed, AutoFishing Real-Use Pass

## Status

implemented

## Source Request / Goal

Complete the 0.2.1 follow-up from `readme.md`: make the Y debug console reliable and player-sized, fix time-period skipping, render zero-progress animal hidden produce in the official animal panel, extend the config menu API for row/swatch/visibility controls, make ActionSpeed's right-click bottled-water drink and no-key bottle fill usable through native paths, and make AutoFishing F6 visibly enable and auto-cast through the native fishing state machine.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `testmods/ActionSpeedMod/*`
- `testmods/AnimalHusbandryProgressMod/*`
- `testmods/AutoFishingMod/*`
- `testmods/DebugConsoleMod/*`
- `testmods/FishBreedingAssistantMod/*`
- `testmods/OneActionCompleteMod/*`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Replaced frame-fragile Y handling with a cached Win32 key-down edge in the DTMAPI Unity input layer; the Y console consumes Y/Escape while open.
- Enlarged the reflected Unity Canvas debug console, added native item sprites, localized-only weather/destination button text, paged destination lists, and hover-only item naming.
- Changed the time debug API to advance to fixed weather-period boundaries: 06:00, 18:00, 24:00/next-day 00:00, then 06:00.
- Reworked the animal progress overlay to clone native progress-bar rows, populate missing configured husbandry entries at `0`, and refresh row text under the official mood UI without showing a generic label.
- Added experimental config controls for inline bool+number rows, color swatches, and conditional visibility/editing; ActionSpeed and AnimalHusbandryProgress use them.
- Narrowed continuous right-click drink to bottled water, kept ordinary food/drink as animation speed, and added no-key in-water empty-bottle filling through native `ItemBottle.UseAsItem`.
- Added AutoFishing F6 feedback, no-water/no-rod feedback, native `BodyController.UseFishRod` auto-cast, and retained wait-phase InstantBite evidence.

## Validation

- Build: `tools/scripts/build.ps1 -Configuration Release` passed on 2026-06-02 with `0 warning, 0 error`.
- Y console/input/UI: `docs/debug/evidence/GAME-SMOKE/20260602-022417` passed with `DebugConsoleTenYShortTaps=true`, `DebugConsoleHoldYNoFlicker=true`, visible native item icons, hover-name evidence, and clean exit. Earlier full debug API exercise remains in `GAME-SMOKE/20260601-160554`.
- Time periods: `docs/debug/evidence/GAME-SMOKE/20260602-021349` passed with `targetHour=18`, `targetHour=24`, and `targetHour=6` using native time pass; `process-check.txt` says no `DolocTown.exe`.
- Animal zero progress: `docs/debug/evidence/GAME-SMOKE/20260602-002953` captured `animal-viewer-ui-delayed.png` showing `羊毛脂 0/100` under the official mood rows.
- Config UI: `docs/debug/evidence/GAME-SMOKE/20260602-020710` captured ActionSpeed single-row controls and AnimalHusbandryProgress swatches/locked hex behavior.
- ActionSpeed real use: `docs/debug/evidence/GAME-SMOKE/20260602-004823` verified bottled-water right-click continuous drink, native bottle fill, no-key auto-fill, and existing fuel/feed/plant/harvest/resin/vegetation paths.
- AutoFishing real use: `docs/debug/evidence/GAME-SMOKE/20260602-015720` verified F6 feedback, no-water/no-rod feedback, native `UseFishRod` auto-cast, Wait phase, MiniGame phase, and InstantBite.

## Evidence Links

- Debug console hover/icon screenshot: `docs/debug/evidence/GAME-SMOKE/20260602-022417/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260602-022500/debug-console.png`
- Animal screenshot: `docs/debug/evidence/GAME-SMOKE/20260602-002953/DTMAPI-evidence/ANIMAL-001/20260602-003035/animal-viewer-ui-delayed.png`
- ActionSpeed config screenshot: `docs/debug/evidence/GAME-SMOKE/20260602-020710/DTMAPI-evidence/UI-004/20260602-020747/title-settings-config-action-speed.png`
- Animal config screenshot: `docs/debug/evidence/GAME-SMOKE/20260602-020710/DTMAPI-evidence/UI-004/20260602-020747/title-settings-config-animal-husbandry-progress.png`

## Related Records

- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback Notes

Rollback the public config API and GameBridge changes together. The migrated mods now depend on inline/swatch/visibility config controls and on GameBridge-owned native ActionSpeed, animal, time, and fishing paths.

## Follow-Up

- Future manual QA should capture the debug-console hover-name behavior in English mode as well; current smoke evidence is Chinese-first.
- AutoFishing recast/minigame completion beyond the verified auto-cast and wait-phase InstantBite path remains experimental.
