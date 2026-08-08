# 20260701-0001 Hatch AnimalVoice Audio Replacement

## Status

runtime-smoke-verified / user-manual-verified

## Source Request

Implement the planned Hatch AnimalVoice audio replacement infrastructure: a reusable, native-function-aligned short-SFX replacement path where content authors can ship only JSON plus WAV files. The first case is Hatch, whose custom animal reuses native chicken animation/AI and therefore shares vanilla chicken call events. Replacement must be scoped to Hatch child/adult calls and must not pollute vanilla chickens.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/goals/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- `docs/goals/2026/20260701-0001-hatch-animal-voice-audio-replacement.goal.txt`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\DTMAPI\audio-replacements.json`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\DTMAPI\audio-replacements.json`

## Implementation Notes

- Added content-pack schema `Content/DTMAPI/audio-replacements.json` for short-SFX replacements. The first supported category is `AnimalVoice`.
- Extended `AudioReplacementService` from one hard-coded paper-box allowlist into a policy table:
  - `SimpleSfx` keeps the current code-mod `PLAY_RESOURCE_PAPER_BOX` behavior for Manbo.
  - `AnimalVoice` accepts reviewed `PLAY_ANIMAL_PET_*` events only through active animal context.
- Added enabled content-pack scanning on the same cadence style as custom animals. The scanner resolves replacement WAV paths under the content-pack root and rejects unsupported categories, invalid stages, unreviewed events, missing WAV files, duplicate suppressing scopes, and path escapes.
- Added `Animal.PlayAnimalSound()` prefix/postfix hooks. The prefix captures current animal `protoName`, child/adult stage, and expected native sound event; the existing Wwise prefix consumes the context and matches `AnimalVoice` entries by `speciesId + stage + nativeSoundEvent`; the postfix clears context.
- Added defensive stale-context expiry so later vanilla chicken sounds cannot inherit a previous Hatch call.
- Kept fail-open audio semantics: native Wwise audio is suppressed only after local WAV playback starts. Missing/pending/failed WAV, cooldown, playback failure, missing context, stale context, and mismatched animal scope all allow native audio.
- Added slot 7 smoke flag `-AutoExerciseHatchAnimalVoice`, which finds `protoName=hatch`, toggles native `DEBUG_SetAdult(false/true)`, calls native `PlayAnimalSound()`, and waits for child/adult replacement logs.
- Added Hatch content-pack JSON entries using existing WAV files `Content/Audio/hatch_pet_young.wav` and `Content/Audio/hatch_pet_adult.wav`.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` on 2026-07-01. `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` package vulnerability source warnings were emitted.
- Unit coverage now verifies:
  - valid Hatch JSON parsing,
  - invalid category/stage/event/path escape rejection,
  - Hatch child context selects only the young WAV,
  - Hatch adult context selects only the adult WAV,
  - vanilla chicken, no context, stale context, mismatched stage, and mismatched event do not match Hatch replacement,
  - existing paper-box Manbo registration remains on the reviewed public API path while animal events remain rejected through `IAudioReplacementApi`.
- Passed: `git diff --check` on 2026-07-01 with CRLF warnings only.
- Retained failed runtime attempt: `docs/debug/evidence/GAME-SMOKE/20260701-071336` exposed a smoke-harness bug where `TryExerciseHatchAnimalVoiceForSmoke()` forced content-pack audio refresh every frame, repeatedly removed/rebuilt the same Hatch replacements, and prevented preload readiness from stabilizing. The fix adds a one-shot smoke refresh guard before waiting for preload state.
- Verified: `docs/debug/evidence/GAME-SMOKE/20260701-073005` passed slot 7 DirectExe smoke under the shared runtime lock with `-IncludeHookProbe -SaveSlot 7 -AutoExerciseHatchAnimalVoice`.
  - Result JSON: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Summary: `HatchAnimalVoiceEvidence=...;ready=True;smoke=True;child=True;adult=True`.
  - Runtime log: `AudioReplacement content-pack refresh reason=Smoke.HatchAnimalVoice registered=2 replacements=DTMAPI.HatchAssets/hatch-pet-adult:AnimalVoice:hatch:adult:PLAY_ANIMAL_PET_CHICKEN,DTMAPI.HatchAssets/hatch-pet-child:AnimalVoice:hatch:child:PLAY_ANIMAL_PET_CHICKEN_CHILD`.
  - Runtime log: `AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-child event=PLAY_ANIMAL_PET_CHICKEN_CHILD played=True suppressed=True ... path=...\Content\Audio\hatch_pet_young.wav`.
  - Runtime log: `AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-adult event=PLAY_ANIMAL_PET_CHICKEN played=True suppressed=True ... path=...\Content\Audio\hatch_pet_adult.wav`.
  - Runtime log: `Smoke exercise HatchAnimalVoice OK hatch=hatch childEvent=PLAY_ANIMAL_PET_CHICKEN_CHILD adultEvent=PLAY_ANIMAL_PET_CHICKEN vanillaChickenCheck=not-applicable`.
  - `process-check.txt`: `No DolocTown.exe process found.`
- User manual QA on 2026-07-01: Hatch adult and child pet sounds passed; Hatch adult and child hit/attacked sound paths passed; the original vanilla chicken was not polluted by the Hatch replacement.

## Related Records

- Goal: `docs/goals/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- Prior audio API slice: `docs/updates/2026/20260617-0002-audio-replacement-cardboard-api.md`
- Prior audio research: `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`
- Hatch custom animal baseline: `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback

Remove `Content/DTMAPI/audio-replacements.json` from `DTMAPI_HatchAssets` to stop Hatch voice replacement while keeping custom animal visuals/AI and save data intact. If runtime hooks must be backed out, remove the `Animal.PlayAnimalSound` context hook and content-pack `AnimalVoice` registration path; keep the older Manbo paper-box `SimpleSfx`/`IAudioReplacementApi` route unless that route separately regresses.

## Follow-Up

- The automated slot 7 vanilla-chicken negative check was not applicable because no separate `protoName=chicken` animal was found, but user manual QA on 2026-07-01 confirmed the original vanilla chicken was not polluted. Keep the unit/context negative coverage for regression protection.
- Keep `IAudioReplacementApi` Experimental and do not expose `AnimalVoice` as public C# API until owner unload, arbitration, backend, and broader real-mod cases are proven.
- Defer BGM/music/looping audio to a separate design because native BGM has callbacks, STOP events, and stateful playback concerns outside this short-SFX scope.
