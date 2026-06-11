# Auto Harvest Mod

DTMAPI sample mod for the Experimental `ICropHarvestingApi`.

This mod intentionally does not reference `Assembly-CSharp`, Harmony, or raw
Doloc Town crop types. It requests semantic crop scans and harvests from the
DTMAPI Doloc Town GameBridge. The default config is disabled so the sample does
not automatically harvest a player's save unless the player turns it on.

The mod scans through `ICropHarvestingApi` and immediately follows up with a
harvest request for the returned target ids. Those ids are opaque, transient
GameBridge handles and are not stored in config or save data.

Current API coverage is intentionally narrow: ordinary PlantBasin-family crop
containers, plus API-classified vine, mushroom-bag, and bush containers when
they are still executed by the reviewed `PlantBasin.Harvest(bool,bool)` native
owner. Tree-basin cocoa crops, grass/forage basins, wild trees, and forage are
not auto-harvested by this sample.
