# DTMAPI Public API Matrix

Status date: 2026-05-31

The first API batch is intentionally split between stable and experimental. Stable means the contract shape should not churn without migration. Experimental means usable by test mods, but not promised stable until two real mods and game evidence verify it.

| Area | API | Status | Evidence |
| --- | --- | --- | --- |
| GameLoop | `IGameLoopEvents.GameLaunched` | stable | HookProbe logged `GameLaunched OK` |
| GameLoop | `IGameLoopEvents.UpdateTicked` | experimental | HookProbe logged `UpdateTicked OK`; bootstrap fallback pump verified |
| GameLoop | `IGameLoopEvents.OneSecondUpdateTicked` | experimental | HookProbe logged `OneSecondUpdateTicked OK` in `HOOK-PROBE/20260530-202404` |
| GameLoop | `IGameLoopEvents.ReturnedToTitle` | experimental | contract only |
| Input | `IInputEvents.ButtonPressed` | experimental | HookProbe logged migrated key registration `F6,F9,F10,F11`; title settings is the player-facing config entry |
| Input | `IInputEvents.ButtonReleased` | experimental | contract only |
| Input | `IInputHelper.IsDown` | experimental | helper implemented |
| Input | `IInputHelper.WasPressed` | experimental | helper implemented |
| Input | `IInputHelper.Suppress` | experimental | helper implemented |
| Save | `ISaveEvents.SaveLoaded` | experimental | third save loaded through official UI; `SaveLoaded OK slot=2` in `HOOK-PROBE/20260530-080759` |
| Save | `ISaveEvents.SaveSaving` | experimental | fifth save auto-save logged `SaveSaving OK slot=4` in `HOOK-PROBE/20260530-081411` |
| Save | `ISaveEvents.SaveSaved` | experimental | fifth save auto-save logged `SaveSaved OK slot=4` in `HOOK-PROBE/20260530-081411` |
| Config | `IConfigHelper.ReadConfig<T>` | stable | ConfigMenuExample reads/writes config |
| Config | `IConfigHelper.WriteConfig<T>` | stable | ConfigMenuExample writes defaults |
| Config | `IConfigHelper.GetConfigPath` | stable | unit-tested via runtime config path |
| Config | `IConfigHelper.RegisterMigration<T>` | experimental | implemented |
| Config | `IDtmConfigMenuApi.SetDisplayName` | experimental | migrated config pages use localized display names |
| Config | `IConfigMenuPage.DisplayName` | experimental | title settings config list renders page display name |
| Localization | `IDtmHelper.Translation` | experimental | five migrated mods use `helper.Translation` for Chinese/English config text |
| Localization | `ITranslationHelper.Language` | experimental | language detection supports env override and Chinese-first default |
| Localization | `ITranslationHelper.Get` | experimental | reads `i18n/schinese.json` with `i18n/english.json` fallback |
| Logging | `IMonitor.Log` | stable | DTMAPI/TestMod logs captured |
| Logging | `IMonitor.LogOnce` | stable | implemented |
| Logging | `IMonitor.LogException` | stable | implemented |
| ModRegistry | `IModRegistry.IsLoaded` | stable | runtime uses DTMAPI + test mod registry |
| ModRegistry | `IModRegistry.Get` | stable | implemented |
| ModRegistry | `IModRegistry.GetAll` | stable | UI status/mod pages use it |
| ModRegistry | `IModRegistry.GetApi<T>` | stable | ConfigMenuExample obtains menu API |
| ModRegistry | `IModRegistry.RegisterApi<T>` | stable | runtime registers ModConfigMenu API |
| Workshop | `IWorkshopHelper.GetOfficialMods` | experimental | scanner reads local/Workshop-capable manifests |
| Workshop | `IWorkshopHelper.GetDtmApiMods` | experimental | implemented |
| Workshop | `IWorkshopHelper.IsOfficialEnablementManaged` | experimental | UI respects official/Steam enablement |
| Workshop | `IWorkshopEvents.ModListChanged` | experimental | official `ModManager.ReloadMods` smoke logged `WorkshopModListChanged OK count=3` in `HOOK-PROBE/20260530-081752` |
| UI | `IUiHelper.OpenDtmApiStatusPage` | experimental | HookProbe logged `UI Status OK` in `HOOK-PROBE/20260530-080759` |
| UI | `IUiHelper.OpenModListPage` | experimental | HookProbe logged `UI Mods OK`; DTMAPI does not override official enablement in `HOOK-PROBE/20260530-150808` |
| UI | `IUiHelper.OpenConfigPage` | experimental | HookProbe logged `UI Config OK`; migrated ActionSpeed/AutoFishing/OneAction config pages save/cancel/reset in `HOOK-PROBE/20260530-202404` |
| UI | `IUiHelper.OpenErrorPage` | experimental | HookProbe logged `UI Errors OK` in `HOOK-PROBE/20260530-150808` |
| UI | `IUiHelper.OpenHookStatusPage` | experimental | HookProbe logged `UI Hooks OK` in `HOOK-PROBE/20260530-150808` |
| UI | `IUiHelper.ExportLogs` | experimental | HookProbe exported `dtmapi-report-20260530-150806.zip` |
| Diagnostics | `IDiagnosticsHelper.GetErrors` | stable | error page/report uses it |
| Diagnostics | `IDiagnosticsHelper.GetHookStatuses` | stable | hook page/report uses it |
| Diagnostics | `IDiagnosticsHelper.ExportLogs` | stable | report zip verified with DTMAPI/BepInEx/Unity logs |
| Diagnostics | `IDiagnosticsHelper.RecordEvidence` | experimental | HookProbe records save evidence when reached |
| Content query | `IContentQueryHelper.FindAssets` | experimental | scans JSON/PNG/TXT/CSV in mod roots |
| Content query | `IContentQueryHelper.GetKnownContentTypes` | experimental | implemented |
| Content query | `IContentQueryHelper.TryReadTextAsset` | experimental | implemented |
| GameBridge | `IActionCompletionApi.Configure` | experimental | OneActionComplete registers policy; resource/tool-hit path verified by `GAME-SMOKE/20260531-032318` and `GAME-SMOKE/20260531-125529`; wrong-tool negative matrix verified by `GAME-SMOKE/20260531-133212` / logs `GAME-SMOKE/20260531-133256` for `Tree`, `Ore`, `Garbage`, and `Weeds` (`tool-type-mismatch`, unchanged health, `removed=False`, `oneActionDelta=0`); fuel/feeder native consume/fill path verified by `GAME-SMOKE/20260531-140335` / logs `GAME-SMOKE/20260531-140416`; vegetation/dandelion verified as a native `VegetationRenderer.OnFell` exception path, not a `DungeonResourceRenderer` one-action resource, by `GAME-SMOKE/20260531-160900` / logs `GAME-SMOKE/20260531-160943` |
| GameBridge | `IActionSpeedApi.Configure/GetStatus` | experimental | ActionSpeed registers policy through GameBridge; tool animation speed verified by `GAME-SMOKE/20260531-044611` and logs in `GAME-SMOKE/20260531-044654` (`Smoke.ActionSpeedTool = verified ... samples=body:1->3;tool-renderer:1->3;tool-collider:1->3`); fuel/feed machine add, right-click eat/drink continuous use, `IWaterContainer` bottle fill, in-water-pit bottle fill, planting, plant-basin mature crop harvest, resin collection, and wild vegetation harvest verified by `GAME-SMOKE/20260531-154400` / logs `GAME-SMOKE/20260531-154445` with `pending=none`; AutoFillBottle automatic trigger remains experimental UI/config policy, not a verified automation claim |
| GameBridge | `IFishingAutomationApi.Configure/SetEnabled` | experimental | AutoFishing registers policy/state; F6 toggle/state and wait-phase `InstantBite` verified by `GAME-SMOKE/20260531-035217` and logs in `GAME-SMOKE/20260531-035305` (`Smoke exercise AutoFishingPhase OK ... phase=Wait`); auto cast/recast, skip-minigame, and fast animations pending |
| GameBridge | `IItemTooltipApi.ConfigureFishRoeProvider` | experimental | FishBreedingAssistant registers cached lookup provider; item title/description/detail path verified by `Smoke exercise FishRoeTooltip OK` in `HOOK-PROBE/20260530-150808` |
| GameBridge | `IAnimalViewerApi.ConfigureSpecialProduceProgress` | experimental | AnimalHusbandryProgress registers display policy; real official animal bell UI verified by `Animal viewer UI evidence OK ... 隐藏产物: 羊毛脂 99/100` and delayed screenshot in `GAME-SMOKE/20260531-022415` |

Count: 53 public API members/events/helpers in the first batch.
