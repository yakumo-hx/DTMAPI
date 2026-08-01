# 2026-07-01 Shell Crab AnimalVoice JSON Goal

## Source Request

Add a trial `AnimalVoice` audio replacement JSON for the Shell Crab content package, reusing the new content-pack JSON plus WAV infrastructure without changing GameBridge code.

## Scope

- Add `Content/DTMAPI/audio-replacements.json` to the Shell Crab prototype package.
- Sync the same JSON to the local runtime `DTMAPI_ShellCrab` package for manual testing.
- Use the existing Shell Crab WAV files:
  - `Content/Audio/shell_crab_pet_young.wav`
  - `Content/Audio/shell_crab_pet_adult.wav`
- Do not add DLL code, Wwise banks, copied game audio, BGM/music handling, or new public API.

## Native Owner Findings

- Shell Crab custom animal id: `shell_crab`.
- Shell Crab custom animal metadata uses `templateSpeciesId: goat` and `aiTemplate: goat`.
- Shell Crab `animal_tbanimal.json` defines the native child sound event as `PLAY_ANIMAL_PET_SHEEP_CHILD`.
- Shell Crab `animal_tbanimal.json` defines the native adult sound event as `PLAY_ANIMAL_PET_SHEEP`.
- Replacement must remain scoped by `speciesId=shell_crab`, stage, and native event so native sheep/goat-like calls are not globally replaced.

## Content Configuration

```json
[
  {
    "id": "shell-crab-pet-child",
    "category": "AnimalVoice",
    "speciesId": "shell_crab",
    "stage": "child",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_SHEEP_CHILD",
    "file": "Content/Audio/shell_crab_pet_young.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  },
  {
    "id": "shell-crab-pet-adult",
    "category": "AnimalVoice",
    "speciesId": "shell_crab",
    "stage": "adult",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_SHEEP",
    "file": "Content/Audio/shell_crab_pet_adult.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  }
]
```

## Validation Requirements

- Parse the source and runtime JSON with `ConvertFrom-Json`.
- Verify both referenced WAV files exist in the source package.
- Verify both referenced WAV files exist in the runtime package after sync.
- Full game playback smoke is not required for this JSON-only trial unless manual testing exposes a mismatch.

## User Manual QA

- 2026-07-01: User manually verified Shell Crab child/adult pet and hit/attacked sound paths, matching the Hatch coverage style.
- 2026-07-01: User manually verified the Shell Crab replacement did not pollute native animals.

## Rollback

Remove `Content/DTMAPI/audio-replacements.json` from `DTMAPI_ShellCrab` to stop Shell Crab voice replacement. The copied runtime `Content/Audio` WAV files can remain inert without the JSON.
