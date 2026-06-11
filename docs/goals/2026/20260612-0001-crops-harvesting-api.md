# Crops Harvesting API Goal

Status: completed
Created: 2026-06-12
Target branch: `codex/crops-harvesting-api`
Target merge branch: future mainline / Refactor successor
Target version: no version bump

## Source Request

The user asked to implement a Crops / Harvesting API based on native responsibility functions, after reading `AGENTS.md`, writing update/debug records, and treating screenshots of an external old AutoHarvest mod as reference only.

## Required Reading

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
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`

## Scope

Implement the first experimental Crops / Harvesting API:

- `ICropHarvestingApi.ScanMatureCrops(IManifest owner, CropHarvestRequest request)`
- `ICropHarvestingApi.HarvestMatureCrops(IManifest owner, CropHarvestRequest request)`
- `CropHarvestRequest`
- `CropHarvestResult`
- `CropHarvestTargetResult`
- crop target status/kind/scope enums
- `CropHarvestingFeature`, `CropHarvestingService`, and smoke integration
- default-off `testmods/AutoHarvestMod` using only the DTMAPI API

## Constraints

- Do not copy or imitate old DLKsmapi / DolocSMAPI code.
- Do not reference old SMAPI SDK, old AutoHarvest DLL code, `Assembly-CSharp`, or Harmony from ordinary `AutoHarvestMod`.
- Do not hand-spawn harvest output.
- Use reviewed native owners only: first version executes ordinary `PlantBasin.Harvest(bool,bool)` after revalidation.
- Do not modify CameraView, Fishing, SaveSlots, StrongPlantingGun, AnimalViewer, release installer, Workshop uploader, or public API stability labels outside the new experimental API.

## Acceptance Criteria

- Public API compiles and remains marked `Experimental`.
- GameBridge registers `ICropHarvestingApi` through the feature host.
- Hook/status entry `Crops.HarvestingApi` is published as explicit API ownership, not as a Harmony patch.
- Smoke path can create a transient mature `PlantBasin`, scan it by opaque target id, harvest it through the API, and verify it is no longer mature/harvestable.
- `AutoHarvestMod` is default-off and consumes only `ICropHarvestingApi`.
- API matrix, hook map, smoke matrix, native-owner review, and update record are updated.

## Required Validation

- `git diff --check`
- `tools/scripts/build.ps1 -Configuration Release`
- `tools/scripts/test.ps1 -Configuration Release`
- `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseCropHarvestingApi -SaveSlot 3 -TimeoutSeconds 240`

If the game smoke cannot produce a mature-crop scene or fails, keep the API Experimental, record evidence, and do not claim stable crop automation.

## Completion Notes

Final response should include:

- public API and feature/service summary;
- native responsibility function used;
- safety boundaries and unsupported target families;
- test mod behavior;
- build/test/smoke results;
- remaining work before any stability promotion.
