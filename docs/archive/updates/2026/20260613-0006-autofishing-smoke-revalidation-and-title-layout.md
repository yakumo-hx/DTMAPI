# 20260613-0006 AutoFishing Smoke Revalidation And Title Layout

- Date: 2026-06-13
- Status: verified for fifth-save AutoFishing smokes and title homepage smoke; pause-menu visual confirmation still manual
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user confirmed the fifth save now enters game with the fishing rod selected, then requested continued audit/fix of AutoFishing and title/pause layout behavior.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Fixed AutoFishing fifth-save smoke false negatives where the first auto-cast had already advanced into Cast/Wait/Battle/Pull before the smoke baseline was captured.
- Fixed a second FastAnimations smoke false negative where the latest automation summary was overwritten by `FastAnimation` evidence after the successful AutoCast.
- Kept AutoFishing runtime semantics unchanged from `20260613-0005`: F6 default loop, InstantBite wait-only, SkipMiniGame native-result routing, and FastAnimations Cast/Pull-only.
- Updated native UI layout diagnostics so active `HomePageUiState.Update` can correct stale `HomePageTextMenu` constraint count back to one column, and active `MainMenuUiState.Update` can correct the actual pause `MenuUI` to the visible-icon-count row without broad title/pause polling.
- Revalidated the title homepage icon/menu path with screenshot smoke after the owner-state layout repair. Pause-menu cycling still needs user visual confirmation because the automated smoke does not open and observe the in-save pause menu after the delayed repro window.

## Validation

- Passed:
  - `git diff --check` (line-ending warnings only)
  - `tools/scripts/test.ps1 -Configuration Release`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/install-to-game.ps1 -Configuration Release`
  - Title homepage/settings smoke `docs/debug/evidence/GAME-SMOKE/20260613-114113`
  - Fifth-save AutoFishing `DefaultLoop` smoke `docs/debug/evidence/GAME-SMOKE/20260613-112900`
  - Fifth-save AutoFishing `InstantBite` smoke `docs/debug/evidence/GAME-SMOKE/20260613-113127`
  - Fifth-save AutoFishing `SkipMiniGame` smoke `docs/debug/evidence/GAME-SMOKE/20260613-113240`
  - Fifth-save AutoFishing `FastAnimations` smoke `docs/debug/evidence/GAME-SMOKE/20260613-113654`
  - Fifth-save AutoFishing `CombinedInstantSkip` smoke `docs/debug/evidence/GAME-SMOKE/20260613-113824`
  - Fifth-save AutoFishing `CombinedInstantComplete` smoke `docs/debug/evidence/GAME-SMOKE/20260613-113937`
  - DirectExe HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-114850`

## Evidence

- `GAME-SMOKE/20260613-112900` records `SaveLoaded slot/index=4`, real pool `freshwater_forest`, selected `carbon_fishrod`, `BodyController.UseFishRod`, native wait, `NativeBiteReady`, visible `FishingGameScrollBar` success, `AgentStateFishingPull`, cooldown, and next AutoCast.
- `GAME-SMOKE/20260613-113240` records `action=SkipMiniGameNativeResult` and `autoHook=AgentStateFishingPull` without minigame completion evidence, as expected.
- `GAME-SMOKE/20260613-113654` records Cast and Pull animation-speed evidence with animator samples and a completed native loop.
- `GAME-SMOKE/20260613-113937` records `InstantBite`, visible minigame auto-complete, Cast/Pull fast animation, and next AutoCast in the combined-complete route.
- All latest AutoFishing smoke result files record `RunStatus=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Title smoke `GAME-SMOKE/20260613-114113` passed title button/menu screenshot checks and clean exit.
- HookProbe smoke `GAME-SMOKE/20260613-114850` passed `HookProbe`, `SaveLoaded`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.

## Rollback

- If the updated AutoFishing smoke gate creates a false pass, revert only the `AutoFishingSmokeCase` baseline/summary check changes; do not revert the native-loop runtime rewrite from `20260613-0005`.
- If user manual QA still sees pause icon cycling, keep `UI.NativeLayoutDiagnostics` evidence and narrow the next fix to the concrete native writer shown in logs rather than restoring broad periodic layout polling.
