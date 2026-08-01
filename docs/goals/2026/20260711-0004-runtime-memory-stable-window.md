# Goal: 30-minute inactive runtime memory stable window

Status: complete; inactive curve flattened, platform-layer branch not entered, active fishing loop remains pending.

Target version: `0.5.3-alpha` / `0.5.3.0` (already current; do not bump).

Source review: `docs/reviews/api/2026/20260711-0004-runtime-memory-stable-window-review.md`.

## Task 1 — trend windows and slope

- Add startup `0..180s`, stable `180s..end`, and trailing final-600-second window results.
- Include all existing metrics and a least-squares slope per minute without changing the existing endpoint trend field.
- Keep sample retention bounded and sufficient for 1800 seconds at 30-second cadence.

## Task 2 — Gen2 MonoUsed low water

- Detect sampled Gen2-count advances and retain bounded low-water epochs until the next observed advance/end.
- Record collection delta, detection/close time, MonoUsed at detection, low-water value/time, and whether the epoch is complete.
- Never call `GC.Collect()`.

## Task 3 — automatic validation

- Verify window boundaries, regression slopes, final-ten-minute selection, multi-Gen2 interval grouping, low-water updates, null Mono metrics, bounded retention, JSON serialization, and no forced-GC code path.
- Run Release build, complete unit tests, parser, version consistency, and `git diff --check`.

## Task 4 — one 30-minute runtime

- Shared runtime lock, fifth save, independent cold start.
- `InactiveNoConsumer`, `AutoFishingPerformanceZeroWarmupSeconds=0`, `AutoFishingPerformanceZeroMeasureSeconds=1800`, 30-second cadence.
- Require 0 cast/fish, allocation subprobe explicitly Blocked if Mono remains nonfunctional, truthful process/GC metrics, zero DTMAPI domain growth, title cleanup, and no leftover process.

## Task 5 — classify and route

- Apply the review routing to startup/stable/trailing slopes and Gen2 low waters.
- Only if sustained growth remains, run platform-layer baselines; otherwise stop after the single inactive run.
- Update the 0004 record, Debug Index, smoke matrix, public API matrix, Hook Map if relevant, and ISSUE-010.

## Completed evidence

- Implementation: `dac31cc`.
- Automatic: Release 0 warnings/errors, complete unit runner OK, parser/JSON/version/diff checks passed.
- Runtime: `GAME-SMOKE/20260711-115939`, JSON `AUTO-FISHING-PERF/20260711-123039`, 1800.002 seconds, 61 samples, 0 cast/fish, zero structural deltas, title/process cleanup passed.
- Classification: trailing MonoUsed and Gen2 lows fall, reserves/heap are flat, working set is nearly flat, and the private-memory wave recovers. No platform-layer baselines were run.
- Stop boundary: do not repeat/extend inactive measurement or run platform layers. The near-4.97-GB recovered private wave is a platform transient; only inactive AutoFishing sustained-retention suspicion is closed, while ISSUE-010 and active-loop validation remain open.
