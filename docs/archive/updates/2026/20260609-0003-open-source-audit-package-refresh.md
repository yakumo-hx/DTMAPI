# 20260609-0003 Open Source Audit Package Refresh

## Metadata

- Update ID: 20260609-0003
- Date: 2026-06-09
- Status: verified
- Source: User request to update the open-source package and audit package from the latest `Refactor` worktree.
- Owner: Codex

## Summary

- Refreshed `E:\Python_project\DTMAPI-open-source-Refactor` from the latest `Refactor` worktree.
- Regenerated `E:\Python_project\DTMAPI-open-source-Refactor.zip`.
- Refreshed `E:\Python_project\DTMAPI-audit-package-Refactor` from the same clean source snapshot.
- Regenerated `E:\Python_project\DTMAPI-audit-package-Refactor.zip`.
- Synchronized the latest API/input scope docs, debug matrix, and update records into the audit package.
- Preserved the existing CAMERA-PLAYABLE full report zip because the latest input-suppress scope update is Core-only and did not run a new game smoke.

## Changed Files

- `docs/updates/2026/20260609-0003-open-source-audit-package-refresh.md`
- `docs/updates/INDEX.md`
- External package artifact: `E:\Python_project\DTMAPI-open-source-Refactor`
- External package artifact: `E:\Python_project\DTMAPI-open-source-Refactor.zip`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`

## Validation

- Passed: clean source package build before final export:
  - `E:\Python_project\DTMAPI-open-source-Refactor.new\tools\scripts\build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: package build output cleanup after validation.
- Passed: package exclusion audit on clean source and audit package folders:
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, and `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.rar`, `.7z`, `.nupkg`, and `.snupkg`.
  - Unexpected zip files found: 0; the audit report zip is the only allowed nested zip.
- Passed: required-file audit for root `LICENSE`, `NOTICE.md`, `README.md`, sanitized `AGENTS.md`, `Directory.Build.props`, `DTMAPI.sln`, FishBreeding generated lookup, latest API docs, focused hook maps, smoke scripts, update records, and report zip.
- Passed: open-source package local-path audit found no `E:\Python_project`, `D:\Steam`, or `D:\steam`.
- Passed: audit package source-root local-path audit found no `E:\Python_project`, `D:\Steam`, or `D:\steam`; evidence-only audit docs still preserve historical game evidence paths.
- Passed: open-source package `testmods`, `tools`, and `DTMAPI.sln` have no dependency on `references/doloc-town/own-mod-sources` or `own-mod-sources/FishBreedingAssistantMod`.
- Passed: source and audit zip entry audits found 0 bad `.git`, `.tools`, `bin`, `obj`, binary, NuGet, or unexpected zip entries.

## Evidence

- The open-source package root includes `Directory.Build.props`, `DTMAPI.sln`, `README.md`, `LICENSE`, `NOTICE.md`, and sanitized `AGENTS.md`.
- The open-source package includes latest `src`, `testmods`, `tests`, `tools`, `assets`, `docs/api`, `docs/architecture`, and sanitized `references` docs only.
- The audit package includes `audit/docs/updates/2026/20260609-0002-input-suppress-one-frame-scope.md` and this refresh record.
- The audit package keeps `audit/report/DTMAPI-CameraPlayable-20260608-222542-report.zip` as the bundled complete report zip.

## Known Facts And Rejected Hypotheses

- Known fact: the first replacement attempt hit a `.tools\dotnet` file lock after the clean source package build. The staged package was regenerated from the repository, `.tools` was shut down and removed before final replacement, and the final required-file audit passed.
- Known fact: the latest runtime/input behavior change is covered by Release build/unit evidence in `20260609-0002`; it does not require a new game report zip.
- Rejected hypothesis: a package that lacks root `Directory.Build.props` is acceptable because it can build with a newer local compiler. The final package must include root build metadata and pass required-file audit.
- Rejected hypothesis: evidence docs with historical local game paths should be removed from the audit package. They remain inside audit/evidence records, while source-root package material is local-path clean.

## Related Records

- Previous package refresh: `docs/updates/2026/20260608-0029-open-source-audit-package-hygiene-refresh.md`
- Latest input scope update: `docs/updates/2026/20260609-0002-input-suppress-one-frame-scope.md`
- Public source hygiene: `docs/updates/2026/20260608-0028-public-source-hygiene.md`

## Rollback Notes

- Delete and regenerate `E:\Python_project\DTMAPI-open-source-Refactor`, `E:\Python_project\DTMAPI-open-source-Refactor.zip`, `E:\Python_project\DTMAPI-audit-package-Refactor`, and `E:\Python_project\DTMAPI-audit-package-Refactor.zip` from the previous package recipe if this refresh is discarded.
- Keep `.tools`, `bin`, and `obj` out of final package artifacts even when build validation is rerun.

## Follow-Up

- Consider turning the refresh recipe into a checked-in packaging script so future package updates do not rely on inline shell orchestration.
