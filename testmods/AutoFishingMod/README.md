# AutoFishingMod DTMAPI Migration

This migration understands the old mod as: use the selected fishing rod, preserve original costs/pools/results, automate cast/wait/hook/minigame/recast, and stop on manual movement/menu input.

Current 0.1.13 boundary:

- DTMAPI-native `DtmMod` entry.
- Unified config menu for the main automation switches and timing options.
- Registered input keys for toggle and fish-info/config entry.
- The mod registers `IFishingAutomationApi` policy/state with `DTMAPI.GameBridge.DolocTown`.
- Title-page DTMAPI Settings is the player-facing config entry; the migrated default toggle remains F6.
- External F6 input, toggle/state, Chinese experimental status, and the wait-phase `InstantBite` behavior have third-save Steam smoke evidence.
- Auto cast, skip-minigame, recast, fast animations, and the visual fish-info page remain experimental/pending.
