# 20260606-0003 Public Preview Publish And 0.2.9 Review Goal

- Date: 2026-06-06
- Status: implemented
- Area: git/publish/docs/reviews/readme
- Source request: user asked to review file structure, make a git checkpoint, publish `yakumo-hx/DTMAPI.git` with only useful public/download content, include confirmed ActionSpeed and OneActionComplete packages, and convert the current review into the next goal.

## Summary

- Created a local full-workspace checkpoint on `master` before publishing.
- Built Release and ran UnitTests successfully.
- Created a separate public staging repository at `E:\Python_project\DTMAPI-public-publish`.
- Published a curated `main` branch to `https://github.com/yakumo-hx/DTMAPI.git`.
- Updated `readme.md` from the completed 0.2.8 ledger into the next 0.2.9 manual-QA implementation ledger.

## Public Publish Boundary

Included in the GitHub public branch:

- `README.md`
- `PROJECT.md`
- `assets/branding`
- `docs/public-api-matrix.md`
- `docs/confirmed-packages.md`
- `downloads/DTMAPI-0.2.8-public-preview.zip`
- `downloads/DTMAPI-0.2.8/BepInEx/plugins/DTMAPI`
- `downloads/DTMAPI-0.2.8/OfficialLocalMods/Yuuka_DTMAPI_ActionSpeed`
- `downloads/DTMAPI-0.2.8/OfficialLocalMods/Yuuka_DTMAPI_OneActionComplete`

Excluded from the GitHub public branch:

- Full `src/` source tree.
- `references/` reverse data, official docs cache, Stardew SMAPI reference, and third-party samples.
- `docs/debug`, `docs/reviews`, `docs/updates`, and local evidence.
- `tools`, `tests`, private research notes, unfinished experimental mods, and old DLKsmapi material.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed.
- UnitTests: `DTMAPI.UnitTests: OK`.
- Public staging audit:
  - 28 files.
  - About 1.5 MB.
  - No `src`, `references`, `docs/debug`, `docs/reviews`, `testmods`, `tools`, or `tests` path.
- Push result:
  - Remote branch: `origin/main`
  - Public commit: `5fe53af public preview: DTMAPI 0.2.8 runtime and confirmed mods`

## Evidence Links

- Manual QA review source: `docs/reviews/manual-qa/2026/20260606-0002-028-manual-qa-followup-review.md`
- New implementation ledger: `readme.md`
- Public staging directory: `E:\Python_project\DTMAPI-public-publish`
- Remote repository: `https://github.com/yakumo-hx/DTMAPI.git`

## Rollback Notes

- Local full workspace can be restored from the latest `master` checkpoint.
- The public GitHub branch is intentionally independent from the full local `master`; deleting or replacing `origin/main` does not affect the full local source checkpoint.
- If a future public release should include source code, create a separate decision/update record first and explicitly exclude reverse/reference/third-party material.

## Follow-up

- The next implementation Codex should use `readme.md` as the 0.2.9 task ledger.
- Confirmed public packages remain limited to ActionSpeed and OneActionComplete until the user marks more mods safe for public sync.
