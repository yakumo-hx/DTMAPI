# 20260616-0004 Player Log Console Movement Animal Follow-Up

## Status

implemented-verified

## Area

diagnostics/debug-console/movement/animalviewer

## Source Request / Goal

Player feedback from `D:\下载\DTMAPI-logs.zip` reported long-play AnimalViewer disappearance, Y-console search closing on `Y`, right-click item give selecting the wrong item, movement debug speed being lost after idle/wake, and ActionSpeed well/planting intermittency.

Manual QA review:

- `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`

## Changed Files

- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260616-0001-actionspeed-bottom-layer-rebuild.md`
- `docs/goals/2026/20260616-0001-actionspeed-bottom-layer-rebuild.goal.txt`
- `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Kept log growth lightweight: current-run `latest.log` still has no hard byte cap, but high-risk new paths use throttled publication. GameBridge feature failures now unwrap nested `TargetInvocationException` to store the root cause first, and bootstrap `Update` failures now record full errors only for the first three hits before 30-second summaries.
- Made the reflected Y console focus-aware: `Y` no longer closes the console while a reflected `InputField` is focused, while Escape still closes.
- Removed the stale global `Mouse1` screen-rectangle item-give path. Right-click give now uses the actual item-cell Unity event/hover route; if reflected right-click binding fails, it fails closed instead of giving the wrong item. The smoke harness now avoids auto-exiting before the external mouse-give step.
- Added a lightweight `IMovementDebugApi` lease over `MotionAbility.SetMoveScaler`: non-default movement speed is reapplied at low frequency while active, reset/returned-to-title/save-load clears the lease, and reset clears future reapply state even if native `MotionAbility` is temporarily unavailable.
- Hardened AnimalViewer clone lifetime: save/title/environment boundaries clear clone sessions, refresh validates parent/clone/row counts before writing, failed refresh clears the session, and parent scanning destroys `DTMAPI.AnimalProduceProgress.*` leftovers even if a clone failed before entering the active list.
- Split ActionSpeed well/planting intermittency into a future native-owner rebuild handoff instead of patching symptoms in this update.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Steam third-save DebugConsole/Movement smoke `GAME-SMOKE/20260616-120453` passed with `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `DebugConsoleOpenY1=Passed`, `DebugConsoleCloseEscape=Passed`, `DebugConsoleOpenY2=Passed`, `DebugConsoleCloseY=Passed`, `DebugConsoleTenYShortTaps=Passed`, `DebugConsoleHoldYNoFlicker=Passed`, `DebugConsoleMouseGive=Passed`, `DebugMovement=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Steam third-save AnimalViewer smoke `GAME-SMOKE/20260616-120817` passed with `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `AnimalViewerUi=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Refreshed the DTMAPI Runtime local upload package with `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly -OutputRoot C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`, preserving `workshop_id=3743016467`.
- Source Release output and local upload package runtime DLL SHA256 hashes match:
  - `DTMAPI.BepInExBootstrap.dll`: `EF21BE632FC1C5504930A87CDFDE98EA301AD5E8C8B174F38625855E40F341EB`
  - `DTMAPI.Abstractions.dll`: `F83E9E3285EA28F8DCEFA207D50DF0E161B367FA1D2D5C14DD63FA36D96DEF49`
  - `DTMAPI.Core.dll`: `3EC116BF4F1AA0835E43C5A8C731F70CCA90C6FB027E4C0E4FE03E695B11012F`
  - `DTMAPI.GameBridge.DolocTown.dll`: `088707A7AE8D5C83E341A275C215F98CEDBF58F08103FFC59B49D2A55CAA0F83`
  - `DTMAPI.ModConfigMenu.dll`: `EAC730741BEDA5AE4F17AE9CC810EFE8A1262185237EEE104629A691B42DDAC5`

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260616-120453`
- `docs/debug/evidence/GAME-SMOKE/20260616-120817`

## Related Records

- Review: `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`
- Future ActionSpeed goal: `docs/goals/2026/20260616-0001-actionspeed-bottom-layer-rebuild.md`
- Prior log rotation update: `docs/updates/2026/20260616-0001-runtime-latest-log-rotation.md`

## Rollback Notes

- Reverting this update restores the old Y-console global right-click hit test and one-shot movement scaler behavior, so rollback should be paired with a warning that wrong-item right-click give and lost movement speed may return.
- AnimalViewer rollback should be limited to clone-session lifecycle changes; do not revert the earlier first-frame flicker guard unless explicitly investigating that separate path.

## Follow-Up

- Run the ActionSpeed bottom-layer rebuild goal before claiming well fill or planting acceleration is fixed.
- Consider a future current-session log byte cap only if throttled diagnostics still cannot contain a real per-frame exception loop.
