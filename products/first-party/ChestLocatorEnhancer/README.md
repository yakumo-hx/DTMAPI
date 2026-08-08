# DTMAPI Chest Locator Enhancer

Managed Advanced ProductNative implementation of the ChestLocatorEnhancer
behavior.

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
- This is not an arbitrary remote-item API. Calls which deliberately bypass
  shared containers, including the farming gun's
  `GetBackpackWithInsideBoxes(false)` path, remain outside the Hook.
- Product startup fails closed when the frozen GameBridge compatibility
  executor already owns the target.
