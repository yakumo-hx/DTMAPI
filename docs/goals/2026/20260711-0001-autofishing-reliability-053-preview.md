# 20260711-0001 AutoFishing Reliability and 0.5.3 Preview

## Status

- Implementation: complete.
- Automatic validation: passed.
- Limited runtime validation: passed for three independent single-fish cases and both bounded 60-second-warm-up plus 600-second baselines.
- Release boundary: preview metadata aligned; no formal DTMAPI or AutoFishing publication.
- Long-run boundary: 100/500 fish and arbitrary gameplay soak remain deferred; `ISSUE-010` remains open.
- Parent review: `docs/reviews/api/2026/20260711-0001-autofishing-reliability-version-freeze-review.md`.

## Objective

Make first-party AutoFishing robust to stalled visible-reel frames and partial/failed Hook activation, remove per-stage animation reflection/allocation, freeze the legacy Experimental compatibility API, and establish the `0.5.3-alpha` developer-preview line without publishing AutoFishing.

## Required Changes

1. Add a separate injectable-clock visible-reel state with 250 ms edges, native-frame retries, 500 ms post-consumption confirmation, a five-second timeout to `Interrupted`, bounded counters, and lifecycle cleanup.
2. Make primitive acquisition require the complete current fishing Hook set and defend `TryCast` against lost readiness.
3. Make activation transactional with injectable Hook coordination and failure checkpoints; commit `activeSession` last and roll back every runtime/session/native attachment.
4. Add `FishingAnimationNativeCache` for typed agent/body/rod/animator, speed, hook velocity/gravity, pull duration, and component access. Remove active-stage reflection, LINQ, temporary collections/arrays, boxed vector mutation, and restore snapshots.
5. Add fake-clock/readiness/rollback tests and a 10,000 Ready-CastHook-Pull-Restore zero-allocation warm-path test with exact native restoration.
6. Align runtime, API, binary, release scripts, installer, metadata, and tests to `0.5.3-alpha` / `0.5.3.0`.
7. Keep AutoFishing at `1.4.3-dtmapi`, DeveloperOnly, and unpublished; align its minimum dependencies and official/temporary metadata.
8. Mark `IFishingAutomationApi` and related public types Obsolete and document them Deprecated/Frozen. Warn once per compatibility owner and add a repository consumer guard.

## Automatic Acceptance

- At 251 ms an unconsumed edge is rearmed; a consumed edge is not repeated within 500 ms; absence of phase confirmation rearms it; five seconds produces `Interrupted`; phase/release/title cleanup removes pending state.
- Pending or incomplete Hooks create no session and allow no cast. Complete Hooks acquire successfully.
- Every injected activation checkpoint failure leaves callback runtime, feature runtime, primitive attachment, session, lease, scheduler, and native references at zero; the next acquire succeeds.
- After warm-up, 10,000 animation stage cycles allocate zero bytes on the current thread, build accessors once, use no fallback, and restore animator speed, velocity, gravity, and duration exactly.
- Compatibility behavior remains available but frozen, and repository products do not consume `IFishingAutomationApi`.
- Release build, complete unit runner, PowerShell parser, version/package consistency, and `git diff --check` pass.

## Limited Runtime Acceptance

Use the shared runtime lock, fifth save, independent cold processes, and clean exits.

1. Natural wait, visible minigame, no Fast, charge 0, one PullExit, typed F6 disable.
2. InstantBite plus SkipMiniGame, no Fast, charge 0, one PullExit, typed F6 disable, input lease zero.
3. Natural wait, visible minigame, Fast x4, charge 1, one PullExit, typed F6 disable, exact restoration.
4. `InactiveNoConsumer`: 60-second warm-up plus 600-second measurement with no enable.
5. `EnabledNoRod`: QuickDeselect, typed enable, 60-second warm-up plus 600-second measurement, zero cast/fish, native movement cancellation, restore selection.

Every fish run requires zero accessor failures, zero visible-reel pending, zero legacy maps, and full title cleanup. Every baseline records profile, cumulative thread allocation, Gen0, log bytes/lines, accessor start/end/delta, cast/fish counts, all runtime/native counters, and title cleanup; repeated measured Fishing hot logs, accessor deltas, or retained final objects fail. Allocation API absence marks the run blocked rather than substituting `GetTotalMemory`.

## Exclusions

- Do not run 100/500 fish or another soak.
- Do not delete or mechanically split the legacy service.
- Do not expose Fishing Primitives publicly.
- Do not formally publish AutoFishing.
- Keep `ISSUE-010` open.

## Legacy Removal Gate

No fixed removal version is promised. Deletion requires a separate breaking goal after the version-system project, zero consumer scan, migration documentation, and at least one preview cycle carrying the Obsolete warning.

## Completion Evidence

- Release build passed with 0 warnings/errors; full unit runner printed `DTMAPI.UnitTests: OK`; PowerShell parser, metadata consistency, consumer scan, allocation microtests, and `git diff --check` passed.
- Single-fish fifth-save runs: natural visible minigame `GAME-SMOKE/20260711-011246`; Instant+Skip `GAME-SMOKE/20260711-011805`; full-charge Fast x4 `GAME-SMOKE/20260711-012735`.
- `InactiveNoConsumer` accepted performance JSON: `GAME-SMOKE/20260711-012851/DTMAPI-evidence/AUTO-FISHING-PERF/20260711-014032/auto-fishing-performance.json`. It completed the full measurement and passed its performance invariants; the outer wrapper's unrelated diagnostics-export false positive was fixed afterward.
- `EnabledNoRod` final run and JSON: `GAME-SMOKE/20260711-024427`, including 600-second measurement, 0 cast/fish, stable accessors, no hot Fishing logs, native `HorizontalMoveFactor=-1` cancellation, and full title cleanup.
- Evidence correction: follow-up `20260711-0002` proved Unity Mono's resolved allocation counter is nonfunctional. The historical baselines do not prove zero thread allocation; their non-allocation invariants remain accepted.
- Update record: `docs/updates/2026/20260711-0001-autofishing-reliability-053-preview.md`.
