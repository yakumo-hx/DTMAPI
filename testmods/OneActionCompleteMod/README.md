# OneActionCompleteMod DTMAPI Migration

This migration understands the old mod as: after one normal successful tool hit or direct interaction, finish the remaining tree/ore/weed/garbage/fuel/feeder work while preserving original enablement and energy-cost intent.

Current 0.2.0 boundary (gameplay behavior unchanged from the 0.1.13 verification):

- DTMAPI-native `DtmMod` entry.
- Unified config menu options for all original switches.
- Title-page DTMAPI Settings is the player-facing config entry; F11 remains only a diagnostic shortcut while the migration is under test.
- The mod registers an `IActionCompletionApi` policy with `DTMAPI.GameBridge.DolocTown`.
- Resource/tool-hit completion, tree/ore/garbage/weeds wrong-tool guards, and fuel/feeder native consume/fill paths have third-save smoke evidence.
- Vegetation/dandelion is recorded as an exception path: it uses the game's `VegetationRenderer.OnFell -> Vegetation.CheckToolConstraints(ItemTool)` path, not the `DungeonResourceRenderer` one-action completion path. DTMAPI verifies wrong tools are rejected by the game and correct tools fell through the native path without a DTMAPI forced-completion delta.
