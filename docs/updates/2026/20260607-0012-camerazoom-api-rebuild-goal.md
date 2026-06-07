# 20260607-0012 - CameraZoom API Rebuild Goal

Date: 2026-06-07
Status: implemented
Scope: docs/goals/api

## Source Request

After updating the main workflow files, the user asked for an API rebuild prompt. Based on the 0008/0009/0010 audits and the new API rebuild workflow, the handoff starts with a narrow CameraZoom/native-panorama API rebuild instead of a broad all-API implementation.

## Changed Files

- Added `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`.
- Added `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.goal.txt`.
- Added this update record and linked it from `docs/updates/INDEX.md`.

## Summary

- Created a dedicated API rebuild handoff for CameraZoom / panorama rendering.
- The goal requires native-owner method-body review before runtime edits.
- The goal explicitly blocks implementation if the workspace is still on `0.4.0` and the prior `0.4.1` handoff is unresolved.
- The implementation scope is limited to CameraZoom/GameBridge/ZoomMod-as-consumer; MachineProduction, EquipmentSlots, Vehicle, custom entity runtime verbs, and Input.Suppress are excluded.

## Validation

- Documentation-only goal creation.
- Ran `git diff --check`; no whitespace errors were reported. Git emitted existing LF/CRLF warnings for touched Markdown files.
- No build or game smoke was run because no runtime/API/mod/game files were changed.

## Evidence Links

- API rebuild workflow: `docs/workflows/codex-api-rebuild.md`.
- CameraZoom special audit: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md`.
- Final API risk table: `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`.
- Panorama background note: `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`.

## Rollback

Remove the `20260607-0002-042-camerazoom-api-rebuild.*` goal files, remove this update record, and remove the `20260607-0012` row from `docs/updates/INDEX.md`.

## Follow-up

- Run this goal only after the active `0.4.1` state is resolved or explicitly abandoned by the user.
- Future API rebuild goals should remain similarly narrow.
