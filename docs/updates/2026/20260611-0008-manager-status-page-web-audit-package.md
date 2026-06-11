# 20260611-0008 Manager Status Page Web Audit Package

## Summary

Refreshed the compact web audit package route after merging the Manager Status page wiring back to `Refactor`, keeping final package generation web-only and adding the final Manager Status title UI smoke to the default evidence set.

## Source Request

User requested the Manager Status page real wiring plan: merge the isolated branch back to `Refactor`, verify build/test plus title UI smoke, generate only the compact web audit package, and do not generate a full package or full zip.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260611-0007-manager-ui-status-page.md`
- `docs/updates/2026/20260611-0008-manager-status-page-web-audit-package.md`

## Details

- Added `GAME-SMOKE/20260611-092012` as the first default web package evidence item.
- Kept the existing final Camera, ActionSpeed, AutoFishing, and HookProbe evidence in the default compact package set.
- The package route remains compact/web-only for this update; screenshot-heavy evidence directories and report zip payloads are omitted by design.
- No public API members, ConfigMenu contracts, GameBridge hooks/features, hook/status IDs, or smoke result schemas changed in this package step.

## Validation

- Passed final merged `Refactor` validation before this package record:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoOpenTitleSettingsMenu -SaveSlot 3 -TimeoutSeconds 240`
- Final title UI evidence: `docs/debug/evidence/GAME-SMOKE/20260611-092012`.
- Package commands for the final source commit:
  - `tools/scripts/update-audit-package.ps1 -WebOnly`
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly -WebOnly`

## Evidence

- `GAME-SMOKE/20260611-092012` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- `process-check.txt` records no leftover `DolocTown.exe`.
- `fatal-window-check.txt` records no fatal instance popup.

## Package Output

- Web package directory: `E:\Python_project\DTMAPI-audit-package-Refactor-web`.
- No full package and no full package zip are generated for this route.

## Rollback Notes

Restore the previous default evidence list in `tools/scripts/update-audit-package.ps1` and remove this package record. Runtime code rollback is tracked by `20260611-0007`.

## Follow-Up

- Keep Export Report exception safety as a separate branch.
- Keep Manager hook/feature severity refinement as a separate branch.
- Keep fallback overlay and remaining Manager pages as later UI slices.
