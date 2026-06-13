# Semantic API Map

Status: docs-only semantic map
Date: 2026-06-13

This table maps the SMAPI ecosystem semantics from the user-provided summary to DTMAPI candidate API concepts. The status column is a research recommendation, not a public API promotion.

| Semantic capability | SMAPI signal | DTMAPI candidate | Owner layer | Final status |
| --- | --- | --- | --- | --- |
| Mod entry, manifest, helper | all standard SMAPI mods | `DtmMod`, `IManifest`, `IDtmHelper` | Core-owned | `Stable` / `StableCandidate` |
| Game launched integration point | GMCM, Content Patcher, Automate | `IGameLoopEvents.GameLaunched` | Core-owned | `StableCandidate` |
| Update pump | Automate, Data Layers, CJB | `UpdateTicked`, `OneSecondUpdateTicked` | Core pump | `Experimental` |
| Save/title lifecycle | Automate, CJB, per-save data mods | `SaveLoaded`, `Saving`, `Saved`, `ReturnedToTitle` | Core plus save boundary bridge | `Experimental` |
| Config read/write | GMCM, Automate, UI Info Suite | `ReadConfig<T>`, `WriteConfig<T>` | Core-owned | `Stable` |
| Config menu registration | GMCM ecosystem | `IDtmConfigMenuApi.Register`, `AddBoolOption`, `AddKeybind` | Core protocol plus DTMAPI UI host | `StableCandidate` for registration only |
| Data files | Automate, Lookup Anything, UI Info Suite | `IDataApi`, `ModData`, `SaveData`, `TempSessionData` | Core-owned and save-bound variants | `Proposed` |
| Translation and locale | Content Patcher, GMCM, Data Layers | `ITranslationHelper.Get`, `LocaleChanged` | Core-owned | `StableCandidate` / `Proposed` split |
| Console commands | Automate, Data Layers, Content Patcher, CJB | `IConsoleCommandApi.Register` | Core plus Diagnostic split | `Proposed` / `Diagnostic` |
| Mod registry and API exchange | Automate, Data Layers, GMCM | `IModRegistry.IsLoaded`, `GetApi<T>`, `RegisterApi<T>` | Core-owned registry | `StableCandidate` |
| Local mod messages | Automate and Chests Anywhere integration | `ILocalModMessageApi.Publish`, `Subscribe` | Core-owned | `Proposed` |
| Multiplayer messages | SMAPI multiplayer usage | `IMultiplayerApi` | Future reserved | `Future-reserved` |
| Input and keybinds | Chests Anywhere, Data Layers, GMCM | `IInputApi`, `IKeybindApi` | Core event facade plus UI state | `Experimental` |
| Input suppression | Data Layers, UI Info Suite | `SuppressActiveKeybinds`, `InputHandled` | Core helper plus UI host | `Experimental`; native isolation `Blocked` until owner proof |
| HUD drawing | UI Info Suite, Chests Anywhere | `IHudApi`, `RenderedHud`, `DrawIcon` | UI host | `Proposed` / `Experimental` |
| Menu events and overlays | Chests Anywhere, GMCM, CJB | `IMenuEvents`, `IMenuApi.AttachOverlay` | UI host plus native menu adapters | `Proposed` / `Experimental` |
| Tooltip and info panels | Lookup Anything, UI Info Suite | `ITooltipApi`, `IInfoPanelApi` | UI host plus info query | `Experimental` |
| Visual debug/data layers | Data Layers | `IDebugLayerApi`, `IWorldOverlayApi` | UI host plus Diagnostic | `Diagnostic` / `Experimental` |
| Target inspection | Lookup Anything | `ITargetInspectionApi` | UI host plus GameBridge adapters | `Proposed` / `Experimental` |
| Info query | Lookup Anything, UI Info Suite | `IInfoQueryApi` | GameBridge read-only DTO | `Experimental read-only` |
| Content query | Content Patcher, content ecosystem | `IContentQueryHelper`, `IContentSourceIndex` | Core content index | `Experimental read-only` |
| Content packs | Content Patcher | `IContentPackApi`, `OwnedContentPacks` | Core content pipeline | `Proposed` |
| Asset patch pipeline | Content Patcher | `IContentEvents.AssetRequested`, `IAssetData.Edit` | Content pipeline plus GameBridge adapters | `Proposed` |
| Token and condition framework | Content Patcher | `ITokenRegistry`, `IConditionContext` | Core content framework plus optional GameBridge query | `Proposed / P2` |
| World scan | Automate, NPC Map Locations, Tractor | `IWorldScanApi` | GameBridge read-only query | `Experimental` |
| Room/location events | Automate, Tractor, NPC Map Locations | `IRoomEvents`, `ILocationEvents` | Core facade plus GameBridge lifecycle | `Experimental` |
| World object events | Automate | `IWorldObjectEvents` | GameBridge native-owner needed | `Proposed` / `Experimental` |
| Map markers | NPC Map Locations | `IMapMarkerApi`, `IMapOverlayApi` | UI host plus GameBridge read-only DTO | `Experimental` |
| Character location query | NPC Map Locations, Lookup Anything | `ICharacterLocationApi`, `IPlayerLocationApi` | GameBridge read-only | `Experimental` |
| Container query | Chests Anywhere, Automate | `IContainerApi.GetContainers` | GameBridge read-only query | `Experimental` |
| Container metadata | Chests Anywhere | `IContainerMetadataApi` | DTMAPI sidecar plus GameBridge identity | `Experimental` |
| Container UI open | Chests Anywhere | `IContainerUiApi.OpenContainer` | UI host plus GameBridge | `Deep Experimental` / `Blocked` |
| Inventory snapshot and transaction | Chests Anywhere, Automate | `IInventorySnapshotApi`, `IInventoryTransactionApi` | GameBridge query and mutation | query `Experimental`, transaction high-risk `Experimental` |
| Machine query and automation | Automate, Mine-like demand | `IMachineQueryApi`, `IMachineAutomationApi` | GameBridge plus DTMAPI scheduler | query `Experimental`, automation later `Proposed` |
| Building query/content | Tractor Mod | `IBuildingQueryApi`, `IBuildingContentApi` | GameBridge read-only plus content pipeline | `Proposed` / `Experimental` |
| World entity registry/runtime | Tractor Mod semantics | `IWorldEntityApi` | Core registry plus GameBridge runtime | registry `Proposed`, runtime `Blocked` |
| Motor vehicle adapter | Tractor-like vehicle semantics and Doloc motor domain | `IMotorVehicleApi` | GameBridge native motor adapter | `Experimental` |
| Custom vehicle registry | Tractor-like new vehicle semantics | `IVehicleRegistryApi` | GameBridge runtime | `Blocked` |
| Debug and cheat tools | CJB Cheats Menu | `IDebugToolsApi` | Diagnostic plus GameBridge adapters | `Diagnostic-only` |
