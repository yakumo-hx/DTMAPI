# 20260711-0001 AutoFishing Reliability and 0.5.3 Preview

## Status

- Source, unit, allocation-microtest, parser, metadata, three-fish, and two bounded baseline validations completed.
- Runtime/API preview sources are aligned to `0.5.3-alpha` / `0.5.3.0`; AutoFishing remains `1.4.3-dtmapi`, DeveloperOnly, unpublished.
- No 100/500-fish matrix or general long-gameplay soak was run. `ISSUE-010` remains open.

## Source Request

Implement `docs/goals/2026/20260711-0001-autofishing-reliability-053-preview.md` from review `docs/reviews/api/2026/20260711-0001-autofishing-reliability-version-freeze-review.md`: make the visible-reel edge retryable, fail closed on incomplete Fishing Hooks, make primitive activation transactional, remove animation/Hook-physics reflection and per-stage allocation, freeze the legacy Experimental API, align the 0.5.3 preview line, and run only three single-fish cases plus two ten-minute baselines.

## Changes

- Added a separate fake-clock-testable visible-reel state. Its 250 ms edge is rearmed when unconsumed, waits 500 ms for native phase confirmation after consumption, and times out after five seconds to `Interrupted` without forcing `NextState` or `Overwrite`. MiniGame/Pull, movement cancel, disable, title return, and release clear it.
- Limited visible-reel consumption to `NormalUseTool`, `NormalUseItem`, and `NormalFishing`; added queued, consumed, retry, and timeout counters without per-frame retry logging.
- Made primitive acquisition require the complete `FishingAutomationHookBridge.HooksReady` set. Pending, incomplete, and activation-failure states return `hooks-pending`, `hooks-unavailable`, or `activation-failed`; `TryCast` also rejects a later readiness loss.
- Made primitive activation transactional through an injectable Hook coordinator and activation checkpoints. The candidate runtime, primitive attachment, and Hook callback are rolled back in reverse order; `activeSession` is committed last. Process-scoped Harmony patches remain installed while DTMAPI runtime roots are detached.
- Added `FishingAnimationNativeCache` with typed cached access for agent/body/rod/animator, Animator speed, Cast Hook velocity and gravity, Pull duration, and component lookup. Candidate names are static, candidate buffers are reused and reference-deduplicated, active stages do not invoke reflection or allocate temporary LINQ/array state, and restore iterates snapshots directly before `Clear`.
- Extended accessor telemetry to include the animation cache and retained the separate Builds, Rebuilds, BuildFailures, and InvocationFailures classes.
- Preserved the first-party internal Fishing Primitives boundary. The ordinary product path still does not construct `LegacyFishingAutomationService` or populate legacy owner maps.
- Marked `IFishingAutomationApi` and its related public fishing option/state types Obsolete without making the warning an error. The compatibility adapter warns once per owner and is frozen: no new behavior, no removal version promise, and no repository product consumers.
- Aligned runtime/API/binary/release/install/official metadata to `0.5.3-alpha` / `0.5.3.0`. AutoFishing minimum dependencies now target that preview while its product version remains `1.4.3-dtmapi`; it was not added to the formal published set.
- Extended the smoke/performance probe with visible-reel counters, release reason, native `HorizontalMoveFactor` availability, accessor deltas, fishing hot-log counts, runtime/native/legacy counters, and ReturnedToTitle cleanup.
- Corrected bounded-smoke tooling found during validation: native `QuickDeselectCurrentItem` does not clear `SelectedItem`, so the no-rod fixture temporarily selects an empty/non-rod quick slot; external movement uses scan-code `SendInput`; and performance baselines no longer incorrectly require the standard diagnostics report export.

## Main Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingVisibleReelInputState.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitiveHookRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAnimationNativeCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAnimationController.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/FishingCompatibilityController.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPerformanceProbe.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/install-to-game.ps1`
- `Directory.Build.props`
- `tests/DTMAPI.UnitTests/Program.cs`

## Automatic Validation

