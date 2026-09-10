# 20260711-0002 AutoFishing Performance Counter and Reel Telemetry

## Status

- Implementation: complete.
- Automatic validation: passed.
- Limited runtime validation: passed for the natural one-fish telemetry gate; Unity Mono allocation calibration correctly produced a clean `Blocked/counter-nonfunctional` result.
- Long-run boundary: replacement ten-minute baselines, 100/500 fish, and soak were not run; `ISSUE-010` remains open.
- Parent review: `docs/reviews/api/2026/20260711-0002-autofishing-performance-counter-reel-telemetry-review.md`.

## Objective

Reject nonfunctional Unity Mono thread-allocation counters, preserve visible-reel telemetry across primitive runtime release, correct prior evidence wording, and leave the 500 ms post-consumption retry behavior unchanged but explicitly tracked as a deferred native-side-effect risk.

## Required Changes

1. Calibrate `GC.GetAllocatedBytesForCurrentThread` on the measurement thread with a kept-alive 4096-byte array before establishing allocation/Gen0 baselines.
2. Distinguish unavailable, nonfunctional, invocation-error, and functional counters. Serialize allocation and Gen0 interval metrics as null when blocked; never publish a zero-allocation conclusion from a blocked counter.
3. Make smoke Hook status and `run-game-smoke.ps1` surface allocation-counter blockage as `Blocked` while still requiring title cleanup and clean process exit.
4. Archive visible-reel queued/consumed/retry/timeout counts exactly once in `FishingPrimitivesService` when a primitive runtime detaches. Expose archived plus live totals to diagnostics.
5. Add visible-reel start/end/delta fields to `auto-fishing-performance.json` and validate InactiveNoConsumer remains zero.
6. Record the 500 ms post-consumption rearm risk without changing behavior.

## Automatic Acceptance

- Injected counter tests cover missing method, constant/stub, invocation exception, and functional growth; calibration is excluded from measured allocation and Gen0 baselines.
- Blocked JSON contains null allocation/Gen0 interval fields and an explicit counter status/failure reason.
- Runtime release, title return, throwing subscriber, second acquire, and activation rollback preserve exact monotonic visible-reel totals without duplication; diagnostics contain no empty values.
- Performance telemetry start/end/delta is exact and InactiveNoConsumer remains zero.
- Release build, complete unit runner, PowerShell parser, JSON serialization, version checks, and `git diff --check` pass.

## Limited Runtime Acceptance

- Shared runtime lock, fifth save, independent clean processes.
- One natural wait plus visible-minigame Mono gate: after disable/release, queued and consumed remain readable, timeout is zero, and all runtime/session/lease/native fields return to zero.
- One short InactiveNoConsumer calibration gate: Unity Mono reports either a calibrated functional counter or `Blocked/counter-nonfunctional`; blocked allocation fields are null and title/process cleanup still passes.

## Exclusions

- No replacement ten-minute baselines, 100/500 fish, or soak.
- No public API, version, AutoFishing feature, or 500 ms retry change.
- Keep `0.5.3-alpha` preview and `ISSUE-010` open.

## Completion Evidence

- Release build passed with 0 warnings/errors; complete unit runner printed `DTMAPI.UnitTests: OK`; parser and version/package checks passed.
- One-fish fifth-save gate `GAME-SMOKE/20260711-065929` passed with queued=1, consumed=1, retries=0, timeouts=0 after runtime release and all title cleanup roots zero.
- Short fifth-save calibration gate `GAME-SMOKE/20260711-070116` produced outer `RunStatus=Blocked`, current JSON `AUTO-FISHING-PERF/20260711-070201`, counter before/after/delta `0/0/0`, null allocation/Gen0 interval fields, and title/process cleanup passed.
- Update: `docs/updates/2026/20260711-0002-autofishing-performance-counter-reel-telemetry.md`.
