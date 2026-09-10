# 20260719-0001 DLL Mod Entry Model Audit

## Metadata

- Update ID: `20260719-0001`
- Date: 2026-07-19
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: review/compatibility/mod-loading/smapi/third-party/migration/api/content-host/roadmap/batch6
- Source: user requested a no-game static comparison of published DTMAPI 0.5.2, “姓王且名建林” DLL mods, the Terraria pack, local SMAPI mods, and current DTMAPI 0.5.5 authoring

## Scope

- established the exact published `0.5.2-alpha` loader baseline from Workshop item `3743016467` and build commit `8caf8403b45c`;
- audited three matching “姓王且名建林” subscriptions and separated two valid thin DTMAPI CodeMods from one non-DTMAPI `DolocSMAPI`/BepInEx package;
- compared DTMAPI entry semantics with the local SMAPI loader, installed code mods, content packs and dependency packaging model;
- bounded a source-available thin migration and source-unavailable/full-API migration for Terraria Workshop item `3759797170`;
- compared legacy Runtime compatibility with the current `0.5.5` strict Author SDK and 18 current first-party/test projects;
- produced a file/responsibility-level AutoFishing rehome decision, retained bundled GMCM ownership, and replaced the proposed universal native-reference ban with strict/advanced/external lanes;
- performed a code-level SMAPI helper/content/data/event/build comparison against current DTMAPI, classified missing framework APIs and excess product-shaped public/runtime code, and made no-DLL ContentPack management explicit;
- reclassified JSON/PNG/WAV custom livestock as an optional official content-host capability rather than species product code or mandatory Core;
- corrected the lightweight roadmap so single-product native implementation may live in an advanced managed Mod instead of accumulating in base GameBridge;
- consolidated the decision history, category error, Batch 1-5 disposition and G0-G7 PASS/BLOCKED criteria into a canonical Batch 6 boundary-correction prerequisite;
- recorded managed ContentPack, strict CodeMod, advanced CodeMod and external BepInEx classes without changing current Runtime policy or implementation.

This is a documentation and static source/package audit only. It does not launch the game, acquire the Runtime lock, edit third-party packages, change public APIs, change loader/SDK behavior, or modify the user's in-progress Batch 5 source work.

## Changed Files

