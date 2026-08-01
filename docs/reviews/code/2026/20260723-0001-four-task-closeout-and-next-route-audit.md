# Four-Task Closeout And Next-Route Audit

**Review ID:** `20260723-0001`
**Date:** 2026-07-23
**Status:** recorded — NO-GO for declaring all four tasks closed or admitting a sixth product; GO for bounded Host/doc corrections and already-reviewed independent cleanup
**Scope:** commits `1350f5de..c42fc8b4`, current tracked worktree, focused source/test/evidence review only; no implementation, game launch, complete Release, L0–L5, GC ladder or long test

## Source Request

The user reported that the previously listed tasks 1–4 were complete and requested an audit plus the next route. The four task headings are preserved below so later work does not confuse a partial subtask with the whole reviewed phase.

## Baseline

- Branch: `codex/major-update-batch0-20260713`.
- Reviewed HEAD: `c42fc8b4d71336e90ec62f7813f4206790520016`.
- The tracked tree was clean at audit start.
- The unrelated portable reverse-capture Update, tool directory and builder remain untracked and were not reviewed as implementation.
- Current-DLL runtime evidence exists for ItemDisplayName at `GAME-SMOKE/20260723-074311` and for the Compatibility Host Fishing sample at `GAME-SMOKE/20260723-092718`.

## 1. Documentation Truth And Portable-Work Isolation

**Reported completion:** correct the current documents and remove the premature portable-package ledger claim.

The committed monthly-ledger link was removed, so clean-commit authority no longer advertises the unfinished portable package. The local workspace is nevertheless not governance-clean: `docs/updates/2026/20260720-0006-portable-full-reverse-capture-package.md` still exists untracked while the matching index row does not. `check-doc-governance.ps1` therefore reports one unlinked Update and an Update/index count mismatch.

This is not permission to register or commit the portable Update by itself. Its accompanying untracked implementation still has known public/test-branch, sidecar and source-provenance gaps. The coherent choices are to finish and validate that isolated package later, or remove/abandon the complete untracked group. It must not be mixed into the Batch 6 closeout.

**Disposition:** partial locally; committed authority is corrected, but the current workspace document gate is red because the unfinished Update remains present.

## 2. ItemDisplayName Short Runtime Acceptance

**Reported completion:** run the minimal third-save lifecycle acceptance for the shared ItemDisplayName route.

`GAME-SMOKE/20260723-074311` is valid current-DLL evidence. It proves two third-save cycles, Fish and Animal queries, real `DolocAPI.SetEnvCamera` callback invalidation, cache/demand/callback return to zero, title recovery, restoration and clean process exit. The earlier focused source and Unit coverage matches the runtime path rather than calling only the service helper.

**Disposition:** complete. It does not require another game run for this audit.

## 3. Dormant Compatibility Host

**Reported completion:** move the five frozen compatibility executors out of the default-loaded GameBridge while retaining the old provider identities and ABI.

The architecture is materially implemented:

- the `netstandard2.0` Host is shipped outside BepInEx scan paths and has no Mod or `BepInPlugin` identity;
- mandatory GameBridge has no static Host reference and retains thin public proxies;
- Catalog, package, installer, status, Doctor, Manager and collection paths project one optional component;
- exact retained consumer hashes/MemberRefs and all five provider identities have static coverage;
- `GAME-SMOKE/20260723-092718` proves ordinary-start dormancy, first-call byte loading through the Fishing facade under Unity Mono, compatibility cleanup and resident Fishing owner dictionaries returning to zero;
- the measured claim is correctly limited to a 118,272-byte / about 10.90% reduction in the default-loaded GameBridge. The combined shipped GameBridge plus Host is larger than the old GameBridge.

Three acceptance gaps remain; the first and second are the same identity boundary seen from Runtime and Doctor.

### P1 — Actual Assembly Identity Is Checked After Load

The governing [Host Review](20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md) requires the actual component path, length, hash, simple name and assembly version to be validated before `Assembly.Load(byte[])`. `CompatibilityHostBroker.LoadAndCreateBackend` validates receipt text plus path/length/hash, calls `Assembly.Load(bytes)`, and only then calls `ValidateLoadedIdentity` for the loaded simple name, version and target framework.

