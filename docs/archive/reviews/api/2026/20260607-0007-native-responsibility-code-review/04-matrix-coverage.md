# 04 public-api-matrix coverage appendix

Date: 2026-06-07
Status: positive coverage appendix complete for the 82 matrix rows

This appendix maps every row in `docs/api/public-api-matrix.md` to a 0007 code-review block or grouped block. It is a coverage ledger, not a substitute for the method-body review in volumes 01-03.

Coverage result: 82/82 matrix rows have a 0007 review target.

## Top Risks

1. Matrix rows can give a false sense of completion if they point only to family-level review; this appendix maps each row to 0007 code-review targets so the gap is visible.
2. Several matrix rows are family rows (`ICustomAnimalApi`, `IMotorVehicleApi`, `IMachineProductionApi`, `IDebugConsoleApi`) that hide many public members and DTO fields.
3. Stable markers in the matrix do not always mean ordinary mod usability. Custom entity API families are stable at contract level but blocked at runtime-adapter level.
4. Experimental rows often have smoke evidence, but smoke evidence is not native-owner proof for ordinary mods.
5. Rows that are DTO/result exports, such as `TeleportCsvExportResult`, need different native-owner treatment from runtime mutation methods.

## Native Owner Map

| Matrix area | Native owner status | Review target |
| --- | --- | --- |
| Framework/config/logging/registry | No native owner expected; DTMAPI-owned | Vol. 03 and 05 |
| Events/input/save/workshop | Mixed DTMAPI Core and Harmony/native lifecycle hooks | Vol. 03 |
| Content/workshop | Read-only source metadata; native runtime availability checked elsewhere | Vol. 03 and 05 |
| Gameplay/debug bridges | Method-specific native owners, debug-only, or DTMAPI policy hooks | Vol. 01-02 |
| Custom entities | Stable DTMAPI registry; native runtime adapters blocked | Vol. 01 and 05 |

## Ordinary Mod Usability Table

| Matrix category | Ordinary mod usability | Notes |
| --- | --- | --- |
| DTMAPI framework helpers | 普通 mod 可用 | No native owner required. |
| Read-only content/workshop helpers | 普通 mod 可用 with caveats | Source metadata is not native runtime proof. |
| Display-only UI hooks | 普通 mod 可用 with caveats | Do not imply native data creation. |
| Debug/Y-console APIs | debug-only | Native mutations are intentionally not ordinary platform APIs. |
| Migrated gameplay APIs | 仅 DTMAPI 自家 mod 可用 unless noted | Narrow hooks and sidecar state. |
| Custom entity runtime verbs | 禁止依赖 | Runtime creation/execution adapters are blocked. |

## Matrix row mapping

