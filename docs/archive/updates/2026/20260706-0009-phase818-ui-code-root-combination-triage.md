# 20260706-0009 - Phase 8.18 UI Code Root Combination Triage

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / input-heavy UI pair reproduced / issue-010-open

## Source Request

External review requested Phase 8.18:

- keep the Phase 8.17 light diagnostic route;
- keep CustomAnimals, action/utility owners, and Manbo audio;
- add back UI code owners by combination;
- first run the input-heavy pair `YConsole + Zoom`;
- stop if that pair reproduced before running triples.

## Changed Files

- `docs/reviews/code/2026/20260706-0008-phase818-ui-code-root-combination-triage.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/debug/evidence/GAME-SMOKE/20260706-185612/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-185612/validation-summary.txt`

No source files were changed in this phase.

## Runtime Evidence

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-185612`

The run used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `CoreCustomAnimals`, the five non-UI base extras, and the UI pair `Workshop.3742714442` plus `Workshop.3742717440`.

It reproduced Fatal GC before SaveLoaded:

- `RunStatus=Aborted`
- `SaveLoadCycle=Failed`
- `SaveLoaded=Failed`
- `NoFatalInstanceWindow=Failed`
- `ProcessExited=Passed`
- `requests=1`
- `nativeEnter=1`
- `nativeReturn=0`
- `saveLoaded=0`
- `duplicateRequests=0`
- final breadcrumb `NativeContinuation.Step=DolocAPI.LoadGame.Enter`

## Owner / Root Result

Compared with the Phase 8.17 no-UI-code-root passes, `YConsole + Zoom` raised counters from:

- `ModOwner.records=65` to `87`
- `EventHandler=11` to `18`
- `InputButton=2` to `9`
- `ConfigMenuPage=5` to `7`
- `LoadedCodeMod=6` to `8`

MoreEquipmentSlots and MoreSaves remained disabled. `UnexpectedRemainingOwners=none`; `DTMAPI.DebugConsoleHost` remained only as the unsupported host/API owner under `UiRuntime`.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired and released.
- No leftover `DolocTown.exe`.
- Profile was restored.
- `SaveLoadObjectSnapshotMode=Lite` was verified at `BeforeNextLoadGame` and `LoadGameNativeEnter`.
- `SmokeNativeLoadContinuationProbe=VersionPatcher` was active.

## Classification

`YConsole + Zoom` is sufficient to reproduce the pre-SaveLoaded terrain/dungeon native LoadGame fatal under the Phase 8.17 route. This narrows the UI-code-root suspect from four owners to the input-heavy pair, but it does not yet prove whether YConsole alone, Zoom alone, or their combined input/config/event roots are necessary.

Next phase should split `YConsole` versus `Zoom`, or inspect their shared input/static/config roots, before service-level hard-disable.

## Rollback

No runtime rollback is needed because this phase made no source or public API changes. The only new files are documentation and evidence summaries.

## Non-Changes

This phase did not:

- change public API;
- change ordinary player runtime behavior;
- run HookProbe;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run service hard-disable;
- destroy unknown native Unity objects;
- mark ISSUE-010 solved.
