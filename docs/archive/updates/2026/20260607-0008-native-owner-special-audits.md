# 20260607-0008 - Native Owner Special Audits

Status: implemented
Date: 2026-06-07
Area: docs/reviews/api

## Summary

Added a focused code-level review for the four highest-risk API domains identified by the 0007 native-responsibility review:

- `IInputHelper.Suppress`
- `ICameraZoomApi`
- `IMachineProductionApi`
- CustomEntity runtime verbs

This update is docs-only. It does not change runtime code, public API contracts, mods, game files, Workshop files, or official/decompiled sources, and it does not create an implementation goal.

## Source Request

User requested a `/goal` to verify that Codex can do serious per-function review rather than generating broad template conclusions. The requested completion standard was that each specialty must answer where the API connects, where it does not connect, why ordinary mods cannot safely depend on it, and how the next rebuild should be split.

## Changed Files

- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/01-input-helper-suppress.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/03-machine-production.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md`
- `docs/updates/2026/20260607-0008-native-owner-special-audits.md`
- `docs/updates/INDEX.md`

## Review Evidence

- Public API matrix rows for Input, MachineProduction, CameraZoom, and CustomEntity APIs.
- 0007 native-responsibility index, high-risk runtime bridge volume, and completion audit.
- Core, Bootstrap, GameBridge, test, and sample mod code paths cited by path and line.
- Existing debug, hook-map, smoke-matrix, and update records for Y-console input isolation, Zoom, Mine/Machine production, and CustomEntity blocked runtime results.
- DolocPlus research notes and reverse-build candidate file searches for camera/background/fog owners.
- Reverse-build candidate searches for animal, monster, attack/projectile, drone, machine, input, and camera native owners. No decompiled source was copied.

## Validation

- No game smoke was run, per goal constraint.
- Runtime/API/mod/game/Workshop files were not modified.
- Validation run after editing: `git diff --check`.

## Rollback

Remove the new 0008 review index, the `20260607-0008-native-owner-special-audits/` directory, this update record, and the `20260607-0008` row from `docs/updates/INDEX.md`.

## Follow-up

No implementation goal was created. Future work should be requested explicitly and should choose one native-owner rebuild at a time: input action suppression, camera/background/fog zoom, machine native production, or custom entity runtime adapters.
