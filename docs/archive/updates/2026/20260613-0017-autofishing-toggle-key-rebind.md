# 20260613-0017 AutoFishing Toggle Key Rebind

- Date: 2026-06-13
- Status: verified
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user confirmed the other AutoFishing functions are manually OK and requested a replaceable AutoFishing hotkey.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260613-0006-autofishing-minigame-animation-follow-up.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Root Cause

- AutoFishing already had a `ToggleKey` config field and registered that field through `IInputHelper`, but the option was not exposed in the DTMAPI config menu.
- Runtime normalization and smoke setup still treated the AutoFishing toggle as fixed F6 behavior, so a player could not reliably rebind it or prove a rebound key through the fifth-save smoke route.
- This belongs in AutoFishingMod config/input ownership. No new native fishing responsibility function is needed, and `IFishingAutomationApi` should remain Experimental without a public contract promotion.

## Summary

- Added a player-facing AutoFishing keybind option in the DTMAPI config menu.
- Preserved custom toggle keys, kept explicit `None` as disabled, and migrated missing/blank old config values to default `F6`.
- Re-registers the current toggle key after config saves while keeping movement cancel keys separate.
- Extended `run-game-smoke.ps1` with `-AutoFishingToggleKey` so AutoFishing fifth-save smokes can write the selected key, send it to the game window, and wait for key-specific DTMAPI/AutoFishing log evidence.
- Added unit coverage for `F7` preservation, missing-key F6 migration, explicit `None`, and unchanged multiplier/charge normalization.

## Validation

- Passed:
  - PowerShell 5.1 parse check for `tools/scripts/run-game-smoke.ps1`
  - `git diff --check`
  - `tools/scripts/test.ps1 -Configuration Release`
  - Local Release install with AutoFishing official-local package refresh
  - Fifth-save AutoFishing `DefaultLoop` smoke with `-AutoFishingToggleKey F7`: `docs/debug/evidence/GAME-SMOKE/20260613-175055`

## Evidence

- `GAME-SMOKE/20260613-175055/result.json` records `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `GAME-SMOKE/20260613-175055/summary.txt` records `SaveSlot=5`, `AutoFishingScenario=DefaultLoop`, `AutoFishingToggleKey=F7`, `AutoPressAutoFishingHotkey=True`, and `SentExternalAutoFishingToggleAttempt1=F7`.
- `GAME-SMOKE/20260613-175055/DTMAPI-latest.log` records `AutoFishing native loop policy registered. Toggle=F7`, `Input F7 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F7`, `Smoke.AutoFishingMiniGameComplete = verified`, and final loop evidence `AutoCast:True->Wait:True->BiteReady:True->BattleOrPull:True->PullExit:True->NextAutoCast:True`.
- `latest-report.txt` points to `D:\Steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-175205.zip`.
- `process-check.txt` says no `DolocTown.exe` process was found, and `fatal-window-check.txt` says no fatal instance popup was found.

## Rollback

- If the keybind option causes input conflicts, remove only the AutoFishingMod keybind menu item and keep the underlying native fishing loop, minigame input, animation multiplier, and cast-charge behavior intact.
- Do not hard-code F6 in the GameBridge fishing service; hotkey policy belongs in the ordinary mod layer.