| # | Matrix line | API | Matrix marker | 0007 target | Result / usability |
| --- | ---: | --- | --- | --- | --- |
| 1 | 9 | `IGameLoopEvents.GameLaunched` | stable | `03-framework-content-ui.md` / `IEventsHelper lifecycle events` | Watch; 普通 mod 可用 with event-source caveats |
| 2 | 10 | `IGameLoopEvents.UpdateTicked` | experimental | `03-framework-content-ui.md` / lifecycle events and bootstrap update bridge | Watch; 普通 mod 可用 |
| 3 | 11 | `IGameLoopEvents.OneSecondUpdateTicked` | experimental | `03-framework-content-ui.md` / lifecycle events and bootstrap update bridge | Watch; 普通 mod 可用 |
| 4 | 12 | `IGameLoopEvents.ReturnedToTitle` | experimental | `03-framework-content-ui.md` / lifecycle events | Watch; native `DolocAPI.ReturnHome` hook dependent |
| 5 | 13 | `IInputEvents.ButtonPressed` | experimental | `03-framework-content-ui.md` / bootstrap update/input bridge | Watch; DTMAPI input observation |
| 6 | 14 | `IInputEvents.ButtonReleased` | experimental | `03-framework-content-ui.md` / bootstrap update/input bridge | Watch; DTMAPI input observation |
| 7 | 15 | `IInputHelper.IsDown` | experimental | `03-framework-content-ui.md` / bootstrap update/input bridge | Watch; DTMAPI input state |
| 8 | 16 | `IInputHelper.WasPressed` | experimental | `03-framework-content-ui.md` / bootstrap update/input bridge | Watch; DTMAPI per-frame input state |
| 9 | 17 | `IInputHelper.Suppress` | experimental | `01-high-risk-runtime-bridges.md` / `IInputHelper.Suppress(string)` | Gap; 禁止依赖 |
| 10 | 18 | `ISaveEvents.SaveLoaded` | experimental | `03-framework-content-ui.md` / lifecycle events | Watch; native save-load hook dependent |
| 11 | 19 | `ISaveEvents.SaveSaving` | experimental | `03-framework-content-ui.md` / lifecycle events | Watch; native save prefix dependent |
| 12 | 20 | `ISaveEvents.SaveSaved` | experimental | `03-framework-content-ui.md` / lifecycle events | Watch; native save postfix dependent |
| 13 | 21 | `IConfigHelper.ReadConfig<T>` | stable | `03-framework-content-ui.md` / `IConfigHelper` | OK; 普通 mod 可用 |
| 14 | 22 | `IConfigHelper.WriteConfig<T>` | stable | `03-framework-content-ui.md` / `IConfigHelper` | OK; 普通 mod 可用 |
| 15 | 23 | `IConfigHelper.GetConfigPath` | stable | `03-framework-content-ui.md` / `IConfigHelper` | OK; 普通 mod 可用 |
| 16 | 24 | `IConfigHelper.RegisterMigration<T>` | experimental | `03-framework-content-ui.md` / `IConfigHelper` | OK; DTMAPI config only |
| 17 | 25 | `IDtmConfigMenuApi.SetDisplayName` | experimental | `03-framework-content-ui.md` / `IDtmConfigMenuApi` | OK/Watch; DTMAPI config UI only |
| 18 | 26 | `IConfigMenuPage.DisplayName` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 19 | 27 | `IConfigMenuPendingPreview.PreviewPendingValues` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; pending DTMAPI config preview |
| 20 | 28 | `IDtmConfigMenuApi.AddInlineBoolNumberOption` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 21 | 29 | `IDtmConfigMenuApi.AddInlineBoolBoolOption` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 22 | 30 | `IDtmConfigMenuApi.AddColorPresetOption` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 23 | 31 | `IDtmConfigMenuApi.AddTextOption(..., canEdit, isVisible)` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 24 | 32 | `IDtmConfigMenuApi.AddBoolOption(..., canEdit, isVisible)` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; DTMAPI config UI only |
| 25 | 33 | `IConfigMenuItem.IsVisible/CanEdit` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK/Watch; callback-driven UI state |
| 26 | 34 | `DtmColorPreset` | experimental | `03-framework-content-ui.md` / ConfigMenu registry | OK; DTO is DTMAPI UI metadata |
| 27 | 35 | `IDtmHelper.Translation` | experimental | `05-abstractions-reverse-coverage.md` / helper shell MatrixGaps | OK/Watch; DTMAPI translation service |
| 28 | 36 | `ITranslationHelper.Language` | experimental | `05-abstractions-reverse-coverage.md` / helper shell MatrixGaps | OK; env/culture-derived DTMAPI language |
| 29 | 37 | `ITranslationHelper.Get` | experimental | `05-abstractions-reverse-coverage.md` / helper shell MatrixGaps | OK; i18n file lookup with fallback |
| 30 | 38 | `IMonitor.Log` | stable | `05-abstractions-reverse-coverage.md` / logging shell MatrixGaps | OK; DTMAPI file/host logging |
| 31 | 39 | `IMonitor.LogOnce` | stable | `05-abstractions-reverse-coverage.md` / logging shell MatrixGaps | OK; DTMAPI once-key logging |
| 32 | 40 | `IMonitor.LogException` | stable | `05-abstractions-reverse-coverage.md` / logging shell MatrixGaps | OK; DTMAPI error logging |
| 33 | 41 | `IModRegistry.IsLoaded` | stable | `03-framework-content-ui.md` / `IModRegistry` | OK; DTMAPI registry |
| 34 | 42 | `IModRegistry.Get` | stable | `03-framework-content-ui.md` / `IModRegistry` | OK; DTMAPI registry |
| 35 | 43 | `IModRegistry.GetAll` | stable | `03-framework-content-ui.md` / `IModRegistry` | OK; DTMAPI registry |
| 36 | 44 | `IModRegistry.GetApi<T>` | stable | `03-framework-content-ui.md` / `IModRegistry` | OK; DTMAPI API registry |
| 37 | 45 | `IModRegistry.RegisterApi<T>` | stable | `03-framework-content-ui.md` / `IModRegistry` | OK; DTMAPI API registry |
| 38 | 46 | `ICustomAnimalApi` | stable | `01-high-risk-runtime-bridges.md` / custom animal blocks | Watch/Blocked; split contract/runtime |
| 39 | 47 | `ICustomMonsterApi` | stable | `01-high-risk-runtime-bridges.md` / custom monster blocks | Watch/Blocked; split contract/runtime |
| 40 | 48 | `ICustomAttackApi` | stable | `01-high-risk-runtime-bridges.md` / custom attack blocks | Blocked; runtime verbs forbidden |
| 41 | 49 | `ICustomDroneApi` | stable | `01-high-risk-runtime-bridges.md` / custom drone blocks | Blocked; runtime verbs forbidden |
| 42 | 50 | `IWorkshopHelper.GetOfficialMods` | experimental | `03-framework-content-ui.md` / `IWorkshopHelper` | OK/Watch; read-only official state |
| 43 | 51 | `IWorkshopHelper.GetDtmApiMods` | experimental | `03-framework-content-ui.md` / `IWorkshopHelper` | OK/Watch; read-only DTMAPI discovery |
| 44 | 52 | `IWorkshopHelper.IsOfficialEnablementManaged` | experimental | `03-framework-content-ui.md` / `IWorkshopHelper` | OK/Watch; no toggle authority |
| 45 | 53 | `IWorkshopEvents.ModListChanged` | experimental | `03-framework-content-ui.md` / lifecycle events | Watch; native `ModManager.ReloadMods` hook dependent |
| 46 | 54 | `IUiHelper.OpenDtmApiStatusPage` | experimental | `03-framework-content-ui.md` / `IUiHelper` | OK/Watch; DTMAPI overlay only |
| 47 | 55 | `IUiHelper.OpenModListPage` | experimental | `03-framework-content-ui.md` / `IUiHelper` | OK/Watch; DTMAPI overlay only |
| 48 | 56 | `IUiHelper.OpenConfigPage` | experimental | `03-framework-content-ui.md` / `IUiHelper` | OK/Watch; DTMAPI overlay only |
| 49 | 57 | `IUiHelper.OpenErrorPage` | experimental | `03-framework-content-ui.md` / `IUiHelper` | OK/Watch; DTMAPI overlay only |
| 50 | 58 | `IUiHelper.OpenHookStatusPage` | experimental | `03-framework-content-ui.md` / `IUiHelper` | OK/Watch; DTMAPI overlay only |
| 51 | 59 | `IUiHelper.ExportLogs` | experimental | `03-framework-content-ui.md` / `IUiHelper`, `IDiagnosticsHelper` | OK; DTMAPI report export |
| 52 | 60 | `IDiagnosticsHelper.GetErrors` | stable | `03-framework-content-ui.md` / `IDiagnosticsHelper` | OK; DTMAPI diagnostics |
| 53 | 61 | `IDiagnosticsHelper.GetHookStatuses` | stable | `03-framework-content-ui.md` / `IDiagnosticsHelper` | OK; evidence labels, not native contracts |
| 54 | 62 | `IDiagnosticsHelper.ExportLogs` | stable | `03-framework-content-ui.md` / `IDiagnosticsHelper` | OK; DTMAPI report export |
| 55 | 63 | `IDiagnosticsHelper.RecordEvidence` | experimental | `05-abstractions-reverse-coverage.md` / diagnostics shell note | OK/Watch; DTMAPI evidence only |
| 56 | 64 | `IContentQueryHelper.FindAssets` | experimental | `03-framework-content-ui.md` / `IContentQueryHelper` | OK/Watch; read-only index |
| 57 | 65 | `IContentQueryHelper.GetKnownContentTypes` | experimental | `03-framework-content-ui.md` / `IContentQueryHelper` | OK/Watch; read-only index |
| 58 | 66 | `IContentQueryHelper.TryReadTextAsset` | experimental | `03-framework-content-ui.md` / `IContentQueryHelper` | OK/Watch; read-only file access |
| 59 | 67 | `IContentQueryHelper.GetIndexedItems/GetIndexedItem` | experimental | `03-framework-content-ui.md` / `IContentQueryHelper` | OK/Watch; metadata not native availability |
| 60 | 68 | `IContentItemInfo` | experimental | `03-framework-content-ui.md` / DTO semantic notes | Watch; source metadata DTO |
| 61 | 69 | `IActionCompletionApi.Configure` | experimental | `02-gameplay-debug-bridges.md` / ActionCompletion block | Watch; DTMAPI-owned migrated mod API |
| 62 | 70 | `IActionSpeedApi.Configure/GetStatus` | experimental | `02-gameplay-debug-bridges.md` / ActionSpeed block | Watch; DTMAPI-owned migrated mod API |
| 63 | 71 | `IFishingAutomationApi.Configure/SetEnabled` | experimental | `02-gameplay-debug-bridges.md` / Fishing block | Watch; DTMAPI-owned migrated mod API |
| 64 | 72 | `IItemTooltipApi.ConfigureFishRoeProvider` | experimental | `02-gameplay-debug-bridges.md` / ItemTooltip block | Watch; display-only |
| 65 | 73 | `IAnimalViewerApi.ConfigureSpecialProduceProgress` | experimental | `02-gameplay-debug-bridges.md` / AnimalViewer block | Watch; display-only |
| 66 | 74 | `IDebugConsoleApi` | experimental | `02-gameplay-debug-bridges.md` / debug UI host coverage, `05` reverse gap note | debug-only; DTMAPI Y-console host |
| 67 | 75 | `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` | experimental | `02-gameplay-debug-bridges.md` / AdvancedDebug block | Watch/Gap; debug-only |
| 68 | 76 | `IInventoryDebugApi.GetItems/GiveItem` | experimental | `02-gameplay-debug-bridges.md` / InventoryDebug block | OK/Watch; debug-only |
| 69 | 77 | `IMailDeliveryApi.SendItemMail/GetStatus` | experimental | `02-gameplay-debug-bridges.md` / MailDelivery block | Watch; debug-only |
| 70 | 78 | `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` | experimental | `02-gameplay-debug-bridges.md` / Weather block | OK/Watch; debug-only |
| 71 | 79 | `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` | experimental | `02-gameplay-debug-bridges.md` / Teleport block | OK/Watch; debug-only |
| 72 | 80 | `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` | experimental | `05-abstractions-reverse-coverage.md` / debug DTO field notes | OK; debug evidence export |
| 73 | 81 | `IInstantSaveDebugApi.GetState/SaveHere/GetStatus` | experimental | `02-gameplay-debug-bridges.md` / InstantSave block | Watch; debug-only |
| 74 | 82 | `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus` | experimental | `02-gameplay-debug-bridges.md` / TimeDebug block | Watch; debug-only |
| 75 | 83 | `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` | experimental | `02-gameplay-debug-bridges.md` / MovementDebug block | Watch; debug-only |
| 76 | 84 | `IMotorVehicleApi` | experimental | `01-high-risk-runtime-bridges.md` / MotorVehicle blocks | Watch/Gap; original native, second motor DTMAPI-owned |
| 77 | 85 | `IMachineProductionApi` | experimental | `01-high-risk-runtime-bridges.md` / MachineProduction blocks | Gap; DTMAPI runtime loop |
| 78 | 86 | `IEquipmentSlotsApi` | experimental | `01-high-risk-runtime-bridges.md` / EquipmentSlots blocks | Gap; DTMAPI sidecar slots |
| 79 | 87 | `ISaveSlotsApi` | experimental | `01-high-risk-runtime-bridges.md` / SaveSlots block | Watch; native count owner reached |
| 80 | 88 | `ICameraZoomApi` | experimental | `01-high-risk-runtime-bridges.md` / CameraZoom block | Gap; orthographic-size only |
| 81 | 89 | `IChestLocatorEnhancerApi` | experimental | `02-gameplay-debug-bridges.md` / ChestLocator block | Watch; narrow DTMAPI mod bridge |
| 82 | 90 | `IStrongPlantingGunApi` | experimental | `02-gameplay-debug-bridges.md` / StrongPlantingGun block | Watch; narrow DTMAPI mod bridge |

