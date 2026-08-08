# DTMAPI Historical Goal Files

Status: retired workflow; historical files retained in place.

This directory stores implementation ledgers and short prompt backups from earlier Codex goal rounds. The project no longer creates new files here as part of the normal workflow.

## Why This Exists

The old mutable root task ledger was removed because it was too unstable for multi-Codex work. Per-goal files replaced it for a period, but the short `/goal` handoff workflow is no longer needed.

All existing files remain available as historical evidence. Do not delete, move, rename, or reinterpret them as current instructions.

## File Layout

```text
docs/goals/
  YYYY/
    YYYYMMDD-NNNN-short-slug.md
    YYYYMMDD-NNNN-short-slug.goal.txt
```

- The `.md` files contain historical task ledgers, review facts, constraints, acceptance gates, blockers, and validation requirements.
- The `.goal.txt` files contain historical short prompts.
- Their presence does not authorize restarting old work.

## Current Rules

- Do not create new goal files or `.goal.txt` files during normal work.
- Do not treat any file in this directory as an active task unless the user explicitly names that historical file for inspection.
- Record new implementation work in `docs/updates/YYYY/...` using `proposed` or `in-progress` while work is underway, then `implemented`, `verified`, `blocked`, or `reverted` as appropriate.
- Use `docs/reviews` for durable pre-implementation analysis and `docs/debug` for runtime issues and evidence.
- DTMAPI `0.3.1` remains a historical Codex version-bump mistake. Do not infer a future target version from old goal files.
- Completed, blocked, or superseded files remain in place as history.

## Historical API Rebuild Handoffs

Earlier API rebuild rounds used goal files. Current API rebuild work follows `docs/workflows/codex-api-rebuild.md` and records implementation state in an update record instead.

The historical files may contain:

- the exact API/domain being rebuilt;
- the prior native-owner review records it relies on;
- the native responsibility functions or state holders already known;
- the native-owner questions that must be answered before code changes;
- the intended API status result: `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, or `blocked-rebuild`;
- the GameBridge boundary and any public API matrix/doc changes required;
- game smoke and lifecycle evidence required before completion.

These fields are historical context, not current execution requirements.

## Migrated Historical Goal

- `docs/goals/2026/20260606-0001-031-regression-new-content.md`
- `docs/goals/2026/20260606-0001-031-regression-new-content.goal.txt`

These files preserve the 0.3.1 handoff for audit only. They are not active implementation prompts because the user later identified the 0.3.1 bump as a Codex error.
