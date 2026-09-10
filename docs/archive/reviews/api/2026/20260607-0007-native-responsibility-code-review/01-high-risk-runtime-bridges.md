# 01 high-risk runtime bridges

Date: 2026-06-07
Status: manual code review slice complete for this volume

This volume reviews the API surfaces most likely to over-promise native Doloc Town runtime ownership: custom entities, machine production, equipment slots, save slots, camera zoom, motor vehicles, and input suppression.

## Top Risks

1. `ICustomAnimalApi.RequestSpawn`, `ICustomMonsterApi.RequestSpawn`, `ICustomAttackApi.SpawnProjectile`, `ICustomAttackApi.ExecuteAttack`, and `ICustomDroneApi.RequestSummon` expose runtime verbs while the implementation returns `runtime-creation-blocked`. Ordinary mods get no native animal, monster, projectile, attack execution, or drone object.
2. Custom entity DTO fields such as `RuntimeStatus`, `Snapshot`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount`, `SpawnRules`, `AttackSlots`, `SummonPolicy`, `Succeeded`, and `FailureReason` can sound like native runtime truth, but current state is mostly DTMAPI registry/status.
3. `IMachineProductionApi.RegisterMachine` patches native recipe/tech table slices but schedules production through a DTMAPI runtime loop. Ordinary mods can cause output duplication/loss, pass-time divergence, shared table pollution, or cross-room state drift.
4. `IEquipmentSlotsApi` consumes/recovers native inventory items and applies native attribute functions, but slot ownership/storage/UI are DTMAPI sidecars. Ordinary mods can see only UI/stat success while native equipment slots never exist.
5. `ICameraZoomApi` writes `Camera.orthographicSize` only. It does not own `CameraController` room range, background scale, depth fog, parallax, or resolution refresh; ordinary mods risk visual and input/camera-bound mismatch.
6. `IMotorVehicleApi.RegisterSecondMotor` creates a DTMAPI clone path around the game's singleton original motor flow. Ordinary mods risk shared singleton pollution, clone residue, original/second-motor state mismatch, and cross-room riding desync.
7. `IInputHelper.Suppress` has no GameBridge/native consumer. Ordinary mods relying on it still leak Doloc native tool/item/menu actions.
8. `ISaveSlotsApi.RegisterSlots` does reach the official save-slot count, but the count is global and shared. Multi-mod composition and extreme slot counts are still watch items.

## Native Owner Map

| API group | Current native owner reached | Current DTMAPI owner | Native ownership verdict |
| --- | --- | --- | --- |
| Custom entity definitions | None required for registry-only contract | `CustomEntityRegistryService` dictionaries | OK for definitions only |
| Custom animal spawn/remove | Intended `AnimalManager.CreateAnimal` and animal lifecycle, not connected | `CustomEntityRegistryService` request/status events | Blocked runtime |
| Custom monster spawn/spawn tables | Intended monster assets/room host, not connected | DTMAPI definition/spawn-table dictionaries | Blocked runtime |
| Custom attack/projectile execution | Intended native projectile/attack/damage owner, not connected | DTMAPI request/status events | Blocked runtime |
| Custom drone summon/equip/mode | Intended companion/drone runtime owner, not connected | DTMAPI request/status events | Blocked runtime |
| Machine recipe/tech | `DolocConfig.Tables.TbRecipe`, `DolocAPI.assets.techTrees`, `TbTechNode` | DTMAPI registration | Native table slice only |
| Machine production loop | Optional official electric component check and output placement | DTMAPI `MachineRuntimeEntry`, `machineStates` | Hybrid, not native production owner |
| Equipment slots | `DolocAPI.CostItem`, `TryPlaceInBackpack`, `AgentEquipmentFunction`, `AgentEquipmentManager.ReloadParams` | DTMAPI extra-slot storage and cloned UI strip | Native stats/inventory slice only |
| Save slots | `DolocAPI.gameManager.archiveFileCount`, official save UI refresh | DTMAPI max-policy registry | Native count owner reached |
| Camera zoom | `DolocAPI.mainCamera.orthographicSize` / Unity `Camera.orthographicSize` | DTMAPI owner scale map | Partial camera slice only |
| Original motor | `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, `AgentControllerState.GetOffMotor` | DTMAPI wrapper state/status | Native original motor path reached |
| Second motor | Private `MotorController` clone/routing hooks | DTMAPI clone/runtime maps | DTMAPI-owned experimental runtime |
| Input suppression | None | Core `InputService.suppressed` hash set | Not connected |

## Ordinary Mod Usability Table

| API group | Usability | Why |
| --- | --- | --- |
| Custom entity definition registration | 普通 mod 可用 for contract metadata | It is DTMAPI-owned, deterministic, and does not claim native runtime creation when documented narrowly. |
| Custom entity runtime verbs | 禁止依赖 | They return blocked or mutate only DTMAPI status. No native object/save/room/UI/AI owner accepts the request. |
| Machine production | 仅 DTMAPI 自家 mod 可用 | It is tuned for the current Mine/Oil packages and relies on DTMAPI polling/sidecar state plus table patches. |
| Equipment slots | 仅 DTMAPI 自家 mod 可用 | It is a sidecar UI/storage/stat bridge, not a native slot contract. |
| Save slots | 普通 mod 可用 with caution | It writes the official count, but all mods share the same global archive count policy. |
| Camera zoom | 仅 DTMAPI 自家 mod 可用 | Full camera/background/depth owner is not connected. |
| Original motor debug helpers | debug-only / watch | Native path is real, but public gameplay mods should not drive singleton vehicle state without stricter lifecycle. |
| Second motor | 仅 DTMAPI 自家 mod 可用 | It depends on clone/routing hooks and DTMAPI cleanup policy. |
| `Input.Suppress` | 禁止依赖 | No native input path reads it. |

