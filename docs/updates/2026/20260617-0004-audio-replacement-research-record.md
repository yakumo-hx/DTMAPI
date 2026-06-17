# 20260617-0004 Audio Replacement Research Record

## Status

recorded-docs-only

## Source Request

The user provided two external research notes comparing Stardew Valley, Don't Starve/DST, and Doloc Town audio replacement approaches, then asked to record the research content in repository files.

## Changed Files

- `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260617-0004-audio-replacement-research-record.md`

## Summary

Recorded the long-term audio replacement direction:

- Future audio replacement should be keyed by stable Wwise `eventName` / `SoundEvents` identifiers, not by one gameplay native responsibility function per sound.
- Current paper-box proof remains a narrow short-SFX slice only.
- Recommended backend order is Wwise custom-bank redirect first, Unity `AudioSource` plus DTMAPI-owned local decode second, and `System.Media.SoundPlayer` as debug/probe fallback only.
- Future work should generate an event policy table from `SoundEvents` and `soundEventLUT`, then refine it with runtime discovery logs.
- BGM, loop, STOP, callback, RTPC/state/bus/mixer events remain blocked from broad replacement until Wwise-native or lifecycle-complete backend evidence exists.

## Validation

- Docs-only change.
- No build, unit tests, or game smoke were run because no runtime, API code, package, hook, or upload folder changed.
- Existing verified paper-box smoke remains `docs/debug/evidence/GAME-SMOKE/20260617-065447`.

## Related Records

- API review: `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`
- Previous native-owner review: `docs/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md`
- Previous implementation/update: `docs/updates/2026/20260617-0002-audio-replacement-cardboard-api.md`
- Publish metadata update: `docs/updates/2026/20260617-0003-manbo-audio-upload-metadata.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback

Remove the new API research record and the corresponding update-index/API-matrix notes. No runtime or packaged files are affected.

## Follow-up

If the audio API continues, create a narrow goal for an Audio Discovery and Wwise custom-bank probe. Keep `IAudioReplacementApi` Experimental until backend, lifecycle, multi-owner, and event-policy evidence is stronger.
