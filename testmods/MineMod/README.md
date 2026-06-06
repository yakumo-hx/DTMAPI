# DTMAPI Mine

Experimental 0.3.1 mod package for the new mine machine.

- Official JSON adds `dtmapi_mine` as a separate equipment item and recipe.
- The equipment reuses `sprite_equipment_well` / `icon_item_well` but keeps its own ID, uses `EquipmentFuncCase` with a `16` slot / `4` per-line internal case, keeps an `8x6` footprint versus the base well's `4x3`, and does not replace the base well.
- The fallback recipe is added to `equipment_workbench`, unlocks through the official Industrial tech route, and consumes `metal_framework x15`, `engine_core x10`, `steel_ingot x20`, and `coal x100`.
- When OilMod is loaded and the recipe replacement option is enabled, GameBridge overrides the native recipe inputs to `metal_framework x10`, `engine_core x5`, `steel_ingot x20`, and `crude_oil x10`.
- Runtime hybrid fuel/electric production is declared through `IMachineProductionApi`: default electric mode consumes 10 official power plus low DTMAPI fuel, while pure fuel mode is available and consumes fuel faster. Previous evidence `GAME-SMOKE/20260604-111533` and 0.2.9 smokes verify native item/equipment/recipe/group metadata, generated `DolocTown.ItemEquipment`, official tech route `node=dtmapi_mine`, `parent=alloy_material`, `rightOfParent=True`, `aboveCommander=True`, visible renderer scale `2x2`, Mine-owned storage output, and clean exit; 0.3.1 evidence must recheck the new hybrid mode fields.
