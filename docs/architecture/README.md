# Architecture Notes

Write durable architecture decisions here. Keep implementation details in source docs and hook details in `docs/hook-map`.

Initial architecture:

```text
BepInEx -> DTMAPI.BepInExBootstrap -> DTMAPI.Core -> DTMAPI.GameBridge.DolocTown -> DTMAPI.Abstractions -> DTMAPI mods
```

The public API must remain more stable than the decompiled game internals.

## Records

- [20260608-runtime-hardening-branch-roadmap.md](20260608-runtime-hardening-branch-roadmap.md): archived roadmap for using clean `master` plus narrow feature branches to harden runtime, clarify API stability, split GameBridge, rebuild CameraView, and later expand content pipeline work.
