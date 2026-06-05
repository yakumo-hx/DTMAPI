# Hook Map

Every DTMAPI hook must be registered here before it becomes a stable API event/helper.

Use this template:

```md
## Hook: PublicApi.EventOrHelperName

- Status: proposed / experimental / verified / stable / disabled
- Public surface:
- Game build:
- Game method/type:
- Patch type: Postfix / Prefix / Finalizer / Transpiler / Unity callback / reflection
- Why this point:
- Failure behavior:
- Mods/tests depending on it:
- Evidence:
  - Build:
  - Save:
  - Log line:
  - Screenshot/report:
- Regression cases:
```

Default preference: Postfix or read-only reflection first, Prefix only when needed, Transpiler only with explicit review and regression evidence.

## Diagnostic: Startup.SegmentTiming

- Status: experimental
- Public surface: DTMAPI startup logs and smoke evidence only.
- Game build: 23465763 workshop
- Game method/type: Steam launch wall-clock checkpoints plus BepInEx `Awake`, DTMAPI runtime start, manifest/official MODS/workshop scans, content query index, Harmony initialization, icon loading, and mod loading.
- Patch type: timing instrumentation around existing bootstrap/runtime paths plus smoke-harness timeline capture.
- Why this point: the occasional 30s launch must be compared through segment logs instead of guessed optimizations.
- Failure behavior: if Steam never creates `DolocTown.exe`, no DTMAPI segment log exists; that is tracked as a launch-blocking smoke issue rather than DTMAPI startup slowness.
- Mods/tests depending on it: smoke harness, repeated startup sampler, startup regression matrix.
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: n/a
  - Log line: startup segment logs for `Bootstrap.Awake`, `Bootstrap.HarmonyInitialize`, `Core.Start`, `ManifestScan`, `OfficialModsScan`, `WorkshopScan`, `ContentQueryIndex`, `ModLoad`, and `IconLoad`; smoke timeline fields `LaunchToProcessMs`, `LaunchToDtmapiLogFileMs`, and `LaunchToStartupPatternMs`.
  - Screenshot/report: normal collected logs `docs/debug/evidence/GAME-SMOKE/20260531-114255`, `docs/debug/evidence/GAME-SMOKE/20260531-115235`, `docs/debug/evidence/GAME-SMOKE/20260531-124429`, `docs/debug/evidence/GAME-SMOKE/20260531-125453`, `docs/debug/evidence/GAME-SMOKE/20260531-125613`, `docs/debug/evidence/GAME-SMOKE/20260531-160943`, and `docs/debug/evidence/GAME-SMOKE/20260531-161545`; launch-blocked evidence `docs/debug/evidence/GAME-SMOKE/20260531-120507` and `docs/debug/evidence/GAME-SMOKE/20260531-123258`; analyzer report `docs/debug/evidence/STARTUP-COMPARE/20260531-162725/startup-analysis.md`; integrated title smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-163330/startup-analysis.md`; integrated timeline smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-analysis.md`; repeated startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-165252/startup-analysis.md` with primary timelines `docs/debug/evidence/GAME-SMOKE/20260531-165253/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-165447/startup-timeline.json`; extended baseline aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-170717/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-170718` through `docs/debug/evidence/GAME-SMOKE/20260531-171105`; fast pure-startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-171630/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-171631` through `docs/debug/evidence/GAME-SMOKE/20260531-171859`; threshold-summary aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-172605/startup-sample-summary.md` with two normal primary samples `docs/debug/evidence/GAME-SMOKE/20260531-172606` and `docs/debug/evidence/GAME-SMOKE/20260531-172640`; stop-on-slow aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-173318/startup-samples.md` proved `-StopOnSlowSample` using artificial `SlowLaunchThresholdMs=1`, stopped after primary sample `docs/debug/evidence/GAME-SMOKE/20260531-173318`, and did not indicate DTMAPI runtime slowness; real-threshold stop-on-slow baseline `docs/debug/evidence/STARTUP-SAMPLES/20260531-174136/startup-sample-summary.md` captured six normal samples from `docs/debug/evidence/GAME-SMOKE/20260531-174136` through `docs/debug/evidence/GAME-SMOKE/20260531-174425` with `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=6136-6165`, and `Bootstrap.Awake totalMs=587-615`; monitor normal validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175403/startup-monitor.md` captured two normal samples and `Triggered=False`; monitor artificial trigger validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175538/startup-monitor.md` stopped on `slow-launch-threshold` with artificial `SlowLaunchThresholdMs=1`; real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-180222/startup-monitor.md` captured two batches / six normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-180222` and `docs/debug/evidence/STARTUP-SAMPLES/20260531-180406`, with `Triggered=False`, `LaunchToStartupPatternMs=6142-7172`, and `Bootstrap.Awake totalMs=589-608`; follow-up real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-181048/startup-monitor.md` captured three batches / nine normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-181048`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-181230`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-181413`, with `Triggered=False`, `LaunchToStartupPatternMs=6139-7170`, and `Bootstrap.Awake totalMs=586-612`.
  - Failure-capture validation: title-settings monitor `docs/debug/evidence/STARTUP-MONITOR/20260531-182957/startup-monitor.md` stopped with `sample-failure-count-1`, while linked `docs/debug/evidence/STARTUP-SAMPLES/20260531-182958/startup-analysis.md` classified the primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-182958` as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=6153` and `Bootstrap.Awake totalMs=657`; normal no-title recheck `docs/debug/evidence/STARTUP-SAMPLES/20260531-183210/startup-sample-summary.md` recorded `LaunchToStartupPatternMs=6159`, `Bootstrap.Awake totalMs=610`, and no leftover process.
  - Longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-183958/startup-monitor.md` captured four batches / twelve normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-183958`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184141`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184325`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-184508`, with `Triggered=False`, `LaunchToStartupPatternMs=6132-6172`, `Bootstrap.Awake totalMs=584-638`, and no leftover process.
  - Follow-up longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-185341/startup-monitor.md` captured five batches / twenty normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-185341`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185551`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185759`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-190010`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-190217`, with `Triggered=False`, `LaunchToStartupPatternMs=6133-7181`, `Bootstrap.Awake totalMs=586-617`, and no leftover process.
  - External launch observer: `docs/debug/evidence/STARTUP-OBSERVE/20260531-191241/startup-analysis.md` verified that no-launch timeout does not reuse stale logs; `docs/debug/evidence/STARTUP-OBSERVE/20260531-191352/startup-analysis.md` captured an external Steam URL launch as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=7236`, `Bootstrap.Awake totalMs=230`, and cleanup evidence `process-check-after-cleanup.txt` showing no leftover process.
  - Startup comparison gate: `docs/debug/evidence/STARTUP-COMPARE/20260531-192226/startup-comparison.md` compared normal external-launch evidence with blocked/no-fresh-log evidence and reported `OnlyPreRuntimeOrBlockedAbnormalEvidence`, so the true 30-second DTMAPI runtime comparison remains pending.
- Regression cases: STARTUP-001, SMOKE-002

## Hook: GameLoop.GameLaunched

- Status: verified
- Public surface: `helper.Events.GameLoop.GameLaunched`
- Game build: 23465763 workshop
- Game method/type: DTMAPI runtime lifecycle after mod `Entry`
- Patch type: runtime dispatch
- Why this point: lets mods subscribe during `Entry` and receive a first ready signal.
- Failure behavior: if a mod handler throws, DTMAPI records the owning mod error.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.HelloDtmMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: n/a
  - Log line: `HookProbe GameLaunched OK`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260530-071017`
