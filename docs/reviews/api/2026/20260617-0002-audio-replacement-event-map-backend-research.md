# Audio Replacement Event-Map And Backend Research

## Status

recorded-docs-only

This is a research follow-up to `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md` and the first verified paper-box slice in `docs/updates/2026/20260617-0002-audio-replacement-cardboard-api.md`.

It records the external research notes supplied by the user plus the current local reverse/code facts. It does not implement a new backend, does not expand the current allowlist, and does not promote `IAudioReplacementApi` beyond `Experimental`.

## Source Inputs

- User-provided external research note: Stardew Valley and Don't Starve/DST audio replacement patterns are ID/event-map based, not one-native-function-per-sound.
- User-provided external research note: Doloc Town's current facts point at Wwise event-name replacement, not raw `AudioClip` replacement.
- Local DTMAPI facts from the verified first slice:
  - `DungeonResourceModelPaperBox.OnInteract` posts `SoundEvents.PLAY_RESOURCE_PAPER_BOX`.
  - `WwiseSoundManager.InternalPostSoundEvent(string eventName, GameObject emitterObj, EventCallback callback, bool ignoreOccupy)` is the shared native event bridge.
  - `SoundEvents` currently exposes 177 event names.
  - `WwiseSoundManager.soundEventLUT` maps events to `SFX` and `MUSIC` banks.
  - BGM routes through `PlayBgm`, `PlayBGMInternal`, `currentBgmEventName`, `overideBgmEventName`, `isBgmPlaying`, `HandleOnBgmEvent`, `STOP_BGM`, and `STOP_BGM_IMMEDIATELY`.
  - The current `manbo.wav` slice is verified through a platform WAV fallback because Unity `AudioClip` load/create paths produced zero-metadata clips or `SetData` failure in the tested game runtime.

## Core Conclusion

The long-term audio replacement API should not be built by finding one gameplay native responsibility function per sound.

Mature mod ecosystems use a stable audio identifier first, then route that identifier through a replacement map:

- Stardew Valley: cue ID / sound ID to local audio.
- Don't Starve / DST: FMOD event path to remapped event.
- Doloc Town: Wwise `eventName` / `SoundEvents` value to replacement policy.

Native gameplay responsibility functions still matter when proving what a player action owns, but they should not be the primary replacement API key. For Doloc Town, the primary key should be the reviewed Wwise event name, with optional context fields when the same event is reused in multiple meanings.

## Recommended Architecture

The future backend should be layered:

1. Wwise custom SoundBank + event-name redirect.

   This is the preferred route for BGM, looped sound, STOP events, callback-sensitive events, 3D emitters, RTPC/state/bus/mixer behavior, and anything that must remain inside Wwise semantics.

   DTMAPI should not attempt to load a second custom `Init.bnk` or rely on same-name event override behavior. Instead, hook should redirect:

   ```text
   PLAY_RESOURCE_PAPER_BOX -> DTM_PLAY_RESOURCE_PAPER_BOX
   ```

   The replacement event must live in a loaded custom bank. The original event should be suppressed only after the replacement route is known playable.

2. Unity `AudioSource` pool + local audio decode.

   This is a fallback for short SFX, UI sounds, simple one-shot harvest/open/click sounds, and events without callback/loop/STOP semantics.

   It should use DTMAPI-owned decoding/loading rather than relying on the Unity path that produced zero-metadata clips in the first slice. A minimal reliable backend should support common PCM WAV first; OGG Vorbis can be a later layer if licensing and runtime packaging are acceptable.

3. `System.Media.SoundPlayer`.

   Keep this as debug/probe fallback only. It proves hook and file packaging success, but it is outside Unity/Wwise mixer, bus, 3D, callback, pause, and volume semantics. It should not become the official broad replacement backend.

## Event Policy Table

The next API rebuild should generate a policy table from `SoundEvents` plus `soundEventLUT`, then refine it with runtime discovery logs.

Suggested categories:

| Category | Initial Detection | Replacement Policy |
| --- | --- | --- |
| `OneShotSimple` | SFX, short, no callback, no STOP/loop pair | Unity one-shot or Wwise redirect may suppress original after verified playback |
| `OneShot3D` | SFX with emitter object and no callback | Prefer Wwise; Unity fallback must follow emitter/spatial policy |
| `Ui2D` | UI/menu/click event | Unity 2D pool acceptable after latency test |
| `LoopSfx` | PLAY/STOP pair, machine/ambient/continuous sound | Prefer Wwise; Unity fallback requires instance table and stop semantics |
| `BgmMusic` | `MUSIC` bank or BGM methods | Wwise redirect only until a separate music-source lifecycle is proven |
| `StopEvent` | `STOP_*` | Never map to a file; map to stop/fade behavior |
| `CallbackCritical` | non-null callback or nonzero flags | Prefer Wwise; do not suppress original unless callback behavior is mirrored/proven |
| `UnknownRisk` | unclassified | Log only; fail open |

