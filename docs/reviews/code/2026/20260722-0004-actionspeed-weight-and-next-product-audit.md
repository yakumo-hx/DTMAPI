# 20260722-0004 ActionSpeed Weight And Next-product Audit

Date: `2026-07-22`

Status: `recorded — its ActionSpeed/OneAction blockers were resolved by 20260722-141220; later five-product corrections are owned by Update 20260722-0004`

Scope: Audit the corrections made after the two-product readiness review, the third-product `ActionSpeed` migration at committed HEAD `8625554a`, whether those changes actually reduce the mandatory DTMAPI Runtime, and which product should be considered next. This is an audit-only Review. It does not implement a fix, admit a fourth product, publish `0.5.5`, run a complete Release suite, run L0-L5, or launch a long game test. Untracked reverse-capture work was excluded.

## Executive conclusion

Do not start a fourth or multi-product Advanced migration yet.

The third migration is a real **ownership correction**: the current first-party ActionSpeed DLL owns its nine reviewed native Hooks, action state, configuration and restoration implementation; it does not consume `IActionSpeedApi`, add a public API, or place ProductNative code into a new SharedNative service. It also reuses the common ConfigMenu, Loader, SDK, Catalog, Doctor, Manager and package route.

It is **not a DTMAPI mandatory-Runtime reduction**. The complete old `IActionSpeedApi` executor remains in mandatory GameBridge Compatibility while the product adds a second, highly overlapping implementation. From the frozen two-product commit to current HEAD, mandatory Runtime C# grows by 86 physical lines; the ActionSpeed-related production source grows from 1,616 to 3,180 physical lines. This is compatible with the current `0.5.5` old-binary promise, but it must be described as “ProductNative ownership rehome; compatibility weight deferred”, not as completed body slimming.

There are three ActionSpeed continuation blockers:

1. a retained/unknown old-ABI `AutoFillBottle`-only consumer with a different UniqueID can retain the Compatibility updater while the new ActionSpeed product installs, creating two automatic-bottle-fill executors without a Harmony-owner collision;
2. ActionSpeed and OneActionComplete owner deactivation can skip exact-owner unpatch if native-state reset throws;
3. current ActionSpeed QA observes an internal patch counter and config-disable behavior, not the real Harmony target/owner set or actual Manager owner deactivation, while the new Advanced DLL still lacks a successful game run.

Separately, the prior OneActionComplete acceptance does not cover three continuation conditions later imposed by the multi-product readiness audit, so the subsequent “all five issues closed” wording is stronger than the retained evidence. Active authority prose is also stale, and two P2 scaling costs remain. These should be corrected, but they must not be inflated into another large assurance program or made hard prerequisites for exactly one fourth-product ownership review.

The next work should first close the bounded ActionSpeed lifecycle/runtime gaps and make the current authority internally consistent. Repeated SDK/Core scaling cost and the Compatibility retirement route should then be handled proportionally: they block a claim of true body slimming or a batch of migrations, but not one separately reviewed fourth-product ownership rehome.

## Audited state and method

- Branch: `codex/major-update-batch0-20260713`.
- Audited HEAD: `8625554a` (`Batch 6: migrate ActionSpeed as third Advanced product`).
- Comparison point: `cce873b9`, the committed two-product closeout.
- Earlier mandatory-Runtime comparison: `ff0f5fc3` before the AutoFishing ProductNative rehome and `f6ebfe10` after its accepted migration.
- Mandatory Runtime means C# under the five player-loaded projects: `DTMAPI.Abstractions`, `DTMAPI.BepInExBootstrap`, `DTMAPI.Core`, `DTMAPI.GameBridge.DolocTown`, and `DTMAPI.ModConfigMenu`. QA projects, tools, tests and product DLL sources are excluded from this metric.
- Physical and non-empty counts were both computed from tracked Git blobs, so generated `obj` files and the unrelated current untracked files are excluded.
- The ActionSpeed native targets were reopened in the tracked build-`23762374` reverse reference. It remains reference-only; no official or decompiled source is copied here.

## Findings

### P1 — AutoFill-only compatibility can survive product acquisition

The bidirectional ActionSpeed exclusion is complete only for policies which request a Harmony Hook route.

