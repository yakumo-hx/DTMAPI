# Confidence Changes

Status: final four-round summary
Date: 2026-06-13

Scores are review confidence for semantic target to native-owner/domain mapping. They are not public API stability.

## Global Movement

| Area | Round 1 | Round 2 | Round 3 | Round 4 final |
| --- | --- | --- | --- | --- |
| Semantic extraction | Medium/high | Higher | High | High and useful |
| Native owner locating | Medium/high | Slightly higher but more cautious | Mostly mapped to owner/domain | Split by feature: strong for fishing/crops/chests/core, partial for machines/equipment/drones/third-party |
| Stable API confidence | Too optimistic in places | Lowered | Mostly Experimental/Diagnostic | Lowered further; only framework/core samples approach Stable |
| Third-party samples | Strong demand evidence | Native evidence downgraded | Demand-only | Demand-only, clean-room only |
| Debug/trainer features | High semantic detail | Stability lowered | Diagnostic/Internal | Diagnostic or backlog only |
| New/custom runtime creation | Low | Lower | Blocked/proposed | Blocked for new NPC/animal, multi-drone, new vehicle, map boundary mutation |

## Final Testmod Scores

| Mod | R1 | R2 | R3 | R4 final | Final status |
| --- | ---: | ---: | ---: | ---: | --- |
| `HelloDtmMod` | 95 | 94 | 95 | 95 | Stable for entry/log sample |
| `ConfigMenuExample` | 92 | 88 | 88 | 88 | StableCandidate |
| `BrokenManifestMod` | 96 | 96 | 96 | 96 | Core Diagnostic |
| `HookProbeMod` | 88 | 90 | 90 | 90 | Diagnostic/Internal |
| `DebugConsoleMod` | 86 | 84 | 86 | 86 | Diagnostic |
| `MoreSavesMod` | 84 | 80 | 82 | 82 | Experimental |
| `ZoomMod` | 80 | 82 | 84 | 84 | Experimental, `OrthographicOnly` |
| `ActionSpeedMod` | 78 | 76 | 76 | 74 | Partial, Experimental |
| `AutoFishingMod` | 90 | 88 | 88 | 87 | Found/Partial, Experimental |
| `OneActionCompleteMod` | 93 legacy / 84 current | 84 | 84 | 83 | Partial, Experimental |
| `AutoHarvestMod` | 88 | 82 | 82 | 80 | Found/Partial, Experimental |
| `CropHarvestingQaMod` | 86 | 78 | 78 | 77 | Diagnostic fixture |
| `StrongPlantingGunMod` | 84 | 79 | 79 | 80 | Partial, Restricted Experimental |
| `AnimalHusbandryProgressMod` | 84 current / 88 legacy | 82 | 82 | 82 | Display-only Experimental |
| `FishBreedingAssistantMod` | 78 current / 86 legacy | 68 | 68 | 66 | Partial display-only Experimental |
| `ChestLocatorEnhancerMod` | 86 | 80 | 80 | 82 | Found/Partial Experimental |
| `OilMod` | 80 | 76 | 76 | 74 | Content Found plus runtime Partial |
| `MineMod` | 78 | 70 | 70 | 70 | Partial/Gap Experimental |
| `MoreEquipmentSlotsMod` | 78 | 76 | 78 | 78 | Experimental sidecar slots |
| `SecondMotorMod` | 82 | 80 | 82 | 82 | Experimental native motor clone |

## Third-Party Demand Scores

| Sample group | Semantic demand confidence | Native-owner confidence | Final status |
| --- | ---: | ---: | --- |
| ExpandedEncyclopedia | 80 | 40 | Demand-only content/encyclopedia backlog |
| HoldToHarvest | 76 | 48 | Demand-only crop plus input-repeat backlog |
| Infinite Hover | 86 | 60 | Experimental motor backlog |
| Genesis Core / ContentLoader | 78 framework / 25 gameplay | 20 | Loader compatibility risk |
| Auto Drone v1.0/v1.1 | 92 | 45 | Strong demand, owner partial |
| BuildingExpander | 92 | 38 | Runtime mutation blocked; diagnostics query possible |
| DolocPlus | 94 | 32 | Broad QoL taxonomy, not implementation proof |
| FullTrainer | 88 | 12 | Semantic backlog only |

## Effectiveness Of Review Rounds

The review rounds were effective because they changed the output in three important ways:

- semantic extraction confidence rose because every local mod and sample group got inventoried;
- native owner confidence became more precise because display-only, content-only, sidecar, diagnostic, and runtime-owner claims were separated;
- stable API confidence went down, which is the correct result for a native-owner audit that found many shared owners and restore gaps.
