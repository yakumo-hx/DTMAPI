# 20260610-0029 Post-Review Stabilization Audit Packages

## Status

Verified.

## Source Request

User requested the post-review stabilization route, including `OilCoalDropFeature`, API baseline `0.5.0-alpha`, and final full/web audit packages on `Refactor`.

## Summary

- Refreshed `tools/scripts/update-audit-package.ps1` default smoke evidence ids for the post-review stabilization batch.
- Final package generation targets:
  - Full package: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - Web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Full package keeps complete selected smoke evidence directories plus report zip payloads under `audit/report`.
- Web package keeps source/docs/key logs/result files and writes `WEB-REPORT-NOTE.md` instead of carrying oversized report zips or screenshot-heavy evidence.
- OilCoalDrop evidence folders are included even though those smoke modes did not export fresh report zips; the stale `latest-report.txt` pointers are documented in the OilCoalDrop update record and matrix.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/2026/20260610-0029-post-review-stabilization-audit-packages.md`
- `docs/updates/INDEX.md`

## Validation

- Expected pre-package validation is satisfied by the merged branch records:
  - Feature host failure throttle: `GAME-SMOKE/20260610-113540`, `20260610-113752`, `20260610-113902`, `20260610-114008`.
  - Diagnostics entry cap: `GAME-SMOKE/20260610-115014`, `20260610-115225`.
  - API baseline `0.5.0-alpha`: failed compatibility evidence `GAME-SMOKE/20260610-120641`, then passed smokes `20260610-121420`, `20260610-121631`.
  - StrongPlantingGun and AnimalViewer smoke case split: `GAME-SMOKE/20260610-122933`, `20260610-123042`.
  - OilCoalDrop feature split: `GAME-SMOKE/20260610-124357`, `20260610-124511`.
- Package generation command:
  - `tools/scripts/update-audit-package.ps1 -ReportZipPaths <post-review report zips>`
- Package self-audit command:
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`
- Self-audit checks `AUDIT-PACKAGE.md`, `VALIDATION-SUMMARY.md`, report notes, path control characters, hashtable interpolation text, forbidden `bin/obj/.tools`, forbidden binaries, and web package report-zip exclusion.

## Evidence Links

- Package script: `tools/scripts/update-audit-package.ps1`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Package roots:
  - `E:\Python_project\DTMAPI-audit-package-Refactor`
  - `E:\Python_project\DTMAPI-audit-package-Refactor-web`

## Rollback

- Regenerate the previous audit package with explicit earlier `-EvidenceIds`.
- Restore the previous default evidence list in `tools/scripts/update-audit-package.ps1`.

## Follow-Up

- Keep full package as the complete local audit artifact.
- Use the web package for browser upload/review contexts that reject large report or screenshot payloads.
