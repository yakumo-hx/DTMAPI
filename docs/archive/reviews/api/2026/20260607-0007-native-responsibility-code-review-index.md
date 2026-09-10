# 20260607-0007 DTMAPI native-responsibility code review index

Date: 2026-06-07
Status: complete manual code review
Scope boundary: docs-only review; no runtime/API/mod/game-file changes; no implementation goal created.

This review is the code-level continuation after:

- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`: family-level risk map.
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`: symbol and matrix navigation.

`0006` was used only to locate symbols and matrix coverage. The conclusions below come from manual reading of Abstractions declarations, Core service bodies, GameBridge/Harmony/reflection paths, hook-map/debug/update records, and local research notes. This round only records review findings. Serious problems are recorded as review debt; no fix goal is created unless the user explicitly asks later.

## Current completion state

This 0007 slice now covers the public API areas that currently carry the highest native-owner risk and adds coverage appendices:

- Custom entity contracts and runtime-blocked adapters.
- Machine, equipment slots, save slots, camera zoom, motor vehicle, and input suppression.
- Gameplay automation APIs, debug/Y-console APIs, content/workshop helpers, events, diagnostics, UI helpers, and config menu registry.
- A positive coverage appendix mapping all 82 rows in `docs/api/public-api-matrix.md` to 0007 review targets.
- A reverse coverage appendix mapping `src/DTMAPI.Abstractions` public type groups and high-risk DTO field groups to MatrixGap or review coverage.
- A public-member ledger recording the current Abstractions inventory: 234 public type/interface/enum/struct declarations, 1013 public property lines, and 6 public concrete/abstract method lines, with coverage classes by file and risk family.
- A completion audit concluding that a generated 1013-row low-risk property table would not add native-owner review value and would conflict with the "no template block" constraint. The review is complete for important API methods and high-risk public DTO/Result/EventArgs fields.

No implementation goal was created. Findings remain review/refactor backlog only.

## Volumes

| Volume | File | Status | Coverage |
| --- | --- | --- | --- |
| 01 | [high-risk-runtime-bridges](20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md) | reviewed | CustomEntity/Animal/Monster/Attack/Drone, MachineProduction, EquipmentSlots, SaveSlots, CameraZoom, MotorVehicle, Input.Suppress |
| 02 | [gameplay-debug-bridges](20260607-0007-native-responsibility-code-review/02-gameplay-debug-bridges.md) | reviewed | ActionCompletion, ActionSpeed, FishingAutomation, ItemTooltip, AnimalViewer, Inventory/Mail/Weather/Teleport/Save/Time/Movement/AdvancedDebug |
| 03 | [framework-content-ui](20260607-0007-native-responsibility-code-review/03-framework-content-ui.md) | reviewed | Config, ModRegistry, Workshop, Content, UI, Diagnostics, Events, Bootstrap/ConfigMenu host |
| 04 | [matrix-coverage](20260607-0007-native-responsibility-code-review/04-matrix-coverage.md) | reviewed | Positive coverage for all 82 `public-api-matrix` rows |
| 05 | [abstractions-reverse-coverage](20260607-0007-native-responsibility-code-review/05-abstractions-reverse-coverage.md) | reviewed | Reverse coverage by Abstractions public type group, MatrixGap ledger, and high-risk DTO semantic-risk ledger |
| 06 | [abstractions-public-member-ledger](20260607-0007-native-responsibility-code-review/06-abstractions-public-member-ledger.md) | reviewed | Public member inventory counts, file-level coverage classes, high-risk field symbols, and remaining granularity decision |
| 07 | [completion-audit](20260607-0007-native-responsibility-code-review/07-completion-audit.md) | reviewed | Requirement audit, final decision, and no-template-table rationale |

## Cross-cutting conclusions

