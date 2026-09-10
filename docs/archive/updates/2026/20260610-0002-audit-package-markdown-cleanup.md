# 20260610-0002 Audit Package Markdown Cleanup

## Metadata

- Update ID: 20260610-0002
- Date: 2026-06-10
- Status: verified
- Source: User requested the Refactor stability follow-up route, step `codex/chore-audit-package-markdown-cleanup`.
- Owner: Codex

## Scope

- Add a checked-in audit-package generation script at `tools/scripts/update-audit-package.ps1`.
- Regenerate the external full and web Refactor audit package directories with clean package Markdown.
- Add package self-audits for:
  - accidental hashtable interpolation text such as `System.Collections.Hashtable`;
  - control characters in package root/report Markdown;
  - disallowed `.git`, `.tools`, `bin`, `obj`, binary, archive, and oversized web payloads.
- Preserve full/web split semantics:
  - full package keeps complete selected evidence directories and report zips under `audit/report/`;
  - web package keeps compact logs/results and omits `DTMAPI-evidence/` plus zip payloads.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0002-audit-package-markdown-cleanup.md`
- External full package folder: `E:\Python_project\DTMAPI-audit-package-Refactor`
- External full package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
- External web package folder: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- External web package zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`

## Summary

- Replaced the previous ad hoc package Markdown generation path with a reusable script that stages packages in `.new` directories, self-audits them, then replaces the external package directories only after checks pass.
- Wrote package root `AUDIT-PACKAGE.md`, `VALIDATION-SUMMARY.md`, and report notes using explicit string formatting so paths such as `testmods/`, `tools/`, `assets/`, `audit/docs/`, and `bin` no longer become tab/bell/backspace control characters.
- Formatted evidence entries through object properties so smoke ids render as `GAME-SMOKE/<id>` instead of `$(System.Collections.Hashtable.Id)`.
- Regenerated both external package directories and confirmed their Markdown no longer contains control characters or hashtable interpolation text.

## Validation

- Passed: package script parse check via `[scriptblock]::Create((Get-Content tools/scripts/update-audit-package.ps1 -Raw))`.
- Passed: `tools/scripts/update-audit-package.ps1 -SkipZip`.
- Passed: `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`.
- Passed package-source build from `E:\Python_project\DTMAPI-audit-package-Refactor`:
  - `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed package-source test from `E:\Python_project\DTMAPI-audit-package-Refactor`:
  - `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Superseded validation note: the first package-source build used a 120-second command timeout during initial restore/build and was rerun with a longer timeout; the rerun passed.
- Game smoke: not run for this packaging-only branch.

## Evidence

- Full package: `E:\Python_project\DTMAPI-audit-package-Refactor`
- Full package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
- Web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Web package zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
- Package self-audit covers:
  - `AUDIT-PACKAGE.md`
  - `VALIDATION-SUMMARY.md`
  - `audit/report/REPORT-NOTE.md`
  - `audit/report/WEB-REPORT-NOTE.md`

## Related Records

- `docs/updates/2026/20260609-0031-final-refactor-followup-audit-packages.md`
- `docs/updates/2026/20260610-0001-lifecycle-callback-isolation.md`

## Rollback Notes

- Delete and regenerate `E:\Python_project\DTMAPI-audit-package-Refactor` and `E:\Python_project\DTMAPI-audit-package-Refactor-web` with a known-good package recipe if this script is discarded.
- Do not commit the external package directories, package zips, or generated report/evidence payloads.

## Follow-Up

- After this branch is merged back to `Refactor`, rerun `tools/scripts/update-audit-package.ps1` from `Refactor` so the external package root documents name `Refactor` as the source branch.
