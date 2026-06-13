# 20260613-0005 AutoFishing Native Loop Rewrite

- Date: 2026-06-13
- Status: implemented; fifth-save AutoFishing scenarios verified
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user requested the approved AutoFishing native rewrite and fifth-save smoke migration.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/manual-qa/2026/20260613-0005-autofishing-native-loop-fifth-save-review.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Summary

- Replaced the experimental AutoFishing option shape with native-stage strategies: bite wait mode, result mode, animation mode, recast delay, movement cancel, and animation multiplier.
- Reworked AutoFishingMod so F6 defaults to the full player-requested loop: auto cast, native wait, auto reel, visible minigame auto-complete, native collect, and recast.
- Kept only three player-facing switches: `InstantBite`, `SkipMiniGame`, and `FastAnimations`.
- Routed `InstantBite` as a wait-only strategy; it rolls a native bite and then uses the same native post-bite result route as normal play.
- Routed `SkipMiniGame` through the native skip-minigame result path without forcing Pull success/failure fields.
- Limited `FastAnimations` to Cast/Pull phase hooks and kept lifecycle restoration on Pull/base exit plus save/title/environment reset.
- Rewrote AutoFishing smokes to require explicit fifth-save fixture runs and to observe real native phases instead of creating pools, writing `FishingCache`, or overwriting into Wait state.
- Fixed the post-bite reel path found by real fifth-save testing: when `AgentStateFishingWait.NextState()` returns `this` because there is no player input edge, the bridge mirrors the same native responsibility branch by checking `FishingCache.FishProto`, `_fishOnHookDuration`, native skip-game state, native fishing energy cost, and then routing to `AgentStateFishingBattle` or `AgentStateFishingPull`.
- Added monotonic InstantBite/SkipMiniGame smoke counters so scenario evidence survives later phase summaries such as AutoCast.

## Validation

- Passed:
  - `git diff --check` (line-ending warnings only)
  - PowerShell AST parse for `tools/scripts/run-game-smoke.ps1`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - DirectExe HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-095209`
  - Fifth-save AutoFishing `DefaultLoop` smoke `docs/debug/evidence/GAME-SMOKE/20260613-102412`
  - Fifth-save AutoFishing `InstantBite` smoke `docs/debug/evidence/GAME-SMOKE/20260613-103216`
  - Fifth-save AutoFishing `SkipMiniGame` smoke `docs/debug/evidence/GAME-SMOKE/20260613-103328`
  - Fifth-save AutoFishing `FastAnimations` smoke `docs/debug/evidence/GAME-SMOKE/20260613-103427`
  - Fifth-save AutoFishing `CombinedInstantSkip` smoke `docs/debug/evidence/GAME-SMOKE/20260613-103531`
  - Fifth-save AutoFishing `CombinedInstantComplete` smoke `docs/debug/evidence/GAME-SMOKE/20260613-103621`

## Evidence

- Unit coverage added for default native-stage strategy normalization, skip result preservation, missing-input-edge native reel mirroring, and Cast/Pull-only animation speed.
- Early blocker evidence `docs/debug/evidence/GAME-SMOKE/20260613-094928` is retained as the rejected fixture state: slot 5/index 4 was loaded but selected `DolocTown.ItemTool`.
- After the fifth save was updated to hold `carbon_fishrod`, `docs/debug/evidence/GAME-SMOKE/20260613-102412/result.json` records `RunStatus=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- The passed DefaultLoop log records real fifth-save `BodyController.UseFishRod` auto-cast with `rod=carbon_fishrod`, native bite-ready fish including `rainbow_trout`, `carp`, and `shrimp`, visible `FishingGameScrollBar` success after about `0.76s`, `AgentStateFishingPull`, and next AutoCast evidence.
- The scenario smokes verify `InstantBite`, `SkipMiniGame`, `FastAnimations`, `CombinedInstantSkip`, and `CombinedInstantComplete` with clean process/fatal checks. No `DolocTown.exe` process remained after the final run.

## Rollback

- Revert this update if the native-loop rewrite blocks fish-ready fifth-save play. Do not restore the old synthetic AutoFishing smoke as acceptance evidence; it can be kept only as a legacy diagnostic helper if separately named.
