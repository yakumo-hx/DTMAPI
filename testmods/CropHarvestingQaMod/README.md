# Crop Harvesting QA Mod

Temporary developer-only manual QA fixture for the Experimental
`ICropHarvestingApi`.

This mod is intentionally separate from `AutoHarvestMod`:

- `AutoHarvestMod` is the ordinary author-facing sample.
- `CropHarvestingQaMod` is a hand-test tool for checking real farm scenes before
  the API is merged onward.

## Controls

- `F8`: dry-run scan only.
- `F9`: scan, then harvest one pending target by transient `TargetId`.
- `F10`: scan, then harvest up to `MaxHarvests` pending targets.
- DTMAPI Settings page: equivalent buttons plus toggles for target families and
  log verbosity.

Each operation writes a compact summary to the DTMAPI log:

- result counts: rooms, basins, mature, harvested, skipped, failed;
- target kind counts: ordinary, vine, mushroom bag, bush, tree-basin crop,
  grass/forage basin;
- target status counts;
- sample per-target rows when verbose target rows are enabled.

## Safety Boundary

The mod only calls `ICropHarvestingApi`. It does not reference
`Assembly-CSharp`, does not install Harmony patches, and does not call raw
`PlantBasin`, `Crop`, or `TreeCrop` methods.

The current API slice is crop-container only:

- ordinary `PlantBasin` targets may execute when basin-level harvestability is
  true;
- PlantBasin-family vine, mushroom-bag, and bush categories may execute only
  when they still use the reviewed `PlantBasin.Harvest(bool,bool)` owner;
- `TreeBasinCrop` is the cocoa-style tree-basin crop-container path and remains
  scan-only/unsupported until a separate native-owner review;
- `GrassForageBasin`, wild grass, wild trees, and forage are outside this API's
  execution scope.

Do not publish this mod to Workshop. It is a local QA fixture.
