# DTMAPI Zoom

Zoom 1.1.0 is an Advanced ProductNative candidate for Runtime 0.7.5, delivered
through the ordinary Author SDK and NativeContract V2. Its Harmony owner is
`DTMAPI.ZoomMod.Native`.

| Action | Default binding |
| --- | --- |
| Increase view | `Equals`, `KeypadPlus` |
| Decrease view | `Minus`, `KeypadMinus` |
| Go to configured maximum | `Shift+Equals`, `Shift+KeypadPlus` |
| Return to 1× | `Shift+Minus`, `Shift+KeypadMinus` |

All four actions can be rebound. A matching combination takes priority, so
pressing physical Shift with a key does not also apply the ordinary step. The
main keyboard's Equals key and the keypad Plus key are distinct; the printed
`+` character does not itself mean Shift is held. Gameplay focus rules apply.

Maximum view is 1–10 (new default 4); Step is 0.05–2 (new default 0.5). Existing
valid settings and explicit empty bindings are preserved. Official Mod enable
and disable controls own the lifecycle. Loading a save or returning to title
resets the session to 1×; the current session scale is not saved as configuration.

The product holds exact Camera and native background/fog sprite objects. It
conditionally restores only values it still owns, preserves sprite Z, native
positions, parallax material offsets, color and visibility, and yields to
observable later writes. Native resolution refreshes see the held 1× gameplay
size. Zoom does not initiate native refresh, resize UI or expand gameplay range.
At 1× with no restoration debt it stops its extra frame observation.

The accepted S04 normal-behavior scope covers key actions, rebinding and short normal walking; native name-field suppression, keys held across close and recovery after release/repress; active indoor/outdoor scenes, weather/resolution changes, exact-object restoration and the Y/Manager shared-input path.

The input runs used OS-synthesized events (`PhysicalHardware=false`); native UI
and scene setup was programmatically driven. Physical keyboard/controller and
broader device/platform coverage are outside this acceptance. Final
release-candidate runtime and performance validation remain separate.
