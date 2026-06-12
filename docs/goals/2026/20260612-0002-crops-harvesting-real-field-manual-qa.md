# Crops Harvesting Real-Field Manual QA Handoff

Status: handoff-ready
Created: 2026-06-12
Target branch: `codex/crops-harvesting-manual-qa-handoff`
Target merge branch: future mainline / Refactor successor
Target version: no version bump

## Source Request

The user asked to follow the latest audit in the medium/long-term direction and
provide a directly hand-testable setup, including a temporary mod if useful.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/goals/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Scope

Prepare the medium/long-term manual validation path for
`ICropHarvestingApi` without expanding gameplay scope:

- add a temporary developer-only QA mod that consumes only `ICropHarvestingApi`;
- expose scan / harvest-one / harvest-batch controls through hotkeys and DTMAPI
  Settings;
- document real-field manual QA cases for ordinary crops, vine, mushroom bag,
  bush, tree-basin crop, grass/forage, full inventory, duplicate prevention, and
  title reload;
- keep `TreeBasinCrop` scan-only/unsupported until a separate native-owner
  review proves its execution owner.

## Constraints

- Do not copy old DLKsmapi / DolocSMAPI AutoHarvest code.
- Do not reference `Assembly-CSharp`, Harmony, raw `PlantBasin`, raw `Crop`, or
  raw `TreeCrop` from the QA mod.
- Do not implement cocoa/tree-basin harvesting, wild grass, wild tree, or forage
  harvesting in this handoff.
- Do not change the public `DTMAPI.Abstractions` contract in this handoff.
- Do not add the QA mod to the published Workshop mod definition list.
- Ordinary DTMAPI mods must not be placed under `BepInEx/plugins`.

## Acceptance Criteria

- `testmods/CropHarvestingQaMod` builds and is listed only in developer-local
  official mod definitions.
- The QA mod logs result counts, target kind counts, target status counts, and
  optional per-target rows.
- The manual QA checklist records all required user-visible cases and keeps them
  pending user confirmation.
- API matrix / hook map / smoke matrix / update records identify the QA fixture
  and state that automated smoke still only proves the transient ordinary
  `PlantBasin` path.

## Required Validation

- `git diff --check`
- `tools/scripts/build.ps1 -Configuration Release`
- `tools/scripts/test.ps1 -Configuration Release`
- PowerShell AST parse for modified scripts.
- `tools/scripts/install-to-game.ps1 -DryRun -InstallAllDevOfficialMods`
- If safe in the local workspace, normal install with
  `tools/scripts/install-to-game.ps1 -InstallAllDevOfficialMods` so the QA mod
  is ready for hand testing.

## Hand-Test Entry

After normal install, enable `DTMAPI 作物收获手测夹具` in the game's mod list and
use:

- `F8` for scan-only;
- `F9` for harvest-one;
- `F10` for harvest-batch;
- title DTMAPI Settings -> `Crop Harvesting QA` for the same controls.

Record results in
`docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`.

## Completion Notes

Final response should include:

- branch and commit;
- how to hand test immediately;
- which files define the QA mod and checklist;
- validation/install results;
- reminder that `TreeBasinCrop` remains unsupported/not executed and this API is
  still Experimental.
