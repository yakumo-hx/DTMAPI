# 05 Abstractions reverse coverage and MatrixGap ledger

Date: 2026-06-07
Status: reverse-coverage ledger reviewed

This appendix scans `src/DTMAPI.Abstractions` from the opposite direction: public types/members first, `public-api-matrix` second. It records where public symbols are already covered by volumes 01-04 and where a public symbol is not listed in the matrix.

Important distinction:

- `MatrixGap` here means "public in Abstractions but not individually listed in `public-api-matrix`".
- It does not automatically mean the API is unsafe. Many MatrixGaps are stable DTMAPI framework shells, DTOs, enums, or result fields.
- When a MatrixGap DTO field implies native runtime ownership that the implementation does not provide, this appendix marks a semantic risk.

## Top Risks

1. `CustomEntities.cs` has many runtime-implying MatrixGap fields under stable API family rows, while native runtime creation remains blocked.
2. `ExperimentalGameBridge.cs` has many result/state fields whose names imply native truth but actually report DTMAPI policy, sidecar, UI, or debug telemetry.
3. Safe framework MatrixGaps (`DtmMod`, `IManifest`, `IDtmHelper`) are not dangerous, but their absence from the matrix makes reverse coverage hard to audit.
4. DTO fields such as `Enabled`, `SourceEnabled`, `CanDTMApiToggle`, `HookInstalled`, `Success`, and `Status` need source-state wording so authors know whether they describe DTMAPI, native runtime, or debug state.
5. EventArgs and config-menu MatrixGaps are DTMAPI-owned but still need explicit docs to avoid native menu/save lifecycle overclaims.

## Native Owner Map

| MatrixGap group | Native owner | DTMAPI owner | Verdict |
| --- | --- | --- | --- |
| Framework shells and metadata | None expected | Core helper/manifest/log/status services | OK MatrixGap |
| EventArgs/config/UI DTOs | Mixed DTMAPI UI/Core and selected native lifecycle hooks | EventManager / ConfigMenuRegistry / UiRuntimeService | Watch |
| Content/workshop DTOs | Official/Steam state observed only | Manifest/content scanners | Watch read-only |
| Debug DTOs | Native debug commands or save/world/runtime owners by method | GameBridge debug wrappers | debug-only |
| Migrated gameplay DTOs | Narrow native hook slices | DTMAPI policy and sidecar state | Watch/Gap |
| Custom entity DTOs/providers | Intended native runtime owners not connected | CustomEntityRegistryService | Blocked for runtime |

## Ordinary Mod Usability Table

| MatrixGap group | Ordinary mod usability | Notes |
| --- | --- | --- |
| Framework shells and metadata | 普通 mod 可用 | Add matrix/docs entries; no native owner needed. |
| Config/event/UI support DTOs | 普通 mod 可用 with caveats | DTMAPI state, not arbitrary native UI/save contract. |
| Content/workshop DTOs | 普通 mod 可用 read-only | Must pair with native availability checks for runtime actions. |
| Debug/Y-console DTOs | debug-only | They describe debug mutations and evidence output. |
| Migrated gameplay DTOs | 仅 DTMAPI 自家 mod 可用 unless display-only | Sidecar/policy state can diverge from native owner. |
| Custom entity runtime DTOs | 禁止依赖 for runtime behavior | Registry/status only until adapters exist. |

## Reverse coverage summary

