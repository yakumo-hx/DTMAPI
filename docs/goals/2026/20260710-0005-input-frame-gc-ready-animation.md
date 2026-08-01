# 20260710-0005 Input Frame, GC, and Ready Animation

## Status

- Implementation: source/unit complete; native frame cadence and manual-assisted full loop verified; player manual scope passed; smoke external-key injection remains unverified.
- Fixed target version: `0.5.2-alpha`; the workspace is already at this version, so this goal does not authorize a version bump.
- Source review: `docs/reviews/manual-qa/2026/20260710-0002-autofishing-input-gc-ready-review.md`.

## Objective

Replace DTMAPI's 250ms gameplay-input fallback with a reliable main-thread per-frame input path and latched edges, bound config/owner diagnostics, remove AutoFishing hot-path formatting, and include native Ready animation/charge timing in the first-party animation lease without changing charge targets or the working automatic fishing loop.

## Tasks

### A. Reliable per-frame input driver and edge latch

- Establish one DTMAPI-owned, main-thread, scene-stable frame source at the Unity InputSystem/PlayerLoop boundary. Prove continuous cadence and lifecycle cleanup. Do not dispatch ordinary Mod Update from `TimerFallback`.
- Capture registered current-scope controls once per input frame and latch press/release generations until Core consumes them. A press+release wholly between Core frames must still dispatch exactly one KeybindPressed and one KeybindReleased.
- Sample only current-scope registered controls plus held controls needed to close releases. Capture-mode candidate scanning remains temporary and separate.
- Avoid per-frame reflection bool boxing using typed references or cached strong getter delegates. Keep fragile Unity/InputSystem code in Bootstrap, not public abstractions.
- Preserve owner cleanup, chord semantics, suppression, UI blocking, title exclusion, and long-hold single-toggle behavior.

### B. Bound config preview and owner diagnostics

- Stop storing one permanent owner-ledger entry for every successful apply/restore preview. Aggregate success by stable owner/item/kind/operation with counters and timestamps; retain only bounded recent failure detail.
- Do not copy and fully format the entire ledger for every item. Publish lifecycle/config-preview status once per preview scope/render or only on meaningful change/failure.
- Prefer rendering pending values without executing unchanged setters. If real setters remain necessary for preview callbacks, apply/restore only changed/visible requirements and keep rollback semantics.
- Add tests for repeated AutoFishing config renders/sliders showing stable retained structures and bounded status/log counts.

### C. Remove AutoFishing hot-path allocations

- Store runtime phase as enum/scalars; format it only for UI/report/log publication.
- Build movement-cancel reason only after movement exceeds the threshold.
- Replace Fast Ready per-tick `List<string>`/`ToArray`/`string.Join`/summary construction with scalar counters and lazy transition/interval diagnostics.
- Move throttle decisions before message construction where possible. Preserve failures, rejection details, cleanup evidence, and report summaries.
- Cache native/reflection access or compiled getters where safe; lifecycle invalidation must prevent stale native owners.

### D. Include Ready in animation lease

- Extend the internal first-party animation lease with a Ready multiplier while retaining CastHook and Pull components. AutoFishing passes its single configured multiplier to all three.
- On native Ready enter, snapshot and scale both reachable body and fish-rod animators. Restore on Ready exit, lease release, disable, save/title boundary, owner cleanup, exception, and shutdown.
- Scale post-backswing charge timer with the same multiplier without changing the 0..1 target. Zero charge releases input immediately and still waits for accelerated native `_isAnimationDone`.
- Keep InstantBite, SkipMiniGame, FastAnimations, and charge independent. Keep legacy `IFishingAutomationApi` compatibility behavior intact.

### E. Validation and records

- Add allocation/boundedness unit coverage for input frames/latches, config preview aggregation, product idle/active frames, and Fast Ready tick diagnostics.
- Release build/unit, JSON/PowerShell parse where touched, and `git diff --check` must pass.
- Acquire runtime lock and run fifth-save real-game validation: F6 and custom F7 short taps, held-key single toggle, default loop, Fast+charge 0/0.5/1 target preservation, Ready/Cast/Pull evidence, independent/combined options, disable/title cleanup, report export, no leftover process/fatal window.
- Update ISSUE-010, debug index if a new durable issue is added, smoke matrix, hook map, API matrix, update index/record. Do not mark ISSUE-010 solved.

