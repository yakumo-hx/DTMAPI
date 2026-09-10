# 20260705-0004 DTMAPI Mod Boundary Map

Date: 2026-07-05
Status: recorded / docs-only
Area: docs/api/mod-boundary/local-mods

## Trigger

After comparing the local Stardew Valley SMAPI install with its fishing mod, the DTMAPI local mod set needed the same boundary scan: for each DTMAPI-based mod, identify whether the main feature ability is provided by the mod itself or by DTMAPI/GameBridge.

## Changes

- Added `docs/reviews/api/2026/20260705-0002-all-dtmapi-mod-boundary-review.md`.
- Classified active local DTMAPI mods by responsibility boundary.
- Recorded that DTMAPI currently acts as a first-party gameplay feature provider for many shipped/tested mods, not only as a mod manager/API layer.
- Captured specific boundary risks around product-shaped APIs, hardcoded mod-specific GameBridge logic, multi-owner semantics, status labeling, and content/runtime conflation.

## Validation

- Documentation-only change.
- Checked local `testmods/*/manifest.json`, `testmods/*/ModEntry.cs`, README files, selected content JSON, `ExperimentalGameBridge.cs`, GameBridge feature registration, public API matrix, and hook-map references.
- No build or game smoke was run.

## Follow-Up

- Split first-party product mods, GameBridge native adapters, and framework APIs in docs and packaging.
- Move hardcoded OilMod runtime behavior toward a generic resource/drop registration API.
- Use camera leases as the model for other global mutable features.
- Keep broad gameplay APIs Experimental until independent consumers and multi-owner/lifecycle policies exist.
