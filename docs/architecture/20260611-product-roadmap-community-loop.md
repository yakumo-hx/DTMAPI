# DTMAPI Product Roadmap And Community Loop

Status: planning record for the Refactor branch.

Date: 2026-06-11

## Purpose

This record turns the current mid/long review into a durable execution route. It does not promote any API to Stable, does not add runtime behavior, and does not replace native-owner review requirements. It defines the next product phases, long-term module refactors, player feedback loop, and recommended execution order.

## Phase 1: 0.5.0-alpha Developer Preview

Goal: make the current rebuild inspectable and supportable for early developer review.

Required scope:

- Keep `0.5.0-alpha` as a development baseline, not a stable release promise.
- Ship a compact public source package and compact web audit package with clear omitted oversized evidence notes.
- Provide a Manager MVP that can show Mods, Errors/Warnings, Hooks, Features, Config, and Export Report from existing internal/runtime surfaces.
- Keep diagnostics snapshot, hook statuses, and feature statuses honest about Experimental and Diagnostic state.
- Include sample mods that demonstrate stable container helpers plus current Experimental APIs without hiding risk.
- Keep CameraView manual QA pending until real human play confirms movement, boundary, transition, reload, flicker, and ZoomMod interaction behavior.
- Require report export evidence for support scenarios through `DiagnosticsReportExport`.

Exit criteria:

- Build/test pass on Refactor.
- Camera, ActionSpeed, AutoFishing, and Manager/HookProbe smoke paths have current third-save evidence.
- Web audit package source commit matches HEAD.
- README and package notes state that the branch is a developer preview.

## Phase 2: 0.6 Manager And Diagnostics

Goal: make troubleshooting practical for players and reviewers without reading raw logs first.

Required scope:

- Implement real Manager UI pages over the internal view model/provider skeleton.
- Show summary counters and support-oriented row ordering in the UI.
- Let Export Report call the runtime diagnostics export path, refresh snapshot, and show path consistency or mismatch.
- Add copyable rows for mod status, hook status, feature status, and selected error/warning details.
- Keep aggregate diagnostics report-only unless an internal UI data source exposes it without expanding public API.
- Add Known Issues links and compatibility guidance where report data can identify a repeated issue.

Exit criteria:

- Manager UI manual check covers long paths, long mod names, long errors, table overflow, and missing paths.
- Export Report generates a report zip and snapshot `LatestReportPath` matches the returned path.
- The UI distinguishes ready host status from Stable public API status.

## Phase 3: 0.7 Workshop And Official Enablement

Goal: make official/Workshop ownership visible and reduce install/support confusion.

Required scope:

- Review native official enablement state before writing or changing any official mod enablement behavior.
- Show official enablement state in Manager only when the owner and data source are confirmed.
- Build a compatibility database from repeated diagnostics reports, not from chat memory.
- Improve installer and package notes so ordinary DTMAPI mods do not go under `BepInEx/plugins`.
- Keep third-party mods as compatibility samples only; do not merge binaries or copied code.

Exit criteria:

- Official enablement rows explain owner, reason, and whether DTMAPI can act.
- Workshop and local package paths are visible in diagnostics/report data.
- Compatibility notes link to reproduced evidence or review records.

## Phase 4: 0.8 Content Pipeline Phase 1

Goal: move beyond hook demos toward a controlled content pipeline.

Required scope:

- Define adapters for item/entity/content metadata that do not expose raw decompiled types.
- Keep content registration separate from runtime creation until native state holders are understood.
- Require native-owner review before any API is marked StableCandidate.
- Add focused smoke cases per content feature and keep the smoke harness from becoming another giant coordinator.

Exit criteria:

- Content API surfaces have ownership notes, smoke evidence, and rollback notes.
- Manager can show content-mod status and dependency/config errors.
- Runtime creation remains blocked where native creation owners are not verified.

## Long-Term Module Refactor Route

Refactors should continue in this order unless a blocking bug changes priority:

1. `DebugFeature`
   - Move remaining debug/runtime tooling ownership out of broad bridge surfaces.
   - Keep diagnostic/runtime helper contracts separated from player-facing stable APIs.
2. MachineProduction native-owner review.
   - Identify native state holders, recipe lookup, machine update loops, save/load ownership, and UI ownership.
3. `MachineProductionFeature` split.
   - Move service, hook bridge, smoke case, and status owner after the review.
4. EquipmentSlots native-owner review.
   - Confirm inventory/equipment storage, serialization, UI rendering, recovery, and disable/rollback behavior.
5. `EquipmentSlotsFeature` split.
   - Preserve existing API semantics and smoke fields; do not stabilize from UI success alone.
6. MotorVehicle native-owner review.
   - Confirm spawn, restore, save/load, map transitions, mail/key ownership, and original vehicle recovery.
7. `MotorVehicleFeature` split.
   - Keep behavior compatible with current smoke evidence and provide manual evidence for dual-vehicle expectations.

## Native-Owner Review Checklist

Each module review should record:

- Native state holder and lifecycle owner.
- Observational hooks vs intervention hooks.
- Save/load/title/scene-transition cleanup.
- UI owner and whether DTMAPI only displays or mutates state.
- Config owner and conflict behavior.
- Smoke evidence required before implementation.
- Manual QA evidence required before StableCandidate discussion.
- Rollback path and failure containment.

## Smoke Architecture Direction

- One feature should own one focused smoke case file where possible.
- `SmokeHarness` should remain an orchestrator, not the home for all feature logic.
- Result fields and log meanings should stay stable unless a migration record explains the change.
- Diagnostics export should be a global smoke concern via `DiagnosticsReportExport`, while feature-specific compatibility fields can remain during transition.

## Community Feedback Loop

The intended player/support loop is:

1. Player opens DTMAPI Manager.
2. Manager shows status summary and failed rows first.
3. Player clicks Export Report.
4. Player uploads the report zip or compact web evidence to an issue/review.
5. Maintainer triages report data into a review record, known issue, compatibility database entry, or in-progress update record.
6. Fixes cite update records and smoke/manual evidence.
7. Known Issues and compatibility notes are refreshed so repeated reports become searchable, not rediscovered.

Manager should help users provide evidence, not ask them to diagnose raw stack traces by hand.

## Recommended Execution Order

The next most useful work after this record:

1. Finish Manager internal provider integration into real MVP pages.
2. Keep global report export evidence on Camera, ActionSpeed, AutoFishing, and later Manager/HookProbe paths.
3. Run CameraView manual QA and close or refresh the pending gate.
4. Refresh public source/web export notes for developer preview packaging.
5. Update README language for `0.5.0-alpha` Developer Preview.
6. Curate sample mods around stable helper container plus Experimental APIs.
7. Harden installer/package layout with ordinary mods outside `BepInEx/plugins`.
8. Start the next high-risk native-owner reviews before feature splits.
9. Move Workshop/content work only after Manager diagnostics can support players who hit install or dependency issues.

## Non-Goals

- No Stable or StableCandidate promotion in this record.
- No public API additions.
- No Manager UI rendering implementation.
- No official enablement writes.
- No runtime behavior change.
