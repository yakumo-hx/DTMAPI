# DTMAPI Zoom

Experimental 0.3.1 official-local mod for large-view camera zoom.

- Registers `ICameraZoomApi` with vanilla minimum `1x`, default maximum `4x`, and `0.25x` steps.
- Uses `Plus` / `Equals` / `KeypadPlus` to increase view range and `Minus` / `KeypadMinus` to decrease it.
- GameBridge adjusts only the world camera `orthographicSize`; UI scale and click mapping are not intentionally changed.
- Save-load and returned-to-title boundaries reset the runtime camera back to the vanilla scale.
