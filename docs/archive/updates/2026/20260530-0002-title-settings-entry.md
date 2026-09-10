# 20260530-0002: Title Settings Entry

## Metadata

- Update ID: 20260530-0002
- Date: 2026-05-30
- Status: implemented
- Source: user requested the formal DTMAPI main-menu settings entry and to skip the temporary F8/F10 overlay repair.
- Owner: Codex

## Summary

- Added a DTMAPI Settings button/menu on the Doloc Town title homepage using the DTMAPI branding asset and an EventSystem fallback for button input.
- Added Config, Mods, Status, Errors, Hooks, and Logs pages to the reflected Unity UI menu.
- Implemented bool, number, text, choice, and keybind config controls with save, reset, and cancel.
- Updated local migrated mod IDs/folders to `Yuuka.DTMAPI.*` and kept ordinary mods under `Mods/`, not `BepInEx/plugins`.
- Added official-path-aware discovery for local official mods and Workshop items; DTMAPI shows status/locks instead of taking over official enable/disable/order.
- Bumped the runtime/API version to `0.1.11`.

## User-Visible Impact

- Players use the title-page DTMAPI Settings button as the primary config entry.
- F8/F10 overlay repair is deferred; those keys are not the player-facing route for this goal.
- DTMAPI can edit enabled DTMAPI mod configs and shows locked/restart explanations for source-owned or unavailable mods.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/run-hook-probe.ps1`
- `testmods/*/manifest.json`
- `testmods/HookProbeMod/ModEntry.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/OneActionCompleteMod/ModEntry.cs`
- `testmods/FishBreedingAssistantMod/*`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/issues/ISSUE-003-hotkey-openconfig-no-overlay.md`
- `docs/api/public-api-matrix.md`

## Validation

- `tools/scripts/build.ps1`: passed, 0 warnings, 0 errors; `DTMAPI.UnitTests: OK`.
- Title smoke: `GAME-SMOKE/20260530-202152` passed with `TitleSettingsButton=true`, `TitleSettingsMenu=true`, `NoFatalInstanceWindow=true`, `ProcessExited=true`, `ForcedClose=false`.
- Third-save HookProbe: `HOOK-PROBE/20260530-202404` passed with SaveLoaded, OneSecond, UI pages, log export, experimental hook exercises, migrated config save/cancel/reset, no leftover process, and no fatal popup.

## Evidence

- Title UI logs: `docs/debug/evidence/GAME-SMOKE/20260530-202310/DTMAPI-latest.log`
  - `DTMAPI title settings UI created an EventSystem for button input.`
  - `DTMAPI title settings button visible on HomePageUiState.`
  - `DTMAPI title settings button clicked.`
  - `DTMAPI title settings menu opened.`
- Hook/config logs: `docs/debug/evidence/HOOK-PROBE/20260530-202404/DTMAPI-latest.log`
  - `HookProbe OneSecondUpdateTicked OK second=1`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
  - `HookProbe ConfigPage Visible/CancelNoWrite/SaveWrite/ResetDefault OK` for `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, and `Yuuka.DTMAPI.OneActionComplete`.
- Exit/fatal checks:
  - `docs/debug/evidence/GAME-SMOKE/20260530-202328/process-check.txt`
  - `docs/debug/evidence/GAME-SMOKE/20260530-202328/fatal-window-check.txt`

## Related Records

- Debug: `docs/debug/issues/ISSUE-003-hotkey-openconfig-no-overlay.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback Notes

- Revert the title UI by removing `ReflectedTitleMenuSettingsUi` wiring from `BootstrapPlugin`.
- Revert version constants to the previous `0.1.x` value if rolling back the runtime behavior.
- Reinstall to the game after rollback so `BepInEx/plugins/DTMAPI` and `Mods/Yuuka.DTMAPI.*` match the workspace build.

## Follow-Up

- If a future goal explicitly restores F8/F10 overlay support, resume `ISSUE-003` as a diagnostic overlay task rather than the main player config entry.