- Regression cases: HOOK-001

## Hook: GameLoop.UpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.UpdateTicked`
- Game build: 23465763 workshop
- Game method/type: BepInEx plugin frame callback with fallback pump
- Patch type: Unity callback / SynchronizationContext fallback / coroutine fallback
- Why this point: no game internals exposed; enough for first QoL mods and smoke probes.
- Failure behavior: if Unity `Update` does not fire, DTMAPI uses the fallback pump and logs the source.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, official-local `Yuuka.DTMAPI.ActionSpeed` hotload smoke
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe UpdateTicked OK tick=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: LOOP-001

## Hook: GameLoop.OneSecondUpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.OneSecondUpdateTicked`
- Game build: 23465763 workshop
- Game method/type: DTMAPI timer derived from frame loop
- Patch type: runtime dispatch
- Why this point: throttled periodic work without mod-side timers.
- Failure behavior: no event if frame loop stops.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.11 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe OneSecondUpdateTicked OK second=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-202404`
- Regression cases: LOOP-002

## Hook: Save.SaveLoaded

- Status: verified
- Public surface: `helper.Events.Save.SaveLoaded`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.OnAfterLoadArchiveData` event, fallback candidate `DolocAPI.AfterLoadArchiveData(bool isNewGame)`
- Patch type: UnityEvent subscription, Harmony Postfix fallback
- Why this point: Save_Load map identifies it as a medium/risky post-load candidate with no public raw game type exposure.
- Failure behavior: status remains pending; no save event is exposed as verified.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001

## Hook: Save.LoadGameRequested

- Status: verified
- Public surface: diagnostics/internal save-load evidence
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.LoadGame(int index)` / fallback `DolocTown.GameData.DataPersistenceManager.LoadGame`
- Patch type: Harmony Prefix
- Why this point: records the requested save slot before `SaveLoaded` so public save events can carry a stable slot/index without raw game types.
- Failure behavior: `SaveLoaded` still dispatches with `SaveSlot=null` if the load request cannot be observed.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `LoadGame requested for slot/index 2.`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001

## Hook: Save.SaveSaving

- Status: verified
- Public surface: `helper.Events.Save.SaveSaving`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Prefix
- Why this point: exposes a pre-save event without raw save handles.
- Failure behavior: no save-saving event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaving OK slot=4`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Hook: Save.SaveSaved

- Status: verified
- Public surface: `helper.Events.Save.SaveSaved`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Postfix
- Why this point: exposes a post-save event without raw save handles.
- Failure behavior: no save-saved event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaved OK slot=4`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Diagnostic: Debug.InstantSave

- Status: experimental
- Public surface: smoke/debug testing feature only; not a stable public API.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int index)` and `DolocAPI.LoadGame(int index)` with reflected room/position/time snapshots before save and after reload.
- Patch type: smoke/debug reflection call over native save/load methods; public APIs do not expose raw save data.
- Why this point: lets hook/API validation create a checkpoint in field, fishing, or machine-test scenes while using the game's own save path instead of writing save files directly.
- Failure behavior: if the game reloads to a forced bed/home location, DTMAPI logs the room/position delta as a limitation and keeps the feature experimental.
- Mods/tests depending on it: smoke harness only.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `Smoke instant save before`, `SaveSaving hook dispatched. slot/index=2`, `SaveSaved hook dispatched. slot/index=2`, `LoadGame requested for slot/index 2.`, and `Smoke exercise InstantSave OK ... distance=0 ... limitation=none`.
  - Screenshot/report: smoke result `docs/debug/evidence/GAME-SMOKE/20260531-115149`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-115235`; first early-sample limitation attempt `docs/debug/evidence/GAME-SMOKE/20260531-114928`.
- Regression cases: SAVE-003

## Hook: Workshop.ReloadMods

- Status: verified
- Public surface: `helper.Events.Workshop.ModListChanged`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.ModManager.ReloadMods`
- Patch type: Harmony Postfix
- Why this point: respects official ModManager and uses DTMAPI UI only for status/diagnostics. As of 2026-05-31, the notification also lets DTMAPI hot-load newly enabled, not-yet-loaded code mods without taking over official enable/disable ownership.
- Failure behavior: DTMAPI startup scan still works; if a mod is already loaded and later officially disabled, DTMAPI does not attempt DLL unload and marks config as restart-required.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.12 local build/unit passed 2026-05-31
  - Save: title homepage smoke
  - Log line: `Workshop ModListChanged hook dispatched. discoveredMods=5 hotLoaded=1`, followed by later `hotLoaded=0`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260531-011745`, state backup/restored in `docs/debug/evidence/OFFICIAL-HOTLOAD/20260531-011650`
- Regression cases: WORKSHOP-001, WORKSHOP-002

## Hook: UI.TitleSettingsEntry

- Status: verified
- Public surface: title-page DTMAPI Settings button; config pages reached through `IUiHelper.OpenConfigPage`.
- Game build: 23465763 workshop
- Game method/type: `HomePageUiState` active-context detection with a reflected Unity UI Canvas. Blocking title-page states such as `ModUiState`, `GameDataUiState`, and confirmation/menu panels are detected first so the DTMAPI button only appears on the unobstructed title homepage.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback, no official ModManager enable/disable override.
- Why this point: gives players a visible DTMAPI entry on the title homepage while leaving official mod enable/disable/order controls in the official path.
- Failure behavior: if Unity UI creation fails or the active page is not the unobstructed `HomePageUiState`, DTMAPI hides/recreates the canvas and keeps runtime ticks isolated from UI failures.
- Mods/tests depending on it: migrated config pages for `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button visible on HomePageUiState.`, `Title settings button screenshot OK`, and `Smoke exercise TitleButtonLifecycle OK startupOpen=true, closed=true, saveLoaded=True, returnedContext=HomePageUiState, reopened=true`.
  - Screenshot/report: position/localization screenshot `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-button.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; returned-title screenshot `docs/debug/evidence/GAME-SMOKE/20260531-111940/DTMAPI-evidence/UI-006/20260531-111940/title-settings-after-return.png`.
- Regression cases: UI-003, UI-004, UI-006

## Hook: UI.TitleSettingsMenu

- Status: verified
- Public surface: title-page DTMAPI Settings menu with Config, Mods, Status, Errors, Hooks, and Logs pages.
- Game build: 23465763 workshop
- Game method/type: DTMAPI reflected Unity UI Canvas opened from the title settings entry.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback.
- Why this point: replaces the temporary F8/F10 overlay route with a title-screen menu that can edit DTMAPI mod config without taking over official mod management.
- Failure behavior: menu is closed when leaving `HomePageUiState`; ordinary mod updates continue to tick if UI rendering has a recoverable failure.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.ConfigMenuExample`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button clicked.`, `Smoke automation opened DTMAPI title settings menu.`, `DTMAPI title settings menu opened.`, `Title settings menu screenshot OK`, and lifecycle reopen after returning to `HomePageUiState`.
  - Screenshot/report: title UI visual evidence `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-menu.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-111940`.
- Regression cases: UI-003, UI-004, UI-006, CONFIG-003

## Hook: UI.ConfigMenuAdvancedControls

