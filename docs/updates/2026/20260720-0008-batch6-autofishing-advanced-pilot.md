# 20260720-0008: Batch 6 AutoFishing Advanced Pilot

## Metadata

- Update ID: `20260720-0008`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: batch6/g2-real-product/g3/g4/g5/g6/autofishing/advanced/policy-registry/product-native/compatibility/qa/migration-evidence
- Source: User request to close the corrected Batch 6 sequence after synthetic G2 by admitting AutoFishing as the sole real Advanced product pilot and completing G3/G4/relevant G5-G6 without opening other products or G7.
- Owning Review: [Batch 6 AutoFishing Advanced Pilot Prerequisite](../../reviews/code/2026/20260720-0006-batch6-autofishing-advanced-pilot-prerequisite.md)
- G2 prerequisite: [Batch 6 G2 Advanced Synthetic Vertical Slice](20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md)
- Architecture authority: [Batch 6 Managed Mod Identity Contract](../../architecture/batch6-managed-mod-identity-contract.md)
- Independent acceptance: [Batch 6 AutoFishing Independent Acceptance](../../reviews/code/2026/20260721-0003-batch6-autofishing-independent-acceptance.md)

## Scope

This Update owns the complete implementation lifecycle for the sole admitted real Advanced CodeMod pilot, `Yuuka.DTMAPI.AutoFishing`:

- add an AutoFishing-only, UniqueID-bound Advanced reference policy and exact SDK/Core/Doctor policy registry while preserving the immutable synthetic G2 policy and Strict `SDK160`;
- move AutoFishing ProductNative state, reflection/cached delegates, Harmony patches, native transactions, input/animation control, caches and cleanup out of mandatory GameBridge and into the product;
- delete the one-consumer first-party primitives/friend/provider/facade/demand/status scaffolding;
- keep the frozen `IFishingAutomationApi` ABI in a separately named, time-bounded compatibility path for the 0.5.5 window;
- move AutoFishing-specific QA and performance orchestration to optional product QA/tests;
- make Author SDK build/pack/deploy the only production authority for the canonical `Yuuka.DTMAPI.AutoFishing.dll` package;
- generate one consolidated migration evidence manifest for the AutoFishing-specific G4/G5/G6 deltas, reusing Catalog, Author SDK package receipt, ABI, Doctor and release authorities;
- validate each hidden implementation slice with focused checks, then validate the exact final product package once with integrated Release, Doctor/Manager, fifth-save behavior/lifecycle, old-binary compatibility and long-running L0-L5 evidence.

This Update does not admit another real product, implement Content Host G7, remove the frozen 0.5.5 fishing ABI, upload/update Workshop content, or authorize DTMAPI 0.5.5 release. It does not retarget any game-loaded assembly away from `netstandard2.0` and does not place managed DTMAPI products under `BepInEx/plugins`.

### Staged implementation, atomic admission

This remains one implementation lifecycle Update. The intended landing shape was four small, reversible slices while AutoFishing remained blocked from general admission:

1. UniqueID-bound policy registry and SDK/package support, without enabling the real product;
2. ProductNative source/package rehome and removal of mandatory fishing-only execution seams;
3. compatibility and optional QA relocation, with focused ABI/package checks;
4. one integrated final package, migration evidence, fifth-save acceptance and admission decision.

`Atomic` applies only to slice 4's final admission state. A focused check follows each slice. The complete Release suite and long-running game matrix are not repeated after every intermediate documentation or source correction.

The actual implementation did not preserve that intended commit shape: `8c78ec6b` landed the main policy/product/ProductNative/compatibility/QA rehome as one large feature commit, followed by focused integration repairs through `aca65a15`. The final admission state and evidence remain atomic and recoverable, but this Update records the oversized initial landing rather than treating the plan as the result.

## Frozen starting facts

