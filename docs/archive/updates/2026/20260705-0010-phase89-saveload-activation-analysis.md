# 20260705-0010 - Phase 8.9 SaveLoaded Activation Analysis

Date: 2026-07-05
Status: source-and-existing-crash-evidence-reviewed / issue-010-open
Area: core/runtime/saveload/smoke/issue-010/fatal-window

## Trigger

User requested Phase 8.9 after Phase 8.8 captured two full-profile crash packages. The requested order was to analyze `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-205236` first, audit only the SaveLoaded/native LoadGame activation chain, add lightweight breadcrumbs, and add a smoke-only object snapshot mode switch without running Lite/Off classification yet.

## Summary

Compared the two existing crash packages before starting any new long run.

Findings:

- `GAME-SMOKE/20260705-194759` fatals before `SaveLoaded`. `Player.log` points at native `LoadGame` terrain/dungeon activation.
- `GAME-SMOKE/20260705-205236` completed `PreLoadForcedGCProbe`, reached `NotifySaveLoaded`, ran public SaveLoaded handlers and queue flush, then fatals while publishing/logging the full `SaveLoaded` object snapshot.

Added SaveLoaded activation breadcrumbs that log only step, elapsed time, GC collection counters, `GC.GetTotalMemory(false)`, request id, boundary id, slot, and phase.

Added smoke-only `-SaveLoadObjectSnapshotMode Full|Lite|Off`; `Full` is the default and preserves existing behavior. `Lite` and `Off` are diagnostic switches for later classification only.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260705-0005-phase89-saveload-activation-analysis.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0010-phase89-saveload-activation-analysis.md`
- `docs/updates/INDEX.md`

## Evidence

Existing evidence reviewed:

- `docs/debug/evidence/GAME-SMOKE/20260705-194759`
  - no PreLoad GC
  - final DTMAPI position: `LoadGame requested for slot/index 2. requestId=SL-0001.`
  - Unity crash: `Crash_2026-07-05_124847467`
  - SaveLoad: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate `0`
- `docs/debug/evidence/GAME-SMOKE/20260705-205236`
  - `PreLoadForcedGCProbe=Passed`
  - final DTMAPI position: `TitleReturn object graph snapshot 17:SaveLoaded...`
  - Unity crash: `Crash_2026-07-05_135327389`
  - SaveLoad: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate `0`

Both profile summaries used `CoreCustomAnimals` plus the requested full extra IDs and kept `Local.Yuuka_DTMAPI_AutoFishing` disabled.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings from NuGet vulnerability metadata lookup.
- `git diff --check`: passed with line-ending normalization warnings only.
- No new runtime long smoke was started before the existing crash evidence analysis.

## Decision

The next investigation should not return to UI owner/pair bisection. The current fatal class has two observed windows:

- native `LoadGame` activation before `SaveLoaded`;
- SaveLoaded tail full object snapshot/log publication after native activation reaches `NotifySaveLoaded`.

The new breadcrumb is designed to identify the last completed SaveLoaded sub-step in the next fatal window without adding another heavy object enumerator.

Do not run `Lite`/`Off` long classification until a second comparable no-probe full-profile fatal is captured and the fatal window again points at SaveLoaded/snapshot. Do not start service-level hard-disable experiments until full-profile no-probe reproduction is stable enough.

## Rollback

Revert this update to remove the smoke-only `SaveLoadObjectSnapshotMode` setting and SaveLoaded breadcrumbs. Default `Full` mode preserves current behavior, so ordinary runtime behavior is unchanged unless a smoke explicitly sets `Lite` or `Off`.

## Follow-Up

- Run validation commands.
- If evidence remains insufficient, run one comparable no-probe full known-failing profile with `3600s` continuous title idle, max two loads, `-TimeoutSeconds 6000`, and `-FatalWindowCrashDumpGraceSeconds 30`.
- If that fatal again lands in SaveLoaded/snapshot, use `Lite`/`Off` only as a classifier.
- Keep native object ownership conservative: do not destroy unknown Unity/native objects, and do not change public API or GameBridge service boundaries in this phase.
