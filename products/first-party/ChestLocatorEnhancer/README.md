# DTMAPI Chest Locator Enhancer

Managed Advanced ProductNative implementation of the ChestLocatorEnhancer
behavior.

- The product owns one exact Harmony Postfix on
  `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`.
- The game remains the sole owner of inventory contents and the native
  CountItem/CostItem transactions.
- Shared `Case` inventories and `ItemBox` inventories inside shared
  `StorageShelf` equipment are appended without replacing or duplicating native
  `LinearInventory` instances.
- Product startup fails closed when the frozen GameBridge compatibility
  executor already owns the target.
