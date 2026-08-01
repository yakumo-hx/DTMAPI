# Goal: runtime memory trend and native reel acceptance

Status: complete for the bounded 2026-07-11 request; ISSUE-010 remains open.

Target version: `0.5.3-alpha` / `0.5.3.0` (already current; do not bump).

Source review: `docs/reviews/api/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance-review.md`.

## Task 1 — split allocation capability from scenario completion

- Extract same-thread allocation calibration/measurement into a separate subprobe.
- A missing, constant-zero, or throwing counter blocks only allocation byte fields.
- Timed runtime measurement still starts, reaches the requested duration, validates zero-fish invariants, and performs title cleanup.
- Replace ambiguous `Gen0Collections` with independent `ProcessGen0Collections`; record process Gen1/Gen2 as well.

## Task 2 — add a general RuntimeMemoryTrendProbe

- Put the bounded sampling engine in Core diagnostics, independent of AutoFishing and Unity compile-time types.
- Sample start, every 30 seconds, and final completion without calling `GC.Collect`.
- Record Mono used/heap; Unity allocated/reserved/unused reserved; process private/working set; process GC 0/1/2; resource record count; owner-root count; and caller-supplied accessor/reel/transient counts.
- Output bounded samples plus start/end/max/delta and slope-per-minute for each metric.
- GameBridge resolves Unity profiler static `long` methods once into typed delegates; an unavailable metric degrades to null without stopping process/GC sampling.
- On Windows Mono, fall back to `GetProcessMemoryInfo` when managed `Process` private/working-set values are nonpositive; do not serialize a known stub zero as real memory usage.

## Task 3 — confirm native visible-reel acceptance

- Add a required `AgentStateFishingWait.NextState` Postfix Hook.
- Confirm a pending consumed edge only if the same wait instance returns a different native state.
- Accepted state prevents the 500 ms resend; confirmation is idempotent and observable.
- Add delayed-phase fault injection tests proving one energy/reel effect and no second synthetic edge.

## Task 4 — automatic verification and scoped commits

- Release build, complete unit tests, JSON serialization, PowerShell parser, version consistency, and `git diff --check` must pass.
- Commit the generic probe/smoke schema separately from the native reel Hook change.

## Task 5 — limited runtime evidence

- Under the shared runtime lock, use save slot 5 and independent cold starts.
- Run `InactiveNoConsumer`: 60-second warm-up plus 600-second measurement.
- Run `EnabledNoRod`: 60-second warm-up plus 600-second measurement, then native HorizontalMoveFactor cancellation and title cleanup.
- Allocation subprobe is expected to be blocked/nonfunctional on this Mono runtime, but runtime trend and process GC metrics must cover the complete interval.
- No fish loop, 100/500 matrix, or other soak.

## Completion gate

- Both baselines contain the requested bounded 30-second samples and clean title/process exit.
- Allocation fields are null with an explicit blocked substatus; scenario status is not Blocked solely for that reason.
- Native acceptance Hook and fault-injection tests prove a logical reel cannot repeat the native effect after acceptance.
- Update record, Hook map, smoke matrix, Debug Index, and ISSUE-010 cite exact evidence. ISSUE-010 stays open.

## Completed evidence

- Commits: `ab53c7a2` (general probes), `d768ab1d` (native reel acceptance), `a996d9f4` (truthful Windows process-memory fallback).
- Automatic checks: Release 0 warnings/errors, complete unit runner OK, parser and diff checks pass.
- Final runtime: `GAME-SMOKE/20260711-080312` and `GAME-SMOKE/20260711-081532`, both outer Passed with 600-second/21-sample trends, allocation subprobe Blocked, domain deltas zero, title/process cleanup passed; EnabledNoRod also passed 0 cast/fish and native movement cancellation.
