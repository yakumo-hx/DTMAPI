# 20260720-0005: Batch 6 Phase 0 Machine Gate Correction

## Metadata

- Update ID: `20260720-0005`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: architecture/batch6/phase0/g1/g2-prerequisite/manifest/ownership/provenance/version/roadmap
- Source: User request to close the independent Phase 0 acceptance-audit blockers before implementing G2.
- Owning Review: [Batch 5 Closeout And Batch 6 Phase 0 Acceptance Audit](../../reviews/code/2026/20260720-0003-batch5-closeout-and-batch6-phase0-acceptance-audit.md)
- Corrects: [Batch 6 Corrected Phase 0 Closure](20260720-0003-batch6-phase0-closure.md)

## Scope

Correct only the five Phase 0 machine-truth blockers found after commit `8e24a9e4b79825801a2abd7c76c75413cd0943d8`:

1. reject the reserved `CodeModKind` field across schema, Author SDK validation/package/deploy, Doctor and Core before G2;
2. bind current consumers to Catalog plus tracked-source or retained-artifact evidence, keeping the no-artifact AnimalPack only as a planned consumer and mutable-only CustomAnimals prototypes outside the real-consumer count;
3. replace the impossible pre-recorded capture time with a real receipt timestamp bounded by source/audit commit time and the receipt commit or current trusted clock;
4. measure mandatory Runtime `.cs` changes from the fixed baseline to an audited commit, bind every changed blob to an explicit Platform/SharedNative/ProductNative classification and require measured ProductNative added lines to remain zero;
5. synchronize the active API version projections and make AutoFishing, not Zoom, the only product admitted after the separate synthetic G2 fixture.

This Update must not implement the live Advanced discriminator, relax `SDK160`, hand-package an Advanced Mod, migrate AutoFishing or migrate another real product. The correction uses a two-commit receipt model: first freeze the executable correction source, then generate and commit the receipt against that exact audited commit.

## Changed Files

Correction source commits `d440be8` and `2c7b142` change:

