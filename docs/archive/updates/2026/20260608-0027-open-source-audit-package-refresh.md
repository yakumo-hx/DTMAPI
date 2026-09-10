# 20260608-0027 Open Source And Audit Package Refresh

## Metadata

- Update ID: 20260608-0027
- Date: 2026-06-08
- Status: verified
- Source: User request to update the open-source share package and independent audit package from the latest `Refactor` worktree.
- Owner: Codex

## Summary

- Refreshed the clean source package at `E:\Python_project\DTMAPI-open-source-Refactor` from the latest `Refactor` worktree.
- Refreshed the independent audit package at `E:\Python_project\DTMAPI-audit-package-Refactor`.
- Regenerated `E:\Python_project\DTMAPI-audit-package-Refactor.zip`.
- Replaced the old static CameraView report with the latest CAMERA-PLAYABLE dynamic smoke report from `GAME-SMOKE/20260608-222542`.
- Added audit-package copies of the latest CAMERA-PLAYABLE dynamic QA checklist and update record.
- Kept the clean source package limited to source, test mods, tests, scripts, assets, public API/architecture docs, and `references/README.md`.
- Generated package-level `README.md` files because the current repository root does not contain a `README.md`.

## Changed Files

- `docs/updates/2026/20260608-0027-open-source-audit-package-refresh.md`
- `docs/updates/INDEX.md`

## Generated Artifacts

- Clean source package: `E:\Python_project\DTMAPI-open-source-Refactor`
  - File count: 230
  - Size: 2,493,551 bytes
- Audit package folder: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - File count: 297
  - Size: 21,414,387 bytes
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - Size: 4,910,437 bytes
- Audit report zip: `E:\Python_project\DTMAPI-audit-package-Refactor\audit\report\DTMAPI-CameraPlayable-20260608-222542-report.zip`
  - Entry count: 26

## Audit Package Contents

- `audit/docs/api/public-api-matrix.md`
- `audit/docs/hook-map/README.md`
- `audit/docs/hook-map/focused/Camera.md`
- `audit/docs/hook-map/focused/Fishing.md`
- `audit/docs/hook-map/focused/Save.md`
- `audit/docs/hook-map/focused/Input.md`
- `audit/docs/debug/regressions/smoke-matrix.md`
- `audit/docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- `audit/docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `audit/docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- `audit/reverse-snippets/`
- `audit/tools/scripts/run-game-smoke.ps1`
- `audit/tools/scripts/run-hook-probe.ps1`
- `audit/report/DTMAPI-CameraPlayable-20260608-222542-report.zip`

## Report Zip Contents

- `DTMAPI-startup-and-runtime.log`
- `HookProbe-and-camera-lines.log`
- `Player.log`
- `LogOutput.log`
- `result.json`
- `process-check.txt`
- `fatal-window-check.txt`
- Startup analysis/timeline files.
- `DTMAPI-evidence/CAMERA-PLAYABLE/20260608-222622/summary.txt`
- `DTMAPI-evidence/CAMERA-PLAYABLE/20260608-222622/camera-playable-dynamic-telemetry.csv`
- 4x dynamic screenshots: `dynamic-4x-start.png`, `dynamic-4x-mid.png`, `dynamic-4x-end.png`
- 2x dynamic screenshots: `dynamic-2x-start.png`, `dynamic-2x-mid.png`, `dynamic-2x-end.png`
- Legacy context screenshots: `zoom-before.png`, `zoom-4x.png`, `zoom-reset.png`

## Reverse Material Boundary

- Included only DTMAPI-authored reverse maps and selected symbol metadata for Camera/background, Save/Load, Workshop/mod loader, Fishing, and Input.
- Did not copy official DLLs, reverse `input/`, or complete decompiled `.cs` files.
- The reverse snippet README records the redistribution boundary.

## Validation

- Initial package build attempt with PATH `dotnet` failed because the system command has no SDK installed; validation then used the repository-local SDK at `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe`.
- Passed: clean source package solution build:
  - `& E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe build DTMAPI.sln -c Release --nologo`
  - Build completed with 0 warnings and 0 errors.
- Passed: clean source package unit tests:
  - `& E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe tests\DTMAPI.UnitTests\bin\Release\net8.0\DTMAPI.UnitTests.dll`
  - Output: `DTMAPI.UnitTests: OK`
- Passed: cleaned build-created `bin/obj` directories from package folders after validation.
- Passed: package exclusion audit on clean source package and audit package:
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, and `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.rar`, `.7z`, `.nupkg`, `.snupkg`, unexpected `.zip`, or `Assembly-CSharp.dll`.
- Passed: report zip entry audit:
  - Required entries all present, including DTMAPI startup/runtime log, HookProbe/camera filtered log, Player.log, BepInEx LogOutput.log, result, exit checks, dynamic telemetry CSV, and six dynamic screenshots.
- Passed: audit package zip entry audit:
  - Required latest audit docs, report zip, and `src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs` are present.

## Evidence

- Source smoke evidence packaged from: `docs/debug/evidence/GAME-SMOKE/20260608-222542`
- Dynamic CAMERA-PLAYABLE evidence packaged from: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-222622`
- Report zip packaged at: `E:\Python_project\DTMAPI-audit-package-Refactor\audit\report\DTMAPI-CameraPlayable-20260608-222542-report.zip`
- Final audit zip packaged at: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`

## Related Records

- Previous package creation: `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- Public build exclusion: `docs/updates/2026/20260608-0022-public-build-fishbreeding-exclusion.md`
- Smoke result schema v2: `docs/updates/2026/20260608-0023-smoke-result-schema-v2.md`
- CameraPlayable dynamic smoke: `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`

## Rollback Notes

- Delete and regenerate `E:\Python_project\DTMAPI-open-source-Refactor`, `E:\Python_project\DTMAPI-audit-package-Refactor`, and `E:\Python_project\DTMAPI-audit-package-Refactor.zip` from the previous package recipe if this refresh needs to be reverted.
- Remove this update record and its index row if the generated package refresh is discarded.

## Follow-Up

- If a separate source zip is needed, generate it after another exclusion audit so build output and private evidence do not leak into the clean source artifact.
