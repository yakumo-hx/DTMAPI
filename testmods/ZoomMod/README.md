# DTMAPI Zoom

Experimental 0.4.2 official-local mod for large-view camera zoom.

- Registers `ICameraZoomApi` with vanilla minimum `1x`, default maximum `4x`, and `0.25x` steps.
- Uses `Plus` / `Equals` / `KeypadPlus` to increase view range and `Minus` / `KeypadMinus` to decrease it.
- GameBridge adjusts the world camera through the native camera owner path, refreshes `CameraController`, and refreshes native scanners.
- Background and depth-fog transforms are compensated with lifecycle restore guards; UI scale and click mapping are left untouched.
- Save-load, returned-to-title, and environment-camera boundaries reset or reapply the runtime camera safely.
