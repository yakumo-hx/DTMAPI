# 2026-07-01 Hatch AnimalVoice Audio Replacement Goal

## Source Request

Implement a reusable short-SFX replacement infrastructure for DTMAPI content packs, using Hatch as the first real case. Authors should only need JSON plus WAV files; no DLL, Wwise bank, copied game audio, or class-game audio asset should be required.

Hatch reuses the vanilla chicken animal animation controller and AI template, so its native pet sounds are still chicken events. The goal is to replace only Hatch child/adult calls with Hatch package WAV files while leaving vanilla chickens untouched.

## Scope

- Add content-pack JSON schema `Content/DTMAPI/audio-replacements.json`.
- Support the first content-pack category `AnimalVoice`.
- Keep existing code-mod paper-box replacement behavior working as `SimpleSfx`.
- Do not promote or widen `IAudioReplacementApi`.
- Do not implement music, BGM, looped audio, STOP events, state/RTPC, callback, or Wwise-bank replacement.

## Hatch Content Configuration

Expected Hatch package entries:

```json
[
  {
    "id": "hatch-pet-child",
    "category": "AnimalVoice",
    "speciesId": "hatch",
    "stage": "child",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_CHICKEN_CHILD",
    "file": "Content/Audio/hatch_pet_young.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  },
  {
    "id": "hatch-pet-adult",
    "category": "AnimalVoice",
    "speciesId": "hatch",
    "stage": "adult",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_CHICKEN",
    "file": "Content/Audio/hatch_pet_adult.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  }
]
```

## Native Owner Findings

- Native animal call owner: `DolocTown.Animal.PlayAnimalSound()`.
- Native Hatch sound events are inherited from the chicken template through the animal proto sound-event fields.
- Native sound manager path remains `DolocAPI.Sound.PostSoundEvent(...)` -> `WwiseSoundManager.InternalPostSoundEvent(...)` -> `AkSoundEngine.PostEvent(...)`.
- Event-name replacement alone is unsafe for Hatch because vanilla chickens use the same native event names.

## Implementation Requirements

- Add an `Animal.PlayAnimalSound()` prefix/postfix context route that captures the active animal `protoName`, child/adult stage, and expected native sound event.
- Let the existing Wwise `InternalPostSoundEvent` prefix consume that context and match `AnimalVoice` only when `speciesId + stage + nativeSoundEvent` match.
- Expire stale animal contexts defensively so later native chicken sounds cannot inherit Hatch state.
- Allow reviewed animal pet events only, starting with the chicken child/adult events needed by Hatch.
- Suppress native Wwise only after local WAV playback starts; otherwise fail open to vanilla audio.
- Scan enabled content packs every few seconds and validate JSON category, stage, reviewed event, duplicate suppressing scope, missing WAV, and path escape.
- Keep the C# `IAudioReplacementApi` first slice limited to reviewed `PLAY_RESOURCE_PAPER_BOX`.

## Validation Requirements

- `tools/scripts/test.ps1 -Configuration Release`.
- `git diff --check`.
- Runtime smoke under the shared runtime lock.
- Install current runtime and synced `DTMAPI_HatchAssets`.
- Run save slot 7 with `-AutoExerciseHatchAnimalVoice`.
- Evidence must show:
  - startup and hook status,
  - Hatch content-pack audio replacement JSON registered,
  - child Hatch call plays `hatch_pet_young.wav` and suppresses native chicken child event,
  - adult Hatch call plays `hatch_pet_adult.wav` and suppresses native chicken adult event,
  - vanilla chicken negative check is recorded when available,
  - no leftover `DolocTown.exe` and no fatal popup.

## User Manual QA

- 2026-07-01: User manually verified Hatch adult and child pet sounds.
- 2026-07-01: User manually verified Hatch adult and child hit/attacked sound paths.
- 2026-07-01: User manually verified the original vanilla chicken is not polluted by the Hatch replacement.

## Test Requirements

- Unit tests parse valid Hatch JSON and reject invalid category/stage/event/path escape.
- Hatch child/adult contexts match only the corresponding WAV.
- Vanilla chicken, missing context, stale context, wrong stage, and wrong event allow native audio.
- Existing Manbo paper-box registration behavior remains unchanged.

## Rollback

Disable or remove `Content/DTMAPI/audio-replacements.json` from `DTMAPI_HatchAssets` to stop the Hatch voice replacement without touching save data. If the GameBridge bridge must be rolled back, remove the `Animal.PlayAnimalSound` context hook and content-pack registration path while preserving the older paper-box `IAudioReplacementApi` behavior.
