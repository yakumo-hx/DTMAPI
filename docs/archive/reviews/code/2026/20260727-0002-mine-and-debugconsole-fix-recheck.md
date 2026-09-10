# Mine And DebugConsole Fix Recheck

**Date:** 2026-07-27  
**Status:** recorded — requested Mine and DebugConsole corrections implemented;
independent DebugConsole admission review remains pending
**Reviewed commits:** `11f18098`, `b947d096`  
**Source review:** `20260727-0001-mine-fixes-and-debugconsole-split-audit.md`  
**Owning Updates:** `20260726-0004-mine-eleventh-advanced-product.md`,
`20260726-0005-debugconsole-twelfth-advanced-product.md`

## Scope And Conclusion

This is the requested independent code recheck after the findings in Review
`20260727-0001` were corrected. It does not implement another fix, launch the
game, run a complete Release, or authorize a thirteenth product.

The corrections are not cosmetic:

- Mine now retains only failed native restore closures for an exact retry.
- DebugConsole's seven old action executors have physically left mandatory
  GameBridge.
- normal title cleanup now destroys the DebugConsole owner graph;
- movement, time-scale and creative changes capture their native originals;
- `Save here` now requires two clicks and the isolated native-save evidence is
  present;
- the exact retained 0.3.1 DLL/current Host run now exercises the Compatibility
  route.

Mine has no remaining P0/P1 found in this recheck, but two small truth/coverage
items keep it honestly `implemented/open`.

DebugConsole still cannot pass independent acceptance. Its newly moved
Compatibility Hook owner rebuilds Harmony patches on ordinary frames, its
failure transitions are not atomic, and ProductNative cleanup can discard the
only useful retry by marking a failed instance disposed. The two green game
runs cover the successful ProductNative and Compatibility routes separately;
they do not exercise these failure and simultaneous-owner paths.

## Mine Recheck

### Corrected — failed native restores remain retryable

`products/first-party/Mine/src/Native/MineMutationTransaction.cs:95-132`
now walks the restore order in reverse and removes an entry only after its
restore succeeds. A failed entry stays in both the dictionary and order list,
and the aggregate error reports how many entries remain queued.

`products/first-party/Mine/src/Native/MineNativeRuntime.cs:237-240` calls
`DeactivateSession` before installing Hooks or applying another native
mutation. `DeactivateSession` at `:318-334` propagates an unresolved restore,
so a failed old mutation cannot silently become the next activation's
"original".

The focused fixture proves:

- an already successful restore is not replayed;
- the failed restore remains queued;
- a later retry restores the exact original and reaches zero.

The original P1 is therefore closed in source and focused Unit coverage. No
repeat of the successful Mine game matrix is needed for this correction.

### P2 — the fixture stops short of the complete activation chain

`tests/DTMAPI.UnitTests/Fixtures/MineHarmonyOwnerFixture/Program.cs:247-296`
registers two restore closures, mutates their values, and calls
`DeactivateSession` directly. It does not execute the complete promised chain:

```text
ActivateRuntime
-> rollback restore failure
-> attempted reactivation is rejected before Hook/native mutation
-> later cleanup retries only the unresolved original
```

Static inspection shows the current implementation should reject that
reactivation, so this is an acceptance-coverage gap rather than an observed
runtime defect. One focused fault-injection Unit is sufficient; another game
run is not.

### P2 — a later configuration write can hide cleanup-failed status

If title cleanup failed while `saveActive=false`, applying an enabled
configuration reaches
`products/first-party/Mine/src/Native/MineNativeRuntime.cs:130-137` and
overwrites `cleanup-failed` with `waiting-for-save`.

The pending count remains visible in the status summary, and the next
`SaveLoaded` still retries before activation, so this does not wrongly install
Hooks or mutate native state. It is nevertheless less truthful than the Mine
Hook authority's promise that unresolved cleanup remains visibly failed.
Preserve `cleanup-failed` whenever `PendingNativeRestoreCount > 0`.

The two older Mine Reviews also still contain long implementation/acceptance
narratives. Per document governance they should eventually retain only short
resolution links to the Mine Update. This is documentation debt, not another
runtime gate.

## DebugConsole Corrections Accepted

The following findings from Review `20260727-0001` are genuinely corrected:

