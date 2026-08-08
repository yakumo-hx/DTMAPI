# StrongPlantingGun ProductNative Hook Map

## Current Boundary

- Product: `DTMAPI.StrongPlantingGunMod`
- Harmony owner: `dtmapi.mod.dtmapi.strongplantinggunmod`
- Physical owner: ProductNative Advanced assembly
  `DTMAPI.StrongPlantingGun.dll`
- Current status: `verified/closed`; current package, focused rollback fixture,
  protected third-save runtime route and independent closeout passed.
- Public compatibility: `IStrongPlantingGunApi` and its three DTOs remain
  Experimental / Deprecated / Frozen warning shells. There is no retained
  binary and no Compatibility Host executor.

## Atomic Target Set

All five patches install or roll back as one exact-owner set:

1. `DolocTown.ItemFarmingGun..ctor(DolocTown.Config.Item.ItemInfo, int)`
   Postfix;
2. JSON `DolocTown.ItemFarmingGun..ctor(string, int,
   DolocTown.LinearInventory)` Postfix;
3. `DolocTown.ItemFarmingGun.OnUseAsTool()` Prefix;
4. `DolocTown.FarmingGunUiState.HandlePlaceToOtherSide(int)` Prefix;
5. `DolocTown.FarmingGunUiState.HandleSwapOneItem(int)` Prefix.

Any legacy owner, duplicate owner, partial owner set, unresolved target or
partial install fails closed. Install rollback and disable/title/Loader-owner
deactivation re-observe the real Harmony owner before publishing lifecycle
state.

## Native And Product Ownership

The product owns fixed-three seed/film/fertilizer policy, cached reflection
metadata, the function-capacity snapshot, configuration and exact Hook
lifecycle. It never handles water. Existing JSON inventories wider than three
are retained without truncation; the shared native
`ItemFunctionFarmingGun.Capacity` is set to three while enabled and restored
exactly on cleanup. Because native JSON construction occurs while the product
is suspended at title, the product scans the loaded backpack once at
`SaveLoaded` and prepares each already-deserialized farming gun. This is a
ProductNative lifecycle repair, not a per-frame scan or SharedNative API.

The game continues to own `ItemFarmingGun.inventory` serialization,
`LinearInventory`, backpack/container UI, equipment and PlantBasin state.
Tool and UI prefixes snapshot the exact affected native inventory slots,
counts and persisted slot locks before mutation. A failed interaction, native
emit exception or count non-conservation restores both inventory data sets
before resynchronizing every native receiver; one receiver failure does not
prevent the remaining receivers from observing the rollback. Once mutation
ownership begins the prefix never falls through to the original method.

## Lifecycle And Evidence

Configuration disable, return to title and real Loader owner deactivation
must remove all five exact-owner patches, detach callbacks, restore every
captured function capacity and clear cached reflection metadata. The terminal
summary is:

`listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0`

Focused source, production-runtime Unit, SDK/package, Catalog and Doctor
checks are owned by
[Update 20260724-0001](../../updates/2026/20260724-0001-strong-planting-gun-ninth-advanced-product.md).
`GAME-SMOKE/20260724-101757` is the current-DLL protected third-save evidence
and proves `3/3/3`, five real owners, Loader deactivation to exact zero, title
recovery, protected save/config restoration and clean exit. Historical
GameBridge smoke remains compatibility evidence only.
