# 20260720-0003: Batch 6 Corrected Phase 0 Closure

## Metadata

- Update ID: `20260720-0003`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: architecture/batch6/phase0/g0/g1/g2-design/g5/g6/identity/ownership/baseline
- Source: User request to complete corrected Batch 6 Phase 0 after the bounded Batch 5 closure.
- Related Reviews: [Batch 6 Boundary Correction Prerequisite](../../reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md); [Batch 5 And Batch 6 Prerequisite Audit](../../reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md); [AutoFishing Rehome Boundary](../../reviews/api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md); [SMAPI Gap And Content Host Boundary](../../reviews/api/2026/20260719-0011-smapi-api-gap-functional-surplus-and-content-host-review.md)
- Prior Update: [Batch 6 G0 Constraint Alignment](20260720-0002-batch6-g0-constraint-alignment.md)

## Scope

Complete only the corrected Phase 0 work admitted by Review 0012:

1. close G0 authority and project every canonical identity onto manifest, Loader, SDK, Doctor, Manager, package and UI design;
2. create G1's reproducible 23-domain ownership/source/assembly baseline;
3. freeze G2's atomic synthetic-fixture, pre-load admission, validation and rollback design without implementing it;
4. classify Batch 5's generic, product, QA, Compatibility and ContentOwner paths;
5. record the current 0.5.5 consumer scan and compatibility windows.

This Update changes no executable C# source, live manifest/Author SDK schema, template, product manifest, Runtime package schema, Loader behavior, public API, game directory or Workshop state. It does not implement Advanced CodeMod, `ContentPackFor`, a Content Host, the G2 fixture or any product migration.

## G0 Closure

`PROJECT.md` remains the sole normative owner for Strict/Advanced/ContentPack/External and Platform/SharedNative/ProductNative/ContentOwner. The new [managed Mod identity contract](../../../architecture/batch6-managed-mod-identity-contract.md) is only an operational projection.

The projection closes four audited P1 gaps:

- corrects the player topology to one BepInEx plugin entry plus four co-located DTMAPI Runtime dependencies under `BepInEx/plugins/DTMAPI`; managed products remain outside that directory;
- records that current Core/registry/tool predicates disagree for unknown or contradictory `Type` inputs and requires one G2 classifier to reject them before `Assembly.LoadFrom` or publication;
- distinguishes declared Strict identity from SDK-verified provenance, including the eight tracked legacy manifests that omit `Type`;
- makes the architecture gate persistent and machine-tested instead of relying on the one-time assertions in Update 0002.

The reserved G2 design keeps `Type=CodeMod|ContentPack` as the code/content axis and uses an orthogonal versioned `CodeModKind=Strict|Advanced` discriminator. Omission maps to Strict for compatibility. The field is not present in the live schema and must not be hand-authored before G2.

G2 must also bind Advanced to a verifiable local game/reference build. The current fail-open `MinimumGameVersion` warning is insufficient; missing, unknown or mismatched game/reference evidence must reject the Mod before assembly load.

## G1 Reproducible Baseline

The machine contract [batch6-phase0-domain-contract.json](../../../../tools/release/contracts/batch6-phase0-domain-contract.json) inventories all 23 domains named by Review 0012. It keeps Review disposition, canonical physical-ownership candidates, decision state and target deployment form separate. Every row also records:

- current consumers and exact count;
- native owner and state holder, or an explicit not-established blocker;
- Core/platform, GameBridge, product, QA and Compatibility source scopes;
- target assembly and mandatory-load state;
- public/internal API effect;
- Hook/Harmony, cache/updater/event and cleanup responsibility;
- mandatory Runtime and product-package marginal plan.

This closes the Phase 0 G1 reproducible baseline/decision inventory, not the complete G1 ownership gate. Mine/Audio remain candidates, BGM remains candidate-deferred, and CustomEntities/Multiplayer/Pets-and-Vehicles remain blocked or deferred without a fabricated native owner. Their implementation is not admitted; each needs a focused native-owner decision before full G1 can pass.

