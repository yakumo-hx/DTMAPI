# 20260611-0018 Manual QA Batch 1 Fishing, StrongPlantingGun, AnimalViewer

Date: 2026-06-11

Status: verified

Area: manual-qa/gamebridge/ui

## Summary

Implemented the low-risk Manual QA Batch 1 fixes for three focused areas: Fishing/AutoFishing option semantics, StrongPlantingGun slot/range/disabled-state semantics, and AnimalViewer hidden-produce first-frame flicker guard. This branch does not address Camera background synchronization, SaveSlots 18+ paging, Manager full-list paging, F9 fishing info, Equipment, MotorVehicle, MachineProduction, Workshop/installer, Content Pipeline, or API stability promotion.

## Source Request

User supplied a `/goal Manual QA Batch 1` request and asked Codex to act on the attached content. The goal selected the low-risk manual QA findings and required build/test plus AutoFishing, StrongPlantingGun, AnimalViewer, and HookProbe smoke validation.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunService.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `testmods/AutoFishingMod/manifest.json`
- `testmods/StrongPlantingGunMod/ModEntry.cs`
- `testmods/StrongPlantingGunMod/i18n/english.json`
- `testmods/StrongPlantingGunMod/i18n/schinese.json`
- `testmods/StrongPlantingGunMod/manifest.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/goals/2026/20260611-0002-manual-qa-batch1-fishing-strongplanting-animal.md`
- `docs/goals/2026/20260611-0002-manual-qa-batch1-fishing-strongplanting-animal.goal.txt`
- `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`
- `docs/reviews/manual-qa/2026/20260611-0002-animalviewer-first-frame-flicker-manual-gate.md`
- `docs/updates/INDEX.md`

## Details

- Updated AutoFishing config labels, tooltips, i18n text, manifest summary, README, and XML comments so the player-facing contract matches current `FishingAutomationService` behavior.
- Documented the current AutoFishing semantics: selected rod required, `AutoRecast` normalized on, delayed minigame success forcing, instant-bite skip handoff rather than independent minigame skip, and best-effort fast animations that may safely no-op.
- Normalized `StrongPlantingGunOptions.SlotCount` to the verified fixed three-slot seed/film/fertilizer contract inside `StrongPlantingGunService`.
- Removed the unsupported StrongPlantingGun slot-count control from the migrated config page and added visible contract/disabled-state notes.
- Kept old expanded farming-gun capacity from shrinking at runtime to avoid item-loss risk, but limited active use reads to the supported three slots.
- Added AnimalViewer localization-component suppression before cloned progress rows are first activated, kept inactive prefill, refreshed text again in the same callback, and added `Smoke.AnimalViewerFirstFrameFlickerGuard` for same-callback text validation.
- Added an AnimalViewer manual gate because a timing-dependent first-frame visual flicker cannot be fully proven by final-state smoke evidence alone.
- Added unit coverage for StrongPlantingGun slot normalization and AnimalViewer localization-component recognition.

## Validation

- `git diff --check`: passed.
- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe AutoFishing smoke: `GAME-SMOKE/20260611-214536` passed.
- DirectExe StrongPlantingGun smoke: `GAME-SMOKE/20260611-214654` passed.
- DirectExe AnimalViewer smoke: `GAME-SMOKE/20260611-214845` passed.
- DirectExe HookProbe smoke: `GAME-SMOKE/20260611-215014` passed.
- Final `Get-Process DolocTown` found no leftover process.

## Evidence

- `GAME-SMOKE/20260611-214536/result.json` records `RunStatus=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; `latest-report.txt` points to `dtmapi-report-20260611-214620.zip`.
- `GAME-SMOKE/20260611-214654/result.json` records `RunStatus=Passed`, `StrongPlantingGun=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; `latest-report.txt` points to `dtmapi-report-20260611-214731.zip`.
- `GAME-SMOKE/20260611-214845/result.json` records `RunStatus=Passed`, `AnimalViewerUi=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; Animal summary records `localizationDisabled=1` and `firstFrameGuard=sameCallbackTextCheck ... moodTitleHits=0`; `latest-report.txt` points to `dtmapi-report-20260611-214922.zip`.
- `GAME-SMOKE/20260611-215014/result.json` records `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Smoke matrix row: `MANUAL-QA-BATCH1-FISHING-STRONGPLANTING-ANIMAL-20260611`.

## Rollback Notes

Rollback should remove the migrated mod wording changes, restore StrongPlantingGun service/config slot-count behavior to the previous broader DTO interpretation, remove the AnimalViewer first-frame localization guard/status, and remove the new manual gate/update references. Public API members and hook/status IDs were not added, removed, or renamed.

## Follow-Up

- Ask for or capture manual repeated animal-switch confirmation before marking the first-frame flicker fully solved.
- Keep Camera background sync, SaveSlots 18+ paging, Manager full-list paging, and F9 fishing info as separate goals.
