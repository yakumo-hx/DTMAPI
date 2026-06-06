# 20260606-0013 - 0.4.0 Stable Custom Entity API Goal

Status: implemented
Date: 2026-06-06
Area: docs/goals/api

## Source Request

The user requested a fresh-Codex handoff for a DTMAPI 0.4.0 upgrade that provides four stable API groups only:

- custom animals;
- custom monsters;
- custom attacks/projectiles/barrage;
- custom drones.

The user explicitly constrained the next round to not write mods and not change existing mods.

## Changed Files

- Added `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.md`.
- Added `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.goal.txt`.
- Added this update record.
- Linked this record from `docs/updates/INDEX.md`.

## Notes

- The root `readme.md` task ledger remains out of workflow and was not used.
- The goal file fixes the target version at `0.4.0`.
- The goal explicitly guards against repeating the previous Codex auto-bump mistake by forbidding automatic `0.4.1` or later bumps.
- The goal requires API/Core/GameBridge/docs/validation only and forbids creating animal, monster, projectile, drone, or other player-facing mods.

## Validation

- Documentation-only handoff update.
- Build and game smoke were not run because no runtime code was changed in this handoff step.

## Evidence

- Goal file: `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.md`.
- Prompt backup: `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.goal.txt`.

## Rollback

Remove the two goal files, remove this update record, and remove the index row if this handoff is superseded before use.

## Follow-Up

The implementation Codex should execute only the short `/goal` prompt stored in the sibling `.goal.txt` file and use the detailed `.md` goal as the single task source.

