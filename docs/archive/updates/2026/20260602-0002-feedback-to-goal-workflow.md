# 20260602-0002 - Codex Feedback-To-Goal Workflow

## Metadata

- Update ID: 20260602-0002
- Date: 2026-06-02
- Status: implemented
- Source: User requested a fixed workflow for turning manual test findings into Codex constraints and short goal prompts, separate from the DTMAPI implementation Codex.
- Owner: Codex

## Summary

- Added a durable workflow document for converting user test feedback into `readme.md` task details and short `/goal` prompts.
- Updated `AGENTS.md` so future Codex sessions read that workflow before organizing prompts or next-goal constraints.

## User-Visible Impact

- Future feedback整理 turns should produce consistent output and avoid mixing prompt-writing work with DTMAPI implementation work.
- The short `/goal` prompt stays compact, while detailed acceptance criteria live in `readme.md`.

## Changed Files

- `AGENTS.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260602-0002-feedback-to-goal-workflow.md`

## Validation

- Documentation-only change.
- Build and game smoke were not run because no runtime, hook, API, script, package, or game behavior changed.
- Manual document check: workflow defines role split, output sections, strong rules, and a short `/goal` template.

## Evidence

- N/A for runtime evidence.

## Related Records

- Debug: `docs/debug/INDEX.md`
- Hook map: N/A
- Smoke matrix: N/A
- API matrix: N/A

## Rollback Notes

- Remove the workflow file and the `AGENTS.md` reference if this process proves too rigid.

## Follow-Up

- If future prompt-writing turns reveal repeated omissions, update `docs/workflows/codex-feedback-to-goal.md` instead of relying on chat memory.

