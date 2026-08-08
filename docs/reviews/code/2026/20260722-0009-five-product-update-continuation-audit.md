# Five-Product Update Continuation Audit

**Review ID:** `20260722-0009`
**Date:** 2026-07-22
**Status:** recorded — source findings and current-DLL runtime acceptance closed by Update `20260722-0004`
**Scope:** current uncommitted Batch 6 continuation through FishBreedingAssistant and AnimalHusbandryProgress; no implementation fix, sixth-product admission, G7 implementation, Release, L0–L5, GC ladder, long test or 0.5.5 publication

## Question

Does the current update follow the agreed lightweight/ownership roadmap closely enough to freeze the five-product baseline and begin another product migration? If not, which work may continue without expanding the current correction boundary?

## Audited Baseline

- Branch: `codex/major-update-batch0-20260713`.
- HEAD: `3eba68b90986415548c0ca829f695faf0bd07f52` (`Batch 6: harden Advanced product owner closeout`).
- The audited working tree contains 70 changed tracked paths and 57 untracked files. It combines the fourth and fifth products, their shared adapter, current QA/tooling/documentation changes and unrelated portable reverse-capture work.
- The current Batch 6 contract records five separately admitted real Advanced products and `GAME-SMOKE/20260722-161022` as the latest Fish/Animal shared-adapter runtime evidence.
- This Review is audit-only. The existing Fish and Animal Updates remain the implementation lifecycle owners; this Review creates no new Update, receipt, schema or acceptance authority.

## What Is Genuinely Complete

1. `GAME-SMOKE/20260722-141220` closes the corrected ActionSpeed nine-target behavior/owner cleanup and the previously missing OneActionComplete partial-energy, configuration reload and owner-deactivation evidence. Those products need no additional game rerun for this audit.
2. FishBreedingAssistant and AnimalHusbandryProgress now physically own their distinct Hooks, product state, rendering/formatting and exact Harmony owners. The old public executors are retained as demand-inactive Compatibility rather than being presented as the new product implementations.
3. Comparing two real consumers correctly identified one common native responsibility: read-only `DolocAPI.QueryItemProto(itemId).Title`. No common Harmony owner, product state machine, reflected UI engine or content table was promoted to SharedNative.
4. `GAME-SMOKE/20260722-161022` is useful runtime evidence for the current Fish/Animal package behavior, native close and exact Loader owner cleanup. Failed Steam/cloud launch attempts were retained as infrastructure evidence rather than counted as product acceptance.
5. Validation proportionality has mostly held: the current continuation did not run complete Release, L0–L5, GC or long tests.

These are meaningful ownership corrections. They do not yet prove that the mandatory Runtime became smaller: the frozen Compatibility executors remain compiled into GameBridge.

## Findings

The user's screenshot feedback is preserved below in its original numbered order so the correction can be resumed without relying on the image:

1. **P1: title lookup has incorrect negative caching.** Failed or empty `IItemDisplayNameApi` lookups must not enter the cache.
2. **P1: Fish/Animal closeout is not fully exception-isolated.** State restoration, static callback detachment and exact-owner unpatch must each be attempted even if an earlier step throws.
3. **P1: five-product authority state contradicts itself.** Current-truth documents must converge on the five-product verified boundary without rewriting historical observations.
4. **P2: focused QA is becoming a multi-product hard gate.** Final owner-deactivation assertions must operate on the explicitly requested owner set.
5. **P2: policy/tooling duplication keeps expanding by hand.** Registry projection and the general Advanced build/validation path must stop adding product-specific ID/hash/order/resource branches, and one test run must reuse one Author SDK build.
6. **P2: the result is a correct ownership rehome, not mandatory-Runtime slimming.** The retained Compatibility bodies must stay visible in the measurement and wording until a later consumer scan plus optional-host or breaking-removal decision.

On 2026-07-22 the user selected **Option A** for `IItemDisplayNameApi`: keep it as a public `Experimental` API limited to main-thread, read-only `itemId -> localized display name` lookup; cache only successful non-empty results; never negatively cache a failure; clear the cache on `SaveLoaded`, `ReturnedToTitle` and runtime-environment reset. It must not grow into item DTO, enumeration, mutation, Hook, formatting or UI surface. FishBreedingAssistant and AnimalHusbandryProgress remain its two independent real consumers, while all title formatting, animal progress, UI, Hooks and product state remain ProductNative. No 0.5.5 stability promotion is authorized.

