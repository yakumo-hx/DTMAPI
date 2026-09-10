# AutoFishing Reliability, Animation Hot Path, and 0.5.3 Preview Review - 2026-07-11

## Decision

Fix the four deterministic primitive-runtime defects before collecting more runtime evidence: visible-reel edge loss, incomplete Hook activation, non-transactional session activation, and stage-scoped animation reflection/allocation. Keep Fishing Primitives first-party `internal`, freeze the legacy Experimental API, and align the runtime preview line at `0.5.3-alpha` without formally publishing AutoFishing.

## Confirmed Findings

1. Visible minigame reel input is currently a single 250 ms override. `TryReel` reports success without a sequence transition, so the product will not retry if a frame stall outlives that edge. This can leave the native state at BiteReady.
2. `FishingAutomationHookBridge` computes the complete fishing Hook set, but `FishingPrimitiveHookRuntime.SetFishingHooksInstalled` discards it. A partially patched game can therefore acquire a session and cast without a reliable observation path.
3. Primitive activation currently exposes feature/runtime/session references before Hook activation completes. An exception can retain callback or primitive roots and make the next acquire conflict.
4. Ready/Cast/Pull animation and Hook-physics paths still use reflection, temporary lists, LINQ, arrays, and boxed vector mutation per fishing stage. This is transient GC pressure proportional to fish count even though it is not a retained-state leak.
5. No repository product outside the compatibility implementation and tests needs `IFishingAutomationApi`. Deletion is still premature because a warning-bearing preview cycle and migration/version-system gates have not completed.

## Required Reliability Contract

- A visible-reel edge lasts 250 ms, is retried on a native frame if unconsumed, waits 500 ms for phase confirmation once consumed, and gives up after five seconds by entering `Interrupted` with the existing product backoff.
- Only `NormalUseTool`, `NormalUseItem`, and `NormalFishing` may consume that edge. MiniGame/Pull, movement cancellation, disable, title return, and release clear it.
- Primitive acquire is fail-closed: no patcher is `hooks-pending`; an incomplete installed set is `hooks-unavailable`; activation exceptions are `activation-failed`.
- `activeSession` is the transaction commit point. Every earlier attachment must have reverse rollback, while installed Harmony patches may remain process-scoped.
- Animation and Hook physics use cached typed delegates after warm-up. A failed capability is latched unavailable; active stages do not fall back to reflective invocation.

## Version and Compatibility Decision

- Runtime/API preview: `0.5.3-alpha`; binary/file version: `0.5.3.0`.
- AutoFishing remains `1.4.3-dtmapi`, DeveloperOnly, and not formally published. Its minimum DTMAPI dependencies and temporary/official metadata are aligned to this preview line.
- `IFishingAutomationApi` and its related public options/state types become Obsolete, Deprecated/Frozen in documentation, and receive no new capability.
- There is no promised removal version. A later breaking goal may delete the legacy API only after the version-system project is complete, repository and known consumer scans are zero, migration guidance exists, and at least one warning-bearing preview release cycle has elapsed.

## Validation Boundary

Automatic validation covers fake-clock edge retry, Hook readiness, every activation rollback checkpoint, a 10,000-stage typed-animation allocation microtest, legacy consumer scanning, Release build, full units, PowerShell parsing, metadata consistency, and diff checks.

Runtime validation is deliberately bounded to three independent fifth-save single-fish processes and two independent fifth-save ten-minute measurement baselines after 60-second warm-up. It does not include 100/500 fish, another soak, or a formal release. `ISSUE-010` remains open.

## Rejected Directions

- Do not force native state with `NextState` or `Overwrite` to recover a missed visible-reel edge.
- Do not share the visible-reel state object with minigame note input.
- Do not permit primitive acquisition with a partial Hook set.
- Do not mechanically split or delete `LegacyFishingAutomationService` in this goal.
- Do not infer long-run allocation stability from the bounded runtime runs.

## Implementation Result

- The four confirmed defects were fixed: visible-reel input now has consumption-aware retry/timeout, primitive acquire is gated by the complete Hook set, activation has reverse transactional rollback, and Ready/Cast/Pull animation plus Hook physics use a typed cache after warm-up.
- Fake-clock, readiness, injected-checkpoint rollback, and 10,000-stage allocation/restoration tests pass. Release builds with zero warnings/errors, the complete unit runner prints `DTMAPI.UnitTests: OK`, PowerShell parsers and version metadata checks pass, and `git diff --check` passes with line-ending warnings only.
- Three independent fifth-save single-fish runs passed the natural visible-minigame, Instant+Skip, and full-charge Fast x4 paths: `GAME-SMOKE/20260711-011246`, `GAME-SMOKE/20260711-011805`, and `GAME-SMOKE/20260711-012735`.
- The bounded `InactiveNoConsumer` performance JSON under `GAME-SMOKE/20260711-012851` and the complete `EnabledNoRod` run `GAME-SMOKE/20260711-024427` each measured 600 seconds after 60-second warm-up with zero cast/fish, zero Fishing hot-log lines, no accessor delta, and full title cleanup. The inactive run's outer wrapper incorrectly required a standard diagnostics export; that verifier defect was fixed and the completed performance JSON is the accepted evidence.
- Evidence correction 2026-07-11: the two JSON files' `AllocatedBytes=0` values are not valid allocation evidence. Follow-up `20260711-0002` behaviorally calibrated Unity Mono with a kept-alive 4096-byte array and proved the resolved counter is nonfunctional (`0 -> 0`). Retain only the cast/fish, accessor, log, movement, and cleanup conclusions from those baselines; the nine Gen0 collections do not identify a per-thread byte count.
- Native QuickDeselect did not itself clear the selected quick-slot item. The final no-rod fixture therefore preserves the native QuickDeselect call but temporarily selects an empty/non-rod slot and restores the original selection after native `HorizontalMoveFactor` cancellation.
- `IFishingAutomationApi` is now Obsolete and documented Deprecated/Frozen; AutoFishing stays `1.4.3-dtmapi`, DeveloperOnly, and unpublished. No public Fishing Primitives were added.
- This result does not change the validation boundary: no 100/500-fish matrix or arbitrary long-gameplay soak ran, and `ISSUE-010` remains open.
