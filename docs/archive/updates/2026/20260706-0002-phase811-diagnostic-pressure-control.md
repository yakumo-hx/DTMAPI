# 20260706-0002 - Phase 8.11 Diagnostic Pressure Control Before Service-Disable

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / issue-010-open
Area: smoke/saveload/issue-010/diagnostic-pressure/native-loadgame

## Trigger

After Phase 8.10 captured a second comparable full no-probe fatal before SaveLoaded, the user requested one `SaveLoadObjectSnapshotMode=Lite` full-profile run before any service-level hard-disable. The goal was to test diagnostic pressure while preserving the known failing feature profile.

## Summary

Ran the full known-failing profile:

- `CoreCustomAnimals`
- action/utility Workshop IDs
- `Local.Yuuka_DTMAPI_ManboCardboardAudio`
- YConsole, MoreEquipmentSlots, MoreSaves, Zoom
- AutoFishing disabled
- `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-SaveLoadObjectSnapshotMode Lite`
- `3600s` continuous title idle
- max two LoadGame attempts
- `-TimeoutSeconds 6000`
- `-FatalWindowCrashDumpGraceSeconds 30`

Evidence `docs/debug/evidence/GAME-SMOKE/20260706-005212` reproduced Fatal GC, but the window shifted. Unlike the comparable full no-probe `Full` samples, this run reached SaveLoaded and completed every SaveLoaded breadcrumb through `Hook.Exit`; the later Unity stack points through `TextureUtils.DrawArea`, `MapManager.Init(bool)`, `DolocAPI.AfterLoadArchiveData(bool)`, and dynamic `DolocAPI::LoadGame(int)` before native LoadGame returned.

## Changed Files

- `docs/reviews/code/2026/20260706-0001-phase811-diagnostic-pressure-control.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260706-0002-phase811-diagnostic-pressure-control.md`
- `docs/updates/INDEX.md`

No source code or public API files were changed in this phase.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260706-005212`
  - `RunStatus=Aborted`
  - `HookProbe=Passed`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Passed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `OfficialModProfileRestored=True`
- SaveLoad state:
  - `requests=1`
  - `nativeEnter=1`
  - `nativeReturn=0`
  - `saveLoaded=1`
  - duplicate requests `0`
- Final DTMAPI line:
  - `SaveLoaded.Step=Hook.Exit elapsedMs=637 gc0=306 gc1=306 gc2=306 totalMemory=783945728 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=LogExport.`
- Unity crash:
  - latest copied crash directory `Crash_2026-07-05_175308638`
  - fatal process `ProcessId=59144`, `MainWindowTitle=Fatal error in GC`
  - crash evidence present in live collect and post-close collect, final source live collect
- Unity stack:
  - `mono_gc_register_root`
  - `mono_array_new_specific`
  - `DolocTown.TextureUtils.DrawArea`
  - `DolocTown.TextureUtils.CreateTexture`
  - `DolocTown.MapManager.Init(bool)`
  - `DolocAPI.AfterLoadArchiveData(bool)`
  - dynamic `DolocAPI::LoadGame(int)`

## Lite Verification

Lite was verified beyond the command line:

- `summary.txt`: `SaveLoadObjectSnapshotMode=Lite`
- `result.json`: `"SaveLoadObjectSnapshotMode": "Lite"`
- `DTMAPI-latest.log`: `SaveLoad object snapshot mode configured mode=Lite source=smoke-settings.json smokeOnly=true.`
- `BeforeNextLoadGame`: snapshot `9` logged `saveLoad={mode=Lite, requestId=none}` and `Runtime={snapshotMode=Lite...}`.
- `LoadGameNativeEnter`: snapshot `11` logged `saveLoad={mode=Lite, requestId=SL-0001}` and `Runtime={snapshotMode=Lite...}`.

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` process was running before the smoke.

Post-run:

- Runtime lock was released.
- `official-mod-profile-summary.json` was exact for the requested profile and AutoFishing was disabled.
- Profile was restored.
- No leftover `DolocTown.exe` process was found after collection/cleanup.

## Decision

This was not a Lite pass, so do not claim diagnostic pressure was proven as the root cause.

This was not the same pre-SaveLoaded terrain/dungeon fatal as the two comparable no-probe `Full` samples, so do not classify this as "Full snapshot not necessary for the same pre-SaveLoaded native LoadGame fatal."

Because the fatal window shifted after Lite, stop before:

- a second Lite rerun;
- `Off`;
- service-level hard-disable;
- more UI owner/pair splits.

Next analysis should focus on the shifted post-SaveLoaded, pre-native-return LoadGame continuation around `DolocAPI.AfterLoadArchiveData`, `MapManager.Init`, and texture allocation under the same high-pressure full profile.

## Rollback

Docs-only update. No source rollback is needed.

## Follow-Up

- Keep `GAME-SMOKE/20260706-005212` as the Phase 8.11 diagnostic-pressure control sample.
- Compare it directly with full no-probe `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212`, plus PreLoad-GC `GAME-SMOKE/20260705-205236`.
- Plan the next root-set step around native LoadGame activation timing and diagnostic pressure, with HookProbe/no-HookProbe considered before broad service-level hard-disable.
- Continue preserving the guardrails: no public API change, no GameBridge takeover, no service hard-disable in this phase, and no unknown native Unity object destruction.
