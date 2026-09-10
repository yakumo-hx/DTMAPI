# 03 framework, content, UI, and lifecycle APIs

Date: 2026-06-07
Status: manual code review slice complete for this volume

This volume reviews framework-level helpers and lifecycle surfaces. Most are DTMAPI-owned by design and do not need a Doloc Town native owner. The review focus is therefore whether the public name or DTO implies more than DTMAPI actually controls.

## Top Risks

1. `IWorkshopHelper` and `IWorkshopModInfo` expose official/Workshop state but do not own enable/disable/order. Ordinary mods must treat them as read-only diagnostics.
2. `IContentQueryHelper` indexes official local, Workshop, and DTMAPI content files, but runtime availability still depends on official enablement and native `DolocConfig` tables. Indexed metadata is not proof that native runtime loaded an item.
3. Save/workshop lifecycle events depend on Harmony/native hooks being installed and may be pending before GameBridge patches resolve. Event names are stable, but the source credibility differs by event.
4. `IUiHelper.OpenCustomMenu` is DTMAPI overlay/menu state only; it does not integrate with or reserve Doloc Town native menu state unless a specific host does so.
5. `IDtmConfigMenuApi` is a DTMAPI title/config UI registry, not the official Doloc Town options menu. Ordinary mods can use it, but docs must avoid native options-menu wording.
6. Diagnostics hook statuses are DTMAPI evidence/status records. A `verified` hook status is evidence of a path, not a formal native API contract.
7. Public DTO/event-args properties in this area are mostly safe, but `IsEnabledByOfficialPath`, `CanDTMApiToggle`, `EnablementKnown`, and `SourceEnabled` can be misread as control flags rather than observed state.

## Native Owner Map

| API group | Native owner reached | DTMAPI owner | Verdict |
| --- | --- | --- | --- |
| Config helper | None expected | `ConfigService` JSON files under DTMAPI paths | Stable DTMAPI-owned |
| Mod registry | None expected | `ModRegistryService` loaded/API dictionaries | Stable DTMAPI-owned |
| Workshop helper | Official enablement observed from discovery/index files; no toggle path | `WorkshopService` discovered-mod list | Read-only safe |
| Content query helper | File scan and JSON index; runtime availability checked elsewhere through native tables | `ContentQueryService` index | Read-only metadata, watch wording |
| Events helper | BepInEx Update, Core timer, Harmony save/workshop/title hooks | `EventManager` event slots | Stable/watch by event |
| UI helper | DTMAPI overlay/title UI host | `UiRuntimeService` and bootstrap reflected UI | DTMAPI-owned UI only |
| Diagnostics helper | File log/export and Core hook-status records | `DiagnosticsService` | Stable DTMAPI-owned |
| Config menu API | DTMAPI config page/item registry and title menu host | `ConfigMenuRegistry`, `ReflectedTitleMenuSettingsUi` | DTMAPI-owned UI registry |
| Bootstrap input/update | BepInEx `Update`, reflected Unity input polling | `DtmApiRuntime` dispatch | Stable/watch, not native input suppression |

## Ordinary Mod Usability Table

| API group | Usability | Why |
| --- | --- | --- |
| Config helper | 普通 mod 可用 | DTMAPI-owned per-mod JSON config surface. |
| Mod registry | 普通 mod 可用 | DTMAPI-owned loaded-mod/API lookup. |
| Workshop helper | 普通 mod 可用 for read-only status | It intentionally does not toggle official/Steam state. |
| Content query helper | 普通 mod 可用 for read-only discovery | It indexes files/source metadata, not native runtime ownership. |
| Events helper | 普通 mod 可用 with lifecycle caveats | Game/update events are DTMAPI-owned; save/workshop events rely on native hooks. |
| UI helper | 普通 mod 可用 for DTMAPI overlay | It controls DTMAPI UI, not arbitrary native menus. |
| Diagnostics helper | 普通 mod 可用 | DTMAPI-owned logging/status/evidence surface. |
| Config menu API | 普通 mod 可用 | DTMAPI config menu registry; not native options UI. |

## Review Blocks

### `DTMAPI.Abstractions.IConfigHelper`

