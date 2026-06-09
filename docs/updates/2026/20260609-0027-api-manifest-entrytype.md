# 20260609-0027 IManifest EntryType Public Surface

## Metadata

- Update ID: 20260609-0027
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/api-manifest-entrytype`.
- Owner: Codex

## Scope

- Add read-only `EntryType` to `IManifest`.
- Preserve existing `ManifestModel.EntryType` parsing and runtime entry-type selection behavior.
- Mark the new public surface as StableCandidate in the API matrix.

## Changed Files

- `src/DTMAPI.Abstractions/Manifest.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0027-api-manifest-entrytype.md`

## Summary

- Added `string EntryType { get; }` to `IManifest`.
- Added empty-string `EntryType` values to `EmptyManifest` and the internal equipment-slot UI callback manifest.
- Left `ManifestModel.EntryType` unchanged; it already parses, normalizes, and drives runtime entry-type selection.
- Updated the existing EntryType unit test to read the selected value through `IManifest` instead of the concrete manifest model.

## Validation

- Passed: `git diff --check`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
- Passed: `tools/scripts/test.ps1 -Configuration Release`

## Evidence

- Unit test `EntryTypeSelectsEntryAndMissingEntryTypeRejectsAmbiguousDll` now asserts `IManifest.EntryType == selectedEntryType`.
- Build/test completed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.

## Related Records

- `docs/api/public-api-matrix.md` row `IManifest.EntryType`
- Existing runtime EntryType validation in `docs/updates/2026/20260609-0026-gamebridge-native-helper-extraction.md` remains unrelated; this update is the public manifest surface only.

## Rollback Notes

- Remove `IManifest.EntryType`, remove the two empty-string implementation properties, and revert the unit test to concrete `ManifestModel` access.
- Do not remove `ManifestModel.EntryType` or runtime entry-type selection; those predate this public-surface addition.

## Follow-Up

- Merge `codex/api-manifest-entrytype` back to `Refactor`.
- Continue with the diagnostics warning model branch.
