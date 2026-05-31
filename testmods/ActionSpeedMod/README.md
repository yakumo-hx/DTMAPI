# ActionSpeedMod DTMAPI migration

This is the first DTMAPI-native migration slice for `Yuuka.DTMAPI.ActionSpeed`.

Implemented in this slice:

- DTMAPI manifest and `DtmMod.Entry`.
- Defaults-off config model.
- DTMAPI config menu coverage for bool, number, text, choice, and keybind fields.
- Title-page DTMAPI Settings config entry; F10 remains only a diagnostic shortcut while the migration is under test.
- `UpdateTicked` evidence without per-frame log spam.
- Runtime apply/restore boundary logs.

Not complete yet:

- The old timing-sensitive Harmony patches for tool/eat/fill/machine/harvest animation speed are intentionally not copied here.
- Those need a DTMAPI GameBridge/API pass and hook evidence before this mod can claim functional action-speed parity.
