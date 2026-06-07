# 07 - All API Risk Closure Table

## Scope

This volume integrates the code-level closure from:

- `20260607-0008-native-owner-special-audits`
- `20260607-0009-native-owner-special-audits`
- `20260607-0010-native-owner-remaining-api-audit`

It is not a new coverage sweep and does not repeat the 0007/0006 symbol inventory. Its purpose is to make a compact decision table for public API status, ordinary-mod usability, and rebuild/degrade recommendations.

## Files read

- `docs/api/public-api-matrix.md`: lines 9-92.
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`: lines 15-33.
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`: lines 13-38.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/01-framework-gameloop-event-input.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/02-config-localization-logging-registry.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/03-ui-diagnostics-report.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/04-migrated-gameplay-apis.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/05-debug-yconsole-remaining-apis.md`.
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md`.
- `references/doloc-town/research-notes/README-DolocTown-Modding-API.md`.
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`.
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Action_Interaction.md`.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Fishing.md`.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/UI.md`.

## Functions read

This integration volume does not add new function-body reads beyond the six 0010 volumes and imported 0008/0009 special audits. The function-level inputs are those listed in:

- `01-framework-gameloop-event-input.md`: runtime events, input sampler, and title-boundary functions.
- `02-config-localization-logging-registry.md`: config, ConfigMenu, translation, logging, and ModRegistry functions.
- `03-ui-diagnostics-report.md`: UI runtime and diagnostics report functions.
- `04-migrated-gameplay-apis.md`: ActionCompletion, ActionSpeed, Fishing, Tooltip, and AnimalViewer hook functions.
- `05-debug-yconsole-remaining-apis.md`: DebugConsole, Mail, Weather, Movement, and AdvancedDebug functions.
- `06-030-chest-strongplanting-apis.md`: ChestLocatorEnhancer and StrongPlantingGun hook functions.
- 0008/0009 indexes and volumes for Input.Suppress, CameraZoom, MachineProduction, CustomEntity runtime verbs, Save/Time/Teleport/Inventory/EquipmentSlots/Vehicle/Workshop/Content.

## Call graph

```text
Public API matrix
  rows covered by 0008 -> high-risk runtime bridges
  rows covered by 0009 -> save/time/teleport/inventory/equipment/vehicle/workshop/content
  remaining rows covered by 0010 -> framework/config/ui/gameplay/debug/0.3.0 features

Decision model
  If native owner is not needed and DTMAPI owns contract cleanly -> stable/experimental open
  If native owner is reached but API mutates debug/progression/global state -> debug-only
  If only registry/status is safe -> registry-only
  If native owner is partial and sidecar owns runtime semantics -> experimental internal
  If public wording promises runtime behavior that is not connected -> downgrade/rebuild
```

## Function body findings

- Stable/open infrastructure APIs are DTMAPI-owned by design: config, logging, diagnostics snapshots, and registry lookups do not need a native owner, but their docs must avoid implying official Doloc Town state ownership.
- The strongest ordinary-mod APIs are read-only or display-only surfaces: content/workshop source index, DTMAPI UI/report requests, fish-roe tooltip decoration, and animal viewer display rows.
- Debug APIs often have real native calls, but that does not make them ordinary-mod APIs. Time, teleport, inventory give, instant save, weather, movement, mail, and advanced debug mutate or bypass player/game state for console workflows.
- The highest-risk gameplay adapters are those with sidecar runtime ownership: MachineProduction, EquipmentSlots, Vehicle/SecondMotor, CameraZoom, ActionSpeed, FishingAutomation, ChestLocator, and StrongPlantingGun.
- Custom entity runtime verbs are the sharpest semantic mismatch: contracts are stable registry DTOs, but spawn/summon/execute/equip/mode runtime creation remains explicitly blocked until native adapters exist.

## Native owner verdict

| Risk rank | API/domain | Verdict | Why |
| --- | --- | --- | --- |
| 1 | Custom entity runtime verbs | Blocked | Native animal/monster/bullet/drone owners are not connected; verbs return blocked. |
| 2 | `IInputHelper.Suppress` | Blocked | Suppressed set has no sampler/native consumer. |
| 3 | `ICameraZoomApi` | Gap | Only orthographic size is reached; background/fog/room/parallax/input owners missing. |
| 4 | `IMachineProductionApi` | Gap | Native table/electric slices exist, but production scheduler/state is DTMAPI sidecar. |
| 5 | `IEquipmentSlotsApi` | Gap | Native stats/UI slices exist, but slot storage/UI/recovery semantics are DTMAPI sidecar. |
| 6 | `IMotorVehicleApi` second motor | Gap | DTMAPI clone/routing around singleton native motor model. |
| 7 | `IAdvancedDebugApi` | Reached/debug-only | Native calls and broad bypass hooks exist; ordinary-mod exposure would pollute progression/state. |
| 8 | `IStrongPlantingGunApi` | Partial/internal | Native gun hooks reached; DTMAPI owns storage expansion and transfer interception. |
| 9 | `IChestLocatorEnhancerApi` | Partial/internal | Native inventory enumeration reached; DTMAPI owns cross-room traversal and policy arbitration. |
| 10 | `IActionSpeedApi` / `IFishingAutomationApi` / `IActionCompletionApi` | Partial/internal | Native slices reached; general stable owner boundaries not complete. |
| 11 | Debug Save/Time/Teleport/Inventory/Weather/Movement/Mail | Reached/debug-only | Native debug paths reached, but not stable ordinary gameplay contracts. |
| 12 | Framework/config/ui/read-only/display-only APIs | OK/Watch | DTMAPI owns contract or API is display/read-only. |