- Synthetic G2 implementation authority is `c79306dfc7de0e85c74e24ece7d4c5cd47cb0822`; its policy, package and receipts prove only `DTMAPI.AdvancedFixture` with `doloctown-23762374-g2-v1`.
- G2 receipt file/runtime/ownership hashes remain historical authorities and must not be regenerated to include AutoFishing.
- AutoFishing is the sole admitted-but-not-yet-migrated real product.
- Current product identity is `Yuuka.DTMAPI.AutoFishing`, version `1.4.3-dtmapi`, Workshop item `3743799721`, default F6.
- Current product still consumes `IFirstPartyFishingPrimitivesApi` from mandatory GameBridge and its raw project outputs `AutoFishingMod.dll` before release scripts rename it.
- Mandatory GameBridge currently contains the fishing-only product engine; frozen public compatibility still has a known retained binary consumer.
- Batch 5 AutoFishing fifth-save/GC evidence is historical evidence for the old physical owner and old exact bytes. It is not post-migration proof.

The implementation baseline commit is intentionally not filled in at this docs-only checkpoint. It must be the immutable, clean, post-G2/prerequisite commit selected immediately before source migration. A compact baseline record may bind that Git ref and required root identities, but it does not need a product-specific const schema duplicating facts already frozen by Git.

## Planned changed groups

### 1. Policy schema and exact registry

Planned authority:

- add `author-sdk/advanced-reference-policies/doloctown-23762374-autofishing-v1.json`;
- add the corresponding minimal `Assembly-CSharp` compiler reference surface;
- extend the policy schema/model with exact manifest UniqueID binding without changing the bytes/hash of `doloctown-23762374-g2-v1`;
- replace single-policy constants/resources in Author SDK, Core and InstallDoctor with deterministic policy registries;
- bind legacy schema-1 G2 policy to `DTMAPI.AdvancedFixture` in the registry and new AutoFishing policy to `Yuuka.DTMAPI.AutoFishing`;
- validate `policyId`, policy hash/version, UniqueID, game build, native reference set, entry/manifest hashes and canonical Harmony owner before load/publication;
- keep Manager as a projection of Core classification and prevent identity inference from file/path/product name.

The AutoFishing policy ID is frozen as `doloctown-23762374-autofishing-v1`. It must use build `23762374`, exact tracked `0Harmony` and `Assembly-CSharp` references with `copyLocal=false`, and canonical owner `dtmapi.mod.yuuka.dtmapi.autofishing`.

### 2. Product author project and package

Planned product shape:

- Author SDK schema-2 CodeMod project with `codeModKind=Advanced`, explicit `referencePolicyId`, `EntryType` and canonical `EntryDll=Yuuka.DTMAPI.AutoFishing.dll`;
- preserve UniqueID, Workshop identity, config keys/defaults/path, i18n, icon/preview and product version `1.4.3-dtmapi`;
- set the new package minimum Runtime to `0.5.5`;
- retain only the MCM/platform dependency; remove required GameBridge dependency;
- package one product entry DLL plus manifest/i18n/assets/SDK receipt; zero native dependency DLLs;
- remove raw product csproj and DLL rename/repack from production build/release/install authority;
- preserve raw/linked source only where needed for tests without allowing it to produce the player package.

### 3. ProductNative migration

Move/fold into the AutoFishing product:

- native type/member access and cached delegates;
- Ready/Cast/Wait/Pull/minigame adapters and state caches;
- cast/reel transactions and energy gate;
- animation controller/cache and Hook body state;
- synthetic input and visible-reel retry/acceptance state;
- fishing decision/runtime components, sequence checks and cleanup;
- all fishing Harmony callbacks and the fishing-owned `AgentStateBase.OnExit` patch;
- product diagnostics through generic monitor/diagnostics, not a mandatory fishing feature channel.

Entry must resolve the full target/callback inventory before subscribing recurring product work, install it with the canonical owner, and fail closed/atomically. F6 off must remove recurring updater and transient work; cold disabled must load no product assembly/owner; post-load Manager disable remains restart-required.

### 4. Mandatory Runtime cleanup

Delete product-only seams:

- `FirstPartyFishingPrimitives.cs` and AutoFishing friend opening;
- first-party provider/facades/session handlers and product API acquisition;
- GameBridge fishing product feature/composition/callback dispatch;
- fishing demand/updater/retained-callback/readiness/status/no-demand/owner-cleanup specializations;
- fishing branch from shared AgentState base-exit routing;
- obsolete source gates that require an Abstractions-only product or forbid the admitted Advanced native implementation.