1. `DolocTownExperimentalBridgeApi.Diagnostics.cs` and its 2,698-line action
   body are gone from mandatory GameBridge. The mandatory surface is now the
   thin `CompatibilityHost/DebugActionCompatibilityProxy.cs`; the unrelated
   mail implementation is isolated in `DolocTownExperimentalBridgeApi.Mail.cs`.
2. `DebugConsoleUi.ResetForTitleBoundary` now calls
   `ReleaseOwnerGraph("ReturnedToTitle")`. The successful ProductNative and
   Compatibility evidence both record zero Canvas, EventSystem, buttons,
   inputs, listeners, binders and root before Loader cleanup.
3. movement and time-scale leases capture exact native original values, while
   creative mode snapshots all three original flags. Ordinary cleanup protects
   a detected foreign mutation instead of blindly writing zero/one.
4. `SaveHere` arms an eight-second confirmation on the first click and does
   not call `SaveGame` until a second click. The isolated
   `NativeSaveExpected` run covers no-call first click, injected rejection,
   re-confirmation and one successful native save. This accepts the
   confirmation correction only; it does not close the full persistence
   matrix described below.
5. the ABI harness now includes the exact retained Y-console DLL and its
   observed MemberRefs, and all eight Diagnostic interfaces declare a frozen
   Diagnostic disposition.

These results establish a real physical split and successful-path behavior.
They do not close the failure-path findings below.

## DebugConsole Blocking Findings

### P1 — the Compatibility UI rebuilds Harmony topology on ordinary frames

The old UI is updated from
`src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:667-677`. During every update,
`products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs:482,491-492`
writes the drain/modal desired state even when the value did not change.

The Compatibility adapter forwards every write directly:

- `CompatibilityDebugConsoleRuntimeAdapter.cs:47-55`;
- `CompatibilityDebugConsoleInputHooks.cs:58-75`.

Every setter calls `Reconcile`. `Reconcile` at
`CompatibilityDebugConsoleInputHooks.cs:84-121` unpatches all owned patches
and then installs the requested 3 input and/or 15 creative patches again.

The existing Compatibility run already proves that this is a real runtime
defect, not only a static risk. In `GAME-SMOKE/20260727-085929`, roughly 28
seconds of the exact retained 0.3.1 route produced 804
`Harmony owner cleanup completed
owner=dtmapi.compatibility.debugconsole.legacy` entries.

Consequences:

- closed legacy UI repeatedly publishes/reconciles unchanged zero state;
- an open legacy UI repeatedly unpatches and reinstalls its three input
  Prefixes several times per frame;
- creative mode can repeatedly rebuild all eighteen patches;
- a top-level `Passed` smoke result can mask sustained patch, log, allocation
  and status-publication churn unless the steady-state log is inspected.

This is exactly the type of avoidable Unity/Mono pressure the lightweight
boundary work is meant to remove. Cache desired and installed topology, do
nothing when both are unchanged, and change only the affected topology on a
real edge. Add a warmed multi-frame Unit asserting zero patch operations and
zero topology/status changes after the first stable frame.

The same loaded Compatibility action service is also updated once through
GameBridge (`DolocTownGameBridge.cs:256-271`) and once through the UI service
(`DebugConsoleCompatibilityService.cs:86-90`). It should have one frame owner,
not two.

### P1 — Compatibility Hook acquisition is not atomic

The Compatibility setters update `ModalOpen`, `NativeInputDrainActive`, or
`creativeEnabled` before `Reconcile` succeeds. On an installation failure,
`Reconcile` removes partial patches and clears its count, but it does not
restore the previous desired state.

For the UI path this happens after `DebugConsoleUi.Open` has acquired the modal
token and set `IsOpen=true`. A failed three-Prefix install can therefore leave:

```text
UI open / modal token held / desired suppression true / actual patches zero
```

Make desired-state plus installed-topology transition transactional. A failed
install must restore the prior desired/topology state and make the caller
release any newly acquired modal/input state. Cover both input and creative
partial-install failures.

### P1 — failed ProductNative restoration loses its Loader retry

`products/first-party/DebugConsole/src/ModEntry.cs:92-107` calls `Cleanup`,
collects failures, but sets `disposed=true` before throwing them.
`Cleanup` at `:270-272` also continues to unpatch after
`RestoreTransientState` fails.

