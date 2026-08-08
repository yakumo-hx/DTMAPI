# 20260713-0004 BGM Native Lifecycle Project Boundary Review

Status: recorded / fifth-round decision closed / future project uncommitted
Date: 2026-07-13
Scope: BGM separation from short SFX, native Wwise/music state owners, pre-contract probes, lifecycle/release gates, and future author surface
Related decision docket: `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0004-fourth-round-closure-fifth-decision-docket.md`
Closure Update: `docs/updates/2026/20260713-0005-fifth-round-closure-sixth-decision-docket.md`
Related research: `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`

## Source Request

BGM replacement is a stated future platform item. Short paper-box/animal WAV success does not establish the BGM lifecycle. This review defines whether DTMAPI should promise a BGM schema now or keep it as a separate native-owner project.

This is read-only native-owner/product analysis. It does not add a schema/API, load a bank, play/stop BGM, launch the game, or change runtime/package behavior.

## Native Music State Is A State Machine

The current reviewed build has 177 `WwiseSoundManager.soundEventLUT` entries: 47 MUSIC and 130 SFX, including BGM play events plus `STOP_BGM` and `STOP_BGM_IMMEDIATELY`.

Native responsibilities include:

- environment BGM selection from room/environment, day period, weather, and festival state;
- `PlayBGMInternal` posting with a non-null EndOfEvent callback;
- `isBgmPlaying`, `currentBgmEventName`, override event, timer, and paused state;
- natural-end callback clearing state and scheduling the next track after random idle;
- stop-before-play, ordinary STOP, immediate STOP, pause/resume, override/environment restore;
- room switches, weather, archive load, title/save transitions, volume/mute/bus behavior.

Whether STOP produces the expected callback and how a replacement playing id participates in that state directly affects whether native music advances or stalls.

Managed AkSoundEngine exposes bank/base-path/memory-load/unload, post, playing-id, stop/action APIs, and the native binary contains a Wwise 2023.1.0 string. This is not proof that an author bank built for a guessed version will load or that its callbacks/streamed media are compatible.

## T - BGM Product Boundary

### T0 - no current commitment; independent future Wwise/native-lifecycle project

DTMAPI 0.5.5 adds no `BgmMusic` category, file-format promise, playlist schema, public API, or first-party BGM product. A future proposal must first charter a separate native-owner/QA project, prove the backend and lifecycle, then separately decide whether any first-party product or ordinary-author schema should exist.

Final decision: **T0**. This label emphasizes that future research is not a present product commitment.

### T2 - add BGM to current audio-replacements JSON now

Freeze a `BgmMusic` mapping format before bank, STOP, callback, playing-id, unload, transition, and package ownership are established.

This makes author syntax precede the working backend and is not recommended.

### T3 - expose arbitrary Wwise/local-audio replacement now

Offer a broad public API and let authors handle BGM/loop/3D/callback/state themselves.

Rejected. It exposes unstable native semantics and cannot provide DTMAPI owner cleanup, arbitration, or fail-open safety.

## Frozen Non-Promises Under T0

- current `audio-replacements.json` remains reviewed short SFX only;
- `System.Media.SoundPlayer` never becomes a BGM backend;
- no WAV/OGG/MP3 BGM contract, playlist, crossfade, priority, or scene-map format exists yet;
- authors are not promised that arbitrary `.bnk`/`.wem` packages are safe or supported;
- the first BGM capability is internal/first-party until one real product, full lifecycle, cross-build, and long-run evidence exist.

## Required Probe Sequence

1. **Opt-in discovery:** record event/bank, native source state, callback type/presence, current/override/playing/paused, room/day/weather and STOP/callback counts without ordinary-player polling/stacks.
2. **Custom-bank one-shot probe:** load one self-owned QA bank with a new paper-box event through file/memory paths, never replace/load a foreign `Init.bnk`; prove playing id, callback, unload, and fail-open original playback.
3. **Single BGM internal probe:** preserve native selection/callback; prove natural end exactly once, STOP/immediate STOP, playing-id/fade, transition and bank unload sequencing. A replacement post returning zero must fail open to native BGM without setting false playing state.
4. **Lifecycle matrix:** archive load; room immediate/normal changes; day/weather/festival; override/restore; natural end/random idle; stop/pause/resume; volume/mute; save/title/re-enter; disable/unsubscribe/update; duplicate arbitration; bank failure; clean exit.
5. **Resource/long-run matrix:** owner/generation-count bank, playing-id, callback cookie and map resources; short smoke, repeated save/title, long title-idle-to-load, long gameplay comparison.

Audio hot-path correction and Manbo/API cleanup precede these probes. ISSUE-010 remains open; a clean audio resource ledger or slimmer player path does not identify the Fatal GC cause, and a future BGM release needs separate long-run comparison.
