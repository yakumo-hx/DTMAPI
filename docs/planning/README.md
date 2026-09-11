# Planning Documents

## Current platform implementation

- [Platform-next implementation handoff](platform-next/README.md): `active` long-term capability map, M1–M6 roadmap, product acceptance, accepted architecture and continuous implementation tasks. One task-status queue separates code completion from product evidence; current Runtime/API and release facts remain in their canonical owners.

## Contributor support proposal

- [其他开发者支持计划](contributor-support.md): `proposed` independent plan for the accepted 0.7.0 public source handoff and bounded 0.7.1 Linux source build/test work. It does not change the platform-next API/content roadmap or authorize publication; implementation has not started.
- [0.7.0 公开源码交付具体设计](../reviews/code/2026/20260911-0003-public-source-delivery-design.md): `proposed` path rules, license and build dependencies, exact-commit export, E/P/N PR synchronization and Windows source acceptance for item 2. Includes a non-executable policy draft; no publication or Linux implementation.

## Earlier plans and domain routes

This folder also stores the user's original prompts and earlier or domain-specific plans. These remain reference material for the new platform sequence:

- [`DolocTownModdingAPI.md`](DolocTownModdingAPI.md): `superseded` compatibility handoff for historical backlinks; current product and architecture facts route to their canonical owners, while the complete 2026-06-01 transcript is retained as [frozen audit history](archive/20260601-dtmapi-original-planning-transcript.md).
- [`Debug.md`](Debug.md): `superseded` compatibility handoff for historical backlinks; current Debug work routes through [`docs/debug/INDEX.md`](../debug/INDEX.md), while the complete 2026-06-13 planning transcript is retained as [frozen audit history](archive/20260613-dtmapi-debug-system-planning-transcript.md).
- [`20260712-dtmapi-lightweight-functional-mod-roadmap.md`](../archive/planning/2026/20260712-dtmapi-lightweight-functional-mod-roadmap.md): phased roadmap for reducing the normal player runtime, extracting QA/compatibility code, and moving product policy into functional Mods.
- [`20260731-runtime-query-lifecycle-driver-logging-roadmap.md`](20260731-runtime-query-lifecycle-driver-logging-roadmap.md): current C1/Lifecycle-minimum/F1/G1 closeout and the next-version thin official query, individually decided lifecycle, F3 and G2 route.
- [`20260731-audio-replacement-bridge-roadmap.md`](../archive/planning/2026/20260731-audio-replacement-bridge-roadmap.md): current idempotent audio-status closeout plus the next-version structured install, physical/behavioral status and bounded backend route.
- [`20260801-debugconsole-world-actions-roadmap.md`](../archive/planning/2026/20260801-debugconsole-world-actions-roadmap.md): frozen 2026-08-01 hiding decision with a historical handoff; Monster/Animal and item-browser Infinite Fuel are implemented, Resource remains hidden, and Old City Guardian visible partial success is closed by the 2026-08-30 Review/Update. Current Y-console technical debt plus the separately bounded range-maturity and confirmed selected-slot destruction feature order route to [`20260831-0007`](../updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md); nullable warnings are closed, while `0.3.1` Compatibility physical retirement remains deferred.
- [`20260802-0001 DTMAPI 0.6.0（8 月 2–5 日）冻结路线记录`](../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md): historical execution record with a compact unresolved-transaction handoff. It is frozen, is not a current implementation source, and must not receive later progress appendices; each reopened item starts from its canonical owner and a new bounded Update.

Lifecycle and authority must be explicit in a planning document or in this router. Directory placement, a recent-sounding title, or the absence of a `frozen` label does not make a note a current requirement or implementation source. Frozen and superseded entries are audit history only and cannot directly open later work. Resolve current operational rules through `PROJECT.md`, `AGENTS.md`, and `docs/onboarding/current-state.md`; reopened implementation starts from the relevant canonical owner and a new bounded Review/Update.
