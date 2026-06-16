# 20260617-0002 Audio Replacement Cardboard API

## Status

blocked-game-smoke

## Source Request

Active goal: build a second-level branch for music/audio file replacement, replace the old hard-to-test monster projectile sound target with the mod `DTMAPI 曼波音频替换开纸箱子`, and replace the native wild paper-box open sound with `D:\下载\manbo.wav`. The API must start from native responsibility functions and remain long-term maintainable. A parallel multi-agent code review is required before completion.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `testmods/ManboCardboardAudioMod/*`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/goals/2026/20260617-0001-audio-replacement-cardboard.md`
- `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`

## Implementation Notes

- Added experimental `IAudioReplacementApi` and DTOs. The public contract accepts DTMAPI-owned strings and file paths only; it does not expose raw Wwise, Unity, Harmony, or decompiled Doloc Town types.
- Added `AudioReplacementFeature`, which registers the API through GameBridge and installs a narrow Wwise prefix on `DolocTown.WwiseSoundManager.InternalPostSoundEvent`.
- Added fail-open replacement semantics: native Wwise sound is suppressed only after the local WAV is loaded, `AudioSource` properties and `Play()` are available, and DTMAPI local playback starts. Missing, pending, failed, cooldown-skipped, invalid, unsupported emitter/callback, or playback-failed replacements allow native audio.
- Narrowed the first implementation to reviewed sound events only: registration currently accepts `PLAY_RESOURCE_PAPER_BOX`, rejects duplicate suppressing replacements for that event, and keeps broader Wwise/bus/3D/callback semantics out of the public promise.
- Tightened the Wwise hook to exact `InternalPostSoundEvent(string, UnityEngine.GameObject, EventCallback, bool) -> bool` matching and added it to the hook retry completion gate so retries do not stop before the audio hook is patched.
- Added developer test mod `Yuuka.DTMAPI.ManboCardboardAudio`, packaged as official local folder `Yuuka_DTMAPI_ManboCardboardAudio`, targeting `PLAY_RESOURCE_PAPER_BOX` with `assets/manbo.wav`.
- Added `run-game-smoke.ps1 -AutoExerciseAudioReplacement`, gated to explicit `-SaveSlot 10`, which loads the tenth save fixture, sends a real `E` key to the game window, and requires the runtime log line proving `PLAY_RESOURCE_PAPER_BOX` was played by the replacement and suppressed natively.

## Native Owner

The reviewed native path is:

`DungeonResourceModelPaperBox.OnInteract` -> `DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_PAPER_BOX)` -> `WwiseSoundManager.InternalPostSoundEvent` -> `AkSoundEngine.PostEvent`.

The API hooks the shared Wwise internal event owner rather than paper-box gameplay logic, so item drops, removal, save state, particle effects, and paper-box interaction ownership remain native.

## Validation

- Release build passed before this record was opened.
- Release build passed after the parallel-review hardening for exact signature matching, reviewed-event allowlist, duplicate suppressing registration rejection, unsupported emitter/callback fail-open behavior, playback verification, and hook retry readiness.
- `git diff --check` passed on 2026-06-17 with Windows line-ending warnings only.
- Release build passed on 2026-06-17 after adding local PCM WAV parsing and callback-clip fallback diagnostics.
- Release tests passed on 2026-06-17 with `DTMAPI.UnitTests: OK`.
- `tools/scripts/run-game-smoke.ps1` now parses successfully after adding `-AutoExerciseAudioReplacement`.
- Blocked: explicit tenth-save game smoke with `-SaveSlot 10 -AutoExerciseAudioReplacement` does not yet prove replacement playback.
  - `docs/debug/evidence/GAME-SMOKE/20260617-052549`: `AudioReplacement` hook and smoke request fire, but Unity `AudioClip.Create(... PCMReaderCallback)` returns a clip with zero metadata, so the replacement remains retry/pending and native audio is allowed.
  - `docs/debug/evidence/GAME-SMOKE/20260617-053452`: local PCM WAV preload reaches `ready`, but `AudioSource.Play()` does not enter playing state, so DTMAPI keeps fail-open native audio and smoke result stays `AudioReplacement=Failed`. Exit checks are clean: no `DolocTown.exe` and no fatal instance window.
- Parallel multi-agent code review completed and identified hardening blockers: silent suppression risk, weak signature matching, retry-gate omission, broad event surface, and duplicate suppressing ambiguity. The code now addresses those blockers for the first paper-box slice; owner unload cleanup, broader Wwise semantics, and stronger smoke native-owner telemetry remain follow-ups.

## Related Records

- Goal: `docs/goals/2026/20260617-0001-audio-replacement-cardboard.md`
- API/native-owner review: `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback

Disable or remove `Yuuka_DTMAPI_ManboCardboardAudio` from the official local MODS folder to stop the sample mod. If the GameBridge API itself must be rolled back, remove `AudioReplacementFeature` registration and the `IAudioReplacementApi` DTOs; no vanilla save data is written by this API.

## Follow-up

- Do not merge this branch into `Refactor` until the tenth-save paper-box smoke reaches `played=True suppressed=True`.
- Continue native-owner research for a playback path that the game build accepts. The current Unity `AudioClip`/`AudioSource` path can register and fail open, but does not produce verified local playback.
- Keep `IAudioReplacementApi` Experimental until conflict policy, owner cleanup, Wwise bus differences, and at least two real mod use cases are proven.
