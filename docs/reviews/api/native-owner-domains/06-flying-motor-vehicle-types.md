# 06 - Flying Motor Vehicle Types

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Flying motor behavior and animation, creating a new independent flying motor, and creating a wholly new vehicle type with different behavior.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Yes for existing vehicle textures/anchors | `022_*` vehicle docs; `017_*` anchor docs | Existing flying motor appearance |
| Base content mod | Not enough for vehicle runtime | Official item/content docs | No vehicle registry proof |
| Advanced content mod | Not confirmed | Official docs checked | No multi-vehicle type pipeline |
| Runtime behavior mutation | Not public | No official vehicle behavior API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Flying motor behavior | `MotorController.__Init`, `Control`, `AutoFlyTo`, `FlyToTargetCoroutine`, `SetIsRiding`, `UpdateVelocityX`, `UpdateVelocityY`, `CostEndurance`, `RecoveryEndurance`, `OnFixedUpdate` | `MotorController.cs`; `maps/Motor.md` | `MotorController` | Physics, endurance, riding/following, summon flight, collision behavior | High singleton/room coupling | Experimental motor state/query/summon adapter | Found for original motor |
| Motor persistence/location | `MotorDataManager.UnlockMotor`, `UpdateMotorRoom`, `AfterLoadData`; `DolocAPI.Motor` | `GameData/MotorDataManager.cs`; `DolocAPI.cs` | `MotorDataManager` plus `DolocAPI.Motor` singleton | Unlock and location persistence for the original motor | High global state risk | Diagnostic/experimental original-motor snapshot only | Partial |
| Motor key/use/gates | `ItemMotorKey.OnUse`; `DolocAPI.SetMotorPosition`; `MotorController.AutoFlyTo`; `ManagerGate.TryEnterOnMotor`, `TryQuitOnMotor` | `ItemMotorKey.cs`; `DolocAPI.cs`; `MotorController.cs`; `ManagerGate.cs`; `Action_Interaction.md` | Native item and gate interactions | Summons/repositions the native motor singleton and applies gate transition policies | High transition risk | Diagnostic/experimental original-motor interaction adapter | Partial |
| Ride lifecycle | `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor`, `AgentControllerState.GetOffMotor`, `AgentControllerState.OnUpdateRiding`, `ManagerGate.TryEnterOnMotor`, `ManagerGate.TryQuitOnMotor` | motor interactable, agent state, and gate classes | Original motor, agent riding state, camera/body/drone/scanner/gate owners | Native riding lifecycle for the original motor | High transition/render/input risk | Read-only ride state plus diagnostic lifecycle probe | Found for original motor |
| Animation/visuals | `MotorRenderer`, `MotorDriverRenderer`, `MotorLight`; `GlobalParameterInfo.DefaultVehiclePreview` | renderer classes; `GlobalParameterInfo.cs`; official vehicle docs | Renderers and global config | Vehicle visuals, driver animation, light, preview | Medium/high renderer coupling | Vehicle appearance/skin pack first | Partial |
| Wholly new vehicle type | `DolocAPI.Motor`, `ItemFunctionMotorKey`, `ItemMotorKey`, `MotorController`, `MotorDataManager`; no `TbVehicle`, `vehicle_tb`, `VehicleInfo`, prefab registry, save slot, or key-to-vehicle mapping found | `Motor*.cs`; `ItemMotorKey.cs`; official docs | Existing singleton path only | No multi-vehicle registry or save slot found | Blocking | Proposed/blocked until native redesign | Blocked |

## API Translation Notes

- Experimental-facing work should begin with original motor state query and appearance content, not arbitrary vehicle classes.
- Any second-motor/multi-vehicle feature is DTMAPI-owned sidecar behavior around a native singleton and must remain experimental/internal until lifecycle is proven.
- Public APIs must not expose `MotorController`, renderers, or `DolocAPI.Motor`.
- Round 3 confidence: no native vehicle registry/table 94; original motor singleton owner 93; `ItemMotorKey.OnUse` summons existing motor 90; ride lifecycle owner 84; existing motor skin replacement 90; independent second motor/new vehicle sidecar only 92; wholly new vehicle type blocked 95.

## Blockers And Follow-Up

- No multi-vehicle table, prefab registry, save slot, or key-to-vehicle mapping was found.
- Runtime motor APIs need third-save ride, transition, gate, returned-to-title, clean-exit, and no-Steam-wait evidence.
- New vehicle type requires a separate GameBridge redesign, not an appearance-pack API.

## Evidence Checked

Maps: `Motor.md`, `Action_Interaction.md`, `Items_Inventory.md`, `UI.md`, `Assets_Content.md`.
Classes/symbols: `MotorController`, `MotorDataManager`, `ItemMotorKey`, `ManagerGate`, `MotorRenderer`, `MotorDriverRenderer`, `MotorLight`, `GlobalParameterInfo`.
