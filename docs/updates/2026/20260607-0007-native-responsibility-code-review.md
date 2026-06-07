# 20260607-0007 native-responsibility code review

Date: 2026-06-07
Status: implemented
Area: docs/reviews/api

## Summary

Opened the manual code-level native-responsibility review for DTMAPI public APIs and GameBridge/Core implementations. This review continues after the 0005 family-level audit and the 0006 symbol/matrix audit, but does not inherit 0006 conclusions. The new 0007 documents record manually read implementation bodies, GameBridge/Harmony/reflection paths, native owners, ordinary-mod usability, concrete risk scenarios, and refactor backlog for the highest-risk and most-used public API surfaces.

This update is docs-only. It does not change runtime code, public API declarations, migrated mods, game files, Workshop files, or developer docs. It does not create an implementation goal.

## Changed files

- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/02-gameplay-debug-bridges.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/03-framework-content-ui.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/04-matrix-coverage.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/05-abstractions-reverse-coverage.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/06-abstractions-public-member-ledger.md`.
- Added `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/07-completion-audit.md`.
- Added this update record.
- Linked this record from `docs/updates/INDEX.md`.

## Source request / goal

User requested a bottom-layer DTMAPI API review with:

- Code-level review rather than family-only review.
- Bidirectional coverage expectations from `public-api-matrix` and `src/DTMAPI.Abstractions`.
- Symbol-first blocks with declaration/implementation paths.
- Ordinary mod usability decisions.
- Stable/experimental review advice.
- Concrete Gap/Blocked risk explanations.
- DTO semantic-risk review.
- Volume top risks and final decision table.
- Explicit no-implementation-goal boundary.

The active goal further required that 0007 not be marked complete unless the review can answer native-owner credibility, ordinary-mod safety, concrete failure modes, and next rebuild actions for every important API.

## Evidence used

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/reviews/README.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- `src/DTMAPI.Abstractions`
- `src/DTMAPI.Core`
- `src/DTMAPI.GameBridge.DolocTown`
- `src/DTMAPI.BepInExBootstrap`
- `src/DTMAPI.ModConfigMenu`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
- `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`

## Key findings recorded

- 0.4.0 custom entity contracts should be split into stable DTMAPI definition/registry contracts and blocked/experimental native runtime adapters.
- `IInputHelper.Suppress` is currently DTMAPI-only state with no native action suppression consumer.
- `IMachineProductionApi` is a hybrid of native recipe/tech table writes and a DTMAPI runtime production loop, not a full native machine owner.
- `IEquipmentSlotsApi` is a DTMAPI sidecar storage/UI/stat bridge, not native extra slots.
- `ICameraZoomApi` currently writes only camera orthographic size and misses the broader camera/background/fog/parallax owner set.
- `IMotorVehicleApi` original-motor helpers reach native owners, but second motor remains DTMAPI clone/routing logic.
- Debug/Y-console APIs frequently reach native owners, but must remain `debug-only` because they mutate economy, mail, weather, teleport, save, time, movement, creative flags, crops, and room objects.
- Framework helpers are mostly ordinary-mod safe when documented as DTMAPI-owned, read-only, or UI-only; `Input.Suppress` and content/workshop wording need special caution.
- The positive coverage appendix maps all 82 `public-api-matrix` rows to a 0007 review target.
- The reverse coverage appendix identifies major Abstractions MatrixGaps, including manifest/mod base/status metadata, helper shells, provider interfaces, DTO/event args, and high-risk custom entity / migrated bridge fields.
- The public-member ledger records current Abstractions public inventory counts: 234 public type/interface/enum/struct declarations, 1013 public property lines, and 6 public concrete/abstract method lines, then assigns file/type families to coverage classes and flags the high-risk field symbols that require developer-doc warnings.
- The completion audit records that a literal 1013-row low-risk property table would be generated inventory rather than additional manual native-owner review, and concludes the active 0007 review goal is complete.

## Validation

- Planned/current docs-only validation: `git diff --check`.
- Not run in this record yet: game smoke. Reason: docs-only review; no runtime/hook behavior changed.

## Rollback

Delete the 0007 review index, 0007 review directory, this update record, and the single `20260607-0007` row in `docs/updates/INDEX.md`.

## Follow-up

- Done in this update: added the 82-row `public-api-matrix` coverage appendix.
- Done in this update: added a public-member inventory and coverage-class ledger for every Abstractions source file.
- Done in this update: completed the final requirement audit and chose not to generate a literal 1013-row low-risk property table.
- Do not create an implementation goal until the user explicitly requests one.
