# SMAPI Ecosystem Semantic API Map

Status: active, docs-only
Created: 2026-06-13
Scope: clean-room semantic study from the user's SMAPI mod summary

This directory records the four-round review that maps mature Stardew Valley SMAPI mod ecosystem semantics to DTMAPI candidate API concepts.

This is not a compatibility promise with SMAPI or Content Patcher. It is not a public API stability record. It does not add or promote DTMAPI APIs. It is a research map for future DTMAPI API design, native-owner review, and GameBridge planning.

## Rules

- Treat SMAPI and Stardew Valley mods as architecture and ecosystem references only.
- Do not copy SMAPI, Content Patcher, or third-party Stardew mod implementation details into DTMAPI.
- Use clean-room DTMAPI concepts and Doloc Town native-owner review before any implementation.
- Separate research priority from stable API readiness.
- Prefer read-only/query/overlay layers before mutation, automation, or runtime creation.
- Keep DTOs and adapters at public boundaries. Do not expose raw Doloc Town decompiled or Unity types.

## Index

- [Source Index](SOURCE-INDEX.md)
- [Semantic API Map](semantic-api-map.md)
- [DTMAPI Gap Map](dtmapi-gap-map.md)
- [Priority Roadmap](priority-roadmap.md)

## Round Reports

| Round | Purpose | Report |
| --- | --- | --- |
| Round 1 | Parallel semantic extraction from ten SMAPI mod summaries into candidate DTMAPI APIs. | [Round 1 Semantic API Extraction](rounds/ROUND-1-semantic-api-extraction.md) |
| Round 2 | Parallel review, overclaim challenges, and confidence scoring. | [Round 2 Review Confidence](rounds/ROUND-2-review-confidence.md) |
| Round 3 | Revised API map blocks with downgrades and promotion gates. | [Round 3 Revised API Map](rounds/ROUND-3-revised-api-map.md) |
| Round 4 | Final review, confidence summary, file structure, and must-not-overclaim list. | [Round 4 Final Review Confidence](rounds/ROUND-4-final-review-confidence.md) |

## Final Conclusion

The review found a useful DTMAPI ecosystem route:

```text
Core ecosystem surfaces
  -> UI host and overlay surfaces
  -> read-only GameBridge query surfaces
  -> high-risk transaction and automation surfaces
  -> blocked runtime creation surfaces
```

Only a small Core-owned surface is near stable or StableCandidate. Most UI, content, world, container, machine, map, entity, and vehicle concepts remain `Experimental`, `Proposed`, `Diagnostic`, `Blocked`, or `Future-reserved`.

## Final Status Overview

| Group | Final status | Notes |
| --- | --- | --- |
| Mod entry, manifest, helper, config read/write | `Stable` / `StableCandidate` | Core-owned and already closest to current DTMAPI strengths. |
| `GameLaunched`, registry/API exchange, translation | `StableCandidate` | Still needs load-order, identity, and regression evidence before full stability. |
| `UpdateTicked`, save/title events, input/keybind | `Experimental` | Foundational, but not native Doloc simulation/input ownership. |
| Config menu registration protocol | `StableCandidate` | Runtime UI, save/cancel, paging, and live preview remain separate. |
| HUD, menu, tooltip, overlay, debug layers | `Proposed` / `Experimental` / `Diagnostic` | UI host lifecycle and manual visual QA are required. |
| Content pack discovery and read-only content index | `Proposed` / `Experimental` | Asset editing, tokens, and patch actions are later-stage. |
| Target inspection, info query, map marker, world scan | read-only `Experimental` or `Proposed` | DTO-only and native-owner-gated. |
| Container query, inventory transaction, machine query | `Experimental`, with transaction high risk | Query before mutation; mutation before automation. |
| Automation networks | later `Proposed` | Requires mature world, container, inventory, and machine primitives. |
| Runtime world entities, custom vehicles, building expansion | `Blocked` | No stable native lifecycle owner is proven. |
| Multiplayer messages | `Future-reserved` | No implementation promise until Doloc networking exists and is reviewed. |
| Debug/cheat mutation | `Diagnostic-only` | Useful for development, not ordinary gameplay API stability. |

## Next Use Pattern

When turning this map into a future API rebuild goal:

1. Pick one narrow API group from [Priority Roadmap](priority-roadmap.md).
2. Check `docs/api/public-api-matrix.md` for current truth.
3. Check `docs/reviews/api/native-owner-domains/INDEX.md` and local native-owner records.
4. Write a native-owner review before runtime/GameBridge work.
5. Only update public API status after implementation evidence, real mod usage, third-save validation, and debug regression evidence.
