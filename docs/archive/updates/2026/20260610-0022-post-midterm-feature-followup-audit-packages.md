# 20260610-0022 Post-Midterm Feature Follow-Up Audit Packages

## Status

Verified.

## Source Request

User requested the post-midterm follow-up route, including hook hardening, SaveSlots smoke split, `StrongPlantingGunFeature`, `AnimalViewerFeature`, and final full/web audit packages.

## Summary

- Refreshed `tools/scripts/update-audit-package.ps1` default smoke evidence ids for the post-midterm follow-up batch.
- Final package generation targets:
  - Full package: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - Web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Full package keeps complete selected smoke evidence directories plus report zip payloads under `audit/report`.
- Web package keeps source/docs/key logs/result files and writes `WEB-REPORT-NOTE.md` instead of carrying oversized report zips or screenshot-heavy evidence.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/2026/20260610-0022-post-midterm-feature-followup-audit-packages.md`
- `docs/updates/INDEX.md`

## Validation

- Expected pre-package validation is satisfied by the merged branch records:
  - Hook callback failure throttle: `GAME-SMOKE/20260610-092620`, `20260610-092819`, `20260610-092928`, `20260610-093037`, `20260610-093148`.
  - Optional dependency warning semantics: `GAME-SMOKE/20260610-094039`, `20260610-094313`.
  - SaveSlots refresh throttle: `GAME-SMOKE/20260610-095455`.
  - SaveSlots smoke case split: `GAME-SMOKE/20260610-100337`.
  - StrongPlantingGun feature split: `GAME-SMOKE/20260610-101436`.
  - AnimalViewer feature split: `GAME-SMOKE/20260610-103119`.
- Package generation command:
  - `tools/scripts/update-audit-package.ps1 -ReportZipPaths <latest post-midterm report zips>`
- Package self-audit command:
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`
- Self-audit checks `AUDIT-PACKAGE.md`, `VALIDATION-SUMMARY.md`, report notes, path control characters, hashtable interpolation text, forbidden `bin/obj/.tools`, forbidden binaries, and web package report-zip exclusion.

## Evidence Links

- Package script: `tools/scripts/update-audit-package.ps1`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`

## Rollback

- Regenerate the previous audit package with explicit earlier `-EvidenceIds`.
- Restore the previous default evidence list in `tools/scripts/update-audit-package.ps1`.

## Follow-Up

- Keep full package as the complete local audit artifact.
- Use the web package for upload/review contexts that reject large report or screenshot payloads.
