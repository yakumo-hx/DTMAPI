# Update 20260606-0014: 0.4.0 Stable Custom Entity APIs

Date: 2026-06-06
Status: implemented

## Source Request

Goal file: `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.md`

The goal requires upgrading DTMAPI itself to fixed version `0.4.0` and adding stable public APIs for custom animals, custom monsters, custom attacks/projectiles/barrages, and custom drones. This update deliberately does not create a new player-facing mod and does not intentionally modify existing migrated/test mods.

## Version Change

- Old controlled version: `0.3.1`
- New controlled version: `0.4.0`
- Controlled version files changed:
  - `Directory.Build.props`: assembly/file/package version `0.3.1` -> `0.4.0`
  - `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`: `ApiVersion` `0.3.1` -> `0.4.0`
  - `tools/scripts/install-to-game.ps1`: installed-package `MinimumDTMApiVersion` and DTMAPI dependency minimum normalization `0.3.1` -> `0.4.0`
- Existing mod manifests were not bulk-edited for feature work.

## Changed Files

- `src/DTMAPI.Abstractions/CustomEntities.cs`
- `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/install-to-game.ps1`
- `Directory.Build.props`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/api/040-stable-custom-entity-apis.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`

## Implementation Notes

- Added stable public APIs `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, and `ICustomDroneApi`.
- Added stable DTOs for namespaced IDs, owner identity, localized metadata, asset handles, stable runtime handles, validation messages, capability status, lifecycle events, tick policies, persistence, save migration context, behavior providers, snapshots, and request/registration results.
- Added family-specific contracts for animal diet/excrement/breeding/hidden products, monster spawn/AI/movement/attack/loot, attacks/projectiles/barrage/damage/collision policy, and drone owner binding/equipment/movement/energy/repair/summon behavior.
- Added `CustomEntityRegistryService` with owner-aware registries, duplicate-ID detection, invalid-definition validation, lifecycle listener/provider error isolation, save-boundary runtime cleanup, owner cleanup, status/snapshot summaries, and explicit `runtime-creation-blocked` request results.
- Registered the stable APIs from the core `DTMAPI` runtime manifest and the `DTMAPI.GameBridge.DolocTown` runtime API manifest.
- Added GameBridge hook/status records for custom animal, monster, attack/projectile, and drone native adapter research paths. These paths are `configured-blocked` until native adapters are verified.
- Added an internal game smoke harness switch `-AutoExerciseCustomEntityApis`. The harness registers all four families, validates invalid/duplicate handling, verifies blocked request results and snapshots/status, then removes the smoke owner.

## Known Facts And Rejected Hypotheses

- Known animal paths in build `23465763_workshop_38581E`: `AnimalManager.CreateAnimal`, `Animal.Eat`, `Animal.Excrete`, `Animal._Breed`, `Animal.ProduceAsItems`, animal work classes, animal viewer data, and save data.
- Known monster paths: `MonsterController`, `MonsterGroupManager`, `MonsterAI_Target`, `MonsterStateManager`, and `MonsterAttackBehaviourManager`.
- Known attack/projectile paths: `BulletFactory`, `BulletManager`, `Bullet`, `BulletEntity`, `PhysicalDamageBox`, `AttackInfo`, and `AttackHitInfo`.
- Known drone paths: `Drone`, `DroneController`, `DroneWeapon`, `DroneWeaponGun`, `DroneWeaponSword`, `DronePanel`, and `DolocAPI.EquipDrone`.
- Rejected direction: installing new Harmony/native creation patches in this round before proto/table/asset/save adapters are verified. The stable API instead exposes configured registry/status paths and explicit blocked runtime creation results.
- Rejected direction: proving the APIs through a new ordinary mod. The goal forbids new mods, so validation uses unit tests and an internal GameBridge smoke harness owner.

## Validation

- Passed: `./tools/scripts/build.ps1 -Configuration Release`
  - Result: Release build and unit tests passed with 0 warnings and 0 errors after nullable cleanup.
  - Unit test: `CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots`
- Passed: `./tools/scripts/run-game-smoke.ps1 -DirectExe -SaveSlot 3 -TimeoutSeconds 180 -IncludeHookProbe -AutoExerciseCustomEntityApis -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260606-191219`
  - Result JSON: `StartupLog=true`, `GameLaunched=true`, `HookProbe=true`, `SaveLoaded=true`, `CustomEntityApis=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, `ForcedClose=false`
  - DTMAPI log: startup at `2026-06-06 19:12:21.586 +08:00`; custom APIs registered from `DTMAPI`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`; `CustomEntities.CoreRegistry=verified`; all four family statuses `configured-blocked`; `Smoke.CustomEntityApis=verified`.
  - Smoke summary: `registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=5; lifecycleEvents=2/3/2/2`.
  - Exit checks: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- API matrix: `docs/api/public-api-matrix.md`
- Author API notes: `docs/api/040-stable-custom-entity-apis.md`
- Hook map: `docs/hook-map/README.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260606-191219`

## Rollback Notes

- Revert `Directory.Build.props`, `DtmApiRuntime.ApiVersion`, and installer dependency normalization to the previous controlled version only if rolling back the entire 0.4.0 API goal.
- Removing `CustomEntities.cs` and `CustomEntityRegistryService.cs` also requires removing runtime/GameBridge registrations, tests, smoke-script options, API matrix entries, hook-map entries, and this update record.
- No existing player-facing mod manifests were intentionally changed by this update.

## Follow-Up

- Verify native adapters family-by-family before enabling runtime creation:
  - animal proto/home/food/excrement/breeding/product/save adapters;
  - monster spawn group/AI/move/attack/damage/drop/despawn adapters;
  - attack projectile/collision/damage/barrage adapters;
  - drone controller/weapon/equipment/movement/persistence adapters.
- Once a native family is verified in game, update its hook status from `configured-blocked` to `verified` with a dedicated smoke row and evidence.
