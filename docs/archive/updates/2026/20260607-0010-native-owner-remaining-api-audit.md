# 20260607-0010 - Remaining Public API Native Owner Closure Audit

Date: 2026-06-07
Status: implemented
Scope: docs/reviews/api

## Source Request

User requested completion of the remaining DTMAPI public API native-owner closure review, reusing the 0008/0009 function-level audit mode, without modifying runtime/API/mod/game/Workshop files, without creating an implementation goal, and without running game smoke.

## Changed Files

- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/01-framework-gameloop-event-input.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/02-config-localization-logging-registry.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/03-ui-diagnostics-report.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/04-migrated-gameplay-apis.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/05-debug-yconsole-remaining-apis.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md`.
- Added `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`.
- Added this update record and linked it from `docs/updates/INDEX.md`.

## Summary

- Closed the public-api-matrix areas not already covered by 0008/0009:
  framework/game loop/events/input hotkeys, config/config menu/localization/logging/registry, DTMAPI UI/diagnostics/report, migrated gameplay APIs, Debug/Y-console remaining APIs, Chest Locator Enhancer, and Strong Planting Gun.
- Classified APIs as stable open, experimental open, debug-only, registry-only, DTMAPI-internal/restricted experimental, or downgrade/rebuild.
- Preserved the 0008 conclusions for `IInputHelper.Suppress`, CameraZoom, MachineProduction, and CustomEntity runtime verbs.
- Preserved the 0009 conclusions for Save/Time/Teleport/Inventory/EquipmentSlots/Vehicle/Workshop/Content index.
- Added an integrated final risk table and rebuild direction without creating a follow-up implementation goal.

## Validation

- Confirmed all seven 0010 volumes include the requested audit sections: Files read, Functions read, Call graph, Function body findings, Native owner verdict, Ordinary mod usability, Concrete failure modes, Minimal rebuild direction, and Evidence gaps.
- Confirmed `docs/updates/INDEX.md` links this record.
- Ran `git diff --check -- docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit docs/updates/2026/20260607-0010-native-owner-remaining-api-audit.md docs/updates/INDEX.md`; no whitespace errors were reported. Git emitted the existing line-ending warning that `docs/updates/INDEX.md` will be converted from LF to CRLF when Git next touches it.
- Game smoke was not run because this was a docs-only review.

## Evidence Links

- Public API matrix: `docs/api/public-api-matrix.md`.
- Prior high-risk special audits: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`.
- Prior save/time/teleport/inventory/equipment/vehicle/workshop audits: `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`.
- Hook evidence: `docs/hook-map/README.md`.
- Smoke evidence index: `docs/debug/regressions/smoke-matrix.md`.
- Update evidence read during this pass:
  - `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`
  - `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`
  - `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`
  - `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`
  - `docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md`
- Native-owner reference notes/maps were read by symbol search only, without copying decompiled source:
  - `references/doloc-town/research-notes/README-DolocTown-Modding-API.md`
  - `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
  - `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Action_Interaction.md`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Fishing.md`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/UI.md`

## Rollback

Remove the `20260607-0010-native-owner-remaining-api-audit*` review files/directory, remove this update record, and remove the `20260607-0010` row from `docs/updates/INDEX.md`.

## Follow-up

- No implementation goal was created.
- Future implementation goals should be user-requested and should start from the 0010 risk table, especially the downgrade/rebuild rows for input suppression, camera zoom runtime, machine runtime, equipment slots, vehicle second motor, and custom entity runtime verbs.
