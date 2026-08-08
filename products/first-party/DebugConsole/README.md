# Y-Key Console Mod

ProductNative in-save Y-key diagnostic console for DTMAPI 0.5.5.

- Press `Y` after `SaveLoaded` to open or close the console.
- Press `Escape` while the console is open to close it.
- First screen: item icon grid, time/weather-period tools, movement speed, weather, and teleport.
- On Doloc Town 1.00, weather buttons delegate to the exact official current-room command and verify `LocalWeatherType`; `+tech` adds 100 once to NATURE, OPERATE, SCIENCE, and ANIMAL.
- The 1.0 product is an SDK-generated Advanced CodeMod. It owns its UI, typed input, fixed native-action allowlist and transient movement/time-scale/creative leases.
- It exposes no arbitrary command parser and consumes no frozen DebugConsole or GameBridge Diagnostic API.
- Formal installs package it as official-local `MODS/DTMAPI_YKeyConsole/Content/DTMAPI`; loose developer installs may use the game's `Mods` path. It must not be installed under `BepInEx/plugins`.
