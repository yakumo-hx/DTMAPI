# 20260705-0003 Functional Mod Boundary Review

Date: 2026-07-05
Status: recorded / docs-only
Area: docs/api/mod-boundary/smapi-reference

## Trigger

The DTMAPI workspace needed a focused review of how functional mods relate to DTMAPI itself, using SMAPI only as a reference for function boundaries and user experience. Requested focus areas were Y-key console, AutoFishing, ActionSpeed, OneActionComplete, and MoreEquipmentSlots / decoration-equipment bar extension.

## Changes

- Added `docs/reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md`.
- Recorded a docs-only boundary judgment that these functional mods are first-party/product consumers and evidence sources, not automatic proof of stable gameplay APIs.
- Compared the reviewed DTMAPI surfaces against SMAPI's helper/command/config-menu boundaries.
- Captured feature-by-feature recommendations for Diagnostic vs Experimental status, ownership, promotion gates, and future API design.

## Validation

- Documentation-only change.
- Checked the review against the required project, debug, API matrix, native-owner, hook-map, local-mod, and SMAPI reference files listed in the review.
- No game smoke or solution build was run.

## Follow-Up

- Keep Y-key console documentation separate from any future command-helper author API.
- Design owner arbitration for fishing automation before considering promotion.
- Split ActionSpeed evidence and documentation by native subdomain.
- Keep OneActionComplete narrow around resource/fuel/feed domains.
- Finish hot-disable/recovery/visual QA for equipment slot expansion before any stability promotion.
