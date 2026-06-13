# 20260613-0016 AutoFishing Cast Charge Setting

- Date: 2026-06-13
- Status: verified
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user requested a new AutoFishing charge-amount setting: default should be no charge, adjustable up to full charge, and `FastAnimations` should also accelerate the charge phase.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260613-0006-autofishing-minigame-animation-follow-up.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Root Cause

- Native cast charge is owned by `AgentStateFishingReady`: `OnPlay` advances its `RedSaw.CastTimer`, `NextState` keeps the player in Ready while `DolocUserInput.NormalUseToolInProgress` is true, and `OnExit` transfers the timer progress into `FishRodRenderer.SetPower`.
- The correct DTMAPI control point is therefore a native-style input hold/release decision, not a direct write to fish-rod power.
- `FastAnimations` already accelerates the visible Ready timer by ticking `_castTimer` with the extra multiplier delta; the new target-charge setting must compose with that owner so the configured progress is reached faster when animation speed is enabled.

## Summary

- Added experimental `FishingAutomationOptions.CastChargeRatio` in range `0..1`; invalid values are normalized, default is `0`.
- Added AutoFishingMod config text and number control for cast charge ratio. `0` releases immediately, `1` holds until full charge, and intermediate values release after native Ready progress reaches the target.
- Added Ready-phase input override for `DolocUserInput.NormalUseToolInProgress` while AutoFishing owns the Ready state.
- Added smoke field `AutoFishingCastCharge` and script parameter `-AutoFishingCastChargeRatio`.
- Tightened the smoke gate to require a verified `Smoke.AutoFishingCastCharge` line with the requested target value, so a default/no-charge run cannot accidentally satisfy a configured-charge test.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/test.ps1 -Configuration Release`
  - Fifth-save AutoFishing `FastAnimations` smoke with `-AutoFishingCastChargeRatio 0.5`: `docs/debug/evidence/GAME-SMOKE/20260613-171405`

- Retained rejected evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260613-170514` is a false positive from the first smoke gate. PowerShell selected an integer `[Math]::Min/Max` overload and truncated `0.5` to `0`, so it proved default no-charge release only.
  - `docs/debug/evidence/GAME-SMOKE/20260613-171104` failed after the gate was tightened to require `target=0.5`; this isolated the same script clamp bug.

## Evidence

- `GAME-SMOKE/20260613-171405/result.json` records `RunStatus=Passed`, `AutoFishingCastCharge=Passed`, `AutoFishingAnimationSpeed=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `GAME-SMOKE/20260613-171405/summary.txt` records `SaveSlot=5`, `AutoFishingScenario=FastAnimations`, and `AutoFishingCastChargeRatio=0.5`.
- `GAME-SMOKE/20260613-171405/DTMAPI-latest.log` records `Smoke.AutoFishingCastCharge = experimental ... target=0.5, progress=0`, Ready charge speed samples up to `castTimer.Progress:0.44->0.48`, and `Smoke.AutoFishingCastCharge = verified ... target=0.5, progress=0.54, input=release`.
- The same log records `FastCastHookPhysics`, `FishRodRenderer.Pull` duration scaling, native minigame auto-play frame counters, and final loop summary `readyCharge=True, fastAnimation=True`.
- `GAME-SMOKE/20260613-171405/latest-report.txt` points to `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-171455.zip`.
- `process-check.txt` says no `DolocTown.exe` process was found, and `fatal-window-check.txt` says no fatal instance popup was found.

## Rollback

- If charge targeting interferes with manual/native Ready state, disable only the `NormalUseToolInProgress` override path and keep the existing Ready speed, Cast hook physics, Pull duration, and minigame input automation intact.
- Do not replace this with direct `FishRodRenderer.SetPower` writes unless a future native-owner review proves that route does not bypass Ready/Pull lifecycle semantics.
