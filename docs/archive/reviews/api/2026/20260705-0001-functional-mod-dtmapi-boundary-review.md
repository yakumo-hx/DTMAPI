# Functional Mods / DTMAPI Boundary Review

Date: 2026-07-05
Status: recorded / docs-only
Scope: Review the relationship between DTMAPI and functional mods, using SMAPI only as architecture and UX reference. Focus areas: Y-key console, AutoFishing, ActionSpeed, OneActionComplete, and MoreEquipmentSlots / decoration-equipment bar extension.

## Sources Read

- DTMAPI project baseline: `AGENTS.md`, `PROJECT.md`, `docs/planning/DolocTownModdingAPI.md`, `docs/planning/Debug.md`, `references/README.md`, `docs/debug/INDEX.md`.
- DTMAPI API/review baseline: `docs/workflows/codex-api-rebuild.md`, `docs/api/public-api-matrix.md`, `docs/reviews/api/native-owner-domains/INDEX.md`, `docs/reviews/api/local-mods-native-owner/INDEX.md`, `docs/reviews/api/smapi-ecosystem-map/INDEX.md`, `docs/hook-map/README.md`.
- Relevant DTMAPI mods and APIs: `src/DTMAPI.Abstractions/Helpers.cs`, `src/DTMAPI.Abstractions/Events.cs`, `src/DTMAPI.Abstractions/Manifest.cs`, `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`, `testmods/DebugConsoleMod`, `testmods/AutoFishingMod`, `testmods/ActionSpeedMod`, `testmods/OneActionCompleteMod`, `testmods/MoreEquipmentSlotsMod`.
- SMAPI reference files: `src/SMAPI/IModHelper.cs`, `src/SMAPI/IInputHelper.cs`, `src/SMAPI/ICommandHelper.cs`, `src/SMAPI/IModRegistry.cs`, `src/SMAPI/Events/IGameLoopEvents.cs`, `src/SMAPI/Events/IInputEvents.cs`, `src/SMAPI/Mod.cs`, `src/SMAPI.Toolkit.CoreInterfaces/IManifest.cs`, `src/SMAPI.Mods.ConsoleCommands/ModEntry.cs`, `src/SMAPI/Integrations/GenericModConfigMenu/IGenericModConfigMenuApi.cs`.

## Executive Judgment

These functional mods are best treated as first-party/product mods and API demand evidence, not as proof that the underlying DTMAPI gameplay surfaces are stable. SMAPI is useful here mostly for boundary discipline: stable loader/helper/event/config/registry surfaces live in the modding framework, while concrete console commands and gameplay conveniences can be ordinary mods or integrations built on top.

For DTMAPI, the clean relationship is:

- `DTMAPI.Core` / `DTMAPI.Abstractions`: loader, manifest, config, events, logging, registry, UI/config-menu integration, diagnostics/workshop-facing surfaces.
- `DTMAPI.GameBridge.DolocTown`: all Doloc native ownership, Unity/Harmony/reflection, fragile lifecycle and state repair.
- Functional mods: user-facing policy, hotkeys, config defaults, config-menu layout, ordinary UX, and feature-specific enablement.

Do not move the reviewed gameplay features into Core. Keep their public surfaces narrow, explicitly status-marked, and GameBridge-backed. Promote only after native-owner proof, lifecycle behavior, multi-owner policy, save/title transitions, disable/uninstall behavior, and repeatable smoke/manual QA are all documented.

## SMAPI Reference Boundary

SMAPI's `IModHelper` exposes broad but stable mod-author services: events, content helpers, console command registration, config I/O, input, reflection, mod registry, multiplayer, and translations. Its bundled `ConsoleCommands` implementation is an ordinary mod using `ICommandHelper.Add`, not a reason to make every debug command a core framework behavior. Generic Mod Config Menu is also an integration-style declarative API, not a license to expose raw UI internals.

The DTMAPI implication is that a future `ICommandHelper`-like author API could be stable separately from the current Y-key debug console. The current Y console is a diagnostic/product tool with reflected UI and whitelisted debug actions. It should not be conflated with a stable, general-purpose mod command surface.

## Relationship Matrix

| Feature | Current DTMAPI shape | Mod-owned responsibility | DTMAPI/GameBridge-owned responsibility | Status judgment |
| --- | --- | --- | --- | --- |
| Y-key console | `DebugConsoleMod` + reflected console host + debug/diagnostic APIs | Y/Escape UX, save gating, language/config, binding the diagnostic APIs | Canvas host, typed debug actions, native debug paths, input isolation while modal UI is open | Diagnostic product/debug tool. Keep whitelisted typed APIs and Canvas-visible smoke checks; do not expose arbitrary Lua/console/native reflection as ordinary API. |
| AutoFishing | `AutoFishingMod` over `IFishingAutomationApi` and `FishingAutomationFeature` | F6 toggle, movement-cancel policy, config menu, user messaging | Native fishing state machine, cast/bite/minigame/pull/result hooks, bounded strategy options | Experimental but well-shaped as migrated first-party automation. Needs owner lease/arbitration, long-run failure states, energy/result evidence before any promotion. |
| ActionSpeed | `ActionSpeedMod` over `IActionSpeedApi` and action-speed hooks | Multipliers, hotkey/config/menu, user-facing restore/apply policy | Animator/timer writes, native action/interact/eat/bottle/machine/plant classifications | Experimental and high-risk. Split by native subdomain and finish pending manual QA before promotion; avoid stabilizing a broad "speed everything" contract. |
| OneActionComplete | `OneActionCompleteMod` over `IActionCompletionApi` | Config toggles, optional diagnostic shortcut, player-facing policy | `ToolCollider.HandleTools` route, resource completion, fuel/feed interaction exits, vegetation exception handling | Experimental narrow policy API. Keep scoped to proven resource/fuel/feed domains; do not generalize to "complete any action". |
| MoreEquipmentSlots / decoration-equipment bar extension | `MoreEquipmentSlotsMod` over `IEquipmentSlotsApi` | Enable/config and requested extra slots | Sidecar save storage, `AgentEquipmentManager`/`AccessoriesBar` integration, stat application, shield-hit path | Experimental. This is not an official content/JSON expansion. Treat as equipment/accessory sidecar extension; extra slots are currently attribute-oriented and should stay GameBridge-owned until hot-disable/recovery/visual QA is complete. |

