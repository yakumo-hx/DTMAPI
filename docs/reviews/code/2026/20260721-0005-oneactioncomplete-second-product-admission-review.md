# 20260721-0005 OneActionComplete Second Product Admission Review

Date: `2026-07-21`

Status: `accepted — exactly one second real Advanced product`

Scope: Decide the one permitted Checkpoint E product after AutoFishing D.5, before any OneActionComplete implementation work. This Review does not open general Advanced authoring, admit a third product, implement G7, publish 0.5.5, or claim runtime verification for the migrated product.

## Inputs

- `PROJECT.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md`
- `docs/hook-map/focused/ActionCompletion.md`
- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- current `testmods/OneActionCompleteMod` and `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion`
- build `23762374` decompilation for `ToolCollider.HandleTools`, `ResourceFellData`, `AgentStateInteract.OnExit`, `PowerGeneratorFuel` and `Feeder`

## Decision

`Yuuka.DTMAPI.OneActionComplete` is accepted as the only second real managed Advanced product pilot.

The decision is intentionally singular. The accepted identity is bound to:

- UniqueID `Yuuka.DTMAPI.OneActionComplete`;
- Workshop item `3742763540`;
- current product/config version `1.1.2-dtmapi`;
- game build `23762374` and an SDK-tracked product-specific Advanced reference policy;
- canonical Harmony owner `dtmapi.mod.yuuka.dtmapi.oneactioncomplete`;
- one SDK-produced package with no copied game, Harmony, Unity or Runtime assemblies.

AutoFishing remains the first real pilot. No other real product, general Advanced template, hand-authored Advanced manifest/receipt/package, Content Host, or 0.5.5 publication is admitted by this decision.

## Why This Candidate Passes

1. It is a real, independent PublicWorkshop product and the sole consumer of the product rule.
2. Its reviewed production boundary is small: approximately 145 product-entry lines plus 612 GameBridge lines before migration, rather than another AutoFishing-sized state machine.
3. It has no product save sidecar, no long-running update loop, no animation override, and no product-owned UI host. Its persistent player contract is the existing config file and Workshop identity.
4. Existing third-save evidence already covers resource completion, wrong-tool rejection, native energy charging, fuel/feed native consumption, vegetation exclusion, title/exit restoration, and Oil coexistence. That evidence is migration baseline material, not proof of the new Advanced package.
5. AutoFishing D.5 passed its focused package/unit/static gates and one bounded fifth-save start/stop/title/recovery regression without reopening the historical full Release, L0-L5 or long-run matrices.

## Native Responsibility And Physical Owner

### Resource completion

`ToolCollider.HandleTools(Collider2D)` performs the native normal hit, tool-type check, resource-cache/energy handling and `IFellable.OnFell` call. `ResourceFellData(resource, tool, hitPoint)` owns the exact tool-type, tool-level, damage and drop-override validation. `DungeonResource._Fell(ResourceFellData)` owns final damage/removal and native drop behavior.

The product policy “pay the remaining native hits after one valid hit” has one consumer. Its policy, remaining-hit calculation, extra native energy charging, resource classification and `_Fell` invocation are ProductNative. `ToolCollider.HandleTools` therefore moves to the OneActionComplete product's two-hook atomic inventory; the mandatory Runtime must not keep an always-on product executor for the new package.

### Fuel and feeder completion

`PowerGeneratorFuel.OnInteract` and `Feeder.OnInteract` schedule their native consume/add callback through `DolocAPI.agent._Interact`. `AgentStateInteract.OnExit` invokes that callback and clears it. Only after that native first item has been consumed and added may OneActionComplete repeat `CostSelf` plus `AddFuel` or `AddFeeds` until full or inventory exhaustion.

The fill loop, selected-item checks, capacity guards and execution policy are ProductNative. The product owns its own `AgentStateInteract.OnExit` Postfix and must fail closed when types, methods, selected state or native calls are unavailable.

### Shared-hook decision

ActionSpeed also observes `AgentStateInteract.OnExit`, but the two consumers do not share a state machine, cache, restore token, ordering invariant or native write. Harmony can host their independent owner-scoped Postfixes; ActionSpeed restores animator state while OneActionComplete performs item accounting. A common patch point alone is insufficient to introduce a new public callback API or retain the OneAction executor in GameBridge.

Therefore no new SharedNative capability is admitted in this Review. The existing ActionSpeed lifecycle hook remains in GameBridge for ActionSpeed. OneActionComplete uses its canonical product Harmony owner. If later evidence proves an actual cross-owner ordering/conflict invariant, only that minimum hook dispatch may be reconsidered.

## Compatibility Boundary

DTMAPI 0.5.5 still promises no public ABI removals. `IActionCompletionApi`, `ActionCompletionOptions` and the old GameBridge-backed executor therefore remain frozen compatibility for already-built Strict consumers during this release window. They are not the implementation route for the new product and do not justify new API growth.

The migration must:

- move the old executor under an explicit Compatibility boundary or otherwise label it unambiguously;
- keep it demand-inactive with zero gameplay Hook installation when no old API consumer configures it;
- reject/avoid concurrent product and compatibility ownership of `ToolCollider.HandleTools` rather than double-settling;
- record an earliest breaking-release removal review instead of deleting the ABI in this change.

## Admission Gates

Implementation may proceed only with all of the following:

- SDK registry admits exactly the synthetic fixture, AutoFishing and OneActionComplete; all other UniqueIDs remain rejected;
- the manifest, author intent, receipt, game-build binding, entry DLL and Harmony owner are SDK-generated and exact;
- the package preserves current identity/version/config keys/defaults and has only the required ModConfigMenu dependency;
- the two native Hook targets are fully resolved before patching, installed atomically, and exact-owner rollback runs on partial Entry failure or owner deactivation;
- the product has no GameBridge dependency and no public product-shaped API is added;
- mandatory Runtime has a live zero-leftover gate for the new product executor, with only the named frozen compatibility exception;
- focused SDK/package, source/unit, catalog/Doctor/Manager and compatibility checks pass;
- no additional game run, full Release suite, L0-L5 ladder or long test is required by this admission slice.

## Risks And Rollback

- If the tracked game build or native method identities drift, Author SDK resolution or product Entry must fail closed before partial product ownership survives.
- If the new product and frozen compatibility executor can both reach the same resource target, the product must refuse the conflicting resource Hook rather than permit duplicate completion.
- Rollback removes the OneActionComplete policy/project/package admission and restores the prior Strict product route; it must not change AutoFishing, G7, or release state.

## Result

Admission: **PASS**, for `Yuuka.DTMAPI.OneActionComplete` only.

Implementation and focused validation are owned by `docs/updates/2026/20260721-0005-oneactioncomplete-second-advanced-product.md`.
