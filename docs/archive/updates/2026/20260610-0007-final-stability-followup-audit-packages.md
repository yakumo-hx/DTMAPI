# 20260610-0007 Final Stability Follow-Up Audit Packages

## Metadata

- Update ID: 20260610-0007
- Date: 2026-06-10
- Status: verified
- Source: User requested the final Refactor full/web audit packages after the stability follow-up route.
- Owner: Codex

## Scope

- Refresh the final `Refactor` full and web audit packages after:
  - lifecycle callback isolation,
  - audit package Markdown cleanup,
  - FishRoe smoke case split,
  - diagnostics snapshot mod status,
  - ChestLocator feature split,
  - CameraView manual QA gate.
- Keep complete selected evidence/report payloads in the full package.
- Keep source/docs/key logs/result files in the web package and omit oversized screenshot-heavy evidence/report payloads.
- Keep package directories and zips outside the git repository.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0007-final-stability-followup-audit-packages.md`

## Summary

- Extended the controlled package script default evidence list with the latest 2026-06-10 route evidence:
  - `GAME-SMOKE/20260610-021014` FishRoe smoke case split.
  - `GAME-SMOKE/20260610-022332` diagnostics mod status Camera smoke.
  - `GAME-SMOKE/20260610-022543` diagnostics mod status ActionSpeed smoke.
  - `GAME-SMOKE/20260610-024237` ChestLocator feature split smoke.
- Retained the lifecycle isolation evidence and the earlier 2026-06-09 follow-up evidence in the package defaults.
- Regenerated the external full/web packages with `tools/scripts/update-audit-package.ps1`.
- Package root `AUDIT-PACKAGE.md` and `VALIDATION-SUMMARY.md` remain the source of truth for the exact final source commit and generation time.

## Validation

- Passed before final packaging commit:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
- Passed preliminary package generation:
  - `tools/scripts/update-audit-package.ps1`
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`
- The final package regeneration is run after this update record is committed so the package source snapshot includes this record and the final commit hash.
- Not run: game smoke for this final packaging step. The package step changes only package metadata/default evidence selection and uses previously verified smoke evidence.
- Not run: package-source build/test from inside the full package, to avoid creating `bin/obj` inside the delivered package. The package-source build/test path was already verified by the package cleanup branch.

## Package Outputs

- Full directory: `E:\Python_project\DTMAPI-audit-package-Refactor`
- Full zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
- Web directory: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Web zip: `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`

## Evidence

- Full package keeps complete selected `audit/evidence/GAME-SMOKE` directories and report zip payloads under `audit/report`.
- Web package keeps compact evidence logs/results and writes `audit/report/WEB-REPORT-NOTE.md` instead of including oversized report zips.
- Package self-audit checks:
  - required package paths exist,
  - generated Markdown does not contain `System.Collections.Hashtable`,
  - generated Markdown does not contain disallowed control characters,
  - package source excludes `.git`, `.tools`, `bin`, `obj`, DLL/EXE/PDB/zip payloads outside the controlled audit report/evidence area,
  - web package excludes screenshot-heavy `DTMAPI-evidence` directories.

## Related Records

- `docs/updates/2026/20260610-0001-lifecycle-callback-isolation.md`
- `docs/updates/2026/20260610-0002-audit-package-markdown-cleanup.md`
- `docs/updates/2026/20260610-0003-fishroe-smoke-case-file.md`
- `docs/updates/2026/20260610-0004-diagnostics-snapshot-mod-status.md`
- `docs/updates/2026/20260610-0005-chestlocator-feature-split.md`
- `docs/updates/2026/20260610-0006-camera-view-manual-qa-gate.md`

## Rollback Notes

- Re-run `tools/scripts/update-audit-package.ps1` from the desired previous commit if the final package needs to be regenerated from an older baseline.
- Remove the 2026-06-10 default evidence ids from `tools/scripts/update-audit-package.ps1` only if the final package scope is intentionally narrowed.

## Follow-Up

- Tag the final `Refactor` commit as `refactor-stability-followup-20260610` after the final package regeneration and self-audit pass.
