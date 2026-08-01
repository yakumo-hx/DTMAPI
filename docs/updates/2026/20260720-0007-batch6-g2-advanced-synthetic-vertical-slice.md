# 20260720-0007: Batch 6 G2 Advanced Synthetic Vertical Slice

## Metadata

- Update ID: `20260720-0007`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: batch6/g2/advanced/manifest/author-sdk/reference-receipt/package/core/loader/harmony/doctor/manager/synthetic-fixture
- Source: User request to implement the G2 atomic Advanced CodeMod vertical slice after the corrected Phase 0 machine gates pass.
- Owning Review: [Batch 6 G2 Advanced Synthetic Vertical Slice Prerequisite](../../reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md)
- Runtime Root-Cause Review: [Batch 6 G2 First Runtime Matrix Root Cause](../../reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md)
- Prerequisite: [Batch 6 Phase 0 Machine Gate Correction](20260720-0005-batch6-phase0-machine-gate-correction.md)

## Scope

Implement one atomic, revertible Advanced CodeMod lane and prove it only with the synthetic `DTMAPI.AdvancedFixture`:

- add the versioned `CodeModKind=Strict|Advanced` manifest/author-project wire while preserving omitted-kind Strict compatibility and `SDK160` for Strict;
- resolve Advanced native references only from an explicit game root and a tracked, hash-fixed reference policy; never copy or redistribute official game, Unity, Harmony or BepInEx assemblies;
- bind identity, game build, exact native references, manifest, entry payload and expected Harmony owner in `dtmapi-advanced-references.json` through build, pack, deploy, status and recovery;
- classify and verify managed identity before assembly load or owner/API/registry publication;
- supervise the canonical Harmony owner and publish fail-closed, sibling-isolated restart/cleanup diagnostics;
- expose the same identity/provenance/native-risk/game-compatibility/restart facts through Doctor and Manager;
- exercise the harmless read-only `DolocAPI.Has087DemoData()` native owner on public build `23762374` with one synthetic fixture and negative modes.

This Update does not modify or migrate AutoFishing, another real product, Content Host G7, the stable public Abstractions API, or the one-bootstrap/four-dependency Runtime topology. It does not make 0.5.5 releasable.

## Changed Files

The implementation commit boundary is intentionally limited to the following groups; its immutable commit ID and generated ownership receipt are added only by the later G2 closure commit:

- policy/schema/templates: `.gitattributes`, `author-sdk/README.md`, the two files under `author-sdk/advanced-reference-policies/`, the Advanced policy/receipt schemas plus the updated author, manifest, package, deployment and Doctor schemas under `author-sdk/schemas/`, and both CodeMod/ContentPack author templates;
- Author SDK/contracts: `src/DTMAPI.Authoring.Contracts/AuthorContracts.cs` and the project plus eleven Advanced build/reference/package/deploy/status/validation source files under `src/DTMAPI.AuthorSdk/`;
- Runtime/Manager: `src/DTMAPI.Core/DTMAPI.Core.csproj`, `Diagnostics/DiagnosticsModels.cs`, both Manager projection files, the four manifest/classifier/reference-inspector files, and the five runtime registry/snapshot/loader/Harmony-supervisor files changed by this slice;
- Doctor/tooling: the InstallDoctor project, engine/models/formatter/manifest probe, the six Advanced identity/reference/native/build probes, and the two metadata inspection files under `src/DTMAPI.Tooling.Metadata/`;
- synthetic fixture only: the four source-authority files under `testmods/DTMAPI.AdvancedFixture/`;
- automated tests: `tests/DTMAPI.AuthorSdk.Tests/Program.cs`, the InstallDoctor test project/program and its three two-file fixture projects, and the UnitTests project/program, `Batch6AdvancedRuntimeTests.cs`, and the independent two-file `RuntimeCodeModFixture`;
- gates/authorities: the G2 contract and ownership-receipt schema, `tools/release/dtmapi-product-catalog.json`, both G2 build scripts, the G2 checker, Author SDK/Catalog/Phase 0 checker integration, `tools/scripts/test.ps1`, this Update, its prerequisite Review, and only the `20260720-0007` monthly-ledger row.

The Catalog addition is explicitly `QaFixture / Fixture / None / NeverPublish / Experimental` with legacy lane `SyntheticG2Only`. It raises the current Catalog inventory from 26 to 27 and the dynamically checked production-source count from 172 to 175, while the public product count remains 11, Workshop inventory remains 21 and API rows remain 47. The three production-source additions are exactly `ManagedModClassification.cs`, `PortableAssemblyReferenceInspector.cs` and `AdvancedHarmonySupervisor.cs`.

No file under `src/DTMAPI.Abstractions/`, `src/DTMAPI.GameBridge.DolocTown/`, `first-party-mods/AutoFishingMod/` or another real product root changes in this implementation boundary.

The unrelated concurrent portable reverse-capture files (`20260720-0006` and `tools/portable-reverse-capture/**`) are outside this Update and must not be staged or committed with G2.

## Validation