- Status: experimental
- Public surface: `IDtmConfigMenuApi.AddInlineBoolNumberOption`, `AddInlineBoolBoolOption`, `AddColorPresetOption`, bool/text-option `isVisible`/`canEdit`, `IConfigMenuItem.IsVisible`, and `DtmColorPreset`.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings reflected Unity UI menu only; no raw game type exposure.
- Patch type: config registry plus reflected Unity UI rendering.
- Why this point: migrated mods need native-feeling compact controls without hard-coding mod-specific UI in each mod.
- Failure behavior: unsupported controls stay inside the config menu page and do not affect game runtime hooks; save/cancel/reset still use the normal config transaction model.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.AnimalHusbandryProgress`, `DTMAPI.SecondMotorMod`.
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: title homepage.
  - Log line: `GAME-SMOKE/20260603-052444/DTMAPI-latest.log` records `Smoke.TitleSettingsConfigPageScreenshot.action-speed = verified`, `auto-fishing = verified`, `animal-husbandry-progress = verified`, and `second-motor = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows ActionSpeed inline bool+number rows plus same-row `自动装水`/`强化自动装水`; `title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏`/`跳过小游戏`; `title-settings-config-animal-husbandry-progress.png` shows swatches without the right-side `Orange` label or non-Custom hex input; `title-settings-config-second-motor.png` shows the `异色飞行摩托` status page.
- Regression cases: CONFIG-008, CONFIG-009

## Hook: UI.DebugConsoleHost

- Status: experimental
- Public surface: `IDebugConsoleApi`, ordinary mod `DTMAPI.DebugConsoleMod`, and the in-save Unity Canvas debug console with source/category item browser plus time, movement, weather, and teleport controls.
- Game build: 23465763 workshop
- Game method/type: DTMAPI bootstrap Unity `Update` plus reflected Unity UI `Canvas`, `Button`, `Text`, `InputField`, and `EventTrigger`. Ordinary Y/Escape binding is registered by `DTMAPI.DebugConsoleMod`; the host consumes Y/Escape while open so gameplay hotkeys do not receive duplicate toggles. Item give uses Unity `Button` for left-click and a `Mouse1` recent-item fallback for right-click when reflected right-button events are not delivered.
- Patch type: reflection-created native Unity UI host; no IMGUI/F8/F10 overlay route.
- Why this point: keeps the debug console out of the title screen and out of ordinary `BepInEx/plugins` mod placement, while letting a normal DTMAPI mod own the player-facing hotkey.
- Failure behavior: if UI construction fails, the host logs a runtime error and the ordinary mod keeps the game playable; when the menu is open, DTMAPI blocks normal mod updates/hotkeys through the UI boundary.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugConsole` and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: `Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y`, `Debug console closed reason=Escape`, second `reason=hotkey Y`, `Debug console closed reason=Y`, smoke result `DebugConsoleTenYShortTaps=true`, `DebugConsoleHoldYNoFlicker=true`, `DebugConsoleMouseGive=true`, and `Debug console mod item UI evidence OK item=dtmapi_second_motor_key ... sourceId=Local.DTMAPI_SecondMotor`. The 0.2.5 lifecycle run logs `Debug console item search/filter state reset for ReturnedToTitle`, `... SaveLoaded slot=2 isNewGame=False`, first open `searchText=<empty> category=<empty> sourceFilter=__base`, and same-save reopens `searchText=石油 sourceFilter=Local.DTMAPI_Oil`.
  - Official UI: `GAME-SMOKE/20260601-135332` shows `selected=Local.DTMAPI_YKeyConsole, title=Y键控制台`; disabled-state evidence `GAME-SMOKE/20260601-135531` logs `Skipping DTMAPI.DebugConsoleMod` when `Local.DTMAPI_YKeyConsole.enabled=false`, with state restored from `OFFICIAL-ENABLE/20260601-135520`.
  - Screenshot/report: 0.2.5 lifecycle smoke `docs/debug/evidence/GAME-SMOKE/20260604-112250` audits `OpenEmpty=1 OpenOil=7` and no leftover process. 0.2.3 source-column/hover-detail smoke `docs/debug/evidence/GAME-SMOKE/20260603-032406/result.json`; logs `docs/debug/evidence/GAME-SMOKE/20260603-032406/DTMAPI-latest.log`; screenshot `docs/debug/evidence/GAME-SMOKE/20260603-032406/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260603-032451/debug-console.png`; summary records `SourceColumns=True`, `HoverDetailTooltip=True`, `HoverEvidenceItem=dtmapi_second_motor_key`, and `SourceFilter=Local.DTMAPI_SecondMotor`. Real mouse evidence `docs/debug/evidence/GAME-SMOKE/20260603-035653` records left-click `requested=1` and right-click `requested=10` through `owner=DTMAPI.DebugConsoleMod`; exit check says no `DolocTown.exe`. Earlier Workshop source proof remains `docs/debug/evidence/GAME-SMOKE/20260602-122848`.
- Regression cases: DEBUGCONSOLE-001, OFFICIAL-001, OFFICIAL-004, INPUT-001, MANUALQA-025-Y-CONSOLE

## Hook: Debug.InventoryWeatherTeleportApis

- Status: experimental
- Public surface: `IInventoryDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, and their DTOs in `DTMAPI.Abstractions`, including 0.2.3 `InventoryDebugQuery.SourceId` and `InventoryDebugPage.Sources`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.DolocConfig.Tables.TbItem`, `DolocAPI.QueryItemProto`, `DolocAPI.CanPlaceItem`, `DolocAPI.TryPlaceInBackpack`, `DolocAPI.CountItem`, `TbWeather`, `TimeArchiveData.GetWeatherInfoOfDay`, `ArchiveDataHandle.SetWeather`, `ArchiveDataHandle.PatchWeather`, `TbStation`, `StationInfo.MarkPointId/Title`, `TbMarkPoint`, `MarkPointInfo.RoomId/Position`, and `DolocAPI.DoTransport`.
- Patch type: GameBridge-owned reflection over native tables/methods. Public APIs expose stable DTOs, not raw decompiled game types.
- Why this point: debug menus need powerful game actions, but fragile Unity/Harmony/reflection details must stay inside `DTMAPI.GameBridge.DolocTown`.
- Failure behavior: inventory full, illegal items, missing native methods, weather parse failures, and rejected transport requests return result DTOs with failure reasons and write hook-status/log evidence; teleport exposes only a whitelist and records before/after snapshots.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`, `-AutoExerciseDebugWeather`, `-AutoExerciseDebugTeleport`, and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `Smoke exercise DebugInventory OK item=wood, display=木头, before=0, after=1, given=1, modItem=dtmapi_second_motor_key ... sourceId=Local.DTMAPI_SecondMotor ... before=0, after=1, given=1`; `Smoke exercise DebugWeather OK options=7 ... before=THUNDERSTORM, after=CLOUDY`; `Smoke exercise DebugTeleport OK destination=上游丘陵1-下端 ... after={roomId=city_郊区-上游丘陵1...}, changedRoom=True, distance=24.496`.
  - Screenshot/report: 0.2.3 inventory/UI/teleport smoke `docs/debug/evidence/GAME-SMOKE/20260603-032406`; real mouse inventory give proof `docs/debug/evidence/GAME-SMOKE/20260603-035653` records `dtmapi_second_motor_key` left-click `0->1` and right-click `1->11`; earlier final Workshop inventory proof `docs/debug/evidence/GAME-SMOKE/20260602-122848`; weather smoke result and collected logs under `docs/debug/evidence/GAME-SMOKE/20260601-160554`; earlier 0.2.0 evidence remains under `GAME-SMOKE/20260601-060427`.