`ActionSpeedService.Configure` gives an `AutoFillBottle`-only policy demand only on `ActionSpeedAutoFill`. That route is an updater and explicitly does not request Hook installation. GameBridge reconciliation currently runs at `InstallGameBridgeFeatureHooks`, while its assembly-load retry only reacts to `Assembly-CSharp` and `0Harmony`. Loading the managed ActionSpeed product therefore does not itself trigger Compatibility reconciliation.

The product installer rejects GameBridge ownership by inspecting the unique ActionSpeed Harmony targets. An AutoFill-only compatibility policy has installed no such target, so the product has nothing to detect:

```text
old Strict consumer configures AutoFillBottle only
-> Compatibility updater and policy become active, with no ActionSpeed Harmony owner
-> managed ActionSpeed product loads and sees no conflicting Hook owner
-> product installs its nine Hooks and its own automatic-fill updater
-> no later Compatibility Hook-install boundary is required
-> both automatic-fill executors can remain active
```

The existing order tests configure policies that also request tool/interaction Hooks, or invoke reconciliation directly. They do not cover this updater-only order.

The only old first-party ActionSpeed package found in the current source/Catalog has the same UniqueID as the new product and normal source selection should choose only one of them. The concrete overlap therefore requires a retained or unknown third-party old-ABI consumer with another UniqueID; no such published consumer was confirmed in this audit. It remains a real frozen-public-ABI path that the compatibility promise must fail closed against, not evidence that the known official old and new ActionSpeed packages normally run together.

Required correction:

- reconcile all frozen ActionSpeed policy and child demand at the Platform/managed-product-owner activation boundary, not only at a Harmony installation boundary;
- keep the product free of a reverse GameBridge dependency;
- add one AutoFill-only test for each load order and prove the old updater/policy count becomes zero before the product starts processing updates;
- reuse the current demand and owner authorities; do not add a receipt or a second ownership system.

### P1 — owner deactivation can skip exact-owner unpatch

`ActionSpeedNativeRuntime.DeactivateOwner` currently detaches the static callback, calls `engine.ResetBoundary`, and only then calls `hooks.UnpatchOwnedHooks`. If reset throws, unpatch is never attempted. `OneActionNativeRuntime.DeactivateOwner` has the same sequence.

AutoFishing already has the correct shape: attempt runtime restoration and exact-owner unpatch independently, then aggregate failures. ActionSpeed and OneActionComplete should use the same failure semantics without sharing their ProductNative engines.

This is a real lifecycle defect, not just an evidence gap. The static callback root has already been detached, so this path does not leave that callback attached to live product state. It can still leave the exact Harmony owner/patch inventory installed (normally inert against the detached runtime) merely because restoration error reporting interrupted cleanup.

### P1 — current ActionSpeed QA cannot prove the promised owner cleanup

The ActionSpeed external observer reads `InstalledPatchCount` from the product's installer object. It does not query Harmony for all nine reviewed target methods and exact owner IDs. A stale internal count can therefore still say `9` after a failed or incomplete unpatch.

The current “disable recovery” fixture saves `Enabled=false` and deliberately requires all nine Hooks to remain installed. That is valid configuration-disable behavior, but it is not Manager owner disable, `Dispose`, callback detachment or exact-owner unpatch. The Hook map currently groups these different boundaries too broadly.

Before runtime acceptance, the focused QA seam must:

- query the actual Harmony owner inventory for the nine target methods;
- distinguish config disable (Hooks installed but inert) from owner deactivation (callbacks detached and exact owner absent);
- exercise one real Manager/Loader owner-deactivation cleanup at the final point where no further product action is expected in that process.

### P1 — ActionSpeed remains unverified in the game

At audit opening, the sole game attempt `GAME-SMOKE/20260722-100419` loaded the old Strict OfficialLocal ActionSpeed source and then let G6 interrupt G5. The source-selection and ordering bugs were corrected with focused source/unit checks. The later correction run and its additional product failure are recorded in the correction-execution section below.

The current `implemented`, runtime-pending status is correct. It blocks a fourth product. A single bounded third-save run is sufficient after the P1 code/observer corrections; complete Release, L0-L5 and a long run are not required.

### P2 — OneActionComplete closeout wording exceeds its evidence

The product did load from the final Advanced package and the retained run proves two product-owned patches, resource success, wrong-tool non-match, fuel/feed behavior, ActionSpeed coexistence, physical F11 opening, title/save restoration, environment restoration and clean exit.