This table should be an internal GameBridge capability first. Public mods should not receive a blanket "replace any event" promise.

## Discovery Logger Requirements

A future "Audio Discovery" pass should log, at low volume and with throttling:

- `eventName`
- event id/hash when available
- bank name from `soundEventLUT`
- scene/room and active game state
- emitter object name/path when available
- callback presence and flags
- BGM state fields for music events
- first useful caller/context, if affordable
- whether DTMAPI classified the event as simple, BGM, STOP, loop, callback-critical, or unknown

The discovery output should be JSONL or CSV and should not require a specific test mod. It should support both manual play and smoke harness runs.

## Wwise Custom Bank Probe

Before expanding `IAudioReplacementApi`, run a narrow custom-bank probe:

1. Build or obtain a bank containing one new event, such as `DTM_PLAY_RESOURCE_PAPER_BOX`, with embedded media.
2. Reflect available `AkSoundEngine` / `AkUnitySoundEngine` methods in the shipped game runtime:
   - `LoadBank`
   - `LoadBankMemoryCopy` / memory-load variants
   - `AddBasePath` / `SetBasePath`
   - file-package methods, if present
3. Try file-system base path loading first.
4. If path loading fails, try memory bank loading with embedded short media.
5. Redirect only `PLAY_RESOURCE_PAPER_BOX` to the custom event.
6. Keep fail-open semantics until the replacement route returns a playing id or equivalent success evidence.

Known blockers:

- Custom banks must match the game's Wwise runtime version.
- Raw `.wem` is not enough without event metadata in `.bnk`.
- Streamed media paths may not be found unless Wwise IO/base-path/file-package behavior is understood.
- Loading another project's `Init.bnk` risks global bus/state pollution and should be avoided.

## BGM And Loop Boundary

BGM and looped events must not use `SoundPlayer`.

Recommended order:

1. Wwise redirect with custom bank and matching stop/fade/callback behavior.
2. Separate Unity music source only after DTMAPI owns lifecycle:
   - loop
   - fade in/out
   - STOP and STOP_IMMEDIATELY mapping
   - save/title/scene cleanup
   - pause/resume
   - volume/mute behavior
   - callback consequences
3. Log-only until one of those routes is proven.

The current paper-box proof is a short SFX proof. It does not prove BGM, loop, STOP, callback, RTPC, state, or bus behavior.

## Public API Implications

`IAudioReplacementApi` should stay `Experimental`.

Before any broader ordinary-mod promise, the contract needs:

- reviewed event-name allowlist or policy-table gate;
- explicit backend selection/status;
- fail-open result states;
- owner unload cleanup proof;
- duplicate/multi-owner arbitration;
- event category restrictions in developer docs;
- validation that BGM/loop/STOP/callback events are blocked or Wwise-backed;
- logs that distinguish native event heard, replacement event heard, and suppression.

Do not expose raw Wwise, Unity, Harmony, BepInEx, or decompiled Doloc Town types in `DTMAPI.Abstractions`.

## What Not To Do

- Do not build a broad "replace arbitrary local audio by filename" feature over `System.Media.SoundPlayer`.
- Do not suppress original Wwise events before replacement playback is proven.
- Do not treat `AudioClip.name` as Doloc Town's primary audio key while the current verified central route is Wwise event-name based.
- Do not map STOP events to ordinary audio files.
- Do not promote paper-box SFX success to BGM/music support.
- Do not rely on same-name Wwise event collisions as an override mechanism.

## Suggested Next Goal

Create a narrow docs/code goal for "Audio Discovery And Backend Probe":

```text
Generate SoundEvents/soundEventLUT event-map evidence; add an internal discovery logger; classify events into a policy table; run a Wwise custom-bank probe for one paper-box replacement event; keep SoundPlayer as debug fallback only; do not expand public API stability.
```

Acceptance should require Release build/tests, a real tenth-save paper-box smoke, clean exit, and a report showing discovered event context and backend status.

## Related Records

- `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`
- `docs/updates/2026/20260617-0002-audio-replacement-cardboard-api.md`
- `docs/updates/2026/20260617-0003-manbo-audio-upload-metadata.md`
- `docs/api/public-api-matrix.md`
