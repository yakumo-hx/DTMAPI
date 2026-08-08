# FishBreedingAssistant Roe Title Decoration

Status: `current shared-adapter product package runtime-verified`

## Native Boundary

- Game build: `23762374_public_C416D4`.
- `DolocTown.Item::get_title()`: one ProductNative Postfix owned by `dtmapi.mod.yuuka.dtmapi.fishbreedingassistant`.
- For `ItemFishRoe`, the product reads `fishName`, asks owner-bound `IItemDisplayNameApi` for the localized parent item title, and appends one localized marker with a duplicate guard.
- The shared adapter owns only `DolocAPI.QueryItemProto(itemId).Title`; a successful lookup arms the shared `DolocAPI.SetEnvCamera` lifecycle route and is cached only after its independent physical Hook owner is ready. The adapter installs no Fish Hook and contains no Fish formatting.
- The empty generated fish-breeding lookup is not executed. Incubation/growth data is future ContentOwner/G7 work, not a runtime fallback.

## Owner And Lifecycle

- Hook installation pre-resolves the exact title getter, rejects a frozen compatibility owner, patches atomically and rolls back/exact-owner-unpatches on failure.
- Product cache/callback state clears at save/title/deactivation. Final Loader deactivation must reduce one instance, one actual patch/target, one callback and Core roots to zero.
- Native lookup failure preserves vanilla title text without adding a broken marker.

## Compatibility

The old `IItemTooltipApi` title/description/detail executor is frozen under `GameBridge/Compatibility/FishRoeTooltip`. It is demand-inactive and both load orders fail closed before duplicate physical installation. The new product does not consume the old API.

## Evidence And Relations

- Historical GameBridge native-fallback baseline: `GAME-SMOKE/20260613-201326`.
- Migrated product direct-fallback acceptance: `GAME-SMOKE/20260722-145154`, exactly one patch, `鱼卵 (鱼)`, final instance/owner/callback/Core-root zero, restoration and clean exit.
- Pre-correction shared-adapter baseline: `GAME-SMOKE/20260722-161022`, `鱼卵 (鱼)`, exactly one actual patch/target and one callback before real Loader deactivation, then zero instance/patch/callback/Core roots; restoration and exit passed. `153320`/`153845` remain Steam cloud-conflict infrastructure evidence only.
- Current corrected product acceptance: `GAME-SMOKE/20260722-180502` repeats the localized title and exact owner cleanup with the corrected package. Focused Units later close the shared adapter's EnvironmentReset fanout, callback gate and Hook-ready fail-closed caching without changing this Fish Hook evidence.
- Admission Review: `docs/reviews/code/2026/20260722-0006-fishbreedingassistant-fourth-product-admission-review.md`.
- Shared-boundary Review: `docs/reviews/code/2026/20260722-0008-fish-animal-shared-boundary-review.md`.
- Update: `docs/updates/2026/20260722-0002-fishbreedingassistant-fourth-advanced-product.md`.