## Positive coverage caveats

- Rows 27-32 and 55 are covered in the reverse-coverage appendix because their method bodies live in basic DTMAPI services rather than GameBridge-heavy blocks.
- Row 66 (`IDebugConsoleApi`) still needs a fuller host-method block if the final 0007 review is expected to describe every Y-console host method with the same detail as native mutation APIs. Current code-level conclusion is already clear: DTMAPI host/debug-only, not ordinary platform UI.
- Row 72 is a DTO/result export row; the native owner question is file/evidence export rather than Doloc runtime mutation.

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Split matrix family rows into method/DTO rows for custom entities | Stable family rows hide blocked runtime verbs. |
| P1 | Add ordinary-mod usability column to the matrix | `stable`/`experimental` does not answer whether ordinary mods should depend on the API. |
| P1 | Add native-owner/source-state column for bridge DTOs | Prevents smoke evidence from being read as native ownership. |
| P2 | Link matrix rows to 0007 anchors after review stabilizes | Makes future audits easier. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Whether matrix should stay 82 rows or expand per DTO field | Current matrix is family/method oriented; Abstractions has 1013 public property lines. | Decide docs granularity after 0007 completion audit. |
| Whether status markers should be derived from `DtmApiStatusAttribute` | Attribute metadata exists, but current matrix is manually maintained. | Consider a docs-only consistency checker in a later requested goal. |
