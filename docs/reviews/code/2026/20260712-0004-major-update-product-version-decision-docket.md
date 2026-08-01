# 20260712-0004 Major Update Product And Version Decision Docket

Status: recorded / user decisions confirmed; Oil drop tuning remains an implementation input
Date: 2026-07-12
Scope: user clarification and focused source review for Oil/Mine, DTMAPI/Mod version semantics and public naming, AutoHarvest identity, and the next decision rounds
Related Update: `docs/updates/2026/20260712-0009-major-update-product-version-decision-docket.md`
Refines: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

2026-07-13 refinement: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` keeps this first-round product direction but chooses append-only Oil weight 25 instead of rebalancing base coal/amber entries, and makes new Workshop Mod plus stale manually installed Runtime the primary compatibility scenario. The later record is authoritative for those two implementation inputs.

## Source Request

After the full boundary audit, the user clarified the intended maturity and direction of Oil/Mine, requested a SMAPI-like version model with DTMAPI moving to 0.5.5 and functional Mods normalized, questioned the public use of the word Runtime, and clarified that AutoHarvest originated as an API response to another author's automatic-harvest Mod. The user requested that remaining decisions be split into understandable options with reasons.

This is a docs/source decision review. It does not change versions, manifests, product identities, APIs, JSON, Hooks, packages, game files, or Workshop state.

## 1. Oil And Mine Clarification

### User statement

Oil drop should theoretically be expressible through official JSON. OilMod was not completed as a formal product. It was paired with Mine, which also was not promoted to a published Mod. Mine may be able to be driven more by official JSON. It currently reuses an enlarged well visual, consumes electricity, and produces minerals; the known visual gap is that the held placement preview does not receive the enlargement. Development stopped around the earlier release push. The boundary identified by the audit should still be split.

### Analysis immediately following issue 1

This changes the maturity classification but not the source defect:

- Oil and Mine are DeveloperOnly prototypes in `Get-DtmApiDeveloperOfficialModDefinitions`, not Published products. The full audit's Oil finding must be read as **unpublished experimental product behavior leaking into the base framework**, not as a regression of a shipped Oil product.
- `OilMod.Entry` registers only menu text and logs. Base GameBridge nevertheless constructs `OilCoalDropFeature`, installs the shared ToolCollider route, hard-codes `crude_oil`, performs an independent `8%` roll after a coal resource is removed, and injects that callback into OneAction. The ownership split remains mandatory.
- official JSON already owns Oil item identity, price, fuel value, display/source metadata, and icon. Official `mod_tbmoditemspawnextension.json` can extend the native `coal_mine_drop` LUT. That route makes Oil a native weighted candidate among the coal mine's existing drop draws; it is not numerically equivalent to the current extra independent `8% x1` backpack grant.
- Mine's JSON already owns the item, equipment identity, `8x6` footprint, `16/4` storage, recipe/workbench route, and native electric component declaration. It does not by itself own the 120-game-minute scheduler, output weights, Mod-mineral discovery, low-power/catch-up behavior, output placement, conditional Oil recipe, or tech graph mutation. Those are currently a DTMAPI sidecar loop and remain Experimental.
- `VisualScale=2` exists only in the Mine code definition; the official equipment JSON has no scale field. Existing builder Hooks target one indicator path, but repeated player evidence proves the real held preview remains 1x. Directly creating/observing a placed Mine is not placement-preview proof.

### Oil drop options

| Option | Meaning | Benefit | Cost/risk |
| --- | --- | --- | --- |
| A - official native LUT | Add Oil to `coal_mine_drop` through official extension JSON; remove OilCoalDrop Feature/service and OneAction coupling. | Lightest and most native; no product-specific framework Hook. | Drop probability must be rebalanced using native weighted-LUT semantics rather than assuming the old independent 8%. |
| B - owner-bound extra drop | Keep an independent extra percentage, but Oil explicitly registers a generic resource-drop rule; GameBridge knows native resources/transactions, not `crude_oil`. | Can reproduce current 8% semantics. | Requires a new Experimental API/Hook and native-owner review. |
| C - Mine-only Oil | Oil exists only as Mine output/fuel and does not drop from coal. | Smallest safe initial product. | Removes the coal-drop feature. |

Recommendation: **A**, followed by real drop-distribution testing and economy tuning. Choose B only if the independent extra 8% behavior is itself a product requirement.

### Mine architecture options

| Option | Meaning | Benefit | Cost/risk |
| --- | --- | --- | --- |
| A - two-layer Mine | Official JSON owns static item/equipment/storage/recipe; Mine owns economy/cycle/output policy; GameBridge exposes only demand-activated native electricity/instance/storage/preview adapters. Keep the machine bridge first-party/internal Experimental. | Correct current boundary without prematurely promising a generic machine framework. | Still needs native-owner work for scheduling/save/catch-up and focused Mine QA. |
| B - public generic JSON Machine API now | DTMAPI reads generic machine definition JSON for arbitrary authors. | Most extensible on paper. | Prematurely stabilizes a sidecar scheduler without a proven native save/sleep/all-room owner. |
| C - freeze Mine | Remove its base-runtime roots and keep the product dev-only until native scheduling and art are ready. | Fastest framework weight reduction. | Mine remains unavailable for the current release line. |

Recommendation: **A**. B is blocked by the existing machine native-owner gaps.

### Mine visual options

| Option | Meaning | Benefit | Cost/risk |
| --- | --- | --- | --- |
| A - dedicated Mine sprite | Use a correctly sized/anchored Mine asset and scale 1 for both placed and preview states. | Long-term clean solution; removes scale/pool contamination Hooks. | Requires final art. |
| B - scoped temporary preview adapter | Continue reusing the well but activate a Mine-only preview adapter and validate the real held-placement path with screenshots/pixels. | Allows prototype progress before art is ready. | Remains lifecycle/pooling sensitive. |
| C - retain known dev limitation | Do not fix preview until final art exists. | No throwaway Hook work. | Prototype retains a visible defect. |

Recommendation: **B short-term, A before formal 1.0.0 publication**.

Preserve during any split: `DTMAPI.OilMod`, `crude_oil`, `DTMAPI.MineMod`, `dtmapi_mine`, `dtmapi.mine`, existing footprint/storage, pure-electric 10-per-cycle intent, 120-game-minute default, and existing placed-equipment save recognition. Never revive the retired `dtmapi_oil` id.

## 2. Version And Public Naming Clarification

### User statement

Use a SMAPI-like version model. Functional Mod bodies can be normalized to `1.0.0`. DTMAPI can move to `0.5.5`. The largest goal is to unify concepts such as current and minimum version. The public product probably should not be called Runtime.

### Analysis immediately following issue 2

The intended simplification is sound, with one important distinction: **one authority and one comparison policy do not mean that a Mod's minimum required DTMAPI version must always equal the latest DTMAPI version**.

Recommended external model:

```text
DTMAPI product version          0.5.5
file/informational projection   0.5.5.0 / 0.5.5  (generated, not a second player-facing version)
assembly compatibility identity keep 0.5.3.0 for the 0.5.5 line, pending old-DLL Unity Mono proof
functional Mod version         each product's own semantic version
minimum DTMAPI version         MinimumDTMApiVersion
real Mod-to-Mod dependency     Dependencies[].MinimumVersion
```

For the 0.5.5 boundary reset, every formal first-party product entering the selected 1.0.0 rewrite epoch explicitly uses `MinimumDTMApiVersion=0.5.5` as its supported platform baseline. After that, a Mod remains compatible with 0.5.5 until it actually consumes a later API; packaging validates the source declaration and must not silently insert or rewrite its minimum on every DTMAPI update. Legacy/external Mods may retain a truthful lower bound.

2026-07-13 sixth-round refinement: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` owns the final public sequence and compatibility scope. DTMAPI 0.5.5 is published once after all ten baseline gates, AutoFishing is the fixed compatibility Canary, and the publicly obtainable external DTMAPI ecosystem receives a dated compatibility scan. Unobtainable private binaries are outside the default blocker set.

