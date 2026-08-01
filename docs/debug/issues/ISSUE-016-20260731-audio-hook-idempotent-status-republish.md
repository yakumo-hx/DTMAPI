# ISSUE-016: Audio Hook Idempotent Review Republishes and Downgrades Status

State: `mitigated`

## Symptom

With Manbo audio replacement active, a later demand such as Animal Bell Info's first localized item-name query produces one Manager warning:

```text
Hook install signal repeated for the same hook id
```

The warning appears even though audio playback succeeded and no second Harmony patch was installed. The visible Hook status also regresses from `verified` to `experimental`.

## Reproduction

1. Start DTMAPI `0.5.5` with Manbo audio replacement and Animal Bell Info enabled.
2. Trigger a Manbo paper-box sound replacement and observe `played=True`, `suppressed=True`, and `Audio.SoundEventReplacement=verified`.
3. Open the animal information path that first queries a localized product name.
4. `ItemDisplayName.EnvironmentReset` demand causes a global Hook install review.
5. Observe the audio status return to `experimental` and the single lifecycle warning.

## Evidence and Root Cause

The exact retained sequence and hashes are recorded by [Manual-QA Review 20260731-0002](../../reviews/manual-qa/2026/20260731-0002-audio-hook-idempotent-status-republish.md).

`AudioReplacementHookBridge.InstallHooks()` guards all four physical patches but unconditionally republishes status after every review. The lifecycle contract then uses install-looking English source/details to count that publication as another installation. This is a state-publication defect plus a later diagnostic-model debt, not an audio playback failure.

## Rejected Hypotheses

- No functional dependency exists between Animal Bell Info and Manbo audio.
- The Wwise Harmony patch was not installed twice.
- The warning is not evidence that the replacement failed or that every title return repeats the failure.

## Current Correction Boundary

The `0.5.5` correction is intentionally minimal: cache the last published physical Hook tuple and republish only on the initial observation or a real physical-state transition. This preserves retries and prevents a stable physical review from erasing behavioral verification.

The scheduler's at-least-once fanout, structured install outcomes, physical-versus-behavioral status layers, and the general lifecycle heuristic are deferred to the next-version [audio bridge roadmap](../../planning/20260731-audio-replacement-bridge-roadmap.md).

## 2026-07-31 Source Correction

`AudioReplacementHookBridge` now retains the last published four-flag physical snapshot. Initial state and real transitions still publish; an unchanged review returns before `SetHookInstalled` and `SetHookStatus`. Focused and complete Unit tests, build, Catalog, document and artifact-governance checks pass. The issue remains `mitigated` until the player reruns the delayed-demand sequence on the corrected Runtime.

## Acceptance Criteria

1. Focused Unit coverage proves unchanged physical review retains `verified` and creates no repeated-install diagnostic.
2. Focused Unit coverage proves an initially unavailable target may later publish the successful physical transition.
3. Source/governance checks pass.
4. A later player rerun of Manbo plus delayed ItemDisplayName demand records zero duplicate audio-install warning and no behavioral regression.

Criteria 1-3 may move the issue to `mitigated`. Criterion 4 is required for `verified`.

## Related Records

- `docs/updates/2026/20260731-0005-audio-hook-idempotent-status-and-all-mod-profile.md`
- `docs/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README-history-through-20260711.md`
