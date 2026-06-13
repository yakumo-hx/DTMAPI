# Local Mod Native Owner Review Library

Status: active, docs-only
Created: 2026-06-13
Scope: current local `testmods`, legacy local own-mod sources, and local third-party sample groups

This directory records the four-round review of current local mods as API demand evidence:

```text
local mod behavior
  -> semantic target
  -> native responsibility function / state holder candidate
  -> GameBridge or Core adapter boundary
  -> public API status recommendation
```

It is not an API promotion record. A mod working in local smoke evidence does not make its API stable. The public API truth still lives in `docs/api/public-api-matrix.md`, and broad domain native-owner discovery still lives in `docs/reviews/api/native-owner-domains/`.

Related ecosystem API research: [SMAPI Ecosystem Semantic API Map](../smapi-ecosystem-map/INDEX.md). Use it when a local mod demand points at broader ecosystem surfaces such as events, content pipelines, UI overlays, config/data helpers, cross-mod APIs, or diagnostic tooling.

## Rules

- Do not copy decompiled Doloc Town source into DTMAPI.
- Do not copy third-party DLLs, Cheat Engine scripts, installers, or implementation logic into DTMAPI.
- Treat third-party samples as demand and compatibility evidence only.
- Split `Found` into `native-runtime found`, `display-only found`, `content-only found`, or `partial`.
- Mark public APIs as `Stable`, `StableCandidate`, `Experimental`, `Restricted Experimental`, `Diagnostic`, `Demand-only`, or `Blocked` based on bridge and evidence risk.
- Use DTOs and adapters for future API concepts. Do not expose raw decompiled game types.

## Index

- [Source Index](SOURCE-INDEX.md)
- [Report Template](REPORT-TEMPLATE.md)
- [Inventory](inventory.md)
- [API Demand Clusters](api-demand-clusters.md)
- [Shared Native Owner Conflicts](shared-native-owner-conflicts.md)
- [Confidence Changes](confidence-changes.md)
- [Implementation Follow-ups](implementation-follow-ups.md)

## Round Reports

| Round | Purpose | Report |
| --- | --- | --- |
| Round 1 | Parallel extraction from local mods into semantics, native-owner candidates, risks, and first scores. | [Round 1 Mod Semantics Native Owner](rounds/ROUND-1-mod-semantics-native-owner.md) |
| Round 2 | Parallel challenge review, overclaim questions, and confidence scoring. | [Round 2 Review Questions Confidence](rounds/ROUND-2-review-questions-confidence.md) |
| Round 3 | Parallel replies, supplements, revised reports, and proposed structure. | [Round 3 Author Supplemental Structure](rounds/ROUND-3-author-supplemental-structure.md) |
| Round 4 | Final challenge review, confidence changes, coverage check, and required downgrades. | [Round 4 Final Review Confidence](rounds/ROUND-4-final-review-confidence.md) |

## Final Local Testmod Status

The confidence score is the review confidence for semantic target to native-owner/domain mapping. It is not API stability.

| Local mod | Final owner verdict | Final API status | R4 confidence | Key native/domain owner |
| --- | --- | --- | ---: | --- |
| `HelloDtmMod` | Core-owned framework sample | Stable for entry/log sample | 95 | `DtmMod.Entry`, `IDtmHelper.Monitor`, `GameLaunched` |
| `ConfigMenuExample` | DTMAPI-owned config UI sample | StableCandidate | 88 | `IDtmConfigMenuApi`, `ConfigMenuRegistry` |
| `BrokenManifestMod` | Core loader diagnostic | Core Diagnostic | 96 | manifest reader and loader diagnostics |
| `HookProbeMod` | DTMAPI diagnostic fixture | Diagnostic/Internal | 90 | GameLoop, Save, Workshop, Diagnostics evidence paths |
| `DebugConsoleMod` | DTMAPI UI host plus native debug adapters | Diagnostic | 86 | `DolocAPI`, `ArchiveDataHandle`, `MotionAbility` debug paths |
| `MoreSavesMod` | Native save-panel slot adapter | Experimental | 82 | `archiveFileCount`, `LocalSave`, `GameDataPanel` |
| `ZoomMod` | Orthographic camera view lease | Experimental, `OrthographicOnly` | 84 | playable camera `orthographicSize` path |
| `ActionSpeedMod` | Multi-state action speed sidecar | Experimental | 74 | `AgentStateTool`, `AgentStateInteract`, `AgentStateEat`, `UseItemContinues` |
| `AutoFishingMod` | Native fishing loop plus DTMAPI policies | Experimental | 87 | `BodyController.UseFishRod`, `AgentStateFishing*`, `FishingGameScrollBar` |
| `OneActionCompleteMod` | Narrow action-completion slices | Experimental | 83 | `ToolCollider.HandleTools`, `DungeonResource`, `PowerGeneratorFuel`, `Feeder` |
| `AutoHarvestMod` | Crop-container harvest path | Experimental | 80 | `PlantBasin.CouldHarvest`, `PlantBasin.Harvest`, `Crop` |
| `CropHarvestingQaMod` | Crop-harvest QA fixture | Diagnostic fixture | 77 | same crop-container owner as AutoHarvest |
| `StrongPlantingGunMod` | Fixed three-slot farming gun adapter | Restricted Experimental | 80 | `ItemFarmingGun`, `FarmingGunUiState`, `LinearInventory`, `PlantBasin` |
| `AnimalHusbandryProgressMod` | Animal viewer display decoration | Experimental, display-only | 82 | `AnimalFullInfoData`, `AnimalViewer.Show`, `AnimalPanel.RefreshViewer` |
| `FishBreedingAssistantMod` | Fish roe tooltip/title display | Experimental, partial display-only | 66 | `ItemFishRoe`, `Item.get_title`, tooltip/detail paths |
| `ChestLocatorEnhancerMod` | Inventory-array extension | Experimental | 82 | `ArchiveDataHandle.GetAvailableInventories`, `LinearInventory`, `Case`, `StorageShelf` |
| `OilMod` | Official item content plus runtime coal drop | Content Found, runtime Experimental | 74 | item JSON content, `ToolCollider.HandleTools`, backpack placement |
| `MineMod` | Official content plus DTMAPI production loop | Experimental/Gap | 70 | recipe/content owners, `IElectronicComponent`, `Case.inventory`, sidecar scheduler |
| `MoreEquipmentSlotsMod` | Attribute sidecar equipment slots | Experimental | 78 | `AgentEquipmentManager`, `AgentEquipmentFunction`, `AccessoriesBar`, sidecar storage |
| `SecondMotorMod` | Native motor clone/lease | Experimental | 82 | `ItemMotorKey`, `MotorController`, `AgentControllerState`, `DolocAPI` motor paths |

## Coverage Summary

- 20 current `testmods` are covered.
- 5 legacy own-mod source folders are covered as semantic history only.
- Local third-party sample groups are covered as clean-room demand evidence only.
- New NPCs, complete new animals, multi active drones, true new vehicle types, runtime map boundary expansion, true native equipment slot expansion, and trainer-style state mutation remain `Blocked`, `Diagnostic`, or future deep-dive topics.

## Next Use Pattern

1. Pick one local mod or one demand cluster.
2. Re-open the linked native-owner domain report and public API matrix row.
3. Inspect method bodies for the exact native owner and state holder.
4. Write one narrow API rebuild goal only if the owner questions are concrete.
5. Keep third-party samples out of implementation unless license/permission is explicit and compatible.