The three current Runtime version literals in `DtmApiRuntime`, root `Directory.Build.props`, and `release-common.ps1` should be projections from one machine-readable authority. Root MSBuild metadata must stop applying the DTMAPI product version to ordinary Mod DLLs.

### DTMAPI 0.5.5 options

| Option | Meaning | Reason |
| --- | --- | --- |
| A - `0.5.5` | Plain 0.x semantic version; numeric file projection `0.5.5.0`; assembly compatibility identity is handled separately. | Recommended. `0.x` already communicates pre-1.0, while the current comparer strips `-alpha` and does not implement real prerelease ordering. |
| B - `0.5.5-alpha` | Continue the current display channel. | Less immediate wording change, but preserves a suffix whose ordering is currently fake. |
| C - true prerelease policy | Use values such as `0.5.5-alpha.1` with a new SemVer comparer. | Valid future option, but adds migration complexity when a plain 0.5.5 is sufficient. |

Recommendation: **A**.

### Functional Mod `1.0.0` reset

QA fixtures/examples are not functional products and should not be normalized to 1.0.0.

For player-facing products, the user's `1.0.0` normalization can be treated as a one-time stable-product epoch **only if existing live Workshop/subscription packages did not expose a higher authoritative version**. Current source manifests include `1.4.3-dtmapi` AutoFishing, `1.3.4-dtmapi` ActionSpeed, `1.1.3-dtmapi` FishBreeding, `1.1.2-dtmapi` OneAction, and `1.0.1-dtmapi` AnimalHusbandry. Resetting those numeric values to 1.0.0 is a downgrade if they were externally authoritative.

