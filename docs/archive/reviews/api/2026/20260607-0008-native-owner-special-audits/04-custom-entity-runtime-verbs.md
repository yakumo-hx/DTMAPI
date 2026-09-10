# 04 - CustomEntity Runtime Verbs Special Audit

## 1. Scope

APIs under review:

- `ICustomAnimalApi.RegisterSpecies` versus `RequestSpawn`
- `ICustomMonsterApi.RegisterMonster/RegisterSpawnTable` versus `RequestSpawn`
- `ICustomAttackApi.RegisterAttack` versus `SpawnProjectile/ExecuteAttack`
- `ICustomDroneApi.RegisterDrone` versus `RequestSummon/Equip/SetMode`
- DTO/result fields that can imply native runtime support: `Succeeded`, `FailureReason`, `RuntimeStatus`, `Handle`, `Snapshot`, `ActiveRuntimeInstanceCount`, `SaveStateRecordCount`, `SpawnRules`, `Summon`, and execution/summon/spawn result names.

Questions answered:

- Which calls are registry-only success and which runtime verbs return `runtime-creation-blocked`.
- Whether any native animal, monster, projectile/attack, or drone object is created.
- Whether result fields can mislead ordinary mods into thinking native runtime creation exists.
- Which native owners are required before runtime verbs become usable.

Result: `Blocked` for runtime verbs. Registration/status is a DTMAPI Core registry contract; runtime creation is explicitly blocked before GameBridge native adapters are reached.

## 2. Files read

- `docs/api/public-api-matrix.md:46`
- `docs/api/public-api-matrix.md:47`
- `docs/api/public-api-matrix.md:48`
- `docs/api/public-api-matrix.md:49`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:53`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:54`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:58`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:59`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:61`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md:75`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/07-completion-audit.md:80`
- `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md:45`
- `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md:47`
- `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md:67`
- `docs/hook-map/README.md:28`
- `docs/hook-map/README.md:45`
- `docs/hook-map/README.md:62`
- `docs/hook-map/README.md:79`
- `docs/hook-map/README.md:96`
- `docs/debug/INDEX.md:34`
- `docs/debug/regressions/smoke-matrix.md:5`
- `src/DTMAPI.Abstractions/CustomEntities.cs`
- `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- Reverse candidate search under `references/doloc-town/reverse/builds/` for `AnimalManager`, `AnimalInfo`, `AnimalSystem`, `AnimalExcrete`, `AnimalWork_*`, `MonsterController`, `MonsterGroupManager`, `MonsterStateManager`, `MonsterAttackBehaviour*`, `BulletFactory`, `BulletManager`, `PhysicalDamageBox`, `AttackHitInfo`, `DroneController`, `DroneWeapon`, and `DolocAPI.EquipDrone`.

## 3. Functions read

- `DTMAPI.Abstractions.ICustomAnimalApi.RegisterSpecies(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:322`
- `DTMAPI.Abstractions.ICustomMonsterApi.RegisterMonster(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:338`
- `DTMAPI.Abstractions.ICustomMonsterApi.RegisterSpawnTable(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:339`
- `DTMAPI.Abstractions.ICustomAttackApi.SpawnProjectile(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:359`
- `DTMAPI.Abstractions.ICustomAttackApi.ExecuteAttack(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:360`
- `DTMAPI.Abstractions.ICustomDroneApi.RequestSummon(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:376`
- `DTMAPI.Abstractions.ICustomDroneApi.Equip(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:378`
- `DTMAPI.Abstractions.ICustomDroneApi.SetMode(...)`: `src/DTMAPI.Abstractions/CustomEntities.cs:379`
- `CustomEntityRegistryService.RegisterSpecies(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60`
- `CustomEntityRegistryService.RequestSpawn(CustomAnimalSpawnRequest)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101`
- `CustomEntityRegistryService.RegisterMonster(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138`
- `CustomEntityRegistryService.RegisterSpawnTable(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:158`
- `CustomEntityRegistryService.RequestSpawn(CustomMonsterSpawnRequest)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:198`
- `CustomEntityRegistryService.RegisterAttack(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235`
- `CustomEntityRegistryService.SpawnProjectile(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278`
- `CustomEntityRegistryService.ExecuteAttack(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:283`
- `CustomEntityRegistryService.RegisterDrone(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310`
- `CustomEntityRegistryService.RequestSummon(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351`
- `CustomEntityRegistryService.Equip(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371`
- `CustomEntityRegistryService.SetMode(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390`
- `CustomEntityRegistryService.BeginSaveSession(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:450`
- `CustomEntityRegistryService.ClearRuntimeInstances(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:457`
- `CustomEntityRegistryService.RequestAttack(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:476`
- `CustomEntityRegistryService.RemoveRuntimeHandle(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:491`
- `CustomEntityRegistryService.BuildSnapshot(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580`
- `CustomEntityRegistryService.BuildStatus(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611`
- `CustomEntityRegistryService.BuildBlockedDetails(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:787`
- `CustomEntityRegistryService.AnimalRequest(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:954`
- `CustomEntityRegistryService.MonsterRequest(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:971`
- `CustomEntityRegistryService.AttackRequest(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:988`
- `CustomEntityRegistryService.DroneRequest(...)`: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:1005`
- `DtmApiRuntime.NotifySaveLoaded(...)`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:152`
- `DtmApiRuntime.NotifyReturnedToTitle(...)`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:179`
- `DolocTownGameBridge.RegisterExperimentalApis()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:222`
- `DolocTownGameBridge.PublishStableCustomEntityHookStatuses()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:261`
- `DolocTownGameBridge` custom-entity smoke exercise: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3511`
- Unit test `CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots`: `tests/DTMAPI.UnitTests/Program.cs:431`
- Unit helper `IsBlocked(...)`: `tests/DTMAPI.UnitTests/Program.cs:591`

