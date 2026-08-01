# 20260711-0003 — runtime memory trend and native reel acceptance

## Status

`source-unit-and-two-10-minute-runtime-verified / allocation-subprobe-blocked / issue-010-open`

Runtime remains `0.5.3-alpha` / `0.5.3.0`. No public API or AutoFishing product version changed, and no formal release was produced.

## Source request and goal

- User review identified that allocation calibration terminated the entire performance scenario and incorrectly nulled process-wide Gen0 evidence.
- User review also identified that the 500 ms visible-reel resend could repeat native `CostEnergy`/reel effects after the input getter had already been accepted.
- Goal: `docs/goals/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance.md`.
- Review: `docs/reviews/api/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance-review.md`.

## Changes

### General runtime memory diagnostics

- Added Core `RuntimeThreadAllocationProbe`; missing, constant-zero, and throwing counters block only the thread-allocation subresult.
- Added general bounded `RuntimeMemoryTrendProbe`. It samples at start, every 30 seconds, and completion without forced GC and records Mono used/heap, Unity allocated/reserved/unused reserved, process private/working set, process Gen0/1/2, DTMAPI resource records and owner roots, and caller accessor/reel/transient counters.
- Each metric reports start/end/max/delta and trend per minute. Sample retention is bounded to 64 by default.
- Unity profiler methods are resolved once into typed `Func<long>` delegates by GameBridge.
- Unity Mono also returned managed `Process.PrivateMemorySize64` and `WorkingSet64` as zero. Core now falls back to Windows `GetProcessMemoryInfo`; if neither route returns a positive value, the metric is null instead of a false zero.
- `FishingPerformanceProbe` composes the two general probes. Allocation capability no longer owns scenario completion. `Gen0Collections` was replaced by independent `ProcessGen0Collections`, with Gen1/Gen2 added.
- Smoke now publishes `Smoke.AutoFishingAllocationCounter=blocked` independently while `Smoke.AutoFishingPerformance` can verify the full scenario. The outer run remains Passed if all non-allocation gates pass.

### Native reel acceptance

- Added required Harmony Postfix `AgentStateFishingWait.NextState -> FishingWaitNextStatePostfix`.
- The primitive runtime confirms a visible reel only when the pending consumed Wait instance returns a different native state. This callback runs after native `CostEnergy`/state selection and before the next frame.
- Accepted state is cleared immediately and cannot rearm at 500 ms. Confirmation is idempotent.
- Process-lifetime visible-reel telemetry now also preserves `NativeAccepted` through runtime detach.
- The frozen legacy service implements the seam as a no-op because its compatibility transaction directly executes `NextState` and does not use visible-edge retry state.

## Validation

### Automatic

- Release solution build: 0 warnings, 0 errors.
- Complete unit runner: `DTMAPI.UnitTests: OK`.
- PowerShell parser, JSON/version suite, and `git diff --check`: passed.
- Fault injection advances a fake clock 750 ms after native acceptance while phase confirmation is delayed; one simulated energy/reel effect remains exactly one, no second edge is consumable, retry count stays zero, and a second acceptance is rejected.
- Windows process-memory fallback unit check returns positive private and working-set bytes for the current process.

### Runtime evidence

Initial full-duration runs `GAME-SMOKE/20260711-073623` and `GAME-SMOKE/20260711-074859` exposed the managed `Process` zero stub. They remain diagnostic evidence, not final process-memory baselines.

| Profile | Evidence / JSON | Duration / samples | Process memory | GC | DTMAPI domain trend | Result |
| --- | --- | --- | --- | --- | --- | --- |
| InactiveNoConsumer | `GAME-SMOKE/20260711-080312`; `AUTO-FISHING-PERF/20260711-081500/auto-fishing-performance.json` | 600.016 s / 21 | private `4,534,104,064 -> 4,564,086,784`, max `4,564,660,224`, delta `+29,982,720`; working `3,282,137,088 -> 3,311,988,736`, max `3,315,625,984`, delta `+29,851,648` | Gen0/1/2 `9/9/9` | record/root/accessor/reel/transient deltas all `0`; 0 cast/fish | outer Passed; allocation subprobe Blocked/nonfunctional; title/process cleanup passed |
| EnabledNoRod | `GAME-SMOKE/20260711-081532`; `AUTO-FISHING-PERF/20260711-082713/auto-fishing-performance.json` | 600.015 s / 21 | private `4,428,009,472 -> 4,456,230,912`, max `4,460,675,072`, delta `+28,221,440`; working `3,193,290,752 -> 3,222,904,832`, max `3,228,082,176`, delta `+29,614,080` | Gen0/1/2 `10/10/10` | record/root/accessor/reel/transient deltas all `0`; 0 cast/fish | outer Passed; allocation subprobe Blocked/nonfunctional; native movement cancel, title/process cleanup passed |

These are bounded baselines, not leak thresholds. The roughly 28–30 MB process deltas are recorded without declaring a leak or a pass threshold. No 100/500 fish or arbitrary gameplay soak ran.

## Changed areas

- Core runtime allocation/memory diagnostics and direct record/root counters.
- GameBridge Unity metrics, Fishing performance/Hook/runtime/visible-reel/compatibility/smoke paths.
- `tools/scripts/run-game-smoke.ps1`, complete unit runner, and related docs.

## Rollback

- Revert commits `ab53c7a2`, `d768ab1d`, and `a996d9f4` together to restore the pre-split performance scenario and pre-acceptance retry behavior.
- Do not revert the earlier checkpoint `f9f767dd` when rolling back only this update.

## Follow-up

- ISSUE-010 remains open. The allocation byte subprobe is blocked on this Unity Mono runtime.
- 100/500 fish and arbitrary long gameplay remain deferred.
- Future real-fish acceptance evidence may record `visibleReelNativeAccepted`, but no additional fish was required or run in this bounded round.
- Follow-up `20260711-0004` extended the inactive profile to 1800 seconds with startup/stable/trailing windows and classified the tail as flattened/common platform warming; inactive AutoFishing GC suspicion is temporarily cleared.
