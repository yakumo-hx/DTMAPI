# 20260719-0003 - Batch 5 Ordinary No-QA Single-Deadline Tail Overrun Review

- Date: 2026-07-19
- Status: corrective implementation queued after the active formal GC ladder
- Severity: P1 acceptance-infrastructure blocker
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Related interrupted run: `docs/debug/evidence/GAME-SMOKE/20260719-031553`
- Related current behavior receipt: `docs/debug/evidence/GAME-SMOKE/20260719-034102`

## Finding

The ordinary no-QA runner now creates one absolute deadline at launch and uses its remaining budget for startup, `GameLaunched`, `SaveLoaded`, the main no-QA UI deadline and the final process wait. This removes the previously observed second full `TimeoutSeconds` wait after a title-screen navigation timeout.

An independent final source audit found that the promise is still incomplete inside the UI behavior tail. The DebugConsole Y matrix calls helpers with fixed four/five-second waits, performs fixed sleeps and may iterate ten taps without checking the absolute deadline before every input. Equipment sends input before checking remaining time and converts an expired/negative remainder to at least one second through `Max(1, ...)`. The AnimalViewer auto-drive sequence sends each A/E step and sleeps between steps without rechecking the deadline. The hover helper has the same forced one-second remainder.

If the third save loads close to the deadline, these paths can continue sending player input and accepting receipts after the advertised total budget has expired. The overrun is bounded compared with the removed second 900-second wait, but it still makes the structured claim `one launch-to-process-wait deadline` false, can retain the shared Runtime lock longer than declared, and can accept a UI result outside the intended fail-closed observation window.

This is a runner defect, not evidence that the Local11 products or retained `034102` behavior failed. The retained receipt remains valid for its exact run and behavior facts, but it predates an authoritative `completed-before-deadline` gate and therefore cannot alone close the corrected runner contract.

## Required Correction

1. Give every no-QA UI helper the same absolute deadline. Before each input, sleep, polling call and loop iteration, compute the remaining budget and return failure when it is zero.
2. Cap every configured wait to the positive remaining budget. Remove `Max(1, ...)` deadline extensions and do not send Equipment/Animal/DebugConsole input after expiry.
3. Add a structured `NoQaBehaviorCompletedBeforeDeadline` receipt based on a timestamp captured after the last required YConsole, EquipmentSlots and AnimalViewer behavior/cleanup receipt. Include it in the aggregate no-QA pass condition.
4. Add deterministic expired-at-entry and short-budget tests which prove no input/helper call and no post-deadline acceptance occurs.
5. Run a fresh third-save Local11 ordinary no-QA acceptance with the corrected runner. If it passes, move the Catalog's unique current-tree receipt to the new result while retaining `034102` as superseded positive evidence rather than rewriting its historical fields.

## Sequencing Boundary

The formal 30-stage GC ladder `20260719-044157-70ce39e6` was already running when this source-only finding was confirmed. `run-game-smoke.ps1` is an input to every stage, so changing it mid-ladder would create a mixed-runner evidence set. The runner must remain frozen until that ladder reaches a terminal state and releases the Runtime lock. This deadline correction then follows as a separate harness change; it does not retroactively alter GC workload or metric semantics.

No running game, Runtime lock, Author source state, product tree or save was changed by this review.