A coherently wrong package or receipt can therefore place an invalid assembly permanently in the Mono AppDomain before the broker rejects it. Publishing the broker singleton only after validation does not undo that load. Existing negative coverage checks a noncanonical receipt path but does not prove wrong actual name/version/framework bytes are rejected without loading.

The smallest correction is PE metadata inspection of the actual bytes before `Assembly.Load`, reusing an existing generic inspector or a small shared metadata primitive. Add wrong-name, wrong-version and wrong-target-framework negative Units that also prove the candidate assembly was not loaded. Do not add a new receipt family.

### P1 — Doctor Can Report Green For Bytes The Broker Must Reject

`InstalledRuntimeVersionProbe.ValidateOptionalComponents` accepts any non-empty component ID and any path below `DTMAPI/components/`. It compares the actual PE simple name only with the receipt's freely supplied `AssemblyName`, compares FileVersion, but does not freeze the one Catalog component ID/path/name or compare actual AssemblyVersion and actual TargetFramework even though the metadata inspector already exposes them.

The positive Doctor test demonstrates the false-green path: it renames `DTMAPI.GameBridge.DolocTown.dll` to the Compatibility Host filename, writes that DLL's actual GameBridge identity into both receipts, and expects `Consistent`. Runtime's broker would reject the same installation on first old-ABI call. Doctor therefore does not yet guarantee player-start compatibility with the broker.

Use the Catalog-owned exact component ID, path, simple name, assembly version and framework policy in Doctor, require both receipts to agree on all identity fields, and add negative tests for substituted bytes plus receipt identity/version/framework drift. This should share the broker's metadata facts where practical, not introduce a separate Host schema.

### P1 — Upgrade Matrix Does Not Exercise The Host

The closeout Update says the PowerShell 7 and Windows PowerShell 5.1 15-case Runtime upgrade matrices cover the optional component. `tools/scripts/test-runtime-upgrade-transaction.ps1` currently builds a fixture manifest containing only the five `IncludedAssemblies`; it adds neither Host bytes nor `OptionalComponents`. Test mode permits that empty projection. The matrix therefore moves an empty components directory and does not prove real Host receipt/bytes commit, rollback, interrupted recovery or uninstall.

Extend the existing focused transaction fixture with one old-component sentinel, one valid new Host component and its exact existing receipt. Cover the component move/commit failure boundaries, interrupted recovery, final state/receipt equality and uninstall cleanup under the two supported PowerShell hosts. This is a focused script test, not a reason to run the complete Release suite or game.

Only Fishing has current post-extraction Unity invocation evidence. The other four domains have exact retained-binary/static and Unit coverage plus earlier pre-extraction product evidence. That is sufficient for one representative Mono load proof, but documentation must not say that all five retained binaries executed in the new Host in game. A small retained-binary/four-domain compatibility pass may be deferred to the release-candidate boundary.

**Disposition:** implemented and promising, but not verified/closed until the three P1 focused gaps pass.

## 4. API, QA And Animal Independent Cleanup

**Reported completion:** add frozen API warnings/disposition, remove dead input diagnostics, make ActionSpeed QA summaries opt-in and optimize the Animal product's 80 ms refresh.

The first three bounded changes are present and focused checks pass:

- the five retained compatibility API families carry warning-bearing `Experimental + Frozen` metadata without an ABI deletion;
- dead `ReflectedUnityInput` counters/methods were removed;
- ActionSpeed expensive QA summaries are opt-in.

The Animal optimization did not occur. `AnimalHusbandryNativeRuntime.Update()` still calls `Refresh(false)` on the 80 ms schedule, and the refresh path still performs type resolution, reflection, progress/signature construction, child-text rewrites and color parsing on each admitted pass. No Animal source file changed in the reviewed range.