The original OneActionComplete admission Review explicitly did not require another game run. A later multi-product readiness audit made the following three items continuation conditions before calling the enlarged two-product closeout complete:

- energy shortage or partial-energy behavior;
- changing, saving and reloading a ConfigMenu value, rather than only opening the page;
- actual OneActionComplete owner disable/deactivation cleanup.

`ONEACTIONCOMPLETE-ADVANCED/20260722-third-save-accepted/acceptance-summary.json` contains no result for those three conditions. Its ConfigMenu section records only `F11Open`, and `QaHostCleanup` is cleanup of the external QA host, not cleanup of the OneActionComplete owner. The later readiness/manual-QA closeout and broad Update/smoke wording therefore should not say that every continuation condition passed.

Resolve this in one of two bounded ways:

1. downgrade the retained claim to a partial/bounded runtime acceptance; or
2. include the three missing OneActionComplete cases in the next short third-save process, then retain `verified` only if they pass.

No full suite or long test is needed for either choice. This is a retained-evidence correction, not by itself a hard blocker on a separately reviewed fourth product.

### P1 — ActionSpeed is an ownership rehome, not mandatory-Runtime slimming

#### Mandatory Runtime trend

| Git state | Runtime C# files | Physical lines | Non-empty lines | Change from previous state |
| --- | ---: | ---: | ---: | ---: |
| `ff0f5fc3`, before AutoFishing rehome | 161 | 80,099 | 71,726 | baseline |
| `f6ebfe10`, accepted AutoFishing rehome | 149 | 75,911 | 67,874 | `-4,188` physical |
| `cce873b9`, frozen AutoFishing + OneActionComplete | 149 | 75,987 | 67,943 | `+76` physical |
| `8625554a`, current ActionSpeed implementation | 149 | 76,073 | 68,022 | `+86` physical |

Batch 6 still has a real net mandatory-Runtime reduction of 4,026 physical lines relative to the pre-AutoFishing point. That reduction came from the AutoFishing move. The second and third product migrations have since added 162 physical lines to mandatory Runtime because their old executors remain and each admission adds policy/registry/compatibility wiring.

#### ActionSpeed-specific trend

| Shape | Old two-product state | Current third-product state |
| --- | ---: | ---: |
| GameBridge ActionSpeed executor | 1,400 | 1,473 Compatibility |
| player product shell/engine | 216 Strict-shell lines | 1,707 Advanced ProductNative lines |
| ActionSpeed-related production total | 1,616 | 3,180 |

The new `ActionSpeedEngine` has 952 non-empty lines. A trimmed-line multiset comparison finds 902 of them verbatim in the current Compatibility service (94.75%); after ignoring short structural lines and considering only lines at least 20 characters long, 90.43% still match. This is strong evidence of retained duplicate implementation, not merely similar naming.

The Compatibility service is also instantiated when GameBridge constructs its features, so `demand-inactive` prevents old Hook/updater work but does not remove the old code, IL or compatibility state objects from the mandatory assembly.

The three migrated compatibility domains currently occupy:

| Frozen compatibility domain | Physical lines |
| --- | ---: |
| FishingAutomation | 4,164 |
| ActionCompletion | 696 |
| ActionSpeed | 1,473 |
| Total | 6,333 |

Catalog's zero-leftover rule correctly rejects the old product classes and feature path, but deliberately permits the complete Compatibility directory when its frozen ABI token exists. That rule proves ownership separation; it does not prove net Runtime reduction.

### P2 — generic product tooling still rebuilds the Author SDK six times

The previous correction genuinely removed copied per-product builders: each product wrapper is about 14 lines and the Catalog-driven builder/checker is shared. That is a valid structural fix.

Execution cost was not consolidated. `build-batch6-advanced-product.ps1` builds the Author SDK every time it is called. `test.ps1` invokes the generic builder twice for every Advanced product to compare primary/repeat artifacts. With three products this rebuilds the same SDK six times in one suite, and the cost grows linearly with each admission.

The previous readiness audit's “build the SDK once per suite” condition is therefore only partially closed even though its status says all findings were corrected.

Use a validated prebuilt SDK root or a single build-then-product-loop entry. Keep each product's deterministic primary/repeat package comparison, and do not create a new receipt/schema/checker family.

### P2 — every product still adds hand-maintained identity constants to Core

