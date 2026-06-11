# Crops Harvesting Native Responsibility Review - 2026-06-12

## Scope

Public symbol/domain: `ICropHarvestingApi`, `CropHarvestRequest`, `CropHarvestResult`, `CropHarvestTargetResult`, `Crops.HarvestingApi`, and `Smoke.CropHarvestingApi`.

Current matrix status before this work: no public DTMAPI crop harvesting API.

Recommended status after this review: open a narrow `Experimental` API for ordinary mature crop scan/harvest requests, backed by the native `PlantBasin.CouldHarvest` / `PlantBasin.Harvest(bool putInBackpack, bool sendMessage)` responsibility path. Do not stabilize trees, grass/forage, mushroom/vine special families, auto-harvest scheduling, save/load crop mutation, or item spawning.

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
| Trees | `PlantBasinTree` / `TreeCrop` families | DTMAPI classifies these as separate targets and does not execute harvest in the first API. | Unsupported in v1. |
| Grass/forage | `PlantBasinGrass` / `ForageGrass.Harvest` families | DTMAPI classifies as unsupported to avoid conflating ordinary crop harvest with grass/forage. | Unsupported in v1. |
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
- The first version only executes the reviewed ordinary `PlantBasin.Harvest` route.
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
- Tree, grass, forage, and special crop families need separate native-owner reviews before execution support.
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
