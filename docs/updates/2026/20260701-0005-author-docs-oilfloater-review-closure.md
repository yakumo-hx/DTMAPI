# 20260701-0005 Author Docs Oilfloater Review Closure

## Source

User requested a three-way review of the custom animal author guide: one reviewer from the perspective of an artist who is not comfortable with JSON, one from the perspective of an experienced mod author and documentation writer, and one docs-only attempt against the new ONI-derived oilfloater material under `E:/DolocTownUnity/DolocTownMeta/prototypes/oilfloater/DTMAPI_OilfloaterAssets/Content`.

## Changed Files

- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `docs/updates/2026/20260701-0005-author-docs-oilfloater-review-closure.md`
- `docs/updates/INDEX.md`

## Summary

- Added beginner JSON guardrails, a first-test route, and a rename checklist for authors who are primarily artists.
- Clarified the package root layout, optional local/release assets, and the difference between official content JSON and DTMAPI-specific JSON.
- Added validation status notes for `chicken`, `goat`, `marsh_pangolin`, and `slime` templates so authors do not treat static template data as equal to Hatch-level runtime verification.
- Documented PNG authoring rules: one PNG per frame, transparent background, consistent stage canvas, alignment, current lack of left/right suffix support, and intentional `sprite_size` mismatches.
- Clarified current stable `custom-animals.json` fields and warned that prototype/tool fields such as `movementMultiplier`, `packageItemId`, and `shopItemListId` are not current runtime author interfaces.
- Clarified `AnimalVoice` matching as `animal_tbanimal.json levels[*].sound_event` plus matching `audio-replacements.json nativeSoundEvent`, whitelisted short SFX events, and species/stage context.
- Tightened animal document guidance to the current official `AnimalDocumentInfo` field set and warned against unverified prototype fields such as `child_icon`, `adult_icon`, and `entries`.
- Added static preflight checks, shop/product zero-value notes, and troubleshooting sections for package detection and animal document issues.

## Oilfloater Static Review

- Parsed the oilfloater content directory shape and confirmed the expected DTMAPI/custom animal/audio files are present with 48 PNG frames and 2 WAV files.
- Confirmed the PNG frame inventory matches the `slime` template count: 24 young frames and 24 adult frames across `idle`, `move`, `eat`, `sleep`, and `jump`.
- Confirmed `audio-replacements.json` targets `speciesId=oilfloater` with whitelisted honey amoeba events matching `animal_tbanimal.json`.
- Identified oilfloater as still prototype-quality for release because `animal_tbanimaldocument.json` uses unverified prototype fields rather than the current official document fields, store `spawn_weight` values are all `0`, product counts are `0..0`, and the young `sprite_size` differs from the actual young PNG canvas.

## Validation

- Parsed all 11 `json` code blocks in `author-docs/content-packs/custom-animal-json-png-wav.md` with PowerShell `ConvertFrom-Json`.
- Parsed all 10 oilfloater content JSON files with PowerShell `ConvertFrom-Json`.
- Ran static content count over the oilfloater content directory and confirmed `.json 10`, `.png 48`, `.wav 2` under `Content/`.
- Checked oilfloater PNG dimensions: adult frames are `38x30`, young frames are `32x36`.
- Re-read the revised author guide sections after patching.
- Ran `git diff --check`; it reported only existing LF/CRLF normalization warnings.
- Not run: build/test/game smoke, because this change only updates standalone author documentation and performs static review of a prototype content pack outside the repository.

## Related Records

- Author guide creation: `docs/updates/2026/20260701-0003-author-docs-custom-animal-guide.md`
- PNG direction clarification: `docs/updates/2026/20260701-0004-author-docs-png-frame-direction.md`
- Hatch `AnimalVoice` infrastructure: `docs/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- Shell Crab `AnimalVoice` manual QA: `docs/updates/2026/20260701-0002-shell-crab-animalvoice-json.md`

## Rollback

Remove the new author-guide clarifications, delete this update record, and remove the `20260701-0005` row from `docs/updates/INDEX.md`.

## Follow-Up

- Add an actual validator script for custom animal JSON/PNG/WAV packages so the preflight checklist can be automated.
- If oilfloater should become a releaseable sample, convert its animal document JSON to current official fields, decide whether shop/product zero values are intentional, resolve or document the young `sprite_size` mismatch, then run game smoke/manual QA under the shared runtime lock.