| Source file | Public surface | Matrix status | 0007 coverage |
| --- | --- | --- | --- |
| `src/DTMAPI.Abstractions/ApiStatus.cs` | `DtmApiStatus`, `DtmApiStatusAttribute` | MatrixGap | This appendix; DTMAPI metadata only |
| `src/DTMAPI.Abstractions/DtmMod.cs` | `DtmMod`, `Manifest`, `Monitor`, `AttachContext`, `Entry` | MatrixGap | This appendix; DTMAPI mod base only |
| `src/DTMAPI.Abstractions/Manifest.cs` | `IManifest`, `IManifestDependency` | MatrixGap | This appendix; DTMAPI manifest model only |
| `src/DTMAPI.Abstractions/Logging.cs` | `LogLevel`, `IMonitor`, `NullMonitor` | `IMonitor` methods in matrix; enum/null monitor are MatrixGap | This appendix |
| `src/DTMAPI.Abstractions/Helpers.cs` | `IDtmHelper`, translation/config/registry/workshop/UI/diagnostics/content/input helpers and DTO interfaces | Mixed; many helper properties and DTO interfaces are MatrixGap | Vol. 03 plus this appendix |
| `src/DTMAPI.Abstractions/Events.cs` | event helper interfaces and event args | Event rows partly in matrix; many args/properties are MatrixGap | Vol. 03 plus this appendix |
| `src/DTMAPI.Abstractions/ConfigMenu.cs` | config menu API/page/item/preview and `DtmColorPreset` | Main config rows in matrix; many page/item members are MatrixGap | Vol. 03 plus this appendix |
| `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs` | migrated gameplay/debug API interfaces and DTO/result/state classes | API families in matrix; most DTO fields are MatrixGap | Vol. 01-02 plus semantic-risk tables below |
| `src/DTMAPI.Abstractions/CustomEntities.cs` | 0.4.0 custom entity APIs, providers, DTOs, enums, request/result/state classes | Four API families in matrix; most DTO/provider/enum/member fields are MatrixGap | Vol. 01 plus semantic-risk tables below |

## Code-level blocks for matrix-adjacent gaps

### `DTMAPI.Abstractions.IDtmHelper` shell

- Symbol: `DTMAPI.Abstractions.IDtmHelper`
- Matrix status: MatrixGap for `ModManifest`, `Monitor`, `Events`, `Config`, `ModRegistry`, `Workshop`, `UI`, `Diagnostics`, `Content`, `Input`, `ReadConfig<TConfig>()`, `WriteConfig<TConfig>(...)`; `Translation` is listed.
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:7`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:29`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:379`
- Function body summary: Core creates a per-mod `DtmHelper` after manifest discovery and dependency checks. The helper is a service locator for DTMAPI-owned services and wraps config read/write through the mod manifest.
- GameBridge/Harmony/reflection path: None. GameBridge only registers bridge APIs into the mod registry; the helper shell itself is Core-owned.
- Native owner: None expected.
- Native state holder: DTMAPI service instances and mod manifest.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI framework shell only.
- Evidence:
  - public-api-matrix entry: only `IDtmHelper.Translation` is listed directly.
  - update record: No direct native evidence required.
  - smoke path: DTMAPI startup/mod-load smokes indirectly exercise helper creation.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:377`
- Result: MatrixGap / OK
- Recommendation: Add `IDtmHelper` itself and service properties to matrix or developer docs as DTMAPI-owned framework API. No native owner is needed, but the matrix should not imply only `Translation` is public.

### `DTMAPI.Abstractions.ITranslationHelper`

