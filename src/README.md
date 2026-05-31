# Source Layout

DTMAPI source should grow under these projects:

- `DTMAPI.Abstractions`: stable API referenced by mod authors.
- `DTMAPI.Core`: manifest parsing, dependency ordering, mod loading, logging, config, event dispatch, error isolation.
- `DTMAPI.BepInExBootstrap`: the only DTMAPI assembly placed in `BepInEx/plugins`.
- `DTMAPI.GameBridge.DolocTown`: Doloc Town Unity/Harmony/reflection hooks.
- `DTMAPI.ModConfigMenu`: built-in declaration-based config menu API and UI.
- `DTMAPI.ContentPatcher`: official JSON/content-pack bridge, added after the runtime core is stable.
- `DTMAPI.ConsoleCommands`: diagnostics and developer commands.
- `DTMAPI.TemplateMod`: minimal author template.

Do not import old DLKsmapi source. Build the new runtime from these boundaries.