- Regression cases: DEBUGITEMS-001, DEBUGWEATHER-001, DEBUGTELEPORT-001

## Hook: Debug.TimeMovementApis

- Status: experimental
- Public surface: `ITimeDebugApi`, `IMovementDebugApi`, `TimeDebugState`, `TimeSkipResult`, `MovementDebugState`, and `MovementSpeedResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.GlobalParameter.Hour2Min/Day2Hour/GameMinutes2Secs`, `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp(false,true,false)`, `DolocAPI.agent.MotionAbility`, and native `MotionAbility.SetMoveScaler(float)`.
- Patch type: GameBridge-owned reflection over native time and player motion APIs. Public APIs expose DTOs, not raw game types.
- Why this point: debug time skipping and movement speed are powerful save-state/gameplay changes, so the ordinary mod only asks GameBridge for explicit experimental actions.
- Failure behavior: missing native time/motion members return failed result DTOs and hook-status messages; movement reset applies 1x again on explicit reset and `ReturnedToTitle`.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugTime`, `-AutoExerciseDebugMovement`.
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `Smoke exercise DebugTime OK transitions=step1{targetHour=18 ... after=2-1-24 18:00} -> step2{targetHour=24 ... after=2-1-25 00:00} -> step3{targetHour=6 ... after=2-1-25 06:00}`; `Smoke exercise DebugMovement OK levels=0.5x:6,1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`.
  - Screenshot/report: time smoke `docs/debug/evidence/GAME-SMOKE/20260602-021349`; movement/debug API smoke `docs/debug/evidence/GAME-SMOKE/20260601-160554`.
- Regression cases: DEBUGTIME-001, DEBUGMOVE-001

## Hook: ActionSpeed.ToolAnimation

- Status: verified for tool animation, core interaction/eat animation, bottled-water continuous drink, bottle fill, no-key auto-fill including 0.2.3 strong cooldown evidence, planting, harvest, resin, and vegetation slices.
- Public surface: `IActionSpeedApi.Configure`, `IActionSpeedApi.GetStatus`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AgentStateTool.OnEnter`, `DolocTown.AgentStateTool.OnExit`, `DolocTown.AgentStateInteract.OnEnter/OnExit`, `DolocTown.AgentStateEat.OnEnter`, `DolocTown.AgentControllerState.UseItemContinues(float dt)`, `AgentStateBase.OnExit`, and GameBridge reflection over the body/tool/tool-collider/shared interaction animators owned by active states.
- Patch type: Harmony Postfix/Prefix plus scoped reflection writes; public API does not expose raw decompiled game types.
- Why this point: keeps fragile animation-speed writes inside `DTMAPI.GameBridge.DolocTown` while the migrated ActionSpeed mod supplies only a policy and player config.
- Failure behavior: if the hook is not installed or no enabled tool policy exists, ActionSpeed remains configured/pending and no animator speed is changed. Speeds captured during `OnEnter` are restored on `OnExit` and smoke cleanup; continuous-use scaling only changes the `dt` passed to the game's own use-item timer and does not call item logic directly. Auto-fill continues to call native `ItemBottle.UseAsItem`; 0.2.3 normal/strong modes differ only by bridge cooldown and log `strong/cooldownSeconds` for smoke comparison.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `ActionSpeed tool animation speed applied by Yuuka.DTMAPI.ActionSpeed tool=old_pickaxe multiplier=3 animators=3.`, `Hook status: Smoke.ActionSpeedTool = verified. owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=3, animators=3, samples=body:1->3;tool-renderer:1->3;tool-collider:1->3`, `ActionSpeed animator speeds restored reason=AgentStateTool.OnExit restored=3.`
  - Config apply log line: `Smoke exercise ActionSpeedConfigApply OK before=... multiplier=2 ... samples=body:1->2;tool-renderer:1->2;tool-collider:1->2; after=... multiplier=4 ... samples=body:1->4;tool-renderer:1->4;tool-collider:1->4`
  - Interaction hook log line: `Hook status: ActionSpeed.InteractionAnimation = experimental. ... bottled-water right-click continuous drink ... no-key ItemBottle.UseAsItem auto-fill ...`
  - Interaction gameplay log line: `Smoke exercise ActionSpeedInteraction OK ... eatDrink={item=can ... continuous=none}; bottledWaterRightClick={item=bottle_of_water ... continuousDelta=1}; bottleFillInWater={branch=InteractiveWater.IsInWater ...}; autoFillBottle={... behavior=AutoFillBottle ... applications=1}; ... pending=none`.
  - 0.2.3 strong auto-fill log line: `GAME-SMOKE/20260603-041950` records native `ItemBottle.UseAsItem` with `strong=True`, `cooldownSeconds=0.08`, `normalCooldownSeconds=0.1`, `strongCooldownSeconds=0.08`, and `inventoryChanged=True`.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows six inline bool+number rows plus same-row `自动装水` and `强化自动装水`.
  - Screenshot/report: tool smoke result `docs/debug/evidence/GAME-SMOKE/20260531-044611`; config-apply smoke result `docs/debug/evidence/GAME-SMOKE/20260531-045330`; 0.2.1 interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260602-004823`; 0.2.3 strong interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260603-041950`; final title config screenshot smoke result `docs/debug/evidence/GAME-SMOKE/20260603-052444`.
- Regression cases: ACTIONSPEED-001, ACTIONSPEED-002, CONFIG-007, CONFIG-008
- Pending related paths: Recast/minigame fishing remains tracked separately under `Fishing.Automation`.

## Hook: Actions.OneActionComplete

