# 20260613-0014 AutoFishing Minigame Input And Animation Multiplier

- Date: 2026-06-13
- Status: verified
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user reported that AutoFishing still completed the minigame by a direct ~0.75s success write instead of green/red/yellow input automation, and that cast/pull animation speed still did not visibly apply or expose a 1-4 multiplier. Follow-up manual QA confirmed red/green/yellow input now works, but animation was still not visibly accelerated until the cast-hook physics owner was patched; a later follow-up clarified the click/hold Ready charge animation also needs the same multiplier.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260613-0006-autofishing-minigame-animation-follow-up.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Root Cause

- The previous `AutoCompleteVisibleMiniGame` path proved only that a visible `FishingGameScrollBar` could be forced to `Success` after about `0.75s`; it did not perform the requested green hold, red/off-note release, and yellow bonus tap input behavior.
- Native minigame scoring/result ownership is `FishingGameScrollBar.UpdateGame(float dt)`, with the six `DolocUserInput` fishing/tool/item getter paths as the native input surface.
- Ready charge ownership is `AgentStateFishingReady._castTimer` in `AgentStateFishingReady.OnPlay`: native code calls `CastTimer.Tick(Time.fixedDeltaTime)` and refreshes the power bar before `OnExit` writes progress to `FishRodRenderer.SetPower`.
- Pull animation completion is also gated by native `FishRodRenderer.Pull` / `PullCancel` returned durations and `AgentStateFishingPull._pullDuration`; animator speed writes alone can miss the visible pull completion owner.
- Visible cast travel is not owned by `AgentStateFishingCast.OnEnter` animator speed alone. `FishRodRenderer.CastHook()` resets/shows the hook, writes force/velocity through `FishRodHook`, then lets Rigidbody2D gravity drive the hook to the water collision; the first passing animation smoke observed animator/Pull-duration paths but did not prove this visible cast-flight owner.
- Old AutoFishing configs had no `AnimationMultiplier`; missing values were deserialized as `0` and clamped to `1`, so `FastAnimations=true` could become a silent normal-speed configuration.

## Summary

- Replaced delayed direct minigame success with a scoped `FishingGameScrollBar.UpdateGame` prefix plus temporary `DolocUserInput` getter overrides.
- The input policy now holds stable/green note frames, releases red/off-note and delay frames, and sends one short pressed edge for bonus/yellow notes.
- The `UpdateGame` postfix now only observes native `Success`/`Failed` and reports `AutoPlayVisibleMiniGame` evidence with stable/bonus/release frame counters.
- Added player-facing `AnimationMultiplier` next to `FastAnimations`, bounded to `1..4`, defaulting/migrating to `3`.
- Added `AgentStateFishingReady.OnPlay` charge acceleration by ticking native `_castTimer` with the extra multiplier delta and refreshing `_powerBar` progress/color.
- Added `FishRodRenderer.Pull` / `PullCancel` result scaling plus a Pull-phase `_pullDuration` fallback when the native return hook is not observed.
- Added a `FishRodRenderer.CastHook` postfix that scales hook `Velocity` by the configured multiplier and hook Rigidbody2D `gravityScale` by multiplier squared, then restores hook physics on fishing state/lifecycle exit.
- Kept FastAnimations scoped to Ready/Cast/Pull fishing phases and kept restore on Pull/base exit plus save/title/environment reset.
- Tightened the FastAnimations smoke gate so animator-only samples are no longer accepted as visible animation proof; accepted evidence must include Ready `FastReadyCharge` plus `FastCastHookPhysics` or `FastPullDuration`.

## Validation

- Passed:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - Fifth-save AutoFishing `DefaultLoop` smoke `docs/debug/evidence/GAME-SMOKE/20260613-153632`
  - Fifth-save AutoFishing `FastAnimations` smoke `docs/debug/evidence/GAME-SMOKE/20260613-154252`
  - Fifth-save AutoFishing `FastAnimations` hook-physics re-smoke `docs/debug/evidence/GAME-SMOKE/20260613-160354`
  - Fifth-save AutoFishing `FastAnimations` Ready/Cast/Pull re-smoke `docs/debug/evidence/GAME-SMOKE/20260613-163812`

