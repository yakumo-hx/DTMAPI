# DTMAPI Chest Locator Enhancer

Managed Advanced ProductNative implementation of the ChestLocatorEnhancer
behavior. Official enablement activates the complete extension; this product has no configuration or configuration-menu page. Old configuration files are left untouched and are no longer read.

The current project targets DTMAPI **0.7.5**, `netstandard2.0`, NativeContractVersion 2
and DependencyContractVersion 1. Its Harmony owner is
`DTMAPI.ChestLocatorEnhancerMod.Native`. Set `nativeReferences.gameRoot` in
`dtmapi.author.json` to a complete local game installation before using the normal
SDK validate/restore/build/pack commands. The SDK generates host references and
package bindings; only the product DLL belongs in the package.

The explicit required-member list covers the inventory/UI hooks, reflected
fields/accessors, both `GetEquipments<T>` uses and the closed quantity UI base.
Optional historical room probes remain optional. The SDK checks the host's exact
required signatures before loading the product. This project no longer uses the 23762374
receipt policy; historical packages and the old API executor are outside current
compatibility maintenance.

- The product owns one exact Harmony Postfix on
  `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`.
- The game remains the sole owner of inventory contents and the native
  CountItem/CostItem transactions.
- The Postfix intentionally applies to every native
  `GetAvailableInventories` caller. That includes equipment crafting, the
  Exchange Store, building construction, and native count/max/actual-cost
  flows; it is not narrowed to equipment-origin calls.
- Shared `Case` inventories and `ItemBox` inventories inside shared
  `StorageShelf` equipment are appended without replacing or duplicating native
  `LinearInventory` instances.
- Discovery uses the native `EquipmentManager.GetEquipments<Case>()` and
  `GetEquipments<StorageShelf>()` indexes, including subclasses, across the
  current/root room, farm and reachable building rooms. A per-query reference
  set prevents room cycles and duplicate additions. The existing native result
  keeps its order; each visited room appends native-index Cases, then Shelf boxes.
- Every query reads current sharing and current Shelf contents. Shelf boxes
  require both `useBox` and the game's `autoUseBox`; shared Cases are independent
  of those settings. No inventory snapshot, permanent container cache or
  per-frame world scan is used. Diagnostics retain only counts.
- Ordinary native Workbench, EquipmentWorkbench and Building crafting UIs
  refresh their current members while open, at most once every 250 ms while idle,
  and immediately before an action or quantity confirmation. Each keeps its
  original native equipment-area or backpack query. No refresh runs after exit.
- A reduced quantity limit clamps the displayed selection and rejects a stale
  excessive confirmation. Losing all current materials cancels the quantity
  selector through the native path and returns to the refreshed parent page.
  The game still performs the actual consumption, output and original callback.
- Unknown inventory arrays, Exchange Store and Recipe callers with custom
  crafting/max/filter/check callbacks or dish buffers keep their native scope and
  behavior. No callback is replaced with a global inventory source. UI sessions
  end on native exit, title/save boundaries and owner cleanup.
- This is not an arbitrary remote-item API. Calls which deliberately bypass
  shared containers, including the farming gun's
  `GetBackpackWithInsideBoxes(false)` path, remain outside the Hook.
- Product startup fails closed when the frozen GameBridge compatibility
  executor already owns the target.
