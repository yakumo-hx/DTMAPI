# API Design Notes

Use this folder for DTMAPI public API contracts:

- Manifest schema.
- `DtmMod` lifecycle.
- `IDtmHelper` services.
- GameLoop/Input/Save/UI events.
- Config and config menu APIs.
- Mod registry and cross-mod API contracts.
- Content pack and official Workshop bridge contracts.

Mark each API with the status vocabulary from
`docs/api/public-api-matrix.md`:

- `Stable`
- `StableCandidate`
- `Experimental`
- `Diagnostic`
- `Proposed`
- `Failed`

Do not use `verified` as a stability label. Smoke evidence, screenshots, and
internal harness logs can support a status decision, but they do not replace the
stable promotion criteria in the matrix.

## Input Suppression Scope

`IInputHelper.Suppress(button)` is DTMAPI helper state only. It lasts until the
same runtime `Input.ClearFrame` boundary that clears `WasPressed`, so ordinary
mods should treat `GetSuppressedButtons()` as one-frame Core state. It does not
block Doloc Town native tool, item, menu, or UI input paths and is not full
native input isolation.
