# 20260608-0022 Public Build FishBreeding Exclusion

## Metadata

- Update ID: 20260608-0022
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Fix the DTMAPI-open-source-Refactor public build blocker."
- Owner: Codex

## Summary

- Confirmed `FishBreedingAssistantMod.csproj` depended on `references/doloc-town/own-mod-sources/FishBreedingAssistantMod/src/FishBreedingLookup.g.cs`.
- Confirmed `references/README.md` marks `doloc-town/own-mod-sources` as local-only and says it must not be published or required to build DTMAPI.
- Chose fallback plan B: temporarily exclude `FishBreedingAssistantMod` from the public build until its generated lookup can be regenerated or approved for public source placement.
- Removed `FishBreedingAssistantMod` from root `DTMAPI.sln`.
- Removed `FishBreedingAssistantMod` from `tools/scripts/build.ps1`.
- Added README notices that `FishBreedingAssistantMod` is not currently included in the public build.
- Synchronized the same public-build files into `E:\Python_project\DTMAPI-open-source-Refactor`.

## Changed Files

- `DTMAPI.sln`
- `tools/scripts/build.ps1`
- `testmods/README.md`
- `testmods/FishBreedingAssistantMod/README.md`
- `docs/updates/2026/20260608-0022-public-build-fishbreeding-exclusion.md`
- `docs/updates/INDEX.md`
- External package artifact: `E:\Python_project\DTMAPI-open-source-Refactor`

## Validation

- Passed: `tools/scripts/build.ps1`
- Passed: `E:\Python_project\DTMAPI-open-source-Refactor\tools\scripts\build.ps1`
  - The public package build used the repository-local .NET SDK on `PATH` so the source package did not need to keep a `.tools` SDK copy.
- Passed: root solution/build-script membership check:
  - `DTMAPI.sln` no longer contains `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj`.
  - `tools/scripts/build.ps1` no longer lists `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj`.
- Passed: public package membership check:
  - `E:\Python_project\DTMAPI-open-source-Refactor\DTMAPI.sln` no longer contains `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj`.
  - `E:\Python_project\DTMAPI-open-source-Refactor\tools\scripts\build.ps1` no longer lists `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj`.
- Passed: public package cleanliness check after build output cleanup:
  - File count: 222
  - Size: 2,440,599 bytes
  - Bad directories found: 0 for `.git`, `.tools`, `bin`, `obj`, `decompiled`, `input`.
  - Bad files found: 0 for `.dll`, `.exe`, `.pdb`, `.zip`, `.rar`, `.7z`, `.nupkg`, `.snupkg`, or `Assembly-CSharp.dll`.

## Evidence

- Root and public package builds completed with 0 warnings and 0 errors.
- The generated lookup source remains local-only at `references/doloc-town/own-mod-sources/FishBreedingAssistantMod/src/FishBreedingLookup.g.cs`; it was not copied into the public package.
- Public package build outputs were removed after validation to restore the clean source-package state.

## Related Records

- Public package creation: `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- Reference boundary: `references/README.md`
- Fish roe smoke matrix row: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-FISHROE-001`

## Rollback Notes

- If a public generated lookup source is approved or regenerated, move it to `testmods/FishBreedingAssistantMod/Generated/FishBreedingLookup.g.cs`.
- Update `testmods/FishBreedingAssistantMod/FishBreedingAssistantMod.csproj` to compile the in-tree generated source.
- Re-add `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj` to `DTMAPI.sln`.
- Re-add `testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj` to `tools/scripts/build.ps1`.
- Remove the temporary exclusion note from `testmods/README.md`.

## Follow-Up

- Decide whether the fish breeding lookup should be regenerated from public-friendly source data or replaced by a runtime data query before restoring the mod to the public build.
