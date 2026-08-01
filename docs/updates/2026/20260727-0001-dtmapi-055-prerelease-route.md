# 20260727-0001: DTMAPI 0.5.5 Pre-Release Route

## Metadata

- Update ID: `20260727-0001`
- Date: `2026-07-27`
- Lifecycle Status: `implemented`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `partial`
- Related Issue State: `open`
- Area: release/runtime/0.5.5/prerelease/compatibility/gc/workshop/content-host
- Source: User decision to resume the bounded work from the two remaining P2 truth corrections through the pre-Release candidate-freeze boundary, publish Runtime before Advanced products, require retained Manbo compatibility, freeze public GC wording, and defer animal/audio content-host work until the next version.
- Related Review: [Lifecycle Closeout, Product Inventory And Release Route Audit](../../reviews/code/2026/20260727-0004-lifecycle-closeout-product-inventory-release-route-audit.md)
- Related Root-Cause Review: [Author SDK Local-Install Crash-Recovery Review](../../reviews/code/2026/20260728-0001-author-sdk-local-install-crash-recovery-review.md)
- Related RC Cutoff Review: [Workshop RC Cutoff And Retained Manbo Input Review](../../reviews/code/2026/20260728-0002-workshop-rc-cutoff-and-manbo-input-review.md)
- Related Completion Audit: [DTMAPI 0.5.5 Pre-Release Route Completion Audit](../../reviews/code/2026/20260728-0004-dtmapi-055-prerelease-route-completion-audit.md)
- Related Correction Acceptance Audit: [DTMAPI 0.5.5 Pre-Release Correction Acceptance Audit](../../reviews/code/2026/20260729-0001-dtmapi-055-prerelease-correction-acceptance-audit.md)
- Related Release-Entry Audit: [DTMAPI 0.5.5 Release Entry And Transition Matrix Audit](../../reviews/code/2026/20260729-0002-dtmapi-055-release-entry-and-transition-matrix-audit.md)
- Related Release-Blocker Review: [DTMAPI 0.5.5 Release Blocker Independent Review](../../reviews/code/2026/20260729-0003-dtmapi-055-release-blocker-independent-review.md)
- Related MoreEquipment Deferral Review: [MoreEquipmentSlots Production Transaction Three-Pass Reaudit](../../reviews/code/2026/20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)
- Plan: [DTMAPI 0.5.5 Pre-Release Roadmap](../../planning/20260727-dtmapi-055-prerelease-roadmap.md)

## Summary

This Update owns the bounded 0.5.5 pre-release route. Its originally
unauthorized complete Release and player-like package stages were later
explicitly run against the frozen candidate and passed as recorded below. The
current documentation-only continuation does not authorize another complete
Release, mutation of the local Steam upload directory, Steam upload or
post-upload subscription validation.

The 2026-07-28 completion audit reopened this route at `implemented /
provisional`. Its Author SDK snapshot/prepare findings, evidence-retention
inventory, three external-consumer actual-load lane and documentation truth
corrections are now implemented. Those fixes changed Author SDK and Runtime
bytes, so both artifacts were re-frozen and every Runtime-bound Step 6 game
gate was repeated against the new candidate. The ten product ZIPs remain
unchanged. The route cannot return to
`verified / READY-FOR-FINAL-RELEASE` until an independent focused re-review
accepts these corrections.

The 2026-07-29 correction acceptance audit then reproduced one remaining
Author SDK P1: a pending product's whole-file `sourceBefore` recovery could
erase a later, legal source selection made for another product. Commit
`c32a274f` now places every source-state mutation and every other
`install-local` behind one game-root pending-marker barrier, validates all
deployment journals before that decision, and proves the current source state
is the exact transaction pre-state or derived post-state before recovery moves
anything. The Author SDK alone was re-frozen; Runtime, product packages and
their accepted game evidence remain unchanged. This correction still awaits
the audit's required independent focused re-review.

The subsequent release-entry audit found two bounded player-delivery
blockers. Commits `cc6b872d` and `493de436` now require an exact
`CommittedLocalDevelopment` reconciliation before the outer installer can
register success, cover interruption plus a corrupt report under both
PowerShell hosts, and constrain the official player-package root to BAT
entrypoints `1` through `4`. The independent blocker review accepted those
changes with `P0=0 / P1=0 / P2=0`. After one non-acceptance run exposed a
stale evidence allowlist, the prescribed focused/tail route also corrected
two latent release-checker inventories. One final from-start Release suite
then passed. Player-like package acceptance, manual Unity transitions and all
Steam operations remain separate follow-up boundaries, so this Update stays
`implemented / open`.

Step 1 is complete at source/unit scope. The Demand Router is now GameBridge's only active frame-scheduling truth; direct feature fanout remains a separate lifecycle/API/on-demand-Hook dispatch counter rather than a second scheduler. The retired bucket option is accepted only long enough to remove it from an existing config. AutoHarvest is a Catalog-bound `ApiDemandSample` / `NeverPublish` research input with zero real consumers and no admitted ProductNative target. The frozen 2026-07-20 Phase 0 receipt remains byte-for-byte reproducible through an explicit historical projection.

Step 2 is also complete within a deliberately small inventory. The internal Core CLR/file name `RefactorScaffoldOptions` is now `RuntimeSubsystemOptions`, while `[DataContract(Name = "RefactorScaffoldOptions")]`, every `DataMember`, `refactor-scaffold.json`, environment variable, feature/Hook status and report label remain unchanged. The frozen fishing API facade file now matches its existing `FishingAutomationCompatibilityAdapter` type, and the mandatory legacy-named fishing broker proxy explicitly states that it does not own the heavy executor. High-fanout internal access aliases/private fields and compatibility-visible diagnostic strings were intentionally left alone because renaming them offered no 0.5.5 risk reduction.

Step 3's first implementation failed independent review because the outer installer was still a second lifecycle owner: it called deployment, source selection and rollback through separate SDK locks, could not distinguish `update.commit.after-journal` from an uncommitted failure, accepted a mutable package path after preflight, and could overwrite concurrent source-state work with a whole-file backup. The correction removes that outer transaction entirely. `install-to-game.ps1` now owns only the Catalog/zip identity, version and SHA-256 projection. Author SDK's single `install-local` command holds one game-root lock while it copies the expected package to a random immutable input, verifies that hash plus the existing Advanced receipt/marker/entry/native policy, chooses deploy/update, commits LocalDevelopment selection, and restores the exact prior destination, journal and source-state bytes on failure. The existing deployment journal and source-state remain the only authorities; no new receipt, revision or checkpoint family was introduced.

The first corrected transaction matrix installed a real SDK-generated Zoom Advanced package and proved ordinary exceptions restored the old tree and both state files byte-identically. A second independent review correctly rejected that as process-crash proof: the exact state-file snapshots still existed only in memory, and the outer installer replayed the complete mutation after an unreadable JSON report.

The second correction extends the existing deployment journal to schema 3 with one embedded, fail-closed `localInstall` transaction. Before any destination move it persists the exact previous journal/source-state bytes, prior/next deployment records, expected version/package hash and owned staging/recovery/failed paths. A process-style interruption deliberately bypasses same-call rollback and leaves `recovery-required`; a later, separate `recover` command restores the prior destination plus exact state-file bytes. On success the SDK commits Local Development selection and atomically clears `localInstall`. The outer installer invokes `install-local` once and uses the new read-only `install-local-status` only when the response cannot be parsed. No mutation is replayed, and no second receipt/checkpoint family exists.

A third independent pass found one final shared boundary: `File.Replace` can already publish the terminal journal and then report a write-after exception. Both terminal paths now perform exact read-only reconciliation. If destination, journal and source state equal the new committed state, install returns success; if all three equal the old pre-state, recovery returns success. A failure is called retryable only while the matching schema-3 `localInstall` authority still exists. Recovery clears that authority last, after destination, source state and owned-tree cleanup.

The final focused matrix adds a valid wrong-product package with the correct hash, an unrelated pre-existing Local Development selection, five durable interruption points, recovery failure/retry, terminal install and terminal recovery write-after reconciliation, corrupt-report reconciliation, and the original ordinary failure windows. Every rollback/recovery case preserves both products' authority exactly; the final update preserves `Installed` deployment status, `LocalDevelopment` source status and the exact committed package identity/version/hash.

Step 4 freezes the RC discovery boundary without treating current Steam search relevance as product authority. The exact `DTMAPI` query plus three reviewed compatibility identities yields 22 authoritative public rows; all four queries now paginate through an empty page and yield a noisy 37-item union, including the new `DolocTownHungerSystem` BepInEx-only false positive. The extra second-page row, `3749143385`, is an already-reviewed official JSON-content-only Mod with no DTMAPI manifest or assembly. The read-only subscription cache contains 44 directories and the same fifteen real `DTMAPI.Abstractions` consumers. The existing retained ABI harness now resolves all 23 MemberRefs from the four exact external consumer DLLs against the candidate and records them beside the eleven first-party products in the same schema-3 retained-runtime report.

The retained Manbo input is Workshop `3746319981`, `0.1.0-dtmapi`, seven files, tree SHA-256 `23a3209e75788b68041f4e1eecfe81550f87d6579893272af67711b2c0bfc40e`, and entry DLL SHA-256 `AB85C0BAB39702E7FE2689BB4D528B6EF3726F0BB272F6F172CFFE8A57A17EC4`. A different eight-file stale local copy currently exists in the player's `MODS` tree. It was not changed in this step; the step-6 runner must isolate it as a non-save test asset, prove one exact Workshop load source, and restore it only after process exit and the `NoNativeSave` archive proof.

The authorized step-4 review reopened two P1 boundaries. The capture now
paginates each search through its empty terminal page and freezes each
complete ID-set digest. Runtime `0.5.2-alpha` is no longer recoverable only
from Steam's mutable cache: the exact 46-file payload is retained in a
read-only private/non-distribution archive outside the source/distribution
tree, with internal `SHA256SUMS`, Catalog-bound archive bytes/SHA-256 and a
fail-closed verification entrypoint. The same correction also binds the
Catalog snapshot authority pointer to the file actually loaded by its gate
and cross-checks every retained external reference version and MemberRef
count/list digest.

Step 5 originally froze the source/product candidate at clean commit
`3f5cb3268f3e786a13594fae44e542c0c9d2657e`. Runtime/API remains
`0.5.5`, file version `0.5.5.0` and compatibility assembly identity
`0.5.3.0`. The ten existing PublicWorkshop Advanced products selected for
later waves now project their current source, official-info and publish text
to `1.0.0`; their historical `publishedVersion`, G2 receipts, retained
consumer identity and rollback bytes remain unchanged. Manbo remains the
retained `0.1.0-dtmapi` compatibility input and is not part of the Advanced
projection.

The first Step 5 independent pass reopened one path-authority boundary:
product rollback `ArchiveRoot` could equal the repository exactly, and both
rollback tools accepted a lexical external path whose existing parent was a
junction back into the repository or `dist`. The shared guard now rejects
repository equality/descendants and every existing reparse-point ancestor
before verification or creation. Current PowerShell and Windows PowerShell
5.1 negative tests cover both equality and junction routes for both archive
tools; the default eleven retained artifacts still verify unchanged.

That original corrected clean commit produced the Runtime Workshop directory, Bootstrap,
Abstractions, Core, GameBridge, ModConfigMenu, the dormant-shipped
Compatibility Host, Player Doctor, one Author SDK and ten independent
Advanced product ZIPs. Runtime package tree
`7c5148d1247cdd8faac63148e014801c1f0389fd210e677be18b3f2037cb8fab`
contains 31 files / 71,447,151 bytes; its release manifest SHA-256 is
`acb152dc216def1fd5a1ce6bc6d323ca377508d409fb23ecceb0d053bbe42b91`
and records BuildCommit `3f5cb3268f3e`. Author SDK ZIP SHA-256 is
`4d81906e6d8e73e0ef35fb75b89c4592e784bb612f01564996b6eea0725ce950`
for that original Step 5 build. Step 7 review first invalidated that SDK
candidate; the completion audit later invalidated the Runtime candidate as
well. Their corrected replacements are frozen separately below. All ten
product bytes remain unchanged.
The ten product package hashes are listed under Validation below. These are
local pre-release artifacts, not Steam uploads or new compatibility
authorities.

