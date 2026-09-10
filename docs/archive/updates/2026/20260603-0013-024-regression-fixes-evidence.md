# 20260603-0013: 0.2.4 regression fixes and evidence

## Summary

Implemented the fresh 0.2.4 manual QA regression slice for AnimalHusbandryProgress, AutoFishing/ActionSpeed, Y-console instant save/teleport CSV, and SecondMotor independence evidence.

## Source Request

- User `/goal` on 2026-06-03: treat older 0.2.3 evidence as historical, bump once to 0.2.4, then fix牧铃, AutoFishing/ActionSpeed, Y-console save/teleport, and SecondMotor regressions with third-save evidence.

## Changed Files

- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/DebugConsoleMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`

## Validation

- Release build/unit: passed on 2026-06-03 after the final title-smoke custom-color stage.
- Title config smoke: `docs/debug/evidence/GAME-SMOKE/20260603-160739` passed with no leftover `DolocTown.exe`.
- Third-save animal smoke: `docs/debug/evidence/GAME-SMOKE/20260603-152851` passed.
- Third-save Y-console instant-save/teleport CSV smoke: `docs/debug/evidence/GAME-SMOKE/20260603-152451` passed.
- Third-save ActionSpeed config smoke: `docs/debug/evidence/GAME-SMOKE/20260603-154207` passed for config apply, while the combined interaction flag exited early by design; interaction was verified separately.
- Third-save ActionSpeed interaction smoke: `docs/debug/evidence/GAME-SMOKE/20260603-154721` passed.
- Third-save AutoFishing smoke: `docs/debug/evidence/GAME-SMOKE/20260603-154816` passed.
- Follow-up AutoFishing skip=false smoke: `docs/debug/evidence/GAME-SMOKE/20260603-173435` passed after the smoke-only fish-roll gate added in `20260603-0016`.
- Third-save SecondMotor smoke: `docs/debug/evidence/GAME-SMOKE/20260603-155505` passed.

## Evidence

- Animal config ordinary preset screenshot hides direct hex editing; staged Custom/`+` screenshot shows `填充颜色` and `F0F0F0`.
- Animal panel log: `Animal viewer UI evidence OK ... overlay=rows=1, 羊毛脂 0/100`.
- ActionSpeed config log: `Smoke exercise ActionSpeedConfigApply OK ... multiplier=2 ... after=... multiplier=4`.
- ActionSpeed interaction log: `Smoke exercise ActionSpeedInteraction OK`, including bottle fill, in-water fill, auto-fill, planting, harvest, resin, and vegetation samples.
- AutoFishing log: `Smoke.AutoFishingMovementCancel = verified`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingAnimationSpeed = experimental ... phase=Pull multiplier=3`, `Smoke.AutoFishingPhase = verified`, and `Smoke.AutoFishingMiniGameSkip = verified`.
- Follow-up AutoFishing skip=false log: `FishingGameScrollBar`, `autoHook=AgentStateFishingBattle`, `fish=loach`, `isFish=True`, `Fishing automation completed minigame ... currentGameStatus=Success visibleSeconds=0.76`, and `Smoke.AutoFishingMiniGameComplete = verified`.
- Y-console log: `Smoke exercise InstantSave OK ... sameRoom=True, distance=0`, `Smoke exercise DebugTeleportCsv OK rows=80`, and `Smoke exercise DebugTeleport OK ... changedRoom=True`.
- SecondMotor log: `dualVisible=True`, `appearanceIsolated=True`, `originalScopedTint=0/10`, `secondScopedTint=5/10`, ride/dismount/original restore, and 2x effective speed.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Revert the touched runtime/GameBridge/UI/config files and reinstall the previous 0.2.3 package set. Keep this update record and ISSUE-005 as historical evidence if rollback is needed.

## Follow-Up

- Direct smoke coverage for AutoFishing `AutoCompleteMiniGame=true` with `SkipMiniGame=false` was added in `docs/updates/2026/20260603-0016-autofishing-skipfalse-minigame-smoke.md`.
- SecondMotor edge-transition smoke was later covered in `20260603-0020-second-motor-edge-transition-smoke.md`; manual visual crossing remains useful but is no longer this record's unresolved automated edge-transition gap.