## Review Blocks

### `DTMAPI.Abstractions.IInputHelper.Suppress(string)`

- Symbol: `DTMAPI.Abstractions.IInputHelper.Suppress(string button)`
- Current marker: stable in helper surface
- Review advice: 降级 / Rename unless native suppression is implemented
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:153`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:408`
- Function body summary: Adds `button` into the Core `InputService.suppressed` set. `GetSuppressedButtons()` returns the set, but the set is not consumed by `DtmApiRuntime.RecordInputPressed`, `RecordInputReleased`, the bootstrap polling loop, or GameBridge gameplay input hooks.
- GameBridge/Harmony/reflection path: No path for this symbol. The only proven native suppression is separate debug-console logic in `DolocTownHookCallbacks.AgentControllerStateUseToolPrefix`, `AgentControllerStateUseItemPrefix`, and `AgentControllerStateEnterUiCheckPrefix`.
- Native owner: None connected for general mod input suppression. Candidate owners searched: bootstrap `ReflectedUnityInput`, Core `DtmApiRuntime` input methods, GameBridge `DolocTownHookCallbacks`, and `GetSuppressedButtons` references.
- Native state holder: None. DTMAPI state holder is `InputService.suppressed`.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI-only intent record.
- Evidence:
  - public-api-matrix entry: helper/input surface in `docs/api/public-api-matrix.md`
  - update record: No direct evidence for this method as native suppression.
  - smoke path: No direct evidence for ordinary mod suppression; debug console input isolation evidence is separate.
  - hook-map entry: `UI.DebugConsoleInputIsolation` is separate and only guards Y console.
  - code path: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:373`, `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:138`
- Result: Gap
- Recommendation: Ordinary mods cannot use this to suppress Doloc Town actions now because native tool/item/menu input still fires. The concrete failure mode is input leakage: a mod can mark a button suppressed and still trigger `UseTool`, `UseItem`, backpack/menu entry, or other AgentControllerState actions. Either wire this through a native input/action owner or rename it as DTMAPI-local suppression metadata and remove ordinary-mod docs.

### `DTMAPI.Abstractions.ICustomAnimalApi.RegisterSpecies(...)`

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用 for definition registry only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:318`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60`
- Function body summary: Validates owner/species id, rejects duplicate definitions, stores `CustomAnimalSpeciesDefinition` in a DTMAPI dictionary, invokes provider `OnRegistered`, raises a DTMAPI lifecycle event, and returns a successful request result. It does not call a native animal factory or mutate native animal tables.
- GameBridge/Harmony/reflection path: `DolocTownGameBridge.RegisterExperimentalApis` registers Core `runtime.CustomEntities` as `ICustomAnimalApi`; `PublishStableCustomEntityHookStatuses` reports the stable registry path as verified and the runtime adapter as configured-blocked.
- Native owner: No native owner required for metadata registration. Runtime candidate owner is `AnimalManager.CreateAnimal`, but this method does not reach it.
- Native state holder: None. DTMAPI state holder is `CustomEntityRegistryService.animalDefinitions`.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI registry success only.
- Evidence:
  - public-api-matrix entry: custom entity 0.4.0 stable custom animal API.
  - update record: `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`
  - smoke path: `docs/debug/regressions/smoke-matrix.md` custom entity smoke row around `GAME-SMOKE/20260606-191219`
  - hook-map entry: `docs/hook-map/README.md` custom entity core registry stable / runtime adapter blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:263`
- Result: Watch
- Recommendation: Keep a stable registry/definition contract, but developer docs must state that success means DTMAPI accepted the definition, not that Doloc Town can spawn or save the species. Without that wording, ordinary mods can misread `Succeeded=true` as native animal availability.

### `DTMAPI.Abstractions.ICustomAnimalApi.RequestSpawn(...)`

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:321`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101`
- Function body summary: Validates request/definition ownership, raises a DTMAPI lifecycle event with blocked runtime status, and returns a failed `CustomEntityRequestResult` with failure reason `runtime-creation-blocked`. It creates no native animal.
- GameBridge/Harmony/reflection path: Registered through `DolocTownGameBridge.RegisterExperimentalApis`; GameBridge publishes `CustomAnimals.StableApi` as configured-blocked with details about missing AnimalManager/proto/room/home/feed/excrement/breeding/produce/save adapters.
- Native owner: Intended `AnimalManager.CreateAnimal` and animal lifecycle managers; not connected.
- Native state holder: Intended animal room/home data, food/produce/breeding/save state; currently no native state is created. DTMAPI status comes from request/lifecycle result.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only; native runtime rejects by absence because it is never called.
- Evidence:
  - public-api-matrix entry: custom animal spawn runtime method.
  - update record: `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`
  - smoke path: custom entity smoke confirms blocked runtime request results, not spawn success.
  - hook-map entry: custom animal runtime adapter configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:268`
- Result: Blocked
- Recommendation: Ordinary mods cannot use this now because no animal is created in the room, no animal is attached to home/food/produce/breeding systems, and no save record exists. The API should keep the stable request/result shape only if docs split it from a blocked runtime adapter and require callers to handle `runtime-creation-blocked`.

### `DTMAPI.Abstractions.ICustomAnimalApi.RequestRemove(...)`

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RequestRemove(IManifest owner, string runtimeId, string reason)`
- Current marker: stable
- Review advice: 降级 runtime wording
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:322`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:116`
- Function body summary: Delegates to `RemoveRuntimeHandle`, which removes a DTMAPI runtime handle from dictionaries and emits DTMAPI lifecycle status. It cannot remove native animals because spawn never created one and no native handle is stored.
- GameBridge/Harmony/reflection path: No native removal path.
- Native owner: Intended animal manager/room animal list/save data; not connected.
- Native state holder: DTMAPI handle dictionary only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom animal remove method.
  - update record: `20260606-0014`
  - smoke path: No direct evidence of native removal; blocked runtime smoke only.
  - hook-map entry: custom animal runtime adapter configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:116`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:457`
