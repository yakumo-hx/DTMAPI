# Implementation Follow-ups

Status: docs-only follow-up list
Date: 2026-06-13

These are not implemented in this review. They are future task seeds.

## P0 Documentation And Boundary Follow-ups

| Follow-up | Reason |
| --- | --- |
| Keep this library linked from API rebuild docs. | Future API work should check local mod demand before redesigning a boundary. |
| Keep third-party samples marked clean-room demand only. | No license or implementation permission was established. |
| Use `candidate native-owner domain to verify` for third-party reports. | Third-party visible metadata is not owner proof. |
| Keep `Found` qualifiers explicit. | Display-only and content-only found states are easy to overread. |

## P1 Testmod Manifest And Version Follow-ups

| Follow-up | Reason |
| --- | --- |
| `ActionSpeedMod` should declare explicit `DTMAPI.GameBridge.DolocTown` required dependency. | It calls GameBridge API directly. |
| `AutoHarvestMod` should declare explicit `DTMAPI.GameBridge.DolocTown` required dependency. | It calls crop GameBridge API directly. |
| `CropHarvestingQaMod` should declare explicit `DTMAPI.GameBridge.DolocTown` required dependency or document diagnostic degraded behavior. | It is a QA fixture using crop GameBridge API. |
| `AutoFishingMod` should align `MinimumDTMApiVersion` and GameBridge dependency with the 0.5.1-alpha native-stage DTO surface. | Current `0.2.5` wording is too old for the reviewed DTO semantics. |

## P1 Naming And API Boundary Follow-ups

| API or mod | Required wording |
| --- | --- |
| `MoreEquipmentSlotsMod` | Attribute sidecar slots, not native equipment slot expansion. |
| `SecondMotorMod` | Native motor clone/lease, not generic vehicle creation. |
| `ZoomMod` | `OrthographicOnly`; background, fog, panorama, and room render sync are separate owners. |
| `MoreSavesMod` | Save UI slot adapter, not stable save file format. |
| `OilMod` | Split official item content from runtime coal-drop behavior. |
| `MineMod` | Split official content/recipe/tech from DTMAPI production scheduler. |
| `FishBreedingAssistantMod` | Tooltip/title display only until real roe/fish breeding lookup is found. |

## P1 Native-Owner Deep Dives

| Deep dive | Reason |
| --- | --- |
| Auto drone resource collection | Strong demand; must split battery, resource owner, tool gate, inventory, and story lock. |
| Building and room geometry diagnostics | Strong demand; runtime mutation is blocked, but read-only geometry snapshots may be useful. |
| Content index and encyclopedia | ExpandedEncyclopedia and DolocPlus/FullTrainer demand a safe content-query surface. |
| Crafting and recipe transaction | DolocPlus/FullTrainer demand max craft and all-chest material use. |
| Machine production lifecycle | `MineMod` needs native owner proof for due time, storage, power, unloaded rooms, and save/load. |
| Camera background/panorama/fog | `ZoomMod` only owns orthographic size today. |
| Input repeat/native input isolation | HoldToHarvest and action automation need safe input ownership. |

## P2 Backlog Domains

- player status query/mutation;
- monster/combat/spawn/drop;
- NPC read-only database versus new NPC creation;
- animal runtime creation versus existing animal display;
- drone multi-instance feasibility;
- custom vehicle type feasibility;
- map generation and runtime boundary mutation;
- hand-held ranged weapon animation/projectile/damage/attachments.

## Validation Needed Before Runtime Work

Any runtime follow-up must require:

- method-body review for the exact native owner;
- GameBridge adapter boundary;
- DTO-only public surface;
- save/load, return-to-title, transition, disable/restore, and multi-mod tests;
- third local save slot game smoke when runtime state is touched;
- clean exit with no leftover `DolocTown.exe`;
- update records, public API matrix changes, and debug/hook evidence when applicable.
