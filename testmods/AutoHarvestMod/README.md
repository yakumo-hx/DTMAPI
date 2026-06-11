# Auto Harvest Mod

DTMAPI sample mod for the Experimental `ICropHarvestingApi`.

This mod intentionally does not reference `Assembly-CSharp`, Harmony, or raw
Doloc Town crop types. It requests semantic crop scans and harvests from the
DTMAPI Doloc Town GameBridge. The default config is disabled so the sample does
not automatically harvest a player's save unless the player turns it on.
