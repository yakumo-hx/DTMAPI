# 20260609-0007 Audit Package Feature Host Refresh

## Metadata

- Update ID: 20260609-0007
- Date: 2026-06-09
- Status: verified
- Source: User request to package a new audit package after the GameBridge feature-host update.
- Owner: Codex

## Summary

- Refreshed `E:\Python_project\DTMAPI-audit-package-Refactor` from the current `Refactor` worktree.
- Regenerated `E:\Python_project\DTMAPI-audit-package-Refactor.zip`.
- Included the latest GameBridge feature-host source files, including `IGameBridgeFeature` and `DolocTownGameBridge.Update.cs`.
- Synchronized latest Camera hook map, smoke matrix, update records, and API matrix into `audit/docs`.
- Bundled the latest GameBridge feature-host CameraPlayable smoke evidence folder and complete report zip.
- Preserved audit-only focused Fishing/Save/Input hook maps from the prior audit package, because the workspace currently keeps only the Camera focused hook map.

## Changed Files

- `docs/updates/2026/20260609-0007-audit-package-feature-host-refresh.md`
- `docs/updates/INDEX.md`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`

## Validation

- Passed: audit package build from `E:\Python_project\DTMAPI-audit-package-Refactor.new`.
  - `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: package build-output cleanup after validation.
- Passed: package exclusion audit:
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, and `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.rar`, `.7z`, `.nupkg`, and `.snupkg`.
  - Unexpected nested zip files found: 0; the complete report zip under `audit/report/` is allowed.
- Passed: required-file audit for root `LICENSE`, `NOTICE.md`, `README.md`, sanitized `AGENTS.md`, `Directory.Build.props`, `DTMAPI.sln`, latest GameBridge feature-host source files, focused audit docs, smoke scripts, evidence folder, and report zip.
- Passed: audit package source-root local-path audit found no `E:\Python_project`, `D:\Steam`, or `D:\steam`; evidence-only audit docs and logs intentionally preserve runtime game paths.
- Passed: final audit package zip entry audit found no `.git`, `.tools`, `bin`, `obj`, build binaries, NuGet packages, or unexpected nested zips.

## Evidence

- Audit package folder: `E:\Python_project\DTMAPI-audit-package-Refactor`
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
- Bundled smoke evidence: `audit/evidence/GAME-SMOKE/20260609-031302`
- Bundled complete report zip: `audit/report/DTMAPI-GameBridgeFeatureHost-20260609-031339-report.zip`
- Bundled update records include:
  - `audit/docs/updates/2026/20260609-0004-camera-feature-split.md`
  - `audit/docs/updates/2026/20260609-0005-camera-feature-merge-refactor.md`
  - `audit/docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`
  - `audit/docs/updates/2026/20260609-0007-audit-package-feature-host-refresh.md`

## Related Records

- Feature-host update: `docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`
- Previous audit package refresh: `docs/updates/2026/20260609-0003-open-source-audit-package-refresh.md`
- Camera merge validation: `docs/updates/2026/20260609-0005-camera-feature-merge-refactor.md`

## Rollback Notes

- Delete and regenerate `E:\Python_project\DTMAPI-audit-package-Refactor` and `E:\Python_project\DTMAPI-audit-package-Refactor.zip` from the previous package recipe if this refresh is discarded.
- Keep `.tools`, `bin`, `obj`, and build binaries out of final package artifacts even when package build validation is rerun.

## Follow-Up

- Consider turning the audit-package refresh recipe into a checked-in script so future audit package updates do not rely on inline shell orchestration.
