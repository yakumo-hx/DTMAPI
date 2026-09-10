# 20260608-0021 Open Source And Audit Package

## Metadata

- Update ID: 20260608-0021
- Date: 2026-06-08
- Status: verified
- Source: User request to prepare the `Refactor` branch open-source share package and independent audit package.
- Owner: Codex

## Summary

- Added root `DTMAPI.sln` containing every project currently listed by `tools/scripts/build.ps1` under `src/`, `tests/`, and `testmods/`.
- Exported a clean source-only package to `E:\Python_project\DTMAPI-open-source-Refactor`.
- Exported an independent audit package to `E:\Python_project\DTMAPI-audit-package-Refactor` and `E:\Python_project\DTMAPI-audit-package-Refactor.zip`.
- Added audit-only copies of the latest API matrix, hook map extracts, smoke matrix, CameraZoom manual QA failure review, smoke scripts, reverse metadata snippets/maps, and the complete CameraView smoke report zip.

## Changed Files

- `DTMAPI.sln`
- `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- `docs/updates/INDEX.md`

## Generated Artifacts

- Clean source package: `E:\Python_project\DTMAPI-open-source-Refactor`
  - File count: 222
  - Size: 2,440,660 bytes
- Audit package folder: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - File count: 287
  - Size: 18,885,658 bytes
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - Size: 2,537,912 bytes
- Audit report zip: `E:\Python_project\DTMAPI-audit-package-Refactor\audit\report\DTMAPI-CameraView-20260608-150914-report.zip`

## Audit Package Contents

- `audit/docs/api/public-api-matrix.md`
- `audit/docs/hook-map/README.md`
- `audit/docs/hook-map/focused/Camera.md`
- `audit/docs/hook-map/focused/Fishing.md`
- `audit/docs/hook-map/focused/Save.md`
- `audit/docs/hook-map/focused/Input.md`
- `audit/docs/debug/regressions/smoke-matrix.md`
- `audit/docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `audit/reverse-snippets/`
- `audit/tools/scripts/run-game-smoke.ps1`
- `audit/tools/scripts/run-hook-probe.ps1`
- `audit/report/DTMAPI-CameraView-20260608-150914-report.zip`

## Reverse Material Boundary

- Included only DTMAPI-authored reverse maps and selected symbol metadata CSVs for Camera/background, Save/Load, Workshop/mod loader, Fishing, and Input.
- Did not copy official DLLs, reverse `input/`, or complete decompiled `.cs` files.
- The reverse snippet README explicitly records this redistribution boundary.

## Validation

- Passed: root solution build using repository-resolved .NET SDK:
  - `& $dotnet build DTMAPI.sln -c Release --nologo`
  - Build completed with 0 warnings and 0 errors.
- Passed: package exclusion audit on clean source package and audit package:
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.rar`, `.7z`, `.nupkg`, `.snupkg`, unexpected `.zip`, or `Assembly-CSharp.dll`.
- Passed: report zip entry audit:
  - Includes `DTMAPI-startup-and-runtime.log`, `HookProbe-and-camera-lines.log`, `Player.log`, `LogOutput.log`, `result.json`, `process-check.txt`, `fatal-window-check.txt`, startup analysis files, and `CAMERA-PLAYABLE/20260608-150952` screenshot evidence.

## Evidence

- Source smoke evidence packaged from: `docs/debug/evidence/GAME-SMOKE/20260608-150914`
- Report zip packaged at: `E:\Python_project\DTMAPI-audit-package-Refactor\audit\report\DTMAPI-CameraView-20260608-150914-report.zip`
- Hook map focused extracts contain:
  - Camera: 32 lines
  - Fishing: 35 lines
  - Save: 107 lines
  - Input: 17 lines

## Related Records

- CameraView rebuild: `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- Manual QA failure review: `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback Notes

- Remove `DTMAPI.sln` if the repository should not expose a root solution file.
- Delete regenerated external artifacts under `E:\Python_project\DTMAPI-open-source-Refactor`, `E:\Python_project\DTMAPI-audit-package-Refactor`, and `E:\Python_project\DTMAPI-audit-package-Refactor.zip` if a new export is needed.
- Remove this update record and its index row if rolling back the packaging trace.

## Follow-Up

- If a public release zip is needed later, rerun the package audit after any new build or smoke output is generated so no binary/runtime evidence leaks into the clean source package.
