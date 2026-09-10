# 20260705-0005 Custom Animal Content Boundary

## Status

recorded/docs-only

## Summary

Reviewed the current JSON + PNG + WAV custom livestock route and recorded it as the healthiest DTMAPI/mod boundary currently in the project. The route keeps animal identity, official-style content rows, frames, sounds, shop items, and produce in content packs; DTMAPI provides generic bridge mechanics and diagnostics; Doloc Town native systems continue to own animal lifecycle, package release, growth, feeding, breeding, production, room capacity, and save behavior.

## Source Request

User noted that DTMAPI currently has one route they consider strongest: adding livestock animals through PNG + WAV + JSON, close to the official content path, and asked me to inspect it in the context of the wider DTMAPI/mod boundary review.

## Changed Files

- `docs/reviews/api/2026/20260705-0003-custom-animal-content-boundary-review.md`
- `docs/updates/2026/20260705-0005-custom-animal-content-boundary.md`
- `docs/updates/INDEX.md`

## Evidence

- Reviewed `author-docs/content-packs/custom-animal-json-png-wav.md` and the local new-animal draft/generator artifacts.
- Parsed current local runtime packages under `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`:
  - `DTMAPI_HatchAssets`: `hatch`, `chicken`, `pngSpriteOverride`, 44 PNG, 2 WAV.
  - `DTMAPI_MoleAssets`: `mole`, `marsh_pangolin`, `pngSpriteOverride`, 45 PNG, 2 WAV.
  - `DTMAPI_DreckoAssets`: `drecko`, `goat`, `pngSpriteOverride`, 36 PNG, 2 WAV.
  - `DTMAPI_OilfloaterAssets`: `oilfloater`, `slime`, `pngSpriteOverride`, 48 PNG, 2 WAV.
  - `DTMAPI_ShellCrab`: advanced `assetBundle` route with 2 WAV AnimalVoice entries.
- Reviewed bridge source in CustomAnimals and AudioReplacement features.
- Reviewed `ICustomAnimalApi` and `CustomEntityRegistryService` to confirm this content route is distinct from blocked public C# runtime spawn verbs.
- Visually spot-checked generated Hatch idle/move/eat frame atlas images.

## Related Records

- `docs/reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md`
- `docs/reviews/api/2026/20260705-0002-all-dtmapi-mod-boundary-review.md`
- `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`
- `docs/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- `docs/updates/2026/20260701-0008-mole-drecko-hatch-products.md`
- `docs/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md`

## Validation

- Documentation-only review.
- Ran no solution build and no game smoke because no runtime/source behavior was changed.
- Static checks after writing the docs: `git diff --check` passed with existing line-ending normalization warnings only.

## Rollback

Remove this update record, remove the linked review, and remove the `20260705-0005` row from `docs/updates/INDEX.md`. No runtime rollback is needed.

## Follow-Up

- Turn this route into an explicit authoring/validation product: package preflight, schema checks, frame-count checks, sound-event checks, and clearer diagnostics.
- Keep `ICustomAnimalApi.RequestSpawn` and other public runtime creation verbs documented as blocked until native adapters are actually proven.
- Use this content-pack route as the preferred boundary pattern when redesigning DTMAPI-heavy functional mods.
