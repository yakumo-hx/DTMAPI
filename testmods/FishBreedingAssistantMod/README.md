# FishBreedingAssistantMod DTMAPI Migration

This migration identifies fish roe and appends the hatch fish name to the item title only.

Current 0.2.1 boundary:

- DTMAPI-native `DtmMod` entry.
- Uses the existing generated fish lookup data as migration input.
- Registers a cached fish roe lookup provider with `IItemTooltipApi`.
- Config menu is reduced to the core enabled toggle plus a short scope note.
- Detail/description annotation is intentionally disabled in the migrated mod.
- Tooltip rendering hook evidence remains tracked under `FISHROE-001`.
