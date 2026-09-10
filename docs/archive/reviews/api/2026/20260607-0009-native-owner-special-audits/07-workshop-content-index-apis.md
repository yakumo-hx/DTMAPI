# 07 - Workshop/Content Index APIs

Scope: `IWorkshopHelper`, `IWorkshopEvents.ModListChanged`, `IContentQueryHelper`, `IContentItemInfo`, and official content source semantics.

## 1. Files read

- `src/DTMAPI.Abstractions/Events.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `references/doloc-town/research-notes/README-DolocTown-Workshop-Functional-Mods.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/ModManager_Workshop.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/ModManager_Workshop.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Assets_Content.md`

## 2. Functions read

- `IWorkshopEvents.ModListChanged`, `Events.cs:47`.
- `IWorkshopHelper.GetOfficialMods/GetDtmApiMods/IsOfficialEnablementManaged/GetEnablementHint`, `Helpers.cs:51`.
- `IWorkshopModInfo` fields, `Helpers.cs:60`.
- `IContentQueryHelper.FindAssets/GetKnownContentTypes/TryReadTextAsset/GetIndexedItems/GetIndexedItem`, `Helpers.cs:112`.
- `IContentItemInfo` fields, `Helpers.cs:131`.
- `DtmApiRuntime.NotifyWorkshopModListChanged`, `DtmApiRuntime.cs:195`.
- `DtmApiRuntime.GetIndexedContentItems/GetIndexedContentItem`, `DtmApiRuntime.cs:245`.
- `DtmApiRuntime.DiscoverMods`, `DtmApiRuntime.cs:249`.
- `WorkshopService.SetMods/GetOfficialMods/GetDtmApiMods/IsOfficialEnablementManaged/GetEnablementHint`, `WorkshopContentInputUi.cs:13`.
- `ContentQueryService.Rebuild/FindAssets/TryReadTextAsset/GetIndexedItems/GetIndexedItem`, `WorkshopContentInputUi.cs:44`.
- `RebuildOfficialContentItemIndex`, `WorkshopContentInputUi.cs:100`.
- `AddOfficialContentRoot/AddOfficialContentDirectory`, `WorkshopContentInputUi.cs:131`.
- `ContentItemInfo` DTO implementation, `WorkshopContentInputUi.cs:300`.
- `WorkshopModInfo`, `RegistryAndHelpers.cs:73`.
- `ModScanner.Discover/AddWorkshopRoot/AddFromRoot/AddSingleDirectory`, `ManifestReader.cs:51`.
- `OfficialModEnablementIndex.Load/TryGetEnabled`, `ManifestReader.cs:209`.
- `DolocTownHookCallbacks.ReloadModsPostfix`, `DolocTownHookCallbacks.cs:47`.
- `DolocTownGameBridge.InstallHarmonyHooks`, workshop slice, `DolocTownGameBridge.cs:877`.
- `DolocTownGameBridge.TryReloadOfficialModsAndConfig`, `DolocTownGameBridge.cs:3737`.

## 3. Call graph

```text
Startup / official reload
  -> DtmApiRuntime.DiscoverMods
     -> ModScanner.Discover
        -> BepInEx/DTMAPI local mods path
        -> official persistent MODS path
        -> Steam workshop content/2285550 roots
        -> OfficialModEnablementIndex.Load(SAVE/mod_infos.json)
     -> WorkshopService.SetMods
     -> ContentQueryService.Rebuild
        -> scan DTMAPI assets
        -> RebuildOfficialContentItemIndex
           -> official local MODS and workshop roots
           -> Content/**/item_tbitem.json
           -> ContentItemInfo source metadata

Official reload hook
  -> Harmony postfix ModManager.ReloadMods
  -> DtmApiRuntime.NotifyWorkshopModListChanged
  -> DiscoverMods + LoadMods(initialLoad:false)
  -> Workshop.ModListChanged event

Smoke/manual reload helper
  -> DolocAPI.modManager.ReloadMods()
  -> DolocConfig.Reload()
