# Crops Harvesting Native Responsibility Review - 2026-06-12

## Scope

Public symbol/domain: `ICropHarvestingApi`, `CropHarvestRequest`, `CropHarvestResult`, `CropHarvestTargetResult`, `Crops.HarvestingApi`, and `Smoke.CropHarvestingApi`.

Current matrix status before this work: no public DTMAPI crop harvesting API.

Recommended status after this review: open a narrow `Experimental` API for ordinary mature crop-container scan/harvest requests, backed by the native `PlantBasin.CouldHarvest` / `PlantBasin.Harvest(bool putInBackpack, bool sendMessage)` responsibility path. Do not stabilize tree-basin cocoa execution, grass/forage basins, non-PlantBasin special families, auto-harvest scheduling, save/load crop mutation, or item spawning.

## 2026-06-12 Hardening Addendum

After review of the first audit package, the public wording must be narrower than "auto harvest trees/grass." The API is a crop-container harvesting API, not a wild tree, forage, or grass automation API.

Current classification policy:

- `OrdinaryCrop`, `Vine`, `MushroomBag`, and `Bush` are PlantBasin-family crop-container categories. They may execute only when the target is still backed by the reviewed `PlantBasin.Harvest(bool,bool)` native owner and basin-level harvestability is true.
- `TreeBasinCrop` means a `PlantBasinTree` crop container. In the current game this is the cocoa-style tree-basin crop path, not a generic wild-tree harvest path. It does not expose `PlantBasin.Harvest(bool,bool)` and must stay scan-only/unsupported until a separate native-owner review proves the `TreeCrop` / `PlantBasinTree.GenerateCropOutput` route.
- `GrassForageBasin` means `PlantBasinGrass` / forage-style basin content. It is outside this API's execution scope and must not be described as crop harvesting support.

Execution gate policy:

- `Pending` / executable harvest targets must be based on basin-level `CouldHarvest` or `IsCropMature`.
- `Crop.isMature` and `TreeCrop.isMature` are diagnostic/display facts only. They must not by themselves cause DTMAPI to invoke a native harvest.
- `ScanMatureCrops` with no mature executable targets is a successful no-op, not an API failure.

## 2026-06-12 Manual QA Handoff Addendum

The medium/long-term follow-up adds a developer-only `CropHarvestingQaMod`
manual QA fixture without expanding the API contract.

Purpose:

- give the user a directly hand-testable setup for real farm scenes;
- separate release/sample `AutoHarvestMod` behavior from detailed QA logging;
- keep tree-basin/cocoa and grass/forage families visible in scan summaries
  while preserving their non-executing status.

Fixture controls after QA-isolation cleanup:

- the fixture is installed only through explicit QA-fixture install mode;
- default hotkeys are `None` to avoid collisions with AutoFishing, ActionSpeed,
  HookProbe, and other developer mods;
- DTMAPI Settings page buttons provide scan-only, harvest-one, and harvest-batch
  operations;
- optional manual bindings may still be set for an isolated keyboard pass. The
  partial user-verified pass used historical `F8` scan, `F9` harvest-one, and
  `F10` harvest-batch bindings.

The fixture consumes only `ICropHarvestingApi`; it does not reference
`Assembly-CSharp`, Harmony, raw `PlantBasin`, raw `Crop`, or raw `TreeCrop`.
It is listed only in developer-local official mod definitions and must not be
treated as part of the first Workshop release set.

Manual checklist:

- `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`

Blocking rule: if manual QA shows `TreeBasinCrop`, wild grass, wild tree, or
forage harvesting through this API slice, the API must remain blocked for
mainline/public use until the native owner is reviewed and the contract is
split or corrected.

## Files Read

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Crops.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/metadata/methods.csv`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/metadata/types.csv`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeHelpers.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ActionSpeedSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunService.cs`

## External Compatibility Observation

The user supplied screenshots and notes for an old Workshop package at `D:\Steam\steamapps\workshop\content\2285550\3742771572`. This package is compatibility context only and is not copied or imitated.

Observed old layout:

```text
Content/
  DolocSMAPI/
    manifest.json
    plugins/
      DolocTownAutoHarvest.dll
