# 20260608-0029 Open Source Audit Package Hygiene Refresh

## Metadata

- Update ID: 20260608-0029
- Date: 2026-06-08
- Status: verified
- Source: User request to update the open-source share package and independent audit package from the latest `Refactor` worktree after public source hygiene fixes.
- Owner: Codex

## Summary

- Refreshed `E:\Python_project\DTMAPI-open-source-Refactor` after the public source hygiene fix.
- Refreshed `E:\Python_project\DTMAPI-audit-package-Refactor` from the same source snapshot.
- Regenerated `E:\Python_project\DTMAPI-audit-package-Refactor.zip`.
- Synchronized root `README.md`, `LICENSE`, `NOTICE.md`, sanitized `AGENTS.md`, `DTMAPI.sln`, build/install scripts, public docs, `references/README.md`, and `references/COPY-MANIFEST.md`.
- Synchronized `testmods/FishBreedingAssistantMod/Generated/FishBreedingLookup.g.cs` so the open-source package can build FishBreeding without local-only generated source.
- Synchronized latest audit entry docs and kept the existing Camera/Fishing/Save/Input focused hook maps, reverse snippets, smoke scripts, manual QA record, and CAMERA-PLAYABLE report zip.

## Changed Files

- `AGENTS.md`
- `docs/updates/2026/20260608-0028-public-source-hygiene.md`
- `docs/updates/2026/20260608-0029-open-source-audit-package-hygiene-refresh.md`
- `docs/updates/INDEX.md`
- External package artifact: `E:\Python_project\DTMAPI-open-source-Refactor`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`

## Generated Artifacts

- Clean source package: `E:\Python_project\DTMAPI-open-source-Refactor`
  - File count: 234
  - Size: 2,500,447 bytes
- Audit package folder: `E:\Python_project\DTMAPI-audit-package-Refactor`
  - File count: 302
  - Size: 21,426,099 bytes
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`
  - Size: 4,915,794 bytes
  - Entry count: 310

## Validation

- Passed: repository `tools/scripts/build.ps1 -Configuration Release`.
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: repository `tools/scripts/test.ps1 -Configuration Release`.
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: clean source package build using the repository-local .NET SDK on this command's `PATH`.
  - `E:\Python_project\DTMAPI-open-source-Refactor\tools\scripts\build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: package build output cleanup after validation.
- Passed: package exclusion audit on both folders.
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, and `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.rar`, `.7z`, `.nupkg`, and `.snupkg`.
  - Unexpected zip files found: 0; the audit report zip is the only allowed nested zip.
- Passed: required-file audit for root `LICENSE`, `NOTICE.md`, `README.md`, sanitized `AGENTS.md`, `DTMAPI.sln`, FishBreeding generated lookup, latest audit docs, focused hook maps, smoke scripts, and report zip.
- Passed: open-source package local-path audit found no `E:\Python_project`, `D:\Steam`, or `D:\steam`.
- Passed: audit package source-root local-path audit found no `E:\Python_project`, `D:\Steam`, or `D:\steam`; evidence-only audit docs still preserve historical game evidence paths.
- Passed: open-source package `testmods`, `tools`, and `DTMAPI.sln` have no dependency on `references/doloc-town/own-mod-sources` or `own-mod-sources/FishBreedingAssistantMod`.
- Passed: audit package zip entry audit found 0 missing required entries and 0 bad `.git`, `.tools`, `bin`, `obj`, binary, or unexpected zip entries.

## Evidence

- `FishBreedingAssistantMod` appears in both package `DTMAPI.sln` files and both package `tools/scripts/build.ps1` files.
- Both package roots include `LICENSE`, `NOTICE.md`, sanitized `AGENTS.md`, and the public root `README.md`.
- The audit package zip includes `audit/docs/updates/2026/20260608-0028-public-source-hygiene.md` and the CAMERA-PLAYABLE report zip.

## Related Records

- Public package creation: `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- Previous package refresh: `docs/updates/2026/20260608-0027-open-source-audit-package-refresh.md`
- Source hygiene fix: `docs/updates/2026/20260608-0028-public-source-hygiene.md`

## Rollback Notes

- Delete and regenerate `E:\Python_project\DTMAPI-open-source-Refactor`, `E:\Python_project\DTMAPI-audit-package-Refactor`, and `E:\Python_project\DTMAPI-audit-package-Refactor.zip` from the previous package recipe if this refresh is discarded.
- If the public FishBreeding placeholder is replaced, rerun the clean package build and exclusion audit before publishing the package.

## Follow-Up

- If a standalone source zip is needed, generate it from `E:\Python_project\DTMAPI-open-source-Refactor` after one more exclusion audit so validation-created build outputs are not included.
