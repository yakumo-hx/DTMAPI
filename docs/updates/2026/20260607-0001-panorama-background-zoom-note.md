# 20260607-0001 - Panorama Background And Zoom Note

## Summary

Recorded a small review note comparing DolocPlus Panorama Camera behavior with DTMAPI Zoom and preserving the next-goal constraint: add a future panorama/photo API, but only patch Zoom background/depth-fog compensation for the current large-view issue.

## Source Request

User asked to summarize the experience into a small file for later aggregation into a unified upgrade goal.

## Changed Files

- `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260607-0001-panorama-background-zoom-note.md`

## Validation

- Document-only change.
- No build or game smoke run.

## Evidence

- Prior chat/tool review inspected DolocPlus `CameraRescaler`, `PanoramaViewController`, and `BackGroundViewController` metadata/IL from the local third-party archive.
- Prior chat/tool review inspected decompiled Doloc Town `CameraController`, `BackgroundRenderer`, `Dungeon`, and `PhotoState` paths.

## Rollback Notes

Remove this update record and the matching review note if the later unified goal no longer uses the Panorama/Zoom background compensation direction.
