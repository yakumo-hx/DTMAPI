# 08 - Hats Accessories Equipment Slots

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

The original owner table below is the historical fixed-slot baseline. The
2026-08-06 amendment records the superseding 1.00/current dynamic passive-slot
owner; do not use the old `passiveItem2` unlock model for current product or API
design.

## User Semantic Target

Accessories and hats: expand equipment slots and implement extra equipment.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Yes for hats/player/tool texture categories | `006_*`, `015_*`, `018_*` | Appearance/content only |
| Base content mod | Yes for new hats/items | `015_*`, `046_*` | Does not prove runtime extra slot ownership |
| Advanced content mod | Partial for equipment placement/conditions | `047_*`, `048_*`, 0.96.06 notes | Equipment data, not player slot expansion |
| Runtime behavior mutation | Not public | No official extra-player-slot API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Player equipment slots | `AgentEquipmentManager.hatItem`, `droneItem`, `activeItem`, `passiveItem1`, `passiveItem2`, `EquipHat`, `EquipDrone`, `EquipActiveItem`, `EquipPassiveItem1`, `EquipPassiveItem2`, `AfterLoadData`, `ReloadParams`, `AddAgentEquipmentFunction`, `RemoveAgentEquipmentFunction`; `ArchiveOperationGlobal.Equip*`; `DolocAPI.Equip*`; `AccessoriesBar` | `AgentEquipmentManager.cs`; `ArchiveOperationGlobal.cs`; `DolocAPI.cs`; `UI/AccessoriesBar.cs` | `AgentEquipmentManager` and official equipment operations | Save-backed equip state, UI refresh, ability params | High raw item/UI/save coupling | Read-only slot snapshot plus experimental native equip transaction | Found for native slots |
| Extra passive slot | `UnlockDataCollection.isUnlockedAdditionalPassiveSlot`; `ArchiveOperationUniversalUnlock.IsAdditionalPassiveSlotUnlocked`, `UnlockAdditionalPassiveSlot`; `ArchiveOperationGlobal.EquipPassiveItem`; `ArchiveOperationGlobal.EquipPassiveItem2`; `AccessoriesBar.passiveItem2` | unlock/operation/UI classes | Unlock data collection | Progression flag gates second passive slot; generic `EquipPassiveItem` respects `IsAdditionalPassiveSlotUnlocked`, direct `EquipPassiveItem2` is unsafe/internal | High permanent save pollution risk | Read-only slot-state first; experimental unlock with owner/reason | Partial |
| Hats | `ItemFunctionHat`, `ItemFunctionHatBase`, `ItemFunctionHatShield`, `HatInfo`, `TbHat`, `ItemHat.Use`, `ItemHatShield.Use`, `DolocAPI.EquipHat`, `BodyController.SetHatInfo`, `MotorDriverRenderer.SetHatInfo`, `AgentHatRendererUtils.TryLoadHatRenderer`, `AgentHatRenderer.CurrentHatRenderInfo` | hat item/config/render classes; official hat docs | Hat item function, equipment manager, body/motor renderers | Hat item definition, equip, and visible render; equip refreshes body/motor render paths, but motor/save-load refresh remains high-risk | Medium/high renderer coupling | Split `HatDefinition` from runtime `TryEquipHat` | Partial |
| Equipment sockets/containers/decal slots | `EquipmentInfo.FitSlotDatas`, `ContainedSlotDatas`, `FitSlots`, `ContainedSlots`, `isDecal`, `isDecalHost`; `Equipment.FitSlots`, `ContainedSlots`; `IEquipmentHost.CreateEquipment`; `IDecalHost`; `DecalHostExtension`; terrain fill/render owners; `EquipmentFuncCase.PlaceCondition`; `ItemPlaceConditionInfo.CheckCondition`; `Case.ContentFilter` | equipment config/classes; `content-configs/equipment_tbitemplacecondition.json`; Workshop 0.96.06 notes | Equipment definition, placement hosts, and placement filters | Equipment socket/decal/container restrictions and placement ownership | Lower for definitions, high for runtime placement | Equipment slot schema DTO; experimental placement adapter | Partial |
| Extra equipment beyond native slots | `AgentEquipmentManager` native slot fields; `AccessoriesBar` native UI fields | same as above | Native manager has fixed known slots | Additional slots are not native-owned except existing passive2 unlock | High/blocking | Registry-only/DTMAPI-internal sidecar until native owner exists | Blocked for stable public API |

## API Translation Notes