- `author-sdk/schemas/manifest.schema.json`, `src/DTMAPI.AuthorSdk/JsonSupport.cs`: make current author manifests closed-shape inputs and make all shared SDK manifest readers reject unmapped/reserved fields.
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`, `src/DTMAPI.InstallDoctor/ManifestProbe.cs`: reject `CodeModKind` before Core discovery/load and through Doctor's structured `invalid-manifest` path without adding it to the live manifest model.
- `tests/DTMAPI.AuthorSdk.Tests/Program.cs`, `tests/DTMAPI.InstallDoctor.Tests/Program.cs`, `tests/DTMAPI.UnitTests/Program.cs`: execute validate/pack/deploy, Doctor and Core pre-load hostile-field negatives.
- `tools/release/contracts/batch6-phase0-domain-contract.json`, `tools/scripts/build-batch6-phase0-baseline.ps1`, `tools/scripts/test-batch6-phase0-contract.ps1`: own schema-2 consumer evidence, real-time provenance, executable mandatory Runtime delta and active-authority gates.
- `tools/release/contracts/batch6-g0-mod-identity-contract.json`: keeps Phase 0/G1 baseline explicitly `correction-in-progress` until receipt commit B.
- `AGENTS.md`, `PROJECT.md`, `docs/architecture/batch6-managed-mod-identity-contract.md`: withdraw the stale Phase 0 admission claim while correction is in progress so no agent can use an active authority to enter G2 early.
- `docs/api/public-api-matrix.md`, `docs/design/mod-owner-lifetime-contract.md`, `docs/onboarding/current-state.md`, `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`, `tools/release/dtmapi-product-catalog.json`: synchronize current version routing/projections and the AutoFishing-first order while preserving historical evidence.
- this Update and `docs/updates/INDEX-2026-07.md`: own the correction lifecycle.

The containing receipt/authority commit adds the regenerated `tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json`, restores verified Phase 0 state, links the Review resolution, and records final validation. `docs/debug/regressions/smoke-matrix.md` records the clean third-save acceptance; the new dated Batch 4 G0-G7 history preserves unchanged older rows moved out when the active router reached its size limit. G2 Runtime and product files remain outside this Update.

## Acceptance Gates

- A hostile `CodeModKind` manifest fails SDK validation and package/deploy admission, is a structured Doctor error, and is rejected by Core before registry/owner/API publication or assembly entry.
- Every `currentConsumers` value resolves to one Catalog identity with tracked-source or retained-artifact evidence; AnimalPack has zero current consumers and remains planned/no-artifact, while five mutable-only CustomAnimals prototype inputs remain explicitly non-qualifying.
- The receipt proves `baseline/audit commit time <= capturedAtUtc <= receipt commit/current UTC` and reproduces with its frozen capture time.
- Mandatory Runtime delta rows cover the complete five-root `.cs` union, use actual baseline/audit blobs and added lines, contain no unclassified change, report zero ProductNative added lines, and fail on later committed, dirty or untracked source drift.
- The API matrix projects release/API `0.5.5`, binary/file `0.5.5.0`, assembly compatibility `0.5.3.0`, and future-blocked `0.5.6-alpha` without rewriting historical compatibility evidence.
- The roadmap keeps Zoom blocked until the synthetic G2 fixture, the unique AutoFishing pilot, G3/G4/related G5-G6 and the complete G0-G7 proof.
- Focused gates pass under PowerShell 7 and Windows PowerShell 5.1; the full Release suite passes serially; any required clean runtime smoke follows the shared lock protocol.

## Validation

Source-stage validation before commit A:

- Release builds for UnitTests, InstallDoctor tests and Author SDK tests completed with zero warnings/errors through the repository-local .NET 8 toolchain.
- `DTMAPI.UnitTests`, `DTMAPI.InstallDoctor.Tests` and `DTMAPI.AuthorSdk.Tests` all passed; their new hostile paths prove Core rejection before any mod-load checkpoint/registry publication, Doctor `invalid-manifest`, SDK validation/package failure, and deploy refusal without a destination.
- The schema-2 generator produced and reproduced a temporary 23-domain/115-Batch-5-file receipt. A checker run against that temporary receipt rejected the uncommitted `ManifestReader.cs` drift, demonstrating that the new audited-commit gate observes the worktree instead of trusting a JSON maximum.
- PowerShell 7 and Windows PowerShell 5.1 both pass the correction contract with explicit `-AllowStaleReceipt`; this transition-only mode warns that the tracked schema-1 receipt is stale and skips receipt claims.

Final validation:

- The official schema-2 receipt is bound to baseline commit `653487b7`, audited correction commit `2c7b142`, audited commit time `2026-07-20T09:02:25Z` and real capture time `2026-07-20T09:02:36Z`. It contains 23 domains, 115 Batch 5 files, evidence-bound current consumers and the executable five-root mandatory Runtime delta with measured ProductNative added lines `0`.
- PowerShell 7 and Windows PowerShell 5.1 both reproduce the official receipt and pass the focused contract without `-AllowStaleReceipt`. The pre-commit runs correctly warn that the containing-commit upper bound must be rerun after the receipt commit.
- `tools/scripts/test.ps1 -Configuration Release` passed serially with exit `0` in `725.2s`; all managed builds reported zero warnings/errors and the Unit, QA, Doctor, Author SDK, transaction, Catalog, Batch 2/4/5, ABI, artifact and governance gates passed.
- `GAME-SMOKE/20260720-171708` is a preserved orchestration-only failed attempt: Runtime startup, three Strict CodeMods, HookProbe GameLaunched, mod-load transaction, registry, fatal, cleanup and exit passed, but no participant owned title-to-save navigation (`saveLoad requests=0`), so SaveLoaded was correctly not claimed.
- `GAME-SMOKE/20260720-172620` passed under the shared lock with Steam, slot 3, isolated CoreOnly, HookProbe and the staged QA load owner: `StartupLog`, `GameLaunched`, `HookProbe`, `SaveLoaded`, `QaSaveLoadedObservation`, `ModLoadTransaction`, registry, no-fatal, forced close, process exit, exact QA cleanup and byte-identical profile restoration all passed. No `DolocTown.exe` or Runtime lock remained.
- Independent correction reviews returned GO after the active-authority, mutable-prototype and architecture-scope contradictions were closed; no P0/P1 remained.

Phase 0 is reaccepted. This authorizes only the separate G2 synthetic fixture Update. G2 Runtime, AutoFishing and every other real product remain blocked.

## Rollback

Revert the implementation commit and the later receipt/authority commit together. Restore the prior Phase 0 receipt only as historical evidence; doing so reopens the five blockers and must also restore `g2Runtime=blocked`/Phase 0 correction-required state. No game, Workshop or product rollback is expected unless runtime validation later records such a mutation.

## Follow-up

Create a separate G2 Update for the synthetic Advanced vertical slice. Only a fully passing synthetic slice may admit AutoFishing as the single real-product pilot.