Options:

| Option | Meaning | Benefit | Cost/risk |
| --- | --- | --- | --- |
| A - public history remains monotonic | Unpublished products start at 1.0.0; already published products retain/increase their real public version. | Correct update/dependency ordering. | Official functional Mods do not all display the same number. |
| B - one-time 1.0.0 epoch reset | Declare old `*-dtmapi` labels development-only and set every formal functional product to plain 1.0.0. | Matches the requested clean slate. | Requires checking live Workshop/subscription packages first; AutoFishing is the known likely downgrade. |
| C - all products use 2.0.0 | One common version higher than every current product version. | Monotonic and uniform. | Misleadingly implies a major product generation for Mods that were below 1.0. |

Pre-decision recommendation: **A unless live-package verification proves B is safe**. The user subsequently selected B explicitly as an authorized formal-product epoch; the follow-up resolution below is authoritative and requires status/update tools to understand the epoch.

### Public replacement for Runtime

| Layer | Recommended term |
| --- | --- |
| Workshop/product/player UI | `DTMAPI` |
| Chinese dependency explanation | `DTMAPI 前置` |
| developer architecture | `DTMAPI Core`, `Bootstrap`, `GameBridge` |
| actual process/lifecycle language | runtime / 运行时 |

Do not globally rename internal `DtmApiRuntime`, runtime lock, historical package kinds, or compatibility identity `DTMAPI.Runtime` as part of 0.5.5. That is high-churn work with no player benefit. Hide technical Runtime wording from the public product instead.

Keep `MinimumDTMApiVersion` as the canonical manifest field. `MinimumApiVersion` already exists as a compatibility alias and should not become a second author-facing spelling. Longer term, ordinary functional Mods should depend on DTMAPI as one framework rather than manually declaring every co-packaged internal GameBridge/ModConfigMenu provider unless a real optional provider boundary remains.

## 3. AutoHarvest Clarification

### User statement

AutoHarvest originated because another author had an automatic-harvest Mod and the user proposed writing an API. It now appears to have the same kind of functional/product boundary question.

### Analysis immediately following issue 3

AutoHarvest differs from Oil/Mine:

