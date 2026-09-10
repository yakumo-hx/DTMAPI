# Update 20260531-0005: AutoFishing wait-phase smoke

Date: 2026-05-31

Status: implemented

## Source Request / Goal

- Continue DTMAPI 0.1.12 player-visible cleanup without redoing already verified official-local packaging, base localization, fish roe display, animal bell evidence, or OneAction resource-hit work.
- Close one small AutoFishing vertical slice: hotkey/state must be reachable, visible in logs, and at least one real fishing phase automation behavior must work in-game.
- Keep AutoFishing experimental; do not claim full auto-cast/recast/minigame automation is complete.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - Added stronger AutoFishing smoke setup diagnostics.
  - Finds active/inactive scene `FishingPool` objects before falling back to config-table candidates.
  - Validates candidate pools with `DolocAPI.RollFish` before entering `AgentStateFishingWait`.
  - Records detailed failure source if pool discovery or transient pool creation fails.
- `testmods/AutoFishingMod/ModEntry.cs`
  - Updated fallback config text to say F6 plus wait-phase instant bite is verified, while broader automation remains experimental.
- `testmods/AutoFishingMod/i18n/schinese.json`
- `testmods/AutoFishingMod/i18n/english.json`
  - Updated player-facing labels from generic pending to wait-phase verified/experimental.
- `testmods/AutoFishingMod/README.md`
  - Updated current boundary to 0.1.12.
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Known Facts / Rejected Hypotheses

- Direct `DolocTown.exe` launch remains rejected for hook smoke because it can show the fatal "Another instance is already running" popup. This validation used Steam launch.
- First AutoFishing phase attempt failed before exercising the hook because no active scene fishing pool was found and the smoke path had weak pool diagnostics.
- The failure did not contradict startup, third-save load, F6 input, or clean-exit behavior: `GAME-SMOKE/20260531-033903` had `StartupLog=true`, `GameLaunched=true`, `SaveLoaded=true`, `AutoFishingHotkey=true`, `NoFatalInstanceWindow=true`, and `ProcessExited=true`, but `AutoFishingPhase=false`.
- The final passing run used a real scene pool named `default`; no copied/decompiled game source or old DLKsmapi implementation was used.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Result: passed, 0 warnings, 0 errors.
  - `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -TimeoutSeconds 240 -AutoExerciseAutoFishingPhase -SkipBuild`
  - Result: passed.
  - Evidence root: `docs/debug/evidence/GAME-SMOKE/20260531-035217`.
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-035305`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\install-to-game.ps1 -SkipBuild`
  - Result: passed after the final AutoFishing i18n wording update.
  - Installed official-local package includes `自动钓鱼（等待阶段已验证/实验）` in `MODS/Yuuka_DTMAPI_AutoFishing/i18n/schinese.json`.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260531-035217/result.json`
  - `StartupLog=true`
  - `GameLaunched=true`
  - `SaveLoaded=true`
  - `AutoFishingHotkey=true`
  - `AutoFishingPhase=true`
  - `NoFatalInstanceWindow=true`
  - `ProcessExited=true`
  - `ForcedClose=false`
- `docs/debug/evidence/GAME-SMOKE/20260531-035217/process-check.txt`
  - `No DolocTown.exe process found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-035217/fatal-window-check.txt`
  - `No fatal instance popup found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-035305/DTMAPI-latest.log`
  - `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - `AutoFishing SaveLoaded boundary OK slot=2`
  - `AutoFishing automation enabled reason=hotkey F6`
  - `Smoke automation entering AgentStateFishingWait for AutoFishing phase evidence. owner=Yuuka.DTMAPI.AutoFishing, rod=ItemFishingRod, poolSource=scene:default, fish=golden_fish, scenePools=1.`
  - `Fishing automation instant-bite applied by Yuuka.DTMAPI.AutoFishing fish=old_electric_wire pool=default.`
  - `Smoke exercise AutoFishingPhase OK owner=Yuuka.DTMAPI.AutoFishing, behavior=InstantBite, phase=Wait, fish=old_electric_wire, pool=default, applications=1`

## Related Records

- Regression matrix: `docs/debug/regressions/smoke-matrix.md` / `AUTOFISH-001`.
- Hook map: `docs/hook-map/README.md` / `Fishing.Automation`.
- API matrix: `docs/api/public-api-matrix.md` / `IFishingAutomationApi.Configure/SetEnabled`.
- Earlier failed evidence kept for audit: `docs/debug/evidence/GAME-SMOKE/20260531-033903` and collected logs `docs/debug/evidence/GAME-SMOKE/20260531-034346`.

## Rollback Notes

- Revert the AutoFishing smoke pool discovery and `TryRollFishForSmoke` helpers in `DolocTownGameBridge.cs` to return to the previous weaker pool setup.
- Revert AutoFishing i18n/README/docs wording to pending if the wait-phase hook regresses.
- The runtime API remains experimental, so rollback does not require a stable API compatibility break.

## Follow-Up

- Verify full auto-cast and recast behavior through real input/state transitions.
- Verify or keep disabled `SkipMiniGame` and `FastAnimations` until there is separate evidence.
- Consider a manual visible fishing UI screenshot after the next broader AutoFishing slice; this update only proves the wait-phase hook/log evidence.