The broader reviewed phases also remain open: Core retains the old string input/fatal-window endpoints and their tests, while Phase 4 still has `StableCandidate` CustomEntity declarations, duplicate provider registrations, the InstantSave-dependent AutoHarvest path and unresolved helper owner/thread/stale-owner classification. These were not all part of the smallest cleanup subtask, but they prevent describing Phase 1 or Phase 4 as complete.

**Disposition:** partial. Frozen metadata, dead Bootstrap diagnostics and ActionSpeed opt-in are complete; Animal and the documented Phase 1/Phase 4 tails remain open.

## Cross-Cutting Documentation Ownership Finding

Update `20260722-0004` began as the five-product/ItemDisplayName correction but now owns the Compatibility Host, public API metadata, input diagnostics and ActionSpeed QA cleanup as one `verified/closed` lifecycle. The Host Review required a later separately authorized implementation Update, and the API/QA Reviews describe independent slices. Absorbing them into one old Update makes rollback and acceptance ambiguous and is the same type of management overgrowth the assurance-proportionality rules are intended to prevent.

Do not rewrite the implementation commits or create new receipt/checker families. Correct lifecycle ownership in documentation:

1. return Update `20260722-0004` to its five-product/ItemDisplayName scope, which can remain verified on `074311`;
2. create one Compatibility Host implementation Update, initially `implemented/open`, reusing all existing evidence and closing only after the two focused P1 corrections;
3. create one small API/QA cleanup Update for the already-complete metadata and diagnostics slices, linked back to their Reviews;
4. leave Animal optimization and the remaining Phase 1/Phase 4 work for their own already-reviewed implementation scopes.

This is a document-ownership repair, not a request for duplicate audits or duplicate evidence.

## Validation Performed

- Compatibility Host Release build: PASS, zero warnings and errors.
- focused Unit `compatibility-host`: PASS.
- focused Unit `api-metadata`: PASS.
- Install Doctor tests: PASS, 11 cases.
- Catalog checker: PASS, `products=27`, `public=11`, `workshop-items=21`, `api-rows=48`.
- `git diff --check`: PASS.
- document governance: FAIL, two related findings caused only by the untracked portable Update missing its monthly index row (`495` Update files versus `494` rows).
- no game, complete Release, L0–L5, GC ladder or long test ran during this audit.

Passing focused tests do not cover the three P1 gaps described above; the current Doctor positive test actually preserves one of them.

## Route Decision

The branch may continue with bounded corrective work and read-only preparation, but the Host baseline must not remain frozen as fully verified and no sixth product should be admitted yet.

Recommended order:

1. repair Host pre-load metadata validation, align Doctor with the exact broker identity, and extend the existing component transaction fixture; run only Host/Doctor/Catalog/transaction focused checks;
2. split the lifecycle-document ownership and resolve the isolated untracked portable group without registering an unfinished Update;
3. complete the Phase 1 Core old-endpoint/test migration and the Phase 4 contract-only API cleanup;
4. optimize Animal entirely inside `AnimalHusbandryProgress`, then run focused Unit/source checks and one short third-save repeated-switch acceptance only if the behavior path changed;
5. reconcile the already-reviewed G1 Oil/Mine/Audio/Pet/Vehicle ownership decisions into the machine-readable contract without implementing Oil, Mine, a generic host or a sixth product;
6. remeasure the default Runtime after those closures, then decide the next product/capability-domain extraction.

The four-domain retained compatibility run belongs near the final 0.5.5 candidate, not after each focused correction. Content Host G7, a sixth real product, complete Release and 0.5.5 publication remain blocked by their own gates.

## Later Resolution

This Review remains the historical closeout finding at `c42fc8b4`; its issue
order and NO-GO are not rewritten. The Host prerequisites were closed by Update
[`20260723-0003`](../../updates/2026/20260723-0003-moresaves-admission-prerequisites.md),
Animal received its own ProductNative optimization, and MoreSaves later passed
a separate admission and implementation lifecycle. Review
[`20260723-0003`](20260723-0003-sixth-product-commit-range-audit.md) records the
subsequent sixth-product corrections. The isolated portable reverse-capture
work remains outside all of those authorities.
