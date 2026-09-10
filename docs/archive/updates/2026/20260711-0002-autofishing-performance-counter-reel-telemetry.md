# 20260711-0002 AutoFishing Performance Counter and Reel Telemetry

## Status

- Source, Release build, full unit, parser, one-fish Mono telemetry, and short Unity Mono allocation-calibration gates completed.
- Unity Mono `GC.GetAllocatedBytesForCurrentThread` is behaviorally nonfunctional in the tested runtime; allocation matrices are now Blocked instead of reporting zero.
- No replacement ten-minute baseline, 100/500 fish, soak, public API change, version bump, or formal release occurred. `ISSUE-010` remains open.

## Source Request

Implement `docs/goals/2026/20260711-0002-autofishing-performance-counter-reel-telemetry.md` from review `docs/reviews/api/2026/20260711-0002-autofishing-performance-counter-reel-telemetry-review.md`: calibrate the Mono allocation counter, serialize blocked interval metrics as null, persist visible-reel counters across runtime release, and record—but do not change—the 500 ms post-consumption retry risk.

## Changes

- `FishingPerformanceProbe` now calibrates the resolved allocation getter on the measurement thread by reading it, allocating and keeping alive a 4096-byte array, and reading it again before establishing allocation and Gen0 baselines.
- Counter states are explicit: unavailable, untested, functional, nonfunctional, and error. A calibration delta below 4096 blocks as `blocked-allocation-counter-nonfunctional`; invocation exceptions block as `blocked-allocation-counter-error`.
- Blocked allocation and Gen0 interval fields are nullable and serialize as `null`. Calibration payload, before/after/delta, functional flag, counter status, and failure reason are included in the performance JSON.
- `run-game-smoke.ps1` recognizes `Smoke.AutoFishingPerformance = blocked`, preserves clean title/process validation, and reports outer `RunStatus=Blocked` instead of Failed or Passed.
- `FishingPrimitivesService` archives visible-reel queued/consumed/retry/timeout counts exactly once when the active primitive runtime detaches. Diagnostics read archived plus live totals and always format numeric values.
- Performance JSON now records visible-reel start/end/delta for all four counters. InactiveNoConsumer rejects any nonzero visible-reel value.
- The one-fish Mono gate now requires archived queued/consumed counts to remain readable after release, timeout zero, and all runtime/session/lease/native roots zero.
- The 500 ms post-consumption rearm remains unchanged. Native build `23762374_public_C416D4` confirms `AgentStateFishingWait.NextState()` deducts energy at the native input branch, so a later injected test must count native reel/energy effects rather than treating getter consumption as transaction confirmation.

## Main Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPerformanceProbe.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`

## Automatic Validation

- Release build: passed with 0 warnings and 0 errors.
- Complete unit runner: `DTMAPI.UnitTests: OK`.
- Injected tests cover missing, constant, throwing, and functional counters; the functional fake proves the calibration allocation is excluded from the measurement baseline.
- Blocked JSON serialization test verifies `AllocatedBytes` and `Gen0Collections` are null.
- Lifecycle tests cover retry/timeout telemetry, release, ReturnedToTitle, throwing transition subscribers, second acquire, and every activation rollback checkpoint without loss or double archive.
- PowerShell parser passed for `run-game-smoke.ps1`, `install-to-game.ps1`, and `release-common.ps1`.
- Existing version/package consistency and frozen-API consumer checks pass in the complete unit runner.
- `git diff --check` passed with line-ending warnings only.

## Limited Runtime Evidence

- Natural wait plus visible minigame: `docs/debug/evidence/GAME-SMOKE/20260711-065929` passed. The final Mono gate records `visibleReelQueued=1`, `visibleReelConsumed=1`, `visibleReelRetries=0`, `visibleReelTimeouts=0`, zero accessor failures, and zero session/lease/native/callback roots after title return.
- Short InactiveNoConsumer calibration: `docs/debug/evidence/GAME-SMOKE/20260711-070116` completed with outer `RunStatus=Blocked`, `AutoFishingPerformanceCounter=Blocked`, and clean title/process exit. Current JSON `DTMAPI-evidence/AUTO-FISHING-PERF/20260711-070201/auto-fishing-performance.json` records method available, functional=false, status=nonfunctional, payload=4096, before=0, after=0, delta=0, allocation/Gen0 interval fields null, all visible-reel fields zero, and title cleanup true.
- Historical `20260711-012851` and `20260711-024427` JSON files remain unchanged. Their `AllocatedBytes=0` values are invalid because method presence was mistaken for counter functionality. Their 0 cast/fish, accessor stability, no Fishing hot-log, native movement, and title-cleanup evidence remains usable; their nine Gen0 collections do not identify a thread byte count.

## Rollback

Rollback counter calibration, nullable result fields, smoke blocked classification, and serializer tests together. Roll back service telemetry archival together with diagnostics and performance start/end/delta consumers so no path can return to empty released-runtime counters.

## Follow-up

- Keep the allocation matrix blocked on Unity Mono unless a genuinely functional cumulative allocation source is introduced and independently calibrated.
- Future delayed-phase fault injection must prove that one logical visible-reel request cannot cause duplicate native energy/reel effects. Do not use input getter consumption alone as proof.
- Keep `0.5.3-alpha`, AutoFishing `1.4.3-dtmapi`, legacy freeze, 100/500 deferral, and `ISSUE-010` status unchanged.