## 4. Call graph

Registration contract:

`mod -> RegisterSpecies/RegisterMonster/RegisterSpawnTable/RegisterAttack/RegisterDrone -> CustomEntityRegistryService.ValidateDefinition -> dictionary store -> provider OnRegistered -> DTMAPI lifecycle event -> RegistrationResult.Succeeded=true`

Runtime verb contract:

`mod -> RequestSpawn/SpawnProjectile/ExecuteAttack/RequestSummon -> ValidateRequest -> owner check -> DTMAPI lifecycle requested event -> RequestResult.Succeeded=false, FailureReason=runtime-creation-blocked, RuntimeStatus=RuntimeCreationBlocked`

Drone command/equipment verbs:

`mod -> Equip/SetMode -> RemoveRuntimeHandle on a DTMAPI handle dictionary -> map runtime-instance-not-found to runtime-creation-blocked -> no native drone command`

Status/snapshot:

`GetSnapshot/GetStatus -> dictionary counts -> RuntimeStatus=ConfiguredNoRuntimeInstance when definitions exist and no handles -> FailureReason=runtime-creation-blocked`

GameBridge publication:

`DolocTownGameBridge.RegisterExperimentalApis -> runtime.RegisterRuntimeApi<ICustom*Api>(runtime.CustomEntities)`
`PublishStableCustomEntityHookStatuses -> CoreRegistry=verified; per-family GameBridge statuses=configured-blocked`

Save boundary:

`DtmApiRuntime.NotifySaveLoaded -> CustomEntities.BeginSaveSession -> ClearRuntimeInstances`
`DtmApiRuntime.NotifyReturnedToTitle -> CustomEntities.ClearRuntimeInstances`

## 5. Function body findings