- Native player slots can be represented with DTMAPI slot enum values: `Hat`, `Drone`, `Active`, `Passive1`, `Passive2`.
- Extra slots must not be presented as stable until save transaction, UI, overflow, disable cleanup, and cross-save restoration are proven.
- Hat content definitions are safer than live renderer mutation.
- `Passive2` must not be exposed as an unconditional public slot because native progression gating and direct internal bypass differ.
- Round 3 confidence: native fixed-slot DTO 95; generic passive equip unlock routing 92; direct `EquipPassiveItem2` unsafe/internal 90; extra stable equipment slots blocked 96; hat render owner found but runtime adapter experimental 84; decal/socket placement owner found but high risk 82.

## Blockers And Follow-Up

- Equip transaction must specify old-item return, inventory cost, overflow behavior, and save restoration.
- Extra-slot features risk permanent save pollution when tied to native unlock flags.
- Equipment attach/detach ownership for building/decal hosts needs separate review.

## Evidence Checked

Maps: `Items_Inventory.md`, `Recipe_Crafting.md`, `UI.md`, `Assets_Content.md`.
Classes/symbols: `AgentEquipmentManager`, `ArchiveOperationGlobal`, `AccessoriesBar`, `UnlockDataCollection`, `ArchiveOperationUniversalUnlock`, `HatInfo`, `ItemFunctionHat*`, `AgentHatRenderer*`, `EquipmentInfo`, `EquipmentFuncCase`, `ItemPlaceConditionInfo`.

## 2026-08-06 Current 1.00 Amendment

### Exact current owner

Read-only inspection of the locally installed build `24585411` confirms the
same dynamic owner introduced in retained test builds `24456188` and
`24567135`. The live `Assembly-CSharp.dll` has SHA-256
`68AEA11BD040805712673E506D1D429BC71338DD52D2659683ADC60B62715739`.
Only the named types/members and derived layout facts are recorded here; no
official binary, decompiled body or extracted asset is distributed.

| Semantic target | Current exact owner | Current behavior | Disposition |
| --- | --- | --- | --- |
| Native passive state | `AgentEquipmentManager.passiveItems[]`, `EquipPassiveItem(int,...)`, `SetPassiveSlotCount(int)` | A new save starts with one passive entry. Resizing preserves the common prefix, adds/removes native functions for native entries and reloads parameters. | Supersedes fixed `passiveItem1/passiveItem2` and the boolean second-slot model. |
| Official progression | `FunctionDefines.add_accessory_slot` | Increments `passiveItems.Length` by one through `SetPassiveSlotCount`. | Official progression owns native count; a Mod must not pre-consume or rewrite it. |
| Native UI | `AccessoriesBar.slotRoot`, `passiveItemPool`, `RenderPassiveItems(Sprite[])`, `allSelectablesArray`; `EquipmentBarUiState.RefreshPassiveViewer()` | The pool count follows `passiveItems.Length`; pool entries receive native indices and participate in official navigation. | Dynamic native UI owner found. |
| Product-owned extra slots | MoreEquipmentSlots Product-v3 sidecar and product UI | Three extra records may remain product-owned, but their UI must compose after the variable native prefix and join focus/navigation. | Stable public arbitrary-slot API remains blocked; Product UI integration is currently failed. |

The old enum-like translation `Hat/Drone/Active/Passive1/Passive2` is therefore
not a complete current model. Any future read-only native snapshot would need a
variable passive collection. This amendment does not redesign or remove the
frozen `IEquipmentSlotsApi`; its compatibility service remains intact pending a
separate approved API decision.

### Current MoreEquipmentSlots 1.0 conflict

The current product UI still probes the removed `passiveItem2` and
`passiveItem1` fields, then falls back to cloning `positiveItem`. It places
three fixed clones under `positiveItem.transform.parent`, not under the native
`slotRoot` pool, and those clones are absent from `allSelectablesArray`.

Read-only parsing of the live build's `uu` bundle confirms the exact current
layout: the AccessoriesBar root is a one-row `GridLayoutGroup` with `112`-pixel
cells and `12`-pixel horizontal spacing; hat, active item and the passive
container are its three layout children, while the arrow ignores layout. The
passive container is one grid cell containing a `HorizontalLayoutGroup` with
the same `12`-pixel spacing. Product clones are appended as later root-grid
children.

With one native passive slot, the first product clone follows the native
container. After official progression grows the native array from one to two,
the second pool child advances `112 + 12` pixels inside that one-cell container
and occupies the same position as the first product clone. Data ownership stays
separate, but UI geometry overlaps and official navigation still omits all
three product controls. This is an independent current publication blocker for
MoreEquipmentSlots 1.0 and is recorded in manual QA Review
`20260806-0001`.

The minimum safe direction is to preserve Product-v3 storage and old recovery,
render a variable native prefix followed by three product slots through a
layout-safe owner after native pool rendering, and integrate product controls
with the same focus/navigation lifecycle. Expanding the official array by three
on the player's behalf is not an accepted shortcut because it changes official
progression and native save ownership.
