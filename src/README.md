# Source Layout

DTMAPI source should grow under these projects:

- `DTMAPI.Abstractions`: public author contracts with per-surface stability and adoption disposition; public does not mean uniformly Stable.
- `DTMAPI.Core`: manifest parsing, dependency ordering, mod loading, logging, config, event dispatch, error isolation.
- `DTMAPI.BepInExBootstrap`: the one BepInEx plugin entry; the current package also places four co-located Runtime dependencies beside it under `BepInEx/plugins/DTMAPI`, while managed Mods remain outside that directory.
- `DTMAPI.GameBridge.DolocTown`: proven SharedNative Doloc Town Unity/Harmony/reflection adapters; not the default owner of Platform or single-product ProductNative code.
- `DTMAPI.ModConfigMenu`: built-in declaration-based config menu API and UI.
- `DTMAPI.ContentPatcher`: official JSON/content-pack bridge, added after the runtime core is stable.
- `DTMAPI.ConsoleCommands`: diagnostics and developer commands.
- `DTMAPI.TemplateMod`: minimal author template.

Do not import old DLKsmapi source. Build the new runtime from these boundaries and the canonical Mod/ownership model in `../PROJECT.md`. Corrected Phase 0, bounded G2 and the exact current admitted-product/evidence set are recorded only by `../docs/architecture/batch6-managed-mod-identity-contract.md`. Advanced remains SDK-, identity- and tracked-policy-bound; the verified first-party set is not a general authoring lane or permission for another product.
