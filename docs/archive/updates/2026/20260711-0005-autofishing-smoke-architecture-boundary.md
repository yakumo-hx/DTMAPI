# 20260711-0005 — AutoFishing smoke 与架构边界隔离

## Status

`source-and-unit-verified / primitive-smoke-isolated / legacy-compatibility-island-gated / no-runtime-run / issue-010-open`

No public API, Hook target, AutoFishing behavior/configuration, version, manifest, DeveloperOnly state, or release metadata changed.

## Source request

After the 30-minute inactive curve flattened, the user stopped further inactive/no-consumer, platform-layer, extended-duration, and active-fishing runtime work. The remaining task was to prove in source and smoke architecture that the old service is a compatibility island, and to preserve AutoFishing's one reliable-first-edge registration as an explicit input-contract exception.

- Review: `docs/reviews/api/2026/20260711-0005-autofishing-smoke-architecture-boundary-review.md`.
- Goal: `docs/goals/2026/20260711-0005-autofishing-smoke-architecture-boundary.md`.

## Implementation

- The normal AutoFishing smoke route is primitive-only. It fails when the first-party product does not acquire a primitive session; it no longer falls back to `LegacyFishingAutomationService` or merges legacy counters into product results.
- `AutoFishingPrimitiveSmokeCase` owns the first-party loop and movement-cancel entry points.
- `LegacyFishingAutomationCompatibilitySmokeCase` independently verifies the frozen API sequence: policy-only `Configure`, legacy activation only through obsolete `SetEnabled(true)`, disable, owner cleanup, and zero retained state.
- Both cases use a neutral `FishingSmokeCaseResult`, `FishingSmokeCleanupSnapshot`, and `FishingSmokeCleanupAssertions`. The existing general performance probe/result remains shared diagnostic infrastructure.
- The primitive smoke source contains no `IFishingAutomationApi`, `LegacyFishingAutomationService`, or `FishingAutomationFeature.Service` reference.
- The legacy compatibility case is opt-in through `AutoExerciseLegacyFishingCompatibility`; it is not part of normal product smoke and was not run in this task.
- `run-game-smoke.ps1 -AutoExerciseLegacyFishingCompatibility` serializes the separate setting/result gate and rejects combining it with the primitive phase smoke.

## Changed files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`: removed all legacy fallback/count/cleanup branches from the primitive core.
- `AutoFishingPrimitiveSmokeCase.cs`, `LegacyFishingAutomationCompatibilitySmokeCase.cs`, and `FishingSmokeShared.cs`: separate case entry points plus neutral result/cleanup contracts.
- `SmokeHarness.cs` and `tools/scripts/run-game-smoke.ps1`: separate opt-in compatibility setting/result route.
- `tests/DTMAPI.UnitTests/Program.cs`: architecture and input-lifetime source gates.
- `docs/design/smapi-like-input-lifetime-contract.md`, API/debug/smoke/ISSUE-010 records, and the 0004 interpretation: durable boundary and stop decisions.

## Architecture gates

The unit runner now scans and fails on boundary regression:

- AutoFishing product source cannot reference Harmony, reflection, raw native fishing state names, or the legacy API.
- DTMAPI Core cannot contain AutoFishing/Fishing product names.
- ordinary first-party/test mods cannot consume `IFirstPartyFishingPrimitivesApi`; the internal friend boundary remains explicit for AutoFishing.
- primitive session/runtime and primitive smoke cannot reference the legacy service/facade.
- the single legacy constructor remains inside `FishingAutomationFeature`, with one activation call from compatibility `SetEnabled(true)`.
- AutoFishing retains exactly one `RegisterKeybind`; Zoom retains local `DtmKeybindList.JustPressed(helper.Input)` and no persistent registration.

## Input contract

Added `docs/design/smapi-like-input-lifetime-contract.md`: local keybind queries are the ordinary-mod default; AutoFishing's one owner-bound Gameplay toggle is the deliberate bounded exception because reliable delivery of the first short edge is a product requirement. Manual movement fallback remains local, native `HorizontalMoveFactor` stays preferred, and registration never restores per-registration native polling.

## Validation

- Release solution build: 0 warnings, 0 errors.
- Complete unit runner: `DTMAPI.UnitTests: OK`.
- New architecture gates: passed.
- `git diff --check`: passed with line-ending notices only.
- No runtime lock was acquired. Doloc Town was not installed, launched, or tested. No inactive memory, platform layer, active fish, 100/500, soak, or extended-duration run occurred.

## ISSUE-010 and prior evidence

- ISSUE-010 stays open.
- Only the suspicion of AutoFishing inactive sustained retention is closed by the existing `GAME-SMOKE/20260711-115939` evidence.
- The recovered near-4.97-GB private-memory wave is classified as a common game/Unity/platform transient, not an AutoFishing leak.
- Active fishing-loop trend validation remains deferred to formal-release readiness: one approximately 10-minute or 10–20-fish run, not 100/500.

Related records: smoke row `AUTOFISHING-SMOKE-ARCHITECTURE-BOUNDARY-20260711`, public API matrix 0005 note, Debug Index 0005 note, and ISSUE-010's 2026-07-11 stop boundary. No new runtime evidence directory exists by design.

## Rollback

Revert this update's source changes to restore the mixed smoke fallback. No player/runtime data migration is needed. The architecture gates and input specification should be reverted together if the old coupling is intentionally restored.
