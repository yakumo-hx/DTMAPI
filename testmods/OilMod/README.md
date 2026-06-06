# DTMAPI Oil

Official-local DTMAPI content package for the experimental oil item.

- Adds `crude_oil` through official `Content/**/item_tbitem.json`.
- Uses `electric_energy=1500`, intentionally higher than the base table's current highest fuel value (`pumpkin=1200`).
- Uses the base coal icon until a scoped DTMAPI asset pipeline is added.
- Current 0.2.9 target: Y-console-facing metadata should show `crude_oil` from `Local.DTMAPI_Oil`, `category=material_ore`, `title=原油`, `indexedIcon=icon_item_coal`, sale/buy/fuel fields, and `fuelEnergy=1500` above base highest fuel.
- Coal mining drops are GameBridge runtime behavior, not copied Workshop content. Current 0.2.9 target: a rendered `coal_mine` hit through native `ToolCollider.HandleTools` should place `crude_oil x1` through native backpack placement.
- Legacy note: old 0.2.4-0.2.7 test saves may contain `dtmapi_oil`; 0.2.9 no longer generates it as active content.
- Mine machine output can also include oil through the experimental Machine API output table.
