# 20260531-0002: Animal Bell UI Evidence

## Source Request

- Goal: continue DTMAPI 0.1.12 player-visible fixes without redoing completed official-local packaging, base localization, or fish roe display.
- Focus for this update: make the AnimalHusbandryProgress migration visibly work in the real official animal bell UI, with build/game/screenshot/exit evidence.

## Changes

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
  - Animal progress text is inserted at the start of the animal state description so it is visible in the game's limited animal viewer text area.
  - Default fallback label changed from `Special produce` to Chinese-first `隐藏产物`.
  - Real UI evidence now writes `summary.txt`, immediate screenshot, delayed screenshot, and verified hook statuses for `Animals.ViewerRendering`, `Smoke.AnimalPanelUi`, `Smoke.AnimalViewerUi`, and `Smoke.AnimalViewerUiScreenshot`.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - Added smoke-only official `AnimalPanelUiState` open/refresh/select path for third-save animal-bell validation.
  - Added delayed screenshot scheduling before smoke auto-exit.
  - Kept `Smoke.AnimalPanelUi` pending status before `EnterUI`, so final evidence is not overwritten back to pending.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
  - Added evidence callbacks for `AnimalViewer.Show` and `AnimalPanel.RefreshViewer`.
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
  - Changed `AnimalHusbandryProgressOptions.ProgressLabel` default to `隐藏产物`.
- `testmods/AnimalHusbandryProgressMod/ModEntry.cs`
  - Migrates blank or old default `Special produce` labels to `隐藏产物`.
- `tools/scripts/run-game-smoke.ps1`
  - Added `-AutoOpenAnimalPanel` smoke path and AnimalViewer UI pass/fail result.
- `tools/scripts/run-hook-probe.ps1`
  - Added `-AutoOpenAnimalPanel` passthrough and evidence check.
- `tools/scripts/collect-logs.ps1`
  - Collects game `DTMAPI/evidence` so screenshots and summaries are preserved with smoke logs.
- Docs updated:
  - `docs/debug/regressions/smoke-matrix.md`
  - `docs/hook-map/README.md`
  - `docs/api/public-api-matrix.md`
  - `docs/updates/INDEX.md`

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1`
  - Passed 2026-05-31 after the final source changes.
  - `DTMAPI.UnitTests: OK`.
- Final animal bell smoke:
  - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 180 -AutoExerciseExperimentalHooks -AutoOpenAnimalPanel -SkipBuild`
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-022329/result.json`
  - Collected logs/evidence: `docs/debug/evidence/GAME-SMOKE/20260531-022415`
  - `StartupLog`, `GameLaunched`, `SaveLoaded`, `AnimalViewerUi`, `NoFatalInstanceWindow`, and `ProcessExited` are all true.
  - `ForcedClose` is false.
  - `process-check.txt` says no `DolocTown.exe` process found.
  - `fatal-window-check.txt` says no fatal instance popup found.

## Evidence

- Log lines in `docs/debug/evidence/GAME-SMOKE/20260531-022415/DTMAPI-latest.log`:
  - `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - `Smoke exercise AnimalViewerRendering OK animal=chicken stateDescription=隐藏产物: 羽毛 7/60 | 正在繁育后代，几天后就能相见。`
  - `Animal viewer UI evidence OK owner=Yuuka.DTMAPI.AnimalHusbandryProgress title=角羊驼2 label=隐藏产物 ... stateDescription=隐藏产物: 羊毛脂 99/100 | 开心的动物能够提供产出。`
  - `Hook status: Animals.ViewerRendering = verified.`
  - `Hook status: Smoke.AnimalPanelUi = verified.`
  - `Hook status: Smoke.AnimalViewerUiScreenshot = verified.`
- Screenshot summary:
  - `docs/debug/evidence/GAME-SMOKE/20260531-022415/DTMAPI-evidence/ANIMAL-001/20260531-022409/summary.txt`
- Visible UI screenshot:
  - `docs/debug/evidence/GAME-SMOKE/20260531-022415/DTMAPI-evidence/ANIMAL-001/20260531-022409/animal-viewer-ui-delayed.png`
  - Visual check confirmed the official animal bell panel shows `隐藏产物: 羊毛脂 99/100`.

## Related Records

- Smoke matrix: `ANIMAL-001`
- Hook map: `Animals.ViewerRendering`
- Public API matrix: `IAnimalViewerApi.ConfigureSpecialProduceProgress`
- Related earlier evidence: synthetic constructor-only evidence in `HOOK-PROBE/20260530-150808` is superseded for this case by the real UI evidence above.

## Rollback

- To roll back only the evidence automation, remove the smoke-only `-AutoOpenAnimalPanel` script setting and the `AnimalViewer.Show` / `AnimalPanel.RefreshViewer` evidence callbacks.
- To roll back the player-visible text placement, change AnimalHusbandryProgress display insertion back from prepend to append, but that reintroduces the clipped-text risk seen in `GAME-SMOKE/20260531-021447`.
- Keep ordinary DTMAPI mods under the official/local DTMAPI package path; do not move them to `BepInEx/plugins`.

## Follow-up

- ActionSpeed still needs real animation/timing evidence before it can be treated as more than experimental.
- AutoFishing still needs a real fishing phase automation smoke.
- OneActionComplete still needs a real resource/tool hit evidence path.
