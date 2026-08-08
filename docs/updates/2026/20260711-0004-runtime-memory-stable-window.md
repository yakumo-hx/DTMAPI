# 20260711-0004 — 30-minute inactive runtime memory stable window

## Status

`source-unit-and-30-minute-runtime-verified / inactive-autofishing-gc-suspicion-cleared / active-loop-pending / issue-010-open`

Runtime remains `0.5.3-alpha` / `0.5.3.0`. No public API, Hook, AutoFishing behavior, or product version changed.

## Source request

The two `20260711-0003` 600-second curves had the same roughly 28–30 MB process-memory rise, stable MonoHeap/UnityReserved, opposite MonoUsed directions, and zero DTMAPI structural deltas. The user requested observation windows and one longer inactive process before deciding whether platform-layer baselines were necessary.

- Review: `docs/reviews/api/2026/20260711-0004-runtime-memory-stable-window-review.md`.
- Goal: `docs/goals/2026/20260711-0004-runtime-memory-stable-window.md`.

## Implementation

- `RuntimeMemoryTrendResult` now contains:
  - `StartupWindow`: requested `0..180s`;
  - `StableWindow`: requested `180s..end`;
  - `TrailingTenMinutesWindow`: final 600 seconds.
- Every overall/window metric retains start/end/max/delta and endpoint `TrendPerMinute`, and adds least-squares `SlopePerMinute`.
- `Gen2MonoUsedLowWaters` records sampled Gen2 epochs with count-before/after/delta, detection/close times, MonoUsed at detection, low-water value/time, and close reason.
- Multiple Gen2 collections inside one 30-second interval are grouped explicitly instead of fabricating per-collection values.
- Sample retention remains bounded. A 30-minute/30-second run requires 61 samples, below the existing 64-sample bound.
- No code path calls `GC.Collect()`.

## Automatic validation

- Release solution build: 0 warnings, 0 errors.
- Complete unit runner: `DTMAPI.UnitTests: OK`.
- PowerShell parser, JSON serialization, version suite, and `git diff --check`: passed.
- Synthetic 1800-second curve verifies 61 total samples; 7 startup, 55 stable, and 21 trailing samples; startup/stable slopes of 60,000/6,000 bytes per minute; grouped multi-Gen2 intervals; completed low-water epochs; zero reserve/root trends.

## Runtime evidence

- Evidence: `docs/debug/evidence/GAME-SMOKE/20260711-115939`.
- JSON: `DTMAPI-evidence/AUTO-FISHING-PERF/20260711-123039/auto-fishing-performance.json`.
- Profile: fifth-save `InactiveNoConsumer`, zero separate warm-up, 1800.002 seconds measured, 30-second cadence.
- Result: outer Passed, allocation subprobe Blocked/nonfunctional, 61 samples, zero trimmed samples, Process Gen0/1/2 `30/30/30`, 0 cast/fish, title cleanup passed, clean process exit.
- Whole-run DTMAPI record/root/accessor/reel/transient deltas are all zero.

### Window results

| Metric | Startup 0–180s | Stable 180–1800s | Trailing 1200–1800s |
| --- | --- | --- | --- |
| Samples | 7 | 55 | 21 |
| MonoUsed delta / OLS slope | `-15,532,032` / `+10,930,604 B/min` | `-18,567,168` / `-1,007,099 B/min` | `-139,452,416` / `-8,877,347 B/min` |
| MonoHeap delta | `0` | `0` | `0` |
| UnityAllocated delta / OLS slope | `+1,575,560` / `+380,937 B/min` | `+8,526,503` / `+271,060 B/min` | `+1,069,349` / `+62,041 B/min` |
| UnityReserved delta | `0` | `+2,097,152` | `0` |
| Process private delta / OLS slope | `-99,545,088` / `-19,251,999 B/min` | `+21,647,360` / `+6,722,392 B/min` | `-42,098,688` / `+22,271,824 B/min` |
| Process working delta / OLS slope | `-60,227,584` / `-12,209,718 B/min` | `+33,067,008` / `+865,419 B/min` | `+2,400,256` / `+329,239 B/min` |
| DTMAPI record/root/accessor/transient delta | all `0` | all `0` | all `0` |

The trailing private OLS slope is positive because a late, non-linear platform wave rose from the normal `~4.52–4.58 GB` band to `~4.97 GB` during 1560–1680 seconds. It then returned to `4,530,388,992` bytes at 1800 seconds, below the window start `4,572,487,680`; endpoint delta is `-42,098,688`. This is a transient wave, not a continuously rising baseline.

Trailing working set moved only `3,282,329,600 -> 3,284,729,856` (`+2,400,256`, about 2.29 MiB in ten minutes). MonoUsed ended at the window minimum. UnityAllocated stayed in a roughly 348.8–350.8 MB band and its trailing slope was about 60.6 KiB/min.

### Gen2 low-water result

- Thirty Gen2 advances were sampled.
- In the trailing window the observed MonoUsed lows move from `795,127,808` at 1230s through `763,502,592`, `761,606,144`, `760,004,608`, `758,345,728`, `756,785,152`, `729,882,624`, `731,844,608`, `728,883,200`, `726,253,568`, to `725,606,400` at 1800s.
- The tail is descending overall, not a monotonic retained-low-water staircase.

## Classification and routing

The longer inactive curve is classified as common game/Unity/platform warming, collection sawtooth, and cache waves followed by a flat tail. There is no evidence of a linear inactive AutoFishing leak:

- AutoFishing never acquired a session or fished.
- DTMAPI structural counters stayed fixed.
- MonoHeap and trailing UnityReserved stayed fixed.
- Working set nearly flattened.
- MonoUsed and its post-Gen2 low waters declined.
- The private-memory positive OLS value is contradicted by its negative endpoint delta and explicit temporary wave/recovery.

Therefore the platform-layer baselines are not run. The inactive AutoFishing GC suspicion is temporarily cleared. The remaining AutoFishing item under ISSUE-010 is an active long fishing loop, which this no-fishing baseline intentionally does not verify.

Follow-up stop decision: do not run another inactive/no-consumer curve, do not extend this 30-minute process, and do not run platform-layer baselines. The recovered near-4.97-GB private-memory wave is retained as a common game/Unity/platform transient rather than classified as an AutoFishing leak. ISSUE-010 remains open; only inactive AutoFishing sustained retention is removed from its suspect list. Formal-release readiness may add one approximately 10-minute or 10–20-fish active trend; 100/500 remains deferred.

## Commit and rollback

- Implementation commit: `dac31cc` (`feat: add stable runtime memory windows`).
- Revert that commit to remove the window/low-water schema. Existing 0003 overall trend fields remain the fallback.
