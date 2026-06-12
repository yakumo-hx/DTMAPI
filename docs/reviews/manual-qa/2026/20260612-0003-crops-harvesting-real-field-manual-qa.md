# Crops Harvesting Real-Field Manual QA Checklist

Status: partial user verified
Created: 2026-06-12
Branch: `codex/crops-harvesting-manual-qa-handoff`
Related API review: `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
Related automated smoke: `docs/debug/evidence/GAME-SMOKE/20260612-072544`
Manual fixture: `testmods/CropHarvestingQaMod`

## User Confirmation - 2026-06-12

User reported that the manual test result was OK:

- `F9` harvests one crop target.
- `F10` harvests many crop targets.
- Harvested output goes into the backpack.

Interpretation: this confirms the intended ordinary crop-container hand-test
path for one-target and batch harvest. It does not by itself verify the
remaining family-boundary cases such as vine, mushroom bag, bush, tree-basin
scan-only behavior, grass/forage unsupported behavior, full inventory, farm
building rooms, or title reload.

## Scope

This manual QA record is for the Experimental `ICropHarvestingApi` real-field
behavior before the Crops/Harvesting branch is merged onward.

The API is a crop-container harvesting API. It is not grass, wild tree, or
forage automation. `TreeBasinCrop` means the cocoa-style `PlantBasinTree`
crop-container family and remains scan-only/unsupported until a separate
native-owner review proves its execution owner.

## Fixture Controls

Install the developer local DTMAPI packages with explicit QA fixtures, then
enable `DTMAPI 作物收获手测夹具`.

- Default hotkeys are `None` to avoid collisions with AutoFishing, ActionSpeed,
  HookProbe, and other developer mods.
- DTMAPI Settings -> `Crop Harvesting QA`: scan-only, harvest-one, and
  harvest-batch buttons.
- Optional manual bindings: bind scan/harvest keys in DTMAPI Settings for an
  isolated keyboard-driven pass. The partial user-verified pass used historical
  bindings `F8` scan, `F9` harvest one, and `F10` harvest batch.

The fixture writes `CropHarvesting QA ...` log lines with result counts,
target-kind counts, target-status counts, and sample target rows.

## Manual Cases

| Case | Setup | Action | Expected | Observed | Status |
| --- | --- | --- | --- | --- | --- |
| Ordinary mature crop | Place or find one mature ordinary `PlantBasin` crop on the farm. | Press `F8`, then `F9`. | Scan reports at least one `OrdinaryCrop=...` pending target; harvest calls the API by transient `TargetId`; crop is collected by native behavior; second scan does not report the same target as pending. | User reported `F9` harvests one target and output goes into the backpack. | user verified |
| Ordinary immature crop | Place or find an immature ordinary `PlantBasin` crop. | Press `F8`. | Target is not harvested and appears as `NotMature` or another non-pending state. | pending | pending |
| Multiple mature crops | Prepare at least two mature ordinary crop-container targets. | Set `Max harvests` to 1, press `F10`; then set higher and press `F10` again. | First pass harvests only one pending target; later pass can harvest remaining pending targets; no duplicated drops from already harvested targets. | User reported `F10` harvests many targets and output goes into the backpack. | user verified |
| Consecutive no-duplicate run | After a successful harvest, immediately press `F9` again. | Press `F9` twice without changing the scene. | Second pass is a successful no-op or reports no pending executable target; no duplicate output. | pending | pending |
| Farm building room crop | Put or find an eligible crop-container target inside a farm building room if available. | Press `F8`, then `F9` while in or after visiting the room. | Farm-scope traversal finds the room only if the current native root/farm relationship is available; if no farm scope is found, API fails clearly rather than harvesting arbitrary rooms. | pending | pending |
| Vine crop-container | Find a vine-type crop-container if available. | Press `F8`, then `F9` only if it is pending. | If backed by reviewed `PlantBasin.Harvest(bool,bool)`, it may harvest; otherwise it must remain non-pending/unsupported. | pending | pending |
| Mushroom bag crop-container | Find a mushroom-bag crop-container if available. | Press `F8`, then `F9` only if it is pending. | If backed by reviewed `PlantBasin.Harvest(bool,bool)`, it may harvest; otherwise it must remain non-pending/unsupported. | pending | pending |
| Bush crop-container | Find a bush crop-container if available. | Press `F8`, then `F9` only if it is pending. | If backed by reviewed `PlantBasin.Harvest(bool,bool)`, it may harvest; otherwise it must remain non-pending/unsupported. | pending | pending |
| Tree-basin cocoa-style crop | Find a cocoa-style `PlantBasinTree` target if available. | Press `F8`, optionally press `F9` with tree-basin display enabled. | Scan may classify it as `TreeBasinCrop`, but it must not execute through `PlantBasin.Harvest(bool,bool)`. Expected status is unsupported/not executed. | pending | pending |
| Grass/forage/wild tree | Stand near wild grass, forage, or wild trees. | Press `F8` and `F9`. | The API must not harvest wild grass, forage, or wild trees. Any related row must be unsupported/not executed. | pending | pending |
| Full inventory | Fill inventory, then attempt one known pending crop-container target. | Press `F9`. | Behavior follows native `PlantBasin.Harvest(bool,bool)` responsibility without hand-spawning items; record whether native path blocks, drops, or otherwise handles full inventory. | pending | pending |
| Title return/reload | Run a scan, return to title, reload the third save. | Press `F8` again after reload. | Fixture clears transient summary; `TargetId` from the old session is not reused; scan/harvest still works in the new loaded session. | pending | pending |

## Evidence To Attach

- DTMAPI startup log.
- `CropHarvesting QA` log lines for each case.
- Screenshots or short clips for harvested crop output and unsupported target
  cases when useful.
- `Player.log` and `BepInEx/LogOutput.log`.
- Exit check: no leftover `DolocTown.exe`, no fatal instance popup.
- Optional report zip from Manager Logs or `package-report.ps1`.

## Blockers

- Any wild tree, grass, or forage target is harvested by this API.
- `TreeBasinCrop` executes through the current `PlantBasin.Harvest(bool,bool)`
  path.
- `TargetId` from a previous save/load session is reusable for harvest.
- No-target scan or no-pending harvest becomes a failure instead of a successful
  no-op.
- Native harvest exceptions repeat without readable diagnostics.