The exact retained ABI gate was rerun against the assemblies inside that
Runtime candidate, not a source-tree build substitution. It reports zero
public API removals, eleven exact first-party retained products, four exact
external consumers and all `23/23` external DTMAPI MemberRefs resolved. A
separate ZIP/path scan rejected QA, test, testmods, negative and fixture
entries across the Runtime directory and all ten product packages. Runtime
still contains exactly the five mandatory assemblies, zero bundled products
and one dormant-shipped Compatibility Host.

Step 6 now binds its runtime evidence to those exact frozen bytes. The
current-candidate no-demand run completed 300 warm-up plus 10,000 measured
frames with no optional demand/updater membership and zero optional Hook,
directory, reflection, retained-callback or native-updater work. The retained
Manbo `0.1.0-dtmapi` seven-file Workshop package was selected exactly once
from Workshop `3746319981` while its stale Local duplicate was isolated; its
Entry, audio registration and WAV-ready path completed without ABI/provider/
Loader/fatal failure.

The active-gameplay focus leased the existing ActionSpeed and AutoFishing
managed deployments instead of adopting their recovery history. It installed
the exact Step 5 product candidates, isolated every other receipt-bound
managed product, held one shared Runtime lock, and restored the original
product trees, deployment journals and Author source-state exactly after all
game processes exited. ActionSpeed completed the selected L0, L1, four L3
workloads, L4 and L5 stages. AutoFishing completed L1, L3, L4 and L5 with the
real LongRun minimum of five warm-up and ten measured fish. All twelve smokes
were `NoNativeSave`, proved player archives and committed sidecars unchanged
before cleanup, performed no player archive writeback and exited cleanly.

The authorized Step 6 review accepted the runtime observations but reopened
two evidence/lifecycle boundaries. First, the outer product/journal lease
recorded its move flags only after mutation and had no separate interrupted-run
recovery entrypoint. The same lease file is now schema 2: every original,
candidate and restore move writes its pending phase before the move, binds the
frozen package hash plus entry ledger, and can be resumed only from the exact
transaction paths. `-RecoverOnly` requires this worktree's stale Runtime lock,
an absent owner/game process and the recorded configured game directory; it
releases the lock only after exact product, journal and source-state checks.
Separate Windows PowerShell child processes now terminate after each of seven
move/publication boundaries, and a later process restores the exact original
tree, journal and pre-existing recovery-artifact ledger.

Second, r14's original AutoFishing stage receipts were generated before the
final target-driven bound correction and therefore still record
`MaximumMeasurementSeconds=600`. They are not cited as proof of the corrected
bound. `auto-fishing/final-validator-reevaluation.json` binds the unchanged raw
and original stage file length/SHA-256 values and replays the current validator
without launching the game or mutating Runtime/product/SDK state. It records
the old `600`, the final `190`, and passing terminal observations of about
`22/10/13/13` seconds for L1/L3/L4/L5. This is an interpretation receipt for
the existing run, not replacement runtime evidence.

Step 7 first recorded the provisional pre-release freeze in this same Update.
The exact clean input HEAD is `19e4a74c2146cd9686d8c8de908a8fc6238cfaa1`;
at that first freeze Runtime and ten product candidates were built from
`3f5cb3268f3e786a13594fae44e542c0c9d2657e`. Initial freeze commit
`b2730ea85287a66eb347ca115b8e772c1841d783` remains historical
`implemented / PROVISIONAL-PRE-RELEASE-FREEZE`, not
`READY-FOR-FINAL-RELEASE`. Its document review passed, but code/artifact and
route reviews reopened the Author SDK candidate because the shipped journal
schema/README still described schema 2 while the executable wrote schema 3,
and a damaged but deserializable `localInstall` marker could still be called
retryable without full structural validation. `f384d233` closes that
lifecycle/schema boundary and `add0dba3` closes the Windows PowerShell
strict-mode release checker. The replacement Author SDK is now exactly
first re-frozen from `add0dba3da52bda5decc3542ea92341b4b357166`; its focused
Release, Unit, artifact-parity and dual-host real-Zoom transaction gates pass.
Runtime, ten product packages, rollback inputs, wave membership,
three-language copy, evidence, risks and deferrals were recorded below. A
later completion audit found three P1 and four P2 route gaps. The replacement
Runtime/SDK freeze and repeated Runtime-bound gates are recorded in the
completion-audit correction section; the current state remains
`implemented / provisional`.

On 2026-07-30 the user narrowed the publication set without changing the
other release waves. New MoreEquipmentSlots `1.0.0`, its migration and its
Steam update are deferred; Workshop `3744059735` remains
`0.3.1-dtmapi`. Runtime 0.5.5 must still support that exact old package through
the frozen ABI/Compatibility path. The selected Runtime input is the already
frozen `f96c9cc6` 30-file artifact below, not a rebuild from later
MoreEquipmentSlots/Compatibility Host source. That later source and the
generic protected-storage discussion are next-version inputs.

Runtime `0.5.5` will be released first. Existing Advanced products will then
move to `1.0.0` in user-selected waves: AutoFishing alone, followed by the
remaining eight public Advanced products in one release window with
independent packages and rollback. New MoreEquipmentSlots `1.0.0` is not in a
0.5.5 release wave. The retained Manbo `0.1.0-dtmapi` and MoreEquipmentSlots
`0.3.1-dtmapi` subscription packages are mandatory Runtime compatibility
inputs and are not rebuilt for this release.

CustomAnimals, AudioReplacement, AnimalPack, G7, Oil, Mine and
StrongPlantingGun publication work are explicitly deferred to the next or
later version. BGM and ShellCrab remain non-published capability/prototype
work.

## Decisions

- The current implementation scope is:
  1. dead bucketed-update truth cleanup and current AutoHarvest contract correction;
  2. one bounded private/internal naming and readability pass;
  3. the Author SDK source-preflight/install-order tail;
  4. the public consumer and subscription cutoff refresh;
  5. exact 0.5.5 candidate construction and focused Catalog/ABI/Doctor/package/ownership gates;
  6. one current-candidate Manbo `NoNativeSave` compatibility smoke;
  7. bounded AutoFishing/ActionSpeed active-gameplay GC validation;
  8. final candidate and three-language copy freeze.
- One final complete Release and the frozen-candidate player-like Workshop
  package matrix are now accepted below. Actual upload-directory staging,
  Steam upload and post-upload subscription validation remain separate
  publication stages.
- The Manbo gate uses Workshop `3746319981`, version `0.1.0-dtmapi`, minimum DTMAPI `0.5.2-alpha`, and its retained binary/tree. Source recompilation is not compatibility evidence.
- The Manbo minimum gate proves load and audio-provider registration without errors; it does not require a paper-box interaction.
- The exact three-language public conclusion is frozen only in [0.5.5 Workshop Update Copy](../../releases/0.5.5-workshop-update-copy.md). It does not mean that every Unity/Mono GC issue or crash has been solved. Contradictory final-candidate evidence stops the route and returns the wording to user authority.
- Steam update text is a manual operation. Package metadata is not treated as proof that Steam's visible language pages changed after the official game's recent update.
- Subscription count is not a Catalog fact. The user-selected wave order supersedes the historical wave order without rewriting the old decision record.
- The start-of-route independent audit corrected two ordering boundaries: step 4 freezes the retained Manbo input and test plan while step 6 runs it against the final candidate; step 7 first creates an `implemented` provisional freeze, and only the post-step-7 independent audit can promote it to `READY-FOR-FINAL-RELEASE` / `verified`.

## Post-0.5.5 Direction

- CustomAnimals and AudioReplacement may remain DTMAPI-maintained and distributed with Runtime as optional, demand-loaded foundation components.
- With no animal/audio content demand, they must not load their implementation, install hooks or run per frame.
- A future G7 Content Host may own that optional substrate. G7 remains blocked for 0.5.5 and this Update does not admit it.
- AnimalPack will combine Hatch, Mole, Drecko and OilFloater, prove multi-species official JSON in one Mod and produce an author tutorial.
- Species, probability, economy, product and audio-mapping policy remains in AnimalPack, Manbo or another content product rather than the DTMAPI substrate.
- ShellCrab is not published. Oil and Mine publication work remain later.
- The generic protected-storage API discussion is paused. 0.5.5 declares no
  `IProtectedStorageApi`; the frozen Equipment API remains an exact old-binary
  compatibility surface, not a promoted shared storage capability.

## Changed Files

