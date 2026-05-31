# FishBreedingAssistantMod DTMAPI Migration

This migration understands the old mod as: identify fish roe, append the hatch fish, incubation time, growth time, and parent summary to item title/description/detail rendering.

Current 0.1.10 boundary:

- DTMAPI-native `DtmMod` entry.
- Uses the existing generated fish lookup data as migration input.
- Registers a cached fish roe lookup provider with `IItemTooltipApi`.
- Config menu supports title/detail toggles and cache/log options.
- Tooltip rendering hooks are pending GameBridge verification, so final in-game tooltip evidence is not yet captured.
