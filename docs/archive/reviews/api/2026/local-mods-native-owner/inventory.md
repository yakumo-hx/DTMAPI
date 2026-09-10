# Local Mod Inventory

Status: covered by four-round review
Date: 2026-06-13

## Current `testmods`

| Mod | Source path | Group | Current conclusion |
| --- | --- | --- | --- |
| `ActionSpeedMod` | `testmods/ActionSpeedMod` | migrated gameplay | Experimental. Multiple `AgentState*`/animation owners and DTMAPI restore policy. Missing explicit GameBridge dependency follow-up. |
| `AnimalHusbandryProgressMod` | `testmods/AnimalHusbandryProgressMod` | animal UI display | Experimental display-only. Animal viewer decoration, not animal behavior owner. |
| `AutoFishingMod` | `testmods/AutoFishingMod` | migrated gameplay | Experimental. Strong fishing owner evidence, but F6, cancel, single-owner, and animation restore are DTMAPI policy. Minimum version follow-up. |
| `AutoHarvestMod` | `testmods/AutoHarvestMod` | crop automation | Experimental. Crop-container only, not tree/grass/wild forage. Missing explicit GameBridge dependency follow-up. |
| `BrokenManifestMod` | `testmods/BrokenManifestMod` | core diagnostic | Core Diagnostic. Loader error fixture, no native owner needed. |
| `ChestLocatorEnhancerMod` | `testmods/ChestLocatorEnhancerMod` | inventory transaction | Experimental. Native inventory-array extension found, but not stable global inventory system. |
| `ConfigMenuExample` | `testmods/ConfigMenuExample` | framework/config | StableCandidate. DTMAPI-owned config UI sample. |
| `CropHarvestingQaMod` | `testmods/CropHarvestingQaMod` | QA fixture | Diagnostic. Evidence fixture for crop-container API only. Missing explicit GameBridge dependency follow-up. |
| `DebugConsoleMod` | `testmods/DebugConsoleMod` | debug console | Diagnostic. UI host plus debug adapters must not become ordinary gameplay APIs. |
| `FishBreedingAssistantMod` | `testmods/FishBreedingAssistantMod` | tooltip/display | Experimental Partial. Tooltip/title owner found, fish roe lookup remains placeholder/sidecar. |
| `HelloDtmMod` | `testmods/HelloDtmMod` | framework sample | Stable for entry/log sample only. |
| `HookProbeMod` | `testmods/HookProbeMod` | smoke fixture | Diagnostic/Internal. Evidence fixture, not API stability proof. |
| `MineMod` | `testmods/MineMod` | content plus runtime loop | Experimental/Gap. Content and native slices found; production scheduler is DTMAPI sidecar. |
| `MoreEquipmentSlotsMod` | `testmods/MoreEquipmentSlotsMod` | equipment sidecar | Experimental. Attribute sidecar slots, not native equipment slot expansion. |
| `MoreSavesMod` | `testmods/MoreSavesMod` | save UI slots | Experimental. 12-slot UI adapter, not stable save format proof. |
| `OilMod` | `testmods/OilMod` | content plus drop behavior | Content Found plus runtime Experimental. Item JSON and coal-drop bridge are separate evidence. |
| `OneActionCompleteMod` | `testmods/OneActionCompleteMod` | action completion | Experimental. Narrow resources/fuel/feed slices, not a universal completion API. |
| `SecondMotorMod` | `testmods/SecondMotorMod` | motor clone | Experimental. Native motor clone/lease, not generic vehicle creation. |
| `StrongPlantingGunMod` | `testmods/StrongPlantingGunMod` | planting gun | Restricted Experimental. Fixed three-slot farming gun adapter only. |
| `ZoomMod` | `testmods/ZoomMod` | camera view | Experimental. `OrthographicOnly`; background/fog/panorama need separate owner review. |

## Legacy Own-Mod Sources

| Legacy source | Source path | Current matching API/mod | Review use |
| --- | --- | --- | --- |
| `ActionSpeedMod` | `references/doloc-town/own-mod-sources/ActionSpeedMod` | `IActionSpeedApi` / `ActionSpeedMod` | Semantic history only. |
| `AnimalHusbandryProgressMod` | `references/doloc-town/own-mod-sources/AnimalHusbandryProgressMod` | `IAnimalViewerApi` / `AnimalHusbandryProgressMod` | Display semantics only. |
| `AutoFishingMod` | `references/doloc-town/own-mod-sources/AutoFishingMod` | `IFishingAutomationApi` / `AutoFishingMod` | Semantic history only. |
| `FishBreedingAssistantMod` | `references/doloc-town/own-mod-sources/FishBreedingAssistantMod` | `IItemTooltipApi` / `FishBreedingAssistantMod` | Tooltip semantics only. |
| `OneActionCompleteMod` | `references/doloc-town/own-mod-sources/OneActionCompleteMod` | `IActionCompletionApi` / `OneActionCompleteMod` | Semantic history only. |

## Third-Party Sample Groups

| Group | Source samples | Review use | Final boundary |
| --- | --- | --- | --- |
| Encyclopedia/content visibility | `ExpandedEncyclopedia.zip` | Content index and encyclopedia demand | Demand-only, clean-room only. |
| Hold-to-harvest | `HoldToHarvest.zip` | Crop owner plus input-repeat demand | Demand-only, owner to verify. |
| Motor endurance | `Infinite Hover.7z` | Motor thrust/endurance demand | Experimental backlog, not vehicle proof. |
| Competing loader/content loader | `Genesis.Core.7z`, `Genesis.ContentLoader.7z` | Loader compatibility and content lifecycle risk | Compatibility only, not gameplay API proof. |
| Auto drone/resource automation | auto drone v1.0/v1.1 archives and screenshot | Drone/resource/inventory/story-lock demand | Strong demand, native owner partial, clean-room only. |
| Building geometry expansion | larger buildable space archive | Room geometry and build-area demand | Runtime mutation blocked; query diagnostics only. |
| QoL and trainer bundles | DolocPlus, FullTrainer | Broad QoL, diagnostic, and blocked backlog | Demand-only. No implementation or stability proof. |
