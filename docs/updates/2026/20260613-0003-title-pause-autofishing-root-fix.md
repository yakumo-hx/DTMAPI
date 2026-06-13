# 20260613-0003 Title Pause AutoFishing Root Fix

- Date: 2026-06-13
- Status: implemented
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user clarified that title starts single-column then becomes two-column without pressing ESC; in-save pause starts horizontal, then cycles between two-column/vertical and horizontal; AutoFishing `InstantBite` means directly hook and enter the native minigame.
- Version: stays `0.5.1-alpha` / `0.5.1.0`; no additional version bump.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Root Cause

- UI: the `20260613-0002` active pause-menu polling repair fought a later native/UI writer which repeatedly set the same grid `constraintCount` back to `2`. That explains the observed horizontal/two-column cycling. The new implementation removes the polling mutation path and keeps the active `MainMenuUiState.Update` probe diagnostic-only.
- UI diagnosis/fix: concrete postfix diagnostics now cover `HomePageTextMenu.ResetLayoutSize`, `MenuUI.ResetLayoutSize`, `MenuUI.SetCapacity`, `MainMenuPanel.OnStartShow`, and `GameDataPanel.SetCapacity`, with call stacks. Homepage repair is constrained to the concrete `HomePageTextMenu` instance. Pause repair now runs only on concrete `MenuUI` owner events while `MainMenuUiState` is active, using the native `MainMenuPanel.OnStartShow` invariant: visible icon count in one row.
- AutoFishing: `AgentStateFishingWait.NextState()` only enters `AgentStateFishingBattle` after a bite-ready fish plus a player reel input. The prior DTMAPI `InstantBite` path forced bite readiness and native bite cue, but did not route the post-bite state, so manual testing could see a bite without entering the minigame. `InstantBite` now directly reels the forced native bite: fish enter `AgentStateFishingBattle`; non-fish or native `skipFishingGame` routes to `AgentStateFishingPull`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild` wrote the local Workshop staging package to `E:\Python_project\DTMAPI\dist\workshop-packages`.
- Installed from `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI\Content\DTMAPIInstaller\tools\install-to-game.ps1 -InstallBepInEx`.
- `tools/scripts/check-dtmapi-status.ps1` reported `[OK] Required DTMAPI install files are present.`
- Installed GameBridge evidence: `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\DTMAPI.GameBridge.DolocTown.dll`, length `975360`, last write `2026-06-13 07:57:45 +08:00`.
- Installed title icon evidence remains present: `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png`, length `8018`.
- Exit/process check after install found no `DolocTown.exe` process.
- Unit coverage now expects `InstantBite` after a forced bite-ready state to resolve to `ReelNativeBite`, while `SkipMiniGame` still wins over auto-complete.
- Third-save visual confirmation remains pending.

## Pending Manual Evidence

- Install the rebuilt package and retest title homepage, in-save pause after the delayed repro window, and third-save AutoFishing `InstantBite`.
- Expected UI logs: concrete `HomePageTextMenu.ResetLayoutSize`, `MenuUI.ResetLayoutSize`, `MenuUI.SetCapacity`, or `GameDataPanel.SetCapacity` diagnostics with call stacks; no `MainMenuUiState.Update.MenuUI` repair loop.
- Expected AutoFishing log: `Smoke.AutoFishingInstantBite=verified` with `action=ReelNativeBite` and `autoHook=AgentStateFishingBattle` for a fish roll.

## Rollback

- UI rollback: remove concrete ResetLayoutSize/GameDataPanel diagnostics and repairs; do not restore the active 500 ms pause-menu repair loop because it caused visible cycling.
- AutoFishing rollback: restore `InstantBite` to timing-only. This is not recommended because user-facing semantics require immediate hook and native minigame entry.
