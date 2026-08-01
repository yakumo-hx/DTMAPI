# 20260711-0006 — Retire Goal prompt workflow

## Metadata

- Update ID: `20260711-0006`
- Date: 2026-07-11
- Status: docs-verified / workflow-only / no-runtime-change
- Source: user requested removal of `docs/goals` requirements while retaining every existing Goal and `.goal.txt` file.

## Summary

- Retired automatic creation of `docs/goals/YYYY/...` task ledgers and sibling `.goal.txt` prompt backups.
- Kept all historical Goal files in place and marked `docs/goals/README.md` as a retired workflow.
- Made an in-progress `docs/updates/YYYY/...` record the durable implementation boundary for long-running work.
- Kept durable pre-implementation reasoning in `docs/reviews` and runtime issue/evidence truth in `docs/debug`.
- Updated manual-QA, API rebuild, native-owner, third-party review, community feedback, onboarding, and architecture rules so they no longer require Goal files.

## User-Visible Impact

- Future Codex work no longer generates a Goal `.md` plus short `.goal.txt` pair by default.
- Historical files remain readable and linked, but they are not active instructions and must not restart old work.
- Normal implementation uses one update record from `proposed` or `in-progress` through its final validation state.

## Changed Files

- `AGENTS.md`
- `docs/goals/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/workflows/player-feedback-community-loop.md`
- `docs/reviews/README.md`
- `docs/reviews/manual-qa/README.md`
- `docs/reviews/templates/manual-qa-review.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/native-owner-domains/review-rounds/INDEX.md`
- `docs/reviews/api/third-party-mods/INDEX.md`
- `docs/onboarding/current-state.md`
- `docs/architecture/20260608-runtime-hardening-branch-roadmap.md`
- `docs/architecture/20260611-product-roadmap-community-loop.md`
- `docs/updates/INDEX.md`

## Validation

- Confirmed no file under `docs/goals/2026` was deleted, renamed, moved, or edited.
- Scanned active rules, workflows, templates, onboarding, and current indexes for remaining requirements to create Goal files or `.goal.txt` backups.
- Checked Markdown links and `git diff --check` after the final edits.
- No runtime code changed. Build, unit tests, runtime lock, install, and game smoke were not required or run.

## Evidence And Related Records

- Historical workflow origin: `docs/updates/2026/20260606-0012-goal-files-root-ledger-removed.md`.
- Prior maintainability audit: `docs/reviews/code/2026/20260615-0001-codex-handoff-space-maintainability-audit.md`.
- Current implementation record policy: `AGENTS.md` and `docs/workflows/codex-feedback-to-goal.md`.

## Rollback Notes

- Revert this documentation update to restore mandatory Goal/`.goal.txt` generation.
- Historical Goal files require no restoration because this update does not remove or move them.

## Follow-Up

- Refresh `docs/onboarding/current-state.md` in a separate scoped change; this update removes its retired Goal reference but does not claim to reconstruct current product truth.
- Consider compacting large root indexes and adding automated duplicate-ID/link/status checks.
