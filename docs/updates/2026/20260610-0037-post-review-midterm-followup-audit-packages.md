# 20260610-0037 Post-Review Midterm Follow-Up Audit Packages

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, including feature failure status throttle, OilCoalDrop lifecycle cleanup, shared ToolCollider hook ownership, OilCoalDrop smoke split, diagnostics aggregate counters, API version compatibility policy, Fishing native responsibility review, and final full/web audit packages on `Refactor`.

## Summary

- Refreshed `tools/scripts/update-audit-package.ps1` default smoke evidence ids for the post-review midterm follow-up batch.
- Final package generation targets:
  - Full package: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - Web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Full package keeps complete selected smoke evidence directories plus report zip payloads under `audit/report`.
- Web package keeps source/docs/key logs/result files and writes `WEB-REPORT-NOTE.md` instead of carrying oversized report zips or screenshot-heavy evidence.
- Docs-only policy/review branches are represented by their durable review records plus retained supporting smoke evidence.
- OilCoalDrop cleanup/shared/smoke-case smokes that did not export fresh report zips are included as evidence directories; their stale `latest-report.txt` pointers remain documented in their branch update records and are not cited as fresh report payloads.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/2026/20260610-0037-post-review-midterm-followup-audit-packages.md`
- `docs/updates/INDEX.md`

## Validation

- Expected pre-package validation is satisfied by the merged branch records:
  - Feature failure status throttle: `GAME-SMOKE/20260610-133933`, `20260610-134145`, `20260610-134259`, `20260610-134406`.
  - OilCoalDrop pending cleanup: `GAME-SMOKE/20260610-135456`, `20260610-135605`.
  - Shared ToolCollider hit hooks: `GAME-SMOKE/20260610-140419`, `20260610-140641`.
  - OilCoalDrop smoke case split: `GAME-SMOKE/20260610-141632`.
  - Diagnostics aggregate counters: `GAME-SMOKE/20260610-142415`, `20260610-142626`.
  - API version compatibility policy retained evidence: `GAME-SMOKE/20260610-120641`, `20260610-121420`, `20260610-121631`.
  - Fishing native responsibility retained evidence: `GAME-SMOKE/20260608-080848`, `20260609-170646`, `20260610-012052`, `20260610-035752`.
- Final package-prep validation:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
- Package generation command:
  - `tools/scripts/update-audit-package.ps1 -ReportZipPaths <post-review-midterm report zips>`
- Package self-audit command:
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`
- Self-audit checks `AUDIT-PACKAGE.md`, `VALIDATION-SUMMARY.md`, report notes, path control characters, hashtable interpolation text, forbidden `bin/obj/.tools`, forbidden binaries, and web package report-zip exclusion.

## Evidence Links

- Package script: `tools/scripts/update-audit-package.ps1`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- API reviews:
  - `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md`
  - `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- Package roots:
  - `E:\Python_project\DTMAPI-audit-package-Refactor`
  - `E:\Python_project\DTMAPI-audit-package-Refactor-web`

## Rollback

- Regenerate the previous audit package with explicit earlier `-EvidenceIds`.
- Restore the previous default evidence list in `tools/scripts/update-audit-package.ps1`.

## Follow-Up

- Keep full package as the complete local audit artifact.
- Use the web package for browser upload/review contexts that reject large report or screenshot payloads.
