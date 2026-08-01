# 20260705-0012 - Phase 8.10 Comparable Full No-Probe Breadcrumb Run

Date: 2026-07-05/06 +08:00
Status: runtime-evidence-captured / issue-010-open
Area: smoke/saveload/issue-010/native-loadgame/root-set

## Trigger

After Phase 8.9 compared one no-probe fatal and one PreLoad-GC fatal, the user requested exactly one comparable full no-probe run with the new SaveLoaded breadcrumbs installed. The goal was to classify the no-probe baseline window before trying `Lite`/`Off` or any service-level hard-disable.

## Summary

Ran one full known-failing profile:

- `CoreCustomAnimals`
- action/utility Workshop IDs
- `Local.Yuuka_DTMAPI_ManboCardboardAudio`
- YConsole, MoreEquipmentSlots, MoreSaves, Zoom
- AutoFishing disabled
- `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-SaveLoadObjectSnapshotMode Full`
- `3600s` continuous title idle
- max two LoadGame attempts
- `-TimeoutSeconds 6000`
- `-FatalWindowCrashDumpGraceSeconds 30`

Evidence `docs/debug/evidence/GAME-SMOKE/20260705-231212` reproduced Fatal GC on the first post-idle native LoadGame before SaveLoaded. `SaveLoaded.Step` breadcrumb count was `0`, so the new breadcrumb confirmed the fatal window did not reach the SaveLoaded hook/runtime chain.

## Changed Files

- `docs/reviews/code/2026/20260705-0006-phase810-comparable-full-no-probe-breadcrumb.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0012-phase810-comparable-full-no-probe.md`
- `docs/updates/INDEX.md`

No source code or public API files were changed in this phase.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260705-231212`
  - `RunStatus=Aborted`
  - `HookProbe=Passed`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Full`
  - `SaveLoaded=Failed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `OfficialModProfileRestored=True`
- SaveLoad state:
  - `requests=1`
  - `nativeEnter=1`
  - `nativeReturn=0`
  - `saveLoaded=0`
  - duplicate requests `0`
- Final DTMAPI line:
  - `LoadGame requested for slot/index 2. requestId=SL-0001.`
- Unity crash:
  - latest copied crash directory `Crash_2026-07-05_161323045`
  - fatal process `ProcessId=58872`, `MainWindowTitle=Fatal error in GC`
  - crash evidence present in live collect and post-close collect, final source live collect
- Unity stack:
  - `mono_gc_register_root`
  - `TerrainLayer`
  - `Terrain.Create`
  - `Room.ResetTerrain`
  - `Room.AfterLoadData`
  - `Dungeon.AfterLoadData`
  - `DataPersistenceManager.LoadGame(int)`
  - dynamic `DolocAPI::LoadGame(int)`

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` process was running before the smoke.

Post-run:

- Runtime lock was released.
- `official-mod-profile-summary.json` was exact for the requested profile and AutoFishing was disabled.
- `process-check.txt` reported no leftover `DolocTown.exe`.

## Decision

This is the second comparable no-probe full-profile fatal before SaveLoaded, matching `GAME-SMOKE/20260705-194759`.

Therefore:

- Do not run `SaveLoadObjectSnapshotMode Lite|Off` for this window.
- Do not treat the SaveLoaded snapshot path as the no-probe baseline root cause.
- Continue native LoadGame/save activation root-set analysis.
- Do not resume UI owner/pair bisection.
- Do not do service-level hard-disable in this phase.

## Rollback

Docs-only update. No source rollback is needed.

## Follow-Up

- Use `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212` as the two comparable no-probe full-profile native LoadGame fatal samples.
- Keep `GAME-SMOKE/20260705-205236` as a separate PreLoad-GC sample showing the SaveLoaded full snapshot/log path can be a trigger or amplifier after native activation reaches SaveLoaded.
- Next investigation should focus on native/root-set analysis around save-load terrain/dungeon activation and the full-profile stable title/process root-set.
- Keep `Full` as default; reserve `Lite`/`Off` only for a future no-probe fatal that actually reaches SaveLoaded/snapshot.
