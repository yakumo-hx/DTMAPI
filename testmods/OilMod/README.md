# DTMAPI Oil

Official-local DTMAPI content package for the experimental oil item.

- Adds `dtmapi_oil` through official `Content/**/item_tbitem.json`.
- Uses `electric_energy=1500`, intentionally higher than the base table's current highest fuel value (`pumpkin=1200`).
- Uses the base coal icon until a scoped DTMAPI asset pipeline is added.
- Third-save smoke `GAME-SMOKE/20260603-190919` verifies Y-console-facing metadata (`sourceId=Local.DTMAPI_Oil`, `category=material_ore`, `title=石油`, `indexedIcon=icon_item_coal`, sale/buy/fuel fields) and confirms `fuelEnergy=1500` is above base highest `pumpkin=1200`.
- Coal mining drops are GameBridge runtime behavior, not copied Workshop content. Third-save smoke `GAME-SMOKE/20260603-190919` verifies a rendered `coal_mine` hit through native `ToolCollider.HandleTools` placed `dtmapi_oil x1` through native backpack placement.
- Mine machine output can also include oil through the experimental Machine API output table.
