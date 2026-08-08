# Crop Harvesting QA Mod

Temporary developer-only manual QA fixture for the Experimental
`ICropHarvestingApi`.

This mod is intentionally separate from `AutoHarvestMod`:

- `AutoHarvestMod` is the ordinary author-facing sample.
- `CropHarvestingQaMod` is a hand-test tool for checking real farm scenes before
  the API is merged onward.

It is an explicit QA fixture. It is not installed by the default developer-local
official-mod path; use `install-to-game.ps1 -InstallQaFixtures` when this fixture
is needed for a manual crop pass.

## Controls

- Default hotkeys are `None` to avoid collisions with AutoFishing, ActionSpeed,
  HookProbe, and other developer mods.
- DTMAPI Settings page: scan, harvest-one, harvest-batch, target-family toggles,
  and log verbosity.
- Optional manual bindings: set scan/harvest keys in the DTMAPI Settings page
  if a keyboard-driven pass is desired. The historical hand-test used `F8` for
  scan, `F9` for harvest-one, and `F10` for harvest-batch.

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