```

Observed old manifest/DLL facts:

- `UniqueID=None.AutoHarvest`
- `MinimumApiVersion=0.8.1`
- `EntryDll=plugins/DolocTownAutoHarvest.dll`
- `EntryType=Dlk.DolocAutoHarvest.AutoHarvestMod`
- DLL references old `DolocTownSMAPI.SDK`, `0Harmony`, and `Assembly-CSharp`.
- Type shape included a mod entry with lifecycle callbacks and a `CropGrowthPatch`.
- The implementation reportedly scanned farm rooms, found `PlantBasin`, checked maturity, and called `PlantBasin.Harvest(...)`.

Review conclusion: the old package proves demand and suggests native responsibility names, but its architecture is not acceptable for DTMAPI rebuild work. New DTMAPI code must not depend on old SMAPI SDK types, old Harmony patches, or raw `Assembly-CSharp` references in ordinary mods. The safe DTMAPI route is a GameBridge-owned experimental API over reviewed native owners.

## Native Owner Map

| Responsibility | Native owner / state holder | DTMAPI relation | Review result |
| --- | --- | --- | --- |
| Crop instance state | `DolocTown.Crop` with `isMature`, growth/month state, output generation, and `AfterHarvest` | DTMAPI reads maturity and identifiers through reflection only inside GameBridge. | Native-owned state. Public API must not expose raw crop objects. |
| Crop container | `DolocTown.PlantBasin` with `HasCrop`, `Crop`, `CouldHarvest`, and `Harvest(bool,bool)` | DTMAPI may scan and execute this path when a caller explicitly requests harvest. | First API version can use this native owner. |
| Official harvest side effects | `PlantBasin.Harvest` calling native output, broadcast, `Crop.GenCropOutput`, `Crop.AfterHarvest`, and `PlantBasin.AfterHarvest` | DTMAPI delegates to this method and does not hand-spawn items. | Required execution path. |
| Automation discovery | `AutomateSystemEnv.GetMatureCropNearStation` and `AutomateTaskHarvestCrop.OnExecute` | DTMAPI uses these as proof that native automation already treats mature `PlantBasin` as harvest target; DTMAPI does not copy station routing. | Supporting native-owner evidence only. |
| Tree-basin crop container | `PlantBasinTree` / `TreeCrop` families, currently cocoa-style crop container rather than generic wild tree harvest | DTMAPI classifies as `TreeBasinCrop` and does not execute harvest in the first API. | Scan-only/unsupported in v1 until separate native-owner review. |
| Grass/forage basin | `PlantBasinGrass` / `ForageGrass.Harvest` families | DTMAPI classifies as `GrassForageBasin` to avoid conflating ordinary crop harvest with forage/grass. | Unsupported in v1. |
| Room/equipment scan | `Room.DM_equipment.AllEquipments` and farm/building room graph | DTMAPI scans current farm and farm rooms for explicit API calls. | Experimental scan policy, not stable global crop ownership. |

## Chosen API Shape

The API is semantic and narrow:

```text
ICropHarvestingApi.ScanMatureCrops(owner, request)
ICropHarvestingApi.HarvestMatureCrops(owner, request)
```

`CropHarvestRequest` controls scope, families, dry-run behavior, max harvest count, native message output, verbose logging, and optional opaque target ids returned by scan. `CropHarvestResult` reports total scanned targets, mature targets, harvested count, skipped count, failed count, and per-target statuses.

Important safety choices:

- Calls are explicit API requests, not an always-on Harmony patch.
- A batch lock prevents overlapping scan/harvest operations.
- Harvest revalidates the target immediately before invoking native harvest.
- Harvest is limited by `MaxHarvests` and optional `TargetIds`.
- The first version only executes reviewed `PlantBasin.Harvest(bool,bool)` targets, gated by basin-level harvestability.
- Unsupported target kinds are reported rather than force-harvested.
- No item output is spawned by DTMAPI code.

## Status Model

Target statuses:

- `Pending`: mature target can be harvested or dry-run found it.
- `Harvested`: native harvest returned successfully.
- `NotMature`: target exists but is not mature.
- `AlreadyHarvested`: target lost its harvestable crop before execution.
- `UnsupportedBasinType`: target is a native crop family not owned by the first API.
- `NativeHarvestFailed`: native owner threw or returned an unexpected result.
- `SkippedByRequestFilter`: request family filter, target-id filter, or max-harvest limit skipped the target.
- `Busy`: another scan/harvest batch is in progress.

## Ordinary Mod Usability

Ordinary mods should use this API for conservative automation and previews only. They should not patch crop growth, call raw `PlantBasin`/`Crop`, or ship `Assembly-CSharp` references. The included `AutoHarvestMod` sample is intentionally default-off and uses only `ICropHarvestingApi`.

## Risks And Open Questions

- Room traversal is limited to current farm/current room/building-room relationships; broader map ownership needs more native review.
- Tree-basin cocoa, grass/forage, and any crop container not backed by the reviewed `PlantBasin.Harvest(bool,bool)` path need separate native-owner reviews before execution support.
- Multi-owner scheduling is not merged; each mod call is explicit and isolated by the batch lock.
- Save/load, season rollover, and growth tick ownership are intentionally untouched.
- Real player acceptance still needs longer manual play with multiple mature crop types before any stability promotion.

## Recommended Next Action

Proceed with an `Experimental` Crops/Harvesting API slice:

- Add public experimental DTOs and `ICropHarvestingApi`.
- Add `CropHarvestingFeature` / service ownership in GameBridge.
- Add a default-off `AutoHarvestMod` test mod that consumes only the API.
- Add a smoke-only transient `PlantBasin` mature-crop proof.
- Update API matrix, hook map, smoke matrix, and update records.

Do not modify Camera, Fishing, SaveSlots, StrongPlantingGun, AnimalViewer, release installer, Workshop upload, or old SMAPI migration behavior in this slice.
