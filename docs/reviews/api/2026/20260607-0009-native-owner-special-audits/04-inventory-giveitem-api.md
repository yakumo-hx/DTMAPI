# 04 - Inventory/GiveItem API

Scope: `IInventoryDebugApi.GetItems`, `GiveItem`, source grouping, and runtime/source semantic boundary.

## 1. Files read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/INDEX.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Items_Inventory.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Assets_Content.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md`

## 2. Functions read

- `IInventoryDebugApi.GetItems/GiveItem/GetStatus`, `ExperimentalGameBridge.cs:57`.
- `InventoryDebugQuery`, `InventoryDebugItem`, `InventoryGiveResult`, `ExperimentalGameBridge.cs:216`.
- `IContentQueryHelper.GetIndexedItems/GetIndexedItem`, `Helpers.cs:112`.
- `IContentItemInfo`, `Helpers.cs:131`.
- `DtmApiRuntime.GetIndexedContentItems/GetIndexedContentItem`, `DtmApiRuntime.cs:245`.
- `ContentQueryService.Rebuild`, `WorkshopContentInputUi.cs:44`.
- `ContentQueryService.RebuildOfficialContentItemIndex`, `WorkshopContentInputUi.cs:100`.
- `ContentQueryService.AddOfficialContentDirectory`, `WorkshopContentInputUi.cs:148`.
- `CountNativeBackpackItem`, `DolocTownExperimentalBridgeApi.cs:3429`.
- `TryCostNativeBackpackItem`, `DolocTownExperimentalBridgeApi.cs:3436`.
- `TryPlaceNativeBackpackItem`, `DolocTownExperimentalBridgeApi.cs:3443`.
- `GetItems`, `DolocTownExperimentalBridgeApi.cs:3465`.
- `GiveItem`, `DolocTownExperimentalBridgeApi.cs:3559`.
- `FilterInventoryBySource`, `DolocTownExperimentalBridgeApi.cs:10612`.
- `BuildInventoryDebugSourceGroups`, `DolocTownExperimentalBridgeApi.cs:10621`.
- `EnumerateInventoryDebugItems`, `DolocTownExperimentalBridgeApi.cs:10678`.
- `EnumerateRuntimeItems`, `DolocTownExperimentalBridgeApi.cs:10721`.
- `ApplyContentSource`, `DolocTownExperimentalBridgeApi.cs:10766`.
- `CreateSourceOnlyInventoryItem`, `DolocTownExperimentalBridgeApi.cs:10790`.
- `InventoryGiveFailed`, `DolocTownExperimentalBridgeApi.cs:10939`.
- `DolocTownGameBridge.TryExerciseDebugInventoryForSmoke`, `DolocTownGameBridge.cs:2228`.

## 3. Call graph

```text
GetItems(query)
  -> EnumerateInventoryDebugItems
     -> runtime.GetIndexedContentItems()
     -> EnumerateRuntimeItems()
        -> DolocConfig table TbItem.DataList
     -> ApplyContentSource for runtime-loaded indexed items
     -> CreateSourceOnlyInventoryItem for indexed but not runtime-loaded rows
  -> filter by source/search/category/mod-only

GiveItem(owner, itemId, count)
  -> DolocAPI.QueryItemProto(itemId, out proto)
  -> runtime.GetIndexedContentItem(itemId) for source enablement
  -> proto.Overlay spawnability check
  -> DolocAPI.CountItem(itemId, false)
  -> DolocAPI.CanPlaceItem(itemId, chunk)
  -> DolocAPI.TryPlaceInBackpack(itemId, chunk, false)
  -> CountItem after
```

## 4. Function body findings

- `GetItems` merges two sources: runtime `TbItem` rows and DTMAPI's read-only official/workshop/local content index (`DolocTownExperimentalBridgeApi.cs:10678`).
- Runtime rows are treated as spawnable only if `Overlay > 0`; source-only indexed rows are returned with `RuntimeLoaded=false`, `CanGive=false`, and `CannotGiveReason` of `not-runtime-loaded` or `source-disabled` (`DolocTownExperimentalBridgeApi.cs:10741`, `:10790`).
- `ApplyContentSource` marks a mod item giveable only when it is runtime-loaded, spawnable, and from an enabled source (`DolocTownExperimentalBridgeApi.cs:10781`).
- `GiveItem` first requires native `DolocAPI.QueryItemProto` to find the item in runtime config; content index alone is not enough (`DolocTownExperimentalBridgeApi.cs:3578`).
- Disabled official/workshop sources block give even when the item exists in the index (`DolocTownExperimentalBridgeApi.cs:3586`).
- Placement uses native backpack owners: `CanPlaceItem` before each chunk and `TryPlaceInBackpack` for the actual insertion (`DolocTownExperimentalBridgeApi.cs:3594`, `:3611`).
- The API chunks by `Overlay` max stack and can return partial failure reasons (`partial-inventory-full`, `partial-native-placement-failed`) (`DolocTownExperimentalBridgeApi.cs:3601`).
- The smoke helper intentionally gives one official item and, when present, a workshop runtime-loaded item (`DolocTownGameBridge.cs:2234`, `:2243`).

## 5. Native owner verdict

`OK/Watch`. `GiveItem` reaches native item query/count/placement owners for runtime-loaded enabled items. `GetItems` is mixed: runtime rows are native table-derived, but source metadata is DTMAPI's read-only content index.

Reverse/map evidence: reverse inventory maps list `DolocAPI.CanPlaceItem` (`23465763.../maps/Items_Inventory.md:187`, `:188`), `DolocAPI.CountItem` (`:201`, `:203`), and `DolocAPI.QueryItemProto` (`:265`). Both builds report matching rows. The map search did not find `TryPlaceInBackpack` as a map row, so that owner is evidenced by DTMAPI reflection code and existing smoke/update records.

## 6. Ordinary mod usability

`debug-only`. Ordinary mods should not use `IInventoryDebugApi.GiveItem` as a stable reward/drop/economy API. Source/index read APIs can inform UI or diagnostics, but not runtime item creation proof.

## 7. Concrete failure modes

- A mod can see an indexed workshop item in `GetItems`, but `GiveItem` still fails `unknown-item` or `not-runtime-loaded` when the official loader has not merged it into `TbItem`.
- A mod can bypass intended economy, quest, mail, or drop rules by injecting items straight into the backpack.
- Inventory capacity can produce partial placement, leaving a mod to reconcile `GivenCount` versus `RequestedCount`.
- Disabled official source content returns `source-disabled`; ordinary mods that ignore this can show UI/actions for unavailable content.
- Items with `Overlay <= 0` are runtime table rows but not spawnable by this path.

## 8. Minimal rebuild direction

- Keep `IInventoryDebugApi` debug-only.
- For ordinary mods, use or build dedicated APIs for rewards, mail delivery, drops, recipes, or shop unlocks, each with its native owner.
- Add docs that `IContentItemInfo` is source metadata. Runtime existence must be confirmed by native query or a purpose-built result field.
- If `TryPlaceInBackpack` remains a dependency, add a reverse-map/update-record owner note for its signature, because current map output did not list it.

## 9. Evidence gaps

- No new smoke was run in this round.
- `TryPlaceInBackpack` was not found in map files during this audit; direct evidence is DTMAPI code and older smoke records.
- The audit did not inspect every native item subtype's side effects after forced insertion.
- The API has no transaction/rollback surface for partial gives.