- Release build: passed with 0 warnings and 0 errors.
- Full unit runner: passed with `DTMAPI.UnitTests: OK`.
- Fake-clock tests cover 251 ms rearm, 500 ms consumed confirmation window, retry after absent progress, five-second timeout, and phase/lifecycle cleanup.
- Hook readiness and every activation checkpoint failure leave callback runtime, feature runtime, primitive attachment, session, leases, scheduler, and native references at zero; a subsequent acquire succeeds.
- Warmed 10,000 Ready-CastHook-Pull-Restore cycles allocate zero bytes on the measured thread, build typed accessors once, report no rebuild/failure/fallback, and restore animator speed, velocity, gravity, and duration exactly.
- Repository consumer scan permits legacy API references only in compatibility implementation, contract declarations, documentation, and tests.
- Windows PowerShell parser checks passed for `run-game-smoke.ps1`, `install-to-game.ps1`, and `release-common.ps1`.
- Version/package metadata consistency tests passed. `git diff --check` passed with line-ending warnings only.

## Limited Runtime Evidence

All final runs used the shared runtime lock, fifth save, independent processes, title return, clean exit, and no residual `DolocTown.exe`.

1. Natural wait, visible minigame, Fast off, charge 0: `docs/debug/evidence/GAME-SMOKE/20260711-011246` passed one PullExit, typed disable, zero accessor failures, and title cleanup.
2. InstantBite plus SkipMiniGame, Fast off, charge 0: `docs/debug/evidence/GAME-SMOKE/20260711-011805` passed one native skipped result, zero input lease after completion, disable, and title cleanup.
3. Natural wait, visible minigame, Fast x4, charge 1: `docs/debug/evidence/GAME-SMOKE/20260711-012735` passed full-charge Ready/Cast/Pull acceleration, restoration, disable, and title cleanup.
4. `InactiveNoConsumer`, 60-second warm-up plus 600-second measurement: `docs/debug/evidence/GAME-SMOKE/20260711-012851/DTMAPI-evidence/AUTO-FISHING-PERF/20260711-014032/auto-fishing-performance.json` completed with 0 cast/fish, 9 Gen0 collections, 0 Fishing hot-log lines, accessor count `0 -> 0`, all runtime/native/legacy counters zero, and complete title cleanup. The outer wrapper's Failed result was a false diagnostics-report requirement for baselines; that verifier was corrected.
5. `EnabledNoRod`, 60-second warm-up plus 600-second measurement: `docs/debug/evidence/GAME-SMOKE/20260711-024427/DTMAPI-evidence/AUTO-FISHING-PERF/20260711-025609/auto-fishing-performance.json` and its enclosing run passed with 0 cast/fish, 9 Gen0 collections, 0 Fishing hot-log lines, accessor count `4 -> 4`, zero rebuild/failure deltas, legacy maps zero, native movement cancellation `manual-move HorizontalMoveFactor=-1`, and complete title cleanup.

Evidence correction: follow-up `20260711-0002` proved that Unity Mono's present `GC.GetAllocatedBytesForCurrentThread` method is a nonfunctional stub in this runtime: a kept-alive 4096-byte same-thread allocation left it at `0 -> 0`. The historical `AllocatedBytes=0` fields above are invalid and must not be cited as zero-allocation proof. The nine Gen0 values show collection activity only; all other listed baseline invariants remain usable.

The Instant+Skip route initially exposed a real missing scoped use-tool edge before the skip transaction and was fixed before final evidence. Other retained attempts were smoke expectation, fixture, parameter, regex, or external sender diagnostics and are not product-failure evidence.

## Public API and Release Boundary

- No public Fishing Primitives were introduced. `IFirstPartyFishingPrimitivesApi`, snapshots, sessions, and leases remain `internal` to first-party consumers through the existing friend assembly boundary.
- `IFishingAutomationApi` remains Experimental for compatibility but is now Deprecated/Frozen. It receives no new capability.
- Legacy deletion requires a separate breaking goal after the version-system project, a zero known-consumer scan, migration guidance, and at least one actually published preview cycle carrying the warning.
- This work establishes preview metadata only. It does not formally publish DTMAPI or AutoFishing.

## Rollback

Rollback visible-reel retry/readiness/transaction changes together so a partially attached runtime cannot be reintroduced. Roll back `FishingAnimationNativeCache` together with its controller call sites and telemetry. Version/manifest/release-script changes must also move as one coherent release line; do not leave AutoFishing minimum dependencies ahead of or behind the runtime metadata.

## Follow-up

- Keep `ISSUE-010` open.
- Defer the isolated 100/500-fish allocation matrix and arbitrary long-gameplay soak to a separate runtime goal.
- Keep the legacy service isolated and frozen; do not split or delete it in this preview line.
