# 20260708-0003 - Lightning Chicken Research Import

Status: docs-only / imported-archival-records / superseded-experiment

## Source Request

The user asked to copy the experiment records, research reports, and problem-description files from the independent `E:\Python_project\DTMAPI-animal` worktree into the latest original `E:\Python_project\DTMAPI` branch.

## Changed Files

- `docs/goals/2026/20260617-0002-lightning-chicken-mvp.md`
- `docs/goals/2026/20260617-0002-lightning-chicken-mvp.goal.txt`
- `docs/reviews/api/2026/20260617-0004-custom-animal-json-ai-animator-research.md`
- `docs/reviews/api/2026/20260618-0001-lightning-chicken-native-store-release-blocker.md`
- `docs/updates/2026/20260617-0009-custom-animal-json-ai-animator-research.md`
- `docs/updates/2026/20260617-0010-lightning-chicken-mvp.md`
- `docs/updates/2026/20260618-0004-lightning-chicken-runtime-precheck-imported.md`
- `docs/updates/2026/20260618-0005-lightning-chicken-native-store-release-blocker-imported.md`
- `docs/updates/2026/20260708-0003-lightning-chicken-research-import.md`
- `docs/updates/INDEX.md`

## Summary

Imported the Lightning Chicken MVP goal, the custom-animal JSON/AI/animator research report, the native store/release blocker report, and the related historical update records from the abandoned `DTMAPI-animal` worktree.

The imported records are archival evidence only. They preserve why the early `runtimeOverrideController` / native-store Lightning Chicken route stopped, while the current DTMAPI mainline remains on the later JSON + PNG + WAV content-pack route represented by Hatch, Shell Crab, Oilfloater, Mole, and Drecko.

The two 2026-06-18 imported update records were renumbered to `20260618-0004` and `20260618-0005` because the latest DTMAPI branch already has `20260618-0001` and `20260618-0002` installer records.

## Validation

- Copied docs only; no runtime, source, script, package, or testmod files were imported.
- Did not import the old `DTMAPI-animal` runtime adapter files, `LightningChickenSmokeCase`, release/install script edits, version bumps, or `testmods/LightningChickenMod`.
- Ran no build or game smoke because no runtime/source behavior changed.
- Static validation after the copy:
  - confirmed all imported goal/review/update files exist in `E:\Python_project\DTMAPI`;
  - confirmed `git status --short` shows only documentation additions/updates from this import plus the pre-existing unrelated untracked `docs/reviews/code/2026/20260708-0001-autofishing-longplay-gc-research.md`;
  - `git diff --check` reported only the existing LF/CRLF normalization warning for `docs/updates/INDEX.md`.

## Rollback Notes

Remove the imported Lightning Chicken goal/review/update files listed above and delete this row from `docs/updates/INDEX.md`. No runtime rollback is needed.

## Follow-Up

If Lightning Chicken is revived as a sample, recreate it in the latest DTMAPI branch as a Hatch-style JSON + PNG + WAV content pack. Do not merge the old `DTMAPI-animal` runtime adapter, smoke case, version bump, or developer package wiring.
