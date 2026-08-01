# 20260705-0008 - Phase 8.7 UI Owner Split And PreLoad GC Probe

Date: 2026-07-05
Status: source-and-runtime-smoke-verified / issue-010-open
Area: smoke/saveload/issue-010/ui-owner/root-set

## Trigger

User requested Phase 8.7: use the existing per-owner delta diagnostics, add a smoke-only `-AutoExercisePreLoadGcProbe`, then run the fixed `CoreCustomAnimals + action/utility + Manbo audio` base with AutoFishing disabled against one-hour-title-idle UI single-owner profiles and high-value pairs.

## Summary

Added the PreLoad forced-GC probe as an opt-in smoke-only diagnostic. It records `BeforePreLoadForcedGC`, runs `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();`, then records `PreLoadForcedGC` before the first post-idle native LoadGame.

The Phase 8.7 UI split did not isolate a single UI owner or high-value pair. YConsole, MoreEquipmentSlots, MoreSaves, Zoom, and the requested pairs all passed the one-hour-title-idle two-load route with no Fatal GC, no duplicate LoadGame, and no owner root growth at `LoadGameNativeEnter` or `AfterReturnedToTitleComplete`.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260705-0003-phase87-ui-owner-split-preload-gc.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0008-phase87-ui-owner-split-preload-gc.md`
- `docs/updates/INDEX.md`

## Runtime Evidence

Short PreLoad probe validation:

- `GAME-SMOKE/20260705-101446`: `CoreOnly`, one short SaveLoad cycle, `PreLoadForcedGCProbe=Passed`.

Phase 8.7 singles:

- `GAME-SMOKE/20260705-101737`: B + YConsole, passed.
- `GAME-SMOKE/20260705-111900`: B + MoreEquipmentSlots, passed.
- `GAME-SMOKE/20260705-122003`: B + MoreSaves, passed.
- `GAME-SMOKE/20260705-132240`: B + Zoom, passed.

Phase 8.7 high-value pairs:

- `GAME-SMOKE/20260705-142438`: B + YConsole + MoreEquipmentSlots, passed.
- `GAME-SMOKE/20260705-152600`: B + YConsole + MoreSaves, passed.
- `GAME-SMOKE/20260705-162708`: B + MoreSaves + MoreEquipmentSlots, passed.

For every single and pair run:

- `RunStatus=Passed`
- `SaveLoadCycle=Passed`
- `NoFatalInstanceWindow=Passed`
- `ProcessExited=Passed`
- `requests=2`
- `nativeEnter=2`
- `nativeReturn=2`
- `saveLoaded=2`
- `duplicateRequests=0`
- `fatalWindows=0`

`official-mod-profile-summary.json` was checked for each run. Target UI IDs were enabled and AutoFishing was not enabled.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired and released for each game smoke.

## Decision

This is Phase 8.7 situation C. The tested UI single owners and high-value pairs did not isolate the crash or show owner-root growth. Stop UI guessing and escalate to full-profile native/root-set analysis.

## Rollback

Revert this update to remove the PreLoad GC smoke switch and associated docs. The runtime behavior for ordinary players is unchanged because the new GC probe only runs when the smoke settings explicitly request it.

## Follow-Up

- Reproduce the full known failing profile and collect Unity crash dump plus `Unity-Crashes/summary.txt`.
- Rerun the same failing profile once with `-AutoExercisePreLoadGcProbe` if it reproduces, to distinguish title-stable managed GC failure from native LoadGame activation.
- Compare the final `BeforeNextLoadGame`, `PreLoadForcedGC`, and `LoadGameNativeEnter` snapshots.
- Continue fixing only definite DTMAPI-owned stale roots, duplicate registrations, or stale binders.