- Result: Gap
- Recommendation: Ordinary mods cannot use this as native removal. The failure mode is stale native state if a future partial spawn path creates an object but removal still only clears DTMAPI dictionaries. Keep it blocked until native room/save owner deletion is implemented and smoke-tested.

### `DTMAPI.Abstractions.ICustomMonsterApi.RegisterMonster(...)` and `RegisterSpawnTable(...)`

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RegisterMonster(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RegisterSpawnTable(...)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: `RegisterMonster` 普通 mod 可用 for metadata only; `RegisterSpawnTable` 禁止依赖 for native spawning
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:334`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:158`
- Function body summary: `RegisterMonster` validates and stores the definition in a DTMAPI dictionary. `RegisterSpawnTable` validates and stores spawn-table metadata in another DTMAPI dictionary. Neither inserts native monster protos, room spawn tables, AI, loot, or save handlers.
- GameBridge/Harmony/reflection path: Registered through `DolocTownGameBridge.RegisterExperimentalApis`; GameBridge reports custom monster runtime adapter configured-blocked.
- Native owner: Intended native monster assets and room monster host; not connected.
- Native state holder: DTMAPI monster/spawn-table dictionaries.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI registry/status only.
- Evidence:
  - public-api-matrix entry: custom monster registration/spawn table rows.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates registration/status and blocked runtime.
  - hook-map entry: custom monster runtime adapter configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:158`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:273`
- Result: Watch for definition, Gap for spawn-table runtime semantics
- Recommendation: Ordinary mods can store monster definitions as DTMAPI metadata, but cannot rely on `RegisterSpawnTable` to affect Doloc Town room spawning. The concrete risk is author-visible configuration success with no native monster generation, no AI, no loot, and no per-room count enforcement.

### `DTMAPI.Abstractions.ICustomMonsterApi.RequestSpawn(...)`

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:338`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:198`
- Function body summary: Validates request/definition ownership and immediately returns a blocked request result with `runtime-creation-blocked`. It does not call room monster host creation or native monster asset lookup.
- GameBridge/Harmony/reflection path: GameBridge status says configured-blocked.
- Native owner: Intended current-room monster host and native monster assets; not connected.
- Native state holder: None native. DTMAPI request result/status only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom monster spawn row.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates blocked result.
  - hook-map entry: custom monster runtime adapter configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:198`
- Result: Blocked
- Recommendation: Ordinary mods cannot use this because it will not create a native monster, AI state, loot state, room membership, or save entry. Keep runtime method documented as blocked until a native room-host adapter exists.

### `DTMAPI.Abstractions.ICustomAttackApi.SpawnProjectile(...)` and `ExecuteAttack(...)`

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.ExecuteAttack(IManifest owner, CustomAttackExecutionRequest request)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:351`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:282`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:476`
- Function body summary: Both runtime verbs delegate to `RequestAttack`, which validates the definition/owner, raises a DTMAPI lifecycle event with blocked status, and returns `runtime-creation-blocked`. No projectile, hitbox, damage, homing, pierce, bounce, or lifetime owner is called.
- GameBridge/Harmony/reflection path: GameBridge reports custom projectile API configured-blocked.
- Native owner: Intended native projectile/attack/damage runtime owner; not connected.
- Native state holder: None native. DTMAPI request result/status only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom attack/projectile rows.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates blocked results.
  - hook-map entry: custom attack projectile API configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:476`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:278`
- Result: Blocked
- Recommendation: Ordinary mods cannot use these now because no native projectile is spawned and no damage/hitbox/collision system recognizes the request. The DTO names `SpawnProjectile` and `ExecuteAttack` are semantic over-promises unless docs say the runtime adapter is blocked.

### `DTMAPI.Abstractions.ICustomDroneApi.RequestSummon(...)`, `Equip(...)`, and `SetMode(...)`

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RequestSummon(IManifest owner, CustomDroneSummonRequest request)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.Equip(IManifest owner, string runtimeId, CustomDroneEquipmentRequest request)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.SetMode(IManifest owner, string runtimeId, string modeId, string reason)`
- Current marker: stable
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:368`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390`
- Function body summary: `RequestSummon` validates and returns blocked status. `Equip` and `SetMode` call DTMAPI handle mutation helpers and map missing runtime handles to blocked status. No native companion/drone object, owner binding, equipment inventory, AI, energy, repair, or mode controller is called.
- GameBridge/Harmony/reflection path: GameBridge reports custom drone runtime configured-blocked.
- Native owner: Intended custom companion/drone runtime owner; unknown/not connected.
- Native state holder: DTMAPI definitions/handles only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom drone rows.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates registration/status and blocked runtime.
  - hook-map entry: custom drone stable API configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:283`
- Result: Blocked for summon; Gap for equip/mode semantics
- Recommendation: Ordinary mods cannot use these now because no native drone exists to own mode/equipment/energy/pathing/save state. If a mod calls `Equip` or `SetMode`, it can only mutate or fail DTMAPI status around a nonexistent runtime handle, which creates false UI/status confidence.

### Custom entity DTO and result semantics

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus`
- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus`
- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot`
- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult`
- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition`
- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition`
- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition`
- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition`
- Current marker: stable / experimental mix by matrix row
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用 for metadata fields; 禁止依赖 for runtime-implying fields
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:21`, `src/DTMAPI.Abstractions/CustomEntities.cs:188`, `src/DTMAPI.Abstractions/CustomEntities.cs:199`, `src/DTMAPI.Abstractions/CustomEntities.cs:245`, `src/DTMAPI.Abstractions/CustomEntities.cs:395`, `src/DTMAPI.Abstractions/CustomEntities.cs:554`, `src/DTMAPI.Abstractions/CustomEntities.cs:695`, `src/DTMAPI.Abstractions/CustomEntities.cs:809`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611`
- Function body summary: Snapshot/status builders count DTMAPI dictionaries and return `ConfiguredNoRuntimeInstance` or `RuntimeCreationBlocked` when no runtime handles exist. `SaveStateRecordCount` is hard-coded to `0`; `PruneOrphanedSaveState` returns `0`.
- GameBridge/Harmony/reflection path: GameBridge status publication separates stable registry from blocked runtime adapters.
- Native owner: None for status/snapshot. Native runtime owners are intentionally not connected.
- Native state holder: DTMAPI dictionaries; no save-state owner for custom entities.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: DTO/Result field rows included in 0006 symbol audit.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates snapshots/status and blocked results.
  - hook-map entry: custom entity core registry stable, runtime blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:471`
- Result: Watch / Gap by field
- Recommendation: Add explicit developer-doc language: `Succeeded`, `FailureReason`, `RuntimeStatus`, `Snapshot`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount`, `SpawnRules`, `AttackSlots`, `SummonPolicy`, and similar fields report DTMAPI registry/status unless a future adapter says otherwise. Ordinary mods that treat these fields as native runtime proof will mis-handle missing spawns, missing save state, missing room membership, and missing combat/drone behavior.

### Custom entity unregister and registry getter methods

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.UnregisterSpecies(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinitions(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinition(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.UnregisterMonster(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinitions(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinition(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.RegisterAttack(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.UnregisterAttack(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinitions(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinition(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RegisterDrone(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.UnregisterDrone(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinitions(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinition(...)`
- Current marker: stable family rows
- Review advice: 保持 for DTMAPI registry contracts; keep runtime wording split
- Ordinary mod usability: 普通 mod 可用 for registry metadata only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:319`, `src/DTMAPI.Abstractions/CustomEntities.cs:335`, `src/DTMAPI.Abstractions/CustomEntities.cs:352`, `src/DTMAPI.Abstractions/CustomEntities.cs:369`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:80`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:89`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:177`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:186`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:257`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:266`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:330`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:339`
- Function body summary: Registration methods validate IDs, reject duplicates, store definitions in DTMAPI dictionaries, invoke `Provider.OnRegistered`, raise DTMAPI lifecycle events, and return registration result DTOs. Unregister methods remove DTMAPI definitions and associated DTMAPI instance snapshots, invoke `Provider.OnUnregistered`, and raise DTMAPI lifecycle events. Getter methods return arrays or single definitions from DTMAPI dictionaries filtered by owner.
- GameBridge/Harmony/reflection path: Registered through `DolocTownGameBridge.RegisterExperimentalApis`; no Harmony/reflection/native creation path is used for these registry getters.
- Native owner: None required for DTMAPI metadata registration. They do not insert into native animal/monster/attack/drone tables or managers.
- Native state holder: DTMAPI definition dictionaries and DTMAPI lifecycle events.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI registry only.
- Evidence:
  - public-api-matrix entry: custom entity family rows 38-41.
  - update record: `20260606-0014`
  - smoke path: `Smoke.CustomEntityApis=verified` registration/duplicate/cleanup evidence.
  - hook-map entry: custom entity core registry stable.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:80`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310`
- Result: OK/Watch
- Recommendation: Keep these as stable DTMAPI registry contracts. Ordinary mods can use them to register and inspect definitions, but docs must say they do not make native runtime classes recognize the animal/monster/attack/drone.

### Custom entity instance getter, snapshot, status, and lifecycle events

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstances(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstance(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSnapshot(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetStatus(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.LifecycleChanged`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstances(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstance(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetSnapshot(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetStatus(...)`
- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.LifecycleChanged`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetActiveAttacks(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackInstance(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetSnapshot(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetStatus(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.LifecycleChanged`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstances(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstance(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetSnapshot(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetStatus(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.LifecycleChanged`
- Current marker: stable family rows
- Review advice: 保持 for DTMAPI registry/status; downgrade runtime implication in docs
- Ordinary mod usability: 普通 mod 可用 for DTMAPI status; 禁止依赖 as native runtime state
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:319`, `src/DTMAPI.Abstractions/CustomEntities.cs:335`, `src/DTMAPI.Abstractions/CustomEntities.cs:352`, `src/DTMAPI.Abstractions/CustomEntities.cs:369`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:121`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:133`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:227`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:299`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:408`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611`
- Function body summary: Instance getters read DTMAPI instance snapshot dictionaries. Snapshot/status methods build counts from DTMAPI definition/instance dictionaries and report `ConfiguredNoRuntimeInstance` or `RuntimeCreationBlocked` when definitions exist but no runtime handles exist. Lifecycle events are raised by registry/request methods, not by native entity lifecycle callbacks.
- GameBridge/Harmony/reflection path: No native lifecycle hook path exists for custom entities today; GameBridge only publishes configured-blocked hook statuses.
- Native owner: Intended native entity lifecycle owners are not connected. Current owner is DTMAPI registry/status service.
- Native state holder: DTMAPI instance dictionaries and event dispatch.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom entity family rows 38-41.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke verifies snapshots/status and cleanup, not native instances.
  - hook-map entry: core registry stable, runtime adapters blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:121`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611`
- Result: Watch
- Recommendation: Ordinary mods can inspect DTMAPI registry/status, but must not treat active-instance counts, snapshots, or lifecycle events as native entity existence. The concrete risk is UI/status-only success: a mod sees `configured-no-runtime-instance` or lifecycle `SpawnRequested` and assumes an animal/monster/projectile/drone exists in the room/save, when no native owner was called.

### Custom entity despawn, expire, dismiss, and remove-handle methods

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RequestDespawn(...)`
- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.RequestExpire(...)`
- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RequestDismiss(...)`
- Current marker: stable family rows
- Review advice: 降级 runtime implication; keep as blocked/status contract
- Ordinary mod usability: 禁止依赖 as native removal/despawn/expire/dismiss
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:335`, `src/DTMAPI.Abstractions/CustomEntities.cs:352`, `src/DTMAPI.Abstractions/CustomEntities.cs:369`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:213`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:288`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:366`
- Function body summary: These methods delegate to `RemoveRuntimeHandle`, which only removes a handle from DTMAPI instance dictionaries and returns DTMAPI request/status results. Because spawn/summon/execute adapters are blocked, there is normally no native handle to remove.
- GameBridge/Harmony/reflection path: No native despawn/expire/dismiss path.
- Native owner: Intended monster room host, projectile manager, and drone runtime owner are not connected.
- Native state holder: DTMAPI instance dictionaries only.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI status only.
- Evidence:
  - public-api-matrix entry: custom entity family rows 39-41.
  - update record: `20260606-0014`
  - smoke path: custom entity smoke validates cleanup and blocked runtime behavior.
  - hook-map entry: custom monster/attack/drone runtime adapters configured-blocked.
  - code path: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:213`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:288`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:366`
- Result: Gap
- Recommendation: Ordinary mods cannot use these as native despawn/expire/dismiss calls. The risk is stale native state if a future partial adapter creates a native object but removal still only clears DTMAPI dictionaries; keep runtime removal blocked until native deletion owners and save cleanup are implemented.

### `DTMAPI.Abstractions.IMachineProductionApi.RegisterMachine(...)`

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.RegisterMachine(IManifest owner, MachineDefinition definition)`
- Current marker: experimental
- Review advice: 拆成 native table bridge + experimental runtime loop
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:132`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568`
- Function body summary: Normalizes and validates the machine definition, stores it in `machineDefinitions`, patches native recipe inputs, patches native tech-tree route/table data, creates a DTMAPI `MachineProductionState`, and reports `configured-experimental-runtime-loop` when the runtime loop is installed. It does not create a native machine class or transfer scheduling to an official machine production owner.
- GameBridge/Harmony/reflection path: Native table writes go through reflection into `DolocConfig.Tables.TbRecipe`, `DolocAPI.assets.techTrees`, and `TbTechNode`. Runtime production is driven later by `UpdateMachineProduction` from DTMAPI update polling.
- Native owner: Partial: recipe and tech UI owners are native. Production scheduling/fuel/cycle telemetry are DTMAPI-owned; electric mode can call native electronic component `Launch`.
- Native state holder: Native table/config for recipe/tech; DTMAPI `machineDefinitions`, `machineStates`, and `MachineRuntimeEntry` for production.
- DTMAPI registry/status/UI-only vs native runtime: Hybrid. The UI/table part is native; production success is DTMAPI runtime loop state plus native inventory/output placement.
- Evidence:
  - public-api-matrix entry: MachineProduction experimental API.
  - update record: `docs/updates/2026/20260603-0014-024-new-content-api-partial.md`, `docs/updates/2026/20260603-0019-mine-json-telemetry-smoke.md`, `docs/updates/2026/20260603-0021-mine-placement-equipment-ui-smoke.md`
  - smoke path: `docs/debug/regressions/smoke-matrix.md` Mine/Machine smoke rows, including `GAME-SMOKE/20260606-150834`
  - hook-map entry: Machine production runtime loop / visual scale / recipe table entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:620`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:702`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6977`
- Result: Gap
- Recommendation: Ordinary mods cannot safely depend on this as a general machine API. Failure modes include native tech table/shared recipe pollution between mods, production continuing or skipping based on DTMAPI polling rather than the official equipment worker, cross-room/pass-time desync, output duplication/loss, and save-state divergence. Future work should split a stable read/write native content-table bridge from an explicitly experimental `DtmapiRuntimeMachineLoop`.

### `DTMAPI.Abstractions.IMachineProductionApi.GetState(...)`

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.GetState(string uniqueId, string machineId)`
- Current marker: experimental
- Review advice: 保持 experimental with semantic warning
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:136`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1174` and runtime updates around `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6977`
- Function body summary: Reads DTMAPI `MachineProductionState` and runtime-loop telemetry. Production count, last output, fuel mode, due time, and messages describe DTMAPI's loop, not an official machine's authoritative state.
- GameBridge/Harmony/reflection path: No separate native state query for official production owner is used.
- Native owner: DTMAPI state, with partial native inventory/electric component observations.
- Native state holder: `machineStates`, `MachineRuntimeEntry`, optional native inventory/equipment object scanned at runtime.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI telemetry with native slices.
- Evidence:
  - public-api-matrix entry: Machine state/result DTO rows.
  - update record: `20260603-0019`, `20260603-0021`
  - smoke path: Mine production smoke rows.
  - hook-map entry: Machine.ProductionApi.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7076`
- Result: Watch
- Recommendation: Document that `ProductionCycleCount`, `LastOutputItemId`, `RemainingFuel`, and status fields are DTMAPI loop telemetry. Ordinary mods that treat them as native equipment truth can make bad balance/save decisions when the room is unloaded, the equipment is moved, or another mod changes recipes.

### `DTMAPI.Abstractions.IEquipmentSlotsApi.RegisterSlots(...)`

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.RegisterSlots(IManifest owner, EquipmentSlotsOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental; do not document as native extra slots
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:141`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2597`
- Function body summary: Normalizes slot options, loads DTMAPI extra-slot storage, ensures storage entries, optionally recovers disabled slot items, applies stored native equipment functions, renders a DTMAPI cloned equipment strip, and builds DTMAPI state.
- GameBridge/Harmony/reflection path: Patches `AgentEquipmentManager.ReloadParams`, `AccessoriesBar.__Init`, and `AccessoriesBar.OnStartShow` so DTMAPI can refresh stats and draw cloned UI.
- Native owner: Native `AgentEquipmentFunction` and `AgentEquipmentManager.ReloadParams` for stat application; native inventory placement/cost for item movement. Slot ownership is not native.
- Native state holder: DTMAPI sidecar slot storage plus native backpack/inventory when recovering items.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI sidecar storage/UI with native stat/inventory slices.
- Evidence:
  - public-api-matrix entry: EquipmentSlots API rows.
  - update record: `docs/updates/2026/20260603-0015-equipment-slots-storage-recovery-smoke.md`, `docs/updates/2026/20260603-0021-mine-placement-equipment-ui-smoke.md`, `docs/updates/2026/20260606-0004-029-readme-implementation.md`
  - smoke path: Equipment slots smoke rows, including `GAME-SMOKE/20260606-150834`
  - hook-map entry: `Player.EquipmentSlotsApi`.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2597`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:1206`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:316`
- Result: Gap
- Recommendation: Ordinary mods cannot use this as a native extra equipment slot API because the game does not own the extra slots. Concrete risks are sidecar storage divergence from native save transactions, UI clone desync, orphaned item recovery, and stats applying without an official equipped slot. Keep it scoped to the current DTMAPI More Equipment Slots mod until native slot ownership is proven.

### `DTMAPI.Abstractions.IEquipmentSlotsApi.EquipExtraSlot(...)` and `UnequipExtraSlot(...)`

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.EquipExtraSlot(IManifest owner, string slotId, string itemId)`
- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.UnequipExtraSlot(IManifest owner, string slotId, string reason)`
- Current marker: experimental
- Review advice: 保持 experimental
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:145`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2637`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2717`
- Function body summary: `EquipExtraSlot` checks native backpack count, generates/validates the native item, recovers existing sidecar entry, consumes the item through native item cost, stores item/function metadata in DTMAPI storage, applies equipment functions, saves storage, and renders UI. `UnequipExtraSlot` clears the sidecar entry and returns items through native backpack placement.
- GameBridge/Harmony/reflection path: UI click hooks call these APIs from DTMAPI cloned slots; stat refresh runs through `AgentEquipmentManager.ReloadParams`.
- Native owner: `DolocAPI.CostItem`, `DolocAPI.TryPlaceInBackpack`, `AgentEquipmentFunction`.
- Native state holder: Native backpack inventory plus DTMAPI sidecar slot storage.
- DTMAPI registry/status/UI-only vs native runtime: Hybrid; item movement is native, slot is DTMAPI-only.
- Evidence:
  - public-api-matrix entry: Equipment slot equip/recover rows.
  - update record: `20260603-0015`, `20260606-0004`
  - smoke path: `Smoke.NewContentEquipmentSlotsClick`, `Smoke.NewContentEquipmentSlotsUi`
  - hook-map entry: equipment slot UI and reload hooks.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2637`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2717`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6712`
- Result: Gap
- Recommendation: Ordinary mods cannot depend on this for a native slot contract. A call can consume or return real inventory items while the lasting slot record remains in DTMAPI storage, so a crash, save-boundary bug, or UI clone mismatch can strand items or apply stats without a native equipment-slot owner.

### `DTMAPI.Abstractions.ISaveSlotsApi.RegisterSlots(...)`

- Symbol: `DTMAPI.Abstractions.ISaveSlotsApi.RegisterSlots(IManifest owner, SaveSlotsOptions options)`
- Current marker: experimental
- Review advice: 升级候选 after multi-slot lifecycle evidence
- Ordinary mod usability: 普通 mod 可用 with caution
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:153`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201`
- Function body summary: Stores a per-owner requested save-slot count/policy, computes the global max requested count, clamps it, writes `DolocAPI.gameManager.archiveFileCount`, and refreshes save-slot states for official UI.
- GameBridge/Harmony/reflection path: Reflection reads/writes `DolocAPI.gameManager.archiveFileCount`; official save UI remains the owner of rendering/create/load/delete/copy behavior.
- Native owner: Native game manager archive count and official save UI.
- Native state holder: `gameManager.archiveFileCount` plus official local save/archive files.
- DTMAPI registry/status/UI-only vs native runtime: Native count owner reached; DTMAPI only arbitrates max policy.
- Evidence:
  - public-api-matrix entry: SaveSlots API rows.
  - update record: `docs/updates/2026/20260606-0004-029-readme-implementation.md`
  - smoke path: MoreSaves third-save/title smoke rows in smoke matrix.
  - hook-map entry: save UI / archive count entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2238`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2307`
- Result: Watch
- Recommendation: This is closer to ordinary-mod usable than most migrated APIs because the official save UI owns the count after DTMAPI writes it. Keep it experimental/watch because all mods share one global count, high counts can expose official UI edge cases, and create/delete/copy/load flows need continuing evidence.

### `DTMAPI.Abstractions.ICameraZoomApi.Register(...)`, `SetViewScale(...)`, and `ResetViewScale(...)`

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.Register(IManifest owner, CameraZoomOptions options)`
- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.SetViewScale(IManifest owner, double viewScale, string reason)`
- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.ResetViewScale(IManifest owner, string reason)`
- Current marker: experimental
- Review advice: RebuildNativeBridge before ordinary public docs
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:161`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1274`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1316`
- Function body summary: Registers owner zoom constraints, clamps and stores requested/current view scale, computes the strongest enabled owner scale, and writes a target orthographic size through `ApplyCameraZoomTarget`. Reset returns scale to `1`.
- GameBridge/Harmony/reflection path: Reads/writes `DolocAPI.mainCamera.orthographicSize` or `UnityEngine.Camera.main.orthographicSize`; lifecycle reset reapplies on save/title boundaries.
- Native owner: Partial Unity camera property only. Research notes identify broader owners: `CameraController` cam size/range/resolution, `envBackgroundEx`, background/depth fog/parallax, scanner refresh.
- Native state holder: Unity camera orthographic size plus DTMAPI zoom owner states.
- DTMAPI registry/status/UI-only vs native runtime: Partial native visual property; not full camera runtime.
- Evidence:
  - public-api-matrix entry: CameraZoom rows.
  - update record: `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md`, `docs/updates/2026/20260607-0001-panorama-background-zoom-note.md`
  - smoke path: Zoom 4x/restore smoke, including `GAME-SMOKE/20260606-134042`
  - hook-map entry: camera zoom entry.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2337`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2496`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2526`
  - research path: `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`, `research-DolocPlus-deep-dive-20260607.md`
- Result: Gap
- Recommendation: Ordinary mods cannot rely on this for correct camera zoom because only the orthographic size changes. Concrete risks are centered/small background, depth fog/parallax mismatch, room-bound/camera-range errors, click/input coordinate surprises, and multi-mod max-policy conflict. Rebuild against the full camera/background owner set before stable docs.

### `DTMAPI.Abstractions.IMotorVehicleApi.RegisterSecondMotor(...)`

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.RegisterSecondMotor(IManifest owner, SecondMotorOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental / MoveToDTMAPI-owned mod API
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:115`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4843`
- Function body summary: Normalizes options, validates owner/source/key ids, stores a `SecondMotorRuntime` in DTMAPI maps, and configures key/vehicle metadata. It does not register a native multi-vehicle system.
- GameBridge/Harmony/reflection path: Patches `ItemMotorKey.OnUse`, `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor/GetOffMotor`, `MotorController.OnFixedUpdate`, `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, and `DolocAPI.EnterRoom`.
- Native owner: Original motor owner exists; second motor is a DTMAPI clone/routing layer around private motor controller state.
- Native state holder: Original motor singleton/native motor controller plus DTMAPI second-motor runtime maps and clone objects.
- DTMAPI registry/status/UI-only vs native runtime: DTMAPI clone runtime using native interaction hooks.
- Evidence:
  - public-api-matrix entry: MotorVehicle / SecondMotor rows.
  - update record: `docs/updates/2026/20260603-0020-second-motor-edge-transition-smoke.md`, `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`
  - smoke path: SecondMotor smoke rows in smoke matrix.
  - hook-map entry: vehicle second motor hook entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4843`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:1137`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:256`
  - research path: `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`
- Result: Gap
- Recommendation: Ordinary mods cannot rely on this as a general vehicle API. Failure modes include shared singleton motor pollution, second-motor clone residue, original motor visual/state contamination, cross-room transition desync, duplicate keys/vehicles, and save state that DTMAPI rather than Doloc Town owns.

### `DTMAPI.Abstractions.IMotorVehicleApi.UnlockOriginalMotor(...)` and `SummonOriginalMotor(...)`

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.UnlockOriginalMotor(IManifest owner, double yOffset)`
- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.SummonOriginalMotor(IManifest owner)`
- Current marker: experimental
- Review advice: Keep debug/watch
- Ordinary mod usability: debug-only / watch
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:121`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4890`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4923`
- Function body summary: `UnlockOriginalMotor` reflects and invokes `DolocAPI.UnlockMotor(float)`. `SummonOriginalMotor` checks room eligibility, computes a position near the agent, invokes `DolocAPI.SetMotorPosition(Room, Vector2)`, and attempts native auto-fly-to-agent behavior.
- GameBridge/Harmony/reflection path: Reflection to `DolocAPI` methods and vehicle hook statuses.
- Native owner: Original motor path through `DolocAPI.UnlockMotor` and `DolocAPI.SetMotorPosition`.
- Native state holder: Official motor singleton/controller and room state.
- DTMAPI registry/status/UI-only vs native runtime: Native original motor runtime is reached.
- Evidence:
  - public-api-matrix entry: MotorVehicle original motor rows.
  - update record: SecondMotor/original motor smoke records.
  - smoke path: vehicle smoke rows.
  - hook-map entry: vehicle original motor entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4890`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4923`
- Result: Watch
- Recommendation: Native owner is real, but ordinary gameplay mods should not use these as stable product APIs yet because they mutate shared original-motor singleton state and can interfere with save/room/vehicle ownership. Keep for debug or DTMAPI controlled vehicle mod paths until lifecycle policy is documented.

### `DTMAPI.Abstractions.IMotorVehicleApi.RideVehicle(...)` and `DismountVehicle(...)`

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.RideVehicle(IManifest owner, string vehicleId)`
- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.DismountVehicle(IManifest owner, string reason)`
- Current marker: experimental
- Review advice: 保持 experimental; document original ride limitation
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:125`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4994`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5038`
- Function body summary: `RideVehicle` refuses programmatic ride for the original motor as `native-only`; second motor uses DTMAPI's clone start path and then checks `IsRiding`. `DismountVehicle` invokes native/reflected `AgentControllerState.GetOffMotor`.
- GameBridge/Harmony/reflection path: AgentControllerState get-on/get-off and MotorController fixed-update hooks.
- Native owner: Original motor ride remains native interaction-owned; second motor ride is DTMAPI clone hook path; dismount reaches native get-off owner.
- Native state holder: Agent controller/motor controller singleton plus DTMAPI second-motor maps.
- DTMAPI registry/status/UI-only vs native runtime: Mixed native singleton plus DTMAPI clone status.
- Evidence:
  - public-api-matrix entry: MotorVehicle ride/dismount rows.
  - update record: vehicle smoke records.
  - smoke path: SecondMotor ride/dismount smoke rows.
  - hook-map entry: vehicle hook entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4994`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5038`
- Result: Gap
- Recommendation: Ordinary mods cannot use this as a general riding API. Original motor ride is intentionally not forced; second-motor riding depends on DTMAPI clone hooks. If another mod calls it, concrete failures include no-op original ride, desynced riding flag, clone/original motor confusion, and stuck motor state across room changes.

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Split custom entity stable contract from blocked native runtime adapters | Prevents stable 0.4.0 docs from implying native spawn/summon/execute support. |
| P0 | Rename or implement `IInputHelper.Suppress` | Current public helper misleads ordinary mods and cannot stop native gameplay input. |
| P0 | Rebuild CameraZoom against full camera/background/fog owner map | Orthographic-only zoom is the known DolocPlus compatibility failure. |
| P1 | Split MachineProduction into native content-table bridge and DTMAPI runtime-machine loop | Avoids presenting DTMAPI polling as native production ownership. |
| P1 | Constrain EquipmentSlots docs to sidecar UI/storage/stat bridge | Prevents ordinary mods from assuming native slot persistence. |
| P1 | Keep SecondMotor as DTMAPI-owned migrated mod API | Avoids exposing clone/singleton complexity as general platform vehicle API. |
| P2 | Add multi-mod arbitration policy for SaveSlots and CameraZoom | Both use global/max policy and can surprise independent mods. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Exact custom animal native creation signature and required save fields | Hook map cites `AnimalManager.CreateAnimal`; custom registry code contains no call path. | Use local reverse build to map AnimalInfo/proto/home/feed/produce/save adapter before any runtime goal. |
| Exact custom projectile native owner | Custom attack API never reaches GameBridge; no current hook map entry beyond configured-blocked. | Map official projectile/damage classes in reverse build before exposing attack execution. |
| Full camera owner set for DTMAPI Zoom | DolocPlus research identifies `CameraController`, background, fog, scanner refresh; current code writes only camera size. | Build a camera owner map and smoke tests for background/depth/room bounds before implementation. |
| Native multi-vehicle support, if any | Motor research maps original singleton path and DTMAPI clone hooks. | Confirm whether game has any native multi-motor registry; otherwise keep second motor DTMAPI-owned. |
