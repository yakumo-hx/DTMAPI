# ActionSpeedMod DTMAPI Migration

DTMAPI-native migration of `Yuuka.DTMAPI.ActionSpeed`.

Implemented:

- DTMAPI manifest and `DtmMod.Entry`.
- Player-facing config menu focused on core toggles and multipliers.
- `UpdateTicked` evidence without per-frame log spam.
- Runtime apply/restore boundary logs.
- GameBridge-backed tool animation, interaction animation, eat/drink continuous-use timing, bottle-fill timing, machine/feed add timing, harvest timing, and plant timing.
- Right-click continuous drinking policy for bottled water and in-water bottle-fill policy remain routed through GameBridge continuous-use paths.

Experimental boundary:

- Auto-fill bottle is exposed as policy/config text but deeper hands-free trigger evidence is still experimental.
- See `ACTIONSPEED-001` and `ACTIONSPEED-002` in the smoke matrix for current hook evidence.
