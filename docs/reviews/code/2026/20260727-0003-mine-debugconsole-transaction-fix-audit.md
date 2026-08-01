# Mine And DebugConsole Transaction Fix Audit

**Date:** 2026-07-27
**Status:** recorded — Mine source correction accepted; Mine package and
DebugConsole independent admission remain open
**Reviewed HEAD:** `4a7a6a79`
**Reviewed commits:** `9b4b6152`, `4a7a6a79`
**Source Review:** `20260727-0002-mine-and-debugconsole-fix-recheck.md`
**Owning Updates:** `20260726-0004-mine-eleventh-advanced-product.md`,
`20260726-0005-debugconsole-twelfth-advanced-product.md`

## Scope And Decision

This is the requested independent recheck after the second Mine and
DebugConsole correction. It reviews source, focused tests, built artifacts and
the existing game evidence. It does not implement a correction, launch the
game, run a complete Release, or authorize a later product.

Mine's two requested source/coverage corrections are complete. Its production
lifecycle is tested through activation failure, rollback failure, blocked
reactivation, truthful `cleanup-failed`, and a later exact cleanup retry.
However, the SDK product DLL and package still contain the pre-correction
bytes, so Mine remains `implemented/open` until they are rebuilt and checked.

DebugConsole closes the ordinary steady-state churn and several first-order
failure paths, but it still has four P1 lifecycle defects. The current
`implemented/open` state is therefore correct. The current-byte retained 0.3.1
Compatibility acceptance must wait until these source findings are corrected.

## Mine Source Correction Accepted

`products/first-party/Mine/src/Native/MineNativeRuntime.cs:130-145` preserves
`cleanup-failed` while `PendingNativeRestoreCount > 0`.

The focused fixture at
`tests/DTMAPI.UnitTests/Fixtures/MineHarmonyOwnerFixture/Program.cs:300-383`
uses the real linked Mine runtime and executes:

```text
InitializeAtEntry
-> SaveLoaded / ActivateRuntime
-> activation and exact-restore failure
-> attempted reactivation rejected before new Hook/native work
-> title cleanup still reports failure
-> configuration refresh preserves cleanup-failed
-> later cleanup restores the exact original and reaches zero
```

This closes the two Mine P2 findings from Review `20260727-0002`. No repeat of
the already successful Mine game behavior matrix is needed for the source
change.

### P1 acceptance blocker — deployable Mine bytes are still pre-fix

The current author output, installed product and
`temp/batch6-mine-advanced-pilot/DTMAPI-Mine-advanced-pilot.zip` still contain:

- length `86,016`;
- SHA-256
  `3E396C9ED6DBA919E81A519756AA0C3294D4EF7307CCD7B945D5A482A5479774`;
- a build timestamp before commits `11f18098` and `9b4b6152`.

The focused fixture links current source, so its PASS does not update the
player DLL. Rebuild and validate/pack the Mine product through the existing
Author SDK, then run the existing focused package/Catalog checks. This needs
no new Mine game run.

## DebugConsole P1 Findings

### P1 — removing the duplicate update creates a zero-update service path

`DebugConsoleCompatibilityService.Update` no longer calls
`actionService.Update`, but the proposed GameBridge owner does not necessarily
know that the nested action service exists:

- `CompatibilityHostFactory.cs:32-43,75-80,94-98` creates
  `DebugActions` while constructing `DebugConsole`, but records only the
  requested `DebugConsole` service key;
- `CompatibilityHostBroker.cs:90-109,130-134` independently records only the
  requested service key;
- `DebugActionCompatibilityProxy.UpdateIfLoaded` and
  `ResetForSaveBoundaryIfLoaded` look only for the outer `DebugActions` key;
- `DebugConsoleCompatibilityService.cs:86-99` now updates and resets only the
  UI.

An old consumer which calls `IDebugConsoleApi.Bind` without separately
demanding one of the seven action proxies gets a real nested
`DebugConsoleNativeActions`, but that instance receives zero frame updates and
zero SaveLoaded restoration. Movement owner rebinding stops, and a failed
title restoration can cross into the next save.

Define one observable action lifecycle owner/alias and prove behavior for both
`DebugConsole-first` and `DebugActions-first` construction orders. The source
string assertion at `tests/DTMAPI.UnitTests/Program.cs:6305-6312` currently
protects only the absence of one call site; replace it with an exactly-once
behavior test.

### P1 — ProductNative also admits a new save before retrying restoration

`products/first-party/DebugConsole/src/ModEntry.cs:110-120` sets
`inSave=true` and resets only the UI during SaveLoaded. If
`OnReturnedToTitle` retained a failed movement, time-scale or creative restore,
the new session starts with the old ledger and native state.

SaveLoaded must retry the retained exact restoration before publishing
`inSave=true`; failure must keep the product fail-closed. Cover:

```text
ReturnedToTitle restore failure
-> SaveLoaded retry
-> no in-save activation before success
-> exact restoration and zero ledger
```

The same requirement applies to the Compatibility action instance; its
existing `CompatibilityDebugActionService.ResetForSaveBoundary` method is
currently unreachable from the `DebugConsole-first` service path.

### P1 — rollback failure can leave a Harmony patch with no cleanup tombstone

The ordinary `patch failure -> rollback success` path is corrected. A combined
failure remains unsafe:

```text
partial InstallGroup
-> CleanupPartialGroup unpatch also fails
-> logical installed/demand state returns to false
-> exact Harmony owner patch remains
-> later Shutdown(false,false,false) exits on equality without observing it
```