## Feature Notes

### Y-Key Console

The Y console is closest to a DTMAPI product/debug feature, not a general framework primitive. It has valuable user experience requirements: same-press open/close suppression, Escape close, focus handling, right-click behavior, save/title gating, and official enablement behavior. Those requirements belong in the feature smoke ledger because a simple "hotkey dispatched" log is not enough; the Canvas must visibly open and remain open past the opener key edge.

Boundary recommendation:

- Keep `IDebugConsoleApi` and related advanced debug APIs Diagnostic.
- Keep arbitrary native console/Lua/reflection out of public ordinary-mod APIs.
- If DTMAPI later needs SMAPI-like console commands, design a separate command registration helper with stable text-command semantics. Do not stabilize the current Y UI as that API.

### AutoFishing

AutoFishing has the healthiest separation among the gameplay automation mods: the ordinary mod owns UX and configuration, while GameBridge owns the native fishing stages. The current public options already describe native-stage policy instead of raw hook details, which is the right direction.

Remaining boundary risk:

- One effective owner is acceptable for first-party use, but insufficient for a general stable API.
- A future stable surface needs an exclusive lease or owner arbitration policy, clear failure/status results, save/title lifecycle reset, and evidence across the fifth-save fixture and long fishing sessions.

### ActionSpeed

ActionSpeed has a broad blast radius because it touches multiple native phases: tool animation, interaction timers, eating, item use, machine/harvest/plant paths, animal interaction, electric switches, resin, and other edge cases. The current mod/API split is directionally correct, but the surface should stay Experimental until the feature is decomposed enough to state what is actually supported.

Boundary recommendation:

- Keep fragile classification and animator/timer writes inside GameBridge.
- Prefer subfeature documentation and QA gates over one broad stable contract.
- Do not patch prompt/tip UI animation as part of the gameplay speed API.
- Treat repeated fertilizer/film failures, low-row interactions, animal connector edges, electric switches, and ready resin as promotion blockers until reverified.

### OneActionComplete

OneActionComplete is acceptable as a narrow "complete this proven native-owner operation after one normal interaction" policy, not as a general action-completion engine. Its boundary is especially important because `ToolCollider.HandleTools` can be shared with other behavior such as OilCoalDrop-like processing.

Boundary recommendation:

- Keep callback isolation/order and shared `ToolCollider` dispatch in GameBridge.
- Keep vegetation/resource exceptions explicit and documented.
- Do not expose raw collider/native-resource objects in Abstractions.

### MoreEquipmentSlots / Decoration Bar Extension

This feature should be named and explained carefully. The current evidence is not "the official decoration bar can be expanded by JSON"; it is a DTMAPI equipment/accessory sidecar extension that integrates with native equipment/stat paths. Existing notes also distinguish visible decoration equipment from extra attribute-oriented slots.

Boundary recommendation:

- Keep sidecar storage, UI clones, native stat recompute, and shield-hit behavior in GameBridge.
- Keep the ordinary mod as a slot request/config consumer.
- Do not promote until hot-disable visual cleanup, disabled/unsubscribed recovery, mail overflow/recovery, save transaction behavior, and visual QA are repeatable.
- Consider stabilizing read-only equipment query/status helpers earlier than mutating extra-slot registration, if a mod-author need emerges.

## Cross-Feature Conclusions

- First-party functional mods are valuable integration tests and product examples, but they should not define API stability by themselves.
- DTMAPI should first stabilize boring author surfaces: manifest, config, game loop/save/input events, logging, mod registry, config-menu integration, diagnostics reporting, Workshop/install state, and possibly a future command helper.
- Native gameplay convenience features should expose policy DTOs and result/status models, not Unity/Harmony/reflection/native objects.
- Global mutating features need owner tokens, priority, leases, or explicit "single effective owner" contracts before they can become stable ordinary-mod APIs.
- Workshop enablement, ordinary DTMAPI mod placement, and user-visible status/error reporting are part of the UX boundary, not afterthoughts.

## Recommended Next Actions

1. Clarify docs wording so Y-key console remains Diagnostic and separate from any future command-helper API.
2. Design owner lease/arbitration for `IFishingAutomationApi`; keep fifth-save and longer-session smoke evidence as promotion gates.
3. Break `IActionSpeedApi` documentation into subdomains and keep all pending manual QA cases as blockers.
4. Keep `IActionCompletionApi` narrow around resource/fuel/feed domains and document shared `ToolCollider` dispatch constraints.
5. For `IEquipmentSlotsApi`, prioritize hot-disable/unregister cleanup, disabled/unsubscribed recovery, sidecar transaction evidence, and visual QA before promotion.
6. Consider a stable read-only equipment/status API separately from mutating slot expansion.

## Non-Claims

- This review is documentation-only and did not run the game, build the solution, or perform live smoke tests.
- Hook presence, UI display, or a working local first-party mod does not by itself prove stable API readiness.
- SMAPI was used as a clean-room architecture and UX reference only; this review does not copy or imitate SMAPI implementation code.
