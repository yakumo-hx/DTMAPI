# DTMAPI Auto Fishing

DTMAPI Auto Fishing is the sole admitted first-party Advanced CodeMod pilot. It is optional; installing the five-assembly DTMAPI Runtime does not install or enable this product.

The product DLL owns its fishing state machine, cached native adapters, Harmony patch set, transactions, input/animation overrides and cleanup. It uses only the AutoFishing-specific SDK policy `doloctown-23762374-autofishing-v1`; it does not consume GameBridge, the deleted first-party primitive seam, or the frozen `IFishingAutomationApi`. That frozen public API remains a mandatory Runtime compatibility path for retained old binaries only.

Current boundary:

- DTMAPI-native `DtmMod` entry in canonical `Yuuka.DTMAPI.AutoFishing.dll`.
- SDK-generated `CodeModKind=Advanced` manifest, exact build-23762374 native reference receipt and canonical Harmony owner `dtmapi.mod.yuuka.dtmapi.autofishing`.
- Author SDK `build`, `pack` and `deploy` are the only production authority; the package contains one product DLL and no native dependency DLLs.
- Unified config menu for the toggle hotkey and optional behavior switches.
- Configurable toggle hotkey, defaulting to F6.
- Toggle input uses one owner-bound Gameplay keybind registration, so short pressed edges are armed before the frame and the title screen does not poll the binding.
- Enabling starts the product-private fishing session; F6 off leaves only fast-return process-lifetime callbacks and no recurring updater, active transaction, or input/animation override.
- F6, movement cancel, prompts, 0.25-second recast timing, minigame decisions, and config menu wording belong to this product, not DTMAPI Core or the compatibility API.
- Title-page DTMAPI Settings is the player-facing config entry; the migrated default toggle remains F6 and can be rebound or set to `None`.
- Default behavior after F6: cast at the configured charge, wait for native bite, reel, show and auto-complete the real minigame, collect the native result, and recast.
- Native `HorizontalMoveFactor` is the primary movement-cancel signal at `abs(value) > 0.001`. A/D/Space/Shift snapshots are used only if the native property is unavailable; Space/Shift alone are not movement when the native factor is readable.
- Optional behavior switches:
  - `Instant bite` skips the native waiting period after the hook reaches water, then reels into the normal minigame/result path.
  - `Cast charge` accepts 0–1 in 0.05 steps. Zero releases at exact minimum power; the native Ready animation gate still completes the backswing before Cast.
  - `Skip minigame` routes bite-ready results through the native no-minigame result path and preserves native success/failure.
  - `Fast fishing animations` accepts multiplier 1–4 in 0.5 steps (default 3). The native Ready backswing/charge animator, positive-charge timer progress, hook velocity/gravity, and Pull timing are accelerated without changing the charge target; original animator speeds are restored at state/session boundaries.
- InstantBite, CastChargeRatio, SkipMiniGame, and FastAnimations are independent. Skip mode does not acquire a synthetic-input lease; InstantBite does not imply Skip; FastAnimations does not change the charge target, wait policy, or result policy.
- The old product `VerboseLogging` field remains ignored. The compatibility DTO retains `CastChargeRatio` for existing reviewed clients.
- AutoFishing smoke coverage now targets the fifth save fixture, where the player starts in front of a pond; synthetic pool/cache/wait-state setup is not accepted for final evidence.
