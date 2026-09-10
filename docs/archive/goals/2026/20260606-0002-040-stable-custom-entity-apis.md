# DTMAPI 0.4.0 Stable Custom Entity API Goal

Status: active handoff goal
Created: 2026-06-06
Target version: 0.4.0

## Source Request

The user wants the next fresh Codex to upgrade DTMAPI itself, not to build another player-facing mod:

- Provide stable APIs for custom animals, including food consumption, poop/excrement, breeding, and hidden products.
- Provide stable APIs for custom monsters, including spawning, behavior, attacks, movement, and combat lifecycle.
- Provide stable APIs for custom attacks/projectiles/barrage patterns.
- Provide stable APIs for custom drones, including behavior, equipment slots, damage, movement, and persistence.
- Upgrade DTMAPI to version 0.4.0.
- Do not create new mods.
- Do not modify existing migrated/test/official-local mods.
- Only provide the API surface, Core registries, GameBridge adapters/hooks, documentation, and validation needed for the API.

## Current Context

- Root `readme.md` is not part of the workflow and must not be used as a task ledger.
- This goal is the only task source for the implementation Codex.
- DTMAPI `0.3.1` was previously produced by a Codex version-bump mistake. Do not infer any target from that mistake.
- The fixed target for this goal is `0.4.0`. If the current code is `0.3.1`, `0.3.0`, or another earlier value, normalize controlled version sources to `0.4.0` only.
- If the workspace is already at `0.4.0`, do not bump again.
- Existing public APIs include many experimental GameBridge surfaces for debug console, machines, equipment slots, vehicles, camera zoom, chest locator, and planting gun. These must not be casually broken or renamed.
- The new 0.4.0 APIs should become a broad, stable author-facing foundation for living entities, combat objects, and autonomous companions.

## Non-Goals

- Do not write a custom animal mod.
- Do not write a custom monster mod.
- Do not write a projectile/barrage demo mod.
- Do not write a drone mod.
- Do not edit existing player-facing mods such as ActionSpeed, OneActionComplete, AutoFishing, AnimalHusbandryProgress, FishBreedingAssistant, SecondMotor, Oil, Mine, MoreEquipmentSlots, MoreSaves, Zoom, ChestLocatorEnhancer, StrongPlantingGun, or DebugConsole/Y-console except if a compile break is caused by the API upgrade and the smallest compatibility adjustment is unavoidable.
- Do not copy old DLKsmapi implementation.
- Do not expose raw decompiled Doloc Town, Unity, Harmony, or BepInEx types in stable public API DTOs.
- Do not mark the goal complete by writing placeholder interfaces with no registry, docs, or validation path.

## Design Direction

Implement 0.4.0 as an API-first upgrade:

```text
DTMAPI.Abstractions stable contracts
  -> DTMAPI.Core registries/lifecycle/persistence/error isolation
  -> DTMAPI.GameBridge.DolocTown adapters/hooks/reflection
  -> future ordinary DTMAPI mods
```

Stable public APIs should use Doloc-styled, versionable DTOs and handles. Fragile raw game interaction belongs in `DTMAPI.GameBridge.DolocTown`.

The implementation should prefer registration-based APIs:

- mods register definitions and behavior providers during `Entry`;
- Core stores owner, UniqueID, version, enablement, and lifecycle;
- GameBridge turns stable definitions into runtime hooks/adapters;
- state and persistence are tied to save lifecycle and cleaned up on mod disable/missing owner;
- errors are attributed to the owning mod and do not crash the whole API.

## Task A: Baseline And 0.4.0 Version Target

- Run `git status` before changes.
- Read the required project, workflow, debug, hook, API, and reference docs listed in the short goal prompt.
- Inventory current controlled version sources and current public API organization.
- Set the fixed target version to `0.4.0` only.
- Do not bump to `0.4.1` or any other value unless the user explicitly changes this goal.
- Preserve existing APIs and mod behavior unless a compile fix is necessary.
- Record the old version, new version, touched version files, and reason in the update record.

Acceptance:

- The implementation can state exactly which files controlled the version before and after.
- No existing mod was intentionally changed for feature work.
- Any unavoidable compatibility edit to an existing mod is documented with the reason.

## Task B: Stable Custom Entity API Architecture

Add a coherent stable API architecture for author-defined runtime entities.

Required public concepts:

- namespaced definition IDs;
- owner mod identity;
- localized display metadata;
- icon/asset handles;
- stable runtime handles that do not expose raw game objects;
- registration/unregistration results;
- enablement and capability status;
- lifecycle events;
- save data keys and migration hooks;
- validation errors and warnings;
- behavior provider interfaces;
- safe event dispatch with owner attribution;
- deterministic update/tick policies;
- snapshot DTOs for current runtime state;
- failure reasons suitable for UI/log display.

Required Core behavior:

- registries for animals, monsters, attacks/projectiles, and drones;
- duplicate-ID detection;
- owner disable/unload cleanup rules;
- save/load restoration and orphan-data handling;
- clear separation between definitions, runtime instances, and save-state records;
- debug/status summaries for `docs/debug` and future DTMAPI UI use.

Acceptance:

- The API matrix lists the 0.4.0 surfaces as stable or explicitly explains any part that remains experimental.
- Unit/contract tests cover registration, duplicate IDs, invalid definitions, owner cleanup, and state snapshot behavior.
- GameBridge can report "configured but no runtime instance" without throwing.

## Task C: Stable Custom Animal API

Provide stable author-facing APIs for custom animals.

Required definition fields:

- species ID and optional variants;
- localized names and descriptions;
- age/life-stage data where useful;
- habitat/building/room placement constraints;
- food/diet rules;
- consumption cadence and hunger/feed state;
- poop/excrement rules, item outputs, timing, capacity, and cleanup;
- breeding compatibility, cooldowns, pregnancy/incubation, offspring rules, and population limits;
- hidden product rules, progress, visibility metadata, and output conditions;
- ordinary produce rules if needed to make hidden product contracts coherent;
- stat/mood/health hooks as stable DTOs;
- icon/sprite/asset references without raw Unity exposure.

Required services/events:

- register animal species;
- query species definitions;
- query animal instances by stable handle;
- spawn/create request result;
- remove/despawn request result;
- feed/consume events;
- poop/excrement produced event;
- breeding started/completed/failed events;
- hidden-product progress changed and ready events;
- animal state snapshot and save-state migration.

GameBridge research requirements:

- Inspect official animal, animal viewer, produce, breeding, food, and save paths in the decompiled build.
- Document the chosen hook points in the hook map.
- Do not use the existing AnimalHusbandryProgress mod as the API implementation; it may only inform required display data.

Acceptance:

- A future mod author can define an animal species and reason about food, excrement, breeding, and hidden products from stable DTOs alone.
- No stable public type exposes raw Doloc Town animal or UI classes.
- The API has a documented persistence and cleanup plan for disabled/missing animal-owner mods.

## Task D: Stable Custom Monster API

Provide stable author-facing APIs for custom monsters.

Required definition fields:

- monster ID and variants;
- localized names/descriptions;
- spawn rules by room/map/biome/time/weather/season/probability;
- spawn group and max-count controls;
- despawn rules;
- faction/hostility/team metadata;
- health/armor/resistance/damage stats;
- target selection rules;
- movement style;
- behavior state machine hooks;
- attack slots and attack selection;
- loot/drop rules;
- visual/audio/asset handles;
- save persistence policy.

Required services/events:

- register monster type;
- query monster definitions and instances;
- spawn/despawn requests with failure reasons;
- spawn wave or spawn table registration;
- behavior tick provider;
- movement provider;
- aggro/target-changed event;
- attack-start/attack-hit/attack-end events;
- damaged/death/despawn events.

GameBridge research requirements:

- Inspect official monster spawn, AI, movement, attack, damage, drop, and despawn paths.
- Prefer stable adapters and behavior callbacks over letting mods Harmony-patch monster internals directly.

Acceptance:

- Future mods can add monsters without owning raw Unity update loops.
- Spawn, behavior, attack, movement, and drops have stable DTO contracts.
- Failure modes such as invalid room, missing asset, or blocked spawn are visible in result DTOs/logs.

## Task E: Stable Custom Attack Projectile And Barrage API

Provide stable author-facing APIs for attacks, projectiles, and bullet-pattern style effects.

Required definition fields:

- attack/projectile ID;
- owner/faction/team;
- source entity handle;
- damage payload;
- status effects or effect tags;
- hitbox/collision shape;
- trajectory/movement policy;
- barrage/pattern definition;
- lifetime/despawn policy;
- pierce/bounce/homing/speed parameters;
- friendly-fire rules;
- visual/audio/asset handles;
- network-free deterministic update assumptions for current single-player scope.

Required services/events:

- register attack/projectile definitions;
- spawn projectile or execute attack;
- query active attacks/projectiles;
- collision/hit callbacks;
- damage-applied events;
- expired/despawned events;
- pattern tick provider for barrage behaviors.

Integration requirements:

- Monster and drone APIs should be able to reference this attack API.
- The API should be usable by future player-skill mods, monster mods, and drone mods.
- Do not directly expose physics, collider, Unity object, or decompiled projectile types as stable public values.

Acceptance:

- A future custom monster or drone can declare an attack pattern through stable DTOs.
- Damage ownership and friendly-fire rules are explicit.
- The hook map identifies the game damage/collision paths used or records what remains blocked.

## Task F: Stable Custom Drone API

Provide stable author-facing APIs for custom drones/autonomous companions.

Required definition fields:

- drone ID and variants;
- localized names/descriptions;
- owner binding and permission rules;
- behavior modes such as follow, guard, patrol, return, idle, attack, harvest/support if supported;
- equipment slots and allowed equipment categories;
- module/weapon slots;
- health/shield/damage/resistance;
- energy/fuel/ammo if relevant;
- movement style, speed, altitude/layer, collision policy;
- damage and repair rules;
- summon/dismiss rules;
- persistence and cleanup rules;
- visual/audio/asset handles.

Required services/events:

- register drone type;
- summon/create drone;
- dismiss/remove drone;
- query drone instances and equipment;
- equip/unequip drone equipment;
- command/mode change;
- behavior tick provider;
- movement provider;
- attack provider or linked attack definitions;
- damaged/destroyed/repaired events;
- save-state migration and orphan recovery.

Integration requirements:

- Drone attacks should use the stable attack/projectile API where possible.
- Drone equipment slots must not reuse the existing MoreEquipmentSlots mod implementation as a feature mod; if shared concepts are useful, expose them as neutral DTOs in Abstractions/Core.

Acceptance:

- Future authors can define a drone with equipment, movement, combat, and persistence without touching GameBridge internals.
- Disabling a drone-owning mod leaves no invalid live drone state and has a documented recovery behavior.

## Task G: GameBridge Hook And Research Layer

For each new API family, identify the minimum hook/adaptation points needed in `DTMAPI.GameBridge.DolocTown`.

Required:

- animal lifecycle/food/produce/breeding/save hooks;
- monster spawn/AI/move/combat/drop hooks;
- attack/projectile/damage/collision hooks;
- drone entity/update/equipment/combat/movement hooks or a safe DTMAPI-managed fallback if no native analog exists;
- status reporting for missing/blocked hook points;
- no raw decompiled types in public APIs;
- hook-map entries for every hook or reflection path;
- debug records for risky or blocked paths.

Acceptance:

- Each API family has either a verified hook path or an explicit "registered/configured, runtime creation blocked" result with evidence.
- The implementation does not scatter reflection/Harmony code across ordinary mods.

## Task H: Documentation API Matrix And Validation

Update the project documentation and validation records.

Required docs:

- `docs/api/public-api-matrix.md` with 0.4.0 API surfaces and stability status.
- `docs/hook-map/README.md` for new hook/adaptation points.
- `docs/debug/INDEX.md` and relevant debug/regression docs if runtime lifecycle, hooks, save/load, input, or entity loops are touched.
- `docs/debug/regressions/smoke-matrix.md` with 0.4.0 API smoke/contract checks.
- `docs/updates/YYYY/...` update record with changed files, validation, evidence, rollback, and follow-up.
- Any new author-facing API docs needed to explain the stable contracts.

Validation requirements:

- Release build passes.
- Unit/contract tests pass.
- Game smoke uses the third local save unless this goal file is explicitly updated by the user.
- Because this goal writes no mods, smoke should verify DTMAPI boots, registries initialize, no existing mod behavior is changed, API registration/status paths work through an internal harness or test-only hook probe, and exit leaves no `DolocTown.exe`.
- Do not use existing player-facing mods as proof that custom animals/monsters/projectiles/drones are implemented unless the proof is specifically about backward compatibility.

Completion standard:

- Only mark complete when the stable 0.4.0 API contracts compile, are documented, have Core registry behavior, have GameBridge status/hook paths or explicit blockers, have tests, and have third-save smoke evidence.
- If any of the four requested stable API groups cannot be made stable in this round, keep the goal incomplete and report the blocker with logs, inspected code paths, and the minimal next step.
- Do not call placeholder-only interfaces "complete".