Core deliberately retains a lifecycle instance whose `Dispose` failed so a
later owner-deactivation pass can retry idempotent native cleanup
(`DtmApiRuntime.cs:3823-3839`). DebugConsole defeats that contract: its next
`Dispose` immediately returns because `disposed` is already true. A movement,
time-scale or creative value can remain modified after the only tracked
instance has become a no-op cleanup root.

Set `disposed=true` only after every required restoration and exact-owner
cleanup succeeds. Preserve a retryable instance/status on failure and test
`Dispose failure -> second Dispose exact restoration -> zero`.

### P1 — write/readback failure can corrupt the lease ledger

The normal original-value paths are improved, but several post-write failure
paths still cannot distinguish their own partial mutation from a foreign
mutation:

- time-scale acquisition marks the lease active before the write, but a
  successful write followed by failed/mismatched readback leaves
  `timeScaleAppliedMultiplier` stale;
- movement acquisition attempts to roll back after failed readback but ignores
  whether rollback succeeded before returning without an active ledger entry;
- movement update writes the new value before readback but leaves the previous
  applied marker on failure;
- creative rollback writes are neither fully verified nor retained as a
  partial-write state.

A later cleanup can then reject the product's own write as foreign, or have no
entry with which to retry it. Each mutation needs a small
original/attempted/observed/restore-pending transaction, including injected
write-success/read-failure and rollback-failure Units.

### P1 — ProductNative and Compatibility Harmony owners are not mutually exclusive

The exact retained 0.3.1 product and the new product share one Catalog identity,
so the two published product packages are not selected together. The frozen
Diagnostic APIs remain callable by other old consumers, however, and can load
the Compatibility action/UI service while the new product is already active.

`DebugConsoleHookInstaller.ThrowIfOwnerPresent` at
`products/first-party/DebugConsole/src/Native/DebugConsoleHookInstaller.cs:171-183`
checks only its own ProductNative owner. Compatibility `Reconcile` checks no
ProductNative owner before patching. Therefore either order can place both
`dtmapi.mod.dtmapi.debugconsolemod` and
`dtmapi.compatibility.debugconsole.legacy` on the same input/creative targets.

Add explicit both-order arbitration before physical installation and a focused
test proving rejection leaves zero patches for the losing owner. The two
separate game runs do not cover this combination.

### P1 — the native-save evidence does not close Working/Committed semantics

`GAME-SMOKE/20260727-090702` proves the two-click UI contract, propagation of an
injected native rejection, re-confirmation, and one successful `SaveGame`
invocation in a disposable fixture. It does not prove either of the
persistence rows required by the admission Review:

- a successful save followed by cold reload produces the exact committed
  DebugConsole result;
- a Working mutation followed by save failure preserves the previous committed
  state.

The run is valid for the technical paths it actually exercised, but it cannot
close the complete `Working -> Committed` and rollback matrix. Add those two
isolated assertions to the smallest disposable-fixture acceptance; do not use
or write back a player's live save.

## Remaining P2 Boundaries

1. `DTMAPI.GameBridge.DolocTown.Compatibility.csproj:9-18` directly compiles the
   current product UI and native-action source. A future 1.0 UI/action rewrite
   would silently change the supposedly frozen 0.3.1 Host. Create a
   Compatibility-owned snapshot or a deliberately versioned stable internal
   source boundary before that rewrite.
2. The new product installs all eighteen patches at Entry and permanently
   subscribes `UpdateTicked`. While closed, its first save frame still reaches
   `ApplyVisibilityState -> EnsureInitialized` and creates a hidden Canvas.
   This is not a default-Runtime regression when the product is absent, but it
   remains enabled-product work and should be measured/reduced separately.
3. `ProductRuntimeAdapter` reflects three concrete Core modal method names.
   This is a version-internal coupling and must not be presented as a reusable
   public mod seam.
4. The DebugConsole Update records the retained DLL hash as
   `E5A34963A5C73...`; the Catalog, ABI harness and actual evidence agree on
   `E5A34963C0B66...`. Correct the Update typo when implementation resumes.

## Resolution

Implementation lifecycle and validation are owned by Updates
[`20260726-0004`](../../../updates/2026/20260726-0004-mine-eleventh-advanced-product.md)
and
[`20260726-0005`](../../../updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md).
The later independent transaction audit is Review
[`20260727-0003`](20260727-0003-mine-debugconsole-transaction-fix-audit.md).
