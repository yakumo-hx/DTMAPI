# ActionCompletion Hook Map

Last updated: 2026-07-22

## Scope

This focused map owns the OneActionComplete product resource/fuel/feeder Hook boundary, the frozen old-ABI compatibility route, and the retirement of the former Oil-specific ToolCollider branch. Oil item-spawn ownership belongs to official JSON plus the game's native resource-drop pipeline; it has no DTMAPI gameplay Hook or public extra-drop API.

## Native Owners

- `DolocTown.ToolCollider.HandleTools(UnityEngine.Collider2D other)` is the native tool-collision entry observed by the managed OneActionComplete product after the normal first hit.
- `DolocTown.DungeonResource._Fell(ResourceFellData data)` remains the native validated resource-damage/removal operation invoked for paid extra OneAction hits.
- `DolocTown.AgentStateInteract.OnExit()` is the post-interact boundary used after native fuel-generator and feeder consumption. ActionSpeed also observes this method, but no common product state or write/restore invariant has been proved.
- `GuaranteedManager.SpawnResourceDropItems(DungeonResource resource, bool isRender, string overrideSpawnLut)` owns native resource draw count, collection bonuses, guarantee accounting, and `IDropItemHost` world-drop creation. It is reference evidence for Oil, not a DTMAPI Hook target.
- `ISpawnLut.SpawnInternal<T>(...)`, the official Mod item-spawn extension handler, and `SpawnData.Unlimited` own the merged weighted pool. None is patched for Oil.

## Hook: Actions.OneActionComplete

- Current status: product-owned implementation runtime-verified by the bounded third-save routes `GAME-SMOKE/20260722-080842` and corrected continuation `141220`; `GAME-SMOKE/20260713-221610` remains the pre-migration behavior baseline only.
- Patch kind: Harmony Postfix on `ToolCollider.HandleTools(Collider2D)`.
- DTMAPI owner: Advanced product `Yuuka.DTMAPI.OneActionComplete`, canonical Harmony owner `dtmapi.mod.yuuka.dtmapi.oneactioncomplete`. `OneActionHookInstaller` resolves both product targets before applying either patch, then exact-owner unpatches on partial Entry failure or deactivation.
- Public surface: none for the new product. `IActionCompletionApi` is Deprecated/Frozen compatibility for already-built Strict consumers and is not consumed by this product.
- Behavior: product-native code constructs and validates `ResourceFellData`, charges native energy for each remaining hit, and invokes native `_Fell`. Wrong-tool, wrong-level, missing-method and insufficient-energy paths fail closed.
- Lifecycle: callback attachment and ProductNative state are product-owned; save/title clears only log dedupe. During owner deactivation, Core removes platform event roots, while product Dispose clears local flags/menu ownership, detaches callbacks and unpatches the exact native owner.
- Compatibility collision: the product refuses installation if frozen GameBridge compatibility already owns `ToolCollider.HandleTools`; compatibility `Configure` refuses the resource route before storing demand if the product owner is active. If compatibility demand arrives first and the product appears later, GameBridge reconciles at the physical install boundary and removes the complete ActionCompletion policy/parent/child demand before testing either target, while preserving unrelated shared demand. Both load orders therefore fail closed before duplicate installation or settlement.
- Failure behavior: callback exceptions are isolated and logged; native validation failures return without synthetic damage.

## Hook: Actions.OneActionFuelFeed

- Current status: product-owned implementation runtime-verified by the bounded third-save acceptance. Historical fuel/feeder evidence remains a pre-migration baseline.
- Patch kind: the second product-owned Harmony Postfix on `AgentStateInteract.OnExit()`.
- DTMAPI owner: `Yuuka.DTMAPI.OneActionComplete` under the same canonical owner and atomic two-target install as the resource Hook.
- Behavior: after the native first interaction, only reviewed `CostSelf` plus `AddFuel` / `AddFeeds` paths repeat until full, unsuitable input, inventory exhaustion or the bounded guard. Missing state/methods fail closed.
- Coexistence: ActionSpeed's independent ProductNative Postfix restores animator state. A shared method alone did not justify a common dispatcher. Frozen interaction-only compatibility can coexist because both fill-until-full loops become no-ops after the first owner reaches full state.
- Lifecycle: product callback and ProductNative state are removed on deactivation; Core owns platform event-root cleanup. There is no Oil item, probability, content-pack knowledge or resident QA counter.

## Retired Hook: Resources.OilCoalDrop

- Current status: retired and runtime-absent. `GAME-SMOKE/20260713-221443` verifies disabled Oil leaves only the native coal/amber table and publishes no former Oil Hook or feature status.
- Former boundary: Prefix/Postfix branches on `ToolCollider.HandleTools`, an Oil feature/service, pending coal-hit cache, independent random roll, fixed `crude_oil x1` direct-backpack placement, Hook/status publication, smoke forcing, and a OneAction-to-Oil callback.
- Retirement result: the ToolCollider Prefix is no longer installed, the Postfix has no Oil branch, and the Oil feature/service/cache/status path no longer exists.
- Replacement owner: official `mod_tbmoditemspawnextension.json` appends `crude_oil` to `coal_mine_drop`; the native resource owner produces world drops. No replacement DTMAPI Hook was added.
- Reactivation rule: do not restore an Oil-specific patch, public extra-drop API, pity/save cache, or direct backpack grant without a new native-owner Review and product decision.

## Validation

- Current focused gates pass: exact identity/config/policy registry, build-23762374 native method anchors, two-target full pre-resolution, exact-owner rollback, callback detach, bidirectional product/compatibility owner fail-closed, old Strict source zero-leftover, no new-product GameBridge/API consumption, Catalog-driven Author SDK validate/build/pack, Catalog zero-leftover and generic install/uninstall transaction tests.
- The current package identity and hashes are owned by the OneActionComplete Update rather than duplicated in this Hook map; the earlier `080842` package is predecessor evidence, while corrected `141220` and the later current-byte re-exercise own the accepted boundary.
- `GAME-SMOKE/20260722-080842`: migrated Advanced package loaded on the third save, installed exactly two product-owned patches and attached its callback; resource completion, wrong-tool non-match, fuel/feed completion and ActionSpeed coexistence passed. Foreground physical F11 opened the config page without `PostMessage`; title/save restoration, QA/source/deployment restoration, fatal-window and process-exit gates passed.
- `GAME-SMOKE/20260722-141220`: partial-energy behavior and configuration save/readback/reload passed; after title recovery, real Loader owner deactivation reduced one instance, two actual patches/two targets and one callback to zero instance/patch/target/callback/Core roots with clean restoration and exit.
- `GAME-SMOKE/20260713-221443`: third save; startup and HookProbe passed; Oil item/index entries were absent; the native coal/amber LUT was unchanged; former Oil Hook/feature status was absent; profile restoration and clean exit passed.
- `GAME-SMOKE/20260713-221610`: pre-migration third-save baseline; merged `990/10/25` LUT passed; a natural `crude_oil` world drop appeared on attempt 8; OneAction resource, wrong-tool, fuel/feed and vegetation paths passed; profile restoration and clean exit passed.
- Historical pre-retirement evidence remains audit material only and is not current-boundary proof.

## Related Records

- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- `docs/reviews/code/2026/20260721-0005-oneactioncomplete-second-product-admission-review.md`
- `docs/updates/2026/20260721-0005-oneactioncomplete-second-advanced-product.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
- `docs/api/public-api-matrix.md` row `IActionCompletionApi`
- `docs/debug/regressions/smoke-matrix.md`
