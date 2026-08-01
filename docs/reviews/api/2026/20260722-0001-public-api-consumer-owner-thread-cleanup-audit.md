# Public API Consumer, Owner, Thread And Cleanup Audit

**Review ID:** `20260722-0001`
**Date:** 2026-07-22
**Status:** recorded — Phase 4 remains open; five-product baseline unaffected
**Scope:** read-only Phase 4 audit of public API classification, consumers, provider identity, thread and stale-owner cleanup contracts; no API removal, implementation, sixth product, G7, Release, game, L0–L5, GC, long test or 0.5.5 publication

## Result

The five admitted products and the corrected `IItemDisplayNameApi` lifecycle are not reopened by this audit. The remaining findings concern broader API governance and must be handled through later bounded API work. Phase 2 consumer scanning and optional Compatibility Host design may proceed, but Phase 4 cannot be called complete.

## P1 — Custom Entities Cannot Remain StableCandidate

`src/DTMAPI.Abstractions/CustomEntities.cs` marks `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi` and `ICustomDroneApi` as `StableCandidate`. The public API matrix repeats that classification.

The machine-readable G1 authority in `tools/release/contracts/batch6-phase0-domain-contract.json` instead records:

- disposition `blocked-retire-or-internalize`;
- decision state `blocked-unresolved`;
- zero real consumers;
- no admitted native runtime owner or host;
- registry-only state with native creation intentionally absent.

Phase 4 requires real ordinary-Mod consumption and stable semantics before `StableCandidate`. These four interfaces therefore need an ABI-preserving reclassification to `Experimental/Frozen` in a separate API Update. They must not be silently removed, and no speculative native implementation may be added to justify the existing label.

This mismatch blocks a Phase 4 completion or CustomEntities/G7 stability claim. It does not block the five-product baseline or Phase 2 consumer scan.

## P1 — AutoHarvest Uses A Diagnostic API As Product Lifecycle State

`testmods/AutoHarvestMod/ModEntry.cs` calls `IInstantSaveDebugApi.GetState()` to decide whether its automatic updater begins in a save. The matrix classifies that API as diagnostic/controlled-QA rather than ordinary gameplay. G1 separately records AutoHarvest as a single-consumer ProductNative candidate.

The current roadmap already treats AutoHarvest only as consumer/API research input. Any future admission must first replace this dependency with an ordinary lifecycle contract or product-owned state. `IInstantSaveDebugApi` must not be stabilized or generalized merely to admit AutoHarvest.

## P1 — Four Frozen APIs Have No Warning-Bearing Source Metadata

The matrix calls these interfaces Deprecated/Frozen:

- `IActionCompletionApi`;
- `IActionSpeedApi`;
- `IItemTooltipApi`;
- `IAnimalViewerApi`.

In `ExperimentalGameBridge.cs` they still carry only `DtmApiStatus.Experimental`; unlike `IFishingAutomationApi`, they have no deprecation Notes and no `[Obsolete(..., false)]` warning. Their retained ABI remains valid, but an effective warning/removal window has not started.

Phase 2 may design the optional host now. It may not claim these APIs are on a completed deprecation clock, and no deletion gate may open, until the interface and related DTO metadata consistently publish a warning while preserving binary compatibility.

## P2 — Attribute Status And Matrix Disposition Use Different Vocabularies

The roadmap uses `Stable`, `StableCandidate`, `Experimental`, `Internal`, `Deprecated` and `Proposed`. `DtmApiStatus` currently defines `Proposed`, `Experimental`, `Verified`, `Stable`, `Disabled` and `StableCandidate`; it has no `Internal`, `Deprecated` or `Diagnostic` values. As a result, diagnostic APIs remain `Experimental` in metadata while the matrix says `Diagnostic`, and frozen APIs require prose or `[Obsolete]` to express disposition.

A later API-governance decision must explicitly choose one of two models:

1. stability and disposition are separate dimensions, with `DtmApiStatus` retaining only stability and a second supported metadata channel expressing diagnostic/deprecated/internal state; or
2. one expanded enum is the canonical vocabulary and all Author SDK/Doctor/matrix projections consume it.

