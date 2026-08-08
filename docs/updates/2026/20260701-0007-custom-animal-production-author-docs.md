# 20260701-0007 Custom Animal Production Author Docs

## Source

User asked to patch the standalone custom-animal author documentation, change the Oilfloater prototype so it produces one coal per day, and perform code-level research on which animal production, breeding, feeding, hidden-product, capacity, and mixed-husbandry parameters can be content-author controlled.

## Changed Files

- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `docs/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md`
- `docs/updates/2026/20260701-0007-custom-animal-production-author-docs.md`
- `docs/updates/INDEX.md`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\oilfloater\DTMAPI_OilfloaterAssets\Content\item_tbitemspawn.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\oilfloater\DTMAPI_OilfloaterAssets\Content\item_tbitem.json`

## Summary

- Added author-facing guidance for animal growth, fertility, breeding duration, production cadence, base product selection, room capacity, feed cadence, mood gate, `manual_metabolism`, and sprite/body sizing.
- Documented product spawn semantics: first-animal content should use a clear 1-item `produce_spawn_entry` and single `item_tbitemspawn` row; weighted or multi-item output is supported but must respect equipment capacity.
- Documented hidden husbandry products as an advanced native route through `animal_tbhusbandry.json` and `animal_tbhusbandryenergy.json`: they have independent progress, default feeder storage does not grow that progress, and crop/pasture/wild-grass-style eating paths can because they pass item identity.
- Documented native equipment capacities and mixed-husbandry behavior:
  - chicken nest: 10 item units;
  - honey comb: 10 item units;
  - lint roller: 20 item units;
  - milking machine: 20 item units.
- Clarified that shared-template custom animals share equipment capacity and that one equipment instance can store multiple product item IDs, while breeding remains same-proto gated.
- Changed Oilfloater `oilfloater_produce` to produce `coal` with `min_count=1` and `max_count=1`, paired with existing `count_range 1..1`.
- Removed the unused prototype `oilfloater_meat` item row from Oilfloater `item_tbitem.json`; `sack_oilfloater` remains.

## Validation

Passed:

- Parsed 10 Oilfloater content JSON files with PowerShell `ConvertFrom-Json`.
- Parsed all 11 JSON code blocks in `author-docs/content-packs/custom-animal-json-png-wav.md`.
- Ran `git diff --check`; it reported only LF/CRLF normalization warnings for edited Markdown files and no diff-check failures.

Not planned for this pass:

- Build/test, because this is documentation plus external content JSON only.
- Runtime slot-7 smoke, because no runtime operation was requested for this turn and the shared game runtime lock was not acquired.

## Evidence Links

- Research report: `docs/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md`
- Author guide: `author-docs/content-packs/custom-animal-json-png-wav.md`
- Related author-guide record: `docs/updates/2026/20260701-0003-author-docs-custom-animal-guide.md`
- Related sprite-size record: `docs/updates/2026/20260701-0006-author-docs-sprite-size-collider-guidance.md`

## Rollback

Restore Oilfloater `item_tbitemspawn.json` and `item_tbitem.json` from the previous prototype package state, remove the production/hidden-product/equipment sections from the author guide, delete the review report, and remove this update row from `docs/updates/INDEX.md`.

## Follow-Up

- Runtime-test Oilfloater on slot 7 after installing/syncing the package under the shared runtime lock.
- If hidden products become part of the official author workflow, add a dedicated sample content pack and runtime/manual evidence for `animal_tbhusbandry` plus `animal_tbhusbandryenergy`.
