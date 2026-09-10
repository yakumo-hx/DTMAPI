# Codex API Rebuild Workflow

Use when a public/shared API contract or its native ownership needs redesign. An admitted ProductNative bug fix with an established boundary uses [product validation](product-change-validation.md), not a whole API rebuild.

## Required context

After PROJECT/current-state, read only:
1. the affected [public API rows](../api/public-api-matrix.md) and current declarations/implementation;
2. the latest relevant Review/Update and focused [Hook map](../hook-map/README.md);
3. matching native methods/state holders; first follow references/README.md for reference material.

Use the [native-owner library](../reviews/api/native-owner-domains/INDEX.md) when ownership is unresolved, the [local Mod map](../archive/reviews/api/2026/local-mods-native-owner/INDEX.md) for shared real-consumer questions, and the [ecosystem map](../archive/reviews/api/2026/smapi-ecosystem-map/INDEX.md) only for ecosystem semantics. Historical batch audits/annexes are reference evidence, not a mandatory reading chain. Platform-next tasks follow their accepted task/architecture and specified review node.

## One bounded decision

Identify the native responsibility function/state holder, then apply PROJECT's Platform/SharedNative/ProductNative/ContentOwner classification. Method visibility, a facade or prospective reuse does not prove SharedNative. Use [generated admission](../architecture/managed-product-admission-registry.md) for Advanced products; SDK160 and generated reference/owner/package gates remain in force.

Reuse the existing Review unless the owner, contract, root cause or evidence changed. When one is needed, record:
- symbol/domain and current status;
- real native effect and authoritative state;
- physical owner and real consumers;
- owner/thread/exception/restore behavior, save and transition risks;
- acceptance evidence and precise blockers.

Before implementation preserve this boundary: find the native owner/state holder first; a Mod patch or UI/registry success cannot stand in for an API rebuild.

## Implement and validate

Public contracts expose DTMAPI DTOs/results, never raw Unity/Harmony/BepInEx/game types. Classify gameplay state under PROJECT; do not turn a sidecar into implicit autosave. Integrate affected SDK/docs, failure semantics and old consumers with the contract change.

Run focused source/unit/ABI checks after a changed slice. Real native/Mono claims need the corresponding game behavior, owner-state evidence and clean exit; save/transition/disable/multi-consumer checks trigger only for affected promises. [Product validation](product-change-validation.md) owns preparation, save modes and stop rules.

A complete Release suite or long-run matrix runs only at the named migration/integration/release boundary or because affected shared behavior requires it. Preserve unrelated valid evidence; a failure gets focused repair before broader rerun.

## Status and records

API statuses remain `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, `blocked-rebuild`. Promote only the promise actually proven by ordinary consumers, not a QA helper. Missing native owner means a bounded blocker, not a speculative patch.

One Update owns scope, files, validation, rollback and follow-up. Update API/Hook/Issue/smoke facts only when changed and link the Update. Keep candidate admission hidden/blocked until its required SDK, lifecycle, compatibility and package checks pass; small reversible implementation steps need no new receipt family or repeated architecture review.