- `docs/planning/20260727-dtmapi-055-prerelease-roadmap.md`
- `docs/updates/INDEX-2026-07.md`
- `docs/releases/0.5.5-workshop-update-copy.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md`
- `docs/reviews/code/2026/20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md`
- `docs/reviews/code/2026/20260727-0004-lifecycle-closeout-product-inventory-release-route-audit.md`
- `docs/reviews/code/2026/20260728-0001-author-sdk-local-install-crash-recovery-review.md`
- `docs/reviews/code/2026/20260728-0003-prerelease-no-demand-managed-product-isolation.md`
- `docs/reviews/code/2026/20260728-0004-dtmapi-055-prerelease-route-completion-audit.md`
- `docs/reviews/code/2026/20260728-0005-retained-external-consumer-native-admission-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
- `docs/debug/protocols/evidence-retention.md`
- `docs/debug/evidence-retention-allowlist.json`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/DTMAPI.Core.csproj`
- `src/DTMAPI.Core/Manifesting/ManagedModClassification.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.Core/Runtime/RuntimeSubsystemOptions.cs` (internal rename from the preceding path)
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/GameBridgeFeatureContract.cs` and the 14 feature declarations that consume it
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/FishingAutomationCompatibilityAdapter.cs` (file rename only)
- `src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/LegacyFishingAutomationServiceProxy.cs`
- `src/DTMAPI.AuthorSdk/AuthorApplication.cs`
- `src/DTMAPI.AuthorSdk/AuthorFileTreeDigest.cs`
- `src/DTMAPI.AuthorSdk/AuthorStateInfrastructure.cs`
- `src/DTMAPI.AuthorSdk/DeploymentService.cs`
- `src/DTMAPI.AuthorSdk/SourceStateService.cs`
- `src/DTMAPI.Authoring.Contracts/AuthorContracts.cs`
- `author-sdk/README.md`
- `author-sdk/schemas/deployment-journal.schema.json`
- `tests/DTMAPI.AuthorSdk.Tests/Program.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tests/DTMAPI.UnitTests/Batch6AdvancedRuntimeTests.cs`
- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `tools/scripts/build-batch6-phase0-baseline.ps1`
- `tools/scripts/test-batch6-phase0-contract.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/common.ps1`
- `tools/scripts/test-developer-official-local-install-transaction.ps1`
- `tools/scripts/test-player-runtime-only-uninstall.ps1`
- `tools/scripts/capture-workshop-public-metadata.ps1`
- `tools/scripts/freeze-runtime-rollback-archive.ps1`
- `tools/scripts/freeze-product-rollback-archives.ps1`
- `tools/scripts/test-runtime-rollback-archive.ps1`
- `tools/scripts/test-retained-autofishing-abi.ps1`
- `tests/DTMAPI.AbiCompatibilityHarness/Program.cs`
- `tools/release/baselines/workshop-public-metadata-20260728.json`
- `tools/release/baselines/retained-runtime-052-public-api-audit-20260728.json`
- `tools/release/dtmapi-product-catalog.json`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/check-product-catalog.ps1`
- `tools/scripts/check-author-sdk-release.ps1`
- `tools/scripts/test-batch6-actionspeed-advanced-product.ps1`
- `tools/scripts/test-batch6-animalhusbandryprogress-advanced-product.ps1`
- `tools/scripts/test-batch6-fishbreedingassistant-advanced-product.ps1`
- `tools/scripts/test-batch6-oneactioncomplete-advanced-product.ps1`
- `tools/scripts/batch5-gc-source-transaction.ps1`
- `tools/scripts/batch6-autofishing-runtime-transaction.ps1`
- `tools/scripts/run-batch5-gc-ladder.ps1`
- `tools/scripts/run-batch6-autofishing-gc-ladder.ps1`
- `tools/scripts/run-prerelease-active-gc-focus.ps1`
- `tools/scripts/run-batch5-no-demand-profile.ps1`
- `tools/scripts/test-batch5-no-demand-profile.ps1`
- `tools/scripts/run-prerelease-manbo-compatibility.ps1`
- `tools/scripts/run-prerelease-external-consumer-compatibility.ps1`
- `tools/scripts/build-evidence-retention-allowlist.ps1`
- `tools/scripts/cleanup-duplicate-runtime-evidence.ps1`
- `tools/scripts/test-runtime-evidence-retention.ps1`
- `tools/scripts/test-batch5-gc-ladder.ps1`
- `tools/scripts/test-batch6-autofishing-gc-ladder.ps1`
- `tools/scripts/test-prerelease-active-gc-focus.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/test-player-doctor-packaged-entrypoints.ps1`
- `tools/scripts/check-release-contract.ps1`
- `tools/release/batch4-production-qa-semantic-inventory.json`
- `products/first-party/ActionSpeed/{README.md,manifest.json,official-info.json}`
- `products/first-party/AnimalHusbandryProgress/{manifest.json,official-info.json}`
- `products/first-party/AutoFishing/{manifest.json,official-info.json}`
- `products/first-party/FishBreedingAssistant/{README.md,manifest.json,official-info.json}`
- `products/first-party/OneActionComplete/{README.md,manifest.json,official-info.json}`
- `docs/reviews/code/2026/20260728-0002-workshop-rc-cutoff-and-manbo-input-review.md`
- `docs/reviews/code/2026/20260729-0001-dtmapi-055-prerelease-correction-acceptance-audit.md`
- `docs/reviews/code/2026/20260729-0002-dtmapi-055-release-entry-and-transition-matrix-audit.md`
- `docs/reviews/code/2026/20260729-0003-dtmapi-055-release-blocker-independent-review.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix-history-accepted-20260719-through-20260723.md`
- `docs/debug/regressions/smoke-matrix.md`
- this Update and `docs/updates/INDEX-2026-07.md`

No public API, product asset, package, game directory, save, subscription directory or Workshop state changed in step 1.

## Validation

- `tools/scripts/check-doc-governance.ps1`: passed (`Document governance: OK`, 6,030 checks).
- `src/DTMAPI.Core`, `src/DTMAPI.GameBridge.DolocTown`, and `tests/DTMAPI.UnitTests` Release builds: passed; the Unit project retained ten pre-existing nullable warnings in DebugConsole source and had zero errors.
- focused Unit with `DTMAPI_UNIT_TEST_FOCUS=prerelease-step1`: passed.
- `tools/scripts/test-batch6-phase0-contract.ps1`: passed; the 23-domain historical receipt remained reproducible at `653487b7463778c23e9b96aed9ef713364def22a`.
- `tools/scripts/check-product-catalog.ps1`: passed (`products=27`, `public=11`, `workshop-items=21`, `api-rows=48`).
- `tools/scripts/run-game-smoke.ps1` PowerShell parse: passed.
- `git diff --check`: passed; only working-copy line-ending notices were emitted.
- Step 1 independent review: no P0/P1; its two documentation-only P2 wording findings were corrected by separating frame scheduling from direct fanout and accurately recording the ISSUE-010 change.
- Step 2 Core/GameBridge/Unit Release builds: passed; Core and GameBridge had zero warnings/errors, while Unit retained the same ten pre-existing DebugConsole nullable warnings and had zero errors.
- focused Unit with `DTMAPI_UNIT_TEST_FOCUS=prerelease-step2`: passed. Config creation, serialized member names, environment overrides and old-key migration behavior remained covered.
- Step 2 boundary guard: no `DTMAPI.Abstractions`, public ID, manifest, Catalog, config path/key, sidecar or receipt changed; the CLR rename explicitly preserves the old DataContract name.
- Step 3 first independent review: no P0; three P1 and one P2 were accepted. The step was reopened because commit-after-journal reconciliation, package TOCTOU binding, source-state single ownership and the described test coverage were incomplete.
- Step 3 second independent review of commit `5f84bc5e`: no P0; two P1 and one P2 were accepted. The step was reopened again because an unreadable JSON report replayed the mutating command, the compound pre-state existed only in process memory, and wrong-product/cross-product source-state behavior lacked direct tests.
- Step 3 third independent review of commit `dc2d0178`: no P0; one P1 with two shared terminal-write windows was accepted. The step remained open because install commit and recovery journal replacement could already be durable while a write-after fault was still reported as `recovery-required` / `blocked-preserved`.
- Step 3 terminal-window re-review of commit `f4c6d2b8`: no P0/P1. One P2 reporting ambiguity was accepted and corrected: a failed read-only reconciliation now reports `recovery-required` / `blocked-preserved` only when the exact schema-3 transaction marker can still be read; otherwise it reports `authority-unknown-fail-closed` and makes no recovery claim.
- Corrected Author SDK Release build: passed with zero warnings/errors.
- Final `DTMAPI.AuthorSdk.Tests`: passed in 25.5 seconds. The focused lane covers immutable-copy hash mismatch; a valid wrong-product package with its correct SHA-256; an unrelated committed Local Development selection; ordinary `install-local.after-deployment`, `install-local.source.before-state`, `install-local.after-source-selection` and `update.commit.after-journal` failures; durable process-style interruptions at compound prepare, update publication, update commit, post-deployment and post-source-selection; a recovery failure before final journal restore followed by retry; fault/crash after terminal install journal publication; a fault after terminal recovery journal publication; separate-command recovery; and exact read-only committed/pre-state reconciliation.
- Step 3 Windows PowerShell 5.1/current PowerShell syntax validation: passed for the installer and focused transaction scripts.
- Final `tools/scripts/test-developer-official-local-install-transaction.ps1 -Configuration Release -ManagedAuthorSdk`: passed under two distinct PowerShell hosts (`hosts=2`, 310.7 seconds). Each host ran the existing generic directory transaction cases plus the real SDK-generated `DTMAPI.ZoomMod` seed/update lane. Missing zip, corrupt zip, Catalog/package version mismatch and `AfterPackagePreflight` left the exact destination tree, deployment journal and source state unchanged. Four ordinary SDK failure windows restored the same hashes. Two process-style interruptions per host left durable recovery state; a separate SDK process restored the exact pre-state, including one injected exception after the old terminal journal was already published. A real Advanced update also injected `install-local.commit.after-journal` and reconciled the exact committed terminal state. The final update intentionally corrupted the first command's JSON after its single commit; the outer installer did not replay it and succeeded only after read-only journal/source reconciliation.
- The managed fixture used a random isolated sibling below the validated Steam `steamapps/common` root because Advanced receipts require the real appmanifest/build boundary. It copied only receipt-listed `0Harmony` and `Assembly-CSharp` reference bytes, kept DTMAPI state and the fake persistent root under the test fixture, and removed the isolated sibling after every child. The real Doloc Town directory, player saves, profile and subscriptions were not changed.
- `tools/scripts/test-player-runtime-only-uninstall.ps1 -Configuration Release`: passed. The managed identity assertion now derives the complete admitted set from developer definitions/Catalog (12) rather than conflating it with the currently published set (10); generic publishing remains limited to the published non-managed inventory.
- `tools/scripts/test-installer-invalid-target-failure.ps1 -Configuration Release`: passed.
- Final self-contained Author SDK ZIP SHA-256: `c96cde562f15b405f4c9dc5eaf3878c80277960400e3f611e1a2702f4902f506`; the SDK-generated Zoom fixture SHA-256 remained `E3104404BDB91339A1018F3420E91E0DC4670B4BCDF0939063BE2B9C60FFA702`.
- No new receipt/checkpoint family was added. Deployment journal schema 3 is the same existing per-product authority with an embedded compound recovery phase; receipt/package marker schema remains 2. The Author SDK deployment receipt/journal, source-state and game-root operation lock own the full managed local-install lifecycle. The outer installer neither snapshots nor writes those authorities and never replays a mutation after an ambiguous response. Runtime-only player uninstall ownership is unchanged.
- Step 4 public metadata capture: passed with 22 authoritative rows, fully paged broad discovery union 37 and normalized digest `e74aa6449e00d6ddc83004078a74a03a7e3d25820c9f8a8ccf7e13e535d28ce0`.
- Step 4 read-only subscription scan: 44 directories; four additions since 2026-07-13; no new `DTMAPI.Abstractions` consumer beyond the exact four external DLLs already identified.
- Step 4 complete negative DLL scan: 36 DLLs, 20 with the `DTMAPI.Abstractions` metadata token, of which five are Runtime framework files and fifteen are the exact retained consumers.
- Extended retained ABI harness Release build: passed with zero warnings/errors.
- Exact retained ABI run: passed with zero removed public API rows, 11 first-party products, 4 external consumers and all 23/23 external `DTMAPI.Abstractions` MemberRefs resolving against the candidate.
- Default synthetic retained ABI gate: passed unchanged with ten protected type surfaces and no private artifact dependency.
- Product Catalog after the RC cutoff: passed (`27 products / 11 public / 22 Workshop items / 48 API rows`).
- Runtime rollback archive verification: passed against the private
  read-only 1,934,142-byte archive; archive SHA-256
  `095533cb256e19381d1c51018258b239d53aa01accafa575bd24bdb93e9c4ac6`;
  payload 46 files / 4,319,645 bytes / tree SHA-256
  `ac67aef01109c9558dde5c780c1c64127c3c8e25bf9f70b1eb30dbc9a1408d25`.
- Step 4 archive-creation correction: the temporary ZIP must match the
  Catalog-bound archive length/SHA before publication. A focused
  payload-equivalent/different-timestamp injection was rejected before move,
  left no authoritative target or `.creating-*` residue, and preserved the
  accepted private archive unchanged.
- Document governance: passed (`6,031` checks); `git diff --check` passed with line-ending notices only.
- The metadata/ABI/Catalog scripts parsed under Windows PowerShell 5.1 and the current PowerShell host. No Workshop, game, player Mod or save path was written by the capture/ABI checks.
- Step 5 source candidate commit: `3f5cb3268f3e786a13594fae44e542c0c9d2657e`; tracked tree was clean before corrected artifact construction.
- Step 5 version/Catalog projection: passed (`27 products / 11 public / 22 Workshop items / 48 API rows`). Five products changed from historical source versions to `1.0.0`; Zoom, DebugConsole, MoreSaves, ChestLocatorEnhancer and MoreEquipmentSlots were already `1.0.0`. Historical published versions and Manbo `0.1.0-dtmapi` remain frozen.
- Product rollback archive verification: passed for ten independent read-only private/non-distribution directories under the external retained-artifact root. The Catalog-normalized set digest is `7330c7bb8a42934a81a69be985f92d0999b22687d979e9edf6cbe1402087e779`; verification passed under current PowerShell and Windows PowerShell 5.1 after making path ordering explicitly `OrdinalIgnoreCase`.
- Step 5 independent review: `P0=0 / P1=1 / P2=0`; the step was reopened because repository-root equality and existing junction ancestors could bypass the rollback archive non-distribution path claim. The shared corrected guard and focused transaction test reject product/runtime equality and junction routes under both PowerShell hosts; the test junction and external `_tests` root are absent after each run.
- The same review round confirmed that path correction, then found one candidate-record P1: five Runtime assembly hashes still described the first build rather than the corrected BuildCommit. The line below now comes directly from the corrected package manifest; this record-only correction does not rebuild or replace the already frozen bytes.
- Step 5 final same-round review: `P0=0 / P1=0 / P2=0`. All seven recorded Runtime/Doctor hashes match the current manifest and disk bytes exactly; the reviewer accepts Step 5 and authorizes entry to the already-planned Step 6 focused gates.
- Focused Step 5 Unit (`DTMAPI_UNIT_TEST_FOCUS=prerelease-step5`): passed. The build retained ten existing DebugConsole nullable warnings and zero errors.
- ActionSpeed, OneActionComplete, FishBreedingAssistant and AnimalHusbandryProgress focused Advanced product source gates: passed.
- Original corrected Step 5 Runtime Workshop candidate build: passed. Player Doctor release gate, package Catalog projection and built-in player-package QA exclusion passed. Its historical 31-file / 71,447,151-byte package tree SHA-256 is `7c5148d1247cdd8faac63148e014801c1f0389fd210e677be18b3f2037cb8fab`; release-manifest SHA-256 is `acb152dc216def1fd5a1ce6bc6d323ca377508d409fb23ecceb0d053bbe42b91`. The replacement Runtime identity is frozen below.
- Runtime candidate assembly hashes: Bootstrap `be2fc65442dac72c0ba5af0f04f267f948c0a67eacf508cae99439241f4f2e92`; Abstractions `3cd0eee2815e5b8254c4d9c2b32d101c77364b55eca462759f2244b94add1295`; Core `847e5fec274f9e86bc212736ff1d8695a50a3afb81a00e18772dfe3cf9aaf939`; GameBridge `9e872fb045490319733ee8a60b784f5a23b037cee1f7db23eb1e4cfa396a9bcd`; ModConfigMenu `e9afcab48a550cc72da04a3547233a3070a810de745ba1bd238b8e0f4fc8eea0`; Compatibility Host `25e2219fbad882d91b71ada652b0d6b30c14e3fc28dc18822d431baeded6b6c1`; Player Doctor executable `624e4f8571d66708fa35dfff5300b04556fb6d40fdaf751543f84437576a1e4f`.
- Original Step 5 Author SDK candidate release check: passed; its
  126,947,304-byte ZIP SHA-256
  `4d81906e6d8e73e0ef35fb75b89c4592e784bb612f01564996b6eea0725ce950`
  is historical and was superseded by the Step 7 correction/refreeze below.
- Ten independent Advanced `1.0.0` candidate package builds passed: Zoom `e3104404bdb91339a1018f3420e91e0dc4670b4bcdf0939063be2b9c60ffa702`; DebugConsole/Y Console `858ff9f40236a68fe64970bf7860fc6a20d254fd1f296a08edadf51906012916`; MoreSaves `79f7ae7368864339fa86592b79a0b81138cc897d34ce0acfbf7408e09c7c06f2`; ActionSpeed `431627407b1883e02bb20dcf2b37e86e3ee22d6c2a382010ae60c44b79f2666f`; OneActionComplete `fc20119587a447afabdafdd45ab6c045711bdc0976bb8a8f5ed10f97e07e8f2c`; FishBreedingAssistant `774cd3cea4f14e220d6fc1a54b5ccf14ead0ddbc55395315c59437d3c8a59e77`; AnimalHusbandryProgress `559ca6b7a793cf6bcb0bd8f6e5fdddf390f172eec060840054d29091b12409a2`; ChestLocatorEnhancer `391e5d2ffd62c62b0aa9e796084e589150e99770d54e39fea47fa46692bdf071`; AutoFishing `b39f9881f8a1b8cbbbefaba7b1234be9e2a530b872f0d764d9b5ac49b024b6a0`; MoreEquipmentSlots `d3d1648f1c69812baf7bba7d0f90e119774d1784557b7aa84d2d7cf19b95cd54`.
- Exact retained ABI against the Runtime candidate package: passed with candidate Abstractions file version `0.5.5.0`, compatibility identity `0.5.3.0`, zero public removals, 11 first-party retained products, 4 external consumers and `23/23` external MemberRefs resolved. The mandatory GameBridge has no Compatibility Host assembly reference or heavy-executor marker.
- Player-artifact forbidden-entry scan: passed for one Runtime directory and ten product ZIPs / 81 product entries. Runtime release manifest has `BundledMods=[]` and exactly one dormant-shipped optional Compatibility Host.
- Game/full Release/L0-L5/GC/long soak: intentionally not run for steps 1-5. No candidate was installed and no Steam upload occurred.
- Step 6 source/static candidate gates remained green: Catalog/identity/version projection, exact retained ABI/provider, Player Doctor, package/hash and player-artifact exclusion, mandatory Runtime product-code zero-leftover, and PowerShell parse/install-transaction focus. The retained ABI output at `temp/prerelease-step6-retained-abi.json` reports zero removals, 11 first-party plus 4 external consumers and `23/23` external MemberRefs; the synthetic public gate also passed without private retained artifacts.
- Original-candidate Manbo compatibility passed at `GAME-SMOKE/20260728-043539`. `prerelease-manbo-compatibility.json` binds Runtime `0.5.5` BuildCommit `3f5cb3268f3e`, exact Workshop tree `23a320...c40e`, entry DLL `ab85c0...7ec4`, one Workshop load-source, one ignored stale Local duplicate, one Entry completion, one registration success, one WAV-ready result and zero compatibility failures. It used slot 3 / `NoNativeSave`, left the Workshop artifact unchanged, preserved player archives and committed sidecars before cleanup, performed no native save and exited cleanly.
- Original-candidate no-demand passed at `docs/debug/evidence/BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r3` with smoke `GAME-SMOKE/20260728-051433`: 300 warm-up plus 10,000 measured frames, no optional demand/updater at either boundary and zero optional Hook installs, directory enumerations, reflection searches, retained callbacks or native updater invocations. The installed five Runtime DLL hashes equal that candidate. The Unity live allocation counter was unavailable/non-functional, so this is real-Unity optional-work silence plus the existing offline warmed 10,000-frame zero-allocation gate, not a whole-game zero-allocation claim.
- Original-candidate active GC passed at `docs/debug/evidence/PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14`. The outer receipt reports `FullReleaseRun=false`, `FullHistoricalLadderRun=false`, `ForcedGc=false`, ActionSpeed/AutoFishing exit `0`, exact Author source-state restore, two exact product restores and lock release only after restore. ActionSpeed completed 8/8 selected stages; each verified its behavior/recovery boundary and kept `ResourceSnapshotBuilds` at `35 -> 35`. AutoFishing L1/L3/L4/L5 completed 5 warm-up plus 10 measured fish; owner roots stayed `52 -> 52`, input owners `1 -> 1`, active event handlers `5 -> 5`, API roots `35 -> 35`, total demand `10 -> 10` and Runtime records `2 -> 2` in every stage. Process memory is retained only as short diagnostic data; no quantified memory-risk budget is claimed.
- All 12 r14 game smokes were `NoNativeSave`, reported `RunStatus=Passed`, player archive and committed-sidecar unchanged before cleanup, `PlayerArchiveWritebackPerformed=false` and `ProcessExited=Passed`. Exact post-run verification also found zero managed isolation markers, zero run-owned product/journal backup roots, original product tree and deployment-journal hashes restored, and the pre-existing recovery ledgers unchanged at ActionSpeed `42` and AutoFishing `25`.
- Step 6 first independent review: `P0=0 / P1=1 / P2=1`. Runtime observations, selected stages, NoNativeSave proofs, stable metrics and exact completed-run restores were accepted. The step remained open because the outer product/journal lease had no process-interruption recovery and because r14's existing stage receipts still contained the superseded `600`-second bound.
- The corrected focused transaction tests pass seven real child-process exit points: after original destination move, original journal move, candidate publication, candidate destination preservation, candidate journal preservation, original destination restoration and original journal restoration. A separate process reloads the durable state/package ledger and restores the exact original tree/journal plus two fixture recovery artifacts. Same-call failure, destination tamper refusal, marker ownership and exact frozen SDK binding remain covered.
- `docs/debug/evidence/PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14/auto-fishing/final-validator-reevaluation.json` is a 17,808-byte read-only interpretation receipt with SHA-256 `6c6ff207394ff437783c7741d05410f4302557ee22fb78ac8d6be84e33435fac`. It hashes the unchanged ladder plan, four raw runtime files and four original stage receipts; records original maximum `600`, final maximum `190`, L1/L3/L4/L5 terminal elapsed about `22/10/13/13`, and `GameLaunched=false` / all mutation flags false. No game rerun was needed or performed.
- The focused transaction, ActionSpeed ladder and AutoFishing ladder source/static tests passed under both PowerShell 7 and Windows PowerShell 5.1 after r14. They cover durable phase-before-move recovery, exact product/journal lease and failure restore, candidate tamper refusal, managed-product marker ownership, focused stage selection, separate frozen SDK binding, schema-2/3 journal reading, LongRun minimums, target-driven terminal cadence and exact source-state restoration.
- Step 6 same-round final review of correction commit `8fb40036`: `P0=0 / P1=0 / P2=0`. The reviewer independently confirmed all seven interrupted lease windows, strict recovery identity/lock/package-ledger checks, unchanged original `600`-second stage receipts and the hash-bound read-only `190`-second reinterpretation. Step 6 is accepted and may proceed to Step 7.
- Attempts r1-r13 are non-acceptance evidence:
  - r1-r2 performed no candidate operation because library dot-sourcing overwrote outer parameters.
  - r3 failed package hash preflight without mutation; r4 restored both candidates but misbound the cross-process stage array before any game; r5 failed on a nested Windows PowerShell hash helper before game.
  - r6-r7 ran an ActionSpeed L0 behavior successfully but rejected its actual SDK-managed `Local` provenance; all state restored. r8 passed ActionSpeed L0 and runtime L1 behavior but rejected one legitimate cadence-coincident terminal sample.
  - r9 completed ActionSpeed 8/8 and stopped AutoFishing before game because the frozen SDK wrote schema-3 deployment journals; r10 again completed ActionSpeed 8/8 and stopped AutoFishing before game because the outer product lease made the original LocalDevelopment source selection temporarily stale.
  - r11 completed ActionSpeed 8/8, then launched AutoFishing with invalid `1/1` LongRun counts and failed before product actions. The intentionally interrupted runner left QA activation `832c3b0ddcfd41499058aa0da491a096`; exact activation-bound QA/settings bytes were copied to `PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r11/stale-qa-host-activation-recovery` before only those exact runtime files were removed. Product/source restore still passed.
  - r12 failed closed before game on that stale activation and restored exactly. r13 completed ActionSpeed 8/8 and a genuine AutoFishing L1 5/10 runtime pass at `GAME-SMOKE/20260728-064010`, but the outer gate used nominal 10-second sample bounds instead of the real duration-plus-target contract and therefore stopped before L3/L4/L5.
- No complete Release, historical full L0-L5 ladder, forced GC, long soak, Workshop package acceptance, Steam upload or post-upload subscription validation ran in Step 6. The focused result is consistent with the frozen “significantly reduced Unity GC pressure / lower crash chance to some extent” wording, but ISSUE-010 remains open and the evidence does not claim that all Unity/Mono GC growth or crashes are solved.

## Step 7 Provisional Pre-Release Freeze

### Identity And Toolchain

| Field | Frozen value |
| --- | --- |
| Freeze state | `implemented / provisional` |
| Original Runtime/product clean freeze-input HEAD | `19e4a74c2146cd9686d8c8de908a8fc6238cfaa1` |
| Runtime candidate source commit | `c7e1ec2f3697fb8cd5c61cff500e9016a48476ff` |
| Ten product candidate source commit | `3f5cb3268f3e786a13594fae44e542c0c9d2657e` |
| Author SDK corrected refreeze source commit | `c32a274ff097fb718be96422dbec217f8ae75e49` |
| Accepted provisional refreeze documentation commit | `e2a43355` |
| Accepted route-review input commit | `7b444dc4` |
| Completion-audit correction commits | `9beefe78` exact external-consumer admission; `c7e1ec2f` verified Catalog load lanes; `ed85e11a` Author SDK snapshot/prepare transaction correction; `c32a274f` game-root pending-localInstall barrier and recovery state proof |
| Candidate-affecting diff | Runtime Core and embedded Catalog intentionally changed for the exact retained external-consumer admission; Catalog load gates changed to `ActualLoadLaneVerified`; Author SDK changed for semantic snapshots, prepare reconciliation, game-root source-write exclusion and recovery preflight. Ten product ZIPs, publish projection, `Directory.Build.*` and `global.json` remain unchanged. |
| Runtime/API version | `0.5.5` / `0.5.5.0`; Compatibility assembly identity `0.5.3.0` |
| Repository-local .NET | SDK `8.0.421`, MSBuild `17.11.48`, host/runtime `8.0.27`, `win-x64` |
| PowerShell hosts | PowerShell `7.6.3`; Windows PowerShell `5.1.26100.8875` |
| Shared runtime state at freeze input | Runtime lock free; `DolocTown.exe` absent |

The independent reviews must resolve and record the committed provisional
freeze HEAD rather than treating the input HEAD above as a self-referential
commit claim. Any candidate-affecting correction invalidates the binary freeze
and returns to Step 5/6; documentation-only review closure must still leave a
clean tracked tree.

### Frozen Build Artifacts

| Artifact | Bytes / tree | SHA-256 |
| --- | ---: | --- |
| Runtime Workshop directory `dist/prerelease-step5-candidate/DTMAPI` | 31 files / 71,533,167 bytes | release/retained-artifact tree `f0dd2c5651b8e76e7da207f938aaa4c32f629f8a075806f9ceeac50d8cbf81b2` |
| Runtime `Content/DTMAPI/release-manifest.json` | 2,302 | `b0b6b4a4d325263ce1e099dd73f205a2381a0f6729e5508905d62ddd06c0f055` |
| Author SDK `temp/prerelease-step5-author-sdk/DTMAPI-Author-SDK-0.1.0-win-x64.zip` | 126,959,919 | `1131840b940d9b366fbc24391449cf739078730655b7409b24720b1579c5f1c7` |
| ActionSpeed `temp/prerelease-step5-product-action-speed/DTMAPI-ActionSpeed-advanced-pilot.zip` | 319,107 | `431627407b1883e02bb20dcf2b37e86e3ee22d6c2a382010ae60c44b79f2666f` |
| AnimalHusbandryProgress `temp/prerelease-step5-product-animal-husbandry-progress/DTMAPI-AnimalHusbandryProgress-advanced-pilot.zip` | 22,059 | `559ca6b7a793cf6bcb0bd8f6e5fdddf390f172eec060840054d29091b12409a2` |
| AutoFishing `temp/prerelease-step5-product-auto-fishing/DTMAPI-AutoFishing-advanced-pilot.zip` | 245,924 | `b39f9881f8a1b8cbbbefaba7b1234be9e2a530b872f0d764d9b5ac49b024b6a0` |
| ChestLocatorEnhancer `temp/prerelease-step5-product-chest-locator-enhancer/DTMAPI-ChestLocatorEnhancer-advanced-pilot.zip` | 17,734 | `391e5d2ffd62c62b0aa9e796084e589150e99770d54e39fea47fa46692bdf071` |
| FishBreedingAssistant `temp/prerelease-step5-product-fish-roe-info/DTMAPI-FishBreedingAssistant-advanced-pilot.zip` | 10,681 | `774cd3cea4f14e220d6fc1a54b5ccf14ead0ddbc55395315c59437d3c8a59e77` |
| MoreEquipmentSlots `temp/prerelease-step5-product-more-equipment-slots/DTMAPI-MoreEquipmentSlots-advanced-pilot.zip` | 46,244 | `d3d1648f1c69812baf7bba7d0f90e119774d1784557b7aa84d2d7cf19b95cd54` |
| MoreSaves `temp/prerelease-step5-product-more-saves/DTMAPI-MoreSaves-advanced-pilot.zip` | 11,015 | `79f7ae7368864339fa86592b79a0b81138cc897d34ce0acfbf7408e09c7c06f2` |
| OneActionComplete `temp/prerelease-step5-product-one-action-complete/DTMAPI-OneActionComplete-advanced-pilot.zip` | 208,707 | `fc20119587a447afabdafdd45ab6c045711bdc0976bb8a8f5ed10f97e07e8f2c` |
| DebugConsole / Y Console `temp/prerelease-step5-product-y-console/DTMAPI-YKeyConsole-advanced-pilot.zip` | 348,181 | `858ff9f40236a68fe64970bf7860fc6a20d254fd1f296a08edadf51906012916` |
| Zoom `temp/prerelease-step5-product-zoom/DTMAPI-Zoom-advanced-pilot.zip` | 455,673 | `e3104404bdb91339a1018f3420e91e0dc4670b4bcdf0939063be2b9c60ffa702` |

The Runtime tree value above uses the established release/retained-artifact
normalization: ordinal relative-path rows in the form
`<file-sha256><two spaces><relative-path>`, joined with LF and hashed as UTF-8.
A second read-only rehash with the transaction helper records
`DTMAPI-FileTree-SHA256-v1`
`3f634965ac15af0563abb16202a3141a3d3a4ab01c2c3b9649d8e73ec285f154`
and `DTMAPI-CandidateStructure-SHA256-v1`
`c032f22c8f31475a027b24a98622870049b1e2c64a77ef12e0383dc4e1a93a02`.
These are deliberately separate algorithms over the same 31 files and
71,533,167 bytes; a mismatch between the two digest strings is not artifact
drift.

The Runtime directory contains exactly the five mandatory assemblies plus the
dormant-shipped Compatibility Host; Player Doctor is a bundled support
artifact rather than a game-loaded assembly:

| Runtime component | Bytes | SHA-256 |
| --- | ---: | --- |
| `DTMAPI.BepInExBootstrap.dll` | 161,280 | `42831b4a76e6b7285a16192eea1fb2a399e3984c01fbacb100d2a627ad0fee38` |
| `DTMAPI.Abstractions.dll` | 242,688 | `3cd0eee2815e5b8254c4d9c2b32d101c77364b55eca462759f2244b94add1295` |
| `DTMAPI.Core.dll` | 922,112 | `a7ad62a34a2001c077a25e0684fb48f5e4ce90882c1a0bbb25fcbd4078fb7185` |
| `DTMAPI.GameBridge.DolocTown.dll` | 651,776 | `a18e7949c9a2e45a36ddab68a150f7fa2f762c3d3dd3b8751de09c7cc29d1e1a` |
| `DTMAPI.ModConfigMenu.dll` | 40,448 | `926e020d052d051f048e49dcf06944b6cee1db5601f9495e6fcae387a1f288ba` |
| `DTMAPI.GameBridge.DolocTown.Compatibility.dll` | 630,272 | `f8962000aa62a2b199dc9fbd0655749c8f0458eb9b956134a1129b7909321e4a` |
| `dtmapi-player-doctor.exe` | 67,668,177 | `624e4f8571d66708fa35dfff5300b04556fb6d40fdaf751543f84437576a1e4f` |
| bundled BepInEx `5.4.23.5` ZIP | 639,118 | `82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4` |

### Install, Uninstall And Rollback Inputs

- Runtime player input is the frozen Workshop directory above. Its top-level
  entrypoints are bound inside the Runtime tree and rehashed exactly here:

  | Entrypoint | Bytes | SHA-256 |
  | --- | ---: | --- |
  | `0_probe_dtmapi_install.bat` | 2,879 | `345c17a18542ad33eae7daafd62dde448109b6ee1ff049284b1ad059683ddf11` |
  | `1_install_dtmapi.bat` | 3,043 | `cb63d69697e5cd34353b94b2ae29e2b76e96bfd787cf868638addf71ff511ad9` |
  | `2_uninstall_dtmapi.bat` | 2,806 | `305a8eae2e95695fd399e96dd0a4b15d48c957dbdd80ec6273393e26b75ed7d2` |
  | `3_check_dtmapi_status.bat` | 3,080 | `17878e1fb3b62cd37f8cb40884e922260ac14b25edee4f3034d27054861df3fe` |
  | `4_collect_dtmapi_logs.bat` | 3,016 | `b318d7f81ab469e6c4cb9a24e2f553885dca78df7ef44c27c66f0de759538703` |

  Their bundled PowerShell helpers remain under
  `Content/DTMAPIInstaller/tools`; no helper is taken from the mutable
  repository at player execution time.
- Advanced product local installation uses the frozen Author SDK and its one
  schema-3 `install-local` owner. An ambiguous response is reconciled by
  read-only `install-local-status`; a non-null `localInstall` is recovered by
  the matching SDK's explicit `recover`, never by an outer snapshot owner.
- Runtime rollback input is the verified private, read-only 0.5.2 archive:
  1,934,142 bytes, SHA-256
  `095533cb256e19381d1c51018258b239d53aa01accafa575bd24bdb93e9c4ac6`,
  46-file payload tree
  `ac67aef01109c9558dde5c780c1c64127c3c8e25bf9f70b1eb30dbc9a1408d25`.
  Its location is the Catalog-defined repository sibling/non-distribution
  contract and it remains verified by
  `tools/scripts/freeze-runtime-rollback-archive.ps1`.
- Ten product rollback directories remain separate, private and read-only
  under the Catalog-defined sibling contract. Their normalized authority is
  10 rows with SHA-256
  `7330c7bb8a42934a81a69be985f92d0999b22687d979e9edf6cbe1402087e779`,
  verified by `tools/scripts/freeze-product-rollback-archives.ps1`.
- An interrupted active-GC fixture lease is not a release rollback mechanism.
  If one exists, the exact Step 6 wrapper `-RecoverOnly` must restore it under
  the stale worktree Runtime lock before any tool downgrade or manual cleanup.

### Release Membership And Order

| Wave | Exact content | Boundary |
| --- | --- | --- |
| R0 | Runtime `0.5.5` only | publish first; retain old Manbo `0.1.0-dtmapi` and MoreEquipmentSlots `0.3.1-dtmapi` as compatibility consumers |
| R1 | AutoFishing `1.0.0` | publish and observe alone |
| R3 | ActionSpeed, OneActionComplete, FishBreedingAssistant, AnimalHusbandryProgress, MoreSaves, ChestLocatorEnhancer, Zoom and DebugConsole/Y Console, each `1.0.0` | one release window, independent packages and rollback |
| Deferred | MoreEquipmentSlots `1.0.0` | do not upload in this release; retain Workshop `3744059735` at `0.3.1-dtmapi` and verify old-package compatibility with R0 |

Runtime R0 contains no bundled product. Manbo is not rebuilt or promoted into
the Advanced waves. Mine is admitted but remains publication-blocked by its
art/presentation cleanup and is not one of these ten release packages.

### Copy, Evidence, Risks And Deferrals

- [0.5.5 Workshop Update Copy](../../releases/0.5.5-workshop-update-copy.md)
  is the only editable authority for the exact Simplified Chinese,
  Traditional Chinese and English text. Its first gate is now checked; each
  Steam language paste/save/re-open/readback item remains unchecked and manual.
- Runtime evidence for the current `c7e1ec2f3697` candidate: exact external
  consumers `GAME-SMOKE/20260728-230231`, Manbo
  `GAME-SMOKE/20260728-230430`, no-demand
  `GAME-SMOKE/20260728-230535` /
  `docs/debug/evidence/BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r4`,
  and active GC
  `docs/debug/evidence/PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r15`.
  The earlier `043539` / `051433` / r3 / r14 results remain accepted historical
  evidence for the superseded `3f5cb3268f3e` Runtime candidate only.
- Known risk remains ISSUE-010: the focused/no-demand evidence supports the
  selected bounded GC wording but does not establish a whole-game allocation
  budget or eliminate every Unity/Mono crash. Process memory remains
  diagnostic only.
- Complete Release, the player-like Workshop package matrix, Steam upload,
  visible language-page readback and post-upload subscription verification
  are still blocked follow-up stages. No such action was performed here.
- Explicitly deferred beyond 0.5.5: G7, CustomAnimals/AudioReplacement host
  redesign, AnimalPack, Oil, Mine publication work, StrongPlantingGun,
  BGM, ShellCrab publication, MoreEquipmentSlots `1.0.0`/migration and a
  generic protected-storage API. No thirteenth product or general Advanced
  authoring lane is admitted by this freeze.

### Step 7 Read-Only Freeze Validation

- Rehashed the completion-audit-corrected Runtime manifest, corrected Author
  SDK and all ten product ZIPs from their frozen local paths. The Runtime and
  SDK match the replacement identities in the table above; the ten product
  packages remain byte-identical to the original freeze.
- Rehashed the eight Runtime components/support inputs and five batch
  entrypoints. The Runtime directory independently reproduced both tree
  algorithms and the exact `31 / 71,533,167` inventory recorded above.
- Re-ran the existing Runtime and ten-product rollback verification entries in
  read-only mode; the Runtime archive/payload and all ten private product
  directories matched the Catalog-bound authorities.
- Source comparison records intentional Runtime/Catalog changes through
  `c7e1ec2f3697` and Author SDK changes through `ed85e11ac2bd`; first-party
  product sources, publish projection, shared build properties and
  `global.json` remain unchanged from the original product freeze.
- Repository-local .NET, both PowerShell hosts, the free Runtime lock and zero
  `DolocTown.exe` processes matched the toolchain/state table above.
- `tools/scripts/check-doc-governance.ps1` passed (`6,031` checks), and
  `git diff --check` passed with line-ending notices only.
- No complete Release, Workshop player-package acceptance, Steam mutation or
  upload was performed. Focused builds, SDK tests and the four current-candidate
  game gates below were run because the audit corrections changed candidate
  bytes.
- Initial Step 7 document review of `b2730ea8`: `P0=0 / P1=0 / P2=0`.
  Initial code/artifact review: `P0=0 / P1=1 / P2=0`; it rejected the
  schema-2 journal schema/README shipped beside the schema-3 writer. Initial
  route review: `P0=0 / P1=2 / P2=3`; it required a recorded Step 3 terminal
  re-review, exact artifact paths, current Step 1 wording, StrongPlantingGun
  deferral and the player-like Workshop package gate in copy status. All
  documentation-only findings above are corrected here.
- The requested Step 3 continuation then found `P0=0 / P1=0 / P2=1` in
  `588cbcf6`: `HasRetryableLocalInstallAuthority` accepted a matching
  transaction ID without fully validating `localInstall`. The current source
  correction `f384d2338930b18d4c2b33057193399536ba6fce` validates the
  complete transaction and adds both structurally corrupt and unreadable-marker
  classification tests. `add0dba3da52bda5decc3542ea92341b4b357166`
  fixes nullable-schema parsing under Windows PowerShell strict mode.
- `DTMAPI.AuthorSdk.Tests` Release build and full test entry passed, including
  published journal schema/README parity and the two new fail-closed marker
  injections. The Author SDK Release check passed against the exact re-frozen
  ZIP under PowerShell 7. Windows PowerShell 5.1 passed the same structure,
  manifest and contract checks while using the checker's established
  `-SkipDeterministicZipCheck` path because that host cannot reproduce the
  store-mode ZIP byte replay. The package contains 389 files.
- A fresh Zoom build through that exact SDK reproduced the already frozen
  455,673-byte product ZIP and SHA-256
  `e3104404bdb91339a1018f3420e91e0dc4670b4bcdf0939063be2b9c60ffa702`.
  The isolated official-local install transaction matrix then passed under
  both PowerShell hosts (`hosts=2`) with real SDK preflight, update, rollback
  and recovery. It did not touch the live game or player saves.
- Step 3 same-round continuation independently reran Author SDK Unit and both
  PowerShell release checks, inspected the exact 389-file ZIP and accepted
  `f384d233` / `add0dba3` with `P0=0 / P1=0 / P2=0`. It confirmed full
  `localInstall` validation before any retryable classification, exact
  fault-point corruption/unreadable injections, journal schema 3 parity and
  receipt/package-marker schema 2.
- Its document continuation found only two P2 status strings that still said
  the SDK awaited refreeze. The Workshop copy header and lightweight roadmap
  now say the corrected SDK is already refrozen and that only Step 7
  acceptance remains. No game launch, complete Release, Workshop
  player-package acceptance, Steam mutation or upload was performed by this
  correction/refreeze.
- Step 7 code/artifact continuation independently rehashed the corrected SDK,
  Runtime, all ten product candidates and both rollback families; reran
  Author SDK Unit and both-host release checks; verified compiled
  journal/receipt/marker constants and schema/model shapes; and accepted with
  `P0=0 / P1=0 / P2=0`. It explicitly records that Windows PowerShell 5.1
  skipped only deterministic store-mode ZIP replay, not the structure,
  manifest or contract checks.
- Step 7 document continuation accepted `e2a43355` with
  `P0=0 / P1=0 / P2=0` after 6,031 governance checks, 224 local links and
  `git diff --check`.
- Step 7 route-overall continuation confirmed its original two P1 and three
  P2 findings closed, then found one Changed Files inventory P2. Commit
  `7b444dc4` added the three exact omitted paths; the requested pure-document
  reread accepted the final route with `P0=0 / P1=0 / P2=0`. It explicitly
  keeps complete Release, player-like Workshop package acceptance and every
  Steam operation outside this verified pre-release route.

### 2026-07-28 Completion-Audit Corrections

This section supersedes earlier freeze-specific SDK/Runtime identities and
current-candidate evidence statements in this Update. It preserves the earlier
results as historical evidence rather than rewriting their chronology.

1. **P1-1 and P2-1, Author SDK recovery truth:** deployment and source-state
   snapshots now pass their owner-specific semantic readers before they can be
   accepted as restoration inputs. Prepare publication verifies the exact
   marker written to disk, reconciles an already-published marker after a
   write-after fault, and only then cleans SDK-owned transaction-shaped
   immutable inputs and staging directories that are unreferenced across the
   validated game-root journal set. Pending/active/recovery-referenced stages
   and non-transaction-shaped paths remain protected. Invalid snapshots and
   wrong or unreadable marker states fail closed. Focused Unit covers
   semantically invalid deployment and source-state snapshots, prepare
   write-after reconciliation and this all-journal fail-closed orphan cleanup.
   The rebuilt 389-file SDK from that correction was 126,954,799 bytes with
   SHA-256
   `723f89f5a1a966ab60dd2f63b649c774ea0d5db7a07129bde0561262046115c9`;
   it is historical and superseded by the 2026-07-29 refreeze below.
   Full Author SDK Unit, the PowerShell 7 release check, the Windows PowerShell
   5.1 structural/contract check with its established
   `-SkipDeterministicZipCheck`, and the two-host managed local-install
   transaction passed. These tests used managed isolated fixtures and did not
   touch the game Runtime or player saves.
2. **P1-2, evidence retention:** the existing allowlist contract advances from
   schema 4 to schema 5 and adds `PRERELEASE-ACTIVE-GC` as its eighth durable
   category. The parser rejects traversal/rooted inputs, the cleanup consumer
   fingerprints the complete category root as an indivisible tree, and the
   focused test freezes both historical and replacement no-demand/active-GC
   roots. The focused test passed under PowerShell 7 and Windows PowerShell
   5.1; generation and `-Check` agree on 483 source files, 878 smoke runs,
   62 Runtime identities and 19 durable roots. No parallel receipt or
   retention authority was introduced.
3. **P1-3, retained external actual-load lane:** the first Unity attempt at
   `GAME-SMOKE/20260728-224404` is retained as non-acceptance evidence. It
   proved all three frozen DLLs were rejected by the general Strict native
   reference rule before load. The root-cause Review `20260728-0005` therefore
   bounded a legacy admission to the exact native-verified Workshop source,
   Workshop ID, UniqueID, omitted `CodeModKind`, entry path and SHA-256 frozen
   in Catalog. Core embeds that Catalog projection, validates SHA/MVID around
   `Assembly.LoadFrom`, skips the Strict closure only for the exact entry DLL,
   and still rejects local, unverified, wrong-ID/path/hash and additional
   native DLL inputs. It does not create a general Advanced lane and does not
   change these Mods' Strict identity. The final
   `GAME-SMOKE/20260728-230231` run binds BuildCommit `c7e1ec2f3697` and
   proves one Workshop load, one Entry completion and the scoped monitor,
   config-read or GMCM-registration behavior for each of Workshop
   `3743621104`, `3743644065` and `3754869009`, with zero compatibility
   failures, unchanged subscription trees, slot-3 `NoNativeSave`, clean title
   lifecycle and process exit. Catalog now records all three as
   `ActualLoadLaneVerified`.
4. **P2-2 through P2-4, documentation truth:** Changed Files now includes the
   no-demand Review/runner/test, smoke matrix and every completion-audit source,
   test, Catalog, checker, retention and Review path. No-demand wording is
   limited to the optional Compatibility/content work actually observed;
   mandatory GameBridge empty registration/lifecycle refresh is allowed.
   Complete Release may validate a temporary rebuild of the current source
   tree, but that package cannot replace the frozen player candidate unless it
   is separately re-frozen with exact provenance, length, tree/hash and
   rollback inputs.

Because the exact legacy admission is compiled into Core, the Runtime
candidate changed. The final candidate is BuildCommit `c7e1ec2f3697`, and the
replacement freeze values are the tables above. Every Runtime-bound Step 6
game gate was therefore repeated:

- external consumers: `GAME-SMOKE/20260728-230231`;
- Manbo: `GAME-SMOKE/20260728-230430`, exact retained seven-file Workshop tree,
  one load/Entry/registration/WAV-ready result, zero compatibility failures;
- no-demand: `GAME-SMOKE/20260728-230535` and
  `docs/debug/evidence/BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r4`,
  300 warm-up plus 10,000 measured frames, exact five-DLL binding and zero
  optional demand/updater/Hook/directory/reflection/callback/native work;
- active GC:
  `docs/debug/evidence/PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r15`,
  ActionSpeed 8/8 and AutoFishing L1/L3/L4/L5 passed across twelve
  `NoNativeSave` stages. Resource/owner/input/event/API/demand/Runtime roots
  remained stable, product/journal/source state restored exactly, and the
  shared Runtime lock was released only after exact restore.

The final retained ABI audit reports zero public removals, 11 first-party and
4 external consumers, and all 23/23 external MemberRefs resolved. The Catalog
checker passes against the exact Runtime package and expected BuildCommit.
No full Release, full historical ladder, forced GC, long soak, Workshop player
package acceptance or Steam operation ran. These corrections are implemented,
but the route remains `implemented / provisional` until the required
independent focused re-review passes.

### 2026-07-29 Game-Root Recovery Barrier Correction

The correction-acceptance audit's frozen-SDK reproduction established that
the per-product schema-3 marker was durable but did not exclude another
product's legal write to the same game-root `source-state.json`. The later
recovery therefore had an obsolete whole-file snapshot it could still write
back.

Commit `c32a274f` reuses the existing deployment journals and game-root lock:

- every `source local/workshop/reproduction` write and every
  `install-local` now scans and semantically validates all game-root
  deployment journals, then fails before cleanup, input copy, extraction,
  deployment or source mutation when any `localInstall` is pending;
- `source status`, `deployment-status` and exact `install-local-status` remain
  read-only during the pending state, with the latter reporting
  `RecoveryRequired`;
- explicit recovery accepts only one validated matching pending marker and,
  before any directory move, proves the current source-state bytes are either
  the exact captured pre-state or the exact committed post-state derived from
  that marker's next inventory; unknown external/old-tool drift leaves the
  journal, destination, source state and pending stage unchanged;
- the orphan cleanup description above now records its real ownership:
  after all-journal validation it removes all SDK-owned transaction-shaped
  unreferenced input/stage artifacts, while every valid pending, active or
  retained-recovery stage remains protected.

The Author SDK Unit passed and directly covers cross-product local/workshop/
reproduction write refusal, another product's `install-local` refusal,
pending read-only status, exact recovery, external source-state drift refusal,
retry after restoring an allowed state and pending-stage preservation.
PowerShell 7 rebuilt and deterministically checked the 389-file SDK; Windows
PowerShell 5.1 passed the same release structure/contract check with the
established `-SkipDeterministicZipCheck`. The existing managed local-install
transaction matrix then passed under both hosts (`hosts=2`), including the
real SDK-generated Zoom seed/update, rollback, separate-process recovery and
lost-response reconciliation lanes. The unique refrozen ZIP is 126,959,919
bytes with SHA-256
`1131840b940d9b366fbc24391449cf739078730655b7409b24720b1579c5f1c7`.

No Runtime or product was rebuilt, no game or Steam operation ran, and GC,
Manbo, external-consumer and no-demand evidence was not repeated. The frozen
Runtime remains BuildCommit `c7e1ec2f3697`, 31 files / 71,533,167 bytes, with
retained-artifact tree
`f0dd2c5651b8e76e7da207f938aaa4c32f629f8a075806f9ceeac50d8cbf81b2`.
This Update remains `implemented / provisional` until an independent focused
review accepts this exact correction/refreeze.

### 2026-07-29 Release Blocker Corrections And Complete Release

The release-entry audit accepted the game-root recovery barrier and found two
remaining P1 delivery defects. Commit `cc6b872d` makes a lost or corrupt
`install-local` response fail closed unless the frozen Author SDK's read-only
status is ordinal-exact `CommittedLocalDevelopment` and supplies valid
deployment/source tree digests. `RecoveryRequired` now remains an explicit
retryable recovery state: it cannot set the committed phase, publish success,
register the product, or cause the mutation to be replayed. The focused
managed transaction matrix combines corrupt output with both
`crash:install-local.after-prepare` and
`crash:install-local.after-source-selection`, then requires outer failure,
the intact pending marker, explicit separate recovery and exact destination,
journal and source-state restoration. It passed under PowerShell 7 and
Windows PowerShell 5.1 (`hosts=2`).

Commit `493de436` makes the official player-package generator copy exactly the
four root entrypoints `1_install_dtmapi.bat` through
`4_collect_dtmapi_logs.bat`. Root `0_probe_dtmapi_install.bat` is rejected by
the packaged-layout gate; the internal
`Content/DTMAPIInstaller/tools/probe-install-preflight.ps1` remains present,
and the standalone diagnostic package retains its separate probe boundary.
Both a freshly generated package and the exact frozen directory passed the
offline install/status/log-collection/uninstall matrix. The independent
review in commit `b3c3f3e2` reports `P0=0 / P1=0 / P2=0` and accepted entry
to one complete Release run.

The exact frozen player directory is
`dist/prerelease-step5-candidate/DTMAPI`:

| Property | Accepted value |
| --- | --- |
| Files / bytes | `30 / 71,532,156` |
| release/retained-artifact tree | `9661b96fc106ee7372de26c79e0fd2a9700295d72ec1b8695c4c7d4bb6e38034` |
| `DTMAPI-FileTree-SHA256-v1` | `3e0e34703ab43fcdb136075ac3e49ec3d0fa5470d132fc6e87d76009561cb477` |
| `DTMAPI-CandidateStructure-SHA256-v1` | `6cf56275cfe2cc913c38b2cba717e2eeeb074e5743b3bc4849f6fdb180153418` |
| release-manifest SHA-256 | `4d8e54f02d7ef7ae3d43c5ddd032704a7d491511c65aa8200744d61c006e0408` |
| release-manifest `BuildCommit` | `493de436d2f7` |
| Root BAT set | exact `1`, `2`, `3`, `4`; root `0_probe` absent |

The first from-start Release attempt was non-acceptance because the new
independent Review path was absent from the tracked evidence-retention
allowlist. The allowlist was rebuilt and checked in commit `b3c3f3e2`.
Following the assurance rule, a diagnostic tail then exposed two latent
source-only gate defects rather than restarting the whole suite after each
change. Commit `7993ea71` makes the release-contract source-tree comparison
use the same ordinal relative-path ordering as the SDK. Commit `1f1da9fa`
refreshes the Batch 4 C1, Mine and DebugConsole semantic inventory against
the current physical sources and current lifecycle contracts. Their focused
release-contract, semantic-boundary and meta-negative gates passed before
the final run.

The single final authoritative from-start
`tools/scripts/test.ps1 -Configuration Release` run passed with exit code
`0`. It ran from `2026-07-29T11:38:32.2903098+08:00` through
`2026-07-29T11:51:46.4563848+08:00` (`794.166` seconds) and reached all
build, Unit/QA, installer/Doctor, Catalog, package, Author SDK, dual-host
transaction, Candidate 11, release-contract, Batch 4, no-demand, ABI,
artifact-governance and document-governance gates. The Author SDK remained
the exact 389-file release with SHA-256
`1131840b940d9b366fbc24391449cf739078730655b7409b24720b1579c5f1c7`.
The exact frozen player directory was rehashed and run again through its offline
entrypoint matrix after the suite; its count, bytes, three tree/structure
identities, manifest hash and BuildCommit all remained the accepted values
above.

No Doloc Town process was launched, no Runtime/game/save/subscription path
was mutated, and no Steam operation ran for these corrections or the complete
Release suite. The suite's GC coverage here was its source/static/validation
lane; it did not repeat the already accepted focused game evidence, run a
forced-GC game ladder or perform a long soak. The remaining player-like
dual-host Workshop-package audit, short manual Unity transition matrix and
post-upload Steam checks are therefore not implied by this Release PASS.

## 2026-07-30 MoreEquipment Transition Runtime Refreeze

The MoreEquipmentSlots cold-recovery corrections changed Compatibility Host
bytes after the accepted `493de436` player directory and its complete Release
run. Those historical results remain valid for their exact candidate but do
not accept the replacement Runtime.

After the permanent-winner loser test was narrowed to its ordinal-exact
rejection reason in commit `6751157c`, the player package was rebuilt from
that clean production HEAD and frozen at:

```text
dist/prerelease-055-moreequipment-transition-candidate/DTMAPI
BuildCommit = 6751157c2d42
files / bytes = 30 / 71,552,124
DTMAPI-FileTree-SHA256-v1 =
  52dac1e0ec120bb4467645e52fd139decc0ace726ea8eec61c2ed42ac946a868
