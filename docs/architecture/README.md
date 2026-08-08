# Architecture Notes

Write durable architecture decisions here. Keep implementation details in source docs and hook details in `docs/hook-map`.

Current direction (the normative ownership rules remain in [`PROJECT.md`](../../PROJECT.md)):

```text
BepInEx bootstrap -> DTMAPI Core -> stable public API -> Strict CodeMod
                         |-> shared-native GameBridge -> stable adapter/API
                         |-> future optional Content Host -> ContentPack
                         `-> managed Advanced CodeMod -> ProductNative

External BepInEx Plugin ------------------------------------ outside DTMAPI ownership
```

The public API must remain more stable than the decompiled game internals.

## Records

- [runtime-workshop-installer-boundary.md](runtime-workshop-installer-boundary.md): player Runtime BAT/clean-CMD/PowerShell host boundary, shared compatibility ownership, supported capability floor and required package matrix.
- [batch6-managed-mod-identity-contract.md](batch6-managed-mod-identity-contract.md): corrected Phase 0, verified G2 synthetic fixture and the exact twelve-product Advanced/ProductNative baseline for manifest, Loader, SDK, Doctor, Manager, package and UI identities. Every proof remains identity- and policy-bound; later products, general Advanced authoring and G7 remain blocked. Runtime 0.5.5 publication facts are owned by Update 20260801-0003 rather than this identity index.
- [20260611-product-roadmap-community-loop.md](20260611-product-roadmap-community-loop.md): mid/long product roadmap covering 0.5.0-alpha Developer Preview through 0.8 Content Pipeline Phase 1, long-term module refactors, player feedback loop, and recommended execution order.
- [20260608-runtime-hardening-branch-roadmap.md](20260608-runtime-hardening-branch-roadmap.md): archived roadmap for using clean `master` plus narrow feature branches to harden runtime, clarify API stability, split GameBridge, rebuild CameraView, and later expand content pipeline work.