[The generated receipt](../../../../tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json) is bound to source commit `653487b7463778c23e9b96aed9ef713364def22a`, capture time `2026-07-20T12:00:00Z`, tracked `.cs` include rules and explicit exclusions. It lists every selected file, physical LOC and Git blob, plus deterministic category tree hashes. Documentation/tooling added by this Phase 0 commit does not alter the captured player/product C# tree.

The contract enforces a zero allowed ProductNative implementation delta in mandatory Runtime. Only explicit Platform work or a separately reviewed SharedNative owner with at least two independent real consumers can justify positive mandatory Runtime code.

## G2 Atomic Design

The synthetic `DTMAPI.AdvancedFixture` is the next eligible implementation, not part of this Update. Its G2 Update must atomically cover:

- manifest and author-project schema revisions;
- one pre-load classifier and source/dependency/placement admission;
- Strict and Advanced SDK/reference policies while retaining `SDK160` for Strict;
- hash-receipted local game/Unity/Harmony/BepInEx references with `Private=false`, no official assembly redistribution and `netstandard2.0` output;
- verifiable game/reference build compatibility;
- Doctor machine codes/player text and Manager identity/native-risk/restart rows;
- package/deploy/journal/recovery identity binding and native dependency exclusion;
- canonical `dtmapi.mod.<lowercase UniqueID>` Harmony ownership, audit and restart semantics;
- enabled/disabled/update/failure-isolation/log/report/clean-exit game evidence and full rollback.

The fixture is synthetic and uses the third save only if an in-save path is required. Passing it may admit only AutoFishing as the single pilot. AutoFishing behavior and GC remain fifth-save work. Other products remain blocked until the complete G0-G7/AutoFishing proof.

## G5 Batch 5 Reclassification

The baseline enumerates and classifies the exact 115-file `.cs`/`.ps1` union from `7ff75c4c^..9e31aae5` and `e5aaa964..653487b7463778c23e9b96aed9ef713364def22a`. This is the Batch 5 checkpoint-to-closure source/tooling set and deliberately excludes the intervening `e5aaa964` reverse-baseline-only correction; the shared `test.ps1` entry point remains because Batch 5 also changed it:

- Core event/generation/boundary/demand accounting/owner cleanup is Platform;
- first-party/testmod policy and single-product GameBridge features target ProductNative;
- CustomAnimals targets a future optional ContentOwner;
- optional QA/tests/performance orchestration remain QA;
- legacy fishing execution remains Compatibility with no new product logic;
- mixed GameBridge demand/hook/root files must split generic or proven shared shell from product routes during the owning migration;
- evidence/release scripts remain non-player tooling.

This classification preserves Batch 5's bounded implementation acceptance while explicitly rejecting the inference that demand-inactive or passing smoke evidence proves permanent physical ownership.

## G6 Consumer And Compatibility Plan

The reproducible tracked-source scan records:

- no current product source consumes frozen `IFishingAutomationApi`; the exact retained published AutoFishing DLL remains the known binary consumer;
- first-party AutoFishing is the only tracked product consumer of the internal fishing-primitives seam;
- Zoom is the tracked CameraView consumer;
- twelve tracked product/QA source files consume the current product-mirror API group;
- no tracked product source consumes Lamp or CustomEntity runtime APIs.

No surface is removed here. Frozen fishing and Lamp compatibility remain through 0.5.5, with an earliest 0.6.0 breaking gate only after renewed zero-consumer scans, migration guidance and one actually published warning-bearing preview. The first-party fishing seam can be removed only in the admitted AutoFishing pilot. Every other product API requires its own compatibility Update. External consumers cannot be proven absent from a repository-only scan, so no silent deletion is authorized.

## Changed Files

