# 20260607-0005 Native Responsibility API Audit

- Date: 2026-06-07
- Status: implemented
- Area: docs/reviews/api
- Source request: `/goal 执行DTMAPI底层api审查。`

## Summary

Added a durable code-level review of current DTMAPI low-level/public APIs against Doloc Town native responsibility functions. The audit rates each API family by whether it reaches the native state owner, whether success could be only UI/log/hook/smoke-visible, and what evidence is still missing before ordinary mods should rely on the surface.

## Changed Files

- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/updates/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/updates/INDEX.md`

## Validation

- Documentation-only review.
- No runtime code was changed.
- No build was run.
- No Doloc Town game smoke was run.

## Evidence

- Reviewed `docs/api/public-api-matrix.md`.
- Reviewed Core/API entry points under `src/DTMAPI.Abstractions`, `src/DTMAPI.Core`, `src/DTMAPI.GameBridge.DolocTown`, `src/DTMAPI.BepInExBootstrap`, and `src/DTMAPI.ModConfigMenu`.
- Cross-checked native responsibility candidates against `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps`.
- Cross-checked recent DolocPlus research notes under `references/doloc-town/research-notes`.

## Related Records

- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`
- `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md`
- `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`
- `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`

## Rollback

Revert this review file and remove this index entry. Runtime behavior is unaffected.

## Follow-up

No implementation goal was created. Future implementation should choose one high-risk API family, read the audit first, and create a dedicated goal file plus `.goal.txt` only if explicitly requested.