```

## 4. Function body findings

- `IWorkshopHelper` is read-only. `WorkshopService` stores discovered mod metadata and answers source/enablement hints; it has no toggle method (`WorkshopContentInputUi.cs:13`).
- Official-path mods and Steam Workshop mods are marked `CanDtmApiToggle=false`; `IsOfficialEnablementManaged` is true when DTMAPI cannot toggle or source is Workshop (`WorkshopContentInputUi.cs:25`).
- `ModScanner` reads official local mods from the persistent `MODS` root and workshop mods from Steam workshop content roots, then applies `SAVE/mod_infos.json` enablement state (`ManifestReader.cs:56`, `:75`, `:79`, `:121`).
- If a managed official-path mod has no official enablement record, discovery marks it disabled and gives a user-facing reason (`ManifestReader.cs:130`).
- `ContentQueryService` scans files and builds an index from `Content/**/item_tbitem.json`; it sets `Enabled`, `EnablementKnown`, `SourceKind`, `WorkshopId`, and `LoadOrder`, but does not mutate native tables (`WorkshopContentInputUi.cs:148`, `:210`).
- Content DTO fields are source metadata. `Enabled=true` means official enablement index says enabled; it is not proof that `DolocConfig.Tables.TbItem` currently contains the item.
- The GameBridge reload helper can call official `ModManager.ReloadMods` and `DolocConfig.Reload` for smoke/automation, but public helper APIs do not expose this as an ordinary-mod mutation API (`DolocTownGameBridge.cs:3737`).
- `ModListChanged` is triggered after official `ModManager.ReloadMods` postfix refreshes DTMAPI discovery (`DolocTownHookCallbacks.cs:47`, `DtmApiRuntime.cs:195`).

## 5. Native owner verdict

`OK/Watch`. The APIs correctly expose DTMAPI discovery/index state and official enablement hints as read-only metadata. Native/official owners for actual enablement, load order, Steam subscription, JSON/PNG merge, and runtime table loading remain `DolocTown.Config.ModManager`, `DataPersistenceManager` mod manager save/load, and `DolocConfig.Reload`.

Reverse/research evidence: workshop research notes state official local mods live under persistent `MODS`, Steam subscribed mods are loaded by official `ModManager`, enablement persists to `SAVE/mod_infos.json`, and official loading supports JSON config merge/PNG sprite replacement but not managed-code plugin lifecycle (`README-DolocTown-Workshop-Functional-Mods.md:9`, `:16`, `:18`, `:24`, `:28`). Reverse maps list `DolocTown.Config.ModManager` (`23465763.../maps/ModManager_Workshop.md:32`), `ModManager.ReloadMods` (`:72`), `DataPersistenceManager.SaveModManager/LoadModManager` (`:90`, `:91`), `ModManager.ReloadMods` Harmony candidate (`23249387.../maps/ModManager_Workshop.md:293`), and `DolocConfig.Reload` candidate (`23249387.../maps/Assets_Content.md:168`).

## 6. Ordinary mod usability

`普通 mod 可用` as read-only metadata and diagnostics. It is `禁止依赖` as an enable/disable/load-order/runtime-table mutation API.

## 7. Concrete failure modes

- A mod can see an item in `IContentQueryHelper.GetIndexedItem` but the native runtime table may not contain it if official reload/config merge has not happened.
- A mod can see `Enabled=false` or `EnablementKnown=false` and still attempt runtime behavior, causing missing item/table failures.
- A mod can misread `CanDTMApiToggle=false` as a bug; it is deliberate because official/Steam paths are owned by the official UI and `mod_infos.json`.
- A mod can assume Workshop content can contain managed code; official Workshop loading is data-driven JSON/PNG, not `Assembly.Load` or DTMAPI lifecycle.
- A mod can respond to `ModListChanged` before it has rechecked its own required source ids and runtime tables, leading to stale UI/actions.

## 8. Minimal rebuild direction

- Keep helper APIs read-only and document that source metadata is not runtime table proof.
- Add explicit helper docs that ordinary mods must check enablement and, for items, native runtime availability through a purpose-specific API.
- If runtime reload is ever public, split it into request, official reload result, `DolocConfig.Reload` result, content index rebuild, and mod rebind event.
- Do not expose official enable/disable mutation until `mod_infos.json`, official UI state, Steam source, and restart/hotload semantics are all owned.

## 9. Evidence gaps

- No new smoke was run in this round.
- This audit did not inspect official `ModManager.ReloadMods` method bodies; it used DTMAPI code, reverse maps, and workshop research notes.
- The content index currently only scans item table JSON for indexed item metadata; other official content formats are outside this helper's source-item contract.
- There is no ordinary-mod proof that `ModListChanged` is safe for managed hotload beyond DTMAPI's own reload path.
