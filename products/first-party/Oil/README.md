# DTMAPI Oil

Developer-only prototype packaged as a pure official JSON `ContentPack`. It has no code assembly and no DTMAPI, GameBridge, or public-API dependency.

- Adds `crude_oil` through official `Content/**/item_tbitem.json`.
- Uses `electric_energy=1500`, intentionally higher than the base table's current highest fuel value (`pumpkin=1200`).
- Uses the base coal icon until a scoped DTMAPI asset pipeline is added.
- Extends the native `coal_mine_drop` lookup table append-only through `Content/mod_tbmoditemspawnextension.json`: weight `25`, minimum `0`, maximum `0`, item `crude_oil`.
- Native world-drop and collection-bonus behavior owns the result; the package does not inject `crude_oil x1` directly into the backpack.
- Native official-content enablement owns on/off state. There is no DTMAPI config-menu registration or code-owned toggle.
- The current source version remains `0.3.1-dtmapi`; promotion and versioning remain blocked pending the later product release work.
