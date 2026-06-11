# AutoFishingMod DTMAPI Migration

This migration understands the old mod as: use the selected fishing rod, preserve original costs/pools/results, automate cast/wait/hook/minigame/recast, and stop on manual movement/menu input.

Current 0.5.0-alpha boundary:

- DTMAPI-native `DtmMod` entry.
- Unified config menu for the main automation switches and timing options.
- Registered input keys for toggle and fish-info/config entry.
- The mod registers `IFishingAutomationApi` policy/state with `DTMAPI.GameBridge.DolocTown`.
- Title-page DTMAPI Settings is the player-facing config entry; the migrated default toggle remains F6.
- External F6 input, toggle/state, native auto-cast, manual movement cancel, wait-phase `InstantBite`, and skip=false minigame completion have third-save smoke evidence.
- The UI now describes the current experimental semantics instead of promising unsupported behaviors:
  - selected-rod-only casting is required; no-rod and wrong-selected-item states are safe no-ops;
  - `AutoRecast=false` and `RequireSelectedFishingRod=false` remain config-compatible fields, but the experimental GameBridge normalizes them to safe values;
  - `Delayed minigame success` waits for a real `FishingGameScrollBar`, then forces native status to `Success`; it is not a progress-aware solver;
  - `Skip after instant bite` only affects the `InstantBite` transition and is not an independent minigame skip;
  - `Fast animations` may no-op unless the current cast/pull path exposes supported animator fields;
  - auto reel without instant bite is not promised.
- The visual fish-info page remains experimental/pending.
