# DTMAPI Mine

Experimental 0.2.5 mod package for the new mine machine.

- Official JSON adds `dtmapi_mine` as a separate equipment item and recipe.
- The equipment reuses `sprite_equipment_well` / `icon_item_well` but keeps its own ID, uses `EquipmentFuncCase` with a `16` slot / `4` per-line internal case, keeps an `8x6` footprint versus the base well's `4x3`, and does not replace the base well.
- The fallback recipe is added to `equipment_workbench`, unlocks through the official Industrial tech route, and consumes `metal_framework x15`, `engine_core x10`, `steel_ingot x20`, and `coal x100`.
- When OilMod is loaded and the recipe replacement option is enabled, GameBridge overrides the native recipe inputs to `metal_framework x10`, `engine_core x5`, `steel_ingot x20`, and `crude_oil x10`.
- Runtime fuel/electric production is declared through `IMachineProductionApi`; `GAME-SMOKE/20260604-111533` verifies native item/equipment/recipe/group metadata, generated `DolocTown.ItemEquipment`, official tech route `node=dtmapi_mine`, `parent=alloy_material`, `rightOfParent=True`, `aboveCommander=True`, visible renderer scale `2x2`, player-placement evidence for a temporary `dtmapi_mine`, screenshot `DTMAPI-evidence/NEWCONTENT-025/20260604-111618/mine-placed-dtmapi-mine.png`, electric-mode production, Mine-owned storage output `storage=2/16`, and clean exit.
