# 20260613-0015 SMAPI Ecosystem Semantic API Map

Date: 2026-06-13
Status: recorded
Area: docs/api/smapi-ecosystem-map

## Summary

Added a docs-only SMAPI ecosystem semantic API research library. The library uses the user's pasted summary of ten mature SMAPI C# mods as clean-room semantic input, then records a four-round subagent review workflow:

1. semantic API extraction;
2. review questions and confidence scoring;
3. revised API map blocks;
4. final review, downgrades, and priority roadmap.

The result is an ecosystem research map for future DTMAPI API planning. It does not add runtime code, does not add or promote public APIs, and does not claim SMAPI or Content Patcher compatibility.

## Changed Files

- `PROJECT.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/SOURCE-INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/semantic-api-map.md`
- `docs/reviews/api/smapi-ecosystem-map/dtmapi-gap-map.md`
- `docs/reviews/api/smapi-ecosystem-map/priority-roadmap.md`
- `docs/reviews/api/smapi-ecosystem-map/rounds/ROUND-1-semantic-api-extraction.md`
- `docs/reviews/api/smapi-ecosystem-map/rounds/ROUND-2-review-confidence.md`
- `docs/reviews/api/smapi-ecosystem-map/rounds/ROUND-3-revised-api-map.md`
- `docs/reviews/api/smapi-ecosystem-map/rounds/ROUND-4-final-review-confidence.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260613-0015-smapi-ecosystem-semantic-api-map.md`

## Source Request

User asked to extend the semantic-API research based on a pasted report summarizing ten SMAPI C# mods and their ecosystem-level capabilities.

## Key Results

- Core-owned ecosystem surfaces are the closest fit: config, mod registry, translation, manifest/helper, and some lifecycle integration.
- P0 research priority does not mean Stable API readiness. `UpdateTicked`, save/title lifecycle, input/keybind, and config menu shell still need evidence before promotion.
- UI host surfaces such as HUD, menu overlay, tooltip, and debug layers are valuable but remain `Proposed`, `Experimental`, or `Diagnostic`.
- Read-only/query layers should precede mutation layers almost everywhere.
- Content pipeline work should start from read-only index and content-pack shell, not full Content Patcher compatibility or asset interception.
- Containers, inventory transactions, machines, map/target inspection, and world scans need Doloc native-owner reviews before runtime API rebuilds.
- Runtime world entities, custom vehicles, runtime building expansion, map boundary mutation, and multiplayer remain `Blocked` or `Future-reserved`.

## Validation

Validation is limited to docs checks. No runtime code changed.

- Confirmed the 9 new `smapi-ecosystem-map` files exist.
- Confirmed required docs link `smapi-ecosystem-map/INDEX.md`.
- Confirmed this update record is indexed in `docs/updates/INDEX.md`.
- `git diff --check` passed. It printed existing LF/CRLF normalization warnings only.

Build, unit tests, public API matrix updates, and game smoke were not run because this is a docs-only research/library update.

## Evidence Links

- `docs/reviews/api/smapi-ecosystem-map/INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/semantic-api-map.md`
- `docs/reviews/api/smapi-ecosystem-map/priority-roadmap.md`
- `docs/reviews/api/smapi-ecosystem-map/rounds/ROUND-4-final-review-confidence.md`

## Rollback

Remove `docs/reviews/api/smapi-ecosystem-map/`, remove its links from `PROJECT.md`, `docs/reviews/README.md`, `docs/workflows/codex-api-rebuild.md`, `docs/reviews/api/native-owner-domains/INDEX.md`, and `docs/reviews/api/local-mods-native-owner/INDEX.md`, then remove this update record and its index row.

## Follow-up

- Do not update `docs/api/public-api-matrix.md` from this research alone.
- Future implementation goals must start from one narrow API group, then perform native-owner review, GameBridge design, public DTO design, real mod usage, and third-save validation where runtime state is touched.