Resolution work and evidence are tracked only in [Update 20260722-0004](../../../updates/2026/20260722-0004-five-product-baseline-correction.md); this Review remains the pre-implementation finding record.

### P1 — Shared item-name lookup violates its accepted cache contract

`ItemDisplayNameService.TryGetDisplayName` writes `cache[itemId] = displayName` after both success and failure. A missing item, reflection failure or transient native-query failure therefore stores an empty string until the next save/title/environment clear.

That behavior conflicts with the API matrix and Fish/Animal comparison, which both say that only successful non-empty titles are cached. It can also retain an unbounded set of invalid IDs in mandatory Runtime memory through a public Experimental API. The retained runtime smoke used valid Fish and Animal IDs and does not exercise this failure path.

Before freezing the adapter:

- cache only a successful non-empty title;
- add a small test to the existing Unit surface for success reuse, failure non-retention and lifecycle clear;
- state the supported thread/lifecycle boundary for the Experimental API instead of implying a general thread-safe dictionary contract.

### P1 — Fish and Animal owner closeout does not isolate callback detachment

Both new ProductNative runtimes call `ResetBoundary` and then detach their static callback inside one `try`. If state/native cleanup throws first, exact Harmony unpatch is still attempted, but the callback can retain the product instance.

This repeats the closeout shape corrected for ActionSpeed and OneActionComplete at HEAD. Callback detachment, state/native restoration and exact-owner unpatch must each be attempted independently, with failures aggregated only after all cleanup steps run. Existing success-path evidence cannot prove this exception path.

### P1 — Current-truth authorities contradict the five-product state

- `PROJECT.md` still embeds the obsolete three-product state, old ActionSpeed/OneActionComplete gaps and a fourth-product block. Per workspace rules, changing admitted-product counts belong only to the Batch 6 contract; PROJECT should state the stable identity/ownership rule and link to that authority.
- The Batch 6 contract's current-admission block says all five products are runtime verified, while earlier paragraphs still say the OneAction continuation was not executed and Animal remained runtime-unaccepted.
- The public API matrix contains current Fish/Animal rows but retains dated notes saying corrected ActionSpeed was unverified, OneAction gaps remained open and a fourth product was unauthorized.
- Review `20260722-0004`, Review `20260722-0005`, the Fish smoke row and Fish product README retain obsolete post-correction statements. In particular, the README says Fish owns the item-name lookup/cache and consumes no GameBridge even though the current product requires `IItemDisplayNameApi`.

Historical Reviews should remain historical; add concise supersession/resolution links where their current status or post-audit disposition is misleading. Do not copy the full five-product narrative into every authority.

### P1 — The claimed verified state is not reproducibly frozen

HEAD predates the fourth/fifth products and shared adapter. The current code, packages, policies, evidence documents and unrelated reverse-capture files are still mixed in a large dirty tree, while the two product Updates already use verified language.

Do not stack a sixth product on this state. Correct the current candidate, pass focused checks, isolate unrelated reverse-capture work and commit one reviewable five-product baseline before any new admission implementation.

### P2 — Combined owner QA is becoming a multi-product hard gate

`AdvancedProductOwnerDeactivationFixture` now unconditionally requires and deactivates ActionSpeed, OneActionComplete, FishBreedingAssistant and AnimalHusbandryProgress, while the smoke runner preflight only declares the first two prerequisites. Reusing this switch for one product's focused repair would silently require the other products and turn a small acceptance into a growing integration matrix.

Keep the existing QA system, but make requested/selected owners explicit or product assertions optional/data-driven. A final multi-product integration scenario may still request the exact set. Do not create another fixture, receipt family or acceptance ladder merely to solve this coupling.

### P2 — Policy/tooling duplication has crossed its previously recorded trigger

The ActionSpeed weight audit allowed one separately reviewed next product but recommended genericizing the policy registry before a batch of further products or when another product copied the same constants. The fourth and fifth products now add hand-maintained policy IDs, UniqueIDs, hashes, counts/order and embedded-resource rows across Core, Author SDK, Doctor, schema and project files.

The general test path also invokes the generic SDK product builder repeatedly per product. Its cost therefore scales linearly with every admitted product even when the underlying Author SDK input is unchanged.

