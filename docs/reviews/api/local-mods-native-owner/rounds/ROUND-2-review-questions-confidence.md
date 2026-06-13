# Round 2: Review Questions And Confidence Scoring

Status: complete
Date: 2026-06-13
Mode: parallel challenge review

Round 2 challenged Round 1 overclaims, asked follow-up questions, and lowered several confidence scores.

## Reviewer 1: Gameplay And Crop Group

| Mod | R2 confidence | Main challenge |
| --- | ---: | --- |
| `ActionSpeedMod` | 76 | Missing GameBridge dependency; restore and multi-owner policy not stable. |
| `AutoFishingMod` | 88 | Strong owner, but manifest minimum version too old for 0.5.1-alpha native-stage DTOs. |
| `OneActionCompleteMod` | 84 | Narrow owner slices only, not universal action completion. |
| `AutoHarvestMod` | 82 | Crop-container only; missing GameBridge dependency. |
| `CropHarvestingQaMod` | 78 | Diagnostic fixture only; missing GameBridge dependency. |
| `StrongPlantingGunMod` | 79 | Fixed three-slot contract only. |

Key questions:

- Which manifests call GameBridge APIs without declaring the dependency?
- Does any feature have stable priority/merge arbitration?
- Which restore paths are proven across disable, title return, and multi-owner use?
- Which crop cases are still pending?

## Reviewer 2: Animal, Fish, Chest, Oil, Mine

| Mod | R2 confidence | Main challenge |
| --- | ---: | --- |
| `AnimalHusbandryProgressMod` | 82 | Display-only, not animal lifecycle. |
| `FishBreedingAssistantMod` | 68 | Placeholder lookup, tooltip owner only. |
| `ChestLocatorEnhancerMod` | 80 | Native inventory array path found, but transaction scope limited. |
| `OilMod` | 76 | Split item content from runtime drop. |
| `MineMod` | 70 | Sidecar production loop, not native scheduler. |

Key questions:

- Which results are only display or content support?
- Does FishBreeding have a real roe lookup owner?
- Does ChestLocator prove preview and consume use the same inventory set?
- Does Mine handle pass-time, unloaded rooms, power shortage, and storage overflow?

## Reviewer 3: UI And Core Group

| Mod | R2 confidence | Main challenge |
| --- | ---: | --- |
| `DebugConsoleMod` | 84 | Split DTMAPI UI host from debug adapters. |
| `MoreSavesMod` | 80 | Save slot UI is not save format stability. |
| `MoreEquipmentSlotsMod` | 76 | Sidecar attribute slots, not native slot expansion. |
| `SecondMotorMod` | 80 | Native motor clone, not generic vehicle. |
| `ZoomMod` | 82 | `OrthographicOnly`; background/fog/panorama separate. |
| `ConfigMenuExample` | 88 | StableCandidate only, pending broader QA. |
| `HelloDtmMod` | 94 | Stable only for entry/log sample. |
| `HookProbeMod` | 90 | Evidence fixture, not gameplay API. |
| `BrokenManifestMod` | 96 | Core diagnostic. |

Key questions:

- Are UI hosts being mistaken for gameplay native owners?
- Are clone adapters named too broadly?
- Which capabilities are diagnostic-only?

## Reviewer 4: Third-Party Samples

| Sample group | Semantic confidence | Native-owner confidence | Main challenge |
| --- | ---: | ---: | --- |
| ExpandedEncyclopedia | 80 | 40 | Content/UI demand only. |
| HoldToHarvest | 76 | 48 | Crop and input-repeat owners must be split. |
| Infinite Hover | 86 | 60 | Motor modifier, not stable vehicle API. |
| Genesis | 78 framework / 25 gameplay | 20 | Loader compatibility, not gameplay proof. |
| Auto Drone | 92 | 45 | Battery, resource, tool gate, inventory, story lock must be split. |
| BuildingExpander | 92 | 38 | Runtime geometry mutation blocked. |
| DolocPlus | 94 | 32 | Taxonomy only; many items diagnostic or blocked. |
| FullTrainer | 88 | 12 | Semantic backlog only. |

Required wording changes:

- put license warning at the top;
- replace `likely native-owner domain` with `candidate native-owner domain to verify`;
- keep all third-party results demand-only/clean-room only.

## Reviewer 5: Cross-Cutting Review

Round 2 cross-cutting conclusion:

- semantic extraction confidence went up;
- native owner locating went up slightly;
- stable API confidence went down;
- shared owners and sidecar loops were the main risk.

New backlog domains:

- content index/encyclopedia;
- input repeat/native input isolation;
- crafting/recipe transaction;
- machine lifecycle;
- player status;
- monster/combat/spawn/drop;
- camera background/panorama/fog;
- save/archive UI slots;
- loader compatibility.