- `docs/reviews/code/2026/20260719-0008-dll-mod-entry-and-migration-boundary-audit.md`: owns the evidence, comparison, Terra migration estimates and decision recommendation.
- `docs/reviews/api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md`: owns the SMAPI-backed AutoFishing keep/move/QA/compat/delete assertions and the replacement managed-native rule.
- `docs/reviews/api/2026/20260719-0011-smapi-api-gap-functional-surplus-and-content-host-review.md`: owns the broader framework API gap, functional surplus, no-DLL management and optional CustomAnimals content-host assertions.
- `docs/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md`: owns the causal ruling, four-way ownership model, Batch 6 G0-G7 admission gates, preliminary product classification and Ready definition.
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`: corrects the universal GameBridge-native rule, adds advanced managed CodeMods/optional content hosts, links the Batch 6 admission authority and prevents the old AutoFishing structure from remaining a product template.
- `docs/updates/2026/20260719-0001-dll-mod-entry-model-audit.md`: owns this documentation lifecycle.
- `docs/updates/INDEX-2026-07.md`: routes this Update from the monthly ledger.

## Validation

- Read the published Workshop `0.5.2-alpha` package and matched it to recorded build commit `8caf8403b45c`.
- Read manifests, file inventories, hashes, managed references and type/member metadata for the three Wang-author subscriptions and Terraria subscription.
- Read local SMAPI source and installed sample package manifests under `E:\Python_project\SMAPIlearning`.
- Rechecked the author's tagged `Yet Another Fishing Mod 1.2.0` source at commit `e23b8d72ea1d5551cf770733801d712dec28e83f` and the local SMAPI ModBuildConfig targets that provide game references plus opt-in Harmony.
- Re-fetched that exact public tag without retaining a third-party checkout, counted all seven feature-specific source files and the two `Common.projitems` files, and separated direct SMAPI references from Harmony, native game and external GMCM calls.
- Rechecked the local SMAPI tree for AutoFishing/product-specific implementation and read its 55-line `FishingRodFacade`; the facade is an old-Mod cross-version rewrite shim, not a fishing feature service.
- Read current DTMAPI `0.5.5` version authority, Runtime entry selection, Author SDK policy and current first-party/test project sources.
- Counted current five Runtime projects, Abstractions public declarations and GameBridge feature/compatibility/diagnostic areas, excluding `bin`/`obj`.
- Read local SMAPI `Mod`, helper interfaces and implementations, data/content/content-pack contracts, event families, semantic version contract and native/Harmony build references.
- Read the current non-code loading/content-capability paths and local Hatch/Mole/Drecko/Oilfloater/Shell Crab manifests/assets; verified the CustomAnimals bridge contains no first-party species identifiers.
- Preserved the third-party reference-only boundary: no method-body decompilation, copied implementation or modified/repackaged DLL.
- No runtime test was run by explicit user request; Runtime Validation is `not-required` for this static boundary audit.
- `tools/scripts/check-doc-governance.ps1`: passed (`Document governance: OK`, 5,257 checks).
- Explicit trailing-whitespace scan across Reviews 0008/0010/0011/0012, the corrected roadmap, the Update and July ledger: passed.
- Relative Markdown-link existence scan across Review 0012, the corrected roadmap and this Update: passed.
- `git diff --check` on the tracked roadmap and July ledger changes: passed; Git emitted only the existing LF-to-CRLF working-copy warning.

## Evidence ownership

- Review 0008 owns compatibility facts and estimates.
- Review 0012 owns the Batch 6 admission decision; it does not claim that the current Runtime already implements the new lanes.
- No API matrix row changed because no API contract was implemented or stabilized.
- No Debug or smoke record changed because no runtime issue was investigated and no game run occurred.
- The strict/advanced/content/external direction is now a recorded project decision, but current Loader/SDK behavior remains unchanged until a dedicated implementation Update passes the Batch 6 gates.

## Same-day clarification

User feedback correctly rejected the initial aggregate wording that AutoFishing's small product assembly meant its complexity had been moved into GameBridge. Review 0008 now records both dimensions: the product ownership split is real, but Git history shows the fishing-domain source footprint grew from 3,158 pre-cutover lines to 5,902 lines on the current first-party path, while the retained 3,003-line legacy path brings the current dirty-worktree maintenance total to 8,905. The former 8,393 GameBridge aggregate must not be treated as AutoFishing product code, but the new split must also not be presented as lightweight. This correction changes no source, API status or Runtime behavior.

A second clarification compared the exact local `Yet Another Fishing Mod 1.2.0` binary with the author's tagged MPL-2.0 source and local SMAPI source. The 1,474-line Stardew mod owns its product behavior, five direct Harmony patches, native game access and 450-line GMCM registration while adding zero feature-specific source to SMAPI. Review 0008 now records that the user's intended split criterion is Runtime weight: under that criterion, current DTMAPI AutoFishing is directionally wrong despite a coherent internal ownership seam, and a managed native/advanced code-mod lane is required before moving fishing-native code out of GameBridge. This remains analysis only; current SDK, Runtime, API and compatibility policy are unchanged.

A third clarification traced the rule history and future route. The earliest repository baseline on 2026-06-01 paired GameBridge ownership of fragile implementation with a planned advanced `helper.Patching`/`helper.Reflection` escape hatch; that hatch was never implemented. The API boundary was strengthened on July 5, encoded as internal-only AutoFishing primitives and source gates during July 10-12, and finally enforced by the Author SDK's text-based `SDK160` rule on July 15. The exact 8,832-line cutover is now explained as a 493-line product plus a new 5,342-line protected native path plus the retained 2,997-line compatibility executor, a net increase of 5,674 lines over the 3,158-line parent. Existing lightweight planning can optionalize/remove the legacy executor and extract QA, but does not plan to move the first-party fishing-native layer out of GameBridge. Review 0008 therefore proposes a revised Runtime-weight definition and an evidence-first advanced-lane/AutoFishing route without changing source or current policy.

A fourth clarification traced the recoverable user/SMAPI instruction chain and audited Batch 6 scale. The July 5 SMAPI research correctly described a boring framework, Mod-owned product logic and DTMAPI's existing tendency to act as the feature mod, but it then prohibited ordinary Mods from owning native implementation. The July 12 route copied AutoFishing as the template and measured package/inactive/API lightness without a marginal base-source budget. Current GameBridge is 41,168 lines, including 25,703 Feature lines, while the eleven Catalog PublishedProduct source roots total 3,046 lines. Twelve structurally paired product/native domains total 3,342 Mod lines versus 20,534 GameBridge lines. Review 0008 now records that Batch 6 has linear base-growth risk under the unchanged rule, separates legitimate platform work from single-consumer ProductNative work, and recommends that Batch 6 productization wait for an explicit advanced managed lane and per-product marginal-cost classification. This is a recorded blocker/recommendation only; the canonical roadmap and source remain unchanged.

A fifth clarification re-audited Batch 1-5 under the corrected base-weight definition. Batch 1 removed Oil product ownership from GameBridge but temporarily replaced much of the source reduction with embedded Smoke; Batch 2 added generic release/version authority plus bounded product-ABI shells; Batch 3 added approximately 3,971 net game-loaded Runtime lines, exposing author-session/reload code and the strict SDK policy as the first major base-weight warning; final Batch 4 moved approximately 8,171 net lines out of player Runtime into an 8,815-line optional QA host and is the strongest successful physical reduction; current in-progress Batch 5 has added approximately 7,187 net Core+GameBridge lines including untracked source while product Mods are net `-11`. Review 0008 retains the useful generic event/demand/generation/lifecycle work but records that Batch 5 currently optimizes and instruments the existing product-host boundary instead of correcting it, and requires ProductNative/ContentOwner reclassification before its per-product routes become final architecture. No Runtime source or canonical Batch plan was changed by this audit.

A sixth clarification opened focused API Review 0010 and made the AutoFishing boundary assertive at file/responsibility level. The universal “all managed Mods are Abstractions-only” rule is the key restriction to cancel; the API-only restriction remains the default strict lane, while an advanced DTMAPI-managed `DtmMod` lane may own Harmony/game/Unity access under unique-owner, package, Doctor/Manager and restart-required constraints. The review keeps DTMAPI's bundled GMCM-class implementation, assigns the 5,403-line native fishing directory to AutoFishing product ownership, marks first-party primitives/friend/provider/demand/cleanup code for deletion, moves product QA out of mandatory GameBridge, and treats the 3,007-line old executor/facades as transitional compatibility subject to the existing preview gate. No source rule or compatibility behavior was changed in this docs-only step.

A seventh clarification opened API Review 0011 and expanded the assertion beyond AutoFishing. The current five player Runtime projects total 77,071 physical C# lines; Abstractions exposes 267 public types while lacking ordinary content-pack ownership, data/save, command, reflection/advanced-patching and general content services. At least 16,249 lines in named GameBridge feature directories are plainly product-shaped, while the 3,716-line CustomAnimals bridge is reusable but domain-specific. The local JSON/PNG/WAV livestock packages prove that a zero-DLL ContentPack can still receive full DTMAPI identity/version/dependency/enablement/Manager handling. The review therefore assigns generic ContentPack management to Core, assigns animal schema/native adaptation to an optional `DTMAPI.CustomAnimals` content host, and keeps species/economy/assets in AnimalPack. The canonical lightweight roadmap was corrected accordingly: the strict lane remains available, single-product native code may live in an advanced managed Mod, and only proven shared native capabilities belong in base GameBridge. Runtime/SDK behavior is unchanged until a dedicated implementation Update.

An eighth clarification consolidates the entire discussion as Review 0012, the canonical Batch 6 prerequisite. It rules that no single decision explicitly moved every functional Mod into GameBridge: the advanced escape hatch was omitted, the stable-API native isolation rule was generalized to all managed Mods around July 5, the July 12 roadmap made AutoFishing the execution template, and July 15 `SDK160` enforced the mistake. The review preserves generic Batch 1-5 platform work, distinguishes visibility from physical ownership, defines Platform/SharedNative/ProductNative/ContentOwner, and blocks bulk product migration on G0-G7. Batch 6 Phase 0 may begin with authority alignment, inventory, Advanced lane work and compatibility scans; other product migrations remain blocked until the Advanced channel and AutoFishing reverse-migration pilot pass. The roadmap now links this admission authority and no longer treats the current AutoFishing/Zoom GameBridge structure as a reusable template.

A ninth clarification returns to the original SMAPI comparison and quantifies the actual integration surface. At the pinned Yet Another Fishing Mod 1.2.0 tag, 54 of 1,474 feature-specific C# lines directly name or invoke SMAPI types/members (3.7%), covering ten generic service families. The Mod itself owns 804 lines of fishing/native/Harmony code and a 450-line GMCM form with 59 calls to the separate GMCM API; its project additionally compiles 195 lines of author-shared GMCM-interface/notifier code. SMAPI provides entry, events, config, translation, logging, context, reflection, per-screen state, input and Mod-registry services plus build references, but no AutoFishing engine. Review 0010 and the Batch 6 prerequisite now record this evidence and conclude that DTMAPI needs a managed Advanced lane and generic host services, not a single-consumer `IFishingPrimitivesApi` as the admission price for AutoFishing.

## Rollback

Remove Reviews 0008/0010/0011/0012, revert the 2026-07-19 corrections in the lightweight roadmap, remove this Update and its single July-ledger row. No Runtime, API, package or third-party rollback is needed.

## Follow-Up

- Complete Review 0012 G0 first: align PROJECT, Agent rules, Author SDK, author docs, Loader terminology and Manager wording around Strict/Advanced/ContentPack/External identities.
- Use API Review 0010 as the native-ownership boundary and open a dedicated loader/SDK/Doctor implementation Update for the Advanced lane before migrating AutoFishing.
- Use API Review 0011 to implement semantic version authority, `ContentPackFor`/owned-pack helpers and the optional `DTMAPI.CustomAnimals` host before migrating more functional products into base GameBridge APIs.
- Obtain Terraria source and permissions before attempting a preserved-source migration; otherwise retain only the clean-room responsibility map.
