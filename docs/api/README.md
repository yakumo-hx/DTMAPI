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

Stability is only one axis. Also project the adoption disposition from
`DtmApiDispositionAttribute`: `Open`, `Frozen`, `Diagnostic`, `Internal`, or
`Disabled`. A public type without Stable status is not
made stable by living in `DTMAPI.Abstractions`; Frozen types are retained for
old binaries rather than recommended to new mods, and Diagnostic/Disabled/
internal types are not ordinary gameplay dependencies.

Do not use `verified` as a stability label. Smoke evidence, screenshots, and
internal harness logs can support a status decision, but they do not replace the
stable promotion criteria in the matrix.

## Input Suppression Scope

`IInputHelper.Suppress(button)` is DTMAPI-visible helper state only. It lasts
until the same runtime `Input.ClearFrame` boundary that clears `WasPressed`.
It does not erase sampled physical down/edge state, manufacture a later
Pressed, or cancel a Release already owed to a Pressed recipient. In an owner
modal only that owner may suppress; a platform-modal Mod call is inert. It does
not block Doloc Town native tool, item, menu, Unity Input, or third-party
Harmony paths and is not full native input isolation.