After the correctness baseline is frozen and before a sixth implementation, project the exact fail-closed policy set from the tracked registry/resource set and reuse one Author SDK build per test run. This is consolidation of an existing authority, not permission to create a second registry or cache-pass system.

### P2 — Ownership rehome has not yet reduced mandatory Runtime weight

A rough C# source measurement against HEAD puts the mandatory Runtime about 241 lines higher, while the two new optional product assemblies contain roughly 1,281 lines. More importantly, approximately 1,391 lines of Fish/Animal Compatibility execution remain in mandatory GameBridge.

This is not evidence that the migration was wrong: product-owned behavior has moved out and the retained old ABI path is intentionally frozen for 0.5.5 compatibility. It means the honest result is **ownership rehome**, not **DTMAPI body slimming**. Physical reduction depends on the planned retained-DLL consumer scan and a separately designed optional Compatibility Host or later breaking removal boundary.

### P2 — Animal product hot-path cost remains a follow-up risk

The migrated Animal renderer retains frequent reflected type/member resolution, temporary argument arrays, LINQ ordering, string construction and reflected UI traversal while the overlay is active. This appears inherited from the previous implementation rather than introduced by the ownership move, so it does not invalidate ProductNative placement. It should receive a later product-local D.5-style allocation/refresh audit before the product is described as lightweight or GC-optimized.

## Validation Performed For This Audit

- `tools/scripts/check-doc-governance.ps1`: PASS before this Review (`5654` checks).
- `git diff --check`: PASS; line-ending conversion warnings only.
- `tools/scripts/test-batch6-fishbreedingassistant-advanced-product.ps1`: PASS (`source-files=5`, `hooks=1`, `policies=1`, `native-authorities=3`).
- `tools/scripts/test-batch6-animalhusbandryprogress-advanced-product.ps1`: PASS (`source-files=6`, `patches=4`, `targets=3`, `policies=1`).
- Existing recorded focused gates cover ActionSpeed, OneActionComplete, Catalog, Batch 5 demand Unit, QA Unit and both SDK package paths.
- No game, complete Release, L0–L5, GC or long test was run by this audit.

The two product source gates do not currently cover failed item-name caching or cleanup-step exception independence, so their PASS does not close the P1 findings.

## Continuation Decision

### Allowed now

1. Correct the current five-product candidate only: positive-only item-name caching, independent Fish/Animal closeout, focused owner-QA selection and authority-document drift.
2. Add focused tests to existing Unit/QA/source gates, rebuild the two affected SDK packages and run Catalog/package checks.
3. Because game-loaded bytes will change, run one combined third-save Fish/Animal short acceptance after focused checks pass. It needs valid name lookup, representative Fish/Animal behavior and both exact owner cleanups. It does not need complete Release, L0–L5, GC or a long test, and it does not need to rerun ActionSpeed/OneAction behavior.
4. Freeze and commit the corrected five-product baseline separately from unrelated reverse-capture work.
5. In parallel, read-only/design work may continue for the frozen-ABI consumer scan, public API consumer/owner/thread audit, residual production-QA seam audit, unresolved G1 native-owner Reviews and Oil/Mine official-JSON/economy design.

### Next after the baseline commit

1. Consolidate the tracked Advanced policy projection and reuse the Author SDK build without adding new assurance authorities.
2. Complete the retained Workshop/first-party ABI consumer scan and decide whether one optional Compatibility Host can remove frozen executors from the ordinary player Runtime while preserving 0.5.5 binaries.
3. Use those results to choose between Compatibility-host work and a separately reviewed sixth-product candidate. Under the existing roadmap, MoreSaves precedes MoreEquipmentSlots; candidate analysis is allowed, implementation is not pre-authorized.
4. Perform the Animal product-local allocation/refresh follow-up independently of mandatory Runtime slimming.

### Still blocked

- a sixth Advanced product implementation or any general Advanced authoring lane;
- Content Host G7 implementation;
- broad GameBridge deletion before the consumer/compatibility decision;
- complete Release, 0.5.5 packaging/publication and unrelated GC/long-test campaigns.

## Disposition

**NO-GO** for immediately adding another migrated product or calling the five-product tree frozen.
**GO** for the bounded current-candidate correction and the parallel read-only/design tasks listed above.

The shortest safe route is correction -> focused checks -> one Fish/Animal short runtime acceptance -> authority cleanup -> isolated five-product commit. The next work that can produce genuine DTMAPI body reduction is Compatibility consumer/hosting work, not merely moving a sixth feature into another product DLL.