Keep generic Platform/Event/Owner/Demand/Generation/Diagnostics/MCM mechanisms and non-fishing shared-native consumers. Mixed files require symbol/hunk classifications in the G4 and G5 receipts.

### 5. Compatibility boundary

Retain frozen public `IFishingAutomationApi` DTO/signature ABI and old-binary executor through the 0.5.5 compatibility window, but separate it from the new product:

- remove primitive attachment/arbitration from compatibility;
- give compatibility private helpers where required;
- rename demand/status/callback roots as compatibility-only;
- emit once-per-owner deprecation and Doctor/Manager migration guidance;
- keep retained ABI tests for exact binary SHA-256 `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA` and its `StopOnManualMove` setter MemberRef;
- record mandatory base-weight debt if the executor cannot yet be optionalized;
- do not schedule public surface removal before a real warning-bearing preview, migration guide and fresh zero-consumer scan; earliest planned breaking cleanup remains 0.6.0.

### 6. QA and tests

Move/rewrite AutoFishing-specific fixture, native control, performance and GC orchestration as optional product QA or test-only assets. Keep generic Advanced classifier/build/package/restart/failure-isolation tests in framework test projects. Preserve current mechanic and lifecycle coverage while replacing primitive-seam assumptions.

`tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj` must not remain a production package authority through a direct raw product ProjectReference. Tests may link pure product sources or inspect an SDK-built package, but Release packaging must be SDK-only.

### 7. Release/package tooling and authorities

Update build/release/install definitions, product catalog and protected behavior contract so they describe the self-contained Advanced product while preserving behavioral constants. Historical Batch 5 receipts remain historical. Add only the consolidated product-migration evidence needed for new facts; reference existing package, ABI and release authorities instead of restating them.

Only facts whose owning systems change should be updated: public API matrix for compatibility/version facts, product catalog for product/package/gate facts, active smoke/debug records only after real game runs, and this Update for implementation lifecycle.

## Consolidated G4/G5/G6 migration evidence

The implementation uses one AutoFishing migration evidence manifest, backed by the clean immutable pre-migration Git commit and the final implementation commit. It may reuse a generic schema/builder/checker, but this Update does not authorize three product-specific receipt families.

The manifest records only evidence that is specific to this migration and links the existing owners for facts already covered elsewhere:

- G4: the Abstractions/Core/Bootstrap/GameBridge/ModConfigMenu delta, with no new mandatory ProductNative and zero known AutoFishing ProductNative owner/symbol leftovers, plus changed mixed-file symbols and any justified Platform additions;
- G5: classifications for paths actually touched by this migration, whose physical ownership changes, or which contain a pre-migration known AutoFishing owner/symbol that must be absent afterward. The historical 161-path Batch 5 union remains audit material and is not replayed as a permanent per-product exact-set gate;
- G6: new/legacy consumer deltas and compatibility disposition, while package entries/hashes come from the Author SDK receipt, product identity comes from Catalog, old-binary behavior comes from the ABI canary, and player guidance comes from Doctor/Manager checks.

The consolidated evidence must still prove:

- new product source has zero `IFishingAutomationApi` and zero `IFirstPartyFishing*` usage;
- Abstractions has no AutoFishing friend opening and no first-party primitive definitions;
- new product AssemblyRefs contain only allowed managed/platform references and policy-bound `0Harmony`/`Assembly-CSharp` where expected, with no GameBridge AssemblyRef;
- exact old binary still loads/binds during the compatibility window;
- compatibility warning/guidance is present and compatibility is not the new product engine;
- package contains no official/native dependency DLL and is bound by the existing SDK package receipt;
- unobserved external packages are not represented as a closed universe.

Historical Batch 5 event/demand/generation/reload/performance evidence remains immutable. Demand-inactive or no-demand evidence proves disabled cost, not physical ownership. The live consolidated invariant includes a compact denylist/semantic scan for the known fishing-only primitive, provider/facade, demand, status and execution owners identified by the prerequisite Review; it must fail if any remain in mandatory Runtime even when aggregate line delta is zero. Mutation probes are required for that live invariant, not for copied historical wording, exact closure paths or duplicate receipt hashes.

## Validation matrix

The rows below describe the accepted candidate. Runtime rows use the exact unchanged product package identified under Evidence and checkpoints; the complete Release suite passed on the final source/tooling integration commit.