DTMAPI-CandidateStructure-SHA256-v1 =
  1274239d49d6430123d6d2725f7b06d4fe84841cc2cb5f19162cace213850ffc
normalized retained tree =
  611556795c76a2036103cff34998bc64fcf738afb1a9020e2a9b53b322723533
release-manifest SHA-256 =
  ea184b45bc5b5cc52f8c5315ac6469ce0b66f92d6fce93818d8715771f82a20d
root BAT = exact 1 / 2 / 3 / 4; root 0_probe absent
```

The repeat root
`dist/prerelease-055-moreequipment-transition-candidate-repeat/DTMAPI`
matched the primary candidate in its other 29 files and normalized retained
tree; only the generated manifest `BuildTime` differed. The accepted primary
Runtime hashes are Bootstrap `5d536a52...f18d`, Abstractions
`3cd0eee2...1295`, Core `148404a5...c992`, GameBridge
`c4c0a9c2...6453`, ModConfigMenu `d0f0d8d0...279b`, Compatibility Host
`b37875a5...501f`, and Player Doctor `624e4f85...e4f`.

Packaged Doctor and package-layout checks passed. The player-like audit at
`tmp/test-runs/DTMAPI Workshop Audit 20260730-100109` passed with zero
blockers, including Windows PowerShell 5.1 parsing and the offline
install/status/log-collection/uninstall matrix. The earlier relative-output
attempt at `20260730-100020` is non-acceptance orchestration evidence.

The candidate was installed under the shared Runtime lock and its installed
five mandatory DLLs plus optional Compatibility Host matched the frozen
hashes. The exact transition results are owned by the MoreEquipmentSlots
Update:

- U1 `GAME-SMOKE/20260730-110543`: retained 0.3.1 ABI/UI/two-item state,
  50/80 shield, one normal native save and title/Loader cleanup;
- U3 preparation `GAME-SMOKE/20260730-115306`, backpack recovery
  `20260730-115439` and mail recovery `20260730-115548`: exact one-free-slot,
  full-backpack and terminal sidecar sequence through normal native saves;
- U4 `GAME-SMOKE/20260730-115656`: no native save, no archive writeback or
  routine backup, with SAVE and committed sidecar unchanged before cleanup.

Harness commit `fa3283e6` is QA/runner-only and does not alter the frozen
Runtime package inputs. QA Release build, QA Unit, save-mode/routing checks
and PowerShell parsing passed. The accepted runs used an AutoCloud-isolated
third-save fixture; Product was physically isolated then restored, and no
game process or temporary hold remained. No complete Release, Steam upload,
GC ladder, L0-L5 or long test was run against this replacement candidate.
Runtime validation is therefore `partial` until a current-candidate complete
Release decision is made.

## 2026-07-30 Corrected Transition Candidate And Release Entry

Commit `3a77b93c` repairs the MoreEquipmentSlots player oracle and U1 retained
Workshop provenance without changing Product or Runtime production behavior.
Their installed production Runtime/Host bytes remain the `6751157c` candidate,
but the QA provenance is not one unchanged fixture generation: corrected
U1/U3/U4 used QA SHA-256 `DF0A9307...A69B2` at 1,035,264 bytes, while
`134949`/`135057` used `979E2156...A7465` at 1,038,848 bytes after the
migrated-save phase was added. The product Update owns those runs, targeted C0,
the later final cold-oracle correction and the explicit withdrawal of the
extra player claim-kill gate.

The replacement Runtime candidate was frozen after that source correction:

```text
primary = dist/prerelease-055-final-candidate/DTMAPI
repeat  = dist/prerelease-055-final-candidate-repeat/DTMAPI
BuildCommit = f96c9cc61bf2
files / bytes = 30 / 71,552,124
primary DTMAPI-FileTree-SHA256-v1 =
  DA1C1980203F81702F97FA07365BDF4358C424DB39F6308AA8A7D3D899AEE0ED
