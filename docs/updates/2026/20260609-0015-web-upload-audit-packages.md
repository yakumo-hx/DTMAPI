# 20260609-0015 Web Upload Audit Packages

## Status

Verified.

## Source

- Request: the full audit packages are too large for web ChatGPT upload; provide a practical smaller alternative.
- Full Refactor audit package: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
- Full ActionSpeed branch audit package: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip`

## Changed Files

- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0015-web-upload-audit-packages.md`
- External web package folder: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- External web package zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
- External web package folder: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web`
- External web package zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web.zip`

## Summary

- Created compact web-upload companion packages for both audit packages.
- Kept buildable source snapshots, public API/architecture docs, focused audit docs, hook maps, reverse snippet metadata, smoke scripts, and compact smoke evidence.
- Removed screenshot-heavy `DTMAPI-evidence/` subfolders from the web packages.
- Removed official Workshop documentation assets from the web packages.
- Omitted oversized complete report zips from web packages when impractical for upload.
- Preserved the full audit packages as the source of complete evidence/report payloads.

## Validation

- Passed: Refactor web zip audit.
  - Zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
  - Size: about 1.7 MB.
  - Contains compact `result.json` evidence and `audit/report/WEB-REPORT-NOTE.md`.
- Passed: ActionSpeed branch web zip audit.
  - Zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web.zip`
  - Size: about 2.3 MB.
  - Contains compact `result.json` evidence and `audit/report/WEB-REPORT-NOTE.md`.
- Passed: no `.git`, `.tools`, `bin`, `obj`, `decompiled`, `input`, screenshot-heavy `DTMAPI-evidence`, DLL, EXE, PDB, NuGet, RAR, or 7Z entries in either web zip.
- Build was not rerun for the web packages; they are size-reduced companions derived from the already build-validated full audit packages.

## Evidence

- Refactor web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Refactor web zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
- ActionSpeed branch web package: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web`
- ActionSpeed branch web zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web.zip`

## Related Records

- `docs/updates/2026/20260609-0012-audit-package-camera-smoke-case-refresh.md`
- `docs/updates/2026/20260609-0014-actionspeed-branch-audit-package.md`

## Rollback Notes

- Delete the `*-web` folders and `*-web.zip` files if the compact upload packages are not needed.
- Keep the full audit packages for complete report and screenshot-heavy evidence review.

## Follow-Up

- When a reviewer needs full binary evidence, share the full audit package out-of-band instead of using web ChatGPT upload.
