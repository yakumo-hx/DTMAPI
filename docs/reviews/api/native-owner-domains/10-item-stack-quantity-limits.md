# 10 - Item Stack Quantity Limits

Status: Found for native stack cap; Partial for stable mutation API
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Item quantity limits, item stacking, stack cap changes, and quantity operations.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Not relevant | Beauty docs | No quantity behavior |
| Base content mod | Yes for item stack/overlay definitions | `046_*` new item docs; `015_*` hats; extracted `item_tbitem.json` | Data definition only |
| Advanced content mod | Partial for drops/caps | `026_*`, `027_*` | Drop cap is not inventory transaction proof |
| Runtime behavior mutation | Not public | No official runtime stack API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Stack cap definition | `ItemInfo.Overlay`; `Item.overlay`; `Item.noOverlay` | `Config/Item/ItemInfo.cs`; `Item.cs`; `content-configs/item_tbitem.json` | Item config and item instance | Defines max stack/overlay behavior | Low/medium for read-only; high for runtime mutation | `ItemDefinition.StackLimit` | Found |
| Stack merge/split | `Item.TestCombine(int)`, `Item.TryCombine(int)`, `Item.CostSelf(...)`; `ItemFactory.GenerateItem(ItemInfo,int)`; `DolocAPI.GenerateItem` | `Item.cs`; `ItemFactory.cs`; `DolocAPI.cs` | Item instance and factory | Count clamp, combine, consume, generate items | Medium raw item risk | Opaque `ItemStack` DTO/result | Found |
| Backpack inventory state | `FarmArchiveData.inventory`; `ArchiveDataHandle.InventorySystem`; `InventorySystem.inventory`, `buffer`; `LinearInventory`; `SingleInventory` | `FarmArchiveData.cs`; `ArchiveDataHandle.cs`; `InventorySystem.cs`; `LinearInventory.cs`; `SingleInventory.cs` | Save-backed inventory plus cursor/buffer | Backpack and buffer item state | High mutation/save risk | `IInventorySnapshot`, `IInventoryTransaction` | Partial |
| Placement/cost/count | `LinearInventory.PlaceItem`, `CanPlaceIn`, `MaxItemPlaceCount`, `TryCost`, `Count`; `InventorySystem.PlaceItem`, `CanPlaceItem`, `CostItem`, `GetCount`; `DolocAPI.TryPlaceInBackpack`, `PlaceItemAllForce`, `PlaceInBackpackOrGenerateDropItem`, `CostItem`, `CostSelectedItem`; multi-inventory cost helpers | inventory classes; `Items_Inventory.md`; `DolocAPI.cs` | Inventory system and linear slots | Merges stacks, checks capacity, consumes quantity; wrappers may still partially mutate before overflow/incomplete result | High transaction risk | `TryAddItem`, `TryRemoveItem`, explicit overflow/leftover result | Partial |

## API Translation Notes

- Public stack APIs should expose item ids, counts, stack limits, and overflow/leftover results.
- Raw `Item`, `LinearInventory`, and `InventorySystem` objects should remain GameBridge-internal.
- Any runtime stack limit override must define dirty item/equipment uniqueness, split behavior, and save/load compatibility.
- Runtime stack-limit override is not the same as content-defined `ItemInfo.Overlay`.
- `TryPlaceInBackpack`/email overflow may partially place items or mail leftovers before reporting incomplete/false. Inventory `buffer`/cursor state is internal only, not public transaction state.
- Round 3 confidence: `ItemInfo.Overlay` stack source 96; native stack operations 92; native add/remove wrappers exist but need transaction policy 90; partial-place/mail overflow caveat 84; buffer/cursor internal only 90; runtime stack-limit override and over-cap migration blocked 96.

## Blockers And Follow-Up

- Box/shared-inventory behavior needs separate review.
- Cursor/buffer item side effects must be understood before stable inventory mutation.
- Over-cap quantities need explicit split/reject/overflow policy.
- No native over-cap split/clamp migration owner was found; validation may remove invalid items.

## Evidence Checked

Maps: `Items_Inventory.md`, `Assets_Content.md`.
Classes/symbols: `ItemInfo`, `Item`, `ItemFactory`, `FarmArchiveData`, `ArchiveDataHandle`, `InventorySystem`, `LinearInventory`, `SingleInventory`.