- `RegisterSpecies` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60` validates namespaced id/owner, rejects duplicates, stores the definition in `animals`, invokes the optional provider, raises a DTMAPI lifecycle event, and returns registration success. It does not call `AnimalManager`.
- `RequestSpawn` for animals at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101` validates the definition and owner, raises `SpawnRequested`, and returns `AnimalRequest(false, ..., RuntimeCreationBlocked, BuildBlockedDetails(Animal))`.
- `RegisterMonster` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138` and `RegisterSpawnTable` at `:158` store DTMAPI dictionaries. They do not mutate native dungeon/room spawn tables.
- `RequestSpawn` for monsters at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:198` returns `runtime-creation-blocked` after validation and a DTMAPI lifecycle event.
- `RegisterAttack` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235` stores attack definitions and can warn on non-positive damage. It does not create a bullet/projectile prefab or damage owner.
- `SpawnProjectile` and `ExecuteAttack` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278` and `:283` both delegate to `RequestAttack`, which returns blocked at `:488`.
- `RegisterDrone` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310` stores a drone definition and raises a DTMAPI lifecycle event. It does not call `DolocAPI.EquipDrone`, `DroneController`, or weapon/equipment owners.
- `RequestSummon` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351` returns blocked after validation.
- `Equip` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371` calls `RemoveRuntimeHandle` with the reason that equipment cannot target a native drone yet, then always returns `Succeeded=false` and `RuntimeStatus=RuntimeCreationBlocked`.
- `SetMode` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390` follows the same blocked path as `Equip`.
- `BeginSaveSession` and `ClearRuntimeInstances` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:450` and `:457` clear all instance dictionaries at save boundaries; this prevents stale DTMAPI handles but also proves there is no native restore path in this service.
- `BuildSnapshot` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:580` sets `SaveStateRecordCount=0` and reports `ConfiguredNoRuntimeInstance` when definitions exist without handles.
- `BuildStatus` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:611` sets `FailureReason=runtime-creation-blocked` when definitions exist but no instances.
- `BuildBlockedDetails` at `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:787` names the missing native adapters per family: animal proto/room/food/excrement/breeding/product, monster controller/spawn/AI/attack/damage/drop, bullet manager/factory/hitbox/collision/damage, and drone controller/weapon/equipment/movement/persistence.
- `DolocTownGameBridge.PublishStableCustomEntityHookStatuses` at `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:261` reports the Core registry as verified and all four runtime adapters as `configured-blocked`.
- Unit test lines `tests/DTMAPI.UnitTests/Program.cs:514` through `:517` assert that animal spawn, monster spawn, attack execution, and drone summon are blocked; `:591` defines blocked as `!Succeeded`, `FailureReason=runtime-creation-blocked`, and `RuntimeStatus=RuntimeCreationBlocked`.
- Smoke exercise lines `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3511` through `:3523` repeat blocked checks for spawn/projectile/summon/equip/mode, then status checks at `:3529` through `:3532` assert `configured-no-runtime-instance`.

## 6. Native owner verdict

Verdict: `DTMAPI-only` for registration/status; `Blocked` for runtime verbs.

Reached owners:

- DTMAPI Core `CustomEntityRegistryService` dictionaries, validation, lifecycle event isolation, provider callback isolation, snapshots/status, owner cleanup, and save-boundary cleanup.

Not reached:

- Animal: `AnimalManager`, `AnimalInfo`/proto, room/home placement, feeding, excrement, breeding, produce/hidden produce, animal save data.
- Monster: native spawn table/group, `MonsterController`, `MonsterGroupManager`, targeting/AI/movement, attack behavior, damage, loot/drop, despawn/save.
- Attack/projectile: `BulletFactory`, `BulletManager`, `Bullet`, `PhysicalDamageBox`, `AttackInfo`, `AttackHitInfo`, lifetime/expire and damage ownership.
- Drone: `DroneController`, `DroneWeapon`, `DolocAPI.EquipDrone`, equipment slots, behavior mode, movement, energy/repair, persistence.

Excluded candidates:

- Hook/status publication is excluded as runtime proof because it explicitly says `configured-blocked`.
- Snapshot/status fields are excluded as native proof because they are computed from Core dictionaries and report `SaveStateRecordCount=0`.
- Lifecycle events are excluded as native lifecycle proof because they are raised before any native object exists.

## 7. Ordinary mod usability

Ordinary mod usability:

- Registry definitions: `普通 mod 可用` only as metadata/contract registration when docs clearly state "no native runtime creation."
- Runtime verbs: `禁止依赖`.

An ordinary mod can safely register definitions and inspect registry/status metadata. It cannot safely depend on spawn, summon, execute, equip, mode, remove/despawn/expire, native snapshots, or save restoration.

## 8. Concrete failure modes

1. Animal spawn does not create a native animal; there is no room/home/feed/excrement/breeding/produce/save state, so a mod can display registry success but get no animal in-game.
2. Monster spawn tables do not mutate native room/dungeon spawn logic; a mod can register spawn rules and still see no native monster, AI, loot, or despawn lifecycle.
3. Attack/projectile verbs do not create bullets, hitboxes, collision, damage ownership, or expiry; combat mods receive blocked results rather than runtime attacks.
4. Drone summon/equip/mode commands cannot target a native drone; `Equip` and `SetMode` return blocked even when the request names a handle.
5. DTO semantic risk: `RuntimeStatus`, `Handle`, `Snapshot`, `ActiveRuntimeInstanceCount`, and `SaveStateRecordCount` can look like native state fields, but current values are Core registry/status bookkeeping.
6. Lifecycle semantic risk: `SpawnRequested` events fire before native creation, so a listener can mistake a DTMAPI request event for a native spawn lifecycle.
7. Save semantic risk: `RestoreRuntimeInstancesOnSaveLoad` exists in the contract, but current `BeginSaveSession` clears runtime dictionaries and `BuildSnapshot` reports `SaveStateRecordCount=0`.

## 9. Minimal rebuild direction

- Split the stable surface into `CustomEntityRegistry` and `CustomEntityRuntimeAdapter` concepts.
- Keep registration and DTO definitions stable as data contracts, but mark every runtime verb as blocked/experimental until a per-family adapter is proven.
- For animals, build an owner map around native animal proto creation, room/home placement, food consumption, excrement, breeding, product/hidden product generation, UI viewer data, and save records.
- For monsters, build an owner map around spawn groups, room/host placement, AI targeting, movement, attack behavior, damage, loot/drop, despawn, and save cleanup.
- For attacks, build an owner map around bullet factory/manager, damage payload ownership, hitbox/collision, lifetime/expiry, source/faction, and deterministic pattern ticking.
- For drones, build an owner map around summon/owner binding, `DolocAPI.EquipDrone`, controller, weapons, equipment slots, behavior modes, energy/repair, movement, dismissal, and persistence.
- Rename or document DTO fields so authors see "registry/status" versus "native runtime" immediately. For example, `ActiveRuntimeInstanceCount` should not imply native objects unless the adapter status is `Active`.

## 10. Evidence gaps

- Missing verified native method signature and adapter contract for each family; candidate files were located, but no decompiled source was copied into this review.
- Missing in-game smoke that creates a native animal, monster, projectile/attack, or drone from the custom APIs; existing smoke intentionally verifies blocked results.
- Missing save/load restore evidence for custom runtime instances; current service clears instance dictionaries on save load and title return.
- Missing proof that provider/tick policies can run in the same order/timing as native AI or projectile ticks.
- Missing DTO/documentation split that prevents ordinary authors from reading `Succeeded`, `RuntimeStatus`, `Snapshot`, `Handle`, or `SaveStateRecordCount` as native runtime guarantees.
