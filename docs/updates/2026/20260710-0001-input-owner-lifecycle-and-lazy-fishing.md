# 20260710-0001 - Input Owner Lifecycle and Lazy Fishing

Status: source-and-unit-verified / runtime-cleanup-verified / issue-010-open

## Source Request

First phase of the user-requested AutoFishing/DTMAPI split: remove local snapshot Gen0 churn, add generic owner cleanup, generalize lifecycle aggregation, and delay fishing activation without overwriting the uncommitted `20260708-0005` baseline.

## Changed Files

- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Runtime/ModOwnerCleanup.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OwnerCleanup/`
- GameBridge product service `RemoveOwner` implementations
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `tests/DTMAPI.UnitTests/Program.cs`

## Summary

- Replaced the two rotating owner-to-`HashSet` maps with stable owner/button watches and generation-based two-frame expiry.
- Cached the active-button union and rebuild it only when membership changes; added bounded watch/cache diagnostics.
- Added isolated Core owner cleanup participants and one GameBridge dispatcher for action, animal, audio, camera, chest, crop, custom animal, equipment, fish roe, machine, save slot, strong planting gun, and fishing state.
- Converted lifecycle aggregation to caller-declared `AggregatedCurrentState`; Core no longer recognizes `AutoFishing.*` names.
- Made fishing configuration policy-only and first enable the only hook/service activation path. Disabled/inactive callbacks return immediately and F6 close does not unpatch the shared Harmony owner.

## Validation

- `$env:DOTNET_ROLL_FORWARD='Major'; tools/scripts/build.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`.
- Unit coverage proves warmed local keybind query/sample/record/clear delta is zero bytes on the test thread, stable instances, expiry, owner cleanup, no registered root, failed Entry cleanup, participant isolation, and single lazy activation.
- Fifth-save close evidence in `GAME-SMOKE/20260710-102651`, `103205`, `103350`, and `105201` records owner options/states, sessions, input leases, animation leases, native transient handles, animators, and hook physics returning to zero.

## Evidence Links

- Goal: `docs/goals/2026/20260710-0001-input-owner-lifecycle-and-lazy-fishing.md`
- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Regression row: `AUTOFISHING-PRIMITIVES-CUTOVER-20260710`
- Preserved baseline: `docs/updates/2026/20260708-0005-autofishing-ledger-lifecycle-boundary.md`

## Rollback

Revert this phase as described by its goal. Do not restore per-feature Core naming or runtime `UnpatchSelf`.

## Follow-Up

Keep ISSUE-010 open until 100/500-loop and broader gameplay evidence passes.
