# 08 - Hats Accessories Equipment Slots

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

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