- Status: verified for resource-hit path, tree/ore/garbage/weeds wrong-tool matrix, fuel/feeder native consume/fill path, and vegetation/dandelion exception classification
- Public surface: `IActionCompletionApi.Configure`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ToolCollider.HandleTools(Collider2D)` for resource hits, with native `ResourceFellData(resource,currentTool,hitPoint)` validation before applying final damage and native `DolocAPI.HasEnoughEnergyForUsingTool` / `DolocAPI.CostToolEnergy` charged for each extra hit; `DolocTown.AgentStateInteract.OnExit` for post-native fuel/feed completion using the selected `PowerGeneratorFuel` or `Feeder`; `DolocTown.VegetationRenderer.OnFell(ItemTool,Vector2)` / `Vegetation.CheckToolConstraints(ItemTool)` recorded as a native exception path which DTMAPI must not force-complete through `DungeonResourceRenderer`.
- Patch type: Harmony Postfix plus GameBridge reflection, no public raw game type exposure.
- Why this point: migrates one-action resource completion into GameBridge instead of ordinary mods owning broad Harmony patches, while preserving the game's resource/tool matching rules.
- Failure behavior: if the ToolCollider hook is not installed, policy can still be registered but gameplay resource completion remains disabled; if native validation reports a tool-type/tool-level mismatch or native energy checks reject an extra hit, DTMAPI logs the skip/partial completion and does not apply unpaid extra damage. Fuel/feed completion runs only after the game's native interaction callback and consumes extra items through `CostSelf` before calling native fill helpers. Vegetation/dandelion hits are not `DungeonResourceRenderer` resources, so DTMAPI records the native path and leaves wrong/correct tool behavior to `Vegetation.CheckToolConstraints`.
- Mods/tests depending on it: `Yuuka.DTMAPI.OneActionComplete`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `OneActionComplete SaveLoaded restore boundary OK slot=2`, `Smoke one-action resource-hit waiting: Current room has no rendered one-action resource; requested official main farm transition for smoke`, `One-action tool hook completed resource stone for Yuuka.DTMAPI.OneActionComplete damage=11.`, `Smoke exercise OneActionResourceHit OK owner=Yuuka.DTMAPI.OneActionComplete, resource=stone, tool=old_pickaxe`
  - Guard behavior: 0.1.13 logs native-validation skip reasons such as `tool-type-mismatch` and `tool-level-mismatch`; wrong-tool matrix smoke verified `Tree`, `Ore`, `Garbage`, and `Weeds` in the third save with unchanged health, `removed=False`, and `oneActionDelta=0`. Fuel/feed smoke verified `PowerGeneratorFuel` with `wood` and `Feeder` with `roughage_feed` through `Equipment.DecoratedInteract -> AgentStateInteract.OnExit`, native `CostSelf`, and native `AddFuel`/`AddFeeds`. Vegetation smoke records dandelion as `VegetationDandelion` using `Vegetation.CheckToolConstraints(ItemTool)`, not the `DungeonResourceRenderer` one-action path: wrong `old_pickaxe` is rejected, expected `old_sickle` removes through native `OnFell`, and both sides keep `oneActionDelta=0`. 0.2.1 recheck `GAME-SMOKE/20260601-135239` records energy accounting for resource completion: `nativeDamage=4, paidExtraHits=2/2, damage=6`.
  - Screenshot/report: verified positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-032318`; post-fuel/feed-hook positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-125529`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-125613`; first wrong-tool negative smoke result `docs/debug/evidence/GAME-SMOKE/20260531-130824`; full wrong-tool matrix result `docs/debug/evidence/GAME-SMOKE/20260531-133212`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-133256`; fuel/feed smoke result `docs/debug/evidence/GAME-SMOKE/20260531-140335`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-140416`; final status-text recheck `docs/debug/evidence/GAME-SMOKE/20260531-140944`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-141026`; vegetation exception smoke result `docs/debug/evidence/GAME-SMOKE/20260531-160900`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`.
- Regression cases: ONEACTION-001, ONEACTION-002, ONEACTION-003

## Hook: Fishing.Automation

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure`, `IFishingAutomationApi.SetEnabled`
- Game build: 23465763 workshop
- Game method/type: phase observation over `AgentStateFishingReady.OnEnter`, `AgentStateFishingCast.OnEnter`, `AgentStateFishingWait.OnEnter`, `AgentStateFishingWait.OnPlay`, `AgentStateFishingPull.OnEnter/OnExit`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, native `BodyController.UseFishRod`, selected quick-slot rod placement, fishable pool lookup, cast/pull body animator speed writes, and `FishingGameScrollBar.currentGameStatus=Success` minigame auto-complete.
- Patch type: Harmony Postfix for phase evidence plus GameBridge-owned native auto-cast and wait-phase `InstantBite` on `AgentStateFishingWait.OnPlay`.
- Why this point: AutoFishing must enter the game's native fishing state machine; GameBridge owns the fragile pool/rod/native-call reflection while the mod owns F6 policy and config.
- Failure behavior: policy/state can be registered; if no fishable water or rod is available, DTMAPI logs the state and does not fake fish/item rewards. In 0.2.3 player toasts are intentionally limited to F6 on/off and movement cancel; no-water/no-rod/auto-cast are not player toasts. Skip-minigame routing is smoke-proven through the wait-phase handoff to Pull. Non-skip auto-complete only marks a real `FishingGameScrollBar` as success after it has existed long enough; the smoke-only force-fish gate is used only to guarantee a native fish minigame for regression proof, not to change normal player roll outcomes.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: local slot 3 / index 2
  - Log line: `Input F6 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F6`, `AutoFishing automation disabled reason=manual-move W`, `Smoke.AutoFishingMovementCancel = verified`, `Fishing automation auto-cast invoked native BodyController.UseFishRod`, `Fishing automation animation speed applied ... phase=Pull multiplier=3`, `Smoke.AutoFishingMiniGameSkip = verified ... autoHook=AgentStateFishingPull ... autoCompleteMiniGame=True, skipMiniGame=True`, and `Smoke.AutoFishingMiniGameComplete = verified ... behavior=AutoCompleteMiniGame, status=Success, skip=false`.
  - 0.2.3 toast-policy partial smoke: `docs/debug/evidence/GAME-SMOKE/20260603-030142` verified `toastPolicy=0.2.3-suppressed-no-water-no-rod-cast`, `ProcessExited=true`, and no fatal popup.
  - 0.2.4 direct skip=false minigame smoke: failed attempt `docs/debug/evidence/GAME-SMOKE/20260603-172124` rolled `waste_plastic_bottle` and correctly did not create the native minigame; passing attempt `docs/debug/evidence/GAME-SMOKE/20260603-173435` logged `FishingGameScrollBar`, `autoHook=AgentStateFishingBattle`, `fish=loach`, `isFish=True`, `forceFishForSmoke=True`, `currentGameStatus=Success`, `visibleSeconds=0.76`, clean exit, and no fatal popup.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏` and `跳过小游戏`.
  - Screenshot/report: 0.2.3 movement/skip smoke `docs/debug/evidence/GAME-SMOKE/20260603-042437`; 0.2.4 skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; title config screenshot smoke `docs/debug/evidence/GAME-SMOKE/20260603-052444`; final old auto-cast/wait smoke `docs/debug/evidence/GAME-SMOKE/20260602-015720`; earlier wait-phase evidence retained under `GAME-SMOKE/20260531-035217` and external F6 evidence under `GAME-SMOKE/20260531-112959`.
- Regression cases: AUTOFISH-001, INPUT-004, SMOKE-002

## Hook: Items.FishRoeTooltip

- Status: verified
- Public surface: `IItemTooltipApi.ConfigureFishRoeProvider`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Item.get_title`, `DolocTown.Item.get_description`, `DolocTown.Item.GetDetailInfo`, and `DolocTown.ItemFishRoe.fishName` identity reader.
- Patch type: Harmony Postfix plus GameBridge reflection.
- Why this point: FishBreedingAssistant provides lookup data while GameBridge owns item identity and tooltip rendering fragility.
- Failure behavior: lookup provider can be registered; if item hooks do not install, no tooltip text is changed and diagnostics stay pending/failed.
- Mods/tests depending on it: `Yuuka.DTMAPI.FishBreedingAssistant`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.10 build/unit passed 2026-05-30
  - Save: local slot 3 / index 2
  - Log line: `HookProbe HookStatusChanged OK Items.FishRoeTooltip=experimental`, `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=Hatches: 鱼; Incubate: 4 小时; Grow: 6 小时`
  - 0.2.1 player-facing change: `Yuuka.DTMAPI.FishBreedingAssistant` now registers title decoration only; the old details toggle is removed from config and default options set `LabelFishRoeDetails=false`.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-150808`
- Regression cases: FISHROE-001

## Hook: Animals.ViewerRendering

