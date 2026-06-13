# 20260612-0012 - Bottom-Layer Refactor Audit Implementation

Status: verified
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: `/goal` using `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`.
Version: `0.5.0-alpha` / `0.5.0.0` -> `0.5.1-alpha` / `0.5.1.0`

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `tools/scripts/release-common.ps1`
- `tools/scripts/install-to-game.ps1`
- selected DTMAPI-owned mod manifests under `testmods/`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/DolocTownExperimentalBridgeApi.MachineProduction.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/SaveSlotsSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AnimalViewerSmokeCase.cs`
- `testmods/MineMod/*`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0012-bottom-layer-refactor-audit-implementation.md`

## Summary

- Created and worked on `codex/bottom-layer-refactor-audit-20260612` instead of implementing on `Refactor`.
- Bumped controlled version sources from `0.5.0-alpha` / `0.5.0.0` to `0.5.1-alpha` / `0.5.1.0`; no second bump is applied when already at `0.5.1-alpha`.
- Hardened the PowerShell install/release scripts for the 0.5.1-alpha package path by reading manifest JSON as UTF-8 and keeping `release-common.ps1` package display metadata ASCII-safe for Windows PowerShell parsing.
- Reworked the reflected title Settings entry into a text `模组设置` button and added deterministic paging for the enabled-mod config list, per-page config items, and Manager Mods/Errors/Warnings/Hooks/Features.
- Added SaveSlots official save-panel paging for expanded 18+/24+ layouts while keeping official save archive/render/load/delete/copy ownership. The smoke helper now selects slot 1, slot 13, and the final slot and records `visibleSlotsAfterPaging`.
- Converted MineMod to electric-only/no-fuel runtime/config behavior, with stale persisted fuel fields normalized to zero.
- Hardened Mine placement preview and room/environment visual lifecycle by widening builder id resolution and forcing a quick visual poll on environment reset.
- Tightened AnimalViewer first-frame lifecycle so the prefix clears stale cloned rows and the postfix owns inactive prefill, activation, and same-callback text validation. The smoke helper repeats official panel refresh/select and publishes `Smoke.AnimalViewerRepeatedSwitchFirstFrame`.
- Performed Camera background/fog/panorama native-owner review and deferred runtime changes because ownership is split across `DolocAPI.LoadBackground`, `BackgroundRenderer`, `BackgroundLayerRenderer`, `EnvCovariantController`, and `DepthFogController`.

## Known Facts And Rejected Hypotheses

- SaveSlots 18+/24 containment can be handled as a UI paging adaptation without replacing `LocalSave` or `GameDataPanel`.
- Mine electric mode does not need hidden DTMAPI fuel state; stale old Mine config fields are migration inputs only.
- Mine visual scale needs builder/renderer/environment lifecycle coverage, not only production polling.
- AnimalViewer first-frame protection must clear stale cloned rows before native show and fill cloned text before activation.
- Camera playable movement success is not background/fog/panorama sync proof; no runtime Camera background patch is made in this update.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- DirectExe title/config smoke `GAME-SMOKE/20260612-165456` passed: `TitleSettingsButton=Passed`, `TitleSettingsMenu=Passed`, process/fatal checks clean, and config screenshots reached `DTMAPI.MineMod` plus `DTMAPI.MoreEquipmentSlotsMod`.
- DirectExe Manager MVP smoke `GAME-SMOKE/20260612-170119` passed: Manager Status, Mods, Errors, Hooks, Features, Logs, summary copy/fallback, logs export, screenshots, process exit, and fatal-window checks all passed.
- DirectExe third-save SaveSlots smokes passed:
  - `GAME-SMOKE/20260612-170240` for 6 slots: `archiveFileCount=6`, `renderedSlots=6`, `visibleSlotsAfterPaging=6`, target `1`.
  - `GAME-SMOKE/20260612-170331` for 12 slots: `archiveFileCount=12`, `renderedSlots=12`, `visibleSlotsAfterPaging=12`, target `1`.
  - `GAME-SMOKE/20260612-170421` for 18 slots: `archiveFileCount=18`, `renderedSlots=18`, `visibleSlotsAfterPaging=6`, targets `1|13`.
  - `GAME-SMOKE/20260612-170512` for 24 slots: `archiveFileCount=24`, `renderedSlots=24`, `visibleSlotsAfterPaging=12`, targets `1|13|24`.
- DirectExe third-save Mine smoke `GAME-SMOKE/20260612-170708` passed: `NewContentMineApis`, official JSON, production, official tech-tree UI, HookProbe, SaveLoaded, process exit, and fatal-window checks passed; logs verify `electricOnly=True`, `fuel=disabled`, `electricPowerCost=10`, Mine production/storage, placement screenshot, and visual containment with no non-Mine contamination.
- DirectExe third-save AnimalViewer smoke `GAME-SMOKE/20260612-170819` passed: `AnimalViewerUi`, HookProbe, SaveLoaded, process exit, and fatal-window checks passed; logs verify `Smoke.AnimalViewerFirstFrameFlickerGuard=verified`, `moodTitleHits=0`, and `Smoke.AnimalViewerRepeatedSwitchFirstFrame=verified` with repeated `RefreshViewer`/`Select` targets.
- Steam third-save HookProbe smoke `GAME-SMOKE/20260612-170928` passed: `HookProbe`, `SaveLoaded`, `GameLaunched`, `StartupLog`, process exit, and fatal-window checks passed. `process-check.txt` reports no `DolocTown.exe`; no waiting-for-exit symptom was observed by the harness.

## Evidence

- Goal: `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`.
- Camera review: `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md`.
- Regression row: `BOTTOM-LAYER-REFACTOR-20260612`.
- Title/config UI: `GAME-SMOKE/20260612-165456`; config screenshots under `DTMAPI-evidence/UI-004/20260612-165531`.
- Manager MVP: `GAME-SMOKE/20260612-170119`; Manager screenshots under `DTMAPI-evidence/UI-004/20260612-170153`.
- SaveSlots official UI: `GAME-SMOKE/20260612-170240`, `GAME-SMOKE/20260612-170331`, `GAME-SMOKE/20260612-170421`, `GAME-SMOKE/20260612-170512`; game-side screenshots under `SAVESLOTS-UI/20260612-170314`, `20260612-170405`, `20260612-170455`, and `20260612-170547`.
- Mine: `GAME-SMOKE/20260612-170708`; screenshots under `NEWCONTENT-025/20260612-170750`.
- AnimalViewer: `GAME-SMOKE/20260612-170819`; screenshots and summary under `ANIMAL-001/20260612-170858`.
- Steam/exit regression: `GAME-SMOKE/20260612-170928`; process check reports no `DolocTown.exe`.

## Rollback

- Revert the version bump only together with runtime/manifests/tests if the 0.5.1-alpha branch is abandoned before release.
- SaveSlots rollback is limited to the `GameDataUiState.Show` and `GameDataPanel.Select` paging hooks plus the pager state in `SaveSlotsService`; `archiveFileCount` behavior can remain independently.
- Mine rollback would restore fuel-capable config and normalization but should not be done without a new manual-QA review because the goal intentionally removes player-facing fuel.
- AnimalViewer rollback should not reintroduce native `moodInfo`/`moodProgress` overwrite rendering; prefer the cloned-row lifecycle.
- Camera has no runtime rollback in this update because no background/fog/panorama patch was added.

## Follow-Up

- Keep `ICameraViewApi`, `ISaveSlotsApi`, `IMachineProductionApi`, and `IAnimalViewerApi` Experimental until their broader native-owner and manual-QA gates are complete.
- Future SaveSlots work should cover delete/copy/restart-recognition edges for 18+/24+ layouts; this update verifies render/selection/containment only.
- Future Camera work must begin from the native background/fog/panorama owners recorded in `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md`.