primary DTMAPI-CandidateStructure-SHA256-v1 =
  48388423F87E1F2FE9D87D387A4B5C2E0C63FF3EBFD824160021762D51613825
BuildTime-normalized tree, primary and repeat =
  F33FCA39A6942704F98C530AE9BA8EB67111DD5070CA13DB80159AB75C3F1908
BuildTime-normalized structure, primary and repeat =
  9468E0C255AABA6BEB3EC149553F708A21CCE26C80AAD8B91A443894EC980549
primary release-manifest SHA-256 =
  911CC09047C815E8412DD872D74BF5ABE951C25CACA2CFC803134A36F3BAD4A2
root BAT = exact 1 / 2 / 3 / 4; root 0_probe absent
```

Only `Content/DTMAPI/release-manifest.json` differs between primary and repeat,
and only because of generated `BuildTime`; its normalized JSON SHA-256 is
`DD68E630EC6AB5C1E9D22E8DD9320A138F202458F20CB85D7CA23388543B8017`.
The final candidate assembly hashes are Bootstrap `758FC0F1...1328`,
Abstractions `3CD0EEE2...1295`, Core `7E0223E8...CFDA`, GameBridge
`4CF48116...2BC9`, ModConfigMenu `D7742D0B...795F`, Compatibility Host
`344EA850...9213` and Player Doctor `624E4F85...E4F`.

The final player-like audit at
`tmp/test-runs/DTMAPI Workshop Audit 20260730-f96-final/DTMAPI Workshop Audit
20260730-144213` passed with zero blockers. It parses all ten packaged scripts
under Windows PowerShell 5.1 and passes the offline missing/empty/valid
install, status, log collection and uninstall matrix.

The first from-start complete Release attempt began at
`2026-07-30T13:59:26.7565433+08:00` and is non-acceptance. It reached and
passed build, Unit/QA, Install Doctor, Player Doctor, packaged entrypoint and
runtime-evidence-retention gates, then correctly stopped because the newly
captured evidence made `evidence-retention-allowlist.json` stale. The allowlist
was rebuilt and its focused check passed.

The required diagnostic tail then found one source-only lifecycle-contract
projection lag: `run-game-smoke.ps1` already forbade PostMessage fallback for
the MoreEquipment transition route, while the Batch 4 semantic inventory still
froze the older predicate. Commit `96b95c8e` adds that exact route to the
existing required pattern, ordered token and meta-negative fixture. The
focused semantic boundary and meta-negative gates pass. The diagnostic tail
then reached all previously unexecuted Author SDK, dual-host transaction,
Candidate 11, Catalog, release-contract, Batch 4, GC/no-demand, ABI, artifact
and document gates without another failure. This tail is diagnostic only and
does not count as the requested complete Release PASS.

The one final authoritative from-start
`tools/scripts/test.ps1 -Configuration Release` run then passed with exit code
`0`. It ran from `2026-07-30T14:43:04.9569811+08:00` through
`2026-07-30T14:56:16.0800745+08:00` (`791.123` seconds) and reached build,
Unit/QA, Install Doctor, Player Doctor, package entrypoint, evidence retention,
dual-host Runtime/Author transactions, Candidate 11, Catalog,
release-contract, Batch 4, GC/no-demand, ABI, artifact-governance and
document-governance gates. The full log is
`tmp/test-runs/final-release-f96c9cc6.log`. This is the sole final complete
Release acceptance; the earlier allowlist failure and both diagnostic tails
remain non-acceptance.

That complete Release ran against tracked `f96c9cc6` plus the then-untracked,
read-only Review `20260730-0009`, which the committed evidence-retention
allowlist already enumerated and checked. A clean `f96c9cc6` checkout was
therefore not self-contained for that allowlist gate. The frozen package bytes
and completed Release results remain valid; the reproducible authority is the
frozen `f96c9cc6` artifact/complete Release plus the later documentation-only
closeout that tracks Reviews `0009`/`0010` and rebuilds the same allowlist. No
Runtime refreeze or complete Release rerun is attributed to that docs-only
repair.

## 2026-07-30 Selected-artifact release boundary

The publication-scope decision above re-selects the exact frozen
`dist/prerelease-055-final-candidate/DTMAPI` artifact, BuildCommit
`f96c9cc61bf2`, as the only Runtime 0.5.5 release input. This is an artifact
selection, not a claim that current HEAD source reproduces the same mandatory
Compatibility Host: commits after `f96c9cc6` that modify the paused
MoreEquipmentSlots Product/Host are deliberately outside this release.

The complete Release PASS and player-package matrix recorded above remain
applicable only if the frozen 30-file tree, release manifest and assembly
hashes revalidate exactly. The exact retained Workshop `3744059735`
compatibility target is completed below, and no current-HEAD rebuild or second
complete Release is required for that unchanged artifact. Actual upload
staging, Steam upload and post-upload subscription validation remain
separately blocked.

## 2026-07-30 Retained MoreEquipmentSlots And Publication Preflight

The exact retained Workshop input was validated read-only as item
`3744059735`, version `0.3.1-dtmapi`, with 9 files, 539,565 bytes and its
Catalog-normalized tree matching the frozen compatibility input. The valid
target run is `GAME-SMOKE/20260730-225518`:

- the Loader selected `source=Workshop`, `workshopId=3744059735` and
  `identity=Strict CodeMod`; all local first-party Product copies were
  physically isolated, so this was not the new Advanced ProductNative path;
- the dormant Compatibility Host became `resident; service=EquipmentSlots`;
- the old UI rendered three interactive and hoverable slots with
  `grandmas_button`, `box_hat` and one empty slot; the shield read `50/80`;
- real input hover and close passed, one ordinary native save completed, the
  two target items remained exactly represented by the legacy path, title
  return removed the UI/storage session state, and the game process exited;
- the test used a disposable, Steam-AutoCloud-isolated third-save fixture. It
  created no routine player-save backup and performed no player archive
  writeback. The Workshop subscription bytes were not changed.

`MoreEquipmentSlotsTransition=Passed`, but the aggregate runner
`RunStatus=Failed`: final disposable-root cleanup tried to delete
`runner.stderr.txt` while the outer harness still held it open. A separate
pre-UI input-frame callback warning recovered through the fallback pump and
did not set the runner failure. This is a harness cleanup failure, not evidence
that the old Mod path failed. The active smoke matrix therefore records this
as `partial`: the exact old-package compatibility target passes, while the
whole runner invocation is not relabeled as a PASS. Its accepted scope is one
isolated Mod, two fixed items and one normal save; no no-save rollback,
shield-break, disable/missing, cold-recovery, update-prompt or multi-Mod claim
follows from it.

The selected candidate's retained Manbo target was then rebound at
`GAME-SMOKE/20260730-232836`. The gate selected exact Workshop item
`3746319981` (`0.1.0-dtmapi`, 7 files / 229,384 bytes, tree
`23a3209e...c40e`, entry DLL `ab85c0...7ec4`), isolated the stale Local
duplicate, and observed exactly one Workshop load, Entry, registration and
WAV-ready result with zero compatibility failure. The base runner and gate
both passed on slot 3 in `NoNativeSave` mode; player archives and committed
sidecars were unchanged before cleanup, the official profile restored, the
process exited and the subscription tree remained unchanged. This is the
minimum old-package load/audio-registration claim only; it does not prove a
real paper-box interaction or multi-Mod coexistence. The run also discovered
and loaded three unrelated development-root Advanced products (Mine, the
deferred new MoreEquipmentSlots and StrongPlantingGun), which the official
profile switch does not physically isolate. Their behavior was not part of
this gate. The directly attributable Manbo source/Entry/register/WAV signals
still close the defined minimum target, but the run must not be described as
a Manbo-only profile.

After the gate, the shared game environment was restored to its exact
pre-test development Runtime (`BuildCommit=6751157c2d42`, Compatibility Host
SHA-256 `b37875a5...501f`), with zero `DolocTown.exe` processes and the Runtime
lock released. The frozen `f96c9cc6` candidate was not left installed as the
developer baseline.

The already accepted frozen-candidate package audit at
`tmp/test-runs/DTMAPI Workshop Audit 20260730-f96-final/DTMAPI Workshop Audit
20260730-144213` remains the package authority. It has zero blockers and covers
Windows PowerShell 5.1 parsing plus missing/empty/valid install, status, log
collection and uninstall. A later redundant invocation of the external audit
helper was stopped as non-acceptance before it tested the package: the helper
redirected BAT output but supplied no standard input, so the packaged
interactive `pause` waited indefinitely. That harness stall does not replace
or invalidate the earlier exact-candidate PASS.

The actual local Steam upload directory is not yet the selected candidate. It
still identifies an older build, lacks the selected Compatibility component
and contains the obsolete root `0_probe` BAT. This documentation-only task did
not mutate it. Before upload, explicit authority is required to replace its
DTMAPI payload with the frozen 30-file candidate while preserving only its
existing `workshop.json`, then run one player-like package audit against that
staged directory. Catalog `releaseStop` also remains `Active`; Steam upload
and post-upload subscription validation remain blocked.

## Evidence Boundary

The twelve-product implementation and runtime evidence remain owned by their existing Updates and the Batch 6 contract. The Step 3 Zoom package is an SDK-generated transaction fixture, not new product admission or publication evidence. The retained subscription baseline remains the compatibility source. The Runtime rollback archive is a private non-distribution recovery artifact outside the project tree and is verified only through an explicit path/default sibling contract; it does not make private bytes a default-suite dependency. The new cutoff report extends the existing exact-binary consistency lane. This Update does not copy product receipts and does not promote historical evidence into new default release gates. The old bucketed GameBridge runtime evidence remains historical evidence for the 2026-07-03 implementation; it is not a claim about the current runtime model.

The stronger public GC wording is a selected release statement. The Step 6
current-candidate focus found no contradiction and provides bounded structural,
lifecycle and no-demand support for it; it is not a quantified whole-game
allocation or crash-elimination claim. Candidate artifacts and the focused
evidence directories are local ignored outputs bound by the hashes above; they
are not a new tracked receipt family.

## Related Records

- Architecture: [Batch 6 Managed Mod Identity And Phase 0 Contract](../../architecture/batch6-managed-mod-identity-contract.md)
- Retained baseline: [Workshop Subscription And Prerelease Baseline Review](../../reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md)
- GC gate: [AutoFishing And ActionSpeed Active GC Release Gate](../../reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md)
- Correction acceptance: [DTMAPI 0.5.5 Pre-Release Correction Acceptance Audit](../../reviews/code/2026/20260729-0001-dtmapi-055-prerelease-correction-acceptance-audit.md)
- Release entry: [DTMAPI 0.5.5 Release Entry And Transition Matrix Audit](../../reviews/code/2026/20260729-0002-dtmapi-055-release-entry-and-transition-matrix-audit.md)
- Release blocker acceptance: [DTMAPI 0.5.5 Release Blocker Independent Review](../../reviews/code/2026/20260729-0003-dtmapi-055-release-blocker-independent-review.md)
- MoreEquipment transition fix audit: [Review 20260730-0009](../../reviews/code/2026/20260730-0009-moreequipment-transition-fix-and-test-audit.md)
- MoreEquipment release closeout re-audit: [Review 20260730-0010](../../reviews/code/2026/20260730-0010-moreequipment-transition-release-closeout-reaudit.md)
- Workshop copy: [0.5.5 Workshop Update Copy](../../releases/0.5.5-workshop-update-copy.md)
- Debug: ISSUE-010 and the active smoke matrix now own the new Step 6 runtime facts. Hook/API authorities are unchanged.

## Rollback Notes

Revert step commits in reverse order. Do not downgrade an SDK/installer pair while a schema-3 journal contains non-null `localInstall`; run the matching SDK's explicit `recover` first and verify the exact pre-state. Do not revert the Step 6 wrapper or remove an active-GC lease state while a run-owned original product/journal backup exists; with the recorded worktree lock stale and both owner/game processes absent, run the matching wrapper's `-RecoverOnly -EvidenceRoot <exact-run-root>` and require exact restoration first. Reverting either Step 3 correction alone restores a rejected owner or crash window, so restore the installer, SDK and Authoring.Contracts together only when no managed local install is active. This does not authorize deleting an installed product or changing an Author SDK receipt/journal by hand. Step 2 is an independent internal-name/file/comment change and can be reverted without changing serialized configuration. Reverting step 1 restores the obsolete bucket scheduler/config projection and the incorrect current AutoHarvest classification, so it must be treated as a full truth rollback rather than a product or save rollback. None of these rollbacks changes accepted product splits or makes G7, Mine, Oil, AnimalPack or ShellCrab publishable.

Do not revert `cc6b872d` while any managed local-install marker is pending:
that would reopen the rejected `RecoveryRequired` false-commit path in the
outer installer. Reverting `493de436` restores a fifth root player BAT and
invalidates the accepted layout. Reverting the MoreEquipment transition
Runtime changes or publishing the historical `493de436` candidate would also
discard the current Compatibility Host recovery bytes; either action requires
a new player-package freeze and transition acceptance.

## Follow-Up

The selected Runtime candidate is frozen, package-audited and covered by the
sole final from-start complete Release. Manbo and the exact retained
MoreEquipmentSlots `0.3.1-dtmapi` input have the required compatibility
evidence. New MoreEquipmentSlots `1.0.0`, its migration and the generic
protected-storage API remain deferred with no replacement R2 wave.

This Update remains `implemented / partial / open`, not `verified / closed`,
because the actual local Steam upload directory has not been synchronized to
the selected bytes, Catalog `releaseStop` is still active, and the manual
three-language paste/save/readback, Steam upload and post-upload
subscription/hash validation have not occurred. None of those external stages
is implied or authorized by this documentation closeout.

## 2026-08-01 Resolution

The historical state above is superseded only for local publication readiness
by [Update 20260801-0002](20260801-0002-workshop-upload-release-closeout.md).
That closeout freezes the later player-accepted Runtime/product bytes, restores
the full subscription root, synchronizes and audits the actual upload folders,
and adds exact existing-item Catalog exceptions while retaining the global
release stop. Manual Steam publication, language-page readback and post-upload
subscription/hash verification remain user-owned future steps.
