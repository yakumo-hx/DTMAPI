# 20260603-0016 - AutoFishing Skip-False Minigame Smoke

## Source Request / Goal

Continue the 0.2.4 manual-QA regression goal, specifically Task C: `AutoCompleteMiniGame=true` and `SkipMiniGame=false` must be a separate path from skip-minigame and must reach the native fishing minigame instead of jumping straight to pull.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `testmods/AutoFishingMod/README.md`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260603-0013-024-regression-fixes-evidence.md`

## Implementation

- Added smoke support for `-AutoExerciseAutoFishingMiniGameComplete`, which writes AutoFishing config with `AutoCompleteMiniGame=true` and `SkipMiniGame=false`, waits for `Smoke.AutoFishingMiniGameComplete`, and records the result in `result.json`.
- Added a smoke-only `ForceFishingFishForSmoke` gate so the direct minigame smoke can guarantee `FishProto.IsFish=true`; normal player fishing still preserves the original fish/trash roll outcome.
- Logged `isFish`, `forceFishForSmoke`, `forceFishSatisfied`, and `rollAttempts` in the wait-phase AutoFishing summary.
- Added a dedicated `LastFishingMiniGameCompleteSummary` so the final smoke OK line is not overwritten by the immediately following Pull animation-speed hook.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Failed diagnostic third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260603-172124`.
- Passing third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260603-173435`.
- Result: `SaveLoaded=True`, `AutoFishingPhase=True`, `AutoFishingMiniGameComplete=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-173435/process-check.txt` says `No DolocTown.exe process found.`

## Evidence

- Rejected hypothesis from failed smoke: `GAME-SMOKE/20260603-172124` entered real `AgentStateFishingWait`, but the roll produced `waste_plastic_bottle` (`FishProto.IsFish=false`), so the native game correctly routed to Pull and never created `FishingGameScrollBar`.
- Passing smoke log: `Fishing phase hook observed phase=MiniGame source=FishingGameScrollBar`.
- Passing smoke log: `Smoke.AutoFishingPhase = verified ... autoHook=AgentStateFishingBattle, fish=loach, isFish=True, pool=default, autoCompleteMiniGame=True, skipMiniGame=False, forceFishForSmoke=True, forceFishSatisfied=True, rollAttempts=1`.
- Passing smoke log: `Fishing automation completed minigame status ... currentGameStatus=Success visibleSeconds=0.76`.
- Passing smoke log: `Smoke exercise AutoFishingMiniGameComplete OK owner=Yuuka.DTMAPI.AutoFishing, behavior=AutoCompleteMiniGame, status=Success, skip=false, visibleSeconds=0.76`.

## Related Records

- Debug: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `MANUALQA-024-C`, `AUTOFISH-001`.
- Hook map: `Fishing.Automation`, `Fishing.MinGameAutoComplete`.
- Prior partial record: `docs/updates/2026/20260603-0013-024-regression-fixes-evidence.md`.

## Rollback

Revert the smoke-only `ForceFishingFishForSmoke` path, the dedicated minigame summary, and the smoke harness `AutoExerciseAutoFishingMiniGameComplete` gate. Keep the retained failed smoke `GAME-SMOKE/20260603-172124` as a reminder that trash/garbage rolls do not create the native minigame.

## Follow-Up

- Run a manual visual fishing pass if player-facing feel needs to be assessed beyond the third-save hook smoke.
- Remaining goal blockers outside this slice at the time: Oil coal-drop mining evidence, Mine native fuel/electric player UI evidence, and native player-equipment-screen integration for More Equipment Slots. Oil coal-drop mining was resolved later in `docs/updates/2026/20260603-0017-oil-coal-drop-newcontent-smoke.md`.