- Status: verified
- Public surface: `IAnimalViewerApi.ConfigureSpecialProduceProgress`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.UI.AnimalFullInfoData(Animal)` constructor, `DolocTown.UI.AnimalViewer.Show`, `DolocTown.UI.AnimalPanel.RefreshViewer`, native progress-bar/Text child cloning, `Animal.husbandryValues`, `Animal.protoName`, and `DolocTown.Config.DolocConfig.Tables.TbHusbandry` enumeration/threshold lookup.
- Patch type: Harmony constructor/Viewer/Panel Postfix plus cached GameBridge reflection, reflected native UI row cloning, and smoke-only official `AnimalPanelUiState` open path.
- Why this point: AnimalHusbandryProgress stays an event/config mod while GameBridge owns private animal viewer data extraction and UI extension.
- Failure behavior: display policy can be registered; if viewer hooks do not install, no progress text is added and diagnostics stay pending/failed.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors; earlier 0.1.12 build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `Animal viewer progress native-like UI overlay rendered rows=1, 羊毛脂 0/100` and `Animal viewer UI delayed screenshot OK`.
  - 0.2.3 note: `20260603-0006` verifies the cloned progress row after `20260603-0003` made rows inactive until title/progress/color patching and enabled Text best-fit for cramped hidden-produce titles.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-040354/DTMAPI-evidence/ANIMAL-001/20260603-040437/animal-viewer-ui-delayed.png` shows `羊毛脂 0/100` below `饱食` and `心情`; config swatch screenshot `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-animal-husbandry-progress.png`.
  - Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-040354/process-check.txt` and `docs/debug/evidence/GAME-SMOKE/20260603-052444/process-check.txt` say no `DolocTown.exe`; fatal-window checks say no fatal instance popup.
- Regression cases: ANIMAL-001, CONFIG-008

## Diagnostic: Content.OfficialItemSourceIndex

- Status: experimental
- Public surface: `IContentQueryHelper.GetIndexedItems`, `IContentQueryHelper.GetIndexedItem`, `IContentItemInfo`, and source metadata on `InventoryDebugItem`.
- Game build: 23465763 workshop
- Game method/type: read-only filesystem scan of official local `MODS`, Steam Workshop `content/2285550`, `SAVE/mod_infos.json`, `info.json`, `Content/**/item_tbitem.json`, and related icon files; runtime give eligibility remains validated by `DolocTown.Config.DolocConfig.Tables.TbItem` and `DolocAPI.QueryItemProto`.
- Patch type: no Harmony patch; runtime/Core source indexing plus GameBridge runtime-table merge.
- Why this point: the Y console needs to show where official/Workshop items came from without modifying third-party files or assuming JSON-only rows are loaded by the game.
- Failure behavior: disabled source rows, JSON-only rows, missing runtime `TbItem`, illegal items, and full backpacks are displayed/logged as unavailable and are not given by DTMAPI.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`.
- Evidence:
  - Build: DTMAPI 0.2.2 local build passed 2026-06-02 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `Official content item source index = 108 item row(s) from 13 source mod(s)`, `Smoke exercise DebugInventory OK ... modItem=mod_butter ... sourceKind=Workshop ... sourceId=Workshop.3722791728 ... workshopRuntimeItem=verified`.
  - Screenshot/report: final butter Workshop item smoke `docs/debug/evidence/GAME-SMOKE/20260602-122848`; temporary enablement backup/restored in `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-122846`; earlier mineral-seed smoke `docs/debug/evidence/GAME-SMOKE/20260602-115735` remains retained.
- Regression cases: DEBUGITEMS-001

## Hook: Mail.ItemDelivery

- Status: experimental
- Public surface: `IMailDeliveryApi`, `MailItemDeliveryRequest`, and `MailItemDeliveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SendItemAsEmail`, `DolocAPI.CountItem`, `DolocTown.EmailManager.emails`, `DolocTown.EmailAttachReward`, and `DolocTown.RewardItem`.
- Patch type: GameBridge reflection over native game objects. Public APIs expose DTOs and never raw decompiled game types.
- Why this point: migrated DTMAPI mods need safe item-mail delivery without directly editing saves or placing fragile `DolocAPI`/mail reflection inside ordinary mods.
- Failure behavior: invalid/missing items, disabled item sources, missing native methods, native item-generation failure, missing/unknown required content source, and native rejection return failed result DTOs and log reasons. Duplicate prevention first checks native backpack count, then scans unclaimed item-mail reward attachments; if a key already exists or is pending, delivery is skipped as a successful no-op. A request can require a specific enabled official content source so disabled mods cannot send attachment-less item mail.
- Mods/tests depending on it: `DTMAPI.SecondMotorMod`, smoke harness `-AutoExerciseVehicle`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: enabled 0.2.5 smoke `GAME-SMOKE/20260604-111533` records `Mail item delivery owner=DTMAPI.SecondMotorMod item=dtmapi_second_motor_key requested=1 sent=False skipped=True backpack=0 pendingMail=1 template=send_item_template source=Local.DTMAPI_SecondMotor sourceEnabled=True sourceKnown=True success=True`, proving source-aware duplicate detection. Disabled 0.2.5 smoke `GAME-SMOKE/20260604-111901` audits `Skip=2 Registration=0 Mail=0`, proving the disabled official source never reaches native mail delivery.
  - Screenshot/report: enabled vehicle/new-content smoke `docs/debug/evidence/GAME-SMOKE/20260604-111533`; disabled-mail smoke `docs/debug/evidence/GAME-SMOKE/20260604-111901`; both process checks say no `DolocTown.exe`. Earlier first-send proof remains `docs/debug/evidence/GAME-SMOKE/20260603-051204`.
- Regression cases: VEHICLE-001, MANUALQA-025-SECOND-MOTOR

## Hook: Vehicle.MotorApi

