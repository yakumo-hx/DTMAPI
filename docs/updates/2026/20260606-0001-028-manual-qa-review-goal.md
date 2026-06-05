# 20260606-0001 0.2.8 Manual QA Review And Goal Ledger

Status: implemented
Area: docs/reviews/readme/goal

## Source Request

The user asked to continue the manual-QA review workflow, perform code-level review, and convert five new findings into the next implementation target:

- MoreEquipmentSlots extra slots are not truly interactive and lack hover preview.
- AnimalHusbandryProgress still renders hidden product incorrectly in the animal bell UI.
- Y-console item grid should use more vertical space and remove the unusable `0.5x` speed.
- MineMod should become electric-only and use conditional recipes depending on OilMod.
- OilMod item id should be formal instead of `dtmapi_oil`.

## Changed Files

- Added review record: `docs/reviews/manual-qa/2026/20260606-0001-equipment-animal-yconsole-mine-oil-review.md`.
- Replaced `readme.md` with the DTMAPI 0.2.8 manual-QA follow-up ledger.
- Linked this update from `docs/updates/INDEX.md`.

## Summary

- Preserved the user's numbered issue order and translated screenshot-only observations into text.
- Attached code-path analysis directly under each issue for context-compaction resilience.
- Identified that MoreEquipmentSlots is currently a read-only UI strip, not real player-slot interaction.
- Identified that AnimalHusbandryProgress still writes hidden-product data into native mood fields, causing the user-visible overlap.
- Identified the Y-console fixed 25-item grid and unusable `0.5x` speed option.
- Identified current Mine/Oil content and runtime references that conflict with the requested electric-only Mine and formal Oil item id.
- Converted the review into a concise 0.2.8 implementation ledger in `readme.md`.

## Validation

- Documentation-only change.
- No build, unit tests, game smoke, or third-save validation were run because this pass did not implement runtime changes.

## Evidence

- Review record: `docs/reviews/manual-qa/2026/20260606-0001-equipment-animal-yconsole-mine-oil-review.md`.
- Implementation ledger: `readme.md`.

## Related Records

- Prior implementation evidence: `docs/updates/2026/20260605-0006-027-manual-qa-root-cause.md`.
- Prior review records:
  - `docs/reviews/manual-qa/2026/20260605-0001-mine-animal-yconsole-code-review.md`
  - `docs/reviews/manual-qa/2026/20260605-0002-mine-time-skip-production-review.md`

## Rollback Notes

Remove this update record and review record, then restore the previous `readme.md` content from the 0.2.7 implementation ledger if the next implementation target should not include these new manual-QA findings.

## Follow-up

The next implementation Codex should start from `readme.md`, bump DTMAPI from 0.2.7 to 0.2.8, and only mark complete after build, third-save game smoke, screenshots/logs, exit cleanup, and documentation updates.
