# 20260603-0011 readme 0.2.4 regression and new mod goal

## Summary

Updated `readme.md` from the completed 0.2.3 manual-QA ledger into the next implementation ledger for DTMAPI 0.2.4. The new ledger records the user's latest manual QA regressions and folds in the previously researched Oil, Mine, and More Equipment Slots mod concepts.

## Source Request

The user asked whether `readme.md` should be updated after the generated short goal/prompt. The answer was yes because the previous `readme.md` still treated 0.2.3 smoke evidence as current verified truth, while the latest user feedback reports new manual QA regressions.

## Changed Files

- `readme.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260603-0011-readme-024-regression-new-mod-goal.md`

## Content Added

- New manual feedback header for the 0.2.4 follow-up round.
- Per-issue review blocks for:
  - AnimalHusbandryProgress hidden-product UI and custom color input.
  - AutoFishing secondary UI, mini-game behavior, and fishing animation speed.
  - Y console instant-save usability and teleport CSV audit.
  - SecondMotor texture, map transition, dual-motor state, and summon animation.
- New task ledger A-I:
  - baseline and version bump;
  - animal UI/config;
  - AutoFishing/ActionSpeed polish;
  - Y console instant save and teleport CSV;
  - robust independent SecondMotor;
  - Oil official-content mod;
  - Mine official shell and DTMAPI machine behavior;
  - More Equipment Slots;
  - verification and documentation closeout.

## Validation

No build or game smoke was run. This was a documentation and goal-ledger update only.

## Rollback Notes

Restore the previous `readme.md` content if the next implementation round should go back to the 0.2.3 completed ledger instead of the new 0.2.4 regression/new-mod ledger.

## Follow-Up

The next implementation Codex should use the short `/goal` prompt and read `readme.md` for all detailed task requirements.
