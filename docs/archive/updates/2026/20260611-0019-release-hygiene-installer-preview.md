# 20260611-0019 Release Hygiene Installer Preview

Date: 2026-06-11

Status: verified

Area: release/installer/uninstaller/manager-ui/mod-metadata/docs

## Summary

Prepared the `0.5.0-alpha` Developer Preview release hygiene slice: release/install-state metadata, DryRun install path, uninstall/status scripts, Runtime Workshop batch templates, selected 8-mod Workshop staging schema, Manager install-state summary, and player migration/install/uninstall docs. This does not change gameplay behavior, public mod APIs, hook/status IDs, or API stability levels.

## Source Request

User asked to prepare a new DTMAPI Developer Preview release batch: new DTMAPI Runtime/Installer, eight new DTMAPI mods, no reuse of old DLK/SMAPI Workshop items, safe migration guidance, install/uninstall/status checks, and player-facing descriptions using author `Yuuka`.

## Changed Files

- `tools/scripts/release-common.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/uninstall-dtmapi.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/release/runtime-workshop/1_install_dtmapi.bat`
- `tools/release/runtime-workshop/2_uninstall_dtmapi.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `src/DTMAPI.Core/Manager/DtmManagerRuntimeModelProvider.cs`
- `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- selected release mod `manifest.json` / `official-info.json` under `testmods/`
- `docs/guides/install-dev-preview.md`
- `docs/guides/uninstall-dtmapi.md`
- `docs/guides/migrate-from-old-doloc-smapi.md`
- `docs/releases/0.5.0-alpha-developer-preview-checklist.md`
- `docs/releases/0.5.0-alpha-release-hygiene-report.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/workflows/player-feedback-community-loop.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Details

- Added shared release helper functions for version constants, UTF-8 no BOM JSON, selected published mod definitions, legacy SMAPI/DLK detection, release manifest creation, and install-state creation.
- Added `install-to-game.ps1 -DryRun`; dry run skips build/install side effects and prints the planned install-state JSON.
- Normal install writes `DTMAPI/release-manifest.json` and `DTMAPI/install-state.json` and copies DTMAPI-owned helper scripts to `DTMAPI/tools`.
- Added `uninstall-dtmapi.ps1`; default uninstall backs up and removes DTMAPI-owned runtime/helper/state files only, while keeping player Mods, Workshop content, reports, configs, backups, and BepInEx core.
- Added `check-dtmapi-status.ps1` and Runtime Workshop batch templates for install/uninstall/status.
- Added Workshop staging script for the Runtime package and the selected eight DTMAPI feature mod packages using `Content/DTMAPI`.
- Added internal Manager install-state summary and displayed install-state presence/version/legacy counters/uninstall availability on Status and Logs.
- Updated the selected release mod metadata to keep `UniqueID`, set author to `Yuuka`, and require `0.5.0-alpha` in source manifests.
- Added player install, uninstall, and old SMAPI/DLK migration guides.

## Validation

- `git diff --check`: passed.
- PowerShell AST parse: passed for `install-to-game.ps1`, `uninstall-dtmapi.ps1`, `check-dtmapi-status.ps1`, `build-release-workshop-packages.ps1`, `release-common.ps1`, and `package-report.ps1`.
- `tools/scripts/build.ps1 -Configuration Release`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed.
- `tools/scripts/install-to-game.ps1 -DryRun`: passed and printed planned `release-manifest.json` / `install-state.json` content without copying or moving files.
- Normal install to an ignored local test path with `-SkipBuild -SkipOfficialLocalMods`: passed and wrote `DTMAPI/release-manifest.json` plus `DTMAPI/install-state.json`.
- `tools/scripts/uninstall-dtmapi.ps1 -DryRun`: passed and confirmed the default removal set keeps player Mods, Workshop content, reports, configs, backups, and BepInEx core.
- Normal uninstall from the ignored local test path: passed and wrote `DTMAPI/uninstall-state-*.json`.
- `tools/scripts/check-dtmapi-status.ps1`: passed against the local game path.
- `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild`: passed and staged the Runtime plus selected eight mod package layouts under the ignored `dist/workshop-packages` directory.
- `tools/scripts/package-report.ps1 -CaseId GAME-SMOKE`: passed.

## Evidence

- Manager MVP title smoke: `GAME-SMOKE/20260612-000310`, `RunStatus=Passed`; Manager summary includes install state `present`, `version:0.5.0-alpha`, legacy detection counts, and `uninstall:available`.
- HookProbe runtime smoke: `GAME-SMOKE/20260612-000435`, `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks.
- Zoom smoke: `GAME-SMOKE/20260612-000606`, `Zoom=Passed`.
- ActionSpeed smoke: `GAME-SMOKE/20260612-000817`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`.
- OneAction smoke: `GAME-SMOKE/20260612-000929`, OneAction resource/wrong-tool/fuel/feed checks passed.
- ChestLocator smoke: `GAME-SMOKE/20260612-001041`, `ChestLocatorEnhancer=Passed`.
- Y-key console smoke: `GAME-SMOKE/20260612-001151`, debug console inventory/weather/teleport/time/movement checks passed.
- FishRoe/AnimalViewer rendering smoke: `GAME-SMOKE/20260612-001359`, experimental hook smoke passed.
- Animal panel UI smoke: `GAME-SMOKE/20260612-001516`, `AnimalViewerUi=Passed`.
- Steam launch HookProbe smoke: `GAME-SMOKE/20260612-002033`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks.

MoreSaves release metadata and package layout were validated in the package staging check. This branch did not produce a fresh `Smoke.MoreSavesOfficialSaveUi` line because the current smoke script has no dedicated MoreSaves/official-save-UI switch and the fresh launch path did not enter the official save UI evidence branch. The unchanged MoreSaves behavior remains covered by prior verified evidence such as `GAME-SMOKE/20260610-100337` and `GAME-SMOKE/20260610-095455`.

## Rollback Notes

Rollback by reverting this release hygiene slice. It should remove release scripts, Manager install-state display, metadata/doc changes, and package staging support without touching gameplay features or public abstractions.

## Follow-Up

- Full Workshop official enablement flow.
- Public GitHub release packaging.
- SaveSlots paging.
- Camera background sync.
- Copy-selected-row Manager support polish.
