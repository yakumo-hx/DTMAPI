# 20260701-0003 Author Docs Custom Animal Guide

## Source

User requested a complete standalone documentation set so they can follow it to make an independent new creature. The docs must live in their own folder for future DTMAPI author docs, must not be loaded into the game or official upload packages, and should be easier to follow than the official docs where possible.

## Changed Files

- `author-docs/README.md`
- `author-docs/content-packs/README.md`
- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `docs/goals/2026/20260701-0003-author-docs-custom-animal-guide.md`
- `docs/goals/2026/20260701-0003-author-docs-custom-animal-guide.goal.txt`
- `docs/updates/2026/20260701-0003-author-docs-custom-animal-guide.md`
- `docs/updates/INDEX.md`

## Summary

- Added root-level `author-docs/` as the standalone future home for DTMAPI author documentation.
- Documented that `author-docs/` is not part of runtime content, game loading, local official `MODS`, or current Workshop upload package generation.
- Added a content-pack documentation index.
- Added a full custom animal guide for the currently verified author workflow:
  - official package tree and DTMAPI extension files;
  - ID/naming rules;
  - template selection for `chicken`, `goat`, `marsh_pangolin`, and `slime`;
  - required frame counts;
  - complete JSON skeletons for a `my_mireling` example;
  - `AnimalVoice` WAV replacement config;
  - manual QA checklist and troubleshooting.

## Validation

- Inspected `tools/scripts/build-release-workshop-packages.ps1`; current release packaging copies explicit runtime/testmod/release sources and does not include arbitrary root-level directories such as `author-docs/`.
- Parsed all 11 `json` code blocks in `author-docs/content-packs/custom-animal-json-png-wav.md` with PowerShell `ConvertFrom-Json`.
- Ran `git diff --check`; it reported only the existing CRLF warning for `docs/updates/INDEX.md`.
- Static docs validation only. No DTMAPI runtime code changed.
- Not run: build/test/game smoke, because this update only adds documentation and trace records.

## Related Records

- Hatch PNG custom animal route: `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`
- Hatch `AnimalVoice` infrastructure: `docs/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- Shell Crab `AnimalVoice` JSON/manual QA: `docs/updates/2026/20260701-0002-shell-crab-animalvoice-json.md`
- Goal: `docs/goals/2026/20260701-0003-author-docs-custom-animal-guide.md`

## Rollback

Remove the `author-docs/` directory and this goal/update record pair, then remove the `20260701-0003` row from `docs/updates/INDEX.md`.

## Follow-Up

- Add a future downloadable author-docs package script only when the docs are ready for standalone public release.
- Add a validator for custom animal JSON/PNG/WAV packs so authors can check missing frames, path escapes, and sound-event mismatches before launching the game.
