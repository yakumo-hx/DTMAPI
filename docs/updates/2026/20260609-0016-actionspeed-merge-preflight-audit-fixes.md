# 20260609-0016 ActionSpeed Merge Preflight Audit Fixes

## Status

Implemented.

## Source

- Request: execute the full ActionSpeed merge and follow-up refactor plan.
- Preflight finding: the ActionSpeed smoke evidence and audit package notes had stale or incomplete evidence pointers before merge.

## Changed Files

- `docs/api/public-api-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-120153/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0016-actionspeed-merge-preflight-audit-fixes.md`
- External audit package notes and zips for the Refactor and ActionSpeed branch packages.

## Summary

- Updated the `IActionSpeedApi` matrix row to cite the latest ActionSpeed feature-host split evidence: `GAME-SMOKE/20260609-133725`; earlier branch-package evidence remains `GAME-SMOKE/20260609-120153`.
- Updated the `ICameraViewApi` matrix row to cite the latest Camera feature-host smoke evidence: `GAME-SMOKE/20260609-110528`, while retaining `GAME-SMOKE/20260608-150914` as earlier lease rebuild evidence.
- Replaced the stale ActionSpeed `latest-report.txt` pointer that referenced Camera report `20260609-110608`.
- Corrected audit package status notes so their bundled evidence/report IDs match the package contents.

## Validation

- Passed: branch build/test/ActionSpeed smoke were rerun before merging `codex/refactor-actionspeed-feature` back to `Refactor`.
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-133725`
  - Result: `RunStatus=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Passed: refreshed package zips were audited after the note/report-pointer fixes.
  - `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip`
  - `E:\Python_project\DTMAPI-audit-package-Refactor-web.zip`
  - `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature-web.zip`
  - Zip entry audit found no `.git`, `.tools`, `bin`, `obj`, `decompiled`, `input`, DLL, EXE, PDB, NuGet, RAR, 7Z, or unexpected nested zip entries.

## Evidence

- ActionSpeed branch-package smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260609-120153`
- ActionSpeed pre-merge smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260609-133725`
- ActionSpeed compact report zip: `docs/debug/evidence/GAME-SMOKE/20260609-133725.zip`
- Camera smoke evidence referenced by API matrix: `docs/debug/evidence/GAME-SMOKE/20260609-110528`

## Related Records

- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`
- `docs/updates/2026/20260609-0014-actionspeed-branch-audit-package.md`
- `docs/updates/2026/20260609-0015-web-upload-audit-packages.md`

## Rollback Notes

- Restore the previous API matrix evidence text only if the merge is abandoned and the branch package is regenerated from the earlier snapshot.
