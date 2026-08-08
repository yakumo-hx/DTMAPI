# ISSUE-015: DebugConsole Compatibility Hook Transactions

## State

`verified`

The repeated Hook rebuild, non-transactional acquisition, retry-loss,
zero-update construction order and dual-owner source defects are corrected and
covered by focused tests. The current ProductNative disposable-save matrix and
exact-current Runtime/Host retained 0.3.1 `NoNativeSave` route pass.
Independent Review `20260727-0004` accepted the final lifecycle correction
with no remaining P0/P1/P2.

## Reproduction

The exact retained DebugConsole 0.3.1 route in
`GAME-SMOKE/20260727-085929` produced 804
`Harmony owner cleanup completed
owner=dtmapi.compatibility.debugconsole.legacy` entries in roughly 28 seconds.
The Compatibility UI wrote unchanged modal/drain state on ordinary frames and
the Hook owner responded by removing and reinstalling its complete topology.

Source fault injection also reproduced three unsafe transitions:

- a partial input installation could fail after the UI had acquired its modal
  token;
- a ProductNative native-restore failure marked the instance disposed before
  Core's retained retry;
- movement, time-scale or creative writes followed by failed readback and
  rollback could lose the only exact-original ledger.

The Catalog identity prevents selecting the new and old product packages
together, but a separate frozen Diagnostic API consumer can still demand the
Compatibility service. Package selection was therefore not owner mutual
exclusion.

## Known Facts And Rejected Hypotheses

- The 804 cleanups are runtime log evidence, not a theoretical allocation
  estimate.
- Harmony itself did not spontaneously rebuild the patches. Repeated
  unchanged Compatibility setter calls invoked the old `Reconcile` path.
- Moving only the UI out of mandatory Runtime did not solve action/Hook
  lifecycle ownership. The optional Host needs its own exact transition
  contract.
- Catalog uniqueness is not a sufficient dual-owner guard because frozen APIs
  are callable by other retained consumers.
- Setting `disposed=true` after a failed restore is not harmless idempotence:
  Core deliberately retains a failed lifecycle instance for a later cleanup
  opportunity.
- A native write returning successfully does not prove the mutation ledger can
  be cleared. Readback and rollback can fail independently.

## Correction

- Compatibility now stores desired demand separately from installed input and
  creative topology. Unchanged warmed frames return without patch operations,
  topology changes or status publication; modal-to-drain transfer does not
  rebuild the same three Prefixes.
- Input and creative transitions use exact-target patch/unpatch operations.
  Partial acquisition is rolled back; failed removal reconstructs the prior
  group before the failure propagates.
- A failed UI open clears visible/open state, modal ownership and desired
  suppression, then propagates the Hook error.
- The duplicate Compatibility action-service frame update was removed.
- ProductNative and Compatibility inspect the other exact Harmony owner before
  installing any target. Both installation orders fail closed for the losing
  owner.
- Product disposal becomes final only after all required restore and Hook
  cleanup operations succeed.
- Movement, time-scale and creative mutations retain original, prior-applied,
  attempted and uncertainty state until exact restoration is observed.
- The broker registers the nested `DebugActions` instance as the one action
  lifecycle alias when `DebugConsole` constructs it first. Both construction
  orders receive exactly one update/save/title/shutdown callback.
- Product SaveLoaded restores any retained prior-session lease before the new
  session becomes active.
- Combined patch and rollback-unpatch failure retains `cleanupPending`; a
  later reconcile/shutdown retries exact-owner cleanup instead of accepting
  logical/physical equality.
- Compatibility Close restores the complete visible UI, Hook demand and Core
  modal token when release fails, so retry cannot lose its cleanup handle.

## Validation

- Focused `debugconsole-product` Unit: passed.
- Stable topology fixture: 240 warmed frames, zero additional patch
  operations, topology transitions or status publications.
- Fault fixtures: partial input and creative acquisition, failed removal with
  exact prior-topology reconstruction, modal/UI release, retryable disposal,
  movement/time/creative post-write readback plus rollback failure.
