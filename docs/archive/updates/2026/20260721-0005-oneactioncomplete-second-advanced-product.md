# 20260721-0005 OneActionComplete Second Advanced Product

## Metadata

- Update ID: `20260721-0005`
- Date: `2026-07-21`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: products/oneactioncomplete/advanced/product-native/sdk/package/compatibility/catalog
- Source: User request to make the unique second-product admission decision after AutoFishing D.5, migrate OneActionComplete only if accepted, and compare the two real products before promoting any shared capability.
- Owning Review: [OneActionComplete Second Product Admission Review](../../reviews/code/2026/20260721-0005-oneactioncomplete-second-product-admission-review.md)
- Comparison Review: [AutoFishing / OneActionComplete Platform Comparison](../../reviews/code/2026/20260721-0006-autofishing-oneaction-platform-comparison.md)
- Acceptance correction Review: [Two-Product Baseline Closeout Feedback](../../reviews/manual-qa/2026/20260722-0001-two-product-baseline-closeout-feedback.md)

## Scope

This Update owns the accepted Checkpoint E migration of `Yuuka.DTMAPI.OneActionComplete` from a thin Strict consumer plus GameBridge executor to one self-contained managed Advanced product. It preserves UniqueID, Workshop item `3742763540`, version `1.1.2-dtmapi`, config path/keys/defaults, F11 config-page behavior and player-visible resource/fuel/feed semantics.

It adds one SDK-tracked policy for game build `23762374`, one canonical product Harmony owner, one atomic two-Hook inventory, one Catalog-driven SDK package route and a mandatory-Runtime zero-leftover check. The existing `IActionCompletionApi` ABI remains frozen and demand-inactive for prebuilt Strict compatibility; the new product does not consume it.

No third product, general Advanced authoring, public API growth, Content Host, 0.5.5 publication, complete Release suite, L0-L5 matrix, long run or additional game process is in scope.

## Native-owner boundary

- ProductNative: resource classification, `ResourceFellData` validation, remaining-hit/energy policy, `_Fell`, fuel/feeder selected-state checks, `CostSelf`, `AddFuel`, `AddFeeds`, logging and the product's exact-owner Hook lifecycle.
- Existing Platform: config read/write, input registration, events, translations, ModConfigMenu, Loader owner cleanup, SDK receipt/package, Doctor and Manager projections.
- Existing GameBridge/Compatibility: frozen `IActionCompletionApi` executor for old binaries only; demand-inactive without a consumer.
- Not promoted: a new shared `AgentStateInteract.OnExit` API or common product runtime. ActionSpeed and OneActionComplete share a patch point but not a common state holder or write/restore invariant.

## Focused slices

1. Admission authority and SDK policy/registry rejection gates.
2. Self-contained product, exact identity/config and atomic two-Hook lifecycle.
3. Package/install/catalog/Doctor/Manager projection and mandatory-Runtime zero-leftover gate.
4. Frozen Compatibility relabeling and focused ABI/demand checks.
5. Two-product comparison and final documentation/link/source validation.

## Changed files

