# AutoFishingMod

AutoFishing uses the selected fishing rod and lets the native Doloc Town fishing states own costs, pools, bite rolls, minigame, pull, and results.

Current 0.5.1-alpha experimental boundary:

- DTMAPI-native `DtmMod` entry.
- Unified config menu for the toggle hotkey, optional behavior switches, and charge amount.
- Configurable toggle hotkey, defaulting to F6.
- The mod registers `IFishingAutomationApi` policy/state with `DTMAPI.GameBridge.DolocTown`.
- Title-page DTMAPI Settings is the player-facing config entry; the migrated default toggle remains F6 and can be rebound or set to `None`.
- Default behavior after F6: cast with the configured charge amount, wait for native bite, reel, show and auto-complete the real minigame, collect the native result, and recast.
- Optional behavior switches:
  - `Instant bite` skips the native waiting period after the hook reaches water, then reels into the normal minigame/result path.
  - `Skip minigame` routes bite-ready results through the native no-minigame result path and preserves native success/failure.
  - `Cast charge` controls the Ready phase release point from `0` no charge to `1` full charge; default is `0`.
  - `Fast cast/pull animations` applies only to native Ready charge, cast hook flight, and Pull phases.
- AutoFishing smoke coverage now targets the fifth save fixture, where the player starts in front of a pond; synthetic pool/cache/wait-state setup is not accepted for final evidence.
