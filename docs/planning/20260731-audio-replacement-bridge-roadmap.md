# Audio Replacement Bridge Simplification Roadmap

Date: `2026-07-31`

## Current Release Boundary

DTMAPI `0.5.5` keeps the existing Wwise event owner and public Experimental audio API. The only authorized closeout is an idempotent physical-status publication guard: a stable Hook review must not erase behavioral `verified` evidence or manufacture a duplicate-install warning.

No scheduler redesign, public API stabilization, new audio domain, or backend expansion belongs to that correction.

## Next-Version Target

The unified audio bridge should establish one simple authority chain:

```text
feature demand
  -> structured physical install result
  -> stable physical Hook state
  -> replacement attempt/result evidence
  -> independent behavioral verification state
```

The required decisions and work are:

1. Replace English-string inference with a typed result such as `NewlyInstalled`, `AlreadyInstalled`, or `Failed`. A review that returns `AlreadyInstalled` is not a new install signal.
2. Separate physical Hook readiness from behavioral verification. Idempotent install review may update the physical layer but cannot downgrade a successful replacement observation.
3. Define scheduler semantics explicitly. At-least-once global review is acceptable only when every installer and publisher is transition-based; targeted demand batches may be considered if they materially simplify ownership without losing late-assembly retry.
4. Consolidate the reviewed event policy around Wwise event names and explicit owner rules. Prefer a custom-bank/native redirect when proven, use Unity `AudioSource` only as a bounded fallback, and retain `SoundPlayer` solely as a diagnostic fallback.
5. Keep reload, error and owner diagnostics bounded. A backend failure must identify the owning Mod and exact event while failing open to native audio.
6. Add cross-feature tests in which a late unrelated demand reviews audio after a verified playback, plus unavailable-to-available retries and owner cleanup.

## Non-Goals Without New Evidence

- Do not stabilize `IAudioReplacementApi`; it remains Experimental.
- Do not add BGM, loop, STOP-event, callback or broad bank ownership.
- Do not claim hot unload or automatic third-party Hook rollback.
- Do not make Full diagnostics necessary to discover an Error.

## Evidence Gates

- native-owner review for every newly admitted event class;
- structured-result Unit coverage independent of message wording;
- one combined-feature runtime case proving late demand cannot downgrade verified audio;
- owner cleanup and failure-isolation evidence;
- public API matrix and Hook map updates only when those owned facts actually change.

## Sources

- [Manual-QA Review 20260731-0002](../reviews/manual-qa/2026/20260731-0002-audio-hook-idempotent-status-republish.md)
- [Audio event-map/backend research](../reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md)
- [Public API matrix](../api/public-api-matrix.md)
