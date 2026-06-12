# 20260612-0001 Release Hygiene Hardening 2

Date: 2026-06-12

Status: verified

Area: release/installer/uninstaller/report/smoke/audit-package/docs

## Summary

Continued the `0.5.0-alpha` release hygiene branch with web audit-package evidence self-audit, safer install/uninstall handling for `mod_infos.json` and DTMAPI-owned official-local packages, report/status metadata diagnostics, clearer selected release mod vs developer-local mod install modes, and a dedicated MoreSaves official save UI smoke path. This does not change gameplay behavior, public mod APIs, hook/status IDs, or API stability levels.

## Source Request

User asked to implement "DTMAPI Release Hygiene Hardening 2 Plus" from `codex/release-050-alpha-hygiene` / `b49590b`, focusing on audit package self-consistency, install/uninstall safety, report diagnostics, release wording, and fresh MoreSaves evidence.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/uninstall-dtmapi.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- selected first-batch `testmods/*/manifest.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/releases/0.5.0-alpha-release-hygiene-report.md`
- `docs/releases/0.5.0-alpha-developer-preview-checklist.md`
- `docs/guides/install-dev-preview.md`
- `docs/guides/uninstall-dtmapi.md`
- `docs/guides/migrate-from-old-doloc-smapi.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Details

- Updated compact web audit package defaults to the release hygiene smoke IDs from 2026-06-12 and added self-audit checks that listed evidence IDs match across root/package notes and each copied evidence directory has `result.json`.
- Kept web evidence compact by copying `result.json`, `summary.txt`, logs, startup analysis, process/fatal checks, and `latest-report.txt`, while omitting report zips and screenshot-heavy evidence directories.
- Hardened install writes for `SAVE/mod_infos.json`: read failures now stop the install, writes go through a temp file, existing `mod_infos.json` is backed up to `DTMAPI/backups/install-*/mod_infos.before.json`, and numeric priority handles `Int32`, `Int64`, numeric strings, and missing values.
- Added `install-to-game.ps1 -InstallPublishedModsOnly` and `-InstallAllDevOfficialMods`. Runtime Workshop payload installs still skip official-local mods; developer local default remains the full dev official-local package set.
- Added `uninstall-dtmapi.ps1 -RemoveOfficialLocalPackages`, limited to marker-owned DTMAPI packages with `Content/DTMAPI/dtmapi-package.json`, with package and `mod_infos.json` backups plus uninstall-state fields.
- Added status output for DTMAPI-owned official-local package count and next-step player guidance.
- Added install/release metadata files to diagnostics report zips when present, and added install/release present/missing/read-error state plus version and legacy counts to `dtmapi-summary.txt`.
- Added `run-game-smoke.ps1 -AutoExerciseMoreSavesOfficialSaveUi` with result fields `MoreSavesOfficialSaveUi` and `MoreSavesOfficialSaveUiEvidence`.
- Added short source manifest descriptions for Zoom, ChestLocatorEnhancer, MoreSaves, and Y-key console.
- Corrected release docs from the old `LegacyModsDetected` wording to the actual `LegacyDetections` schema field.

## Validation

Passed:

- `git diff --check` (line-ending warnings only).
- PowerShell AST parse for `install-to-game.ps1`, `uninstall-dtmapi.ps1`, `check-dtmapi-status.ps1`, `update-audit-package.ps1`, `run-game-smoke.ps1`, `package-report.ps1`, and `release-common.ps1`.
- `tools/scripts/build.ps1 -Configuration Release`.
- `tools/scripts/test.ps1 -Configuration Release`.
- `tools/scripts/install-to-game.ps1 -DryRun`.
- Ignored-path normal install checks for `-InstallPublishedModsOnly` and developer-all official-local modes; published-only created 8 marker-owned packages, developer-all created 14, and `mod_infos.before.json` was recorded in install-state.
- `tools/scripts/uninstall-dtmapi.ps1 -DryRun`.
- Ignored-path dry-run and normal uninstall checks with `-RemoveOfficialLocalPackages`; only marker-owned `Content/DTMAPI/dtmapi-package.json` packages were backed up/removed and an unmarked player package was preserved.
- `tools/scripts/check-dtmapi-status.ps1`; output included runtime/install-state/release-manifest/report status, 14 DTMAPI-owned official-local packages, 17 legacy detections, and next-step guidance.
- `tools/scripts/package-report.ps1 -CaseId GAME-SMOKE`.
- Manager MVP, HookProbe, and MoreSaves official save UI smokes listed below.
- Diagnostics report export from the Manager smoke produced `dtmapi-report-20260612-015800.zip`, containing `install-state.json`, `release-manifest.json`, and `dtmapi-summary.txt` install/release fields.
- `tools/scripts/update-audit-package.ps1 -WebOnly`.
- `tools/scripts/update-audit-package.ps1 -SelfAuditOnly -WebOnly`.

## Evidence

- `GAME-SMOKE/20260612-015402`: MoreSaves official save UI smoke passed with `RunStatus=Passed`, `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`. Logs include `Save.MoreSlotsApi=configured-official-archive-count`, `Feature.SaveSlots=ready`, and `Smoke.MoreSavesOfficialSaveUi=verified` with `archiveFileCount=12`, `panelSlotCount=12`, and `renderedSlots=12`.
- `GAME-SMOKE/20260612-015716`: Manager MVP title smoke passed with Status/Mods/Errors/Hooks/Features/Logs pages, report export button/state text, screenshots, clean process/fatal checks, and report `dtmapi-report-20260612-015800.zip`.
- `GAME-SMOKE/20260612-015832`: HookProbe runtime smoke passed with `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Existing release hygiene evidence remains in the compact web package default list: `GAME-SMOKE/20260612-000310`, `000435`, `000606`, `000817`, `000929`, `001041`, `001151`, `001359`, `001516`, and `002033`.
- Compact web package self-audit passed after copying the existing release hygiene evidence plus `GAME-SMOKE/20260612-015402`, `GAME-SMOKE/20260612-015716`, and `GAME-SMOKE/20260612-015832`.

## Rollback Notes

Rollback by reverting this update. The change should remove the additional release-hygiene script safety, audit-package self-audit, report metadata inclusion, MoreSaves smoke switch, selected release manifest descriptions, and docs without changing gameplay features or public abstractions.

## Follow-Up

- Full Workshop official enablement flow.
- Public GitHub release packaging.
- SaveSlots paging and expanded-slot lifecycle/manual matrix.
- Camera background sync.
- More complete player-facing release package signing/upload automation.