`AdvancedReferencePolicyAuthority` embeds per-product policy IDs, UniqueIDs, policy hashes, compiler-surface hashes, a hard-coded policy count and an exact hand-written admitted array. `DTMAPI.Core.csproj` also adds one `EmbeddedResource` item per product.

This is why the third product added mandatory Core code even though the policy registry already contains the same rows. Before a series of further migrations, the Runtime should generically validate one existing embedded registry authority and its policy resources fail-closed. This is consolidation of the current authority, not permission to add another registry or receipt.

### P2 — active constraint documents disagree about the third product

`PROJECT.md`, the Batch 6 identity contract and the roadmap describe ActionSpeed as the sole implemented third product with runtime revalidation pending. Root `AGENTS.md` and `docs/workflows/codex-api-rebuild.md` still say that exactly two products are admitted and every third product is blocked.

That contradiction can cause a fresh agent either to reject the current committed tree or to treat the stale sentence as permission to redesign the authority. Before continuation, make onboarding/workflow documents route current milestone truth to the Batch 6 identity contract instead of copying a fast-changing product count.

This correction must not be interpreted as fourth-product admission.

### P2 — two retained evidence descriptions remain too broad

- `docs/debug/regressions/smoke-matrix.md` still describes the older AutoFishing `004352` run as having verified input/animation/native cleanup even though the D.5 Update classifies it as preliminary and the `074938` row is the deep-state replacement.
- OneActionComplete's Update/manual-QA/smoke wording calls the incomplete original acceptance set fully passed, as described above.

Correct the claims in their owning records; do not add a compensating receipt.

### P3 — no-consumer reconciliation probes Harmony before the cheap empty check

Both ActionCompletion and ActionSpeed reconciliation call `managedProductOwnerPresent()` before checking whether the compatibility policy dictionary is empty. Reversing the short-circuit order avoids unnecessary reflection/Harmony inspection when no old consumer exists. This is a small cleanup, not a continuation blocker by itself.

## Corrections that are real and should be retained

The audit does not invalidate the whole closeout. These changes are sound:

- OneActionComplete's pending Hook-demand load order now reconciles at the physical GameBridge install boundary, and focused unit coverage proves the Hook-bearing bidirectional case without deleting unrelated shared demand.
- AutoFishing's deep observer now covers the input override, visible-reel state, Ready holders, Animator snapshots, gravity/velocity snapshots and Pull duration; the final retained deep run records them as zero.
- AutoFishing's line metrics now compare physical-to-physical and non-empty-to-non-empty.
- the shared Catalog-driven Advanced builder, package projection, SDK receipt, Doctor/Manager identity and zero-leftover checker were reused for ActionSpeed; a third large builder family was not copied.
- ActionSpeed is physically ProductNative, uses `netstandard2.0`, resolves all nine targets before its first patch, rolls back an atomic failed install, owns one exact Harmony ID, uses the common ConfigMenu API, and adds no public API or false SharedNative promotion.
- focused ActionSpeed/OneActionComplete source checks, managed unit tests, the ActionSpeed SDK package build and package hash comparison passed during this audit.

These results establish architecture and focused build correctness only. They do not close the P1 runtime/lifecycle gaps above.

## Correction execution result

The bounded correction implemented the P1 source and QA work without expanding the platform surface:

- AutoFill-only Compatibility now reconciles before updater work, and focused unit coverage passes both product-first and compatibility-first orders while preserving unrelated shared demand;
- ActionSpeed and OneActionComplete detach callbacks, attempt native restoration and exact-owner unpatch independently, then aggregate failures;
- QA now inventories the exact Harmony owner on all reviewed targets and has one terminal path through the real Runtime `DeactivateOwner` boundary after title recovery;
- the OneActionComplete continuation fixture now contains partial-energy and ConfigMenu save/readback/reload checks, but those cases have not yet completed in game;
- Catalog, live zero-leftover, both product source checks, focused managed Unit/QaUnit checks and both SDK package builds passed. Complete Release, L0-L5, GC and long tests were not run.

The first correction process, `GAME-SMOKE/20260722-125239`, loaded the actual receipt-verified Advanced ActionSpeed DLL. It exposed an additional product defect: the ninth target was declared as `DolocTown.AgentStateBase`, while build `23762374` defines `AgentStateBase` in the global namespace. Atomic pre-resolution threw before any Hook installation; Core rolled the Entry transaction back with zero cleanup failures and no exact ActionSpeed owner patch. The ordered QA stopped at the first ActionSpeed case, so neither the new ActionSpeed lifecycle proof nor the three OneActionComplete continuation cases ran. Save/config/source/profile/QA restoration, no-fatal, process-exit and no-residual-process checks passed.