Do not add isolated enum values until that ownership decision is made; otherwise existing consumers will receive another partially projected classification.

## P2 — `IDtmHelper` Subservices Have Uneven Owner And Thread Boundaries

Core creates owner-bound facades for Events, Config, ModRegistry and Input. Workshop, UI, Diagnostics and Content are passed as shared service instances, while Translation is created per Mod. The shared UI service is mutable without an explicit runtime-thread guard; Workshop owns mutable collections without a documented thread-safe contract. Code holding an old helper can still invoke these shared objects after owner deactivation.

The five admitted products currently call them on the Runtime thread and this audit found no concrete retained resource leak. The issue is therefore not a five-product blocker. Phase 4 must nevertheless classify every helper property as one of:

- Runtime-thread-only and stale-owner-invalid;
- thread-safe read-only/shared;
- owner-bound mutable with cleanup;
- process service deliberately valid after product deactivation.

Only then should a thin owner facade or runtime-thread check be added for the services that require it. Do not wrap immutable/read-only services merely for symmetry.

## P2 — Custom Entities Has Two Provider IDs For One Instance

Core registers the Custom Entities service under `DTMAPI`. GameBridge registers the same instance again under `DTMAPI.GameBridge.DolocTown`. Current tests and fixtures request the Core provider.

Before retirement/internalization, determine whether the GameBridge registration is a retained provider alias for an old binary. It cannot be removed silently, but the duplicate registration is not a second real consumer and must not be used to justify `StableCandidate`.

## P2 — Legacy EnvironmentReset Declarations Need Per-Domain Review

The current native `SetEnvCamera` callback is demand-gated for Camera and ItemDisplayName only. Several frozen compatibility features still declare `EnvironmentResetSensitive=true` and implement meaningful reset methods, including ActionSpeed, AnimalViewer and FishingAutomation.

This does not invalidate the five ProductNative runtime proofs. It is a Phase 2 host input: for each frozen backend, either establish the exact native reset owner and an independent demand route, or remove a stale sensitivity declaration once consumer evidence and behavior compatibility permit. The former broad feature-fanout documentation must not be treated as current production behavior.

## Deferred Single-Consumer Domains

The G1 contract already records Zoom, AutoHarvest, MoreSaves, EquipmentSlots, Chest, StrongPlanting and Audio as zero- or one-real-consumer domains. That evidence does not establish another SharedNative promotion. Their existing dispositions remain authoritative; this Review admits no sixth product and adds no public surface.

## Recommended Order

1. Preserve the five-product/ItemDisplayName closeout under Update `20260722-0004`.
2. Continue Phase 2 exact consumer and optional-host work under its own Review.
3. Open one bounded API Update to reclassify Custom Entities without ABI removal and add warning-bearing metadata to the four frozen APIs.
4. Decide the stability-versus-disposition metadata model before changing the enum or SDK projection.
5. Audit helper properties one at a time, adding stale-owner/thread enforcement only where mutable behavior requires it.
6. Resolve the duplicate Custom Entities provider as an explicit compatibility alias question.

Focused validation is sufficient for those later slices: Abstractions build, public ABI/metadata tests, consumer scan, stale-owner/off-thread Units, provider-identity Unit and documentation governance. No game, complete Release, L0–L5, GC or long test is implied.

## Disposition

**GO** for Phase 2 consumer/host design and later bounded API-governance corrections.
**NO-GO** for calling Phase 4 complete, promoting Custom Entities/G7, stabilizing a Diagnostic API for AutoHarvest, or deleting any frozen ABI.
**UNCHANGED:** five admitted ProductNative products, `IItemDisplayNameApi` Experimental status, sixth-product block and 0.5.5 release block.

Related authorities:

- `docs/api/public-api-matrix.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md` Phase 4
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `docs/reviews/api/2026/20260713-0001-customentity-public-promise-review.md`
- `docs/reviews/code/2026/20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md`
