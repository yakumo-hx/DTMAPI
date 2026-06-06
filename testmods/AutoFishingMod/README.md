# AutoFishingMod DTMAPI Migration

This migration understands the old mod as: use the selected fishing rod, preserve original costs/pools/results, automate cast/wait/hook/minigame/recast, and stop on manual movement/menu input.

Current 0.2.4 boundary:

- DTMAPI-native `DtmMod` entry.
- Unified config menu for the main automation switches and timing options.
- Registered input keys for toggle and fish-info/config entry.
- The mod registers `IFishingAutomationApi` policy/state with `DTMAPI.GameBridge.DolocTown`.
- Title-page DTMAPI Settings is the player-facing config entry; the migrated default toggle remains F6.
- External F6 input, toggle/state, native auto-cast, manual movement cancel, wait-phase `InstantBite`, independent skip-minigame, pull animation acceleration, and skip=false minigame auto-complete have third-save smoke evidence.
- `Skip minigame` and `Auto-complete minigame` are separate config rows: skip jumps from bite to pull/result, while auto-complete only touches a visible `FishingGameScrollBar`. The skip=false proof uses a smoke-only force-fish gate so the test reaches a real minigame; ordinary player rolls still preserve original fish/trash outcomes.
- Recast and the visual fish-info page remain experimental/pending.
