# 05 - Drones Runtime Equipment

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Create new drones, replace drone textures, define movement modes, modify drone accessories/installable slot counts, and support simultaneous multiple drones.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Not confirmed for drones | Official beauty docs checked | No dedicated drone texture doc found |
| Base content mod | Partial for item functions/content | `046_*`; extracted `item_tbitem.json` | Drone table authoring not documented |
| Advanced content mod | Partial resource constraints mention drone plugin level | `032_*` resource docs | Not a drone authoring workflow |
| Runtime behavior mutation | Not public | No official active-drone API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Drone content/item definition | `ItemFunctionBase.DeserializeItemFunctionBase`, `ItemFactory.GenerateItem`, `ItemDroneStructure`, `DroneStruct`, `DolocConfig.Tables.TbDroneStructure/TbDroneWeapon/TbDroneChip/TbDroneEngine/TbDroneAssist/TbDroneSkill/TbDroneSlot`, `ModManager.LoadWithMods`, `ModInfo.LoadConfigs` | `Config/Item/ItemFunctionBase.cs`; `ItemFactory.cs`; `ItemDroneStructure.cs`; `DroneStruct.cs`; `Config/Tables.cs`; mod config loader paths | Item/config pipeline into `DroneStruct` | Native item construction appears config/table driven; active runtime equip/summon is separate and unproven for custom drones | Medium/high raw config and table risk | `DroneDefinition` and component DTOs, registry-only until loader and equip proof | Partial |
| Textures/visuals | `DroneStructureInfo.Sprite`, `DroneStructureInfo.Animator`, `DroneWeaponInfo.Sprite/SpritePivot`, `DroneEngineInfo.Sprite/SpritePivot`, `DroneAssistInfo.Sprite/SpritePivot`, `IDroneComponentItem.ComponentSprite/ComponentPivot`, `ItemDroneStructure.ComposedDroneSprite`, `DroneRenderer.RenderComponent`, `ClearComponents`, `PlayAnimation` | `Config/Drone/*.cs`; `IDroneComponentItem.cs`; `DroneRenderer.cs` | Config-backed visuals plus live renderer | Sprite/pivot composition and runtime render | Renderer mutation high | Visual DTOs and preview/composition API | Partial |
| Movement/combat control | `DroneController.HandleUserInput`, `AutoBattle`, `UpdateDroneDirection`, `SetSelected`, `SetDroneFollower`, `OnUpdate`, `OnFixedUpdate`; `DroneRenderer.SetFollowTarget`, `SetMoveSpeed`, `Dash`, `FollowTarget`; `DroneWeapon.SwitchAutoFire` | `DroneController.cs`; `DroneRenderer.cs`; `DroneWeapon.cs`; `Items_Inventory.md`; `UI.md` | `DroneController` and live current drone | Selected/manual/auto combat, follow target, input through existing native modes | Medium/high active state coupling | Existing-mode query/command only; custom movement mode blocked | Partial |
| Accessories/install slots | `DroneStruct.slots`, `CanEquip`, `GetEquipIndexes`, `Equip`, `TryTakeOff`, `TakeOff`, `InitializeParams`; `DroneSlot`, `DroneSlotProto`, `ComponentType`, `SlotVisualSuitableType`; `ItemDroneWeapon/Engine/Chip/Assist`; `DronePanelUiState`, `DroneWidget`, `DroneItemSlot` | `DroneStruct.cs`; `DroneSlot.cs`; `Config/Drone/*.cs`; `ItemDrone*.cs`; drone panel UI classes | `DroneStruct`, `DroneSlot`, and native drone panel transaction path | Slot validation, locks, component occupancy, UI/inventory transaction reconciliation | High save-backed item transaction risk | Transactional slot API with native error mapping after save/load proof | Found for single drone |
| Active runtime singleton | `AgentEquipmentManager.droneItem`, `HasDrone`, `EquipDrone`, `IsEquippedDrone`; `DroneController.CurrentDrone`; `DolocAPI.droneRenderer`, `RunDrone`, `UnloadDrone`, `CurrentDrone` | `AgentEquipmentManager.cs`; `DroneController.cs`; `DolocAPI.cs` | Single equipped drone item and single runtime renderer/controller | Active runtime equip/summon path is single-native-drone oriented | High/blocking | Active equipped drone only | Found / single active only |
| Simultaneous multiple drones | `AgentEquipmentManager.droneItem`, `DroneController.CurrentDrone`, `DolocAPI.droneRenderer`, `DolocAPI.RunDrone`, `DolocAPI.UnloadDrone` | `AgentEquipmentManager.cs`; `DroneController.cs`; `DolocAPI.cs` | Native singleton active-drone path | No native multi-active owner found | High/blocking | Multi-drone proposed only as DTMAPI-owned companion/sidecar system | Blocked |

## API Translation Notes

- Split definition/index support from runtime summon/equip support.
- The stable shape should model one native active equipped drone unless a future GameBridge rebuild proves multi-instance ownership.
- Component install/remove needs native transaction results, inventory reconciliation, UI refresh, save/load proof, and locked-slot semantics.
- Round 3 confidence: drone config table merge likely but undocumented 65; item definition can likely produce `ItemDroneStructure` 78; active runtime singleton 96; multiple active native drones blocked 94; slot/component transaction path 80; custom visuals smoke-pending 68; custom movement modes not proven 55.

## Blockers And Follow-Up

- No native owner for multiple simultaneous active drones was found.
- Official docs do not expose drone component table authoring.
- New drone visuals need asset address/naming proof before public authoring docs.
- Content smoke required before custom drone authoring: custom drone table plus item table loads, item instantiates, equip/load round-trip works, and no second active native drone is created.

## Evidence Checked

Maps: `Motor.md`, `Action_Interaction.md`, `Items_Inventory.md`, `UI.md`, `Assets_Content.md`.
Classes/symbols: `DolocAPI`, `DroneController`, `DroneStruct`, `DroneSlot`, `DroneRenderer`, `DroneWeapon`, `ItemDrone*`, `AgentEquipmentManager`, drone config tables.
