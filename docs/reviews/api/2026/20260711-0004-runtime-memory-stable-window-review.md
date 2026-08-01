# Runtime memory stable-window review

- Date: 2026-07-11 (Asia/Shanghai)
- Source: user interpretation of the two 600-second `20260711-0003` runtime curves
- Scope: internal diagnostics and one bounded fifth-save inactive baseline; no public API or AutoFishing behavior change

## Confirmed interpretation

- Across `GAME-SMOKE/20260711-080312` and `081532`, MonoHeap and UnityReserved did not grow.
- MonoUsed moved in opposite directions (`+78 MiB`-class versus `-44 MiB`-class), which is inconsistent with one fixed AutoFishing-owned retained set.
- Both process private/working curves grew by roughly 28–30 MB with similar shapes and a shared step around minute six; only small additional working-set growth followed.
- DTMAPI record, owner-root, accessor, reel, and transient deltas were all zero.
- Current evidence fits common game/Unity/platform warming and cache retention better than an AutoFishing-linear leak, but ten minutes is not enough to classify the stable tail.

## Required next checkpoint

- Extend the general trend result with three windows:
  - startup: elapsed `0..180s`;
  - stable: elapsed `180s..end`;
  - trailing: final 600 seconds.
- For every metric preserve start/end/max/delta, endpoint trend, and a real least-squares slope per minute.
- Record sampled post-Gen2 MonoUsed low-water epochs. A 30-second sampler cannot distinguish multiple Gen2 collections inside one interval, so one observation may explicitly represent `Gen2CollectionsObserved > 1`; it must not fabricate per-collection values.
- Continue to prohibit `GC.Collect()`.

## Runtime scope

- One fifth-save `InactiveNoConsumer` process only.
- Measurement starts immediately after the profile is ready: no separate pre-warm-up, 1800 seconds total, 30-second samples.
- AutoFishing is installed but never enabled; no fishing or player energy consumption.
- Do not repeat EnabledNoRod.

## Result routing

- Classify as common warming/flattening when reserve/heap and DTMAPI structural metrics remain bounded, the trailing ten-minute least-squares slopes materially fall from the startup/stable growth shape, and post-Gen2 MonoUsed low waters do not show a continuing monotonic retained staircase.
- Classify as sustained growth when positive private/working/Mono low-water slopes persist through the trailing ten minutes with continuing maxima or low-water steps, rather than merely one early cache step.
- If flattened: temporarily clear AutoFishing as the suspect for inactive GC growth; keep ISSUE-010 open only for an active long fishing loop.
- If sustained: do not fish. Run equivalent-duration platform layers next: game+BepInEx without DTMAPI, then Core/Bootstrap without GameBridge or official mods, and compare with full inactive DTMAPI.

## Completion evidence and decision

- Implementation and synthetic 1800-second unit coverage passed; commit `dac31cc`.
- Runtime `GAME-SMOKE/20260711-115939` passed 1800.002 seconds with 61 samples, no trimming, no fishing, zero DTMAPI structural deltas, and clean title/process exit.
- The trailing window has flat MonoHeap/UnityReserved, only +2.40 MB working-set endpoint growth, falling MonoUsed, and descending Gen2 low waters.
- A late private-memory wave returned below its trailing-window start; its positive least-squares slope is an outlier-shape artifact rather than persistent baseline growth.
- Decision: flattened/common warming. Do not run platform layers. Temporarily clear inactive AutoFishing GC suspicion; keep only active long-loop validation for the AutoFishing portion of ISSUE-010.

## 2026-07-11 stop boundary

- No further inactive/no-consumer memory test, platform-layer baseline, or extension of the 30-minute process is authorized.
- The recovered private-memory peak near 4.97 GB is classified as a common game/Unity/platform transient wave, not an AutoFishing leak.
- This closes only the inactive AutoFishing sustained-retention suspicion. It does not close ISSUE-010 or validate an active fishing loop.
- Active validation is deferred to formal-release readiness and should be limited to one approximately 10-minute or 10–20-fish trend; 100/500 remains deferred.
