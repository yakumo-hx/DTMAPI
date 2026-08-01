# DTMAPI Zoom

Advanced ProductNative implementation for `DTMAPI.ZoomMod`.

The product owns only playable world-camera `orthographicSize`, its two
keybinds, and one exact `DolocAPI.SetEnvCamera` Postfix. It does not resize UI,
refresh the scanner, move the camera controller, or compensate panorama/fog.

The old `ICameraViewApi` and `ICameraZoomApi` surfaces remain frozen
compatibility contracts served by the dormant Compatibility Host.