- it is not a hidden always-on DTMAPI product and is not a current GC hot path;
- `CropHarvestingFeature.Update()` is empty and installs no Harmony Hook;
- the sample owns enablement, interval, key, target selection, filters, limits, logging, and config;
- GameBridge runs only when the consumer explicitly calls the API and hides native `PlantBasin`/room/equipment types.

The current code therefore contains a partly healthy platform/product split. The remaining problem is that the public API itself offers a high-level whole-farm scan and batch-harvest operation, so platform traversal and product-like batching are combined. The only ordinary consumer is the clean-room AutoHarvest sample; CropHarvestingQA is a fixture, not a second product. No real external DTMAPI consumer is known.

### AutoHarvest identity options

| Option | Meaning | When to choose |
| --- | --- | --- |
| A - first-party functional product | Move to `first-party-mods`, own long-term UX/config/compatibility, and publish as 1.0.0. | Only if the project explicitly wants to ship “DTMAPI Auto Harvest”. |
| B - author sample | Remove official/publish identity and keep a small readable example under `samples`. | Useful after the API has a stable shape. |
| C - compatibility/API demand sample | Keep demand, contract tests, and QA value; do not ship the current full AutoHarvest as a product. | Recommended for its stated origin and current adoption. |

Recommendation: **C now**, with a small B-style example later if the API remains public.

### Crop API options before 0.5.5

| Option | Meaning | Benefit | Cost/risk |
| --- | --- | --- | --- |
| A - retain current API | Keep whole-farm `ScanMatureCrops` / `HarvestMatureCrops`. | Least churn. | Bakes product-scale scan/batch policy into the platform. |
| B - lower-level Experimental API | GameBridge returns crop-container DTOs/transient handles and offers single-target revalidation + `TryHarvest`; Mod owns schedule, scope, selection, batching, and messages. | Recommended clean platform/product boundary while native types stay hidden. | Requires an Experimental API replacement/migration before 0.5.5. |
| C - internalize until real adoption | Remove the author-facing API for now and keep only internal/QA paths. | Smallest surface. | Does not serve the original external API goal. |

Recommendation: **B**, subject to the required native-owner method/body review. Current name-string heuristics for vines/mushroom bags/bushes must not become Stable categories without native-owner proof.

## Initial Decisions Requested In This Round - Resolved Below

These were the six product-visible choices presented before the user's follow-up. All are resolved in the later section:

1. **DTMAPI public version:** adopt plain `0.5.5` with generated numeric `0.5.5.0`?
2. **Functional Mod version epoch:** use monotonic public versions, or explicitly reset every formal functional Mod—including AutoFishing—to plain `1.0.0` after live-package verification?
3. **Public name:** show only `DTMAPI` / `DTMAPI 前置`, while keeping internal Runtime names for compatibility?
4. **Oil drop semantics:** accept official weighted-LUT behavior, or preserve the independent extra 8% through an owner-bound bridge?
5. **Mine direction:** use the recommended two-layer Mine and temporary preview adapter until dedicated art exists, or freeze Mine until art/native scheduling is ready?
6. **AutoHarvest:** keep it as a non-published compatibility/API demand sample and reshape the API lower, unless the user explicitly wants a first-party AutoHarvest product?

The original recommended answer set was `1=yes; 2=monotonic unless live evidence permits the explicit 1.0.0 epoch; 3=yes; 4=official LUT; 5=two-layer + temporary adapter; 6=non-published demand sample + lower API`. The user deliberately selected the 1.0.0 epoch and confirmed the other recommended boundaries.

## Later Decision Rounds

The following are intentionally deferred so this round does not mix product identity with unrelated implementation details:

1. optional QA host activation/package design and Compatibility warning windows;
2. final Published/Developer/QA product roster after the version catalog exists;
3. GMCM/Manager player information hierarchy;
4. MoreSaves slot count, naming, scrolling, and migration UX;
5. Y-console optional product host and redesign scope;
6. merged animal pack roster/economy/production ownership;
7. short SFX versus BGM bridge boundaries.