- Status: experimental
- Public surface: `IMotorVehicleApi`, `SecondMotorOptions`, `MotorVehicleState`, `MotorVehicleRegisterResult`, `MotorVehicleSummonResult`, `MotorVehicleRideResult`, and `MotorVehicleEventArgs`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ItemMotorKey.OnUse`, `DolocTown.MotorInteractable.OnInteract`, `DolocTown.AgentControllerState.GetOnMotor`, `DolocTown.AgentControllerState.GetOffMotor`, `DolocTown.MotorController.OnFixedUpdate`, `UnityEngine.SpriteRenderer.color`, `DolocAPI.UnlockMotor(float)`, `DolocAPI.SetMotorPosition(Room, Vector2)`, `DolocAPI.EnterRoom`, `DolocAPI.Motor`, `DolocAPI.CurrentRoom`, `DolocAPI.AgentPosition`, and `AgentControllerState.motorController`.
- Patch type: Harmony Prefix/Postfix plus GameBridge reflection over Unity/game objects. Public APIs expose DTOs and never raw decompiled game types.
- Why this point: second vehicles need native key/use/riding behavior and event evidence while keeping fragile motor-controller routing inside `DTMAPI.GameBridge.DolocTown`.
- Failure behavior: if the owner/source is disabled, or the current room is in-house or disables motors, key/summon requests return failed result DTOs and log failure reasons; if clone/routing fails, the GameBridge runs DTMAPI-owned residue cleanup and restores the original `AgentControllerState.motorController` plus original motor snapshot where possible. Original `DolocAPI.Motor` is not replaced or tinted. During active second-motor map transitions only, the bridge mirrors the invisible original transform because native `DolocAPI.AgentPosition` reads the singleton motor while riding; archive room/visibility still stay restored to the original motor.
- Mods/tests depending on it: `DTMAPI.SecondMotorMod`, smoke harness `-AutoExerciseVehicle`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: enabled 0.2.5 smoke logs `Vehicle.MotorApi = experimental. Patched native motor key, riding, tuning, unlock, position, and room-entry touchpoints...`, `Second motor lifecycle cleanup reason=ReturnedToTitle`, `... SaveLoaded`, `Smoke.VehicleOriginalMotorKey = verified`, `Second motor key intercepted item=dtmapi_second_motor_key success=True`, `Smoke exercise VehicleSecondMotor dual-visible probe originalVisible=True ... secondVisible=True ... dualVisible=True`, `Smoke exercise VehicleSecondMotor appearance probe appearanceIsolated=True, originalScopedTint=0/10, secondScopedTint=5/10`, `Vehicle.SecondMotorMapTransition` target captured/applied, `Smoke exercise VehicleSecondMotor edge-transition OK ... changedRoom=True ... secondInCurrentRoom=True ... distanceToDestination=1.903, nearDestination=True, originalVisibleAfterTransition=False, originalAtNewEntry=False ... noStuck=True`, `Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot`, and `Smoke exercise VehicleSecondMotor OK ... speedMultiplier=2, baseMaxSpeed=25, effectiveMaxSpeed=50, originalVisibleAfterRestore=True`.
  - Disabled-source log line: disabled 0.2.5 smoke `GAME-SMOKE/20260604-111901` logs `Skipping DTMAPI.SecondMotorMod` and audit `Skip=2 Registration=0 Mail=0`; retained disabled-room smoke `GAME-SMOKE/20260602-112141` logged `Second motor key intercepted item=dtmapi_second_motor_key success=False reason=in-house`.
  - Screenshot/report: 0.2.5 enabled vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260604-111533`; 0.2.5 disabled-source smoke `docs/debug/evidence/GAME-SMOKE/20260604-111901`; edge-transition vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260603-202948`; appearance-isolation vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260603-082749`; prior dual-visible vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260603-051204`; status-page screenshot `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-second-motor.png`; retained 0.2.2 vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260602-122848`; historical 0.2.3 failed dual-visible probe remains `GAME-SMOKE/20260603-050208`; retained 0.2.4 edge-transition failures remain `GAME-SMOKE/20260603-194216`, `GAME-SMOKE/20260603-194923`, `GAME-SMOKE/20260603-195426`, and later tightened position failures; no fatal popup and no leftover `DolocTown.exe`.
- Regression cases: VEHICLE-001, DEBUGITEMS-001, CONFIG-009, MANUALQA-025-SECOND-MOTOR

## Diagnostic: Config.PendingPreviewConditionalVisibility

- Status: experimental
- Public surface: `IConfigMenuPendingPreview` plus existing conditional `IConfigMenuItem.IsVisible/CanEdit` renderers.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings retained Unity UI and fallback ImGui overlay.
- Patch type: runtime/config registry preview scope; no game Harmony patch.
- Why this point: conditional rows such as AnimalHusbandryProgress `填充颜色` must respond to unsaved/pending selection of the `+`/Custom color swatch before the player presses Save.
- Failure behavior: if preview fails, committed preset visibility is shown and custom input does not appear until after saving; smoke status remains failed/pending.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`, `DTMAPI.UnitTests`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: title homepage smoke, no save slot.
  - Log line: `Smoke staged AnimalHusbandryProgress Custom color preset for pending-preview screenshot`, followed by `Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress-custom = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-160739/DTMAPI-evidence/UI-004/20260603-160812/title-settings-config-animal-husbandry-progress.png` hides the hex input for an ordinary preset; `title-settings-config-animal-husbandry-progress-custom.png` shows `填充颜色` and `F0F0F0` after staging Custom.
- Regression cases: MANUALQA-024-B, CONFIG-008

## Diagnostic: Debug.InstantSaveAndTeleportCsv

- Status: experimental
- Public surface: `IInstantSaveDebugApi`, `ITeleportDebugApi.ExportDestinationsCsv`, `TeleportDestination.SuggestedDisplayName`, `TeleportDestination.Source`, and `TeleportCsvExportResult`.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int)`, `DolocAPI.LoadGame(int)`, whitelisted native mark/station teleport destination enumeration, and DTMAPI evidence CSV writer.
- Patch type: reflected native calls plus debug-console UI actions; no raw save-file edits and no arbitrary coordinate exposure.
- Why this point: the player-visible Y console needs a discoverable "save here" action and a durable teleport audit file for manual name screening.
- Failure behavior: save/reload limitations are logged with before/after room and distance; CSV failures return a structured result and keep the UI action experimental.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseInstantSave`, `-AutoExerciseDebugTeleport`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03.
  - Save: local slot 3 / index 2.
  - Log line: `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, limitation=none`, `Smoke exercise DebugTeleportCsv OK rows=80 path=...TELEPORT-DESTINATIONS\20260603-152541\teleport-destinations.csv`, and `Smoke exercise DebugTeleport OK ... changedRoom=True`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-152451`; process check says no `DolocTown.exe`.
- Regression cases: MANUALQA-024-D, SAVE-003, DEBUGTELEPORT-001

## Hook: Fishing.MiniGameUpdate

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure` options `AutoCompleteMiniGame`, `SkipMiniGame`, and animation-speed fields.
- Game build: 23465763 workshop
- Game method/type: `AgentStateFishingReady`, `AgentStateFishingCast`, `AgentStateFishingWait`, `AgentStateFishingPull`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, selected rod renderer animator.
- Patch type: Harmony phase hooks plus `FishingGameScrollBar.UpdateGame` inspection/update; GameBridge-owned reflected animator writes.
- Why this point: skip-minigame and auto-complete-minigame are different player choices. Skip routes from wait to pull; auto-complete should only mark the visible mini-game as success after it has existed long enough.
- Failure behavior: if the mini-game object/status cannot be read safely, the bridge leaves the mini-game alone and records pending evidence rather than faking a fish reward.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03.
  - Save: local slot 3 / index 2.
  - Log line: `Smoke.AutoFishingAnimationSpeed = experimental ... phase=Pull multiplier=3, animators=2, samples=body:1->3;fishRodRenderer:1->3`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameSkip = verified`, and `Smoke.AutoFishingMiniGameComplete = verified ... status=Success, skip=false, visibleSeconds=0.76`.
  - Screenshot/report: skip/animation smoke `docs/debug/evidence/GAME-SMOKE/20260603-154816`; skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; process checks say no `DolocTown.exe`.
- Regression cases: MANUALQA-024-C, AUTOFISH-001

## Hook: Resources.OilCoalDrop