## Resolution

Update `20260722-0004` closes findings 1-6 with positive-only caching, independently attempted ProductNative cleanup steps, reconciled current authority, explicit requested-owner QA, registry-driven Advanced policy projection, one same-run Author SDK build reuse and corrected physical/non-empty measurements. The Animal hot-path allocation concern remains an explicitly separate product-local follow-up.

`GAME-SMOKE/20260722-180502` is the accepted runtime boundary. Two preceding real runs, `174252` and `175832`, retained valid Fish/Animal in-save receipts but stopped during native ReturnHome because the QA-owned Animal panel close and ReturnHome request shared one frame. A one-second continuous normal-Gameplay quiescence gate fixed the QA race; the accepted run then passed the isolated Fish/Animal third-save behavior, exact requested-owner deactivation, title recovery, byte-identical save restoration and clean exit. Complete Release, L0-L5, GC, long testing and 0.5.5 publication were not performed.

The audit's pre-correction NO-GO was satisfied for the exercised Fish/Animal behavior and product-owner boundary. It still does not admit a sixth product or authorize the deferred Compatibility-host/removal work.

## Post-Commit Audit

Commit `c29393a521261653575f2aabca51e583c99168fa` includes this Review, Update `20260722-0004`, the five-product candidate and the retained `GAME-SMOKE/20260722-180502` product evidence without mixing in the unrelated portable reverse-capture work. Fresh focused builds and the ItemDisplayName, QA Unit, Author SDK, Install Doctor, Fish, Animal and Catalog checks pass.

One P1 lifecycle defect remains: `ItemDisplayNameFeature.EnvironmentReset` exists, but the real `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset` entry currently invokes only Camera and lifecycle counters. The Unit test calls `ItemDisplayNameService.Clear` directly, so its EnvironmentReset assertion bypasses the missing dispatch. SaveLoaded and ReturnedToTitle still use the normal feature route, and the accepted Fish/Animal behavior/owner evidence remains valid.

Update `20260722-0004` therefore returns to `implemented/partial/open`. Before another product implementation, add the existing independent EnvironmentReset step for ItemDisplayName and prove cache invalidation through the bridge entry. This requires focused source/Unit validation only; it does not require another game process, Release, L0-L5, GC or long test.

Qualified continuation remains allowed for read-only Compatibility consumer scanning, public API/owner/thread review, residual QA seam review, unresolved G1 native-owner analysis, Oil/Mine official-JSON design and the Animal product-local allocation audit. A sixth product implementation, G7, broad GameBridge removal and 0.5.5 publication remain blocked.

## Post-Commit Correction Resolution

Commit `025fdf3e` gave ItemDisplayName its own exception-isolated `RunEnvironmentResetStep`, but its focused Unit called the bridge method directly. A second review of the production chain found that `DolocApiSetEnvCameraPostfix` still used a Camera-only demand gate, so Fish/Animal-only use could never reach that fanout and the first correction was not sufficient.

The completed source correction adds an independent `ItemDisplayName.EnvironmentReset` lifecycle-protection demand and retained callback bit, and moves the one physical `DolocAPI.SetEnvCamera` Postfix into a non-disableable SharedNative HookBridge used by both Camera and ItemDisplayName logical routes. A successful lookup returns its native title immediately but cannot enter the positive cache until that physical Hook reports ready; install failure therefore remains uncached/fail-closed. Physical install requests and readiness counts deduplicate the common `camera.set-env` route. Focused Unit exercises the production callback route, zero-demand sleep, Hook-not-ready retry, guarded caching, Camera-disabled ownership and synchronous release. Current-DLL `GAME-SMOKE/20260723-074311` adds two real third-save cycles, both independent product queries, HookProbe readiness, two real callback clear/releases, title recovery, re-query, restoration and clean exit; Update `20260722-0004` is therefore `verified/passed/closed`. No Release, L0-L5, GC or long test ran.

The parallel read-only continuations are recorded separately in Reviews `20260722-0010` (retained ABI/optional Compatibility Host), `20260722-0011` (production QA seams/Animal refresh), API Review `20260722-0001` (public consumer/owner/thread cleanup) and API Review `20260722-0002` (G1/Oil/Mine). None admits a sixth product or broadens this correction.