## Ordinary mod usability

| Classification | APIs/domains |
| --- | --- |
| `stable 可开放` | Config read/write/path, logging monitor, diagnostics error/hook/log export, DTMAPI `GameLaunched`, ModRegistry as DTMAPI registry-only lookup. |
| `experimental 可开放` | Update/one-second/returned-to-title events, input hotkeys except `Suppress`, ConfigMenu, localization, DTMAPI UI pages, workshop/content read-only index, fish-roe tooltip display, animal viewer display. |
| `debug-only` | Inventory give, instant save, time skip/advance/time scale, teleport, weather, movement, mail item delivery, advanced debug, original motor summon/debug slices. |
| `registry-only` | Custom entity stable definition/registration/status contracts; ModRegistry semantics; content indexed item metadata when runtime-loaded is false or unknown. |
| `experimental 可内用 / 仅 DTMAPI 自家 mod 可用` | ActionCompletion, ActionSpeed, FishingAutomation, ChestLocatorEnhancer, StrongPlantingGun, SaveSlots, MachineProduction current runtime, EquipmentSlots extra slots, Vehicle second motor. |
| `必须降级或重做` | `IInputHelper.Suppress`, CameraZoom runtime, MachineProduction runtime loop, EquipmentSlots storage/UI, Vehicle second motor, custom entity runtime verbs. |

## Concrete failure modes

1. **Stored state without native consumer**: `IInputHelper.Suppress` and custom entity runtime handles can appear successful as DTMAPI state while native gameplay still proceeds or no native entity exists.
2. **Only UI/debug succeeds**: Y-console/debug APIs can display or execute debug commands, but ordinary mod dependency can bypass progression, save unexpected state, or produce user-visible side effects.
3. **Shared singleton pollution**: Vehicle second motor, movement speed, camera zoom, and advanced creative/time-scale hooks can mutate singleton/global native state without owner stacking.
4. **Sidecar runtime drift**: Machine production, equipment slots, ActionSpeed, and FishingAutomation store DTMAPI policy/runtime state that can drift across save/load, title return, scene transitions, or multi-mod ownership.
5. **Semantic overclaim in DTOs**: State fields such as `RuntimeStatus`, `Handle`, `Snapshot`, `HookInstalled`, `LastAppendedInventoryCount`, and `ExpandedGunCount` can be misread as native runtime ownership unless docs explicitly say registry/status/telemetry.

## Minimal rebuild direction

1. **Document now**: ordinary-mod docs should expose only stable/open, experimental-open, debug-only, and registry-only categories. Do not list internal/rebuild APIs as general developer examples.
2. **Downgrade wording**: mark `Suppress`, CameraZoom runtime, MachineProduction runtime, EquipmentSlots extra-slot runtime, Vehicle second motor, and CustomEntity runtime verbs as blocked/rebuild in developer-facing docs until native-owner adapters are redesigned.
3. **Split contracts**: separate stable DTO/registration contracts from experimental runtime adapters for CustomEntity, MachineProduction, EquipmentSlots, Vehicle, and CameraZoom.
4. **Add owner tokens**: for global mutable features such as movement speed, time scale, ActionSpeed, ChestLocator, StrongPlantingGun, and camera zoom, require owner tokens, stacking/priority, and restore semantics before ordinary-mod stabilization.
5. **Prefer read-only first**: future public APIs should start with read-only snapshots/events, then add mutating verbs only after native owner, save/load, scene transition, and multi-mod behavior are proven.

## Evidence gaps

- This pass did not run game smoke; it only read existing code, hook-map, update, and smoke records.
- Several high-risk domains need future decompiled/native owner research before implementation goals: CameraController/background/fog/room rendering, machine native production scheduler, equipment slot save/UI model, vehicle multi-instance model, custom animal/monster/bullet/drone runtime owners.
- Multi-mod arbitration remains untested for ActionSpeed, FishingAutomation, ChestLocator, StrongPlantingGun, Tooltip providers, AnimalViewer providers, and global debug/movement/camera-like modifiers.
- DTO semantic wording remains a docs risk: public names that imply runtime creation or native hook success must be annotated before these APIs are recommended to ordinary mods.
