# 20260715-0007 Decision Escalation Policy And Product Nodes

Status: recorded
Date: 2026-07-15
Scope: reclassify the Batch 3 SDK option set as evidence-gated engineering defaults and define when later work must return to the user as a real product decision
Related Update: `docs/updates/2026/20260715-0015-decision-escalation-policy-and-product-nodes.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Upstream review: `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`
Detailed future nodes: `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`

## Source And Verdict

The user supplied a subtask return which proposed treating SDK A-I as pseudo-decisions: use the engineeringly stronger recommendation by default, record its reason and validation gate, and reopen discussion only if evidence invalidates the assumption. The same return separated later player-visible, economic, identity, save, author-format, API, compatibility, ownership and publication commitments into real decision nodes.

That model is accepted with two qualifications:

1. a pseudo-decision is a provisional implementation default, not an already-proven fact;
2. the decision timetable below identifies likely escalation moments only and does not replace or reorder the canonical Batch 0-8 implementation program.

No new global decision round is required before Batch 3. Other work may continue against the defaults and gates in this record.

## Decision Escalation Rule

Return an issue to the user as a real decision when at least one of these conditions is true:

- it changes player-visible behavior, a default, or game economy;
- it freezes a public ID, save shape, config shape, or author-facing format;
- at least two materially different product directions remain reasonable;
- it creates or widens a long-lived public API promise;
- it changes compatibility scope, file ownership, or publication commitments;
- the selected path has material migration or rollback cost.

Internal host choice, implementation slicing, deterministic hashing, safe-failure mechanics and similar construction details normally remain engineering decisions. Their owning Review or Update must still record:

- the assumption and selected default;
- why it is preferred;
- the evidence/acceptance gate;
- the rollback path;
- the exact failure condition that would escalate it into a user decision.

## Batch 3 Engineering Defaults

The prior `SDK-A1/SDK-B1/SDK-C1/SDK-D1/SDK-E1/SDK-F1/SDK-G1/SDK-H1/SDK-I1` recommendation is now the default Batch 3 baseline rather than a pending ballot.

| Item | Default classification | Required proof or reversal trigger |
| --- | --- | --- |
| SDK-A1 self-contained .NET 8 host | Provisional engineering choice | A clean Windows x64 author environment can run the portable SDK without a separately installed .NET host. Generated game-loaded assemblies remain `netstandard2.0`. Host failure or a required unsupported platform reopens the host design. |
| SDK-B1 target only Runtime 0.5.5 | Existing route consequence | SDK 0.1.x generates only the 0.5.5 baseline; Doctor may inspect older Mods. Evidence that an actively supported author workflow requires dual templates would reopen it. |
| SDK-C1 sliced implementation, one complete preview | Implementation order | Build 3.1 templates/validation/package/Doctor, 3.2 source modes and receipt transactions, then 3.3 explicit reload. Do not publish a partially authoritative workflow as the supported preview. |
| SDK-D1 bundled fixed Abstractions reference | Reproducible-build requirement | Prove reference version/hash parity and offline deterministic builds without reading the player's mutable game install. |
| SDK-E1 Runtime `manifest.json` authority | Provisional SDK 0.1.x author shape | Runtime identity/version/minimum/dependencies remain manifest-owned. SDK-only build/publish data is explicitly unstable before SDK 1.0 and must have an upgrade path; real author use determines whether that shape is frozen. |
| SDK-F1 no force/adopt | File-ownership hard boundary | New deployment or a matching receipt plus matching external state only. Unknown drift refuses and preserves recovery material. Any future adopt flow requires a separate ownership/recovery review. |
| SDK-G1 Doctor is read-only | Permission hard boundary | Human and machine-readable diagnosis must never move, delete, load, execute, adopt, enable or disable unknown packages/DLLs. Mutations stay in separately named receipt-bound commands. |
| SDK-H1 package-external source overrides | Provisional implementation | Key overrides by game installation plus `UniqueID`; prove multiple game roots, clear/restore, restart persistence and Player Reproduction snapshots. The SDK 0.1.x persistence shape remains internally migratable and override state is not a file-ownership receipt. |
| SDK-I1 no Workshop upload | Batch 3 scope boundary | Provide deterministic pack/validate/hash/report and validate already-downloaded Workshop trees only. Account credentials, item creation and upload remain a separate future project. |

## Batch 3 Completion Gates

Batch 3 is complete only when evidence proves:

- a minimal CodeMod can be created and built, both CodeMod and ContentPack scaffolds validate, and both produce byte-deterministic packages;
- no ordinary Mod is installed under `BepInEx/plugins`;
- Doctor remains metadata-only and read-only, including for unknown DLLs;
- receipt operations refuse mismatched external state and preserve recovery material;
- Player/Workshop, Local Development, Workshop Validation and Player Reproduction source modes select and expose the correct selected/shadowed state;
- an explicitly reload-capable DTMAPI content format rebuilds transactionally per `UniqueID` and retains the last successful generation on invalid input;
- formats which cannot be safely reloaded, including any official-native JSON lane not yet proven reloadable, report restart-required instead of pretending to hot reload;
- CodeMod DLLs are never hot reloaded.

These are validation gates, not preference questions.

## Real Decision Nodes

The following work should return to the user only after its prerequisite evidence exists and only for choices which meet the escalation rule.

| Route node | Real decision surface | Evidence required before asking |
| --- | --- | --- |
| ActionSpeed 1.0.0 | Supported action domains, defaults, maxima, continuous-use limits, arbitration, native restoration and old-config migration if existing behavior cannot be preserved as-is. | Independent 1x/enabled-no-speed/common/high/disable/title-cycle allocation and throughput evidence for AutoFishing and ActionSpeed; native owner per action class. Do not assume both systems own the same Animator. |
| Mine 1.0.0 | Output economy, cycle, power/fuel policy, capacity/batch/offline catch-up, Oil relationship, final IDs/assets/placement and inventory handling on update/disable/removal. | Official JSON coverage, native machine/preview/storage/power behavior and migration evidence. Static content stays official-JSON-led; Mine owns its economy. |
| MoreEquipmentSlots 1.0.0 | Slot count/purpose, effect stacking, UI transfer, save representation, disable/downgrade behavior and orphan recovery. | Native slot/effect/save owners plus destructive-transition recovery proof. Orphan recovery remains mandatory safety work and cannot be demand-disabled. |
| Manbo after Canary use | Retain as a small ContentPack, merge through an explicit identity-preserving migration, or retire with notice. A merge may not silently erase the frozen Workshop/`UniqueID`. | Real author migration, subscription and usage evidence from its JSON/WAV Canary role. |
| Public API governance | Facade/migration/retention for a real external consumer; a new first-party capability promoted to public; multi-Mod arbitration; a new lifecycle/save/thread/DTO promise; or an explicit breaking version. | Native-owner proof and externally evidenced demand. Ordinary retirements follow the already selected I1 process without per-symbol voting. |
| Manager player UX | Player home-page information, status wording, advanced-detail depth, row/paging/scroll/search/filter layout, confirmation policy and update notices. | Stable product/state model and representative player flows. Structural work may proceed without freezing these UX choices. |
| MoreSaves expansion | Display-name identity, duplicates, persistence/rollback, limits, ordering, paging/scrolling and access after removal. | Fixed-12 baseline separated and save migration/recovery paths proven. |
| Y-console redesign | History/search/completion, dangerous-command confirmation, player/developer separation, focus/pause/close, copy/export/error location and public-vs-first-party command ownership. | Behavior-equivalent extraction from Bootstrap and an optional diagnostic host first. |
| AnimalPack 1.0.0 | Species roles, ordinary/rare/hidden products, processor/electricity/cycle/capacity/unlock, probabilities/quantities/prices/payback, public IDs/assets, Oil relationship and old-pack migration/conflicts. | Canonical source/provenance plus official dismantle/LUT/multi-result/hidden-product/batch/save/disable/update and pre-native duplicate evidence described by the detailed AnimalPack node. |
| Short-SFX catalog/backend expansion | Additional reviewed categories, replacement priority, load/cache/release/reload, backend/format expansion and failure fallback. Q1/R1/S1 remain frozen: JSON `SoundKey -> WAV` is the author route and the old C# registration API follows I1 retirement; a new public C# API is considered only if real external demand triggers a separate API Review. | Native playback/lifecycle proof across more than isolated successful test functions. BGM remains the independent T0 project. |
| Author SDK 1.0 | Stable CLI, author-format compatibility, template migration, receipt commitment, NuGet mirror, adopt, upload project and supported platforms/architectures. | Real SDK 0.1.x author usage and migration evidence. |

## Conditional Escalation Only

- **GC:** AutoFishing and ActionSpeed remain independent measurements. Escalate only if the remedy must reduce a player-selectable maximum, change a default, remove an action domain, or change long-session/title-cycle behavior. Allocation, callback, caching and lifecycle repairs remain engineering work.
- **0.5.5 compatibility:** Escalate only if Unity Mono cannot retain the published ABI, public identities cannot coexist, a real external consumer requires behavior planned for retirement, or safe update/recovery cannot fit the frozen ownership boundary.
- **QA and hot paths:** QA extraction, removal of ordinary-player polling and demand activation remain engineering work unless they require a new public API or demonstrably change player behavior.

## Items Which No Longer Need A Separate Vote

- the Lamp retained-ABI compatibility shell: restore the published shape and historical defaults, expose a non-null owner-bound fail-closed facade, mark it non-error obsolete/retired-disabled, install no Lamp Hook or recurring work, and leave any real Lamp rebuild to a later native-owner project;
- the Batch 3 SDK A1-I1 defaults above;
- QA extraction, read-only Doctor, the ordinary-Mod/BepInEx boundary, lifecycle-driven zero-player-polling and the 0.5.5 zero-old-ABI-deletion rule;
- Oil/OneAction decoupling and official-JSON-led Oil/Mine static content;
- AutoHarvest retirement as a process/API sample;
- behavior-equivalent OneAction, AutoFishing, Zoom and other low-user boundary splits, provided identity, defaults, config, protected behavior and rollback remain unchanged.

If implementation evidence would violate any qualifier above, the work stops and uses the real-decision rule rather than silently widening scope.

## Route Interpretation And Handoff

The canonical implementation order remains Batch 2 closure, Batch 3 Author SDK, Batch 4 QA extraction, Batch 5 hot-path/demand activation, the 0.5.5 release gate, then Batch 6-8. This sequencing does not turn the Author SDK into an additional standalone 0.5.5 publication gate beyond the release criteria already named by the full boundary audit. This record does not move AnimalPack into current work or omit the other Batch 6 product migrations.

The nearest likely real product decisions are ActionSpeed 1.0.0 or Mine 1.0.0 after their facts are available. AnimalPack remains the largest later concentrated decision round. Until then, implementers should execute the frozen route, record assumptions and validation gates, and escalate only when evidence crosses the rule above.

## Validation Boundary

This classification was cross-checked against the full boundary audit, the current progress/decision-node review, the Batch 2 closure/Batch 3 predecision review and the frozen first-through-sixth decision records. It records project direction only. No Runtime, API, package, game, Workshop, save or source behavior was changed or validated by this review.
