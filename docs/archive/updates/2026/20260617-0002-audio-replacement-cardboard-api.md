# 20260617-0002 Audio Replacement Cardboard API

## Status

verified-experimental-smoke

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
- Added fail-open replacement semantics: native Wwise sound is suppressed only after a local WAV playback backend starts without throwing. Missing, pending, failed, cooldown-skipped, invalid, unsupported emitter/callback, or playback-failed replacements allow native audio.
- Added a local WAV load cascade. DTMAPI first tries Unity's normal local WAV decode path (`UnityWebRequestMultimedia.GetAudioClip`) and keeps the earlier PCM diagnostic paths. In the current game runtime those Unity `AudioClip` paths create zero-metadata clips for `manbo.wav`, so the first verified slice falls back to `System.Media.SoundPlayer` for local PCM WAV playback. This fallback is GameBridge-owned and remains Experimental because it is outside Wwise bus/mixer/3D semantics.
- Narrowed the first implementation to reviewed sound events only: registration currently accepts `PLAY_RESOURCE_PAPER_BOX`, rejects duplicate suppressing replacements for that event, and keeps broader Wwise/bus/3D/callback semantics out of the public promise.
- Tightened the Wwise hook to exact `InternalPostSoundEvent(string, UnityEngine.GameObject, EventCallback, bool) -> bool` matching and added it to the hook retry completion gate so retries do not stop before the audio hook is patched.
- Added developer test mod `Yuuka.DTMAPI.ManboCardboardAudio`, packaged as official local folder `Yuuka_DTMAPI_ManboCardboardAudio`, targeting `PLAY_RESOURCE_PAPER_BOX` with `assets/manbo.wav`.
- Added `run-game-smoke.ps1 -AutoExerciseAudioReplacement`, gated to explicit `-SaveSlot 10`, which loads the tenth save fixture, sends a real `E` key to the game window, and requires post-key runtime log evidence for both native `DungeonResourceModelPaperBox.OnInteract` and the replacement `PLAY_RESOURCE_PAPER_BOX` event.

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
- Earlier blocked smokes:
  - `docs/debug/evidence/GAME-SMOKE/20260617-052549`: `AudioReplacement` hook and smoke request fire, but Unity `AudioClip.Create(... PCMReaderCallback)` returns a clip with zero metadata, so the replacement remains retry/pending and native audio is allowed.
  - `docs/debug/evidence/GAME-SMOKE/20260617-053452`: local PCM WAV preload reaches `ready`, but `AudioSource.Play()` does not enter playing state, so DTMAPI keeps fail-open native audio and smoke result stays `AudioReplacement=Failed`. Exit checks are clean: no `DolocTown.exe` and no fatal instance window.
- `docs/debug/evidence/GAME-SMOKE/20260617-060219`: after preferring `AudioClip.Create + SetData`, Unity `AudioClip.SetData` repeatedly returns `false`, so native audio remains fail-open.
- `docs/debug/evidence/GAME-SMOKE/20260617-061723`: `UnityWebRequestMultimedia.GetAudioClip` starts and completes, but `DownloadHandlerAudioClip.GetContent` returns a zero-metadata `AudioClip`, so native audio remains fail-open.
- `docs/debug/evidence/GAME-SMOKE/20260617-062938` passed a pre-hardening replacement event smoke, but later review found it was not enough to prove a real paper-box interaction because the smoke still used a synthetic `DolocAPI.Sound.PostSoundEvent` path.
- Verified: `docs/debug/evidence/GAME-SMOKE/20260617-065447` passed with `RunStatus=Passed`, `AudioReplacement=Passed`, `SaveLoaded=Passed`, `StartupLog=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Runtime log: `AudioReplacement local WAV ready ... backend=platform ... detail=AudioClip metadata invalid. length=0 samples=0 channels=0 frequency=0; System.Media.SoundPlayer WAV loaded.`
  - Runtime log: `Smoke exercise AudioReplacement waiting for external E input to trigger DungeonResourceModelPaperBox.OnInteract; synthetic DolocAPI.Sound.PostSoundEvent is disabled for this smoke.`
  - Summary: `SentExternalAudioReplacementInteractAttempt2=...;key=E;ok=True;...` followed by `AudioReplacementInteractAttempt2Evidence=...;paperBoxOnInteract=True;replacementEvent=True`.
  - Runtime log: `AudioReplacement paper-box OnInteract owner=DungeonResourceModelPaperBox event=PLAY_RESOURCE_PAPER_BOX instance=DolocTown.DungeonResourceModelPaperBox.`
  - Runtime log: `AudioReplacement event owner=Yuuka.DTMAPI.ManboCardboardAudio replacement=manbo-paper-box event=PLAY_RESOURCE_PAPER_BOX played=True suppressed=True ... backend=platform ...`.
- Parallel multi-agent code review completed and identified hardening blockers: synthetic smoke proof, silent suppression risk, weak signature matching, retry-gate omission, broad event surface, and duplicate suppressing ambiguity. The code now addresses those blockers for the first paper-box slice; owner unload cleanup, broader Wwise semantics, and non-Windows/platform playback policy remain follow-ups.

## Related Records

- Goal: `docs/goals/2026/20260617-0001-audio-replacement-cardboard.md`
- API/native-owner review: `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback

Disable or remove `Yuuka_DTMAPI_ManboCardboardAudio` from the official local MODS folder to stop the sample mod. If the GameBridge API itself must be rolled back, remove `AudioReplacementFeature` registration and the `IAudioReplacementApi` DTOs; no vanilla save data is written by this API.

## Follow-up

- Keep `IAudioReplacementApi` Experimental. The first paper-box slice is verified, but the verified playback backend is platform WAV playback rather than Wwise bus/mixer/3D playback.
- Continue native-owner research for a Wwise-native or Unity-native playback path that the game build accepts without zero-metadata clips.
- Before promotion, prove owner unload cleanup, broader multi-mod arbitration, non-Windows behavior or explicit platform policy, and at least two real mod use cases.