## User Decision Resolution - 2026-07-12 Follow-Up

The user resolved the first decision round. The numbered order below preserves the follow-up request and its analysis together.

### 1. Oil And Mine

#### User-confirmed direction

- inspect whether the local fish-expansion Mod's new rarity buckets and rewritten weights can be applied to coal;
- Oil may be a rare native output or a genuinely additional output, but must not dilute the coal mine's existing rare product and must not remain a fixed `x1` side grant which bypasses native bonuses;
- delete GameBridge's `crude_oil` hard-code and OneAction-to-Oil coupling;
- keep Mine static content under official JSON;
- Mine owns its cycle, output policy, and economy;
- GameBridge retains only demand-activated electricity, instance, storage, and preview native adapters;
- the electricity route is unverified, was later removed from Mine settings, and must be rewritten or validated before it is treated as usable;
- temporarily fix the real held-preview path; before formal release, replace the enlarged well with dedicated Mine art and remove runtime 2x scaling.

#### Analysis immediately following issue 1

Yes: the preview/art choice is the visual part of the confirmed **two-layer Mine**. Official JSON remains the static layer; Mine remains the product-policy layer; the temporary preview adapter is a narrowly scoped native seam and is retired with the 2x prototype art.

Electricity remains an Experimental, disabled-by-default/capability-gated adapter input, not a proven Mine feature and not a player setting. Existing JSON component declarations and an untested launch path are insufficient evidence. It needs a current native-owner rewrite/revalidation plus real low-power, restore-power, save/load, sleep/catch-up, disable, and title-return tests.

The focused native review `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md` answers the fish/coal question:

- fish uses a two-stage rarity bucket; coal uses a flat weighted LUT with 3-4 native draws;
- the fish structure cannot be copied, but its denominator-rebalancing principle can;
- official JSON can make Oil a rare native candidate and explicitly preserve amber's original per-draw probability by rebalancing amber after adding Oil;
- Oil then replaces normal coal slots, not amber probability. With unlimited native count it can produce more than one and participates in the native stone-collection count bonus;
- official JSON cannot make a truly independent extra roll. If “extra without reducing coal or amber” later becomes mandatory, it requires a new generic native-resource-output review, not restoration of the Oil-specific Hook.

Selected direction: use the native rare-output route, preserve amber's probability mathematically, leave the exact Oil weight to economy/distribution testing, and retire the current Oil Hook/service/OneAction callback.

### 2. Version System And Naming

#### User-confirmed direction

- DTMAPI becomes plain `0.5.5`;
- every formal functional Mod starts the rewrite epoch at plain `1.0.0` and is gradually rebuilt;
- preserve compatibility for existing Workshop Mods which already declare DTMAPI as a prerequisite;
- complete a repository-wide readability and naming review before broad public announcement/release.

#### Analysis immediately following issue 2

This explicitly selects the one-time **1.0.0 formal-product epoch**, including products whose current source labels have a higher numeric `*-dtmapi` prefix. Those old labels are pre-rewrite development lineage, not the authoritative ordering for the new formal product epoch. The version catalog/update checker must represent that epoch deliberately; plain numeric comparison must not misreport the formal rewrite as an accidental downgrade.

Compatibility is a separate axis from product version display. The 0.5.5 line must keep reading legacy `MinimumDTMApiVersion` values and the existing `MinimumApiVersion` input alias, retain compatible public contracts/provider IDs/assembly identities or provide explicit facades, and diagnose rather than silently abandon old Workshop Mods. Release tooling must stop rewriting every Mod's minimum requirement to the latest framework version.

The focused compatibility review `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md` found 12 locally subscribed Code Mod DLLs referencing `DTMAPI.Abstractions` 0.5.1.0 or 0.5.2.0. It also found one confirmed public deletion: the existing Workshop AutoFishing DLL calls `FishingAutomationOptions.set_StopOnManualMove`, while the current Abstractions DTO no longer contains that member. Restoring and behavior-checking that obsolete member, proving a zero-deletion API diff, and loading representative old DLLs without recompilation are 0.5.5 release gates.

