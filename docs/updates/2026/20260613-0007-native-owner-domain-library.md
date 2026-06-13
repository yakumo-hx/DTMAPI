# 20260613-0007 Native Owner Domain Library

- Date: 2026-06-13
- Status: recorded
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user requested implementation of the native-owner domain report library plan for broad future API/GameBridge rebuild targets.
- Version: no runtime version change; docs-only.

## Changed Files

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/README.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/native-owner-domains/SOURCE-INDEX.md`
- `docs/reviews/api/native-owner-domains/REPORT-TEMPLATE.md`
- `docs/reviews/api/native-owner-domains/01-world-time-weather-refresh.md`
- `docs/reviews/api/native-owner-domains/02-npc-body-behavior.md`
- `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
- `docs/reviews/api/native-owner-domains/04-wild-birds-events-drops.md`
- `docs/reviews/api/native-owner-domains/05-drones-runtime-equipment.md`
- `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`
- `docs/reviews/api/native-owner-domains/07-maps-dungeons-scenes-resources.md`
- `docs/reviews/api/native-owner-domains/08-hats-accessories-equipment-slots.md`
- `docs/reviews/api/native-owner-domains/09-food-equipment-effects.md`
- `docs/reviews/api/native-owner-domains/10-item-stack-quantity-limits.md`
- `docs/reviews/api/native-owner-domains/11-original-follow-pet.md`
- `docs/reviews/api/native-owner-domains/12-held-ranged-weapons-projectiles.md`
- `docs/updates/INDEX.md`

## Summary

- Added `docs/reviews/api/native-owner-domains/` as a fixed long-lived library for broad native-owner discovery records.
- Added an index, source index, report template, and 12 domain reports covering world refresh, NPCs, livestock, birds, drones, vehicles, maps, equipment, effects, stacks, follower pets, and ranged weapons.
- Linked the library from the project overview, planning/debug reference docs, reference-material boundary docs, debug index, API rebuild workflow, review rules, and public API matrix.
- Preserved the boundary that these reports are discovery records only: they do not add runtime code, public APIs, or stability promotion.

## Validation

- Passed:
  - Verified 15 files exist under `docs/reviews/api/native-owner-domains/`.
  - Verified `INDEX.md` links all 12 domain reports.
  - Verified required entry docs reference `native-owner-domains/INDEX.md`.
  - `git diff --check` exited successfully with LF/CRLF warnings only.

## Evidence

- Initial report contents are derived from read-only inspection of current reverse baseline `references/doloc-town/reverse/builds/23465763_workshop_38581E`, selected comparison against `23249387_workshop_247ACD`, official Workshop docs, existing API reviews, and the 2026-06-13 read-only parallel exploration pass.
- No game smoke, build, or unit tests were run because this update is docs-only and does not modify runtime code, hooks, public APIs, test mods, package scripts, or installed game files.

## Rollback

- Remove `docs/reviews/api/native-owner-domains/` and the added links from the changed docs if the project chooses not to maintain a fixed native-owner domain library.
- No runtime rollback is needed because this update changes documentation only.