The product installer, QA inventory, Hook map and focused source gate now use global `AgentStateBase`. Corrected package `CE71C3E40DC38EF8E1ACBDA7585ADB64DB485FFA4A85557313994AF1E6A381CB` passes SDK validate/build/pack. It was not rerun before this checkpoint because the bounded-run recommendation was incorrectly treated as a one-process quota. The user has corrected that inference: the corrected package may receive the same focused short acceptance without separate authorization. Until it passes, the disposition remains `NO-GO` for a fourth product.

## Required next work before a fourth product

Use one bounded closeout, not another assurance system:

1. **Correct current authority wording.** Remove stale “third blocked” milestone copies from onboarding/workflow prose and route them to the Batch 6 contract.
2. **Fix owner arbitration and cleanup.** Close AutoFill-only reconciliation and make ActionSpeed plus OneActionComplete deactivation attempt exact-owner unpatch even after restore failure.
3. **Strengthen the existing ActionSpeed QA seam.** Observe actual Harmony target owners and perform actual final owner deactivation; do not add a player ABI.
4. **Run focused validation.** Source/unit/Catalog/package checks only for the touched boundaries.
5. **Run the bounded third-save acceptance.** Verify the actual Advanced ActionSpeed package, nine exact-owner Hooks, representative Tool/ConfigApply/Interaction behavior, config disable, title/re-entry restoration, final Manager owner cleanup and clean exit. If a run finds another valid, fixable defect, pass its focused checks and rerun this smallest acceptance as needed; this does not authorize Release, L0-L5, GC or long testing. If safe in the same ordered process, also cover OneActionComplete partial-energy, config save/reload and final owner cleanup; otherwise downgrade its existing evidence claim instead of starting a separate long campaign.
6. **Freeze an honest three-product baseline only after runtime acceptance.** Then record “ProductNative ownership complete; frozen Compatibility Runtime weight deferred”. Complete Release, L0-L5, GC soak and `0.5.5` publication remain separate later boundaries.

The following P2 work is recommended but is not a hard gate for one separately admitted fourth product:

- let a suite/batch build Author SDK once and reuse it for per-product deterministic package comparisons;
- correct the superseded AutoFishing row and bounded OneActionComplete evidence wording;
- scan the retained/public Workshop DLL set for consumers of the three frozen APIs and record whether Compatibility slimming is actionable;
- genericize Core's existing embedded policy registry before a batch of further products, or when the next product would otherwise copy another set of constants; do not turn that safety-sensitive refactor into a prerequisite merely to improve a line count.

For a future migration to be called **mandatory-Runtime slimming**, Compatibility must also physically leave the ordinary load path. The recommended `0.5.5`-compatible route is to preserve Abstractions ABI and old behavior in one optional Compatibility host which Loader activates only when a legacy consumer is actually selected. If that work is deferred, a fourth product may still be admitted as an ownership rehome after the P1 closeout, but it must not be reported as body slimming.

The “mandatory Runtime must have a negative final delta” condition for future slimming should be a recorded comparison in the product's admission Review/Update, not a new permanent receipt or gate family.

## Candidate order after the closeout

The existing roadmap lists `MoreSaves / MoreEquipmentSlots` after ActionSpeed. That is a product-priority order, not the lowest-risk way to prove true Runtime reduction. If body slimming is the immediate objective, revise the candidate order as follows; every row still needs one separate native-owner/admission Review.

