# 20260613-0008 Title Pause Layout Setter Normalization

- Date: 2026-06-13
- Status: superseded by `20260613-0013`
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user confirmed the fifth save now holds a fishing rod, and continued manual QA showed the title homepage still changed from one column to two columns while the in-save pause menu could cycle between horizontal and two-column/vertical layouts.
- Version: remains `0.5.1-alpha` / `0.5.1.0`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Superseded note: this was an intermediate workaround. `20260613-0013` proved the delayed title write came from the broad inherited SaveSlots `GameDataPanel.Select` hook reaching `HomePageTextMenu`/pause `MenuUI`; the final root fix removes that hook and no longer installs the global GridLayoutGroup setter normalization.
- Added a Harmony prefix on `UnityEngine.UI.GridLayoutGroup.set_constraintCount`.
- Normalization is scoped by native UI state and object identity: only the active `HomePageUiState.textMenu.slotLayoutGroup` is normalized to `1`, and only the active `MainMenuUiState.panel.menu.slotLayoutGroup` is normalized to the visible icon count.
- Removed the active `HomePageUiState.Update` / `MainMenuUiState.Update` layout repair loop; those Update paths now sample and log diagnostics only.
- Kept `HomePageUiState.RenderTextMenu` repair as an immediate owner-event fallback, but the delayed stale write is now intercepted before `constraintCount=2` lands.
- Added unit coverage for active title/pause GridLayoutGroup setter normalization and non-target GridLayoutGroup pass-through.

## Validation

- Passed:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - `git diff --check` (line-ending warnings only)
  - DirectExe third-save pause layout smoke `docs/debug/evidence/GAME-SMOKE/20260613-122313`
  - DirectExe title settings smoke `docs/debug/evidence/GAME-SMOKE/20260613-122550`
  - DirectExe HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-122714`
  - DirectExe third-save MoreSaves official save UI revalidation `docs/debug/evidence/GAME-SMOKE/20260613-123521`
  - Steam third-save HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-124240`

## Evidence

- `GAME-SMOKE/20260613-122550` proves `GridLayoutGroup.constraintCount.Prefix=True` and logs `requestedCount=2, normalizedCount=1` for `DolocTown.UI.HomePageTextMenu` while `HomePageUiState` is active.
- `GAME-SMOKE/20260613-122313` proves the native pause menu observed for 12 seconds stayed `slots=8`, `visibleSlots=8`, `layoutConstraintCount=8`, `rowCount=1`, and `twoColumnObserved=False` across 14 samples. It also has no repeated `Native UI layout repair ... before=2, after=8` loop from the prior repair-based approach.
- `GAME-SMOKE/20260613-122714` passed `HookProbe`, third-save `SaveLoaded`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`; process check found no `DolocTown.exe`.
- `GAME-SMOKE/20260613-123521` revalidated MoreSaves after the title/pause setter fix: `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, `visibleSlotsAfterPaging=12`, `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, clean process exit, and no fatal instance popup.
- `GAME-SMOKE/20260613-124240` revalidated the final Steam launch/exit path after installing the latest package: `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `SteamLaunchBlockedCount=0`, no fatal popup, and no `DolocTown.exe` residual process.
- Final local audit on 2026-06-13 reran Release build/test on the current worktree; both completed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.

## Rollback

- If manual QA still sees title/pause layout cycling, revert only the `GridLayoutGroup.constraintCount` prefix and keep the smoke/log diagnostics. Do not restore the old broad polling repair; it was already linked to visible layout cycling.

## Follow-Up

- User visual retest is still useful because the pause-menu smoke samples at one-second cadence and cannot prove every rendered frame. If cycling persists, the next step is to keep the setter hook as a diagnostic and log the owner identity for any active `MainMenuUiState` write that still escapes normalization.