## Acceptance Gates

- Runtime reports a sustained per-frame input source; fallback pump does not sample Gameplay input or dispatch ordinary Update.
- Ten physical/synthetic ~40ms F6/F7 taps produce ten toggles; a held key produces one toggle; title frames poll zero AutoFishing Gameplay buttons.
- Repeated config redraws do not grow retained ledger entries linearly and do not publish six full summaries per item.
- Warm AutoFishing frames and Fast Ready ticks avoid status-string/list allocation; failures remain diagnosable.
- Ready body/rod animation and charge timing use the configured Fast multiplier, charge distance remains correct, and all native snapshots/leases restore to zero.

## Blockers

- If the selected InputSystem/PlayerLoop callback is not guaranteed main-thread or scene-stable, do not dispatch Mod code from it; retain an edge latch and find a proven safe same-frame drain point.
- If exact per-frame allocation counters are unavailable on the game Mono runtime, use source/unit allocation gates plus bounded runtime counters and explicitly record the limitation.
- Automated Ready evidence does not replace player-visible Ready validation.

## Rollback

- The input driver/latch must be removable without restoring registered-string/local-snapshot churn; preserve the stable Core input registry.
- The diagnostic aggregation rollback must not restore unbounded preview entries.
- The Ready lease can fall back to Cast/Pull-only while preserving charge targets if native animator restoration fails, but the goal remains incomplete.

## Implemented Result

- Bootstrap now latches current-scope button edges into stable generation state and consumes each edge once. Input System bool access uses compiled getters instead of per-sample reflective boxing. A native `NormalGameState.OnUpdate` drain owns Gameplay frames; PlayerLoop owns title/non-normal fallback and stands down while the native drain is current; the 250ms timer performs health checks only.
- Config preview success is aggregated by stable owner/item/kind/operation instead of retained per call. Recent failure detail is capped at 64 and the general owner ledger at 2048. A preview scope publishes once, unchanged values are not applied/restored, and rollback continues across individual setter failures.
- First-party AutoFishing keeps phase as an enum, defers movement text until movement is real, and no longer builds Ready diagnostic lists/summaries on every tick. Fast success publication is once per flow instead of once per frame.
- The internal animation lease now carries independent Ready/CastHook/Pull multipliers. AutoFishing supplies one configured multiplier to all three; Ready snapshots/scales body and rod animators, scales the native charge timer, and restores exact prior speeds on native/lifecycle release without changing the `0..1` charge target.
- Release build and unit tests pass. Unit gates cover single-consume short-tap latch, 200-scope config aggregation/failure caps, hot Ready summary reference stability, independent multiplier clamp, and Ready speed restoration.
- Fifth-save frame evidence `GAME-SMOKE/20260710-173812` records 2394 Gameplay input frames in 6.8 seconds through the native drain, clean process exit, and no fatal window. Its external F6 sender failed before input reached the game, so it is cadence evidence only. `GAME-SMOKE/20260710-171352` separately proves current-build Ready/Cast speed and target `0.5` plus cleanup, but its scripted movement intentionally triggered native movement cancel. At that implementation point the player-visible/tap gates remained pending; the later final player retest recorded below closes them. ISSUE-010 remains open.
- The first player retest exposed one physical F6 being emitted twice 17ms apart, enabling and immediately disabling the product. The latch now accepts one source per Unity frame and suppresses repeated pressed flags within an unreleased held cycle; AutoFishing also requires a matching release before another toggle. `GAME-SMOKE/20260710-185323` is a manual-assisted F6 runtime pass—not external sender proof—and completes DefaultLoop, three visible minigames, soak, cleanup/report/exit/fatal gates with one opening press/release. `185857` behavior-verifies InstantBite + Skip + Fast combined at zero charge despite its sender result gate failure; `185537` verifies charge target 1 and Ready/Cast Fast but its full-distance fixture overshoots water. The user then manually confirmed the whole requested scope passes: AutoFishing enable/loop, independent settings, combined behavior, charge, and player-visible behavior. Only automated external-key injection and long ISSUE-010 soak gates remain outside this goal's verified functional scope.
