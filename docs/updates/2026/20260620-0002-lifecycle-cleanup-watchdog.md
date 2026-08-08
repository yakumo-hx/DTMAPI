# 20260620-0002 Lifecycle Cleanup Watchdog

## Summary

Implemented the first source-only long-run crash-risk mitigation pass for DTMAPI-owned lifecycle references. This update does not claim the Unity/Mono GC crash is solved; it bounds several known managed/native reference holders and adds low-frequency counters so the next player package can show whether counts grow over time. Review follow-up `20260620-0003` narrows the high-frequency `EnvironmentReset` parts of this first pass to safer lightweight paths.

## Source Request

User plan: fix the most suspicious long-running strong references without installing the game, launching Doloc Town, or writing local upload packages.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Behavior

- SaveSlots clears official save-panel pager roots and event binders at save/title boundaries; review follow-up `20260620-0003` keeps high-frequency `EnvironmentReset` non-destructive and restores active panels before cleanup.
- EquipmentSlots clears tracked cloned UI objects, event binders, rendered/evidence flags, and stale UI summaries at save/title boundaries; review follow-up `20260620-0003` keeps high-frequency `EnvironmentReset` non-destructive.
- AutoFishing records a pending-cast confirmation around `BodyController.UseFishRod`; real fishing phase or minigame progress clears it. Review follow-up `20260620-0003` moves the marker before native invocation and changes stalled casts into diagnosed short-backoff retries.
- `Runtime.LifecycleRetentionCounters` reports low-frequency counts for environment resets, feature fanout, SaveSlots UI state, EquipmentSlots UI/storage state, machine runtime entries, ActionSpeed animator cache, and AutoFishing pending/cache state; review follow-up `20260620-0003` removes the every-100-reset publication path.

## Validation

Source-only validation passed:

- `git diff --check`
- `tools/scripts/test.ps1 -Configuration Release`
- Windows PowerShell 5.1 parse of `tools/scripts/collect-logs.ps1`
- Follow-up source validation for `20260620-0003` is recorded in `docs/updates/2026/20260620-0003-lifecycle-review-safety-fixes.md`.

No game smoke was run by design. No local game runtime, official local upload package, or Workshop upload folder was modified.

## Evidence Links

- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Code review: `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`
- Smoke matrix row: `RUNTIME-LIFECYCLE-CLEANUP-WATCHDOG-20260620`
- Hook map entry: `Runtime.LifecycleRetentionCounters`

## Rollback Notes

Revert this update if the cleanup statuses or pending-cast watchdog block normal SaveSlots, EquipmentSlots, or AutoFishing behavior during manual/game validation. Prefer reverting the narrower `20260620-0003` follow-up first if the problem is specifically EnvironmentReset timing or AutoFishing backoff behavior. The log-export enhancements from `20260620-0001` are independent and should not be reverted with this change unless report collection itself regresses.

## Follow-Up

Run a long-play soak or collect a new player crash package. Only move ISSUE-010 from `open/evidence-improved` to `mitigated` or `solved` after counters remain bounded and Unity crash evidence no longer points at DTMAPI-managed lifecycle objects.