| Stage | Planned command/evidence | Required result | Current state |
| --- | --- | --- | --- |
| Policy/schema | focused Author SDK schema/unit checks and policy registry mutation probes | immutable G2 + exact AutoFishing UniqueID binding; Strict SDK160 unchanged | `passed` — exact registry/policy mutation coverage passes; the G2 policy remains immutable and `SDK160` remains the Strict boundary |
| Source/architecture | focused source consumer and forbidden-route scans | product self-contained; mandatory Runtime no new fishing ProductNative; compat isolated | `passed` — the product has no GameBridge/legacy/primitive consumption; mandatory Runtime has zero known AutoFishing ProductNative leftovers |
| Build/package | focused SDK build/pack/deploy/recover/status per slice; one repository-local .NET 8 Release suite at final integration | netstandard2.0; canonical DLL; deterministic receipt; no native payload | `passed` — focused SDK/package checks plus complete Release suite at `4da7f97d` |
| Core/Doctor/Manager | classifier/pre-load negative matrix and player projections | exact policy/build/hash/identity; native risk/restart visible; failure zero-publication | `passed` — focused tests plus Manager lifecycle root `BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-121144-ded2b298` |
| G4/G5/G6 migration evidence | one generic baseline-to-implementation manifest plus existing Catalog/SDK/ABI/Doctor/release authorities | no new mandatory ProductNative; known AutoFishing ProductNative owners/symbols zero-leftover; relevant ownership paths classified; zero new legacy consumers; exact package and old ABI evidence linked | `passed` — consolidated manifest and nine mutation probes pass; mandatory ProductNative additions are zero |
| Fifth-save behavior | real selected rod/pool, default and option-cross-product profiles | native behavior, energy/spirit/readback/save restore, movement cancel | `passed` — all eight profiles under `BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-122112-c46597d6` |
| Lifecycle | cold enabled/disabled, F6 off/on, save/title/re-entry, Manager disable/restart, failure recovery | canonical owner, zero duplicates/foreign owners, truthful restart state | `passed` — Manager matrix plus formal L4 recovery and L5 title/re-entry evidence |
| Long run | six independent L0-L5 cold sessions, fifth save, 600s/30s, 5 warmup + 10 measured fish | behavior/source/observer/metrics/restore/exit receipts; no forced GC/Fatal | `passed` as an unchanged-package composite: second-round L0-L3 plus formal L4-only and L5-only; no third full replay and no quantified long-term GC-budget claim |
| Compatibility | exact retained binary plus warning/Doctor/Manager guidance | StopOnManualMove binding and old binary behavior retained | `passed` — retained exact-byte ABI authority plus selected-item Unity Mono canary `GAME-SMOKE/20260721-161256` |
| Exit/recovery | collected logs, source/profile/save restoration, process checks | no forced close or residual process; Runtime lock released | `passed` — every cited formal run restored source/profile/save state, removed QA/deploy state, exited the process and released the lock |

Every game install, OfficialLocal/Workshop source mutation, launch and smoke uses the shared Runtime lock. AutoFishing native/behavior/GC authority is the fifth save slot. The third save is not accepted as a substitute for this pilot's native baseline.

## Evidence and checkpoints

At the initial docs-only checkpoint:

- the prerequisite Review records the native owners, state holders, policy boundary and exact file/responsibility dispositions;
- G2 synthetic implementation and receipts remain the only verified Advanced runtime evidence;
- no AutoFishing source/package/runtime receipt exists yet;
- no build, test, Doctor, game, fifth-save, GC or retained-binary validation has been run for this Update;
- no release, Workshop or Runtime environment state has been changed.

The implementation and evidence checkpoints are:

1. Commit `ff0f5fc3` freezes the immutable pre-migration snapshot in the repository. That snapshot and the consolidated manifest bind baseline ref `9fb8d87d5c03916f2ca083ffb235b293aea58d61`, implementation ref `4bc05990668e219409f1d08be4485e67aa34add6` and evidence commit `467ae6ec1e02edfd5916cbd8fd545078046bfefd`.
2. The main implementation landed in `8c78ec6b`, with focused integration and QA-harness fixes through `aca65a15`. Those later fixes do not change the product DLL, mandatory Runtime, manifest, policy or package hashes.
3. [`batch6-autofishing-migration-evidence.json`](../../../tools/release/baselines/batch6-autofishing-migration-evidence.json) is the single G4/G5/G6 product-migration authority. Its checker and nine mutation probes pass, including the source-comparison zero-increment mandatory ProductNative gate; Catalog reports 27 products, 11 public/Workshop products and 47 API rows, while the separate Workshop snapshot/query union contains 21 items.
4. The exact SDK product is policy `doloctown-23762374-autofishing-v1`, package SHA-256 `B16E85AC81595E0376D983D5ABBFFB166C6385E918CBE4ED086F43BC50EAE8B9`, entry SHA-256 `6DC7A786A9C8C1C1BDCF35158026FD5F97C2E335ED427700CCD322CC741FCB4E`, manifest SHA-256 `EA01588C8FE9D891835BCE832E89CAA3E5210F620D9DC35DB74D407824E5ED43`, receipt SHA-256 `43295B09A1A09B8C0D3E2750AEEEDF19F6607A04716324B9CC148786D1ED9E28`, and policy SHA-256 `C434506848F53E21A6371C487B43941FB3408031479FA85E96BB77145AAD5557`.
5. Manager lifecycle passes under `docs/debug/evidence/BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-121144-ded2b298` ([root](../../debug/evidence/BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-121144-ded2b298/)), covering same-process disable, cold disabled and restored enabled. All eight fifth-save behavior profiles pass under `docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-122112-c46597d6` ([root](../../debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-122112-c46597d6/)).
6. The unchanged-package long-run evidence is deliberately composite. The second formal ladder `docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-132436-b7e23d5f` ([root](../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-132436-b7e23d5f/)) supplies passing L0-L3. The L4 failure is resolved by formal L4-only `docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-152146-c761b08d-l4-only` ([root](../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-152146-c761b08d-l4-only/)) / [`GAME-SMOKE/20260721-152148`](../../debug/evidence/GAME-SMOKE/20260721-152148/), which proves one independently QA-originated native cast/recovery loop and zero post-cleanup roots. Formal L5-only `docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-155340-d24150aa-l5-only` ([root](../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-155340-d24150aa-l5-only/)) / [`GAME-SMOKE/20260721-155342`](../../debug/evidence/GAME-SMOKE/20260721-155342/) proves title return/re-entry, six handshakes, four input events, provenance and cleanup. Replaying L0-L3 a third time would not add product evidence because all product/package hashes are unchanged.
7. The compatibility host gate passes with zero public API removals, all eight consumer types loading and `StopOnManualMove` setter reference/resolution `1/1`. The retained ABI authority [`20260715-0012`](20260715-0012-retained-autofishing-abi-host-gate.md) binds SHA-256 `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA`; with Workshop item `3743799721` selected, the current legacy-product canary then loads, warns and completes compatibility configure/enable/disable under Unity Mono in [`GAME-SMOKE/20260721-161256`](../../debug/evidence/GAME-SMOKE/20260721-161256/). The current smoke root proves the selected-item behavior, while the exact-byte identity comes from the retained ABI authority rather than a hash field in that root.
8. A clean detached worktree at source/tooling integration commit `4da7f97d` passed `tools/scripts/test.ps1 -Configuration Release` end to end on 2026-07-21. This includes build, Unit/QA/Doctor, Catalog, Author SDK release/portable, installer and upgrade transaction matrices, Candidate11 byte-bound source transaction, Batch 4 semantic boundary/meta-negative coverage, AutoFishing reproducible SDK release contract, Batch 6 ladder/behavior/Manager static contracts, retained ABI and test-artifact governance. Clean-worktree-only failures found during this final boundary were repaired in focused commits: pre-copy QA host version reading, frozen Author SDK LF/tool-output reproducibility, Candidate11 long-path ADS enumeration and the existing Manager no-PostMessage semantic contract.
9. These results made this Update `implemented`. The independent acceptance linked above subsequently found no P0/P1 issue, rechecked the focused authorities without changing product or Runtime bytes, and promotes this Update to `verified`.

## Implemented changed groups

