# DTMAPI 0.4.0 Stable Custom Entity APIs

Status: implemented contract and registry; native runtime creation adapters are intentionally blocked until verified.

## Stable Surfaces

DTMAPI 0.4.0 adds four stable author-facing APIs in `DTMAPI.Abstractions`:

- `ICustomAnimalApi`
- `ICustomMonsterApi`
- `ICustomAttackApi`
- `ICustomDroneApi`

The contracts use stable DTMAPI DTOs and handles only. They do not expose raw Doloc Town, Unity, Harmony, BepInEx, or decompiled types.

## Shared Contract Rules

- Definition IDs must be namespaced, for example `Author.Mod.Entity`.
- Registrations are owner-aware and tied to the registering `IManifest.UniqueID`.
- Duplicate definition IDs are rejected globally.
- Lifecycle events and behavior providers are isolated; exceptions are recorded under the owner instead of crashing the runtime.
- Runtime handles are stable DTMAPI values. A missing or stale handle returns a result DTO with a failure reason rather than exposing native object state.
- Save/load boundaries clear runtime instance snapshots so stale runtime state does not survive a new save session.
- Owner cleanup removes definitions and transient runtime snapshots for animals, monsters, attacks, drones, and monster spawn tables.

## Current Runtime Creation State

The 0.4.0 registry is usable for registration, validation, querying, snapshots, status pages, and future UI. Native creation requests currently return:

```text
FailureReason = runtime-creation-blocked
RuntimeStatus = RuntimeCreationBlocked
```

This is deliberate. The GameBridge has identified native adaptation paths, but creating live custom entities requires verified adapters for native proto/table records, scene assets, save data, AI/update loops, collisions, equipment, and cleanup. Until those adapters are proven in game, DTMAPI exposes a stable configured-state path rather than creating unsafe native objects.

## Animal Coverage

`ICustomAnimalApi` covers:

- species and variants;
- localized name/description metadata;
- icon/sprite asset handles;
- life stages;
- habitat/building/room placement constraints;
- diet, feeding, consumption cadence, and fed state;
- excrement timing, outputs, capacity, and cleanup;
- breeding compatibility, cooldown, incubation, offspring, and population limits;
- hidden products and ordinary produce rules;
- health, mood, friendship, movement, persistence, tick policy, lifecycle events, spawn/remove requests, and snapshots.

Native research points are `AnimalManager.CreateAnimal`, `Animal.Eat`, `Animal.Excrete`, `Animal._Breed`, `Animal.ProduceAsItems`, animal work classes, animal viewer data, and save lifecycle paths.

## Monster Coverage

`ICustomMonsterApi` covers:

- monster IDs and variants;
- spawn rules and spawn tables;
- group/max-count controls;
- faction/hostility/team metadata;
- health, armor, contact damage, movement speed, and resistances;
- target selection and movement policy;
- attack slots linked to `ICustomAttackApi`;
- loot/drop rules;
- persistence, tick policy, behavior provider hooks, lifecycle events, spawn/despawn requests, and snapshots.

Native research points are `MonsterController`, `MonsterGroupManager`, `MonsterAI_Target`, `MonsterStateManager`, and `MonsterAttackBehaviourManager`.

## Attack Projectile And Barrage Coverage

`ICustomAttackApi` covers:

- attack/projectile IDs;
- faction, source/target handles, and friendly-fire metadata;
- damage payloads and effect tags;
- hitbox shape;
- trajectory and deterministic pattern/barrage definitions;
- lifetime, pierce, bounce, homing, visual/audio assets;
- provider callbacks for pattern ticks, collisions, damage application, and expiry;
- projectile spawn, direct attack execution, active snapshots, and expire requests.

Native research points are `BulletFactory`, `BulletManager`, `Bullet`, `BulletEntity`, `PhysicalDamageBox`, `AttackInfo`, and `AttackHitInfo`.

## Drone Coverage

`ICustomDroneApi` covers:

- drone IDs and variants;
- owner binding and permission tags;
- behavior modes such as follow, guard, patrol, return, idle, attack, support, and provider-controlled;
- equipment and module slots;
- linked attack definitions;
- health, shield, armor, energy/fuel/ammo-like values;
- movement layer/collision policy;
- repair and summon/dismiss rules;
- persistence, tick policy, behavior provider hooks, lifecycle events, equipment/mode requests, and snapshots.

Native research points are `Drone`, `DroneController`, `DroneWeapon`, `DroneWeaponGun`, `DroneWeaponSword`, `DronePanel`, and `DolocAPI.EquipDrone`.

## Verification

- Release build and unit/contract tests passed on 2026-06-06.
- Unit tests cover invalid IDs, duplicate IDs, four-family registration, registry lookup, lifecycle listener error isolation, snapshots, `runtime-creation-blocked` request results, save-boundary cleanup, and owner cleanup.
- Third-save game smoke `GAME-SMOKE/20260606-191219` uses the internal `DTMAPI.CustomEntityApiSmokeHarness` owner. It registers all four families, verifies invalid/duplicate handling, checks snapshots/status, confirms `runtime-creation-blocked` request results, removes the smoke owner with `cleanupRemoved=5`, and exits without creating a player-facing mod or leaving `DolocTown.exe`.