Relevant paths are
`CompatibilityDebugConsoleInputHooks.cs:204-239,342-417,500-543`.
Retain an explicit cleanup-pending state or force exact-owner observation at
lifecycle cleanup boundaries. Add one combined patch/unpatch failure test and
prove a later retry reaches owner zero without reintroducing per-frame
Harmony queries.

### P1 — failed Compatibility Close can retain an invisible Core modal token

`products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs:274-303` sets
`IsOpen=false` before removing the modal Hook topology. If
`runtime.ModalOpen=false` throws, execution never reaches `runtime.UI.Close`.
The next Update may remove the Hooks, but its closed path does not release the
Core custom-menu token.

The resulting state can be:

```text
UI IsOpen=false / Canvas hidden / Hooks zero / Core modal still owned
```

Other menus then remain blocked. Either restore the complete open state on
failure or retain a close-pending transaction that always releases the modal
token after successful retry. Test through `DebugConsoleUi.Close`, not only by
calling the Hook adapter directly.

## Other Acceptance Blockers

### P1 — the complete Unit entry point is red

A complete `DTMAPI.UnitTests` run reaches
`GameBridgeOwnerRetainingServicesExposeCleanupBoundary` and fails at
`tests/DTMAPI.UnitTests/Program.cs:6471`. The assertion still requires
`DolocTownExperimentalBridgeApi.RemoveOwnerResources`, which was removed with
the product-owned Machine/Debug executor split.

This appears to be a stale test contract rather than a runtime regression, but
the normal Unit authority is nevertheless red and later tests were not
reached. Remove or replace the obsolete Machine/equipment assertion, pass its
focused replacement, then run the complete Unit entry point once against the
final source candidate.

### P1 — corrected old Compatibility bytes have no accepted player run

`GAME-SMOKE/20260727-104513` selected the local ProductNative route and kept
Compatibility dormant. `104654` selected the retained source but timed out
after READY and the first Y tap; it has no accepted result, Doctor, title or
Loader cleanup evidence and did not show the legacy DebugConsole owner
activating.

ISSUE-015=`mitigated` and the active smoke row=`partial` are therefore honest.
After the source blockers above are fixed, freeze/rebuild one exact HEAD
candidate and run only one retained 0.3.1 `NoNativeSave` confirmation for
warmed zero churn, title/Loader zero and unchanged player saves.

## Accepted DebugConsole Corrections

- warmed 240-frame setter traffic performs no repeated Hook operation,
  topology transition or status publication;
- modal-to-Escape-drain handoff preserves the three-Prefix topology;
- ordinary partial installation with successful rollback reaches zero;
- `disposed=true` is committed only after Product cleanup succeeds;
- first-acquisition movement/time/creative post-write failures retain an
  uncertainty-aware exact-original ledger and can retry;
- real Harmony metadata tests prove ProductNative/Compatibility owner
  exclusion in both installation orders;
- disposable evidence `104118 -> 104214` proves rejected money Working state
  cold-reloads the prior committed value, and `104302 -> 104359` proves a
  successful save cold-reloads the exact new value.

The four save runs are valid for the money mutation they exercised; they do
not prove every DebugConsole action independently.

## P2 Documentation And Boundary Debt

1. The retained 0.3.1 hash was “corrected” to another wrong value in the
   DebugConsole Update and Review. The Catalog, ABI harness and evidence agree
   on
   `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`.
2. DebugConsole Update/monthly metadata says Runtime Validation=`passed`,
   while the canonical smoke row is `partial` and ISSUE-015 remains
   `mitigated`. Use `partial` until the required current-byte old route passes.
3. Review `20260727-0002` received a long implementation/validation resolution
   section. Document governance permits only a short resolution link; the
   Update and ISSUE already own those facts.
4. The Compatibility project still directly compiles current DebugConsole
   product UI/action sources. A later 1.0 rewrite can silently alter the frozen
   0.3.1 Host; this remains a pre-rewrite boundary task.

## Validation Performed

- repository-local .NET 8 Release Unit build: passed with 0 errors and 10
  existing nullable warnings in linked DebugConsole sources;
- focused `mine-product`: passed;
- focused `debugconsole-product`: passed;
- focused `compatibility-host`: passed;
- game-smoke save-mode source tests: passed;
- QA Release build: passed with 0 warnings and 0 errors;
- Product Catalog: passed
  (`27 products / 11 public / 21 Workshop items / 48 API rows`);
- document governance: passed (`6007` checks);
- `git diff --check`: passed;
- complete Unit entry point: failed at the stale
  `RemoveOwnerResources` assertion described above.

No game was launched for this audit. No complete Release, L0-L5, GC or
long-duration test was run.

## Smallest Next Route

1. Correct the four DebugConsole P1 lifecycle defects and replace the
   source-string update assertion with construction-order behavior tests.
2. Correct the stale full-Unit Machine cleanup assertion; pass the affected
   focused gates, then run the complete Unit entry point once.
3. Rebuild/validate/pack Mine from current source and run package/Catalog
   checks; do not repeat its game matrix.
4. Correct the hash and `partial` metadata, freeze one exact-HEAD Runtime/Host
   candidate, and bind its bytes/provenance.
5. Run one smallest retained 0.3.1 Compatibility `NoNativeSave` acceptance.
6. Perform one final independent acceptance review. Until it passes, Mine,
   DebugConsole and every later product remain blocked.

## Resolution

Implementation and runtime evidence are owned by Updates
[`20260726-0004`](../../updates/2026/20260726-0004-mine-eleventh-advanced-product.md)
and
[`20260726-0005`](../../updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md).
ISSUE-015 owns the recurring lifecycle disposition; a fresh independent
acceptance review remains open.
