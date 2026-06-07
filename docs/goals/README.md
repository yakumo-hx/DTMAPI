# DTMAPI Goal Files

This directory stores immutable implementation ledgers for individual Codex goal rounds.

## Why This Exists

The old mutable root task ledger was removed because it was too unstable for multi-Codex work: one Codex could replace it while another Codex was still executing an older goal.

Future implementation handoffs must use a dedicated goal file here instead.

## File Layout

```text
docs/goals/
  YYYY/
    YYYYMMDD-NNNN-short-slug.md
    YYYYMMDD-NNNN-short-slug.goal.txt
```

- The `.md` file contains the detailed task ledger, review facts, constraints, acceptance gates, blockers, and validation requirements.
- The `.goal.txt` file stores the exact short `/goal` prompt given to an implementation Codex.
- Do not create or use a mutable root task-ledger file.

## Rules

- Create a new goal file for every implementation handoff. Do not overwrite another active goal file.
- A `/goal` prompt must reference one exact `docs/goals/YYYY/...md` file.
- If chat history, old active-goal metadata, or a context summary conflicts with the referenced goal file, the referenced goal file wins.
- Use fixed target versions. Write `from X to Y` or `target version Y`; do not write only `bump once`.
- If the workspace is already at the target version, do not bump again unless the user explicitly asks for a newer target.
- DTMAPI `0.3.1` was produced by a Codex version-bump mistake. Do not infer future target versions from it; use only the fixed target version written in the active goal file.
- Store the short `/goal` text beside the goal file as `.goal.txt`.
- Completed, blocked, or superseded goal files remain in place as history. New facts go into a new goal file or a review record.

## API Rebuild Handoffs

API rebuild goals must also follow `docs/workflows/codex-api-rebuild.md`.

Each API rebuild goal file must state:

- the exact API/domain being rebuilt;
- the prior native-owner review records it relies on;
- the native responsibility functions or state holders already known;
- the native-owner questions that must be answered before code changes;
- the intended API status result: `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, or `blocked-rebuild`;
- the GameBridge boundary and any public API matrix/doc changes required;
- game smoke and lifecycle evidence required before completion.

Do not use a broad "fix all APIs" implementation goal. Split high-risk domains into separate handoffs.

## Migrated Historical Goal

- `docs/goals/2026/20260606-0001-031-regression-new-content.md`
- `docs/goals/2026/20260606-0001-031-regression-new-content.goal.txt`

These files preserve the 0.3.1 handoff for audit only. They are not active implementation prompts because the user later identified the 0.3.1 bump as a Codex error.