- Symbol: `DTMAPI.Abstractions.ITranslationHelper.Language`
- Symbol: `DTMAPI.Abstractions.ITranslationHelper.Get(string key, string fallback = "")`
- Matrix status: listed
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:25`
- Implementation: `src/DTMAPI.Core/Services/TranslationService.cs:10`, `src/DTMAPI.Core/Services/TranslationService.cs:25`, `src/DTMAPI.Core/Services/TranslationService.cs:27`
- Function body summary: Detects language from `DTMAPI_LANGUAGE`/`DTMAPI_UI_LANGUAGE` or current UI culture, normalizes to `english` or `schinese`, reads `i18n/{language}.json` with English fallback, and returns key/fallback when missing.
- GameBridge/Harmony/reflection path: None.
- Native owner: None expected.
- Native state holder: DTMAPI mod i18n JSON files and process environment/culture.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI localization only.
- Evidence:
  - public-api-matrix entry: localization rows 27-29.
  - update record: config/Y-console localization updates.
  - smoke path: localized config/Y-console screenshots indirectly exercise this path.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Services/TranslationService.cs:17`, `src/DTMAPI.Core/Services/TranslationService.cs:27`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:390`
- Result: OK
- Recommendation: Keep ordinary-mod usable. No Doloc native owner is expected.

### `DTMAPI.Abstractions.IMonitor` and `NullMonitor`

- Symbol: `DTMAPI.Abstractions.IMonitor.Log(...)`
- Symbol: `DTMAPI.Abstractions.IMonitor.LogOnce(...)`
- Symbol: `DTMAPI.Abstractions.IMonitor.LogException(...)`
- Symbol: `DTMAPI.Abstractions.NullMonitor`
- Matrix status: `IMonitor` methods listed; `LogLevel` and `NullMonitor` are MatrixGap.
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:5`, `src/DTMAPI.Abstractions/Logging.cs:16`, `src/DTMAPI.Abstractions/Logging.cs:23`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:9`, `src/DTMAPI.Core/Logging/FileMonitor.cs:24`, `src/DTMAPI.Core/Logging/FileMonitor.cs:41`, `src/DTMAPI.Core/Logging/FileMonitor.cs:51`
- Function body summary: `FileMonitor` writes timestamped log lines to the DTMAPI latest log, mirrors warning/error levels to the host logger, deduplicates `LogOnce` keys per monitor, and formats exceptions. `NullMonitor` is a safe no-op fallback used before real context attachment.
- GameBridge/Harmony/reflection path: None.
- Native owner: None expected.
- Native state holder: DTMAPI log file and host logger.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI diagnostics/logging.
- Evidence:
  - public-api-matrix entry: logging rows 30-32.
  - update record: every smoke/update cites runtime logs.
  - smoke path: DTMAPI startup logs.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Logging/FileMonitor.cs:24`
- Result: OK; `NullMonitor` MatrixGap
- Recommendation: Add `LogLevel` and `NullMonitor` to reverse matrix notes if the public API docs need complete symbol coverage. They are safe DTMAPI infrastructure and do not require native owners.

### `DTMAPI.Abstractions.IManifest` and `IManifestDependency`

- Symbol: `DTMAPI.Abstractions.IManifest`
- Symbol: `DTMAPI.Abstractions.IManifestDependency`
- Matrix status: MatrixGap
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:6`, `src/DTMAPI.Abstractions/Manifest.cs:22`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs`, `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- Function body summary: Manifest reader normalizes DTMAPI manifest JSON into immutable manifest/dependency data used for discovery, dependency checks, helper creation, and mod registry state.
- GameBridge/Harmony/reflection path: None.
- Native owner: None expected; official/Workshop discovery is separate and read-only.
- Native state holder: DTMAPI manifest JSON/discovered mod model.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI metadata.
- Evidence:
  - public-api-matrix entry: No direct matrix row.
  - update record: public preview and mod loading updates rely on manifests.
  - smoke path: startup/mod discovery logs.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Manifesting/ManifestReader.cs:51`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:285`
- Result: MatrixGap / OK
- Recommendation: Add manifest interfaces to public matrix because ordinary mods receive `helper.ModManifest` and config/menu APIs accept `IManifest`. No native owner is required.

### `DTMAPI.Abstractions.DtmMod`

- Symbol: `DTMAPI.Abstractions.DtmMod.AttachContext(IManifest manifest, IMonitor monitor)`
- Symbol: `DTMAPI.Abstractions.DtmMod.Entry(IDtmHelper helper)`
- Matrix status: MatrixGap
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:3`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:377`
- Function body summary: Core instantiates a mod, attaches manifest/monitor context, creates `DtmHelper`, and calls `Entry`. The base class starts with `EmptyManifest` and `NullMonitor` so code can inspect properties before attachment without nulls.
- GameBridge/Harmony/reflection path: None.
- Native owner: None expected.
- Native state holder: DTMAPI mod object context only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI mod lifecycle shell.
- Evidence:
  - public-api-matrix entry: No direct matrix row.
  - update record: mod loading records.
  - smoke path: DTMAPI/TestMod load/startup logs.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:377`
- Result: MatrixGap / OK
- Recommendation: Add `DtmMod` and `Entry` to public matrix. It is the primary mod entry contract and safe DTMAPI-owned API.