Public version, file version, and assembly binding identity are projections with different jobs. Use `0.5.5` publicly and `0.5.5.0` as file metadata, but keep the current `0.5.3.0` assembly compatibility identity for the 0.5.5 line until the real old-DLL Unity Mono matrix proves otherwise. This is not a second user-visible DTMAPI version.

The naming review is a pre-public-release gate, but it should follow the major boundary moves so code is not renamed twice. It may freely improve private/internal classes, files, methods, ownership terms, and comments. Public manifest fields, provider/Unique IDs, assembly/API identities, serialized keys, config paths, and historical evidence require a compatibility/migration decision rather than a cosmetic rename. Public player wording is `DTMAPI` / `DTMAPI 前置`; internal “runtime” remains valid only for real process/lifecycle concepts until the naming audit decides otherwise.

Current source evidence sharpens that order: `DtmApiRuntime` is still a roughly 3,900-line central coordinator referenced across 62 source files, while active `Refactor.*` diagnostics/configuration remain widespread. First extract QA and product domains, shrink the coordinator, then rename it by responsibility (provisional target `DtmApiHost`). Public-release cleanup should retire or domain-name active `Refactor.*` identifiers and split the currently mixed manifest kind, provider kind, release package kind, and catalog scope concepts. Real technical terms such as Unity/Mono runtime, runtime instance/id, runtime lock, and historical evidence remain correct and must not be globally replaced.

Compatibility identities frozen through the 0.5.5 transition include BepInEx GUID `dev.dtmapi.bootstrap`, registry id `DTMAPI`, existing provider IDs, `DTMAPI.Abstractions` public ABI, published Mod Unique IDs, manifest aliases, and current install/config schemas. The release catalog's `DTMAPI.Runtime` lookup key may accept an old alias while the public product becomes `DTMAPI`; it is not permission to rename provider identities.

### 3. Crop Harvesting And AutoHarvest

#### User-confirmed direction

- GameBridge exposes crop-container query DTOs/transient handles, revalidation, and single-target `TryHarvest` only;
- there will be no first-party AutoHarvest product;
- the existing AutoHarvest sample was a process artifact from trying to make DTMAPI feel like a complete SMAPI-style product.

#### Analysis immediately following issue 3

This selects the lower Experimental API and rejects framework-owned scheduling, whole-farm traversal policy, batching, filters, UX, config, and messages. Transient handles must be owner-bound and invalidated/revalidated across save, room, day, and object-lifecycle boundaries; no raw `PlantBasin` or other decompiled type enters Abstractions.

The current AutoHarvest body must lose any official/publish-product identity. It may temporarily remain as a compatibility/API-demand fixture while the new contract and tests are built, then shrink to a small author example or be removed. `CropHarvestingQaMod` remains QA evidence and is not a functional product.

The required safety clause applies before implementation: **first inspect the current-build native owner method bodies and state holders for this API/domain; until the native owner or authoritative state holder is found, a Mod-layer patch must not be presented as a completed API rebuild.**

## Remaining Inputs, Not Product Decisions

The six first-round product choices are closed. The following are implementation inputs rather than new product-direction questions:

1. select Oil's economic target and test tolerance, then derive its native weight while preserving amber probability;
2. turn the completed local subscribed-DLL inventory into an executable 0.5.5 compatibility matrix and define the warning window;
3. create the formal-product roster which distinguishes functional Mods from QA, examples, compatibility fixtures, and unpublished prototypes;
4. schedule the pre-public naming/readability freeze after boundary extraction and before broad announcement.

## Validation Boundary

The focused review cross-checked current source/history, manifests, official JSON, release definitions, prior native-owner/manual-QA records, the existing version policy, local Workshop visible manifests/JSON, and managed public-reference metadata. No code build, game launch, runtime lock, install/uninstall, package staging, network Workshop lookup, third-party implementation decompilation, subscription mutation, or runtime smoke was performed.
