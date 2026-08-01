# Update 20260617-0009: Custom Animal JSON AI Animator Research

> Imported archival note (2026-07-08): this record was copied from `E:\Python_project\DTMAPI-animal` to preserve the Lightning Chicken/custom-animal research trail. It is historical branch evidence, not current mainline runtime support.

Date: 2026-06-17
Status: recorded-docs-only

## Source Request

The user asked to take over the project context and research how DTMAPI can support genuinely new animals independent from the current built-in animal set, with an ordinary author workflow based on JSON for AI and animation rather than requiring Unity.

## Changed Files

- `docs/reviews/api/2026/20260617-0004-custom-animal-json-ai-animator-research.md`
- `docs/updates/2026/20260617-0009-custom-animal-json-ai-animator-research.md`
- `docs/updates/INDEX.md`

## Findings

- Existing animal native-owner research already marks custom animal runtime creation as blocked while keeping `ICustomAnimalApi` registry/definition contracts as `StableCandidate`.
- Current reverse baseline is `references/doloc-town/reverse/builds/23762374_public_C416D4`.
- Current unpacked config material includes animal tables under `content-configs/`, including `animal_tbanimal.json`, `animal_tbanimaldocument.json`, `animal_tbfeed.json`, and `animal_tbhusbandry.json`.
- Official Workshop documentation currently supports small-animal texture replacement for existing IDs, not full new animal species with AI/Animator/save integration.
- Reverse code review shows `AnimalRenderer` uses native `RuntimeAnimatorController` assets, while the official mod override path found in `DolocAssetCache`/`SpriteAsset` is Sprite-only. A Unity-free workflow therefore likely needs a DTMAPI-owned sprite-frame animation adapter or an optional advanced Unity bundle path later.
- Recommended first direction is schema and adapter research, not immediate native spawn enablement.

## Validation

- Static docs/reverse research only.
- No code changed.
- No build, install, game launch, runtime lock, or smoke test was run.

## Evidence Links

- Research report: `docs/reviews/api/2026/20260617-0004-custom-animal-json-ai-animator-research.md`
- Existing domain report: `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Current reverse baseline audit: `docs/reviews/api/2026/20260617-0003-reverse-baseline-23762374-audit.md`

## Rollback Notes

This update is docs-only. Revert the new report file, this update record, and the index row if the research direction needs to be replaced.

## Follow-Up

- Discuss MVP boundary: native animal panel/shop integration vs DTMAPI-owned custom animal runtime first.
- If continuing, create one narrow `docs/goals/2026/...` handoff for schema-only custom animal definitions and a sibling `.goal.txt`.
- Before any runtime work, re-open the current build's `Animal`, `AnimalAI`, `AnimalRenderer`, `AnimalManager`, `IAnimalHost`, `ItemAnimalPackage`, and relevant config classes, then record a method-body native-owner review.
