# DTMAPI Review Records

This directory stores pre-implementation review records. A review record is not an update record and is not proof that a bug is fixed. It is the durable bridge between user manual QA and the next implementation goal.

## Purpose

Use review records when manual QA reveals regressions, ambiguous behavior, lifecycle bugs, UI flicker, state pollution, hook uncertainty, or repeated failures that need code-path understanding before another Codex starts implementation.

The review should answer:

- What did the user actually observe?
- What did the screenshot or log show?
- Which mod, DTMAPI layer, hook, UI lifecycle, config path, or official content path is likely involved?
- Which code paths must be inspected before a fix is attempted?
- What acceptance check would prove the exact user-visible bug is gone?
- What evidence gap would keep the implementation goal incomplete?

## Relationship To Other Docs

- `docs/reviews`: pre-implementation analysis and root-cause/path review.
- `docs/goals/YYYY/...`: immutable implementation task ledgers for future `/goal` prompts.
- `docs/goals/YYYY/....goal.txt`: backups of the exact short `/goal` prompts.
- `docs/debug`: runtime investigation, evidence, regressions, and known bug state.
- `docs/updates`: traceable records of document, runtime, workflow, or project-direction changes.

Do not use a mutable root task-ledger file for review output or implementation handoff. Reviews feed dedicated files under `docs/goals/YYYY/`.

Do not use a review record to claim a fix is complete. Completion belongs to update/debug evidence after implementation and validation.

## File Layout

```text
docs/reviews/
  README.md
  manual-qa/
    YYYY/
      YYYYMMDD-NNNN-short-slug.md
  templates/
    manual-qa-review.md
```

Use `docs/reviews/manual-qa/YYYY/...` for durable user manual-test reviews. The year folder may be created only when the first review for that year is needed.

## When To Create A Durable Review

Create or update a durable review record when:

- the user asks for an audit, review, code-level review, or workflow-backed prompt;
- the same issue has survived a previous claimed fix or smoke pass;
- the issue involves UI lifecycle, input, hotkeys, map transitions, save/load, official/Workshop loading, machines, vehicles, config persistence, or GameBridge hooks;
- the next step is to generate a dedicated goal file or short `/goal` for a fresh Codex.

For pure discussion, a chat-only review is acceptable unless the user asks to update files. If an implementation goal will be produced, prefer a durable review first for complex or repeated issues.

## Minimum Per-Issue Review

Each user-numbered item must keep its own analysis directly under it:

- original feedback;
- screenshot/log transcription;
- user-confirmed facts;
- code/doc facts inspected;
- likely ownership layer;
- root-cause hypotheses;
- rejected or unproven hypotheses;
- acceptance checks;
- blocker conditions;
- downstream docs to update if implementation happens.

Do not move all analysis to the end. This is required so a compacted context or fresh Codex can resume from any single issue.
