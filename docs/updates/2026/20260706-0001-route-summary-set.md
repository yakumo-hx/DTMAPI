# 20260706-0001 Route Summary Set

## Status

recorded/docs-only

## Source Request

User asked to organize the previously identified follow-up files/routes numbered 1, 3, 4, 5, 7, and 8:

- update ledger
- API matrix and native-owner library
- Hook Map
- manual QA records
- installer/Workshop/script workflows
- SecondMotor/vehicle history

## Changed Files

- `docs/updates/phase-map-20260706.md`
- `docs/reviews/api/2026/20260706-0001-api-native-owner-phase-summary.md`
- `docs/hook-map/phase-summary-20260706.md`
- `docs/reviews/manual-qa/2026/20260706-0001-manual-qa-phase-summary.md`
- `docs/workflows/installer-workshop-phase-summary-20260706.md`
- `docs/reviews/api/2026/20260706-0002-second-motor-retirement-retrospective.md`
- `docs/updates/INDEX.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/hook-map/README.md`
- `docs/reviews/manual-qa/README.md`
- `archive/second-motor-20260615/README.md`
- `docs/updates/2026/20260706-0001-route-summary-set.md`

## Summary

Added a route-oriented documentation set so a future handoff can quickly enter the project through:

- update chronology and stale-evidence rules;
- public API/native-owner status and promotion blockers;
- hook-backed feature risk map;
- recurring manual QA issue families;
- installer/Workshop validation lanes;
- the retired SecondMotor vehicle path.

This is a documentation synthesis only. It changes no runtime code, scripts, package payloads, hooks, or public APIs.

## Validation

- Source documents were reviewed before writing the summaries:
  - `docs/updates/INDEX.md`
  - `docs/api/public-api-matrix.md`
  - `docs/reviews/api/native-owner-domains/INDEX.md`
  - `docs/hook-map/README.md`
  - `docs/reviews/manual-qa/README.md`
  - `tools/scripts/README.md`
  - `docs/workflows/workshop-package-subscription-test-matrix.md`
  - `docs/workflows/codex-runtime-lock.md`
  - `archive/second-motor-20260615/README.md`
- No build or game smoke was run because this update is docs-only and does not change source, scripts, packaging, or runtime behavior.

## Evidence Links

- Update route map: `docs/updates/phase-map-20260706.md`
- API/native-owner summary: `docs/reviews/api/2026/20260706-0001-api-native-owner-phase-summary.md`
- Hook summary: `docs/hook-map/phase-summary-20260706.md`
- Manual QA summary: `docs/reviews/manual-qa/2026/20260706-0001-manual-qa-phase-summary.md`
- Installer/Workshop summary: `docs/workflows/installer-workshop-phase-summary-20260706.md`
- SecondMotor retrospective: `docs/reviews/api/2026/20260706-0002-second-motor-retirement-retrospective.md`

## Rollback

Remove the new summary files, remove their links from the four touched index/README files, remove this update record, and remove the `20260706-0001` row from `docs/updates/INDEX.md`.

## Follow-up

If any of these routes becomes active implementation work, create a dedicated goal file under `docs/goals/YYYY/` plus a sibling `.goal.txt` prompt, then link the relevant summary from that goal.
