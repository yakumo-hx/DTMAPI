# 20260701-0002 Shell Crab AnimalVoice JSON

## Status

source-and-runtime-json-verified / user-manual-verified

## Source Request

Add a Shell Crab trial JSON for the new reusable `AnimalVoice` content-pack audio replacement path.

## Changed Files

- `docs/goals/2026/20260701-0002-shell-crab-animalvoice-json.md`
- `docs/goals/2026/20260701-0002-shell-crab-animalvoice-json.goal.txt`
- `docs/updates/2026/20260701-0002-shell-crab-animalvoice-json.md`
- `docs/updates/INDEX.md`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab\Content\DTMAPI\audio-replacements.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_ShellCrab\Content\DTMAPI\audio-replacements.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_young.wav`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_adult.wav`

## Implementation Notes

- Added Shell Crab `AnimalVoice` content-pack JSON with two entries:
  - `shell-crab-pet-child`: `speciesId=shell_crab`, `stage=child`, `nativeSoundEvent=PLAY_ANIMAL_PET_SHEEP_CHILD`, `file=Content/Audio/shell_crab_pet_young.wav`.
  - `shell-crab-pet-adult`: `speciesId=shell_crab`, `stage=adult`, `nativeSoundEvent=PLAY_ANIMAL_PET_SHEEP`, `file=Content/Audio/shell_crab_pet_adult.wav`.
- Confirmed Shell Crab metadata uses `templateSpeciesId: goat` and `aiTemplate: goat`, while `animal_tbanimal.json` carries the reviewed sheep child/adult pet events.
- Synced the same JSON into the local runtime `DTMAPI_ShellCrab` package for hand testing.
- The local runtime package was missing `Content/Audio`, so the existing source WAV files were copied into runtime under the same relative paths used by JSON.
- No GameBridge, public API, hook, or test-code changes were required.

## Validation

- Acquired the shared runtime lock with reason `Shell Crab AnimalVoice JSON sync` before changing the local runtime package, then released it after sync.
- Parsed both source and runtime `audio-replacements.json` files with PowerShell `ConvertFrom-Json`.
- Verified all four referenced source/runtime WAV paths exist:
  - source young: `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_young.wav`
  - source adult: `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_adult.wav`
  - runtime young: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_young.wav`
  - runtime adult: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_ShellCrab\Content\Audio\shell_crab_pet_adult.wav`
- Not run: full game smoke/playback. This was a JSON-only trial sync after the Hatch framework smoke had already verified the shared `AnimalVoice` path.
- User manual QA on 2026-07-01: Shell Crab child/adult pet and hit/attacked sound paths passed, matching the Hatch manual coverage style; the replacement did not pollute native animals.

## Related Records

- Goal: `docs/goals/2026/20260701-0002-shell-crab-animalvoice-json.md`
- Shared `AnimalVoice` infrastructure: `docs/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- Shell Crab animator bridge baseline: `docs/updates/2026/20260628-0003-shell-crab-animator-bridge.md`

## Rollback

Remove `Content/DTMAPI/audio-replacements.json` from source/runtime `DTMAPI_ShellCrab`. The copied runtime WAV files are inert without the JSON and can be removed later if desired.

## Follow-Up

- If manual testing shows the runtime package should permanently include `Content/Audio`, add those WAVs to the normal Shell Crab package sync/upload flow instead of relying on this local runtime copy.
