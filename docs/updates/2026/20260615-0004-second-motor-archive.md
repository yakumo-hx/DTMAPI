# 20260615-0004 Second Motor Archive

## Status

Archived / active project disabled.

## Source Request

User reported the rebuilt second motor was effectively unusable after manual testing: abnormal light textures, abnormal farm-room textures, and severe cross-map texture pollution. User decided to commit and merge the vehicle worktree history, then archive the second motor content while keeping update/debug records. DTMAPI vehicle API code may remain because no active mod will call it.

## Read-Only Research Context

- `E:\DolocTownAssetResearch\README.md`
- `E:\DolocTownAssetResearch\docs\SCOPE.md`
- `E:\DolocTownAssetResearch\builds\23465763_workshop_38581E\reports\domains\03-motor-vehicles.md`

The research confirms the native motor path is single-motor oriented and that official extracted assets must remain local/private research only.

## Changed Files

- `archive/second-motor-20260615/testmods/SecondMotorMod/**`
- `archive/second-motor-20260615/README.md`
- `testmods/README.md`
- `tools/scripts/README.md`
- `tools/scripts/build.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/release/dtmapi-mod-publish-zh - 副本.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Implementation

- Merged the experimental vehicle branch first so its research, API notes, and smoke evidence remain in Git history.
- Moved `testmods/SecondMotorMod` to `archive/second-motor-20260615/testmods/SecondMotorMod`.
- Removed `SecondMotorMod` from the active build project list.
- Removed `DTMAPI_SecondMotor` from developer official package definitions and current release/publish metadata.
- Removed the SecondMotor-only official vehicle example asset copying path from `install-to-game.ps1`.
- Changed `run-game-smoke.ps1 -AutoExerciseVehicle` to return a blocked archived result instead of trying to exercise a retired local package.
- Left `run-game-smoke.ps1 -DisableSecondMotorForSmoke` as a no-op compatibility flag for old commands, but removed its old `mod_infos.json` enablement mutation.
- Moved local installed `DTMAPI_SecondMotor` and `DLK_SecondMotor` packages out of the official local `MODS` folder into `E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS`.

## Validation

- `git diff --check` passed with only existing CRLF normalization warnings.
- PowerShell parser check passed for `tools/scripts/build.ps1`, `tools/scripts/release-common.ps1`, `tools/scripts/install-to-game.ps1`, `tools/scripts/run-game-smoke.ps1`, and `tools/scripts/build-release-workshop-packages.ps1`.
- `tools/release/dtmapi-mod-publish-zh.json` parsed successfully with `ConvertFrom-Json`.
- Official local `MODS` directory had no `SecondMotor`/`Motor` package directories after the archive move.
- Local archive contains both retired packages: `E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS\DTMAPI_SecondMotor` and `E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS\DLK_SecondMotor`.
- `tools/scripts/run-game-smoke.ps1 -SkipBuild -AutoExerciseVehicle` intentionally exited blocked and wrote `RunStatus=Blocked` evidence under `docs/debug/evidence/GAME-SMOKE/20260615-202839`.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.

## Rollback

To restore the archived sample, move `archive/second-motor-20260615/testmods/SecondMotorMod` back to `testmods/SecondMotorMod`, re-add it to `tools/scripts/build.ps1` and `tools/scripts/release-common.ps1`, restore or rebuild local package files, and re-enable the vehicle smoke only after a new native-owner fix and manual QA. Do not restore the old local packages directly as a release candidate.

## Follow-Up

Future vehicle work should start from a smaller native-owner slice using `E:\DolocTownAssetResearch` reports as read-only research inputs. Do not claim stable multi-vehicle support from the archived SecondMotor smoke history.
