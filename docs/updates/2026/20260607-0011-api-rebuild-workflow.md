# 20260607-0011 - API Rebuild Workflow

Date: 2026-06-07
Status: implemented
Scope: docs/workflows/goals/reviews/agents

## Source Request

User asked to check whether the main workflows need updates, update workflow-related files, and then provide an API rebuild prompt. This follows the 0008/0009/0010 native-owner API audits, which showed that the current public API surface has been reviewed but several high-risk APIs require targeted rebuilds.

## Changed Files

- Added `docs/workflows/codex-api-rebuild.md`.
- Updated `AGENTS.md` to require the API rebuild workflow and latest API reviews before native-owner/GameBridge redesign work.
- Updated `docs/workflows/codex-feedback-to-goal.md` so API rebuild handoffs switch to the API rebuild workflow.
- Updated `docs/goals/README.md` with API rebuild handoff requirements.
- Updated `docs/reviews/README.md` with API native-owner review requirements.
- Added this update record and linked it from `docs/updates/INDEX.md`.

## Summary

- Formalized the API rebuild route:
  native responsibility function / state holder -> GameBridge adapter -> public abstraction -> Core/mod usage -> third-save validation -> docs/API matrix.
- Clarified that 0008/0009/0010 cover existing DTMAPI APIs only, not every possible future API and not every decompiled method body.
- Added consistent API status language: `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, and `blocked-rebuild`.
- Required API rebuild goals to be narrow and to begin with native-owner method-body review before runtime edits.
- Prevented future goals from treating UI success, registry success, debug-console success, or smoke-helper success as native-owner proof.

## Validation

- Documentation-only workflow update.
- Ran `git diff --check`; no whitespace errors were reported. Git emitted the existing line-ending warning that `docs/updates/INDEX.md` will be converted from LF to CRLF when Git next touches it.
- No build or game smoke was run because no runtime/API/mod/game files were changed.

## Evidence Links

- Final current-API risk table: `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`.
- Prior high-risk API audits:
  - `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`
  - `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`
  - `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`

## Rollback

Remove `docs/workflows/codex-api-rebuild.md`, revert the related workflow/goal/review/agent text additions, remove this update record, and remove the `20260607-0011` row from `docs/updates/INDEX.md`.

## Follow-up

- The next implementation handoff should use a dedicated `docs/goals/YYYY/...md` file and sibling `.goal.txt`.
- Recommended first API rebuild target is CameraZoom/background/fog/room rendering because it has high visible value and a contained native-owner boundary.
