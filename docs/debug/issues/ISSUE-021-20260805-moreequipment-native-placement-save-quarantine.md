# ISSUE-021: MoreEquipment native placement outcome can escape save quarantine

## Status

- State: `verified`
- Opened: `2026-08-05`
- Severity: high
- Area: MoreEquipmentSlots / Compatibility Host / save commit / owner recovery
- Related review: `docs/reviews/code/2026/20260805-0004-dtmapi-060-eighth-five-slice-parallel-review.md`
- Owning update: `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`

## Symptom And Risk

During Product-absent cold recovery, backpack or mail placement can mutate native
state and then throw, or its immediate readback can fail. The earlier Product
cold path registered the in-memory recovery session only after all placement
calls returned. That left a window in which the durable journal recorded an
attempt but no session remained to veto `SaveSaving`; a later native save could
commit an outcome whose exact destination/count was unknown.

The old game-smoke route could also report recovery success after one
`NoNativeSave` process by observing transient placement logs and deleting its
synthetic sidecar. It did not prove a normal native `SaveGame`, `SaveSaved`,
journal removal, or a second cold process with no replay.

## Known Facts

- This is an `OwnerRecovery` / `OrphanRecovery` transaction, not ordinary
  gameplay mutation.
- A native call returning or throwing is not authoritative if the native state
  may already have changed; exact immediate post-attempt evidence owns the
  placement result.
- A durable prepared/started journal with any incomplete or `Failure` escrow
  must keep the save quarantined. Delayed count-only evidence cannot promote it.
- The Product-v3 sidecar and native save are separate stores. A real native
  save plus `SaveSaved` is required before successful recovery may clear the
  journal.
- No game process was run for the 2026-08-05 source correction. The bounded
  disposable runtime acceptance later passed on 2026-08-06.

## Source Mitigation

- The Product cold-recovery session is registered immediately after the
  prepared journal is durably written, before any native placement attempt.
- Placement uses the shared immediate-evidence operation. Mutation-then-throw
  and unreadable immediate observation retain one outcome-unknown quarantine
  session instead of retrying or inferring success.
- `SaveSaving` persists current evidence and rejects every session whose
  attempt is not started or whose escrow is incomplete/`Failure`.
- `SaveSaved` does not promote or clear incomplete/`Failure` recovery.
- Focused physical Host/Product tests cover backpack and mail mutation-then-
  throw with both exact immediate resolution and unreadable immediate evidence.
- `-AssertMoreEquipmentSlotsColdRecovery` now fails before package staging,
  sidecar writes or game launch. The former one-process result cannot be
  emitted as acceptance evidence.
- The replacement source route is now an explicit three-process transaction:
  `ColdPrepare` performs a normal native sleep save and only then writes one
  exact-scope Product-v3 seed; `ColdCommit` starts cold, observes the prepared
  recovery session and exact native destination, then uses another normal
  native sleep save to finalize the journal; `ColdObserve` starts cold under
  `NoNativeSave` and proves the terminal sidecar/native count cannot replay.
- The seed scope is read from the active disposable archive, including
  `archiveIndex`, player identity and `totalGameSeconds`; PowerShell no longer
  guesses identity or writes/deletes individual synthetic sidecar files.
- `run-moreequipment-cold-recovery-acceptance.ps1` owns one shared Runtime lock,
  retains the marked fixture across the first two successful phases, retains
  it on any failure, and permits the existing whole-fixture cleanup only after
  the final `NoNativeSave` archive/committed-sidecar unchanged gates pass.
- Source/QA builds, QA Unit, and the non-launch three-phase routing/validation
  matrix pass. The runner now permits exactly one foreground-verified Enter
  attempt, requires a `SaveSaving` receipt after that attempt, and
  shares one phase deadline across transition and title cleanup.

## Runtime Verification

The accepted outer receipt is retained as
`docs/debug/evidence/GAME-SMOKE/20260806-015320/cold-recovery-acceptance.json`.
It ran the same marked AutoCloud-isolated fixture through three independent
processes against installed Runtime `02e186cfa8eb` and the exact seven-file
MoreEquipmentSlots 1.0 candidate:

1. `GAME-SMOKE/20260806-015140` passed `ColdPrepare / NativeSaveExpected`.
   One provenance-checked Enter produced `SaveSaving` and `SaveSaved`; the
   disposable backpack was normalized to one free slot before the exact-scope
   Product-v3 seed was published.
2. `GAME-SMOKE/20260806-015232` passed `ColdCommit / NativeSaveExpected` with
   the Product disabled. Demand, Compatibility Host and native destination
   gates passed; exactly one `grandmas_button` was recovered to the backpack,
   one prepared recovery session quarantined it until a second real native
   save, and the terminal document/session were then empty.
3. `GAME-SMOKE/20260806-015320` passed `ColdObserve / NoNativeSave`. Native
   count remained exact, terminal Product-v3 authority stayed empty, recovery
   session/replay count stayed zero, and current/prev/bak plus committed
   sidecars were unchanged before cleanup. No routine archive backup or player
   writeback occurred; profile, QA, process and fatal-window gates passed, then
   the marked fixture root was removed.

Setup/failure evidence remains intentionally non-acceptance: `r1` rejected an
old Runtime manifest and pre-existing Product state; `r2` exposed an
over-constrained backpack precondition; `r3` exposed an exact-clock QA
false-negative after production recovery had succeeded; `r4` recorded one
foreground `SendInput` that the game did not consume and no native save. Those
findings produced the bounded fixture and runner corrections; none is spliced
into the final `r5` PASS.

The existing physical Host/Product fault tests continue to prove backpack and
mail mutation-then-throw/readback-failure quarantine, save veto and retained
recoverable evidence. Together with the clean from-start `r5` success path,
the issue-specific acceptance is verified. MoreEquipmentSlots 1.0 packaging,
the broader product matrix and publication remain separate release gates.

## Rejected Approaches

- Treating a successful native Boolean as proof of exact placement.
- Retrying an outcome-unknown call and risking duplication.
- Accepting a delayed same-item count delta as evidence for the original call.
- Deleting a synthetic sidecar after transient logs without a native commit.
- Testing startup repair against the player's live Steam AutoCloud save.

## Accepted Boundary

The accepted boundary remains the following regression contract:

1. `ColdPrepare` uses `NativeSaveExpected` to normalize and commit one free
   native backpack slot, then stages one exact-scope Product-v3
   `grandmas_button` in the retained fixture;
2. `ColdCommit` starts independently with the Product disabled, proves exactly
   one native backpack placement plus one quarantined Product recovery session,
   completes a normal `SaveGame`/`SaveSaved`, and proves terminal journal
   cleanup;
3. `ColdObserve` starts independently under `NoNativeSave`, proves the exact
   same native count, empty terminal Product-v3 authority, zero recovery
   sessions and no replay, then proves the current/prev/bak family and committed
   sidecars unchanged before cleanup;
4. focused physical fault cases prove backpack and mail mutation-then-throw/readback failure
   veto save and retain recoverable evidence without duplication;
5. only after all three processes and exact process exit may the owned fixture be
   removed.

This boundary passed on 2026-08-06, so the issue is `verified`. It is not a
claim that MoreEquipmentSlots 1.0 has been published.
