# 20260603-0012 DTMAPI 0.2.4 baseline manual regressions

## Summary

Opened the 0.2.4 implementation baseline for the user's new manual QA regression and new-mod development goal. This update records the four new manual QA regressions as newer facts than the older 0.2.3 smoke evidence and bumps controlled DTMAPI version sources from 0.2.3 to 0.2.4.

## Source Request

The user started a `/goal` requiring a new manual-regression fix and new content-mod development round. They explicitly required a patch version bump, preserving prior worktree changes, and recording the new manual regressions before treating older smoke evidence as current truth.

## Version Change

- Old DTMAPI runtime/build version: `0.2.3`
- New DTMAPI runtime/build version: `0.2.4`
- Updated controlled runtime sources:
  - `Directory.Build.props`
  - `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - `tools/scripts/install-to-game.ps1`
  - current official-local/test package manifests for ActionSpeed, AutoFishing, AnimalHusbandryProgress, Y console, SecondMotor, Oil, Mine, and More Equipment Slots where they now target DTMAPI 0.2.4.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `tools/scripts/install-to-game.ps1`
- `testmods/ActionSpeedMod/manifest.json`
- `testmods/AnimalHusbandryProgressMod/manifest.json`
- `testmods/AutoFishingMod/manifest.json`
- `testmods/DebugConsoleMod/manifest.json`
- `testmods/DebugConsoleMod/official-info.json`
- `testmods/DebugConsoleMod/README.md`
- `testmods/SecondMotorMod/manifest.json`
- `testmods/SecondMotorMod/official-info.json`
- `docs/debug/INDEX.md`
- `docs/debug/issues/README.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260603-0012-024-baseline-manual-regressions.md`

## Manual QA Facts Recorded

- AnimalHusbandryProgress: custom `+` color input missing, hidden-produce row still flashes `心情` while switching animals, and hidden product text is too small.
- AutoFishing/ActionSpeed: inline secondary config rows are misaligned; AutoFishing auto-complete still skips the mini-game when skip is off; cast/reel animation speed is not player-visible enough.
- Y console: instant save anywhere is not discoverable and must move into the Y console; teleport destinations need a CSV audit export.
- SecondMotor: official-example texture route/regression, cross-map riding state, original-motor leakage, stuck-state risk, and missing fly-to-player summon animation remain open manual QA regressions.

## Validation

- Release build/unit passed after the final 0.2.4 code changes on 2026-06-03.
- Third-save targeted smokes passed for animal UI (`GAME-SMOKE/20260603-152851`), Y-console instant save/teleport CSV/new content API load (`GAME-SMOKE/20260603-152451`), ActionSpeed config apply (`GAME-SMOKE/20260603-154207`), ActionSpeed interaction (`GAME-SMOKE/20260603-154721`), AutoFishing (`GAME-SMOKE/20260603-154816`), and SecondMotor (`GAME-SMOKE/20260603-155505`).
- Title UI smoke passed for config screenshots including Animal ordinary/Custom color states (`GAME-SMOKE/20260603-160739`).
- Goal completion is still blocked by missing Mine native fuel/electric player UI evidence and real More Equipment Slots UI/storage/populated recovery evidence. Placed Mine production evidence was added later in `GAME-SMOKE/20260603-164202`.

## Evidence Links

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- Smoke matrix rows: `MANUALQA-024-B`, `MANUALQA-024-C`, `MANUALQA-024-D`, `MANUALQA-024-E`, `NEWCONTENT-024-F`, `NEWCONTENT-024-G`, and `NEWCONTENT-024-H`.
- Follow-up records: `20260603-0013-024-regression-fixes-evidence.md` and `20260603-0014-024-new-content-api-partial.md`.

## Related Records

- Prior historical evidence: 0.2.3 update records `20260603-0003` through `20260603-0010`.
- Current task ledger: `readme.md` and update `20260603-0011`.

## Rollback Notes

To roll back only this baseline, restore the version strings from `0.2.4` to `0.2.3`, remove the new issue and smoke-matrix pending rows, and remove this index entry. Do not delete the user's manual QA facts unless the goal itself is withdrawn.

## Follow-Up

Continue from the blocked items: direct AutoFishing auto-complete-without-skip smoke, Oil coal-drop mining smoke, Mine native fuel/electric player UI smoke, and real More Equipment Slots UI/storage/recovery.