- Symbol: `DTMAPI.Abstractions.IConfigHelper.ReadConfig<TConfig>(IManifest manifest)`
- Symbol: `DTMAPI.Abstractions.IConfigHelper.WriteConfig<TConfig>(IManifest manifest, TConfig config)`
- Symbol: `DTMAPI.Abstractions.IConfigHelper.GetConfigPath(IManifest manifest)`
- Symbol: `DTMAPI.Abstractions.IConfigHelper.RegisterMigration<TConfig>(IManifest manifest, Action<TConfig> migrate)`
- Current marker: stable
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:32`
- Implementation: `src/DTMAPI.Core/Services/ConfigService.cs:20`, `src/DTMAPI.Core/Services/ConfigService.cs:40`, `src/DTMAPI.Core/Services/ConfigService.cs:48`, `src/DTMAPI.Core/Services/ConfigService.cs:54`
- Function body summary: Reads/writes JSON config under DTMAPI mod config paths, returns config path, and stores an optional migration callback. No Doloc Town native owner is expected.
- GameBridge/Harmony/reflection path: None.
- Native owner: None; this is DTMAPI-owned infrastructure.
- Native state holder: DTMAPI config files.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI runtime/config only.
- Evidence:
  - public-api-matrix entry: framework/config helper rows.
  - update record: config menu and framework records across updates; no native-specific update needed.
  - smoke path: ConfigMenu/ActionSpeed config smoke indirectly exercises writes, but No direct evidence for every helper method in this review.
  - hook-map entry: No direct native hook expected.
  - code path: `src/DTMAPI.Core/Services/ConfigService.cs:20`
- Result: OK
- Recommendation: Keep stable. Ordinary mods can use it safely because it does not claim native ownership.

### `DTMAPI.Abstractions.IModRegistry`

- Symbol: `DTMAPI.Abstractions.IModRegistry.IsLoaded(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IModRegistry.Get(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IModRegistry.GetAll()`
- Symbol: `DTMAPI.Abstractions.IModRegistry.GetApi<TApi>(string uniqueId)`
- Current marker: stable
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:41`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:9`
- Function body summary: Uses DTMAPI dictionaries to track loaded manifests and registered runtime API instances. It does not query official mod manager state after initial discovery except through DTMAPI's loaded set.
- GameBridge/Harmony/reflection path: Runtime APIs are registered by Core and GameBridge via `DtmApiRuntime.RegisterRuntimeApi<TApi>`.
- Native owner: None expected. It is a DTMAPI mod/API registry.
- Native state holder: DTMAPI loaded/API dictionaries.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI registry.
- Evidence:
  - public-api-matrix entry: framework/mod registry rows.
  - update record: No direct evidence needed for native owner.
  - smoke path: DTMAPI startup/mod-load smokes indirectly cover loaded mod discovery.
  - hook-map entry: No native hook expected.
  - code path: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:9`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:212`
- Result: OK
- Recommendation: Keep stable. Document that this reports DTMAPI-loaded mods/APIs, not arbitrary official Workshop load order control.

### `DTMAPI.Abstractions.IWorkshopHelper` and `IWorkshopModInfo`

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetOfficialMods()`
- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetDtmApiMods()`
- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.IsOfficialEnablementManaged(IWorkshopModInfo mod)`
- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetEnablementHint(IWorkshopModInfo mod)`
- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.IsEnabledByOfficialPath`
- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.CanDTMApiToggle`
- Current marker: stable/watch
- Review advice: 保持 read-only; keep wording strict
- Ordinary mod usability: 普通 mod 可用 for read-only status
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:51`, `src/DTMAPI.Abstractions/Helpers.cs:60`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13`, `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:73`, `src/DTMAPI.Core/Manifesting/ManifestReader.cs:51`
- Function body summary: `WorkshopService` stores discovered mod info and returns snapshots. `IsOfficialEnablementManaged` reports true when DTMAPI cannot toggle or source is Workshop. `GetEnablementHint` returns explanatory text. No enable/disable/order writes exist.
- GameBridge/Harmony/reflection path: `ModManager.ReloadMods` postfix calls `NotifyWorkshopModListChanged`, which re-dispatches DTMAPI diagnostics/events after official reload.
- Native owner: Official Doloc Town/Steam Workshop mod manager owns enablement/order. DTMAPI observes discovery state only.
- Native state holder: Official mod files/Workshop paths and enablement index; DTMAPI discovered-mod list.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI read-only status.
- Evidence:
  - public-api-matrix entry: workshop helper/mod-info rows.
  - update record: Workshop/Y-console and public preview records.
  - smoke path: Workshop reload/status UI smoke where present; No direct evidence for toggling because toggling is intentionally absent.
  - hook-map entry: `Workshop.ReloadMods`
  - code path: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:23`, `src/DTMAPI.Core/Manifesting/ManifestReader.cs:91`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:879`
- Result: OK/Watch
- Recommendation: Keep ordinary-mod usable as read-only. DTO fields must be documented as observed state, not controls. Ordinary mods that treat `CanDTMApiToggle` or `IsEnabledByOfficialPath` as authority to mutate official enablement will fail because no native toggle owner is connected.

### `DTMAPI.Abstractions.IContentQueryHelper`

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.FindAssets(string contentType)`
- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetKnownContentTypes()`
- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.TryReadTextAsset(string relativePath, out string text)`
- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItems()`
- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItem(string itemId)`
- Current marker: stable/watch
- Review advice: 保持 read-only with runtime-availability warning
- Ordinary mod usability: 普通 mod 可用 for read-only discovery
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:112`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34`, `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:69`, `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:79`, `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:91`
- Function body summary: Rebuilds a DTMAPI content-source index by scanning DTMAPI mods, official local content, and Workshop directories; returns asset metadata, known content types, text assets, and item source info. It does not insert data into native `DolocConfig` tables.
- GameBridge/Harmony/reflection path: Debug inventory/mail APIs cross-check this index against native item table/query methods when granting/delivering items.
- Native owner: None for index itself. Runtime item availability is owned by native `DolocConfig.Tables.TbItem` and official enablement/loading.
- Native state holder: DTMAPI content index, source metadata, and file paths.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI source metadata only.
- Evidence:
  - public-api-matrix entry: content query rows.
  - update record: content/Oil/Mine/Y-console source filtering records.
  - smoke path: debug inventory/content-source smokes indirectly cover item source filtering.
  - hook-map entry: No direct native hook expected for read-only indexing.
  - code path: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:49`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3559`
- Result: OK/Watch
- Recommendation: Keep ordinary-mod usable but document that an indexed item is not proof the native runtime loaded or accepts it. Ordinary mods that skip native availability checks risk UI/source success but `QueryItemProto`/spawn/mail/give failure.

### `DTMAPI.Abstractions.IEventsHelper` lifecycle events

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.GameLaunched`
- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.UpdateTicked`
- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.OneSecondUpdateTicked`
- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.ReturnedToTitle`
- Symbol: `DTMAPI.Abstractions.IInputEvents.ButtonPressed`
- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveLoaded`
- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveSaving`
- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveSaved`
- Symbol: `DTMAPI.Abstractions.IWorkshopEvents.ModListChanged`
- Current marker: stable/watch
- Review advice: 保持, with event-source caveats
- Ordinary mod usability: 普通 mod 可用 with lifecycle caveats
- Declaration: `src/DTMAPI.Abstractions/Events.cs:6`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:46`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:65`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:104`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:152`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:167`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:173`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:179`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:195`
- Function body summary: `EventManager` stores owned handlers and dispatches events with exception capture. Core runtime dispatches game/update/input/save/title/workshop events. Save/title/workshop dispatch is triggered by GameBridge hooks into native lifecycle methods.
- GameBridge/Harmony/reflection path: BepInEx `Update` for update events; Harmony patches `DolocAPI.AfterLoadArchiveData`, `LoadGame`, `SaveGame`, `ReturnHome`, and `ModManager.ReloadMods`.
- Native owner: Game loop is bootstrap-owned; save/title/workshop events depend on native lifecycle hooks.
- Native state holder: Event args carry DTMAPI snapshots such as save slot/tick/button; native save/title state remains game-owned.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI event dispatch around native lifecycle where patched.
- Evidence:
  - public-api-matrix entry: events rows.
  - update record: many smoke/update records rely on save/title lifecycle.
  - smoke path: startup/save/title/workshop smoke rows where present.
  - hook-map entry: `Save.SaveLoaded`, `Save.SaveSaving`, `Save.SaveSaved`, `GameLoop.ReturnedToTitle`, `Workshop.ReloadMods`
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:830`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:853`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:871`
- Result: Watch
- Recommendation: Keep stable for mod author ergonomics, but docs should distinguish DTMAPI/Core events from native-hook-dependent events. Ordinary mods can use them, but save/title/workshop logic must tolerate hooks being pending on unsupported builds and avoid assuming event order beyond documented evidence.

### `DTMAPI.Abstractions.IUiHelper`

- Symbol: `DTMAPI.Abstractions.IUiHelper.Toggle()`
- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenDtmApiStatusPage()`
- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenModListPage()`
- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenConfigPage(string? uniqueId = null)`
- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenCustomMenu(string menuId)`
- Symbol: `DTMAPI.Abstractions.IUiHelper.SetUiContext(string inputContext, bool canDrawOverlay, bool gameplayHotkeysAllowed, string reason)`
- Current marker: stable/watch
- Review advice: 保持 DTMAPI UI wording
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:72`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:422`, `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`, `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- Function body summary: Opens/closes/toggles DTMAPI overlay pages and records UI context flags used by bootstrap and DTMAPI UI. `OpenCustomMenu` sets a DTMAPI custom menu id and dispatches DTMAPI menu events.
- GameBridge/Harmony/reflection path: Bootstrap/reflected Unity UI host; debug-console input isolation has separate native hooks.
- Native owner: DTMAPI overlay/title menu host. No generic native menu owner.
- Native state holder: DTMAPI UI state, overlay page, menu id, context flags.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI UI only.
- Evidence:
  - public-api-matrix entry: UI helper rows.
  - update record: title/config/Y-console UI records.
  - smoke path: title button/Y-console UI smokes.
  - hook-map entry: UI/debug console input isolation is separate; UI helper itself is DTMAPI-owned.
  - code path: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:447`, `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:477`, `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:486`
- Result: OK/Watch
- Recommendation: Keep ordinary-mod usable for DTMAPI UI. Document that it does not open or reserve arbitrary native Doloc Town menus. Ordinary mods relying on it for native menu lifecycle will only affect DTMAPI overlay state.

### `DTMAPI.Abstractions.IDiagnosticsHelper`

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetErrors()`
- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetHookStatuses()`
- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetLatestLogPath()`
- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.ExportLogs()`
- Current marker: stable
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:83`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25`, `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:31`, `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:37`, `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:59`
- Function body summary: Returns DTMAPI recorded errors/hook statuses/latest log path and exports logs into an evidence/report path. Hook status mutation is internal Core/GameBridge behavior; public helper is read/export.
- GameBridge/Harmony/reflection path: GameBridge calls `runtime.SetHookStatus`; helper only reads statuses.
- Native owner: None expected. It is a DTMAPI diagnostics layer.
- Native state holder: DTMAPI diagnostics lists and log files.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI evidence/status.
- Evidence:
  - public-api-matrix entry: diagnostics rows.
  - update record: every non-trivial update cites diagnostics evidence.
  - smoke path: log export/debug report rows where present.
  - hook-map entry: hook statuses reference native paths but are DTMAPI evidence records.
  - code path: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25`
- Result: OK
- Recommendation: Keep stable. Add doc wording that hook statuses are evidence labels, not a guarantee that a public native API contract exists.

### `DTMAPI.ModConfigMenu.IDtmConfigMenuApi` / `ConfigMenuRegistry`

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.Register(...)`
- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddBoolOption(...)`
- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddNumberOption(...)`
- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddTextOption(...)`
- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddChoiceOption(...)`
- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddButton(...)`
- Symbol: `DTMAPI.ModConfigMenu.ConfigMenuRegistry.Save(string uniqueId)`
- Current marker: stable/watch
- Review advice: 保持 as DTMAPI config UI
- Ordinary mod usability: 普通 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs` and `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:13`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:20`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:24`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:25`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:27`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:30`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:35`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:175`
- Function body summary: Registers per-mod config pages and option items, tracks pending/committed values, supports save/reset/cancel/preview, and exposes pages to DTMAPI title/config UI. It invokes provided mod callbacks rather than writing native settings.
- GameBridge/Harmony/reflection path: Bootstrap title menu UI renders DTMAPI config pages; no native options menu owner.
- Native owner: DTMAPI title/config UI host only.
- Native state holder: DTMAPI config menu registry and per-mod config callbacks/files.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI UI/config only.
- Evidence:
  - public-api-matrix entry: config menu API rows.
  - update record: config menu UI and ActionSpeed config apply records.
  - smoke path: `Smoke.ActionSpeedConfigApply`, title/config UI smoke rows.
  - hook-map entry: No native options hook expected.
  - code path: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`, `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:68`
- Result: OK/Watch
- Recommendation: Keep ordinary-mod usable. Developer docs should say this is DTMAPI's config menu, not the official Doloc Town options UI. Ordinary mods relying on it for native options persistence will only get DTMAPI callback/config behavior.

### Bootstrap update/input bridge

- Symbol: `DTMAPI.Core.Runtime.DtmApiRuntime.Start()`
- Symbol: `DTMAPI.Core.Runtime.DtmApiRuntime.Update()`
- Symbol: `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputPressed(string button)`
- Symbol: `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputReleased(string button)`
- Current marker: framework runtime
- Review advice: 保持 internal framework; document input caveats through `IInputHelper`
- Ordinary mod usability: ordinary mods consume events/helpers, not Core runtime directly
- Declaration: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:65`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:104`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:119`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:133`
- Implementation: same as declaration
- Function body summary: Starts mod loading and dispatches `GameLaunched`; update increments ticks, dispatches update/one-second events, clears pressed inputs, and calls GameBridge update. Input methods dispatch button events and guard gameplay hotkeys based on DTMAPI UI context.
- GameBridge/Harmony/reflection path: BepInEx bootstrap calls runtime update and reflected Unity input polling; GameBridge publishes hook statuses for update events.
- Native owner: BepInEx/Unity update loop for timing; reflected Unity input for key state. No native gameplay action suppression here.
- Native state holder: DTMAPI tick/input state and UI context.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI event/input state around Unity polling.
- Evidence:
  - public-api-matrix entry: helper/events/input rows.
  - update record: startup/debug/input records.
  - smoke path: startup/log/hotkey smokes.
  - hook-map entry: `GameLoop.UpdateTicked`, `GameLoop.OneSecondUpdateTicked`, debug console input isolation separate.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:214`, `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303`
- Result: Watch
- Recommendation: Keep framework behavior. For ordinary mods, document that `ButtonPressed`/`WasPressed` are DTMAPI input observations; they do not block native gameplay actions unless a separate native suppression hook exists.

## DTO / EventArgs semantic notes

| Symbol / field group | Risk | Recommendation |
| --- | --- | --- |
| `IWorkshopModInfo.IsEnabledByOfficialPath`, `CanDTMApiToggle`, `WorkshopId` | Can be read as control authority. | Say observed/source metadata only; DTMAPI does not toggle Steam/official state. |
| `IContentItemInfo.Enabled`, `EnablementKnown`, `SourceEnabled`-style fields | Can be read as native runtime availability. | Say content index status must be paired with native table/query checks before runtime actions. |
| `HookStatusInfo.Status`, `Source`, `Details` | Can be read as proof of stable API. | Say it is review/smoke evidence, not a compatibility guarantee. |
| Save event args `SaveSlot` | Can be read as always present. | Keep nullable and document pending/unknown slot during unsupported or pre-hook states. |
| UI event/menu ids | Can be read as native menu ids. | Prefix docs with DTMAPI overlay/menu context. |

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Add doc warning for `IInputHelper` observations vs native suppression | Prevents ordinary mods from assuming input can be swallowed. |
| P1 | Add framework docs section: "DTMAPI-owned vs Doloc-owned" | Helps explain why Config/UI/Diagnostics are stable without native owners. |
| P1 | Add content-source docs with native availability examples | Prevents indexed-but-not-loaded item mistakes. |
| P1 | Add event source table to developer docs | Clarifies which events are Core-only and which depend on Harmony/native hooks. |
| P2 | Add ConfigMenu docs note that pages are DTMAPI UI, not official options | Avoids native options-menu overpromise. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Full reverse coverage of every framework DTO/property against `public-api-matrix` | This volume reviewed manually important framework symbols; 0006 has broad MatrixGap navigation. | Add a 0007 appendix mapping all Abstractions public properties to either this volume or a MatrixGap block. |
| Whether Workshop reload event should re-run content/native table availability checks | Read `ModManager.ReloadMods` postfix and content index rebuild path; not all official reload side effects reviewed here. | Inspect reload lifecycle in reverse build before adding stable hot-reload docs. |
| Exact guarantees around config-menu save timing during gameplay | ConfigMenu registry and ActionSpeed config smoke exist, but all mod callback side effects are mod-defined. | Document callback semantics and avoid promising atomic runtime reconfiguration. |