- `products/first-party/OneActionComplete`: added the self-contained `netstandard2.0` Advanced product, exact existing assets/i18n/official metadata, unchanged config keys/defaults, F11 registration, seven source files and external-QA boundary note.
- `author-sdk/advanced-reference-policies`, Author SDK schemas, `DTMAPI.AuthorSdk`, `DTMAPI.Core` and `DTMAPI.InstallDoctor`: added and embedded the exact build-23762374 OneActionComplete policy/compiler-surface binding; registry acceptance remains exactly synthetic G2 plus AutoFishing plus OneActionComplete.
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/ActionCompletion`: relabeled the old executor as frozen compatibility, kept `IActionCompletionApi` unchanged and added bidirectional owner reconciliation. Product-first/request-second rejects without retaining demand; request-first/product-second drops all pending ActionCompletion demand at the physical install boundary before either compatibility Hook can install, while unrelated shared demand remains active. The new product has no GameBridge reference.
- `DTMAPI.sln`, `tools/scripts/build.ps1`, `testmods/OneActionCompleteMod` and `testmods/README.md`: retired the old Strict shell from the current build/source lane.
- `tools/scripts/build-batch6-advanced-product.ps1`, the thin OneActionComplete wrapper and `test-batch6-oneactioncomplete-advanced-product.ps1`: use one Catalog-selected Advanced validate/build/pack/package implementation while retaining product-specific native-owner/Hook/lifecycle checks.
- `tools/scripts/release-common.ps1`, `build-release-workshop-packages.ps1`, `install-to-game.ps1`, `check-release-contract.ps1`, `check-product-catalog.ps1`, `test.ps1` and install/uninstall transaction tests: generalized the two-consumer Author SDK builder/package/artifact-root and live zero-leftover projections while rejecting an unadmitted third product.
- `src/DTMAPI.GameBridge.DolocTown.QA`, `tools/scripts/run-game-smoke.ps1` and the product title log: added external product-state/effect observation and physical F11 provenance for the bounded migration acceptance without adding a player QA ABI.
- Catalog, publish projection, semantic inventory, Unit tests, public API matrix, ActionCompletion Hook map, Batch 6 authority and roadmap records were updated to the new physical owner and compatibility status.

## Validation

- `test-batch6-oneactioncomplete-advanced-product.ps1`: PASS (`source-files=7`, `hooks=2`, one exact product policy, five build-23762374 native authorities).
- Catalog-driven Release validate/build/pack: PASS; reports have zero warnings/errors, one 34,304-byte entry DLL and no bundled native dependencies. The initial closeout package SHA-256 is `80865702888BB78B996611A7E0E085B5A56099C13C9341AE27AA644587675E0C`; entry SHA-256 is `FF21C54AD9B7FC03E6EC7C3853194D53011BB31955705940CF848A66BAEFE2E5`.
- `DTMAPI.GameBridge.DolocTown` focused Release build: PASS with zero warnings/errors after the compatibility move.
- `DTMAPI.UnitTests`: PASS, including exact policy/registry/Core binding and the relocated frozen compatibility assertions.
- Bidirectional compatibility-owner unit: PASS. Product-first/request-second stores no demand; request-first/product-second clears only the ActionCompletion policy/parent/children before Hook installation and preserves an unrelated shared demand.
- `DTMAPI.AuthorSdk.Tests`: PASS; the author schema and template selector admit exactly G2, AutoFishing and OneActionComplete, while arbitrary identities remain rejected.
- `DTMAPI.InstallDoctor.Tests`: PASS; the Doctor embeds the exact three-row registry, verifies the OneActionComplete receipt/owner/policy and keeps installed-game hash/build checks read-only and fail-closed. The first focused run exposed the stale two-row Doctor/schema authority; after updating that authority, the complete Doctor test executable passed all ten cases.
- `check-product-catalog.ps1`: PASS (`products=27`, `public=11`, `workshop-items=21`, `api-rows=47`) with live zero-leftover checks for both Advanced products.
- Batch 4 QA semantic boundary and meta-negative inventory: PASS (`productionSourceFiles=188`, contract coverage `25/25`, pattern-negative coverage `149/149`); ordinary packages still exclude optional QA.
- `test-player-runtime-only-uninstall.ps1`: PASS; exact two-product Author SDK set excluded from the legacy directory lane.
- `test-developer-official-local-install-transaction.ps1`: PASS on two hosts; rollback, drift preservation and fail-closed boundaries remain intact.
- `test-batch6-phase0-contract.ps1`: PASS as a reproducible historical contract; no historical receipt was rewritten.
- Windows PowerShell 5.1 syntax parsing: PASS for the new builder/test and all modified release/install/full-suite wiring.
- Final document governance: PASS (`5583` checks); `git diff --check`: PASS.
- One bounded third-save game acceptance: PARTIAL against the final Advanced package. The product loaded from LocalDevelopment, installed exactly two product-owned patches, attached its callback, passed resource completion, wrong-tool non-match, fuel/feed completion and ActionSpeed coexistence, opened ConfigMenu through foreground physical F11 without `PostMessage`, logged title/save restoration, restored QA/source/deployment state and exited without a residual process. It did not exercise partial-energy behavior, ConfigMenu value save/reload or actual OneActionComplete owner deactivation.
- Complete Release, L0-L5 and long tests were intentionally not run.
- Follow-up `GAME-SMOKE/20260722-141220`: PASS. It exercised partial-energy behavior, configuration save/readback/reload and real final Loader owner deactivation. The product went from one loaded instance, two actual Harmony patches across two targets and one callback to zero instance/patch/target/callback/Core roots; save/config/source/profile/deployment restoration and clean process exit passed.
- The package accepted in `141220` is `BD713A6FFF28BF0FBE74AF36DD57C56379D28CCC9AD4A393E49204C6C68699DC`. The later additive `IItemDisplayNameApi` rebuild produced current package `4BC483DD4EF242B2CF14DF904BE6BBBED417EECC6A6874FD3B83E0A19E58F619`; OneActionComplete source/behavior did not change. `GAME-SMOKE/20260722-161022` re-exercised resource completion, partial energy, config reload and exact two-patch owner deactivation against that current byte image.

## Evidence

- Accepted receipt: `docs/debug/evidence/ONEACTIONCOMPLETE-ADVANCED/20260722-third-save-accepted/acceptance-summary.json` (`Status=Passed`, user save slot 3, exact final package/entry hashes).
- Game smoke root: `docs/debug/evidence/GAME-SMOKE/20260722-080842`.
- The smoke result's original `OneActionMenuKey=Failed` label was a harness classification defect: foreground physical F11 and the product `OpenConfig` log passed, but the QA terminal exited the process before the best-effort Escape. The corrected runner requires physical open provenance and treats that late close as not applicable; the accepted summary records the correction without changing product behavior.
- Three earlier actual processes were non-acceptance harness diagnostics: one selected a disabled Workshop duplicate, one overlapped title cycling with the pending functional case, and one attempted F11 after the QA terminal. Each restored source/deployment state and left no residual process; none is counted as acceptance.
- Existing `GAME-SMOKE/20260713-221610` remains the pre-migration behavior baseline only.
- The 2026-07-22 continuation first added focused fixtures for partial energy, config save/readback/reload and final real Loader owner deactivation. `GAME-SMOKE/20260722-125239` stopped at the preceding ActionSpeed Entry/case failure, so none of those OneActionComplete conditions executed in that historical attempt.
- The later corrected process `GAME-SMOKE/20260722-141220` supersedes that continuation gap and is the authoritative completion evidence for the three missing conditions. The package used there has SHA-256 `BD713A6FFF28BF0FBE74AF36DD57C56379D28CCC9AD4A393E49204C6C68699DC`.

## Rollback

Remove the OneActionComplete Advanced policy/product/package admission and restore the prior Strict consumer route from version control. Keep the frozen ABI compatibility path and AutoFishing D.5 result intact. Do not delete or alter player Workshop/local data as part of source rollback.

## Follow-up

The package/ownership rehome and corrected runtime boundary are closed: one Catalog-driven Advanced builder/validator and generic live zero-leftover projection remain Platform; SharedNative promotion is zero because AutoFishing and OneActionComplete share no native owner. No further product, general Advanced authoring, G7 or 0.5.5 publication follows automatically.
