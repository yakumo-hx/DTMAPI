# Architecture Notes

Write durable architecture decisions here. Keep implementation details in source docs and hook details in `docs/hook-map`.

Initial architecture:

```text
BepInEx -> DTMAPI.BepInExBootstrap -> DTMAPI.Core -> DTMAPI.GameBridge.DolocTown -> DTMAPI.Abstractions -> DTMAPI mods
```

The public API must remain more stable than the decompiled game internals.
