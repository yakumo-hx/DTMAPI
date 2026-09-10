# 20260612-0009 - Crop Harvesting QA Fixture Isolation

Status: verified
Date: 2026-06-12
Branch: `codex/crops-harvesting-manual-qa-handoff`
Source request: user chose the safer path before merging back to `Refactor`: isolate the CropHarvesting QA fixture, remove default hotkey pollution, clean the smoke owner, update docs, validate, then merge.

## Changed Files

- `tools/scripts/release-common.ps1`
- `tools/scripts/install-to-game.ps1`
- `testmods/README.md`
- `testmods/CropHarvestingQaMod/ModEntry.cs`
- `testmods/CropHarvestingQaMod/README.md`
- `testmods/CropHarvestingQaMod/official-info.json`
- `testmods/CropHarvestingQaMod/i18n/english.json`
- `testmods/CropHarvestingQaMod/i18n/schinese.json`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CropHarvestingSmokeCase.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/goals/2026/20260612-0002-crops-harvesting-real-field-manual-qa.md`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`
- `docs/updates/INDEX.md`

## Summary

- Marked `CropHarvestingQaMod` as a `QaFixture` in release definitions.
- Added `install-to-game.ps1 -InstallQaFixtures`.
- Default developer-local installs now skip QA fixtures; `-InstallQaFixtures`
  is required to install `DTMAPI_CropHarvestingQA`.
- `install-state.json` now records `QaFixturesInstalledCount` and
  `QaFixturesInstalled`.
- Changed the QA fixture default scan/harvest hotkeys to `None`; the DTMAPI
  Settings buttons remain the default manual-test route.
- Tightened QA fixture descriptions so tree-basin and grass/forage targets are
  clearly unsupported-boundary checks, not harvest support.
- Changed the crop harvesting smoke manifest owner from `DTMAPI.AutoHarvestMod`
  to `DTMAPI.Smoke.CropHarvesting` so smoke status does not pollute the
  author-facing AutoHarvest sample owner.

## Boundary

This cleanup does not change `ICropHarvestingApi` behavior or any gameplay
harvest logic:

- no public API changes;
- no crop-family expansion;
- no tree-basin/cocoa harvest execution;
- no grass, wild-tree, or forage harvest execution;
- no failure-throttle change;
- no Workshop release packaging for the QA fixture.

## Validation

- `git diff --check` passed with line-ending warnings only.
- PowerShell AST parse passed for:
  - `tools/scripts/install-to-game.ps1`
  - `tools/scripts/release-common.ps1`
  - `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and
  0 errors. A first parallel build/test attempt hit a transient SourceLink
  intermediate-file lock; the build was rerun alone and passed.
- `tools/scripts/test.ps1 -Configuration Release` passed with
  `DTMAPI.UnitTests: OK`.
- `tools/scripts/install-to-game.ps1 -DryRun -InstallAllDevOfficialMods`
  reported `QA fixture install count = 0` and
  `QaFixturesInstalledCount: 0`.
- `tools/scripts/install-to-game.ps1 -DryRun -InstallAllDevOfficialMods -InstallQaFixtures`
  reported `QA fixture install count = 1` and
  `QaFixturesInstalledCount: 1`.
- `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseCropHarvestingApi -SaveSlot 3 -TimeoutSeconds 240`
  passed as `GAME-SMOKE/20260612-101549`.
- Post-smoke local state check found no `MODS\DTMAPI_CropHarvestingQA` folder
  and `D:\steam\steamapps\common\Doloc Town\DTMAPI\install-state.json`
  recorded `QaFixturesInstalledCount=0`.

## Evidence

- Cleanup smoke: `GAME-SMOKE/20260612-101549`.
  - `RunStatus=Passed`
  - `SaveLoaded=Passed`
  - `CropHarvestingApi=Passed`
  - `CropHarvestingApiEvidence=Passed`
  - `ProcessExited=Passed`
  - `NoFatalInstanceWindow=Passed`
  - logs show `owner=DTMAPI.Smoke.CropHarvesting` for scan and harvest.
- Prior crop harvesting automated evidence remains `GAME-SMOKE/20260612-072544`.
- Prior manual partial user verification remains in
  `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`.

## Rollback

- Remove `QaFixture` from the release definition.
- Remove `-InstallQaFixtures` selection logic and install-state fields.
- Restore default QA fixture hotkeys only if isolated manual QA explicitly needs
  keyboard defaults again.
- Restore smoke owner only if a downstream analyzer requires the old
  `DTMAPI.AutoHarvestMod` owner, which is not currently recommended.

## Follow-Up

- If manual QA finds repeated `NativeHarvestFailed` entries, add
  CropHarvesting service-level failure throttling in a separate branch.
- Keep remaining family/boundary manual QA pending until the user confirms the
  specific real-field cases.
