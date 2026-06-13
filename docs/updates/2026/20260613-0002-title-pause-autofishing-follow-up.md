# 20260613-0002 Title Pause AutoFishing Follow-up

- Date: 2026-06-13
- Status: implemented
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user retest after installing the fixed package reported the title/homepage menu still switching to two columns, the in-save pause menu still switching from a horizontal icon row to a two-column/vertical layout, and AutoFishing `InstantBite` still not producing an observable bite.
- Version: stays `0.5.1-alpha` / `0.5.1.0`; no additional version bump.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Fixed the `InstantBite` forced-bite duration to use the game's native `DolocAPI.GlobalParameter.PullTiming` semantics, with the native `skipFishingWait` `100f` branch preserved only when the game itself requests it. The previous DTMAPI path wrote `_fishOnHookDuration=100f` unconditionally, which could leave a bite-ready wait state sitting for a long time and make manual testing look like no bite happened.
- Kept `InstantBite` as timing-only: it rolls the fish, sets the wait state bite-ready, invokes the native bite tip, refreshes the rod line/fish shadow, and then waits for player input unless `SkipMiniGame` or `AutoCompleteMiniGame` is also enabled.
- Replaced the broad removed homepage guard with native-owner repairs:
  - `HomePageUiState.RenderTextMenu` now repairs only the `HomePageTextMenu` instance to `constraintCount=1` after render. This addresses the observed stale `HomePageTextMenu` `constraintCount=2` case where native `TextMenu.Render()` does not reset layout because `totalCapacity=5` and `lineCapacity=1` did not change.
  - While `MainMenuUiState` is active, DTMAPI samples `MainMenuUiState.panel.menu` every 500 ms and repairs only that `MenuUI` to the native-owner invariant from `MainMenuPanel.OnStartShow`: `constraintCount = visible icon count`.
- `UI.NativeLayoutDiagnostics` still logs layout counts and call stacks, but it now also reports `UI.HomePageTextMenuLayout` / `UI.MainMenuLayout` as `corrected` when these narrow repairs apply.

## Known Facts

- Latest installed-game logs proved the title issue before this fix: `HomePageUiState.RenderTextMenu` saw `HomePageTextMenu` with `totalCapacity=5`, `lineCapacity=1`, but `layoutConstraintCount=2` after returning through `GameDataUiState` / `ModChangeListUiState`. Native `DolocVerticalUI.SetCapacity(5)` did not repair it because the logical capacity values were unchanged.
- Native `MainMenuPanel.OnStartShow()` calls `menu.ResetLayoutSize(menu.slots.Count(x => x.isVisible))`, so the expected pause menu layout is the visible icon count in one row. The repair mirrors that native owner instead of guessing a DTMAPI title-context rule.
- AutoFishing log evidence before this fix showed `nativeTipInvoked=True`, but also showed the previous forced bite kept `_fishOnHookDuration` on DTMAPI's hard-coded `100f` path rather than the normal native pull timing.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild` wrote the local Workshop staging package to `E:\Python_project\DTMAPI\dist\workshop-packages`.
- Installed from `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI\Content\DTMAPIInstaller\tools\install-to-game.ps1 -InstallBepInEx`.
- `tools/scripts/check-dtmapi-status.ps1` reported `[OK] Required DTMAPI install files are present.`
- Installed GameBridge evidence: `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\DTMAPI.GameBridge.DolocTown.dll`, length `970752`, last write `2026-06-13 07:12:07 +08:00`.
- Installed title icon evidence remains present: `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png`, length `8018`.
- Exit check before/after install found no `DolocTown.exe` process.

## Pending Manual Evidence

- Retest the title ESC case, delayed in-save pause menu case, and AutoFishing `InstantBite` in the third save with the installed follow-up.
- Expected logs after retest:
  - `Native UI layout repair ... hookId=UI.HomePageTextMenuLayout` or hook status `UI.HomePageTextMenuLayout = corrected` if the homepage stale layout appears.
  - `Native UI layout repair ... hookId=UI.MainMenuLayout` or hook status `UI.MainMenuLayout = corrected` if the pause icon row drifts after `OnStartShow`.
  - `Smoke.AutoFishingInstantBite = verified ... hookDuration=<native PullTiming> ... nativeTipInvoked=True ... source=AgentStateFishingWait.OnEnter Postfix`.

## Rollback

- UI rollback: remove `RepairHomePageTextMenuLayout`, `UpdateActiveMainMenuLayout`, and `RepairGridConstraintCount`; diagnostics would return to logging only.
- AutoFishing rollback: remove `ResolveNativeFishOnHookDuration` and restore the previous `_fishOnHookDuration` write. This is not recommended because it caused bite-ready waits to use a DTMAPI-only long timeout instead of the game's normal pull window.
