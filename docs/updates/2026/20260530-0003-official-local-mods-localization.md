# 20260530-0003: Official Local Mods And Localization

## Metadata

- Update ID: 20260530-0003
- Date: 2026-05-30
- Status: implemented
- Source: user requested formal local official-mod path migration, Chinese-first DTMAPI UI, and respect for Doloc Town's official Mod manager.
- Owner: Codex

## Summary

- Moved the title DTMAPI Settings entry to the top-left anchor to avoid the official settings button.
- Added a minimal Chinese-first DTMAPI UI text service with English fallback for title pages, tabs, buttons, status, errors, hooks, locks, and log export.
- Added `ITranslationHelper` and config-page display names so migrated mods can provide localized config text.
- Packaged the five migrated mods as official local packages under `MODS/Yuuka_DTMAPI_*` with `info.json`, `icon.png`, `preview.png`, `Content/DTMAPI/manifest.json`, renamed `Yuuka.DTMAPI.*.dll`, and `i18n`.
- Made DTMAPI scan official local packages through Unity's persistent data path and respect `SAVE/mod_infos.json` enablement state.
- Moved stale game `Mods/Yuuka.DTMAPI.*` copies and stale `DTMAPI.HookProbeMod` to `DTMAPI/backups/...` so they cannot bypass the official Mod manager.
- Fixed UTF-8 BOM handling for generated JSON manifests and made the installer write no-BOM JSON.

## User-Visible Impact

- The player-facing DTMAPI menu is on the title screen's top-left.
- The DTMAPI menu defaults to Simplified Chinese when no English UI language is requested.
- Migrated mod config pages can display Chinese names and labels.
- The five migrated feature mods are now official-local packages; DTMAPI does not load them until the official path records them as enabled.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.Core/Json/JsonFile.cs`
- `src/DTMAPI.Core/Manifesting/ManifestModels.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.Core/Services/TranslationService.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `tools/scripts/common.ps1`
- `tools/scripts/install-to-game.ps1`
- `testmods/ActionSpeedMod/*`
- `testmods/AutoFishingMod/*`
- `testmods/OneActionCompleteMod/*`
- `testmods/FishBreedingAssistantMod/*`
- `testmods/AnimalHusbandryProgressMod/*`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`

## Validation

- `tools/scripts/build.ps1`: passed, 0 warnings, 0 errors; `DTMAPI.UnitTests: OK`.
- Official package structure: verified all five `Yuuka_DTMAPI_*` packages have `info.json`, `icon.png`, `preview.png`, `Content/DTMAPI/manifest.json`, `Content/DTMAPI/dtmapi-package.json`, and `Yuuka.DTMAPI.*.dll`.
- Installer backup behavior: moved stale game `Mods/Yuuka.DTMAPI.*` folders to `D:\Steam\steamapps\common\Doloc Town\DTMAPI\backups\official-local-migration-20260530-223638` and stale HookProbe to `disabled-testmods-20260530-223638`; old `DLK_*` official-local folders were not deleted.
- Title smoke with default official state: `GAME-SMOKE/20260530-224842` passed; logs collected in `GAME-SMOKE/20260530-225039`.
- Official disabled/unknown gating: `GAME-SMOKE/20260530-225039/DTMAPI-latest.log` discovered 7 DTMAPI-capable folders and skipped all five `Yuuka.DTMAPI.*` official-local packages because `Local.Yuuka_DTMAPI_*` enablement was not present.
- Official enabled-state smoke: `OFFICIAL-ENABLE/20260530-225821` temporarily added `Local.Yuuka_DTMAPI_ActionSpeed enabled=true`, `GAME-SMOKE/20260530-225828` passed, and logs in `GAME-SMOKE/20260530-225930` show `Yuuka.DTMAPI.ActionSpeed` loaded while the other four stayed skipped. `mod_infos.json` was restored after the run.

## Evidence

- Disabled/locked official-local evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260530-224842/result.json`
  - `docs/debug/evidence/GAME-SMOKE/20260530-225039/DTMAPI-latest.log`
  - `docs/debug/evidence/GAME-SMOKE/20260530-224842/process-check.txt`
  - `docs/debug/evidence/GAME-SMOKE/20260530-224842/fatal-window-check.txt`
- Enabled-state evidence:
  - `docs/debug/evidence/OFFICIAL-ENABLE/20260530-225821/mod_infos.before.json`
  - `docs/debug/evidence/OFFICIAL-ENABLE/20260530-225821/mod_infos.enabled-action-speed.json`
  - `docs/debug/evidence/OFFICIAL-ENABLE/20260530-225821/mod_infos.restored.json`
  - `docs/debug/evidence/GAME-SMOKE/20260530-225828/result.json`
  - `docs/debug/evidence/GAME-SMOKE/20260530-225930/DTMAPI-latest.log`
- JSON/BOM evidence:
  - Reinstalled `Content/DTMAPI/manifest.json` starts with bytes `7B-0D-0A`, not UTF-8 BOM.

## Not Fully Verified

- The in-game official Mod manager visual list was not clicked through with desktop automation. The Computer Use runtime failed to start in this sandbox with an `EPERM` path error, so visual confirmation of icons/previews in the official UI remains a manual check.
- The automated enabled-state smoke edited `mod_infos.json` as a controlled state-file simulation, not by clicking the official UI toggle.

Manual check:

1. Launch Doloc Town through Steam.
2. Open the official Mod manager.
3. Confirm the five `Yuuka_DTMAPI_*` entries appear with DTMAPI icon/preview and localized names/descriptions.
4. Enable `Yuuka_DTMAPI_ActionSpeed`, restart if the game requires it, and confirm DTMAPI loads `Yuuka.DTMAPI.ActionSpeed`.
5. Disable it again and confirm DTMAPI skips or locks it with the official enablement reason.

## Related Records

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Debug index: `docs/debug/INDEX.md`

## Rollback Notes

- Revert the installer official-local package generation and reinstall DTMAPI if the official package structure causes a game-side regression.
- Restore backed-up stale game `Mods/Yuuka.DTMAPI.*` folders only for compatibility diagnosis; ordinary player use should keep migrated feature mods under the official local `MODS/Yuuka_DTMAPI_*` path.
- Remove `DTMAPI_DOLOC_PERSISTENT_ROOT` override only if Unity persistent path resolution is replaced by an explicit host-provided persistent path.

## Follow-Up

- Add desktop/manual evidence for the official Mod manager list once UI control is available.
- Consider replacing the environment override bridge with an explicit `IRuntimeHost` persistent-data property if more host types are added.
