# 20260612-0010 - Refactor Branch-Wide Code Audit

Status: verified
Date: 2026-06-12
Branch: `Refactor`
Source request: user requested a full current-branch code-level audit, update/debug records, code adjustment, current audit report, and next plan before bottom-layer refactor.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `docs/reviews/README.md`
- `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0010-refactor-branch-wide-code-audit.md`

## Summary

- Performed a current `Refactor` branch code/docs/debug audit across Manager UI, ConfigMenu title UI, CameraView, FishingAutomation, SaveSlots, Mine/MachineProduction, StrongPlantingGun, AnimalViewer, and release/debug ledgers.
- Removed the unused AnimalViewer single-pass native mood-bar helper that wrote `moodInfo`/`moodProgress`; the active path remains the independent cloned progress-row overlay.
- Added durable code-audit review storage under `docs/reviews/code/YYYY/`.
- Updated API matrix facts for current SaveSlots and CameraView manual evidence while keeping both APIs Experimental.
- Added debug-index and smoke-matrix entries for the current audit.

## Known Facts And Rejected Hypotheses

- Manager/config UI truncation is caused by fixed first-N rendering, not lost manager model data.
- SaveSlots extra-slot save/load now has user evidence, but 18+ layout overflow remains a UI paging problem; it is not evidence of save-file corruption.
- Mine is still hybrid fuel/electric in code and config; pure-electric conversion has not happened.
- CameraView background sync is not implemented by the current orthographic-size-only playable zoom path.
- AnimalViewer first-frame guard has smoke evidence, but repeated manual animal-switch confirmation is still pending.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- No game smoke is claimed for this audit because the only code edit removes an unused private helper and the remaining changes are review/API/debug/update records.

## Evidence

- Review record: `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md`.
- Debug regression row: `BRANCH-WIDE-CODE-AUDIT-20260612`.

## Rollback

- Restore `ApplyAnimalProgressSinglePassData(...)` only if a future AnimalViewer implementation deliberately returns to native `moodInfo`/`moodProgress` rendering. That is not recommended without a fresh visual QA plan because the current active path intentionally avoids the old `心情` first-frame route.
- Revert documentation rows only if newer manual QA contradicts these current branch facts.

## Follow-Up

- Manager/config scroll and paging infrastructure.
- SaveSlots 18+/24-slot paging or scrolling.
- Mine pure-electric runtime/config migration.
- Camera background/fog/panorama native-owner review.
- AnimalViewer repeated-switch manual QA.
