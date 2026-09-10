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

- [platform-next.md](platform-next.md): accepted long-term platform architecture, capability boundaries and cross-domain decisions; routes to staged product acceptance and the implementation queue. Planned APIs are not current behavior.
- [platform-author-delivery.md](platform-author-delivery.md): author projects, SDK targets, actual Mono debugging, dependencies, distribution and compatibility design.
- [platform-sdk-build.md](platform-sdk-build.md): accepted standard MSBuild backend, frozen references, ordinary project ownership, exact artifact packaging and complete 0.7.0 SDK migration; implementation remains gated by the author/Mono acceptance plan.
- [platform-runtime-contracts.md](platform-runtime-contracts.md): lifecycle, scope, scheduler, commands, owner cleanup and native capability design.
- [platform-data-content.md](platform-data-content.md): transactional Data, ModContent/GameContent, Host/Pack and game-domain evolution design.
- [runtime-workshop-installer-boundary.md](runtime-workshop-installer-boundary.md): player Runtime BAT/clean-CMD/PowerShell host boundary, shared compatibility ownership, supported capability floor and required package matrix.
- [managed-product-admission-registry.md](managed-product-admission-registry.md): version-independent, Catalog-generated exact managed Advanced/ProductNative admission set. It deliberately contains no current publication or subscription claims.
- [batch6-managed-mod-identity-contract.md](batch6-managed-mod-identity-contract.md): frozen Batch 6 / 0.5.5 historical annex containing Phase 0, G2 and bounded product migration evidence. It is not a current admission, publication or subscription authority and receives no future implementation updates.
- [20260611-product-roadmap-community-loop.md](../archive/architecture/2026/20260611-product-roadmap-community-loop.md): mid/long product roadmap covering 0.5.0-alpha Developer Preview through 0.8 Content Pipeline Phase 1, long-term module refactors, player feedback loop, and recommended execution order.
- [20260608-runtime-hardening-branch-roadmap.md](../archive/architecture/20260608-runtime-hardening-branch-roadmap.md): archived roadmap for using clean `master` plus narrow feature branches to harden runtime, clarify API stability, split GameBridge, rebuild CameraView, and later expand content pipeline work.
