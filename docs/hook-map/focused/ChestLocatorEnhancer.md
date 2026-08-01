# ChestLocatorEnhancer Inventory Widening

Status: `verified/closed ProductNative package and frozen compatibility boundary`

## Native Boundary

- Game build: `23762374_public_C416D4`.
- Target:
  `DolocTown.GameData.ArchiveDataHandle::GetAvailableInventories(UnityEngine.Vector2Int,UnityEngine.Vector2Int,System.Boolean)`.
- Current product patch: one Postfix owned by
  `dtmapi.mod.dtmapi.chestlocatorenhancermod`.
- ProductNative widens only the returned inventory set according to its
  configuration and deduplicates native `LinearInventory` instances.
- `Case`, `StorageShelf`, `ItemBox`, native room/building graphs, inventory
  identity, `CountItem`/`CostItem`, UI and persistence remain game-owned.

## Owner And Lifecycle

- Entry pre-resolves the exact three-argument target and installs the one
  Postfix atomically.
- Product-first and frozen-compatibility-first requests reconcile before
  physical installation. A physically installed conflicting owner or residual
  exact-owner patch fails closed.
- Disable, failed Entry and Loader deactivation attempt product-state cleanup
  and exact-owner unpatch independently. SaveLoaded and ReturnedToTitle reset
  only product observation state; an enabled product retains its
  process-lifetime Hook for re-entry.
- Re-enable is supported after a successful cold/active disable. A failed
  unpatch retains a truthful residual count and does not report clean state.

## Compatibility

`IChestLocatorEnhancerApi` and its DTOs remain
Experimental/Deprecated/Frozen for the exact retained Strict consumer. The
mandatory GameBridge provider is a thin on-demand proxy; the heavy executor
lives in the existing single dormant-shipped Compatibility Host. The new
product does not consume this API. The compatibility path is not a second
real product consumer and does not justify SharedNative extraction.

## QA And Evidence

- Focused product/compatibility Units cover both load orders, atomic rollback,
  disabled/re-enabled state, stale owner rejection and exact cleanup.
- Catalog, tracked policy, Author SDK, retained ABI, Doctor, QA and focused
  release-contract gates pass.
- `GAME-SMOKE/20260723-203127` loads the current Advanced product in save slot
  3, observes exactly one product patch/target, and proves native inventory
  transactions with `case_locator` counts
  `baseline=0 -> afterPlace=3 -> afterCost=1`.
- Title return passes. Real Loader deactivation reports
  `actual1+callback1 -> instance0+actual0+callback0+roots0`; player save,
  official profile, Author source, QA staging and process exit restore cleanly.
- `GAME-SMOKE/20260723-210424` completes two real third-save cycles with
  `requests=2; nativeEnter=2; nativeReturn=2; saveLoaded=2`. The product
  installs its exact owner once at count `1`, receives both SaveLoaded resets
  and all title resets without a duplicate install or handler failure, and
  exits with exact save/profile/source/QA restoration.
- `GAME-SMOKE/20260723-224200` binds the corrected committed candidate:
  Runtime `BuildCommit=7ace68260cb1` and the 34,304-byte product entry SHA-256
  `29C2FD2EDA5618C6814C851913F64E77245CE9A31703FF6FD70C1B802D0DA9C1`.
  Startup, HookProbe, the third-save native Chest transaction, title recovery
  and exact Loader owner cleanup pass; the final owner transition is
  `actual1+callback1 -> instance0+actual0+callback0+roots0`, all scoped state
  restores, and no game process remains.
- `GAME-SMOKE/20260723-202456` is non-acceptance: the optional QA owner
  allowlist rejected the product before Entry. It contains no product behavior
  or Hook evidence.
- `GAME-SMOKE/20260723-205205` and `20260723-210101` are non-acceptance QA
  orchestration conflicts that returned to title before any save-load request;
  both runners restored their scoped state and neither is positive evidence.

## Relations

- Admission Review:
  `docs/reviews/code/2026/20260723-0005-seventh-product-chestlocator-admission-review.md`.
- Update:
  `docs/updates/2026/20260723-0007-chestlocator-seventh-advanced-product.md`.
- Independent final Review:
  `docs/reviews/code/2026/20260723-0006-chestlocator-seventh-product-final-review.md`.
- Commit-range correction Review:
  `docs/reviews/code/2026/20260723-0007-seven-product-commit-range-audit.md`.

## Rollback

Revert the product/Host switch as one unit while preserving the frozen public
ABI and retained-consumer evidence. Do not leave both the former mandatory
executor and ProductNative patch installable.
