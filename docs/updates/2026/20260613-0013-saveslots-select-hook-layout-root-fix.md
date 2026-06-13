# 20260613-0013 SaveSlots Select Hook Layout Root Fix

- Date: 2026-06-13
- Status: verified
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user asked for the root cause of the title homepage becoming two columns and the in-save pause menu cycling between horizontal and two-column/vertical layouts after the setter-normalization workaround appeared to fix the symptom.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Root Cause

- Temporary setter-stack diagnostics proved that the title-page delayed `constraintCount=2` write came from `SaveSlotsService.TryResetLayoutSize -> RestoreOfficialSavePanel -> ApplyOfficialSavePanelPaging -> EnsureOfficialSavePanelPageForSelection`.
- The `SaveSlotsFeature` `GameDataPanel.Select` prefix was installed against an inherited `DolocGridUI<T>.Select` method. That made non-save grids such as `HomePageTextMenu` and pause `MenuUI` enter the SaveSlots selection callback.
- `RestoreOfficialSavePanel` then treated those non-save grids as save panels and called `ResetLayoutSize(slots.Length / 5 + 1)`, which becomes `2` for the five-button homepage menu and eight-button pause menu.
- The earlier `GridLayoutGroup.constraintCount` setter normalization fixed the visible symptom, but the real owner bug was the broad inherited Select hook plus missing GameDataPanel target guard.

## Summary

- Stopped installing the inherited `GameDataPanel.Select` prefix for fixed 12-slot MoreSaves behavior.
- Kept the `GameDataUiState.Show` postfix as the official save UI entry point and marked `Save.MoreSlotsUiPaging` as `not-required` for the fixed 12-slot path.
- Added a `DolocTown.UI.GameDataPanel` type guard before SaveSlots official-panel layout logic.
- Removed the global `UnityEngine.UI.GridLayoutGroup.set_constraintCount` normalization hook; `UI.NativeLayoutDiagnostics` now remains diagnostic-only for this title/pause layout issue.
- Removed temporary multi-line stack diagnostic logging used to identify the caller.

## Validation

- Passed:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `git diff --check` (line-ending warnings only)
  - DirectExe title settings smoke `docs/debug/evidence/GAME-SMOKE/20260613-143434`
  - DirectExe third-save pause layout smoke `docs/debug/evidence/GAME-SMOKE/20260613-143621`
  - DirectExe third-save MoreSaves official save UI smoke `docs/debug/evidence/GAME-SMOKE/20260613-144038`

## Evidence

- Diagnostic proof before the root fix: `GAME-SMOKE/20260613-142631` logged the full stack frames for the delayed title write, including `SaveSlotsService.TryResetLayoutSize`, `RestoreOfficialSavePanel`, `ApplyOfficialSavePanelPaging`, `EnsureOfficialSavePanelPageForSelection`, `GameDataPanelSelectPrefix`, and `HomePageUiState.<Show>b__13_0`.
- `GAME-SMOKE/20260613-143434` passed title button/menu screenshot checks and logged `Save.MoreSlotsUiPaging = not-required` with no `requestedCount=2`, no `Native UI layout normalized`, and no `GridLayoutGroup.constraintCount.Prefix`.
- `GAME-SMOKE/20260613-143621` passed pause-menu layout smoke: 14 samples over 12 seconds stayed `layoutConstraintCount=8`, `rowCount=1`, and `twoColumnObserved=False`, with clean process/fatal checks.
- `GAME-SMOKE/20260613-144038` revalidated MoreSaves after removing the Select hook: `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, `visibleSlotsAfterPaging=12`, `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, clean process exit, and no fatal instance popup.

## Rollback

- If 12-slot official save UI breaks, revert only the SaveSlots hook/status changes in `SaveSlotsFeature` and `SaveSlotsService`; do not restore the global `GridLayoutGroup.constraintCount` setter normalization unless a new non-SaveSlots writer is proven.

## Follow-Up

- If expanded 18+/24+ slots are reintroduced later, implement the selection/page hook through a GameDataPanel-only wrapper or a strict target guard before touching any `DolocGridUI<T>` inherited method.
