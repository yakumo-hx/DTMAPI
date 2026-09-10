# API And Native Owner Phase Summary - 2026-07-06

Status: docs-only synthesis

Sources:

- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/native-owner-domains/*.md`
- recent update records through `20260705-0012`

## Core Rule

DTMAPI API rebuild work must start from the native responsibility function or state holder, then pass through GameBridge into public abstractions. UI success, registry success, helper smoke, or HookStatus success does not stabilize a public API by itself.

## Public API State

| Bucket | Meaning | Current examples | Debt |
| --- | --- | --- | --- |
| Stable | Public contract has enough native/DTMAPI ownership, mod use, game evidence, clean exit, and regression coverage | `DtmMod`, manifest/helper container basics, logging, config helper basics | Keep child helper status separate from the stable root helper. |
| StableCandidate | Shape is plausible but still short on promotion evidence | registry, translations, `GameLaunched`, config menu registration/query, diagnostics export pieces | Needs broader real-mod usage and manual/game evidence before Stable. |
| Experimental | Useful but not stable API proof | high-frequency events, save events, input helper/events, UI helpers, Workshop/content queries, gameplay helpers such as ActionSpeed, AutoFishing, SaveSlots, ChestLocator, CameraView, AudioReplacement | Must not be treated as stable because a smoke mod succeeded. |
| Diagnostic | Debug or evidence surface, not gameplay contract | diagnostics snapshot, Hook status, Y/debug console evidence | Useful for triage only. Do not build ordinary mods against it as stable behavior. |
| Proposed | Concept exists but native/GameBridge route is not proven | content pipeline, panorama camera, custom entity native adapters | Needs native-owner and runtime design before API shape freezes. |
| Failed/Retired | Previous route rejected or removed | `ICameraZoomApi`, `IMotorVehicleApi` | Future work must start fresh; old smoke is historical only. |

## Native Owner Domain Digest

| Domain | Verdict | What it means now |
| --- | --- | --- |
| 01 world time/weather/season/refresh | Partial | Query owners exist. Mutating date/weather remains debug/experimental. |
| 02 NPC body/behavior/story/trade/location/new NPC | Partial | Existing NPC owners exist. Complete new NPC remains blocked/proposed. |
| 03 livestock/animal behavior | Partial | Lifecycle owners exist. Custom runtime animal creation remains blocked beyond content-pack style work. |
| 04 wild birds/events/drops | Partial | Bird event owner exists. New bird behavior remains proposed. |
| 05 drones runtime/equipment | Partial | Active-drone and slot owners exist. Multiple active drone support remains blocked. |
| 06 flying motor/new vehicle types | Partial | Native motor path is singleton-oriented. Whole new vehicle type remains blocked. |
| 07 maps/dungeons/scenes/resources/teleport | Partial | Room/dungeon/teleport/resource owners exist. Runtime map creation remains blocked. |
| 08 hats/accessories/equipment slots | Partial | Native slots exist. Extra slot mutation is experimental/high risk. |
| 09 food/equipment effects/buffs | Partial | Existing effect owners exist. Brand-new behavior still needs code ownership. |
| 10 item stack/limits | Found | Stack cap and inventory placement owners exist. Mutation needs transaction policy. |
| 11 original follow pet | Blocked | Stable native owner missing; only DTMAPI-owned experimental runtime entity route is plausible. |
| 12 held ranged weapons/projectiles | Partial | Projectile/damage owners exist. Handheld ranged weapon owner is not proven. |

## Phase Conclusions

- The API matrix is now the authority for public-surface status. Native-owner reports are evidence libraries, not promotion records.
- Custom animal content packs are the healthiest current boundary model: data/assets live in content packs, DTMAPI owns generic bridge/diagnostics, and Doloc Town owns simulation.
- Runtime helpers that depend on fragile Unity/Harmony/reflection work should stay behind GameBridge. Public abstractions should avoid raw Unity, Harmony, BepInEx, or decompiled types.
- The SecondMotor result is the cautionary example: a feature can have implementation and smoke history but still be retired after manual QA proves the native boundary is wrong.
- ActionSpeed remains Experimental even after the native-owner pass because third-save manual QA still has edge cases around animal doors/connectors, planting, fertilizer, crop film, sprinklers, and grow lights.

## Current Technical Debt

- Promotion evidence is fragmented across update records, manual QA, smoke matrix rows, and API review files. Future API work should link all of them in one goal file.
- StableCandidate surfaces need two-real-mod usage and manual/game validation before being called stable.
- Experimental gameplay APIs need explicit rollback flags and regression rows while ISSUE-010 remains open.
- Retired/failed APIs should remain visible in the matrix so old local mods fail clearly instead of looking like random loader bugs.
- Native-owner reports must be re-opened against the current reverse baseline before implementation; their historical baseline headers are not enough.

## Next Handoff

For new API rebuild work, start with:

1. `docs/api/public-api-matrix.md`
2. the relevant `docs/reviews/api/native-owner-domains/*.md` report
3. the latest matching manual QA review under `docs/reviews/manual-qa/2026/`
4. the matching Hook Map section if GameBridge hooks are involved
5. a dedicated `docs/goals/YYYY/...` goal plus `.goal.txt` backup