- `products/first-party/AutoFishing/`: canonical Advanced product, ProductNative source and optional product QA.
- `author-sdk/advanced-reference-policies/`, `author-sdk/schemas/`, `src/DTMAPI.AuthorSdk/` and `src/DTMAPI.Authoring.Contracts/`: exact AutoFishing policy, SDK authoring/build/package authority and immutable Strict/G2 boundaries.
- `src/DTMAPI.Core/`, `src/DTMAPI.InstallDoctor/` and Manager projections in `src/DTMAPI.ModConfigMenu/`: pre-load classification, policy registry, diagnostics and restart state.
- `src/DTMAPI.GameBridge.DolocTown/` and `src/DTMAPI.GameBridge.DolocTown.QA/`: removal of product-only Runtime seams, isolated retained compatibility and optional QA relocation.
- `tools/release/`, `tools/scripts/` and `tests/`: consolidated migration authority, Catalog/package/ABI/transaction gates, resumable per-level validation and focused negative coverage. The exact baseline-to-implementation path set is owned by [`batch6-autofishing-migration-evidence.json`](../../../tools/release/baselines/batch6-autofishing-migration-evidence.json).
- `docs/api/public-api-matrix.md`, this Update, its L4 root-cause Review and the evidence-retention authority: compatibility state, implementation lifecycle and retained formal evidence.

## Rollback

Rollback is product- and policy-scoped:

1. verify the game process is absent and withdraw the SDK-deployed AutoFishing package under the Runtime lock;
2. restore OfficialLocal/profile/save/source state from transaction receipts;
3. revert the AutoFishing product migration and production package wiring;
4. revert only the AutoFishing policy/registry/compiler-surface additions, leaving immutable G2 synthetic authority intact;
5. restore the pre-migration primitive/GameBridge product path if needed for the last known buildable state;
6. retain frozen compatibility API/binary evidence and user config/save files;
7. rerun the focused Strict/G2 admission invariants affected by the rollback; do not replay the historical nine-case G2 matrix unless its Runtime behavior changed.

Do not clean up by deleting user saves/config, Workshop subscriptions, the whole MODS tree, BepInEx Runtime files or another worktree's runtime lock.

## Remaining blockers outside this pilot

- 0.5.5 release remains blocked by its independent release provenance, compatibility, candidate and publication gates even after this pilot succeeds.
- Every other real product remains blocked; AutoFishing success does not auto-admit Zoom, ActionSpeed, MoreSaves, AutoHarvest, AnimalPack or any other product.
- Content Host G7 remains blocked and is outside this Update.
- Frozen fishing compatibility cannot be removed in 0.5.5 without a new explicit breaking-change decision.

## Completion criteria

This Update first becomes `implemented` when all implementation and final-package evidence below are bound to immutable commits. It becomes `verified` only after one independent acceptance review confirms that evidence without reopening the task through a separate audit Update:

- independent AutoFishing policy/registry and canonical SDK package;
- self-contained ProductNative implementation with no GameBridge/primitive/legacy consumption;
- compatibility-only retained ABI with exact old-binary canary;
- one consolidated G4/G5/G6 migration evidence manifest with no new mandatory ProductNative, zero known AutoFishing ProductNative owner/symbol leftovers, relevant ownership paths classified, and existing SDK/Catalog/ABI authorities linked;
- one complete Release suite at the final integrated commit; Windows PowerShell 5.1 duplication is required only for player-distributed PowerShell surfaces;
- fifth-save behavior/lifecycle/cleanup matrix;
- one exact-final-package L0-L5 long-run evidence set; independent per-level runs may form the set when hashes are unchanged, and already passing levels are rerun only after a relevant product/Runtime/package change;
- Doctor/Manager, clean restart, clean exit and source/profile/save restoration.

Completion of this Update supplies G2's real-product proof and closes G3/G4 plus only the relevant AutoFishing slices of G5/G6. It does not by itself make G0-G7 globally complete, make 0.5.5 publishable, or admit another product.

All `implemented` criteria above are satisfied at the immutable source/tooling integration commit `4da7f97d` plus its docs-only closeout. The independent acceptance review named above confirms the bounded evidence and promotes this Update to `verified` without changing the accepted product, Runtime or package bytes.
