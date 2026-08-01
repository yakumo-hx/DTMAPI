# 20260705-0006 SMAPI Style Boundary Split

Date: 2026-07-05
Status: recorded/docs-only
Area: docs/api/boundary/smapi-reference

## Trigger

User asked how DTMAPI should adjust if it follows SMAPI as a reference, and requested a boundary-splitting research record.

## Summary

Recorded a SMAPI-style boundary split for DTMAPI. The research separates DTMAPI Framework/Core, Content Host, GameBridge native primitives, Official Mods, Diagnostics/QA, and Author Tools. It also recommends adding an explicit `ContentPackFor` manifest concept, using custom animals as the positive content-pack model, and treating AutoFishing/ActionSpeed-style APIs as first-party product surfaces until smaller primitives are designed and proven.

## Changed Files

- `docs/reviews/api/2026/20260705-0004-smapi-style-boundary-split-research.md`
- `docs/updates/2026/20260705-0006-smapi-style-boundary-split.md`
- `docs/updates/INDEX.md`

## Evidence

- Reviewed SMAPI source interfaces for mod entry, helper services, manifests, content packs, content-pack ownership, console command registration, mod API exchange, mod content, game content, and content events.
- Compared the SMAPI model to DTMAPI `IManifest`, `IDtmHelper`, manifest scanning, content/manifest registries, shadow content registry, public API matrix, and the 2026-07-05 boundary review records.
- Reused the current local conclusions that custom animals are the healthiest content-pack boundary, while AutoFishing and ActionSpeed are the clearest DTMAPI-as-functional-mod examples.

## Validation

- Documentation-only research.
- No solution build and no game smoke were run because no runtime/source behavior was changed.
- Static check after writing: `git diff --check` passed with existing line-ending normalization warnings only.

## Rollback

Remove this update record, remove the linked review, and remove the `20260705-0006` row from `docs/updates/INDEX.md`. No runtime rollback is needed.

## Follow-Up

- Add `ContentPackFor` support as a compatibility extension to the manifest model.
- Add an owned content-pack helper so content owners can read their packs directly.
- Move custom animal scanning behind an explicit `DTMAPI.CustomAnimals` content owner.
- Design primitive replacements for fishing, action-speed domains, resource drops, and command registration before promoting current product-shaped APIs.
