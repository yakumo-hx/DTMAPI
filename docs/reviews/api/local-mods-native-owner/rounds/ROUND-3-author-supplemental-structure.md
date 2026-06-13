# Round 3: Author Responses And Supplemental Reports

Status: complete
Date: 2026-06-13
Mode: parallel response and revised reporting

Round 3 answered Round 2 questions and produced revised reports. The main effect was to split native-owner claims into narrower categories.

## Gameplay And Crop Revisions

| Mod | R3 confidence | Revised conclusion |
| --- | ---: | --- |
| `ActionSpeedMod` | 76 | Experimental. Uses multiple `AgentState*` owners and DTMAPI restore/policy. Needs explicit GameBridge dependency. |
| `AutoFishingMod` | 88 | Experimental. Strong native fishing loop owner, but F6/cancel/recast/restore policies are DTMAPI-owned. Needs 0.5.1-alpha version alignment. |
| `OneActionCompleteMod` | 84 | Experimental. Narrow resources/fuel/feed paths only. |
| `AutoHarvestMod` | 82 | Experimental. Crop-container harvesting only. Needs explicit GameBridge dependency. |
| `CropHarvestingQaMod` | 78 | Diagnostic fixture. Supports evidence but does not promote `ICropHarvestingApi`. |
| `StrongPlantingGunMod` | 79 | Restricted Experimental. Fixed three-slot seed/film/fertilizer adapter only. |

Pending crop cases stayed out of scope:

- `TreeBasinCrop` and `PlantBasinTree`;
- `GrassForageBasin` and `PlantBasinGrass`;
- wild grass, trees, and forage;
- full inventory;
- reload after old target id;
- cross-farm/building room traversal.

## Animal, Fish, Chest, Oil, Mine Revisions

| Mod | R3 confidence | Revised conclusion |
| --- | ---: | --- |
| `AnimalHusbandryProgressMod` | 82 | Display-only Experimental. Does not own feeding, breeding, manure, products, or trade. |
| `FishBreedingAssistantMod` | 68 | Tooltip/title Partial. Real fish roe lookup not found. |
| `ChestLocatorEnhancerMod` | 80 | Experimental inventory-array extension. Native array owner found, transaction scope still limited. |
| `OilMod` | 76 | Item content Found. Runtime coal drop remains Experimental via shared tool-collider route. |
| `MineMod` | 70 | Content found; production remains DTMAPI sidecar loop. |

Important split:

- `OilMod` content and coal-drop behavior are separate evidence classes.
- `MineMod` recipe/item/tech content and production scheduling are separate evidence classes.

## UI, Core, Save, Equipment, Vehicle, Camera Revisions

| Mod | R3 confidence | Revised conclusion |
| --- | ---: | --- |
| `DebugConsoleMod` | 86 | Diagnostic. DTMAPI UI host plus debug adapters. |
| `MoreSavesMod` | 82 | Experimental. Official save-panel slot adapter only. |
| `MoreEquipmentSlotsMod` | 78 | Experimental. Sidecar attribute slots, not native equipment slot expansion. |
| `SecondMotorMod` | 82 | Experimental. Native motor clone adapter, not generic vehicle API. |
| `ZoomMod` | 84 | Experimental, `OrthographicOnly`. |
| `ConfigMenuExample` | 88 | StableCandidate. |
| `HelloDtmMod` | 95 | Stable for entry/log sample. |
| `HookProbeMod` | 90 | Diagnostic/Internal evidence fixture. |
| `BrokenManifestMod` | 96 | Core Diagnostic. |

## Third-Party Revisions

Round 3 changed all third-party samples to `demand-only / semantic backlog`.

| Sample group | Revised conclusion |
| --- | --- |
| ExpandedEncyclopedia | Content/encyclopedia demand. Owner to verify. |
| HoldToHarvest | Crop plus input-repeat demand. Owner to verify. |
| Infinite Hover | Motor endurance demand. Experimental backlog. |
| Genesis Core / ContentLoader | Loader compatibility risk, not gameplay proof. |
| Auto Drone | Strong demand; split battery, resource, tool gate, inventory, story lock. |
| BuildingExpander | Runtime geometry mutation blocked; query diagnostics only. |
| DolocPlus | Broad taxonomy: QoL candidate, Experimental gameplay, Diagnostic/cheat, Blocked. |
| FullTrainer | Semantic backlog only. |

License redline:

- no explicit permission means no migration, copying, or redistribution;
- visible symbols, README text, passwords, screenshots, or PDBs are not authorization;
- future DTMAPI equivalents must be clean-room.

## Structure Revision

Round 3 proposed the permanent directory:

```text
docs/reviews/api/local-mods-native-owner/
```

The final implementation uses a compact subset:

- `INDEX.md`
- `SOURCE-INDEX.md`
- `REPORT-TEMPLATE.md`
- `inventory.md`
- `api-demand-clusters.md`
- `shared-native-owner-conflicts.md`
- `confidence-changes.md`
- `implementation-follow-ups.md`
- `rounds/ROUND-1-mod-semantics-native-owner.md`
- `rounds/ROUND-2-review-questions-confidence.md`
- `rounds/ROUND-3-author-supplemental-structure.md`
- `rounds/ROUND-4-final-review-confidence.md`