The implementation is frozen by commits `8430266b`, `8db36f8b` and `c79306df`; the immutable G2 implementation authority is `c79306dfc7de0e85c74e24ece7d4c5cd47cb0822`. AutoFishing and every other real product remained unchanged throughout this synthetic boundary.

- The baseline-to-implementation ownership receipt audits 41 files, `+5991/-122` physical lines and source-diff SHA-256 `CF60A5BC51A49355C3A038A111092B0E5D4A16ACCA3152A2E12C3BC5D95695E6`. Mandatory Runtime ProductNative delta is `0`, stable Abstractions delta is `+0/-0`, and all 14 mandatory Core/project changes are bound to exact target blobs, per-file diffs and reviewed non-ProductNative semantic allowances.
- The committed runtime receipt binds nine ordered cases to implementation `c79306df`, Runtime release manifest `63FC8156D7924E272799EEE22D63B715444DEA1E98EEF088FEFBEE87E7E2528F`, matrix SHA-256 `EC26CED5E689A0839B438FC4C421BDD032AFD26FC308C17C20936812BC95BAD3`, both package ZIP contents, Doctor projections and exact log/report/cleanup hashes.
- Four cold negative cases proved owner mismatch, duplicate patch, Entry failure and attributable late drift. Disabled cold, normal v1, post-load disable, one-session SDK v1-to-v2 update/reload and final clean v2 proved the positive, restart and transactional paths. Expected negative runner exits remain evidence-bearing failures; all cases passed Strict sibling isolation, third-save load (`runtime slot=2`), exact profile/QA restoration and process absence.
- The runtime receipt independently proves zero `Yuuka.DTMAPI.AutoFishing` load-source/Entry observations, disabled AutoFishing report state, one verified synthetic Advanced identity only, and exact policy/game-build/Harmony-owner projection through Player Doctor.
- PowerShell 7 and Windows PowerShell 5.1 rebuild the ownership receipt identically and validate the committed runtime receipt. Six ownership and four runtime non-writing mutation probes fail closed, including ProductNative reclassification, unmapped mandatory code, target-blob/diff drift, generic smoke-exit drift, AutoFishing identity drift, ZIP-entry drift and cold-runner-receipt drift.
- The Author SDK release contains 363 files with ZIP SHA-256 `82A4BF4970D0967116CC5946AEF3DFFD61A90FACFA11F7E02A16A701290CB434`. Synthetic packages are v1 `D4BF24471D9D5962CC353970DAD5969C06636A2F5B09A043AE50FA742EDFE1B9` and v2 `BE705F7387908DD2C4662A48428C0808A3BD66CF0F123DD0EAB0680E67DBCC69`; neither contains a native Runtime dependency.
- The final pre-closure `tools/scripts/test.ps1 -Configuration Release` suite passed with exit `0` in `801.8` seconds. It included all Release builds, full Unit/QA suites, Doctor `10/10`, Player Doctor release/portable, Author SDK release/portable, install/deploy transactions, Catalog, Phase 0, Batch 2/4/5, document governance, evidence retention and the G2 candidate gate.
- The closure commit must have `c79306df` as its direct parent and change exactly the 32 contract-listed governance/receipt paths. The permanent PowerShell 7 and Windows PowerShell 5.1 gates are run against that exact committed state; their post-commit result is part of the final handoff and the executable gate remains the current authority.

The rejected first attempt `GAME-SMOKE/20260720-203217` remains root-cause evidence only. It led to the bounded sibling-attribution, staged-QA reload, top-level Steam build-id and cleanup-detail corrections recorded by the focused Review; it is not part of the accepted nine-case receipt.

## Evidence

- Phase 0 admission commit: `1239aa577d74e4f0c29131ba644e8a751eebbf5f`.
- G2 implementation commit: `c79306dfc7de0e85c74e24ece7d4c5cd47cb0822`.
- Native target/build evidence is frozen in the owning Review and focused Hook map.
- Ownership receipt: `tools/release/baselines/batch6-g2-ownership-receipt.json`.
- Runtime receipt: `tools/release/baselines/batch6-g2-runtime-matrix-receipt.json`.
- Accepted runtime roots: `GAME-SMOKE/20260720-211547`, `211632`, `211713`, `211754`, `211835`, `212010`, `212235`, `213129`, and `213231`.
- Cold-run orchestration receipt: `GAME-SMOKE/20260720-211835/g2-cold-cases-runner-receipt.json`.

## Rollback

Withdraw the exact synthetic SDK deployment first, using its receipt-bound recovery artifacts, after proving the game process is absent. Then revert the three implementation commits and the independent G2 closure commit as one boundary. The revert must remove all Advanced acceptance and restore Strict-only SDK/Runtime behavior without changing real product manifests/packages or Runtime DLL topology. A partial rollback that leaves `CodeModKind=Advanced` accepted by any live surface is invalid.

## Follow-up

After the independent closure commit and permanent dual-host G2 gate pass, AutoFishing becomes the sole admitted-but-not-yet-migrated real pilot. Its migration must use a separate Review/Update and independently complete G3/G4 plus applicable G5/G6. Every other real product, Content Host G7 and 0.5.5 release remain blocked.