| Suggested order | Candidate | Current product shell | Candidate mandatory GameBridge domain | Reason and risk |
| ---: | --- | ---: | ---: | --- |
| 1 | FishBreedingAssistant / Fish roe display | 183 lines | 365 lines | Best small proof. One current consumer; fish-specific title/detail and native fish-name fallback are ProductNative. Remove or optionalize the executor rather than copying it again. Low-to-medium UI/title validation. |
| 2 | AnimalHusbandryProgress | 157 | 1,097 | One current consumer and clear product display ownership, with meaningful possible reduction. Repeated viewer switching, clone teardown, native close and title lifecycle make it medium/high risk. Animal species/economy design remains deferred to the separate animal-product remake. |
| 3 | ChestLocatorEnhancer | 101 | 542 | One current consumer, but the global available-inventories owner and multi-owner policy/consumption semantics need a conflict review. Medium/high risk. |
| 4 | Zoom | 221 | 1,513 | Product policy has already moved, but CameraView leases/arbitration are a plausible shared adapter and background/fog ownership remains incomplete. Do not delete the adapter merely to improve the count. |
| 5 | MoreSaves | 93 | 885 | This follows the old roadmap, but official save UI, copy/delete/restart recognition and future paging/naming make data/lifecycle validation high risk. |
| 6 | MoreEquipmentSlots | 92 | 2,814 | Largest theoretical reduction and highest blast radius: protected per-save sidecars, UI clones, recovery/overflow, hats/shields and hot-disable cleanup. Do it after MoreSaves, not in the same batch. |
| 7 | StrongPlantingGun | 119 | 1,019 | ProductNative candidate, but inventory transfer and seed/film/fertilizer behavior carry item-loss risk; the current product is unpublished/rebuild-pending and should stay later. |

Independent tracks:

- **Y-key DebugConsole:** moving `ReflectedDebugConsoleUi` (currently 2,335 Bootstrap lines) to an optional diagnostic product/host is likely a larger real mandatory reduction, but it is a diagnostic-platform seam, not an ordinary fourth gameplay Mod.
- **CustomAnimals and AudioReplacement:** continue toward optional Content Host G7; do not copy their large declarative engines into one Advanced product. G7 remains blocked until its own decision.
- **Mine/Oil:** keep static content in official JSON; do not create an Oil DLL. Mine's time/economy/production behavior can be reviewed later as ProductNative or ContentOwner.
- **AutoHarvest:** remains a never-publish API-demand sample, not the next first-party product.

If strict roadmap order is preferred despite risk, `MoreSaves` is the fourth candidate and `MoreEquipmentSlots` the fifth. The audit recommendation is instead to use FishBreedingAssistant as the first small no-duplication slimming proof, then compare it with AnimalHusbandryProgress before attempting save/equipment state.

## Validation performed

- inspected the current product, Compatibility, demand, Hook, cleanup, ConfigMenu, Catalog, SDK builder, test runner, Core policy-registry and QA-observer source;
- reopened the nine named native ActionSpeed targets in the tracked build-`23762374` reverse reference;
- compared tracked source counts at `ff0f5fc3`, `f6ebfe10`, `cce873b9` and `8625554a`;
- passed `test-batch6-oneactioncomplete-advanced-product.ps1`;
- passed `test-batch6-actionspeed-advanced-product.ps1`;
- passed the managed `DTMAPI.UnitTests` run on the repository-local .NET 8 toolchain;
- passed the Release ActionSpeed Advanced pilot package build and reproduced the Update-owned package hash;
- acquired and released the shared runtime lock; an installation preflight first failed before launch on a stale LocalDevelopment source digest, which was cleared without a game process;
- launched one game process at `GAME-SMOKE/20260722-125239`; it loaded the Advanced DLL and retained the fail-closed/restoration evidence described above;
- after correcting the global-namespace target name, reran the focused ActionSpeed source check, optional-QA Release build/QaUnit checks and Catalog-driven SDK package build successfully;
- did not run complete Release, L0-L5 or a long/GC test; no second game process occurred before the user corrected the mistaken launch-quota inference.

## Disposition

Checkpoint decision at audit time: `NO-GO` for a fourth product implementation.

Post-audit disposition (2026-07-22): the blocker closed in `GAME-SMOKE/20260722-141220`, which passed corrected ActionSpeed behavior and exact nine-target owner cleanup and also closed OneActionComplete's continuation gaps. Separate Reviews then admitted FishBreedingAssistant and AnimalHusbandryProgress. Their comparison promoted only the common read-only `QueryItemProto(...).Title` responsibility as the Experimental `IItemDisplayNameApi`; Fish data/title formatting and Animal viewer state/Hooks remain product-owned. Fish's original migrated behavior passed `145154`; after the Steam cloud choice was resolved, `GAME-SMOKE/20260722-161022` verified the current shared-adapter Fish package and Animal DLL together. The current admission authority and correction/freeze lifecycle are the Batch 6 contract and Update `20260722-0004`, not this historical checkpoint decision.