- Status: experimental
- Public surface: OilMod content plus GameBridge resource-hit bridge; no stable public API yet.
- Game build: 23465763 workshop
- Game method/type: native `DolocTown.ToolCollider.HandleTools` prefix/postfix around resource removal, existing one-action resource-hit completion path, and native item generation/backpack placement for `dtmapi_oil`.
- Patch type: GameBridge runtime logic plus prefix capture of the pre-hit coal resource; no official/Workshop JSON mutation.
- Why this point: Oil should remain an official JSON item while DTMAPI supplies the fragile coal-drop behavior through the bridge.
- Failure behavior: if the selected resource is not recognized as coal or the item cannot be generated/placed, no extra drop is awarded and the result summary records the skipped/failed path.
- Mods/tests depending on it: `DTMAPI.OilMod`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03.
  - Save: local slot 3 / index 2.
  - Log line: `OilMod content item=dtmapi_oil fuelEnergy=1500 officialJson=item_tbitem.json`, `Smoke exercise NewContentOilItemMetadata OK ... sourceKind=DTMAPI, sourceId=Local.DTMAPI_Oil, category=material_ore, categoryListed=True, salable=True, sellingPrice=45, buyingPrice=240, fuelEnergy=1500, baseHighestFuel=pumpkin:1200, indexedIcon=icon_item_coal, title=石油`, followed by `OilMod mining drop OK source=native-tool-hit, resource=coal_mine, forced=True, roll=0.5276, oilDrop=dtmapi_oil, count=1, placement={Placed dtmapi_oil x1 through native backpack placement.}`.
  - Screenshot/report: load/API evidence `docs/debug/evidence/GAME-SMOKE/20260603-152451`; coal-drop mining evidence `docs/debug/evidence/GAME-SMOKE/20260603-184455`; final metadata + coal-drop evidence `docs/debug/evidence/GAME-SMOKE/20260603-190919` with `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, and clean exit.
- Regression cases: NEWCONTENT-024-F

## Hook: Machine.ProductionRuntimeLoop

- Status: experimental
- Public surface: `IMachineProductionApi`, `MachineDefinition`, `MachineOutputRule`, `MachineProductionState`, and `MachineRegisterResult`. `MachineDefinition` exposes experimental native-tech route hints for JSON-backed machines. `MachineProductionState` exposes experimental telemetry for item/equipment/recipe/group ids, visual scale, fuel capacity/remaining, fuel/electric cycle costs, cycle minutes/TUs, next due TUs, mode, last output/costs, output target, machine-owned storage fill/capacity, storage line capacity, and native tech-tree summary.
- Game build: 23465763 workshop
- Game method/type: DTMAPI update loop, `DolocAPI.ArchiveData`, current room/subroom equipment enumeration, placed `Equipment`/`Case` inventory, `LinearInventory.PlaceItemAt`, `DolocAPI.assets.techTrees`, and runtime `DolocConfig.Tables.TbTechNode` injection.
- Patch type: GameBridge runtime loop and reflection over room/equipment/tech-tree state; official JSON owns the mine item/equipment/recipe.
- Why this point: a stable public machine contract should not expose raw Doloc Town equipment types, while the bridge can own fragile placed-equipment discovery and output delivery.
- Failure behavior: missing archive/current room/equipment data leaves the API registered but production idle; Mine production fails rather than silently falling back to backpack when Mine-owned storage is unavailable or full. DTMAPI reports fuel/electric/cycle/storage/tech-tree state through experimental config/API telemetry rather than exposing raw native machine UI types.
- Mods/tests depending on it: `DTMAPI.MineMod`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.5 smoke logs `Native machine tech route injected ... node=dtmapi_mine, tree=industrial_techtree, parent=alloy_material, pos=6,0, rightOfParent=True, aboveCommander=True`, `Smoke exercise NewContentMineOfficialJson OK ... equipmentFunction=EquipmentFuncCase, caseStorage=16/4, recipeInputs=dtmapi_oilx10|steel_ingotx10, techPoint=30, defaultUnlock=False`, `Mine placement evidence OK ... placed=dtmapi_mine/DolocTown.Case ... rendererScale=1x1/storage=0/16/line=4`, `Machine.VisualScale = verified. dtmapi_mine visualScale=2, rendererScale=2x2, applied=True`, and `Machine dtmapi.mine produced coal x2 via electric mode; fuelCost=20, electricPowerCost=10, Stored coal x2 in machine-owned storage. filledSlots=2/16, lineCapacity=4`.
  - Screenshot/report: current 0.2.5 official tech/storage/production evidence `docs/debug/evidence/GAME-SMOKE/20260604-111533` with `NewContentMineOfficialJson=true`, `NewContentMineProduction=true`, placement screenshot `DTMAPI-evidence/NEWCONTENT-025/20260604-111618/mine-placed-dtmapi-mine.png`, clean exit, no fatal popup, and no leftover `DolocTown.exe`. Earlier 0.2.4 evidence remains `docs/debug/evidence/GAME-SMOKE/20260603-210216` and earlier telemetry runs for pre-storage behavior.
- Regression cases: NEWCONTENT-024-G, MANUALQA-025-MINE-STORAGE

## Hook: Player.EquipmentSlotsApi

- Status: experimental
- Public surface: `IEquipmentSlotsApi`, `EquipmentSlotsOptions`, `EquipmentSlotsState`, `EquipmentSlotInfo`, `EquipmentSlotEquipResult`, `EquipmentSlotsRecoveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameData.AgentEquipmentManager.ReloadParams`, `DolocTown.UI.AccessoriesBar.__Init`, and `DolocTown.UI.AccessoriesBar.OnStartShow`.
- Patch type: Harmony Postfix observation/update bridge plus read-only UI clone rendering; no raw equipment UI type exposed publicly.
- Why this point: `AgentEquipmentManager.ReloadParams` is the lowest-risk observed stats refresh point for DTMAPI-managed extra-slot attributes, while `AccessoriesBar` lifecycle hooks let DTMAPI render extra slot affordances without taking over vanilla visual equipment slots.
- Failure behavior: API state records hook installation, UI render state, stored/applied counts, and recovery messages; populated extra-slot recovery returns items through native backpack placement with overflow email enabled. If Unity screenshot capture is unavailable in-save, logs keep `uiRendered`, occupied slot, and policy evidence.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03.
  - Save: local slot 3 / index 2.
  - Log line: `Hook status: Player.EquipmentSlotsApi = experimental. Patched native equipment stat refresh and AccessoriesBar lifecycle...`, `reason=AccessoriesBar.__Init, hooks=True, rendered=3`, `EquipmentSlots equip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=grandmas_button display=奶奶的纽扣 backpack=1->0 applied=True`, `ui={rendered=True ... occupied=1 ... readOnly=true, attributeOnly=true, preserveVanillaVisualSlots=true ... screenshot=unavailable}`, followed by `EquipmentSlots unequip OK ... recovered=1` and `Smoke exercise NewContentEquipmentSlots OK ... recoverBackpack=0->1, recoveredStored=0`.
  - Screenshot/report: 0.2.5 full smoke `docs/debug/evidence/GAME-SMOKE/20260604-111533` rechecks `NewContentEquipmentSlots=true`, `uiRendered=True`, `occupied=1`, `readOnly=true`, `attributeOnly=true`, `preserveVanillaVisualSlots=true`, recovery backpack `0->1`, clean exit, no fatal popup, and no leftover `DolocTown.exe`; prior stats-refresh observation in `docs/debug/evidence/GAME-SMOKE/20260603-152451` / `docs/debug/evidence/GAME-SMOKE/20260603-155505`; populated DTMAPI slot storage/equip/recovery evidence in `docs/debug/evidence/GAME-SMOKE/20260603-170813` and `docs/debug/evidence/GAME-SMOKE/20260603-184455`; read-only player equipment strip evidence in `docs/debug/evidence/GAME-SMOKE/20260603-210216`. The strip screenshot fallback returned unavailable, so the retained proof is log/summary based.
- Regression cases: NEWCONTENT-024-H
