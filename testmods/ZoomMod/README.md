# DTMAPI Zoom

Experimental 0.4.2 official-local mod for large-view camera zoom.

- Acquires an `ICameraViewApi` lease with vanilla minimum `1x`, default maximum `4x`, and `0.25x` steps.
- Uses `Plus` / `Equals` / `KeypadPlus` to increase view range and `Minus` / `KeypadMinus` to decrease it.
- GameBridge arbitrates playable-view leases between mods and writes only the world camera orthographic size.
- The playable zoom path does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, or background/fog panorama compensation.
- UI scale and click mapping are left untouched. Save-load, returned-to-title, explicit lease release/reset, and environment-camera boundaries restore or reapply the playable view safely.