- Real Harmony metadata fixture: both ProductNative-first and
  Compatibility-first exclusion pass with zero losing-owner patches.
- Current disposable player-save acceptance:
  - `20260727-104118` failed save keeps prior committed `210726`;
  - `20260727-104214` cold-observes exact `210726`;
  - `20260727-104302` saves exact new committed `211019`;
  - `20260727-104359` cold-observes exact `211019`.
- `104513` and `104654` are non-acceptance source-selection/orchestration
  attempts. The interrupted run's current/prev/bak archive metadata and hashes
  were proven unchanged before its exact QA/profile/source cleanup.
- Complete `DTMAPI.UnitTests`: passed after replacing the stale removed
  `RemoveOwnerResources` contract.
- Exact-current retained route `GAME-SMOKE/20260727-132103`: passed against
  Runtime `BuildCommit=d7db257747a2` and Compatibility Host SHA-256
  `9920410DCF153D6FACB287A9F48E159FC8A9E4BE2D08159AB5352F454612DCE9`.
  The exact old DLL and all seven action groups pass through
  `nativeOwner=Compatibility`. Eight UI open/close cycles produce sixteen
  input-topology edges; one later edge clears creative demand. Across 4,886
  action lifecycle updates there are zero owner-wide Harmony cleanup entries.
  Title UI/patch state is zero; player archives/committed sidecars are
  unchanged; QA/profile/source/Loader/process cleanup passes.
- `130236` is non-acceptance because the deliberately prepared
  `WorkshopValidation` state had no active author session. `131832` proves the
  UI-only PlayerWorkshop path and all cleanup facts but remains a red result
  because no action emitted the runner's required `nativeOwner` marker;
  `132103` supersedes it.

No complete Release, L0-L5, GC or long-duration matrix was run.

## Acceptance Criteria

Before changing this issue to `verified`:

1. satisfied by independent Review `20260727-0004`: desired/installed topology
   transaction, exact-owner arbitration and retained restore ledgers;
2. satisfied by `132103`: a current-byte retained 0.3.1/current Compatibility
   player run holds input and creative demand across warmed frames without
   repeated exact-owner cleanup/install churn;
3. satisfied by `132103`: title and Loader cleanup leave the Compatibility UI
   graph, modal/input demand and exact Harmony owner at zero;
4. satisfied by `132103`: no player save or committed sidecar changes in the
   `NoNativeSave` run.

## Related Records

- `docs/updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md`
- `docs/reviews/code/2026/20260727-0004-lifecycle-closeout-product-inventory-release-route-audit.md`
- `docs/reviews/code/2026/20260727-0002-mine-and-debugconsole-fix-recheck.md`
- `docs/hook-map/focused/DebugConsoleInput.md`
- `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`
- `docs/debug/regressions/smoke-matrix.md`

## 2026-08-01 movement-owner correction

The earlier exact-original movement-ledger result is superseded for current
source. Native review proved `MotionAbility.MoveScaler` is a shared Buff
aggregate, so exact value restoration cannot establish contribution ownership
and an ABA sequence can still make a stale snapshot unsafe.

Current ProductNative and the frozen Compatibility action backend no longer
call `SetMoveScaler`. They share one guarded current-player
`BodyController.get_MoveSpeed` Postfix under the already mutually exclusive
Harmony owners. Compatibility treats it as a separate one-patch transactional
demand group; cleanup-pending recovery includes that group and resets the
in-memory factor. Time-scale and creative exact-original ledgers remain
unchanged.

Focused Unit and the real-Harmony metadata fixture pass source-level and
physical-owner checks. Final player QA then confirmed that 2x/3x/4x remains
effective after closing Y and that 1x restores normal movement without removing
the native Buff. Both latest Runtime sessions contain zero Warning and zero
Error/Fatal records. Exact accepted package and upload evidence is linked by
Update `20260801-0002`; this append does not rewrite the valid 2026-07-27
input/creative transaction evidence.
