# DTMAPI.BepInExBootstrap

The only DTMAPI assembly containing the BepInEx plugin entry. The player package co-locates four DTMAPI Runtime dependencies under `BepInEx/plugins/DTMAPI`; managed CodeMods and ContentPacks never become additional BepInEx plugin entries. Bootstrap initializes Core and hands off all ecosystem behavior.