### `DTMAPI.Abstractions.DtmApiStatusAttribute`

- Symbol: `DTMAPI.Abstractions.DtmApiStatus`
- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute`
- Matrix status: MatrixGap
- Ordinary mod usability: 普通 mod 可用 as metadata only
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:5`, `src/DTMAPI.Abstractions/ApiStatus.cs:15`
- Implementation: attribute metadata only.
- Function body summary: Provides status enum and annotation fields `Status`, `Since`, and `Notes`. It does not enforce runtime behavior.
- GameBridge/Harmony/reflection path: None.
- Native owner: None.
- Native state holder: .NET metadata.
- DTMAPI registry/status/UI-only vs native runtime: Documentation/metadata only.
- Evidence:
  - public-api-matrix entry: No direct matrix row.
  - update record: No direct evidence.
  - smoke path: No direct evidence; not runtime behavior.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Abstractions/ApiStatus.cs:15`
- Result: MatrixGap / OK
- Recommendation: Add a small matrix row or docs note so status annotations themselves are not invisible public API. They should not be treated as runtime guarantees.

### `DTMAPI.Abstractions.IDiagnosticsHelper.RecordEvidence(...)`

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.RecordEvidence(string caseId, string summary)`
- Matrix status: listed, but not covered in earlier 0007 blocks.
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:83`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77`
- Function body summary: Creates an evidence subdirectory under DTMAPI evidence path, writes `summary.txt`, and copies `DTMAPI-latest.log` if present.
- GameBridge/Harmony/reflection path: None.
- Native owner: None expected.
- Native state holder: DTMAPI evidence files.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI diagnostics/evidence only.
- Evidence:
  - public-api-matrix entry: diagnostics row 55.
  - update record: review/update evidence practices.
  - smoke path: HookProbe/evidence directories use diagnostics paths.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77`
- Result: OK
- Recommendation: Keep ordinary-mod usable. Clarify that it records evidence and logs; it does not certify native hook correctness by itself.

### `DTMAPI.Abstractions.IDebugConsoleApi`

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.IsOpen`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Bind(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.BindAdvanced(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.SetLanguage(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Open(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Close(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Toggle(...)`
- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.GetStatus(...)`
- Matrix status: `IDebugConsoleApi` listed as a family row; individual members are MatrixGap.
- Ordinary mod usability: debug-only / only DTMAPI DebugConsoleMod should depend on it
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:44`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:95`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:116`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:130`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:139`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:176`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:190`, `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:221`
- Function body summary: Binds Y-console service dependencies, stores owner/language/advanced API references, opens/closes/toggles a DTMAPI Unity Canvas host, updates DTMAPI UI context, resets search/filter state on lifecycle boundaries, and returns a bound/open status. Native input isolation is not implemented by these methods directly; it is provided by separate `DolocTownHookCallbacks` prefixes while `DebugConsoleModalOpen` is true.
- GameBridge/Harmony/reflection path: UI host is bootstrap/reflected Unity UI. Native input isolation path: `AgentControllerState.UseTool`, `UseItem`, and `EnterUICheck` prefixes in `DolocTownHookCallbacks`.
- Native owner: No native menu owner. DTMAPI owns the Canvas; native owner is only the input swallow hook for preventing gameplay actions while open.
- Native state holder: DTMAPI debug-console UI state and `DolocTownHookCallbacks.DebugConsoleModalOpen`.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI UI host plus native input-isolation hook.
- Evidence:
  - public-api-matrix entry: row 66.
  - update record: Y-console/debug records, including 0.2.7 input isolation and 0.3.1 Y-console smoke.
  - smoke path: `Smoke.DebugConsoleHotkey`, Y/Escape/Y/held-Y smoke rows.
  - hook-map entry: `UI.DebugConsoleInputIsolation`.
  - code path: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:138`
- Result: Watch
- Recommendation: Keep as debug-only/DMTAPI-owned host API. Ordinary mods should not depend on it as a general native menu framework. The concrete risk is UI-only success: opening the DTMAPI Canvas does not make Doloc Town native UI own the menu, and input safety depends on a separate native hook.

### `DTMAPI.Abstractions.ITeleportDebugApi.ExportDestinationsCsv(...)`

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.ExportDestinationsCsv(IManifest owner)`
- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult`
- Matrix status: listed as row 72.
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:81`, `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:404`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3950`
- Function body summary: Enumerates DTMAPI whitelisted teleport destinations, writes a CSV under DTMAPI evidence path with internal ids, rooms, coordinates, display names, source, mark point, group, and station flag, then records hook status.
- GameBridge/Harmony/reflection path: Uses `GetDestinations` data built from GameBridge teleport destination discovery; writes a file, does not mutate native runtime.
- Native owner: Native mark-point/transport ownership is relevant to `Teleport`; CSV export itself is DTMAPI evidence/file output.
- Native state holder: DTMAPI evidence directory.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI export only.
- Evidence:
  - public-api-matrix entry: row 72.
  - update record: Y-console teleport CSV update records.
  - smoke path: `Smoke.DebugTeleportCsv`
  - hook-map entry: debug teleport CSV status.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3950`
- Result: OK
- Recommendation: Keep debug-only because it exposes debug review data. It is safe as file export but should not be documented as native transport mutation.

## Public DTO and field semantic-risk ledger

| Public type / field group | Declaration | Matrix status | Native-owner semantic risk | 0007 result |
| --- | --- | --- | --- | --- |
| `IWorkshopModInfo.*` | `src/DTMAPI.Abstractions/Helpers.cs:60` | Type is implied by Workshop rows, fields are MatrixGap | `IsEnabledByOfficialPath`, `CanDTMApiToggle`, and `WorkshopId` can sound actionable; implementation is read-only discovery/status. | Watch; ordinary read-only |
| `IContentAssetInfo.*` | `src/DTMAPI.Abstractions/Helpers.cs:122` | MatrixGap | Asset existence is file/index metadata, not native load/runtime availability. | Watch; ordinary read-only |
| `IContentItemInfo.*` | `src/DTMAPI.Abstractions/Helpers.cs:131` | Type listed, fields MatrixGap | `Enabled` and `EnablementKnown` can be mistaken for native `TbItem` availability; debug give/mail still verify native tables. | Watch |
| Event args `SaveLoadedEventArgs`, `SaveSavingEventArgs`, `SaveSavedEventArgs` | `src/DTMAPI.Abstractions/Events.cs:86`, `:98`, `:104` | Event rows listed, arg properties MatrixGap | Nullable `SaveSlot` can be absent when hook/source is unknown; not a guaranteed native archive id in all contexts. | Watch |
| Event args `MenuOpenedEventArgs`, `MenuClosedEventArgs` | `src/DTMAPI.Abstractions/Events.cs:110`, `:116` | MatrixGap | `MenuId` is DTMAPI menu id, not native Doloc menu id. | OK/Watch |
| Event args `HookStatusChangedEventArgs` | `src/DTMAPI.Abstractions/Events.cs:134` | MatrixGap | Hook status is evidence label, not API contract. | OK/Watch |
| Config menu item/page interfaces | `src/DTMAPI.Abstractions/ConfigMenu.cs:34`, `:56` | Some members listed; many are MatrixGap | `IsVisible`, `CanEdit`, pending values, validation are DTMAPI config UI state, not native options UI. | OK/Watch |
| Inventory debug DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:216`, `:227`, `:251`, `:282` | Family row listed; fields MatrixGap | `CanGive`, `SourceEnabled`, `BeforeCount`, `AfterCount`, `GivenCount` can sound like stable economy API; implementation is debug grant and native backpack check. | debug-only |
| Mail delivery DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:295`, `:309` | Family row listed; fields MatrixGap | `Sent`, `Skipped`, `PendingMailCount`, `SourceEnabled` can imply normal mail lifecycle; implementation is debug/template-based native item mail. | debug-only |
| Weather DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:328`, `:340`, `:355` | Family row listed; fields MatrixGap | Weather fields are native archive snapshots after debug mutation; not a scheduling contract. | debug-only |
| Teleport DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:367`, `:382`, `:392`, `:404` | Family/export rows listed; fields MatrixGap | `Destination`, `Snapshot`, `AfterRequest`, coordinates can imply arbitrary teleport support; implementation only whitelisted mark points. | debug-only |
| Instant save DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:413`, `:422` | Family row listed; fields MatrixGap | `ReloadAfterSave`, `CanSave`, `FailureReason` must preserve scene-residue warning; save-only native path is not general lifecycle API. | debug-only |
| Time/movement/advanced debug DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:435` through `:553` | Family rows listed; fields MatrixGap | Fields describe debug mutations of global time, time scale, save values, tech trees, crops, creative flags, spawns, and player speed. | debug-only |
| Motor vehicle DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:564` through `:633` | Family row listed; fields MatrixGap | `IsRiding`, `IsSummoned`, `RoomId`, `SpeedMultiplier`, event args can imply native multi-vehicle support; second motor is DTMAPI clone/routing. | Watch/Gap |
| Machine DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:641` through `:738` | Family row listed; fields MatrixGap | `ProductionCycleCount`, `LastOutputItemId`, `RemainingFuel`, `NativeTech*`, `RecipeInputs` mix native table state with DTMAPI runtime loop telemetry. | Gap |
| Equipment slot DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:741`, `:910` through `:974` | Family row listed; fields MatrixGap | `SlotId`, `IsApplied`, `RuntimeUiHookInstalled`, `StoredItemCount`, `AttributeOnly` can imply native slots; implementation is sidecar UI/storage/stats. | Gap |
| Save slot DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:753` through `:781` | Family row listed; fields MatrixGap | `AppliedSlotCount` is native archive-count write, but global count/shared policy remains experimental. | Watch |
| Camera zoom DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:783` through `:830` | Family row listed; fields MatrixGap | `CurrentViewScale`/`CameraOrthographicSize` expose only camera size, not full camera/background/fog state. | Gap |
| Chest locator DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:832` through `:865` | Family row listed; fields MatrixGap | Inventory counts are extension telemetry, not a general shared-inventory API. | Watch |
| Strong planting gun DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:868` through `:907` | Family row listed; fields MatrixGap | `SlotCount` and action counts are DTMAPI policy telemetry around official farming gun hooks. | Watch |
| Action/Fishing/Speed option/state DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:977` through `:1031` | Family rows listed; fields MatrixGap | Options are DTMAPI policy knobs, not native state machine capabilities. | Watch |
| Fish roe / animal viewer display DTOs | `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1034` through `:1058` | Family rows listed; fields MatrixGap | Display text/progress options do not create native item/animal data. | Watch |

## Custom entity MatrixGap field groups

The 0.4.0 custom entity matrix rows list only the four API families. The following public types/fields are public contract surface and therefore MatrixGap unless listed by name elsewhere.

| Type group | Declaration | Semantic risk | 0007 result |
| --- | --- | --- | --- |
| Shared enums: `CustomEntityFamily`, validation/runtime/lifecycle/tick/persistence/movement/relation/hitbox/attack/drone enums | `src/DTMAPI.Abstractions/CustomEntities.cs:6` through `:110` | Enum names imply runtime lifecycle modes; current runtime can report blocked/configured status but cannot create native instances. | Watch |
| Shared handles/vectors/grid/location/asset/localized text | `src/DTMAPI.Abstractions/CustomEntities.cs:122` through `:166` | Handles and positions are DTMAPI handles/coordinates until native adapters exist. | Watch |
| Validation/capability/snapshot/result classes | `src/DTMAPI.Abstractions/CustomEntities.cs:169` through `:255` | `Succeeded`, `FailureReason`, `RuntimeStatus`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount` are DTMAPI status, not native runtime proof. | Watch/Gap |
| Save migration/tick/persistence/behavior context/result | `src/DTMAPI.Abstractions/CustomEntities.cs:258` through `:311` | `SaveState`, `RestoreRuntimeInstancesOnSaveLoad`, and `UpdatedState` imply save/runtime lifecycle that is not implemented for native entities. | Gap |
| Behavior provider interfaces | `src/DTMAPI.Abstractions/CustomEntities.cs:311`, `:386`, `:544`, `:687`, `:798` | Provider callbacks are invoked only for registry/status paths today; no native update loop calls them for spawned entities. | Gap |
| Animal definitions and policy/result/snapshot classes | `src/DTMAPI.Abstractions/CustomEntities.cs:395` through `:539` | Habitat, diet, consumption, excrement, breeding, hidden products, produce, stats, persistence, spawn/snapshot fields imply animal runtime support; spawn is blocked. | Blocked for runtime |
| Monster definitions/spawn rules/stats/target/movement/attack/loot/snapshot classes | `src/DTMAPI.Abstractions/CustomEntities.cs:554` through `:682` | Spawn tables, AI, attack slots, loot, room max counts imply native monster runtime; spawn is blocked. | Blocked for runtime |
| Attack definitions/damage/hitbox/trajectory/barrage/snapshot classes | `src/DTMAPI.Abstractions/CustomEntities.cs:695` through `:793` | Damage/hitbox/trajectory/pierce/bounce/homing imply projectile/collision runtime; spawn/execute is blocked. | Blocked for runtime |
| Drone definitions/owner/equipment/stats/energy/movement/repair/summon/request/snapshot/result classes | `src/DTMAPI.Abstractions/CustomEntities.cs:809` through `:960` | Owner binding, equipment slots, modes, attack links, energy, summon, repair imply native companion runtime; summon/equip/mode are blocked. | Blocked for runtime |

## MatrixGap action list

| Priority | Gap | Why it matters |
| --- | --- | --- |
| P0 | Add explicit public matrix rows for `DtmMod`, `IManifest`, `IManifestDependency`, and `IDtmHelper` | These are ordinary mod entry/context contracts and currently invisible in the matrix. |
| P0 | Add field-level developer docs for custom entity runtime-implying DTOs | Current stable API family rows over-cover many fields whose native runtime adapter is blocked. |
| P1 | Add DTO field notes for Machine, EquipmentSlots, MotorVehicle, CameraZoom, and AdvancedDebug | These fields are where ordinary authors are most likely to mistake DTMAPI telemetry for native state. |
| P1 | Add `IDebugConsoleApi` member rows or mark it internal-to-DTMAPI mods in docs | Public host methods are currently grouped under one matrix row. |
| P2 | Add matrix rows for diagnostics/event/config-menu supporting DTOs | Low risk, but needed for truly bidirectional public-symbol coverage. |

## Property Table Decision

This appendix accounts for public type groups and the highest-risk field families. Volume 06 records the full public-member inventory counts, including 1013 public property lines, and volume 07 explains why a literal 1013-row low-risk property table would be generated symbol inventory rather than additional manual native-owner review.

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Add MatrixGap rows for `DtmMod`, `IManifest`, `IDtmHelper`, and status metadata | Safe but public framework contracts should be visible. |
| P0 | Add custom entity field-level docs for runtime-implying fields | Prevents stable contract from overpromising blocked runtime adapters. |
| P1 | Add state-source labels to migrated bridge DTOs | Clarifies native vs DTMAPI sidecar/debug telemetry. |
| P1 | Add EventArgs/config/menu support docs | Low risk but improves future reverse coverage. |
| P2 | Consider a generated symbol inventory as a check, not a review source | Helps maintain coverage without replacing manual conclusions. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Literal per-property documentation granularity | Source scan found 1013 public property lines; 05 groups high-risk fields and 06 records coverage classes. | Decide whether a future docs artifact should list all 1013 lines individually. |
| Exact future native owners for custom entity runtime fields | Core and GameBridge show configured-blocked status; no runtime adapter call paths exist. | Reverse-map native owners before any implementation goal. |
| Whether all MatrixGaps should be added to `public-api-matrix` or separate developer docs | Matrix currently focuses on API families/methods. | Choose matrix expansion policy after review. |
