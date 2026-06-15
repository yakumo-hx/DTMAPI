# 20260615-0013 Core Helper Semantics Round 4

## Status

verified-static

## Area

core/input/content/mod-loading/maintainability

## Source Request / Goal

Second-level cleanup/refactor branch Round 4 semantic fixes.
The target issues were `IInputHelper.Suppress`, enabled-vs-all content helper boundaries, and duplicate `UniqueID` visibility during manifest discovery.

## Changed Files

- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/updates/INDEX.md`

## Summary

- Made `IInputHelper.Suppress` affect DTMAPI helper state and future `RecordInputPressed/Released` dispatch in the current frame.
- Suppressed input now clears helper `pressed/down` state, blocks later same-frame DTMAPI pressed/released events, and still clears on `runtime.Update()`.
- Changed default content item queries to return enabled official content only.
- Added all-content query methods for diagnostics: `GetAllIndexedItems`, `GetAnyIndexedItem`, `GetAllIndexedContentItems`, and `GetAnyIndexedContentItem`.
- Added manifest scanner warnings when duplicate `UniqueID` packages are discovered and one source is chosen over another.

## Validation

- `git diff --check`
  - Passed with line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release`
  - Passed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.

## Evidence

- Static/build evidence only.
- New unit coverage verifies:
  - Pre-suppressed input does not dispatch DTMAPI input events or leave down-state.
  - Suppression is one-frame and normal input resumes after update.
  - Disabled official-local content is hidden from default content queries but available through all-content diagnostics queries.
  - Duplicate `UniqueID` selection is exposed as a scanner warning.
- Validation was run while unrelated/parallel AudioOverride working-tree changes were present in `src/DTMAPI.GameBridge.DolocTown`; they are not part of this update.

## Related Records

- `docs/debug/INDEX.md`
- `docs/updates/2026/20260615-0006-low-risk-workspace-cleanup-round1.md`
- `docs/updates/2026/20260615-0007-motorvehicle-api-retirement-round2.md`

## Rollback Notes

Rollback by restoring the old `InputService.Suppress` marker-only behavior, removing the all-content query additions, and reverting scanner duplicate warnings.
If rolled back, content helpers may again expose disabled official content through default item lookups and duplicate manifest source selection will be silent.

## Follow-Up

- Game smoke is still required before claiming input behavior against native Doloc Town input paths; this change only covers DTMAPI helper/event semantics.
- Public docs should clarify that `Suppress` affects future DTMAPI input helper dispatch in the same frame, not already-dispatching event handlers or Unity/Doloc Town native input consumption.