- `PROJECT.md`, `AGENTS.md`, `src/README.md`, `src/DTMAPI.BepInExBootstrap/README.md`, `docs/onboarding/current-state.md`: correct current Runtime topology/provenance and link or project the Phase 0 gate.
- `docs/architecture/batch6-managed-mod-identity-contract.md` and `docs/architecture/README.md`: operational identity/G2 contract and navigation.
- `author-sdk/README.md`, `docs/design/dtmapi-manager-ui-mvp.md`, `docs/workflows/codex-api-rebuild.md`, `docs/workflows/workshop-package-subscription-test-matrix.md`: Strict-only current state and future projection references.
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`: corrected Phase 0 status and next-step boundary.
- `tools/release/contracts/batch6-g0-mod-identity-contract.json`: persistent G0 state, topology, reserved G2 design and meta-negative classifications.
- `tools/release/contracts/batch6-phase0-domain-contract.json`: G1 baseline/decision inventory plus G5/G6 machine contract.
- `tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json`: reproducible source/LOC/blob/consumer/classification receipt.
- `tools/scripts/build-batch6-phase0-baseline.ps1` and `tools/scripts/test-batch6-phase0-contract.ps1`: deterministic generator and fail-closed gate.
- `tools/scripts/test.ps1`: full-suite registration and PowerShell 5.1 syntax coverage.
- prior G0 Update/Reviews, this Update and `docs/updates/INDEX-2026-07.md`: resolution links and lifecycle navigation.

## Validation

- `tools/scripts/test-batch6-phase0-contract.ps1` passed: four identities, four ownership categories, 23 domains, 115 classified Batch 5 files, full G1 ownership blocked, G2 Runtime blocked and AutoFishing blocked.
- `tools/scripts/build-batch6-phase0-baseline.ps1 -Check` reproduced the committed receipt exactly from source commit `653487b7`.
- PowerShell 7 and Windows PowerShell 5.1 both passed the complete Phase 0 contract/receipt checker; their semantic receipts agree, while the canonical reproduce command remains `pwsh` because host JSON indentation differs.
- `tools/scripts/check-doc-governance.ps1 -Quiet` passed after the new Update and links were registered.
- `git diff --check` passed.
- `tools/scripts/test.ps1 -Configuration Release` passed on the final tree with exit `0` in `723.1s`: all managed builds reported zero warnings/errors, and Unit/QA, InstallDoctor, Player Doctor, Author SDK, dual-host upgrade/install transactions, Candidate11, Catalog, release contracts, Batch 4 semantic/meta-negative, Batch 5 GC/no-demand source gates, ABI, artifact and documentation/evidence governance checks passed.
- The first full-suite attempt stopped after passing `DTMAPI.UnitTests` because cleanup briefly observed PID `25060`; the process exited naturally before inspection. The managed cleanup protocol reclaimed the completed session, the clean full rerun passed, no DTMAPI test/smoke process remained, and the rerun's reported cleanup-pending session path was absent at final inspection.
- No game smoke was run or required: no runtime, Hook, UI implementation, package format, manifest schema or product changed. The shared Runtime lock was not acquired.

## Rollback

Revert the Phase 0 contracts, generator, receipt, checker, Update row and status links as one unit. Preserve the independently verified current topology—one BepInEx plugin entry plus four co-located Runtime dependencies—unless the actual player package changes under its own reviewed Update. Do not alter Runtime binaries, product packages, local game files or historical evidence. A partial rollback that leaves a live-looking Advanced token or a `Phase 0 passed` claim without its machine gate is invalid.

## Result

Corrected Batch 6 Phase 0 is complete. G0 authority and component projection pass; G1 has a reproducible all-domain baseline/decision inventory while its full ownership gate remains blocked for unresolved domains; the G2 atomic fixture/validation/rollback design and G5/G6 inventories are frozen. G2 Runtime, the synthetic fixture, AutoFishing, all other product migrations and G7 Content Host implementation remain blocked/unimplemented. The next eligible work is one independent atomic G2 minimal-fixture Update. This result does not make 0.5.5 releasable.

## 2026-07-20 Acceptance Correction

The schema-1 receipt and focused gate recorded above were later shown to admit false facts about reserved `CodeModKind`, no-artifact/mutable prototype consumers, capture time and mandatory Runtime ProductNative delta. Preserve this section as historical evidence; do not use those original receipt claims for admission. [20260720-0005](20260720-0005-batch6-phase0-machine-gate-correction.md) supersedes them with the schema-2 audited receipt, active-authority correction, dual-host/full-suite validation and third-save Runtime smoke. The corrected result still authorizes only a separate G2 synthetic fixture and does not make 0.5.5 releasable.