- Retained failed attempt:
  - `docs/debug/evidence/GAME-SMOKE/20260613-153850` failed `AutoFishingAnimationSpeed` because missing old `AnimationMultiplier` config migrated to `1` before the normalization fix.
  - `docs/debug/evidence/GAME-SMOKE/20260613-163257` failed as a smoke-harness false negative because Ready charge evidence was overwritten by later Cast/Pull animation summaries before the old gate observed it.

## Evidence

- `GAME-SMOKE/20260613-153632/result.json` records `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `GAME-SMOKE/20260613-153632/DTMAPI-latest.log` records `behavior=AutoPlayVisibleMiniGame`, `status=Success`, `stableHoldFrames=82`, `releaseFrames=49`, and `lastNote=Stable`.
- `GAME-SMOKE/20260613-154252/result.json` records `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, `DiagnosticsReportExport=Passed`, `AutoFishingReportExport=Passed`, and clean exit/fatal checks, but this evidence is retained as incomplete for visible cast speed because it did not observe hook physics.
- `GAME-SMOKE/20260613-154252/DTMAPI-latest.log` records Cast samples `source.body.animator:1->3;source.body.fishRodRenderer._animator:1->3`, Pull samples with the same `1->3` multiplier, and `FishRodRenderer.Pull` duration `0.104->0.035`.
- `GAME-SMOKE/20260613-154252/latest-report.txt` points to `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-154345.zip`; `process-check.txt` says no `DolocTown.exe` process was found and `fatal-window-check.txt` says no fatal instance popup was found.
- User manual QA confirmed red/green/yellow minigame handling succeeds.
- `GAME-SMOKE/20260613-160354/result.json` records `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, `DiagnosticsReportExport=Passed`, `AutoFishingReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `GAME-SMOKE/20260613-160354/DTMAPI-latest.log` records `behavior=FastCastHookPhysics`, `source=FishRodRenderer.CastHook`, `multiplier=3`, `hook.Velocity:(18.55,29.68)->(55.65,89.04)`, `hook.gravityScale:12->108`, later `FishRodRenderer.Pull` duration `0.147->0.049`, and `Experimental fishing animation state restored reason=AgentStateBase.OnExit animators=2 hookPhysics=1`.
- `GAME-SMOKE/20260613-160354/latest-report.txt` points to `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-160443.zip`; `process-check.txt` says no `DolocTown.exe` process was found and `fatal-window-check.txt` says no fatal instance popup was found.
- Unit coverage verifies stable hold, delay release, first bonus/yellow tap, and bonus no-repeat decisions; manual QA has now also confirmed all three visible colors.
- Unit coverage verifies stable hold, delay release, first bonus/yellow tap, bonus no-repeat decisions, and Ready charge speed ticking native `_castTimer` by `(multiplier-1)*fixedDeltaTime` while refreshing the native progress circle; manual QA has confirmed all three visible minigame colors.
- `GAME-SMOKE/20260613-163812/result.json` records `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, `DiagnosticsReportExport=Passed`, `AutoFishingReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `GAME-SMOKE/20260613-163812/DTMAPI-latest.log` records `FastReadyCharge` from `AgentStateFishingReady.OnPlay` with `castTimer.Progress:0->0.04;extraDt=0.04;powerBar.updated=2`, `FastCastHookPhysics` with `hook.Velocity:(22.366,35.786)->(67.098,107.357)` and `hook.gravityScale:12->108`, `FishRodRenderer.Pull` duration `0.117->0.039`, and final smoke summary `readyCharge=True, fastAnimation=True`.
- `GAME-SMOKE/20260613-163812/latest-report.txt` points to `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-163900.zip`; `process-check.txt` says no `DolocTown.exe` process was found and `fatal-window-check.txt` says no fatal instance popup was found.

## Rollback

- If the native minigame input override causes player input leakage, disable only the `DolocUserInput` getter override path and keep the fifth-save fixture/report evidence for diagnosis; do not restore direct `currentGameStatus=Success` as acceptance behavior.
- If Pull acceleration regresses native fish collection, revert the `FishRodRenderer.Pull` / `PullCancel` duration scaling first while keeping Cast/Pull animator speed restore paths intact.
- If cast hook acceleration overshoots fishable collisions, revert only the `FishRodRenderer.CastHook` hook-physics scaling while retaining the stricter smoke gate and diagnostics.
