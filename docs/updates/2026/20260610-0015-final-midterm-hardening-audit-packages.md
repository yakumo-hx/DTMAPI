# 20260610-0015 Final Midterm Hardening Audit Packages

## Status

Verified.

## Source Request

User requested the complete midterm Refactor hardening route and final full/web audit package refresh on `Refactor`.

## Summary

- Refreshed the external full package at `E:\Python_project\DTMAPI-audit-package-Refactor`.
- Refreshed the external web-upload package at `E:\Python_project\DTMAPI-audit-package-Refactor-web`.
- Generated corresponding zips:
  - `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
- Used explicit smoke evidence ids for the midterm hardening branches so the package reflects the current `Refactor` route instead of the older default evidence list.

## Included Evidence IDs

- `GAME-SMOKE/20260610-035216` hook safe fallback ChestLocator
- `GAME-SMOKE/20260610-035326` hook safe fallback FishRoe/AnimalViewer
- `GAME-SMOKE/20260610-035434` hook safe fallback ActionSpeed
- `GAME-SMOKE/20260610-035639` hook safe fallback OneAction
- `GAME-SMOKE/20260610-035752` hook safe fallback AutoFishing
- `GAME-SMOKE/20260610-040940` feature status throttle Camera
- `GAME-SMOKE/20260610-041156` feature status throttle ActionSpeed
- `GAME-SMOKE/20260610-042459` ChestLocator merged policy
- `GAME-SMOKE/20260610-043619` diagnostics status codes Camera
- `GAME-SMOKE/20260610-043830` diagnostics status codes ActionSpeed
- `GAME-SMOKE/20260610-045414` CustomEntity registry-contract naming
- `GAME-SMOKE/20260610-050506` ChestLocator smoke case split
- `GAME-SMOKE/20260610-051735` SaveSlots feature split

## Full/Web Difference

- Full package keeps the selected complete evidence directories, including screenshot-heavy `DTMAPI-evidence` directories, plus report zip payloads under `audit/report/`.
- Web package keeps source, docs, key logs, `result.json`, startup analysis, Unity/BepInEx logs, process checks, and fatal-window checks; it intentionally omits complete report zips and screenshot-heavy evidence folders.

## Validation

- `git diff --check` passed on `Refactor`.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Package generation command used explicit midterm evidence ids and report zip paths.
- `tools/scripts/update-audit-package.ps1 -SelfAuditOnly` passed for both full and web package roots.
- External package directories and zips were not committed to git.

## Rollback

- Remove the external package directories/zips and regenerate from a prior `Refactor` commit if the audit target needs to move back.
- Re-run `tools/scripts/update-audit-package.ps1 -SelfAuditOnly` after any regeneration before sharing.

## Follow-Up

- Keep the web package as the browser-upload artifact when full evidence exceeds web upload limits.
- Keep full package as the authoritative review artifact whenever screenshot-heavy evidence or report zip payloads are needed.
