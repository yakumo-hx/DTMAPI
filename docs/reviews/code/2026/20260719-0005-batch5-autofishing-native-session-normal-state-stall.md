# 20260719-0005 - Batch 5 AutoFishing Native Session Normal-State Stall

- Date: 2026-07-19
- Status: corrective implementation complete; AutoFishing ladder replay required
- Severity: P1 acceptance-fixture blocker; no product crash, save corruption, source-tree drift, or GC terminal result observed
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Failed ladder: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-044157-70ce39e6`
- Failed stage: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-044157-70ce39e6/AutoFishing-L0/stage.json`
- Failed smoke: `docs/debug/evidence/GAME-SMOKE/20260719-090212`

## Observed Result

The formal `Both`, 600-second Batch 5 ladder completed all 24 ActionSpeed stages before it reached `AutoFishing-L0`. Every retained ActionSpeed stage has `SmokeExitCode=0`, a completed runtime terminal receipt, all eleven required metric categories, the expected independent L0-L5 behavior identity, an exact OfficialLocal load receipt, and an exact Author source-state and product-tree restore receipt.

`AutoFishing-L0` loaded the exact frozen local product tree and reached the third save. The Runtime log records `SaveLoaded`, installation of the demanded fishing hooks, and acquisition of the QA-owned first-party native-control session at `09:02:58`. It then repeatedly records only `Waiting for NormalGameState before Batch 5 native-control fishing. context=Gameplay` at approximately `09:12`, `09:21`, and `09:31`. The stage reached the 1800-second outer limit without a runtime metrics file or terminal behavior receipt. The smoke therefore failed closed with `QaHostLifecycle=Failed`, `OwnerLifetimeCloseCleanup=Failed`, and no QA `Closed` marker. The runner forcibly closed the process and did not promote the stage.

This is not a GC-gradient result. The fixture never entered its five-fish warmup, ten-fish target, or 600-second measurement window, so no memory slope may be inferred from the failed stage.

## Root Cause

`TryExerciseAutoFishingNativeControlCore` checks `DolocAPI.IsNormalState` before it inspects whether an existing `FishingNativeControlQaSession` is already active. On the first playable update it may acquire the session and begin native fishing. Native fishing deliberately leaves `NormalGameState` while cast, bite, visible-reel, pull, and exit phases run. On subsequent fixture updates the unconditional normal-state check returns `Pending` before `GetSnapshot`, `TryReelVisible`, `PullExited`, retry, performance observation, and session cleanup can advance.

The state machine therefore treats the state produced by its own native-control transaction as an unmet entry precondition. The log sequence—session acquired once, followed only by repeated normal-state waits and no terminal receipt—matches this control flow exactly.

`TryAdvanceAutoFishingBatch5DisableRecovery` contains the same ordering defect: it checks `IsNormalState` before advancing an already acquired recovery session. Without correction, L4 could stall after its recovery cast even if L0-L3 passed.

L5 does not reuse this QA native-control core. Its selected route enters `AutoFishingPrimitiveFixtureCase.TryExerciseAutoFishingPrimitiveCore`, while the enabled first-party product advances its own snapshot/reel state machine from `ModEntry.Tick`. The ordinary observation gate later in `AutoFishingFixtureCase` may delay QA observation while the native state is non-normal, but accumulated primitive diagnostics remain observable after the product returns to normal. The retained failure does not establish an L5 deadlock, and this review does not classify one.

The single Input System frame-driver stall and missing-frame fallback warning happened before the third-save workload was established and recovered through the tracked PlayerLoop installation. They do not explain the deterministic post-session normal-state wait and are not the primary cause of this stage failure.

## Required Correction

1. Require `NormalGameState` only when no live QA native-control session exists and a new native transaction is about to be acquired or cast.
2. Once a session is live, always allow its snapshot/reel/pull/interrupt/cleanup state machine to advance while the native game is in fishing states. Do not loosen the initial playable-state gate for acquiring a new session.
3. Apply the same session-aware gate to the L4 disable/recovery state machine.
4. Add deterministic source/unit coverage proving that:
   - no session plus non-normal state remains pending and performs no acquisition/action;
   - a live session plus non-normal state reaches snapshot/reel/pull handling instead of returning at the entry gate;
   - L4 recovery has the same live-session exception;
   - the entry gate is restored after a session is released, so title/loading/menu states cannot acquire a new native session.
5. Add a bounded in-fixture progress timeout with the last session phase/sequence in its failure receipt so a future state-machine regression fails diagnostically before the 1800-second outer timeout.

## Corrective Implementation

The QA fixture now delegates entry/action decisions to a testable session-aware policy. A non-normal state without a live session remains blocked, and every new cast still requires `NormalGameState`. Once a session is live, the fixture reads its snapshot in native fishing states and permits `BiteReady` visible reel, `PullExited` completion, interruption handling, and cleanup without reopening the entry gate. L0 and L4 use the same policy.

The fixture also tracks phase/sequence progress. An unchanged live session, or an entry wait that never becomes playable, fails after a complete 90-second no-progress window with context, phase, sequence, and current normal-state status. Cast/reel attempts now emit action, phase, sequence, normal-state, application, and status evidence. Pull-exit/interruption retry delays are scheduled only on a newly observed phase/sequence so repeated snapshots cannot push the retry deadline forward indefinitely.

Focused validation on 2026-07-19:

- `DTMAPI.QaUnitTests` Release: passed, including no-session/non-normal rejection; live non-normal observation/reel/completion policy; one-action-per-sequence; released-session entry-gate restoration; and watchdog progress/timeout cases.
- `tools/scripts/test-batch5-gc-ladder.ps1`: passed, including the session-aware policy and diagnostic watchdog source gates.
- `git diff --check`: passed.

This validation closes the source-level defect but does not replace the required AutoFishing L0-L5 game replay.

## Recovery And Replay Boundary

After failure, the shared Runtime lock was free and no `DolocTown.exe` remained. The frozen ActionSpeed and AutoFishing trees reproduced `DCC13AED2ABB8F9942FAD343DFE2948317614882B0B77616202CAC70D1AA8293` and `FB2074989669485D899B0C1166B0F8FDABB2A04F6045BCFFEDA6A12E89937A97`. The Author source-state file reproduced length `178` and SHA-256 `1C1A302417CD65B791330062C270DF0BD101893712F86C478CE283F5208C7C19`; the official profile and all three retained third-save files also reproduced their pre-run hashes. No QA activation root remained.

The 24 completed ActionSpeed stages belong to one frozen runner/runtime/product boundary and remain valid evidence. After the fixture correction, rebuild and focused validation, replay only the six-stage `AutoFishing` domain with the same 600/30/10/5 definition and frozen local product-tree expectation. Do not rewrite `AutoFishing-L0` in the failed `Both` ladder or describe the combined result as one uninterrupted 30-stage run; aggregate the retained ActionSpeed receipt set and the corrected AutoFishing receipt set explicitly by their two evidence roots.
