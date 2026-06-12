# 20260612-0005 - Audit Package Quoted Path Fix

## Status

Verified

## Source Request

User requested a fresh audit package after the Crops/Harvesting manual QA confirmation.

## Changed Files

- `tools/scripts/update-audit-package.ps1`

## Summary

Updated the audit package source snapshot step to call `git ls-files` with `core.quotepath=false`.
This keeps tracked non-ASCII file paths as normal paths instead of Git's quoted C-style escape form, which PowerShell/Windows path APIs cannot treat as a valid filename.

Also replaced direct calls to `[System.IO.Path]::GetRelativePath(...)` with a script-local helper that works under the Windows PowerShell runtime used by the release/audit scripts.

## Validation

- Initial `update-audit-package.ps1 -WebOnly` failed before this fix with `Illegal characters in path` while packaging tracked source files.
- The next package attempt exposed the existing Windows PowerShell compatibility issue for `[System.IO.Path]::GetRelativePath(...)`; the helper fix is included in this same audit-package hardening record.
- Follow-up validation is recorded in the package generation output for this branch.

## Evidence

- Crops/Harvesting API smoke: `docs/debug/evidence/GAME-SMOKE/20260612-072544/`.
- QA fixture/runtime load smoke: `docs/debug/evidence/GAME-SMOKE/20260612-085717/`.
- Manual QA confirmation: `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`.

## Rollback

Revert the `core.quotepath=false` option if future packaging must intentionally consume Git's quoted path output.

## Follow-up

Keep the compact web package self-audit enabled so missing evidence directories or malformed package markdown are caught before upload.
