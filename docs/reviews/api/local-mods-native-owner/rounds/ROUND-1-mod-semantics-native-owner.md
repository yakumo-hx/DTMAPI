# Round 1: Mod Semantics To Native Owner Extraction

Status: complete
Date: 2026-06-13
Mode: parallel subagent extraction

Round 1 split current local mods into five read-only extraction lanes. It produced first-pass semantic targets, native owner candidates, adapter concepts, and confidence scores.

## Agent A: Action, Fishing, Crops, Planting Gun

| Mod | First verdict | R1 confidence | Main owner candidates |
| --- | --- | ---: | --- |
| `ActionSpeedMod` | Partial | 78 | `AgentStateTool`, `AgentStateInteract`, `AgentStateEat`, `UseItemContinues`, `ItemBottle` |
| `AutoFishingMod` | Partial | 90 | `BodyController.UseFishRod`, `AgentStateFishing*`, `FishingGameScrollBar` |
| `AutoHarvestMod` | Partial | 88 | `PlantBasin`, `Crop`, room equipment traversal |
| `CropHarvestingQaMod` | Found/Partial | 86 | same crop-container owner as AutoHarvest |
| `StrongPlantingGunMod` | Partial | 84 | `ItemFarmingGun`, `FarmingGunUiState`, `LinearInventory`, `PlantBasin` |

Initial gaps:

- some manifests lacked explicit GameBridge dependency;
- crop reports risked overclaiming beyond crop-container harvesting;
- no stable multi-owner arbitration contract was proven.

## Agent B: Animal, Fish Roe, Chest, Oil, Mine

| Mod | First verdict | R1 confidence | Main owner candidates |
| --- | --- | ---: | --- |
| `AnimalHusbandryProgressMod` | Partial | 84 | `AnimalFullInfoData`, `AnimalViewer`, `AnimalPanel`, animal data tables |
| `FishBreedingAssistantMod` | Partial | 78 | `ItemFishRoe`, item tooltip/title/detail paths |
| `ChestLocatorEnhancerMod` | Found/Partial | 86 | `ArchiveDataHandle.GetAvailableInventories`, `LinearInventory`, `Case`, `StorageShelf` |
| `OilMod` | Partial | 80 | item content JSON plus `ToolCollider.HandleTools` drop path |
| `MineMod` | Partial | 78 | equipment content, electric component, case inventory, sidecar scheduler |

Initial gaps:

- display-only and gameplay behavior needed separation;
- fish roe lookup was not proven;
- content JSON and runtime behavior were mixed too closely.

## Agent C: UI, Debug, Save, Equipment, Vehicle, Camera, Core

| Mod | First verdict | R1 confidence | Main owner/domain candidates |
| --- | --- | ---: | --- |
| `DebugConsoleMod` | Partial/Diagnostic | 86 | DTMAPI UI host plus debug adapters |
| `MoreSavesMod` | Partial/Experimental | 84 | save slot count and official panel UI |
| `MoreEquipmentSlotsMod` | Partial/Experimental | 78 | equipment manager, accessories UI, sidecar storage |
| `SecondMotorMod` | Partial/Experimental | 82 | native motor key/controller/state |
| `ZoomMod` | Partial/Experimental | 80 | orthographic camera size |
| `ConfigMenuExample` | Found/StableCandidate | 92 | DTMAPI config menu registry |
| `HelloDtmMod` | Found/Stable | 95 | DTMAPI entry/log/event sample |
| `HookProbeMod` | Partial/Diagnostic | 88 | diagnostic evidence fixture |
| `BrokenManifestMod` | Found/Core Diagnostic | 96 | manifest loader diagnostics |

Initial gaps:

- UI success could be mistaken for gameplay owner proof;
- save UI slots could be mistaken for stable save format;
- motor clone could be mistaken for generic vehicle creation.

## Agent D: Legacy Own-Mod Sources

| Legacy source | First confidence | Current mapping |
| --- | ---: | --- |
| `ActionSpeedMod` | 90 | `IActionSpeedApi` semantic history |
| `AutoFishingMod` | 94 | `IFishingAutomationApi` semantic history |
| `AnimalHusbandryProgressMod` | 88 | `IAnimalViewerApi` display-only history |
| `FishBreedingAssistantMod` | 86 | `IItemTooltipApi` tooltip history |
| `OneActionCompleteMod` | 93 | `IActionCompletionApi` semantic history |

Round 1 conclusion: legacy own-mod sources are useful for semantics, not current API stability.

## Agent E: Third-Party Samples

Agent E created the initial third-party review under `docs/reviews/api/third-party-mods/`. The initial report was useful, but later rounds downgraded its wording.

| Sample group | R1 semantic confidence | Initial use |
| --- | ---: | --- |
| ExpandedEncyclopedia | 82 | encyclopedia/content visibility demand |
| HoldToHarvest | 74 | crop input convenience demand |
| Infinite Hover | 88 | motor endurance demand |
| Genesis Core / ContentLoader | 80 framework / 45 gameplay | loader/content lifecycle compatibility |
| Auto drone v1.0/v1.1 | 92 to 94 | drone/resource/inventory demand |
| BuildingExpander | 93 | room geometry/build-area demand |
| DolocPlus | 96 feature inventory / 70 owner claims | broad QoL and cheat taxonomy |
| FullTrainer | 90 semantic / 35 owner stability | semantic backlog only |

Round 1 caveat:

- third-party reports were semantic/API-demand evidence only;
- later rounds replaced `likely native-owner` wording with `candidate native-owner domain to verify`.
