# 20260611-0004 Product Roadmap And Community Loop

## Summary

Added durable mid/long planning docs for the 0.5.0-alpha through 0.8 route, long-term module refactors, player feedback/community triage loop, and recommended execution order.

## Source Request

User requested the next-round mid/long implementation focus: code-level issues, phases 1-4, long-term module refactor route, player feedback and community loop, and the next recommended execution order.

## Changed Files

- `docs/architecture/20260611-product-roadmap-community-loop.md`
- `docs/architecture/README.md`
- `docs/workflows/player-feedback-community-loop.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/INDEX.md`

## Details

- Documented phase targets for `0.5.0-alpha` Developer Preview, `0.6` Manager/Diagnostics, `0.7` Workshop/official enablement, and `0.8` Content Pipeline Phase 1.
- Recorded the long-term module route: `DebugFeature`, MachineProduction review/split, EquipmentSlots review/split, and MotorVehicle review/split.
- Added a player/community support workflow from Manager UI status to Export Report, uploaded evidence, triage, Known Issues/Compatibility DB, and follow-up goal creation.
- Updated the Manager MVP design to require support-loop behavior: summary, failed-row priority, fresh report export, and structured handoff evidence.
- No runtime behavior, public API, hook/status ID, or package layout changed in this branch.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`

No game smoke was required; this branch is docs-only.

## Evidence

- Roadmap: `docs/architecture/20260611-product-roadmap-community-loop.md`
- Feedback workflow: `docs/workflows/player-feedback-community-loop.md`
- Manager MVP link note: `docs/design/dtmapi-manager-ui-mvp.md`

## Rollback Notes

Remove the new roadmap/workflow docs and the Manager MVP note. Runtime code and public API are unaffected.
