# 20260609-0014 ActionSpeed Branch Audit Package

## Status

Verified.

## Source

- Request: create a separate audit package for the secondary `codex/refactor-actionspeed-feature` branch.
- Branch: `codex/refactor-actionspeed-feature`
- Package target: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature`
- Zip target: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip`

## Changed Files

- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0014-actionspeed-branch-audit-package.md`
- External audit package folder: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature`
- External audit package zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip`

## Summary

- Created a branch-specific audit package for the ActionSpeed feature-host split without overwriting the `Refactor` audit package.
- Included the current branch source snapshot with `Features/ActionSpeed/ActionSpeedFeature.cs`, `ActionSpeedService.cs`, and `ActionSpeedHookBridge.cs`.
- Included branch docs for the ActionSpeed feature-host split, hook-map evidence, smoke matrix row, and update records.
- Preserved audit-only focused hook maps for Camera, Fishing, Save, and Input.
- Preserved audit-only reverse-snippet metadata for Camera/background, Save/Load, Workshop/mod loader, Fishing, and Input.
- Bundled the latest ActionSpeed third-save smoke evidence and report zip.

## Validation

- Passed: branch build/test/smoke from update `20260609-0013`.
- Passed: staged audit package Release build through `tools/scripts/build.ps1 -Configuration Release`.
- Passed: package cleanup removed generated `.tools`, `bin`, and `obj` directories after package build validation.
- Passed: package exclusion audit found no `.git`, `.tools`, `bin`, `obj`, official DLL/EXE/PDB binaries, NuGet packages, RAR/7Z archives, or unexpected nested zips.
- Passed: source-root local-path audit found no local Steam/workspace paths outside evidence/report materials.
- Passed: final zip entry audit confirmed expected source, audit docs, ActionSpeed evidence, and report entries.

## Evidence

- Audit package folder: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature`
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip`
- Bundled smoke evidence: `audit/evidence/GAME-SMOKE/20260609-120153`
- Bundled report zip: `audit/report/DTMAPI-ActionSpeedFeature-20260609-120153-report.zip`
- Source smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260609-120153`
- Source report zip: `docs/debug/evidence/GAME-SMOKE/20260609-120153.zip`

## Related Records

- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-120153`

## Rollback Notes

- Delete `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature` and `E:\Python_project\DTMAPI-audit-package-codex-refactor-actionspeed-feature.zip` if this branch-specific audit package is discarded.
- Keep the `Refactor` audit package untouched; this package is only for the ActionSpeed secondary branch.

## Follow-Up

- After this branch is merged back to `Refactor` and verified there, remove this branch-specific audit package if it is no longer needed for external review.
