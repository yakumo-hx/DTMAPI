# 20260612-0004 - Crops Harvesting Manual QA Handoff

Status: verified
Date: 2026-06-12
Branch: `codex/crops-harvesting-manual-qa-handoff`
Source request: follow the latest Crops/Harvesting audit in the medium/long-term direction and provide a directly hand-testable setup, including a temporary mod if useful.

## Changed Files

- `DTMAPI.sln`
- `tools/scripts/build.ps1`
- `tools/scripts/release-common.ps1`
- `testmods/README.md`
- `testmods/CropHarvestingQaMod/CropHarvestingQaMod.csproj`
- `testmods/CropHarvestingQaMod/ModEntry.cs`
- `testmods/CropHarvestingQaMod/manifest.json`
- `testmods/CropHarvestingQaMod/official-info.json`
- `testmods/CropHarvestingQaMod/README.md`
- `testmods/CropHarvestingQaMod/i18n/english.json`
- `testmods/CropHarvestingQaMod/i18n/schinese.json`
- `docs/goals/2026/20260612-0002-crops-harvesting-real-field-manual-qa.md`
- `docs/goals/2026/20260612-0002-crops-harvesting-real-field-manual-qa.goal.txt`
- `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Summary

- Added developer-only `CropHarvestingQaMod` as a manual QA fixture for
  `ICropHarvestingApi`.
- The fixture provides F8 scan-only, F9 harvest-one, F10 harvest-batch, and
  equivalent DTMAPI Settings buttons.
- Each operation logs result counts, target-kind counts, target-status counts,
  and sample target rows.
- Added the fixture to the Release build list and developer-local official mod
  definitions, but not to the published Workshop mod definition list.
- Added a durable manual QA checklist and goal handoff for ordinary crops, vine,
  mushroom bag, bush, tree-basin crop, grass/forage, full inventory, no-duplicate
  second run, farm building room, and title reload checks.

## Boundary

This handoff does not expand crop harvesting execution scope:

- no public API changes;
- no raw Doloc Town game type references in the QA mod;
- no Harmony patches in the QA mod;
- no old DLKsmapi / DolocSMAPI AutoHarvest code copied or imitated;
- no cocoa/tree-basin harvest execution;
- no grass, wild tree, or forage harvest execution;
- no Workshop release packaging for the QA fixture.

`TreeBasinCrop` remains scan-only/unsupported until a separate native-owner
review proves its owner.

## Validation

- `git diff --check` passed with line-ending warnings only.
- JSON parse passed for the QA mod manifest, `official-info.json`, and i18n
  files.
- PowerShell AST parse passed for `tools/scripts/build.ps1`,
  `tools/scripts/release-common.ps1`, and `tools/scripts/install-to-game.ps1`.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and
  0 errors; `CropHarvestingQaMod.dll` was built.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and
  0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/install-to-game.ps1 -DryRun -InstallAllDevOfficialMods`
  passed and detected the local Doloc Town install plus existing legacy items.
- Script definition check passed: `CropHarvestingQaMod` is present in
  `Get-DtmApiDeveloperOfficialModDefinitions` and absent from
  `Get-DtmApiPublishedModDefinitions`.
- Normal local developer install passed after commit and installed
  `DTMAPI_CropHarvestingQA`; `install-state.json` records source commit
  `5213dc6c2455`.
- HookProbe startup regression `GAME-SMOKE/20260612-085717` passed after
  installation with `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`,
  `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- The HookProbe evidence logs show `DTMAPI.CropHarvestingQaMod` loaded and
  clearing transient summary at `SaveLoaded` / `ReturnedToTitle`.

## Evidence

- Automated crop smoke baseline remains `GAME-SMOKE/20260612-072544`.
- Setup/startup regression evidence for the QA fixture:
  `GAME-SMOKE/20260612-085717`.
- Manual QA evidence is pending user confirmation in
  `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`.

## Rollback

Remove `testmods/CropHarvestingQaMod`, its build/solution/developer-local
definition entries, and the manual QA docs if the fixture is no longer wanted.
No public API or gameplay behavior needs rollback.

## Follow-Up

- Run the manual QA checklist on real saved farm scenes.
- If native harvest failures repeat in real fields, add service-level
  CropHarvesting failure throttle before any stability promotion.
- Do a separate `PlantBasinTree` / cocoa tree-basin native-owner review before
  implementing tree-basin harvest execution.
