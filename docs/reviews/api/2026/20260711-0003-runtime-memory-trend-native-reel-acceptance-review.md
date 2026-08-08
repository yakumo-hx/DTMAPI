# Runtime memory trend and native reel acceptance review

- Date: 2026-07-11 (Asia/Shanghai)
- Source: user code review of the 20260711 allocation calibration and visible-reel retry implementation
- Scope: pre-implementation API/GameBridge reliability review for DTMAPI `0.5.3-alpha`; no public API change

## Issue 1 — allocation counter failure terminates the whole scenario

### Reported evidence

- `FishingPerformanceProbe.Observe()` completes with `Blocked` as soon as the same-thread allocation counter calibration fails.
- Evidence `GAME-SMOKE/20260711-070116` therefore has `ElapsedSeconds=0`; logging, accessor, visible-reel, transient, process memory, and process GC trends never enter a measurement interval.
- `GC.CollectionCount(0)` is process-wide and independent of `GC.GetAllocatedBytesForCurrentThread`; it must not be nulled merely because the thread byte counter is nonfunctional.

### Review

- Confirmed in source: the unavailable/calibration-error branches set `completed=true` and return `FishingPerformanceProbeUpdate.Blocked` before the scenario baseline.
- Root cause: allocation capability and scenario completion are represented by one state machine.
- Required boundary: a generic `RuntimeMemoryTrendProbe` owns timed process/runtime sampling, while a separate allocation subprobe owns only same-thread byte calibration and measurement.
- The runtime trend must continue for the configured duration without forced GC and report Mono/Unity/process/GC plus bounded DTMAPI domain counters. Allocation can remain explicitly blocked as one nullable submetric.
- Rename the independent interval metric to `ProcessGen0Collections` and also record Gen1/Gen2, preventing it from being mistaken for a thread-allocation metric.
- Acceptance: a constant-zero allocation getter produces a blocked allocation subresult while the runtime trend reaches its full synthetic duration and records start/end/max/delta/slope samples.
- First implementation evidence `GAME-SMOKE/20260711-073623` and `GAME-SMOKE/20260711-074859` exposed a second Mono stub: managed `Process.PrivateMemorySize64` and `WorkingSet64` were both zero while an external Windows process query reported nonzero values. These first runs prove duration/GC/domain behavior but are not final process-memory evidence. The general probe must use `GetProcessMemoryInfo` on Windows when managed values are nonpositive and serialize null if neither provider is functional.

## Issue 2 — a 500 ms visible-reel retry can repeat native side effects

### Reported evidence

- After an input getter consumes the synthetic edge, `FishingVisibleReelInputState` rearms after 500 ms if no phase Hook has confirmed progress.
- Current native `AgentStateFishingWait.NextState()` calls `DolocAPI.CostEnergy(FishingEnergyCost)` inside that input branch before returning Battle or Pull.
- Existing one-fish evidence has `retries=0`, so it does not disprove the delayed-confirmation failure mode.

### Review

- Confirmed against build `23762374_public_C416D4`: the energy effect precedes the returned next state.
- An input getter read proves only edge consumption; it does not prove the native transaction accepted the edge.
- Required boundary: patch `AgentStateFishingWait.NextState` Postfix and confirm acceptance only when the pending wait instance returned a different state. The confirmation is synchronous with the native method and therefore precedes any later frame-based retry.
- Once confirmed, the visible-reel state must suppress rearm, remain idempotent, and expose an accepted counter plus zero-retry evidence. Phase Hooks still clear the pending state normally.
- Fault injection must delay phase confirmation beyond 500 ms while invoking the native-effect confirmation once; no second edge or second simulated energy/reel effect may occur.
- Acceptance: the hook is part of Fishing `HooksReady`; callback failure is isolated; unit fault injection shows one native effect, one acceptance, zero retries, and no second consumption.

## Constraints

- Keep Fishing Primitives first-party `internal` and keep `0.5.3-alpha` unchanged.
- Do not force GC during a trend scenario.
- Sampling is every 30 seconds by default and retained sample storage is bounded.
- Do not claim Unity Mono thread allocation bytes when calibration is nonfunctional.
- Do not use input-getter consumption as native reel acceptance.
- Run only the requested fifth-save 10-minute `InactiveNoConsumer` and `EnabledNoRod` baselines; no 100/500 fish or soak.

## Completion evidence

- Source/unit/parser/diff checks pass. Delayed-phase fault injection proves one native effect, one acceptance, zero resend/retry, and idempotent confirmation.
- Initial full runs `GAME-SMOKE/20260711-073623` and `074859` found and rejected managed Process zero metrics.
- Final Windows-native process-memory runs `GAME-SMOKE/20260711-080312` and `081532` each completed 600 seconds with 21 samples, allocation subprobe Blocked but outer Passed, independent process GC metrics, zero DTMAPI domain deltas, and clean title/process exit.
- The second run additionally verifies 0 cast/fish and native HorizontalMoveFactor cancellation.
- This review is resolved for the bounded request; ISSUE-010 and 100/500/arbitrary gameplay remain open.
