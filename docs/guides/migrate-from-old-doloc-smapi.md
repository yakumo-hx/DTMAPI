# Migrate From Old DolocTown SMAPI / DLK Mods

DTMAPI `0.5.0-alpha` should be installed as a new runtime and new set of Workshop mods. The safest player migration is not to overwrite old DLK/SMAPI Workshop items.

## Safe Migration

1. In Doloc Town or Steam Workshop, disable/unsubscribe old DLK/SMAPI feature mods.
2. Run the old SMAPI uninstaller if you installed the old runtime.
3. Restart Steam and Doloc Town.
4. Confirm old feature mods no longer appear in the in-game enabled mod list.
5. Subscribe to the new DTMAPI Runtime and run `1_install_dtmapi.bat`.
6. Subscribe to new DTMAPI feature mods.
7. Use title-page `DTMAPI Settings -> Status/Mods/Logs` to verify runtime and export a report if something looks wrong.

## Why This Is Safer

Old SMAPI/DLK runtime files and new DTMAPI runtime files can both patch game code. Running both at the same time can cause duplicate hooks, stale DLL state, or mixed configuration paths. The supported migration path is therefore:

```text
old SMAPI runtime removed
old DLK mods unsubscribed or disabled
game restarted
DTMAPI runtime installed
new DTMAPI mods enabled
```

## What Unsubscribe Cleans

Steam unsubscribe normally removes Workshop-downloaded content after Steam and the game refresh. It does not reliably remove manually copied local folders under the user local `MODS` directory. If a local `MODS/DLK_*` folder remains, DTMAPI reports it as legacy content so the player can back it up or remove it deliberately.

Old AutoFishing-like code mods may also remain loaded until the game process restarts. Always restart the game after unsubscribe/disable before judging whether the old mod is gone.

## Never Deleted Automatically

DTMAPI does not delete Steam Workshop content, player save files, `DTMAPI/reports`, `DTMAPI/config`, or `DTMAPI/backups` during migration.
