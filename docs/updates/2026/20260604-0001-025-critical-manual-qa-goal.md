# 20260604-0001 0.2.5 critical manual QA goal

## Summary

Updated `readme.md` into the DTMAPI 0.2.5 critical manual QA follow-up ledger. The new ledger records the user's latest manual QA findings: SecondMotor failed-summon residue and disabled-mod empty mail, Mine missing official research/internal storage requirements, and Y-console search text leaking across save sessions.

## Source Request

The user asked to follow the feedback-review workflow, audit the new manual QA issues, and convert them into a prompt. Two screenshots were provided: one showing a motor-like sprite residue in the farm scene after alternate motor summon failure, and one showing the official Industrial tech tree target location for the Mine node.

## Changed Files

- `readme.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260604-0001-025-critical-manual-qa-goal.md`

## Content Added

- 2026-06-04 manual feedback header.
- Per-issue review blocks for:
  - SecondMotor summon failure, cross-save ghost sprite, and disabled-mod empty mail.
  - Mine official research route, visible 2x scale, changed recipe, and E-key 16-slot internal storage.
  - Y console search text lifecycle across save entry/exit.
- New tasks A-G for the next implementation Codex:
  - baseline and version bump;
  - SecondMotor containment/cleanup;
  - SecondMotor enablement/mail gating;
  - Mine official research/recipe;
  - Mine visible scale/internal storage;
  - Y console search lifecycle;
  - verification and documentation closeout.

## Validation

No build or game smoke was run. This was a documentation and prompt-ledger update only.

## Rollback Notes

Restore the prior `readme.md` if the project needs to return to the 0.2.4 completed-evidence ledger. The new 0.2.5 ledger intentionally treats the user's 2026-06-04 manual QA as newer than all 0.2.4 smoke evidence.

## Follow-Up

The next implementation Codex should use a short `/goal` prompt pointing to `readme.md` and must not mark complete without third-save, cross-save, disabled-mod, and official research UI evidence.
