# 20260610-0041 Post-Review Next Stabilization Audit Packages

## Status

Verified.

## Source Request

User requested the e904d04 post-review stabilization route to merge the three high-priority branches into local `Refactor`, refresh the final full/web audit packages, and tag the result as `refactor-post-review-next-stabilization-20260610`.

## Summary

- Refreshed the default audit-package evidence list so the final package includes the latest e904d04 follow-up evidence:
  - ToolCollider callback isolation OneAction/Oil smokes.
  - Feature failure recovery Camera/ActionSpeed smokes.
  - FishingAutomation feature split AutoFishing/ActionSpeed smokes.
- Generated the final full audit package at `E:\Python_project\DTMAPI-audit-package-Refactor`.
- Generated the final compact web audit package at `E:\Python_project\DTMAPI-audit-package-Refactor-web`.
- Full package keeps complete selected evidence directories and report zip payloads under `audit/report`.
- Web package keeps source/docs/key logs/result files and intentionally omits screenshot-heavy `DTMAPI-evidence` directories plus report zips; `audit/report/WEB-REPORT-NOTE.md` explains the omission.
- No external package directory, package zip, report zip, evidence directory, DLL/EXE/PDB, `bin`, `obj`, `.tools`, or reverse build/decompiled source is committed to the repository.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0041-post-review-next-stabilization-audit-packages.md`

## Validation

- Final `Refactor` branch before packaging already had the three stabilization branches merged:
  - `codex/fix-toolcollider-callback-isolation`
  - `codex/fix-feature-failure-recovery-policy`
  - `codex/refactor-fishingautomation-feature-mechanical-split`
- `git diff --check` passed with CRLF warnings only for the package-record/script update.
- Earlier branch validation in this route passed:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - ToolCollider OneAction smoke `GAME-SMOKE/20260610-163813`
  - ToolCollider Oil/NewContent smoke `GAME-SMOKE/20260610-163928`
  - Feature recovery Camera smoke `GAME-SMOKE/20260610-164830`
  - Feature recovery ActionSpeed smoke `GAME-SMOKE/20260610-165043`
  - FishingAutomation AutoFishing smoke `GAME-SMOKE/20260610-170839`
  - FishingAutomation ActionSpeed regression smoke `GAME-SMOKE/20260610-170950`
- Package refresh command:
  - `tools/scripts/update-audit-package.ps1 -SourceRoot E:\Python_project\DTMAPI -OutputRoot E:\Python_project -BranchName Refactor -SourceCommit <final commit> -ReportZipPaths D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165010.zip,D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165123.zip,D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-171030.zip`
- Package self-audit command:
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly`
- Package self-audit checks passed for both full and web packages:
  - no hashtable interpolation text in package Markdown;
  - no disallowed control characters in package Markdown;
  - no `.git`, `.tools`, `bin`, or `obj` directories in package snapshots;
  - no DLL/EXE/PDB/NuGet/RAR/7Z payloads;
  - no zip payloads in the web package;
  - zip payloads only under `audit/report` in the full package;
  - no screenshot-heavy `DTMAPI-evidence` directories in the web package.

## Evidence Links

- Full package: `E:\Python_project\DTMAPI-audit-package-Refactor`
- Web package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Package zips:
  - `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
- Included latest runtime evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260610-163813`
  - `docs/debug/evidence/GAME-SMOKE/20260610-163928`
  - `docs/debug/evidence/GAME-SMOKE/20260610-164830`
  - `docs/debug/evidence/GAME-SMOKE/20260610-165043`
  - `docs/debug/evidence/GAME-SMOKE/20260610-170839`
  - `docs/debug/evidence/GAME-SMOKE/20260610-170950`

## Rollback

- Restore `tools/scripts/update-audit-package.ps1` default evidence list to the previous package baseline.
- Regenerate the external full/web packages from the previous tagged source commit if an earlier audit package is needed.
- Delete the external package directories/zips manually if the refreshed artifacts should not be retained.

## Follow-Up

- Use `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip` for browser upload review.
- Use `E:\Python_project\DTMAPI-audit-package-Refactor.zip` or the full directory for complete local audit evidence.
