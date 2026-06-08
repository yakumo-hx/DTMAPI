using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void TryExerciseCustomEntityApisForSmoke()
        {
            ManifestModel owner = CreateCustomEntitySmokeManifest();
            string animalId = owner.UniqueID + ".Animal";
            string monsterId = owner.UniqueID + ".Monster";
            string attackId = owner.UniqueID + ".Attack";
            string droneId = owner.UniqueID + ".Drone";
            try
            {
                runtime.CustomEntities.RemoveOwner(owner.UniqueID, "pre-smoke cleanup");

                int animalEvents = 0;
                int monsterEvents = 0;
                int attackEvents = 0;
                int droneEvents = 0;
                EventHandler<CustomAnimalLifecycleEventArgs> animalListener = (_, __) => animalEvents++;
                EventHandler<CustomMonsterLifecycleEventArgs> monsterListener = (_, __) => monsterEvents++;
                EventHandler<CustomAttackLifecycleEventArgs> attackListener = (_, __) => attackEvents++;
                EventHandler<CustomDroneLifecycleEventArgs> droneListener = (_, __) => droneEvents++;
                runtime.CustomEntities.AnimalLifecycleChanged += animalListener;
                runtime.CustomEntities.MonsterLifecycleChanged += monsterListener;
                runtime.CustomEntities.AttackLifecycleChanged += attackListener;
                runtime.CustomEntities.DroneLifecycleChanged += droneListener;
                try
                {
                    CustomAnimalRegistrationResult invalidAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = "SmokeAnimal" });
                    EnsureCustomEntitySmoke(!invalidAnimal.Succeeded && invalidAnimal.FailureReason == "invalid-definition", "Invalid animal definition should fail namespaced-ID validation.");

                    CustomAnimalRegistrationResult animal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition
                    {
                        SpeciesId = animalId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Animal"),
                        Description = SmokeText("Contract-only animal used by the internal DTMAPI smoke harness."),
                        Diet = new CustomAnimalDietPolicy { AcceptedItemIds = new[] { "hay" }, UnitsPerFeeding = 1, CanGraze = true },
                        Consumption = new CustomAnimalConsumptionPolicy { HungerIntervalHours = 12, MaxFeedCapacity = 2 },
                        Excrement = new CustomAnimalExcrementPolicy { Enabled = true, IntervalHours = 24, MaxPendingCount = 2, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_excrement", MinStack = 1, MaxStack = 1 } } },
                        Breeding = new CustomAnimalBreedingPolicy { Enabled = true, CompatibleSpeciesIds = new[] { animalId }, CooldownHours = 48, PregnancyOrIncubationHours = 72, OffspringCount = 1, PopulationLimitPerOwner = 8 },
                        HiddenProducts = new[] { new CustomAnimalProductRule { ProductId = "dtmapi_smoke_hidden_product", DisplayName = SmokeText("Smoke Hidden Product"), HiddenUntilReady = true, ProgressPerGameHour = 5, RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_hidden_product", MinStack = 1, MaxStack = 1 } } } },
                        ProduceRules = new[] { new CustomAnimalProductRule { ProductId = "dtmapi_smoke_product", DisplayName = SmokeText("Smoke Product"), ProgressPerGameHour = 10, RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_product", MinStack = 1, MaxStack = 2 } } } },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(animal.Succeeded, "Animal registration failed: " + animal.FailureReason);
                    CustomAnimalRegistrationResult duplicateAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = animalId });
                    EnsureCustomEntitySmoke(!duplicateAnimal.Succeeded && duplicateAnimal.FailureReason == "duplicate-definition-id", "Duplicate animal registration should fail.");

                    CustomAttackRegistrationResult attack = runtime.CustomEntities.RegisterAttack(owner, new CustomAttackDefinition
                    {
                        AttackId = attackId,
                        FactionId = "DTMAPI.Smoke",
                        RelationToPlayer = CustomEntityRelationKind.OwnerAlly,
                        Damage = new CustomDamagePayload { Amount = 1, DamageType = "smoke" },
                        EffectTags = new[] { "contract", "smoke" },
                        Hitbox = new CustomHitboxDefinition { Shape = CustomHitboxShapeKind.Circle, Radius = 0.5 },
                        Trajectory = new CustomTrajectoryDefinition { Kind = CustomEntityMovementKind.FollowTarget, Speed = 3 },
                        Pattern = new CustomBarragePatternDefinition { Kind = CustomAttackPatternKind.Projectile, ProjectileCount = 1, DeterministicRandomSeed = true },
                        Persistence = new CustomEntityPersistencePolicy { Kind = CustomEntityPersistenceKind.RuntimeOnly, SchemaVersion = 1 },
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(attack.Succeeded, "Attack registration failed: " + attack.FailureReason);

                    CustomMonsterRegistrationResult monster = runtime.CustomEntities.RegisterMonster(owner, new CustomMonsterDefinition
                    {
                        MonsterId = monsterId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Monster"),
                        Description = SmokeText("Contract-only monster used by the internal DTMAPI smoke harness."),
                        SpawnRules = new[] { new CustomMonsterSpawnRule { RuleId = owner.UniqueID + ".SpawnRule", RoomTags = new[] { "smoke" }, Probability = 1, MinGroupSize = 1, MaxGroupSize = 1 } },
                        MaxCountPerRoom = 1,
                        FactionId = "DTMAPI.Smoke",
                        Stats = new CustomMonsterStats { MaxHealth = 5, Armor = 0, ContactDamage = 1, MoveSpeed = 1 },
                        Targeting = new CustomMonsterTargetPolicy { AggroRange = 5, RetargetWhenDamaged = true },
                        Movement = new CustomMonsterMovementPolicy { Kind = CustomEntityMovementKind.Wander, PatrolRadius = 3 },
                        AttackSlots = new[] { new CustomMonsterAttackSlot { SlotId = "primary", AttackId = attackId, CooldownSeconds = 1, Range = 4 } },
                        Loot = new[] { new CustomMonsterLootRule { ItemId = "dtmapi_smoke_loot", MinStack = 1, MaxStack = 1, Chance = 1 } },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(monster.Succeeded, "Monster registration failed: " + monster.FailureReason);
                    CustomMonsterRegistrationResult spawnTable = runtime.CustomEntities.RegisterSpawnTable(owner, new CustomMonsterSpawnTableDefinition { SpawnTableId = owner.UniqueID + ".SpawnTable", MonsterIds = new[] { monsterId } });
                    EnsureCustomEntitySmoke(spawnTable.Succeeded, "Monster spawn-table registration failed: " + spawnTable.FailureReason);

                    CustomDroneRegistrationResult drone = runtime.CustomEntities.RegisterDrone(owner, new CustomDroneDefinition
                    {
                        DroneId = droneId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Drone"),
                        Description = SmokeText("Contract-only drone used by the internal DTMAPI smoke harness."),
                        OwnerBinding = new CustomDroneOwnerBindingPolicy { BindToPlayer = true, BindToOwnerMod = true },
                        SupportedModes = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard, CustomDroneBehaviorMode.Attack },
                        EquipmentSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "weapon", DisplayName = SmokeText("Weapon"), AllowedItemTags = new[] { "smoke-weapon" } } },
                        ModuleSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "module", DisplayName = SmokeText("Module"), AllowedItemTags = new[] { "smoke-module" } } },
                        AttackIds = new[] { attackId },
                        Stats = new CustomDroneStats { MaxHealth = 10, MaxShield = 5, Armor = 1, ContactDamage = 1, MoveSpeed = 3 },
                        Energy = new CustomDroneEnergyPolicy { MaxEnergy = 100, EnergyPerSecond = 1, AttackEnergyCost = 5 },
                        Movement = new CustomDroneMovementPolicy { Kind = CustomEntityMovementKind.FollowTarget, FollowDistance = 2, ProviderCanOverride = true },
                        Repair = new CustomDroneRepairPolicy { CanRepair = true, RepairAmountPerItem = 5, RepairItemIds = new[] { "dtmapi_smoke_repair" } },
                        Summon = new CustomDroneSummonPolicy { CanSummonAnywhere = false, CooldownSeconds = 1, MaxActiveInstances = 1 },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(drone.Succeeded, "Drone registration failed: " + drone.FailureReason);

                    CustomAnimalSpawnResult animalSpawn = runtime.CustomEntities.RequestSpawn(owner, new CustomAnimalSpawnRequest { SpeciesId = animalId, Position = SmokePosition() });
                    CustomMonsterSpawnResult monsterSpawn = runtime.CustomEntities.RequestSpawn(owner, new CustomMonsterSpawnRequest { MonsterId = monsterId, Position = SmokePosition() });
                    CustomAttackSpawnResult attackSpawn = runtime.CustomEntities.SpawnProjectile(owner, new CustomAttackSpawnRequest { AttackId = attackId, Origin = SmokePosition() });
                    CustomDroneSummonResult droneSummon = runtime.CustomEntities.RequestSummon(owner, new CustomDroneSummonRequest { DroneId = droneId, Position = SmokePosition() });
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(animalSpawn), "Animal spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(monsterSpawn), "Monster spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(attackSpawn), "Attack spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(droneSummon), "Drone summon should return runtime-creation-blocked.");

                    CustomDroneEquipmentResult equip = runtime.CustomEntities.Equip(owner, new CustomEntityHandle { Family = CustomEntityFamily.Drone, OwnerUniqueId = owner.UniqueID, DefinitionId = droneId, RuntimeId = "smoke-missing" }, new CustomDroneEquipmentRequest { SlotId = "weapon", ItemId = "dtmapi_smoke_weapon" });
                    CustomDroneCommandResult command = runtime.CustomEntities.SetMode(owner, new CustomEntityHandle { Family = CustomEntityFamily.Drone, OwnerUniqueId = owner.UniqueID, DefinitionId = droneId, RuntimeId = "smoke-missing" }, new CustomDroneCommandRequest { Mode = CustomDroneBehaviorMode.Guard, Reason = "smoke" });
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(equip), "Drone equipment request should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(command), "Drone mode request should return runtime-creation-blocked.");

                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Animal snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Monster snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Attack snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Drone snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Animal status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Monster status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Attack status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Drone status should report configured-no-runtime-instance.");
                }
                finally
                {
                    runtime.CustomEntities.AnimalLifecycleChanged -= animalListener;
                    runtime.CustomEntities.MonsterLifecycleChanged -= monsterListener;
                    runtime.CustomEntities.AttackLifecycleChanged -= attackListener;
                    runtime.CustomEntities.DroneLifecycleChanged -= droneListener;
                }

                int removed = runtime.CustomEntities.RemoveOwner(owner.UniqueID, "smoke cleanup");
                EnsureCustomEntitySmoke(removed >= 5, "Smoke cleanup should remove all four definitions and the monster spawn table. removed=" + removed);
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Animal cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Monster cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Attack cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Drone cleanup snapshot should be empty.");

                string summary = "registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=" + removed + "; lifecycleEvents=" + animalEvents + "/" + monsterEvents + "/" + attackEvents + "/" + droneEvents + ".";
                runtime.RuntimeMonitor.Log("Smoke exercise CustomEntityApis OK " + summary);
                runtime.SetHookStatus("Smoke.CustomEntityApis", "verified", "DTMAPI.Core.CustomEntityRegistryService", summary);
            }
            catch (Exception ex)
            {
                runtime.CustomEntities.RemoveOwner(owner.UniqueID, "smoke failure cleanup");
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke custom entity API exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.CustomEntityApis", "failed", "DTMAPI.Core.CustomEntityRegistryService", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static CustomEntityLocalizedText SmokeText(string value)
        {
            return new CustomEntityLocalizedText
            {
                Default = value,
                English = value,
                SimplifiedChinese = value
            };
        }

        private static CustomEntityPersistencePolicy SmokePersistence()
        {
            return new CustomEntityPersistencePolicy
            {
                Kind = CustomEntityPersistenceKind.SaveScoped,
                SchemaVersion = 1,
                RemoveInstancesWhenOwnerMissing = true,
                RestoreRuntimeInstancesOnSaveLoad = false,
                SaveKeys = new[] { new CustomEntitySaveDataKey { Key = "smoke-state", Version = 1, Description = "Smoke-only stable custom entity state key." } }
            };
        }

        private static CustomEntityTickPolicy SmokeTickPolicy()
        {
            return new CustomEntityTickPolicy
            {
                Kind = CustomEntityTickPolicyKind.OneSecond,
                IntervalSeconds = 1,
                DeterministicOrder = true,
                Order = 0
            };
        }

        private static CustomEntityGridPosition SmokePosition()
        {
            return new CustomEntityGridPosition
            {
                RoomId = "smoke-current-room",
                X = 0,
                Y = 0,
                Layer = 0
            };
        }

        private static bool IsRuntimeCreationBlocked(CustomEntityRequestResult result)
        {
            return result != null &&
                !result.Succeeded &&
                result.FailureReason == "runtime-creation-blocked" &&
                result.RuntimeStatus == CustomEntityRuntimeStatus.RuntimeCreationBlocked;
        }

        private static void EnsureCustomEntitySmoke(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static ManifestModel CreateCustomEntitySmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Custom Entity API Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.CustomEntityApiSmokeHarness",
                Type = "Smoke"
            };
        }
    }
}
