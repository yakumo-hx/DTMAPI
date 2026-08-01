# 20260701-0015 AnimalVoice Cooldown Native Suppression

## Status

runtime-smoke-verified / user-manual-verified

## Source Request

User asked to fix the custom animal and sound side after latest logs showed stacked animals could still leak a native frightened/pet sound, likely a little sheep voice, while broader lifecycle/audio loading concerns are deferred to a future project refactor.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Root-Cause Notes

Latest local logs showed the replacement audio was loaded and working, but stacked custom animals could trigger multiple matching native animal sound events within the same short window. The first event played the custom WAV and suppressed native audio. Later events for the same replacement hit `cooldownMilliseconds` and the old implementation recorded `replacement cooldown active; native sound allowed`, which leaked template voices such as:

- Shell Crab adult `PLAY_ANIMAL_PET_SHEEP`.
- Drecko child `PLAY_ANIMAL_PET_SHEEP_CHILD`.

The bug was therefore not missing WAVs, missing animal context, or vanilla-animal pollution. It was the cooldown policy treating "do not replay the replacement WAV yet" as "allow native Wwise audio."

## Implementation Notes

- Changed `AudioReplacementService.HandleNativeSoundEvent` so a ready replacement with `suppressNativeWhenReady=true` keeps suppressing native audio when playback is skipped only because cooldown is active.
- Cooldown now means "skip duplicate replacement playback", not "fail open to native audio".
- Kept existing fail-open behavior for:
  - no matching replacement,
  - unreviewed events,
  - missing/pending/failed WAVs,
  - unsupported emitter/callback semantics,
  - playback failure,
  - missing/stale/mismatched `AnimalVoice` context,
  - vanilla animals that do not match the scoped custom species.
- Updated `Audio.SoundEventReplacement` status publication so a cooldown-suppressed event remains `verified` instead of appearing to regress to `experimental`.
- Added unit coverage for ready+suppress+cooldown and the non-suppressing/pending/expired/no-cooldown negative cases.

## Validation

- Reverse metadata checked for current build `23762374_public_C416D4`:
  - `DolocTown.Animal.PlayAnimalSound()` still calls `DolocAPI.Sound.PostSoundEvent(...)` with adult/child `AnimalInfo` sound events.
  - `DolocTown.WwiseSoundManager.InternalPostSoundEvent(string, UnityEngine.GameObject, AkCallbackManager/EventCallback, bool)` remains the shared Wwise event post target.
- Passed: `tools/scripts/test.ps1 -Configuration Release` on 2026-07-01.
  - `DTMAPI.UnitTests: OK`.
  - Warnings were restricted-network `NU1900` package vulnerability index warnings.
- Unit coverage now verifies:
  - a ready suppressing replacement suppresses native sound during cooldown,
  - pending replacements do not suppress native sound during cooldown,
  - non-suppressing replacements do not suppress native sound during cooldown,
  - expired cooldown and disabled cooldown do not use the cooldown-suppression path.
- Passed: installed current Release runtime to the local Doloc Town directory under the shared runtime lock, then ran `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -AutoExerciseHatchAnimalVoice -SkipBuild -TimeoutSeconds 240`.
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260701-234850`.
  - Result JSON: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Summary: `HatchAnimalVoiceEvidence=...;ready=True;smoke=True;child=True;adult=True`.
  - Runtime log: Hatch child/adult replacement events both recorded `played=True suppressed=True`.
  - Runtime log: no `replacement cooldown active; native sound allowed` lines in this non-stacked smoke.
  - `process-check.txt`: `No DolocTown.exe process found.`
  - `fatal-window-check.txt`: `No fatal instance popup found.`
- User manual QA on 2026-07-01: repeated multi-custom-animal / stacked-animal testing no longer leaked the native sheep/lamb voice.

## Evidence

- Hook map current behavior: `docs/hook-map/README.md` under `Audio.SoundEventReplacement`.
- Regression row: `docs/debug/regressions/smoke-matrix.md` as `ANIMALVOICE-COOLDOWN-SUPPRESSION-20260701`.
- Latest log review before the fix found 21 cooldown fail-open events in a short stacked-animal run, including 10 Shell Crab adult sheep events and 3 Drecko child sheep-child events.
- Runtime smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260701-234850`.
- User manual QA evidence: 2026-07-01 user retest reported "手测多动物无羊叫".

## Rollback

Restore the old cooldown branch in `AudioReplacementService.HandleNativeSoundEvent` so cooldown records `suppressed=false` and continues to native audio. This will reintroduce stacked custom-animal template voice leakage and should only be used if cooldown suppression causes a worse native audio regression.

## Follow-Up

- If the sheep/lamb voice returns, collect the latest `AudioReplacement event` lines around `replacement cooldown active` and compare whether the entry was ready, suppressing, or outside the scoped custom-animal context.
- The long-title-idle `Fatal error in GC / Unexpected mark stack overflow` path remains intentionally deferred to the planned lifecycle/audio-loading refactor.
