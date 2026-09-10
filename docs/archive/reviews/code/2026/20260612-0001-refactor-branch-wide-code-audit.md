# 20260612-0001 - Refactor Branch-Wide Code Audit

Status: verified
Date: 2026-06-12
Branch: `Refactor`
Source request: user asked for a full current-branch code-level audit, including update records, debug records, code adjustment, current audit report, and next plan before bottom-layer refactor.

## Required Context Read

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/reviews/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`

## Scope

This review covers the current `Refactor` branch state after the manual QA batch and crop-harvesting/release-hygiene updates. It is a code-path and ledger audit, not a new game-smoke pass and not a stability promotion review.

## Findings

### P2 - Manager and title config UI still truncate instead of scrolling

- User-visible issue: Manager pages may hide rows when mod/hook/feature counts exceed the compact page capacity; the Mine config page can overflow when many settings exceed the visible title UI.
- Code facts inspected:
  - `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs` renders config items with `page.Items.Take(13)`.
  - The same title settings UI renders Manager page rows with fixed first-N limits for Mods, Errors/Warnings, Hooks, and Features.
  - `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs` intentionally formats `showing first N of total`.
- Inference: this is a deliberate compact-preview implementation, not a data-source failure. It is insufficient for the user's next UI requirement because hidden rows are not reachable in UI.
- Rejected hypotheses: this is not caused by the Manager view model losing rows; the row-count text and manual QA both indicate compact rendering.
- Acceptance checks for future work: Manager and config pages must expose all rows through scroll or paging, keep total counts visible, prevent text overlap, and preserve button/keybind interactions.

### P2 - SaveSlots data path works, but 18+ layout has no paging layer

- User-visible issue: 12 slots are acceptable, extra save/load works, but 18+ slots overflow off-screen and need paging/scroll.
- Code facts inspected:
  - `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs` clamps requested count to `6..60` and writes only `DolocAPI.gameManager.archiveFileCount`.
  - `SaveSlotsFeature` does not install a save-UI hook or paging adapter.
  - `testmods/MoreSavesMod/ModEntry.cs` exposes `SlotCount` from 6 to 60.
- Inference: archive discovery/load/save can succeed through native owners, but layout containment is outside the current implementation.
- Rejected hypotheses: 18+ overflow does not itself prove save corruption; the user confirmed extra-slot save/load works.
- Acceptance checks for future work: 6, 12, 18, and 24 slot counts must remain contained; selection/load/save/delete/copy targets must remain correct across pages.

### P2 - Mine has not been converted to pure electric consumption

- User-visible issue: Mine settings still expose fuel capacity and fuel consumption, despite the older requirement to remove fuel consumption and use pure electric power.
- Code facts inspected:
  - `testmods/MineMod/ModEntry.cs` still registers `Fuel capacity`, `Fuel mode cost`, `Electric mode fuel`, and `Power per cycle`.
  - Mine definition still sets `AllowFuelMode = true`, `AllowElectricMode = true`, `FuelOnlyFuelCostPerCycle`, and `ElectricModeFuelCostPerCycle`.
  - `src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/DolocTownExperimentalBridgeApi.MachineProduction.cs` still subtracts fuel cost even in electric mode when configured.
- Inference: the previous pure-electric requirement is not implemented; at most earlier work made electric mode available.
- Rejected hypotheses: this is not only a stale config-menu label problem because runtime state and production cost fields still include fuel.
- Acceptance checks for future work: Mine config removes fuel settings, runtime production consumes official electric power only, status text/report fields stop advertising fuel state, and old configs migrate safely.

### P2 - CameraView movement path passes current manual scenarios, but background sync is still absent

- User-visible issue: 2x/4x movement, centering, clamp, no flicker/jump, title return, and building transition behavior passed user manual QA; background still uses the original unsynchronized background.
- Code facts inspected:
  - `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraViewService.cs` writes `Camera.orthographicSize`.
  - `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraDiagnosticsService.cs` explicitly states it does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, or panorama background/fog compensation.
  - `testmods/ZoomMod/README.md` documents the orthographic-size-only contract.
- Inference: background sync is a known separate native-owner problem, not a hidden regression in the current playable zoom implementation.
- Acceptance checks for future work: foreground movement, bounds clamp, building transitions, title return, and background/fog alignment must be proven together.

### P2 - AnimalViewer first-frame issue is narrowed but not fully closed

- User-visible issue: animal hidden-produce row historically flashes `心情` before the correct produce row; manual repeated animal-switch confirmation remains needed.
- Code facts inspected:
  - Current overlay path clones independent progress rows, disables localization-style components before activation, fills text while inactive, refreshes again in the same callback, and publishes a first-frame guard status.
  - The old private `ApplyAnimalProgressSinglePassData` method wrote `moodInfo` and `moodProgress` directly but had no callers.
- Change made in this audit: removed the unused single-pass native mood-bar helper so future work cannot mistake it for the active rendering path.
- Rejected hypotheses: the removal is not a visual fix claim; it removes dead code only. The manual repeated-switch gate remains open.
- Acceptance checks for future work: repeated click/switch across animals must show no `心情` frame before the hidden-produce row, with logs or screenshot/video evidence.

### P3 - StrongPlantingGun has been narrowed to the fixed three-slot contract

- User-visible issue from older QA: disabled config can leave visible extra slots, and range scaling returns to native 4x4 during use.
- Code facts inspected:
  - `testmods/StrongPlantingGunMod/ModEntry.cs` forces `SupportedSlotCount = 3`.
  - `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunService.cs` normalizes every policy to three slots.
  - Unit coverage `StrongPlantingGunNormalizesToThreeSlotContract` verifies oversized/undersized/disabled requests normalize to three.
- Inference: the slot-count contract cleanup is done, but range expansion remains intentionally unsupported and disabled visible-slot cleanup still needs manual behavior work if the user wants immediate UI restoration.

### P3 - AutoFishing option semantics are now documented, but F9 fish info remains pending

- User-visible issue: auto-cast and instant bite work, but skip/auto-complete/fast animation semantics are not independent solver-style behavior; F9 fish info is not implemented.
- Code facts inspected:
  - `testmods/AutoFishingMod/ModEntry.cs` labels auto-complete as delayed minigame success, skip as instant-bite-only, and fast animation as may-no-op.
  - `FishingAutomationService` normalizes `AutoRecast` and `RequireSelectedFishingRod` to true.
  - F9 currently opens the config page until the fish-info UI is promoted.
- Inference: the earlier misleading UI was corrected; remaining F9 info UI is a lower-priority feature gap, not a current regression blocker.

## Code Adjustment

- Removed unused `AnimalViewerService.ApplyAnimalProgressSinglePassData(...)`.
- No public API, hook target, smoke result schema, or runtime feature behavior is intentionally changed.

## Documentation Updates Made

- Updated `docs/reviews/README.md` to recognize branch-wide code audit records under `docs/reviews/code/YYYY/`.
- Updated `docs/api/public-api-matrix.md` status date and current SaveSlots/CameraView evidence boundaries.
- Added a current-branch debug note to `docs/debug/INDEX.md`.
- Added smoke-matrix row `BRANCH-WIDE-CODE-AUDIT-20260612`.
- Added update record `docs/updates/2026/20260612-0010-refactor-branch-wide-code-audit.md`.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- No game smoke is claimed for this audit because the only code edit is dead private method removal and the rest is review/documentation ledger correction.

## Next Plan

1. Implement shared title UI scroll/page infrastructure for Manager lists and config pages.
2. Add SaveSlots official save UI paging/scroll while preserving user-confirmed extra-slot save/load behavior.
3. Convert Mine to pure electric production through MachineProduction contract/config migration.
4. Run Camera background native-owner research before attempting background/fog synchronization.
5. Run AnimalViewer repeated-switch manual QA after the first-frame guard, and only then mark the flicker issue solved.
