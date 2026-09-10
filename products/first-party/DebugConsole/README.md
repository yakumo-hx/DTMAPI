# Y-Key Console Mod

ProductNative in-save Y-key diagnostic console. Current public release is
`1.1.1`; source candidate `1.1.2` requires DTMAPI `0.6.1` and retains the
exact `doloctown-24456188-debugconsole-v1` Author policy.

- Press `Y` after `SaveLoaded` to open or close the console.
- Press `Escape` while the console is open to close it.
- The item browser includes ten native item categories plus searchable Monster
  and Animal catalogs. The source column is stable across all categories and
  its counts combine item rows, monster cards and animal state cards. Animals
  come from the merged runtime `TbAnimal` table, so loaded DTMAPI animal packs
  participate without a species allowlist. Infinite Fuel is an ordinary
  coal-shaped fuel item; Resource spawning remains hidden.
- World tools include time/weather-period actions, movement speed, fixed
  weather controls and exact teleport targets. The obsolete current-location
  label and manual-address CSV exporter are not part of the current UI.
- On Doloc Town 1.00, weather buttons delegate to the exact official current-room command and verify `LocalWeatherType`; `+tech` adds 100 once to NATURE, OPERATE, SCIENCE, and ANIMAL.
- The product is an SDK-generated Advanced CodeMod. It owns its UI, typed
  input, fixed native-action allowlist and transient movement/time-scale/creative leases.
- It exposes no arbitrary command parser and consumes no frozen DebugConsole or GameBridge Diagnostic API.
- Formal installs package it as official-local
  `MODS/DTMAPI_YKeyConsole/Content/DTMAPI`. The retired game-root `Mods` lane
  is not an ordinary player install path. It must not be installed under
  `BepInEx/plugins`.

The `1.1.2` source candidate recognizes the official Old City Guardian
(`space_ship`) as one root plus two synchronously created
`space_ship_bastion` entities. Monster and animal batches keep the existing
left-click `1` / right-click `10` controls, use sequential official calls, stop
the current batch at its first failure, retain every entity already added and
show requested roots, successful roots and actual additions. There is no
spawn rollback, shared circuit, Guardian-specific disable path or extra
confirmation. The immutable public `1.1.1` artifact still predates this fix.
