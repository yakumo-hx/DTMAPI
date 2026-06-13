# 20260612-0011 - Bottom-Layer Refactor Goal Prompt

Status: verified
Date: 2026-06-12
Branch: `Refactor`
Source request: user asked to convert the current audit into a prompt and emphasize branch-based work.

## Changed Files

- `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`
- `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.goal.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0011-bottom-layer-refactor-goal-prompt.md`

## Summary

- Created a new implementation handoff goal for the bottom-layer refactor following the 2026-06-12 `Refactor` branch audit.
- Stored the exact short `/goal` prompt beside the detailed goal file.
- Made branch work a hard requirement: the implementation Codex must create or switch to `codex/bottom-layer-refactor-audit-20260612` before code edits and must not implement directly on `Refactor`.
- Fixed the implementation target version as `0.5.0-alpha` / `0.5.0.0` to `0.5.1-alpha` / `0.5.1.0`.

## Validation

- `git diff --check` passed with line-ending warnings only.
- No build/test/game smoke was run for this prompt-only documentation update.

## Evidence

- Detailed goal: `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`.
- Exact prompt backup: `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.goal.txt`.

## Rollback

- Remove the goal and `.goal.txt` only if the user decides not to run this bottom-layer refactor handoff.
- Keep the prior audit record `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md` unchanged; it remains the source review.

## Follow-Up

- Start a new implementation Codex with the short `/goal` text from the sibling `.goal.txt`.
