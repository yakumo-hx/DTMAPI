# 20260605-0001 0.2.6 Mine and Y-console manual QA goal

## Summary

Updated `readme.md` into a focused 0.2.6 manual QA task ledger for MineMod and the Y key console only. The new ledger records the user's latest manual findings: Mine scaling pollutes unrelated equipment, Mine placement preview is not scaled, Mine research unlock appears twice with the wrong point cost, Y-console reload causes room sprite contamination, Y-console weather/localization/hover behavior is incomplete, and the search box can restore stale `石油` after a full restart.

## Source Request

The user asked to output a goal after reviewing two focused mod/problem areas. The user explicitly noted that broad multi-mod goals are limited even in goal mode, so this task ledger intentionally avoids unrelated mod work.

## Changed Files

- `readme.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260605-0001-026-mine-yconsole-manual-qa-goal.md`

## Content Added

- 2026-06-05 manual feedback header.
- Per-issue review blocks for:
  - MineMod scale contamination, placement preview scale, duplicate research unlock, and research cost.
  - Y-console reload removal, scene residue, compact weather row, localization, hover, and search lifecycle.
- New focused tasks A-G:
  - baseline and version bump;
  - Mine scale containment and preview scale;
  - Mine official research duplicate/cost;
  - Y-console reload removal and scene-residue prevention;
  - Y-console localization and compact weather row;
  - Y-console hover and search reset;
  - verification and documentation closeout.

## Validation

No build or game smoke was run. This was a documentation and prompt-ledger update only.

## Rollback Notes

Restore the previous `readme.md` if the project should return to the 0.2.5 completed-evidence ledger. The new ledger intentionally treats the user's 2026-06-05 manual QA as newer than 0.2.5 smoke evidence.

## Follow-Up

The next implementation Codex should use the short `/goal` prompt pointing to `readme.md`, keep the work limited to MineMod and Y console, and must not mark complete without player-visible third-save and restart evidence.