1. `CustomEntity` 0.4.0 should be split in documentation and future code into stable definition/registry contracts plus experimental or blocked runtime adapters. The current stable marker is defensible only for definition storage, duplicate validation, owner cleanup, snapshots, and status reporting.
2. Most migrated gameplay APIs are not safe general-purpose platform APIs. They are DTMAPI-owned mods calling a narrow native path with smoke evidence, often backed by DTMAPI sidecar state or cloned UI.
3. Debug/Y-console APIs often call real native owners, but they remain `debug-only` because they mutate saves, singleton runtime state, time, inventory, weather, or spawned room objects in ways ordinary mods should not build product logic on.
4. Framework helpers are mostly ordinary-mod safe when they stay in DTMAPI-owned services. `Input.Suppress` is the notable exception: it stores intent but has no native consumer.
5. DTO names are a recurring semantic risk. Fields like `Succeeded`, `RuntimeStatus`, `Snapshot`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount`, `CurrentViewScale`, `ProductionCycleCount`, `SlotId`, and `IsRiding` can sound like native runtime truth even when they are DTMAPI registry/status/sidecar state.

## Decision table

| API / Symbol group | Result | Native owner | Ordinary mod usability | Recommendation |
| --- | --- | --- | --- | --- |
| `IInputHelper.Suppress(...)` | Gap | No native owner connected; Core `InputService.suppressed` only | 禁止依赖 | Rename or implement native action suppression. Ordinary mods relying on it still leak tool/item/menu actions into the game. |
| `ICustomAnimalApi.RegisterSpecies(...)` | Watch | DTMAPI `CustomEntityRegistryService` only | 普通 mod 可用 for definition registry only | Keep stable contract wording, but document that registration does not create native animals. |
| `ICustomAnimalApi.RequestSpawn(...)` | Blocked | Intended owner `AnimalManager.CreateAnimal` not connected | 禁止依赖 | Split stable contract from blocked runtime adapter. Ordinary mods get `runtime-creation-blocked`; no native animal, room/home/feed/produce/save state is created. |
| `ICustomAnimalApi.RequestRemove(...)` | Gap | DTMAPI runtime-handle dictionary only | 禁止依赖 | Do not promise native removal. Removing a DTMAPI handle cannot remove an animal from room/save/native managers. |
| `ICustomMonsterApi.RegisterMonster(...)` | Watch | DTMAPI registry only | 普通 mod 可用 for definition registry only | Keep as contract/metadata registration; document no native monster proto insertion. |
| `ICustomMonsterApi.RegisterSpawnTable(...)` | Gap | DTMAPI dictionary only | 禁止依赖 | Rename or isolate as DTMAPI spawn-rule metadata. Ordinary mods would think room spawn tables changed, but native dungeon spawn owns actual runtime. |
| `ICustomMonsterApi.RequestSpawn(...)` | Blocked | Intended native monster host/monster assets not connected | 禁止依赖 | Split runtime adapter. Ordinary mods get no monster object, no AI state, no loot/save lifecycle. |
| `ICustomAttackApi.SpawnProjectile(...)` | Blocked | Intended projectile/bullet native owner not connected | 禁止依赖 | Keep definition contract only. Ordinary mods get no projectile/hitbox/damage runtime. |
| `ICustomAttackApi.ExecuteAttack(...)` | Blocked | Intended attack execution native owner not connected | 禁止依赖 | Avoid stable wording for execution. Only DTMAPI lifecycle status changes. |
| `ICustomDroneApi.RequestSummon(...)` | Blocked | Intended companion/drone runtime owner not connected | 禁止依赖 | Split runtime adapter. Ordinary mods get no summoned object, no owner binding, no save/runtime AI. |
| `ICustomDroneApi.Equip(...)` / `SetMode(...)` | Gap | DTMAPI handle/status only | 禁止依赖 | Block until a summoned native drone exists. Ordinary mods would mutate no native equipment/mode state. |
| Custom entity DTO/result fields | Watch/Gap | Mostly DTMAPI registry/status | 禁止依赖 for runtime-implying fields | Add developer-doc warnings that `RuntimeStatus`, `Snapshot`, `Succeeded`, `FailureReason`, `ActiveRuntimeInstanceCount`, `SpawnRules`, `SummonPolicy`, and `AttackSlots` are not native runtime proof. |
| `IMachineProductionApi.RegisterMachine(...)` | Gap | Native tech/recipe table slice plus DTMAPI runtime loop | 仅 DTMAPI 自家 mod 可用 | Split native JSON/tech bridge from DTMAPI experimental production runtime. Ordinary mods risk output duplication/loss, room lifecycle desync, and shared tech-table pollution. |
| `IMachineProductionApi.GetState(...)` | Watch | DTMAPI `machineStates` | 仅 DTMAPI 自家 mod 可用 | Mark telemetry as DTMAPI runtime state, not native production truth. |
| `IEquipmentSlotsApi.RegisterSlots(...)` | Gap | DTMAPI sidecar storage/UI; native `AgentEquipmentFunction` only for stats | 仅 DTMAPI 自家 mod 可用 | Do not present as native extra slots. Ordinary mods risk sidecar save divergence, UI clone desync, and recovery edge cases. |
| `IEquipmentSlotsApi.EquipExtraSlot(...)` / `UnequipExtraSlot(...)` | Gap | Native backpack placement/cost plus DTMAPI slot storage | 仅 DTMAPI 自家 mod 可用 | Keep experimental. Ordinary mods can consume/recover items but cannot rely on a native slot owner. |
| `ISaveSlotsApi.RegisterSlots(...)` | Watch | `DolocAPI.gameManager.archiveFileCount` and official save UI | 普通 mod 可用 with caution | Keep experimental/watch. It uses the official save UI count, but all mods share a global singleton count and 60-slot clamp. |
| `ICameraZoomApi.Register(...)` / `SetViewScale(...)` / `ResetViewScale(...)` | Gap | `DolocAPI.mainCamera.orthographicSize` only | 仅 DTMAPI 自家 mod 可用 | Rebuild as full camera adapter before general public docs. Ordinary mods risk background/fog/parallax/room-bound mismatch. |
| `IMotorVehicleApi.UnlockOriginalMotor(...)` / `SummonOriginalMotor(...)` | Watch | `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition` | debug-only / watch | Native original motor path exists, but product mods should avoid singleton motor manipulation without lifecycle policy. |
| `IMotorVehicleApi.RegisterSecondMotor(...)` | Gap | DTMAPI cloned `MotorController`, private routing hooks | 仅 DTMAPI 自家 mod 可用 | Keep as DTMAPI mod-specific experimental API. Ordinary mods risk shared singleton pollution, clone residue, and cross-room ride desync. |
| `IMotorVehicleApi.RideVehicle(...)` | Gap | Original motor ride intentionally native-only; second motor hook path | 仅 DTMAPI 自家 mod 可用 | Document original ride cannot be programmatically forced; custom ride depends on DTMAPI clone hooks. |
| `IActionCompletionApi.Configure(...)` | Watch | Tool/resource/fuel/feed hooks | 仅 DTMAPI 自家 mod 可用 | Keep experimental. Ordinary mods risk wrong-tool, energy, vegetation exception, and resource lifecycle mismatch outside smoked paths. |
| `IActionSpeedApi.Configure(...)` | Watch | Agent state body speed hooks | 仅 DTMAPI 自家 mod 可用 | Keep experimental. Ordinary mods should not compose multiple speed policies without owner arbitration. |
| `IFishingAutomationApi.Configure(...)` / `SetEnabled(...)` | Watch | Fishing state/minigame hooks plus DTMAPI state | 仅 DTMAPI 自家 mod 可用 | Keep experimental. Ordinary mods risk phase/input desync and movement cancellation conflicts. |
| `IItemTooltipApi.ConfigureFishRoeProvider(...)` | Watch | Item title/description/detail postfixes | 普通 mod 可用 with caution | Usable as display-only policy. Must state it does not create item data or fish roe runtime. |
| `IAnimalViewerApi.ConfigureSpecialProduceProgress(...)` | Watch | Animal viewer UI hooks | 普通 mod 可用 with caution | Display-only. Must not imply hidden-produce native data is changed. |
| `IInventoryDebugApi.GiveItem(...)` | OK/Watch | `DolocAPI.QueryItemProto`, `CanPlaceItem`, `TryPlaceInBackpack`, `CountItem` | debug-only | Native placement is real, but ordinary mods should not use a debug grant path for game economy logic. |
| `IMailDeliveryApi.SendItemMail(...)` | Watch | `DolocAPI.SendItemAsEmail`, `EmailManager.emails` | debug-only | Native mail is used, but it bypasses normal content/progression intent; keep out of ordinary mod docs. |
| `IWeatherDebugApi.SetWeather(...)` | OK/Watch | `ArchiveDataHandle.SetWeather` / `PatchWeather` | debug-only | Native owner is real, but save/world state mutation is debug-only. |
| `ITeleportDebugApi.Teleport(...)` | OK/Watch | `DolocAPI.DoTransport` on whitelisted mark points | debug-only | Native request is real; ordinary mods should not depend on debug whitelist transport. |
| `IInstantSaveDebugApi.Save(...)` | Watch | `DolocAPI.SaveGame` | debug-only | Save-only path is allowed; reload path blocked due scene residue. |
| `ITimeDebugApi.SkipToNextWeatherPeriod(...)` | Watch | `ArchiveDataHandle.PassTimeNoControl` + `DolocAPI.OnWakeUp` | debug-only | Real native pass-time path, but can advance crops/weather/machines globally; not ordinary API. |
| `IMovementDebugApi.SetSpeedMultiplier(...)` | Watch | `MotionAbility.SetMoveScaler` | debug-only | Real player motion owner, but global player state mutation; no ordinary dependency. |
| `IAdvancedDebugApi` methods | Watch/Gap | Mixed native commands, save fields, room hosts, GameInitConfig flags | debug-only | Keep strictly whitelisted. Risk varies from save pollution to room object residue and creative singleton flag leakage. |
| `IWorkshopHelper` | OK | DTMAPI discovery index; official enablement remains Doloc/Steam-owned | 普通 mod 可用 | Keep read-only. Do not add toggle promises. |
| `IContentQueryHelper` | OK/Watch | DTMAPI file/index scanner, not native table owner | 普通 mod 可用 for read-only discovery | Document that indexed content is source metadata; runtime availability still depends on native tables and official enablement. |
| `IConfigHelper` | OK | DTMAPI per-mod config files | 普通 mod 可用 | Keep stable. No native owner expected. |
| `IEventsHelper` game/save/workshop events | Watch | BepInEx Update plus Harmony save/workshop hooks | 普通 mod 可用 with event-specific caveats | Keep stable/watch; save events depend on patched native lifecycle and can be pending before hooks install. |
| `IUiHelper` overlay/menu APIs | OK/Watch | DTMAPI overlay host | 普通 mod 可用 | Keep stable for DTMAPI UI only; no native menu ownership implied. |
| `IDiagnosticsHelper` | OK | DTMAPI diagnostics/log export | 普通 mod 可用 | Keep stable for DTMAPI diagnostics. |
| `IDtmConfigMenuApi` / ConfigMenu registry | OK/Watch | DTMAPI title/config UI host | 普通 mod 可用 | Keep stable as DTMAPI config UI, not native options menu integration. |

## Evidence index

The review blocks cite these evidence families:

- Matrix: `docs/api/public-api-matrix.md`
- Prior API review: `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- Symbol/navigation audit: `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Update records: `docs/updates/2026/20260603-0014-024-new-content-api-partial.md`, `20260603-0015`, `20260603-0019`, `20260603-0021`, `20260606-0004`, `20260606-0006`, `20260606-0008`, `20260606-0009`, `20260606-0010`, `20260606-0011`, `20260606-0014`
- Research notes: `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`, `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`, `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`, `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`

## Final Audit

The completion audit is recorded in [07-completion-audit](20260607-0007-native-responsibility-code-review/07-completion-audit.md). It concludes that 0007 is complete for the active review goal and that a literal 1013-row low-risk property table should not be generated as a substitute for manual native-owner review.
