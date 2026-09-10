# 20260706-0008 - Phase 8.17 Managed Root Isolation: UI Code Roots

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / no-UI-code-roots passed twice / issue-010-open

## Source Request

External review requested Phase 8.17:

- stop chasing only the `VersionPatcher` continuation route;
- preserve the Phase 8.16 light diagnostic shape;
- remove the four UI code mods from the full known-failing profile;
- keep CustomAnimals, action/utility owners, and Manbo audio;
- run a continuous `3600s` title idle, then at most two LoadGames;
- if the first no-UI-code-roots sample passed, rerun once after a clean restart before drawing conclusions.

## Changed Files

- `docs/reviews/code/2026/20260706-0007-phase817-managed-root-isolation-ui-code-roots.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/debug/evidence/GAME-SMOKE/20260706-163322/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-163322/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-174103/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-174103/validation-summary.txt`

No source files were changed in this phase.

## Runtime Evidence

Two comparable no-UI-code-root samples passed:

- `docs/debug/evidence/GAME-SMOKE/20260706-163322`
- `docs/debug/evidence/GAME-SMOKE/20260706-174103`

Both used:

- no `-IncludeHookProbe`;
- no `-AutoExercisePreLoadGcProbe`;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- `SmokeNativeLoadContinuationProbe=VersionPatcher`;
- `CoreCustomAnimals`;
- extra IDs `Workshop.3742763309`, `Workshop.3742763843`, `Workshop.3742763540`, `Workshop.3742763706`, and `Local.Yuuka_DTMAPI_ManboCardboardAudio`;
- `FatalWindowProcessDumpMode=DbgHelpFull`;
- continuous `3600s` title idle and two maximum LoadGames.

Both ended with:

- `RunStatus=Passed`;
- `SaveLoadCycle=Passed`;
- `NoFatalInstanceWindow=Passed`;
- `ProcessExited=Passed`;
- `requests=2`;
- `nativeEnter=2`;
- `nativeReturn=2`;
- `saveLoaded=2`;
- `fatalWindows=0`.

## Owner / Root Result

Both runs verified that the four UI code mod owners were disabled/skipped:

- `DTMAPI.DebugConsoleMod`
- `DTMAPI.MoreEquipmentSlotsMod`
- `DTMAPI.MoreSavesMod`
- `DTMAPI.ZoomMod`

The retained managed owner/root counters were:

- `ModOwner.records=65`
- `EventHandler=11`
- `InputButton=2`
- `ConfigMenuPage=5`
- `LoadedCodeMod=6`

`DTMAPI.DebugConsoleHost` remained as an unsupported host/API owner under `UiRuntime`; it is not the disabled `DTMAPI.DebugConsoleMod` code owner.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired and released for the rerun.
- `tools/scripts/runtime-lock-status.ps1`: free after runtime.
- No leftover `DolocTown.exe`.
- Profiles were restored after both runs.

## Classification

Removing the UI code mod roots is not proven to be a fix, because the full baseline remains intermittent. However, two clean-process passes under the Phase 8.16 diagnostic shape make UI code roots a strong suspect/amplifier for the high-pressure native LoadGame GC fatal.

The next phase should not resume UI single/pair bisection and should not claim ISSUE-010 solved. It should either confirm the no-UI-code-root signal with a narrow add-back or follow the next externally requested content/native-heavy isolation axis.

## Rollback

No runtime rollback is needed because this phase made no source or public API changes. The only new files are documentation and evidence summaries.

## Non-Changes

This phase did not:

- change public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run a broad service hard-disable matrix;
- destroy unknown native Unity objects;
- mark ISSUE-010 solved.
