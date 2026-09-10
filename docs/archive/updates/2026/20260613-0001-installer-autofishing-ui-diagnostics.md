# 20260613-0001 Installer AutoFishing UI Diagnostics

- Date: 2026-06-13
- Status: implemented
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user retest after old SMAPI uninstall and new DTMAPI install found the packaged title icon missing, the official title/pause menus still switching to a two-column layout, and AutoFishing `InstantBite` not producing a visible bite after waiting in game.
- Version: stays `0.5.1-alpha` / `0.5.1.0`; no additional version bump.
- Follow-up: `20260613-0002-title-pause-autofishing-follow-up.md` supersedes the UI/AutoFishing runtime behavior from this diagnostic record; the installer asset fix in this record remains current.

## Changed Files

- `tools/scripts/install-to-game.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsService.cs`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Fixed packaged runtime icon installation. The installer now resolves `assets/branding/dtmapi-icon.png` from the Release output, the Workshop package payload, or the source repository before copying it to the installed runtime plugin. The status checker now treats the installed DTMAPI title icon asset as a required runtime file.
- Reworked the AutoFishing wait-phase entry. `AgentStateFishingWait.OnEnter` now applies wait automation immediately, with `OnPlay` retained as a fallback. `InstantBite` still only owns bite timing, but the forced bite now invokes the native private `InvokeFishOnHookTip` path after `RollFish`, so the visible/audio bite cue is driven by the game's own bite notification instead of only mutating internal wait fields.
- Added diagnostics-only native UI layout tracing for the title and pause menu regression. The new `UI.NativeLayoutDiagnostics` feature records official layout calls, counts, visible slots, current UI state, and a truncated stack when `HomePageTextMenu`, `MenuUI`, `MainMenuPanel`, or two-column layout values are observed. It does not mutate official title or pause menu layout.

## Known Facts

- The previous `HomePageTextMenu` single-column guard was already removed in update `20260612-0017`, and the user still reproduced both title two-column and pause icon layout changes after reinstall. This branch therefore does not add another guessed layout guard.
- Old SMAPI uninstall plus DTMAPI reinstall did not make the layout symptom disappear, so the next useful evidence is a native layout call stack from the same session that reproduces the title ESC and in-save pause menu cases.
- Existing internal AutoFishing smoke/status could mark wait automation as applied while the player saw no bite. The likely gap was that the old path set wait-state fields and render helpers but did not call the native bite-tip method that owns the player's visible bite cue.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild` wrote the local Workshop staging package to `E:\Python_project\DTMAPI\dist\workshop-packages`.
- Installed from the generated package script at `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI\Content\DTMAPIInstaller\tools\install-to-game.ps1 -InstallBepInEx`.
- `tools/scripts/check-dtmapi-status.ps1` reported `[OK] DTMAPI title icon asset` and `[OK] Required DTMAPI install files are present.`
- Installed icon evidence: `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png`, length `8018`.
- Exit check: no `DolocTown.exe` process was found after the install/package validation.

## Pending Manual Evidence

- Third-save visual/manual game confirmation is still pending for the new `InstantBite` visible bite cue.
- Title/pause layout is intentionally not claimed fixed in this update. Reproduce the title ESC and in-save pause icon cases, then inspect `Native UI layout diagnostic ...` lines and the `UI.NativeLayoutDiagnostics` hook status to identify the native owner or DTMAPI caller that sets line/constraint count `2`.
- This pending item was converted into the `20260613-0002` native-owner repair follow-up after the user retest still reproduced the issue.

## Rollback

- Installer rollback: remove `Resolve-DtmApiRuntimeIconSource`; package installs would again rely only on source-relative assets and may fall back to text when installing from a packaged payload.
- AutoFishing rollback: remove the `OnEnter` wait automation call and native `InvokeFishOnHookTip` invocation; this would return to the previous internal-state-only bite path.
- UI diagnostics rollback: remove `NativeUiLayoutDiagnosticsFeature`/service and its callback wiring. This has no gameplay/layout mutation to unwind.
