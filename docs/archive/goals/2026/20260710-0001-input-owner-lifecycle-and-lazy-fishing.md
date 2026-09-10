# 20260710-0001 Input Owner Lifecycle and Lazy Fishing

## Summary

Replace per-owner/per-frame local input snapshot collection churn with stable generation-based watch state, add one generic Core-to-GameBridge owner cleanup boundary, and make legacy FishingAutomation activation truly lazy.

This phase must preserve input edges, public API signatures, fishing player behavior, existing shared Harmony ownership, and the uncommitted `20260708-0005` baseline.

## Scope

- Keep stable owner/button watch objects and a cached active-button union; expire unused watches within two frames.
- Expose bounded owner/watch/active/cache-rebuild/expiry diagnostics and prove a warmed local-keybind frame allocates zero bytes on the test thread.
- Add `IModOwnerCleanupParticipant` with `EntryFailed`, unload, and shutdown reasons; isolate participant failures and retain a bounded summary.
- Register one GameBridge participant that removes owner maps, callbacks, leases, arbitration state, and product state across existing services.
- Make `IFishingAutomationApi.Configure` policy-only and delay service/hook creation until `SetEnabled(true)`.
- Replace the Core AutoFishing-name ledger special case with caller-declared aggregated-current-state observation.

## Acceptance

- Local snapshot tests cover zero allocation after warm-up, stable instances, next-frame sampling, press/release, two-frame expiry, title cleanup, owner cleanup, and no registered input root.
- Failed Entry cleanup removes all participating owner state and isolates cleanup exceptions.
- Fishing `Configure` alone creates no runtime service or hook request; first enable activates once; disable never calls `UnpatchSelf`.
- Release build/unit tests and `git diff --check` pass.

## Outcome

Implemented in the current worktree. Source/unit acceptance passed. Runtime AutoFishing evidence in the later cutover phase confirms close cleanup reaches zero primitive sessions and leases. The broader ISSUE-010 long-gameplay gate remains open.

## Rollback

Revert the watch-state, cleanup-participant, aggregated-ledger, and lazy-facade changes as one phase. Do not restore a feature-name special case in Core or uninstall the shared Harmony owner on F6 disable.
