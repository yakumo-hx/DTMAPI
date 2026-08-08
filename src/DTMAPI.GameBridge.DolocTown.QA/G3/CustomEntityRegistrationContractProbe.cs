using System;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class CustomEntityRegistrationContractProbe
    {
        private readonly Func<CustomEntityRegistrationFixtureSession> openSession;

        internal CustomEntityRegistrationContractProbe(
            GameBridgeFixtureAccess access,
            Func<CustomEntityRegistrationFixtureSession>? openSession = null)
        {
            if (access == null)
                throw new ArgumentNullException(nameof(access));
            this.openSession = openSession ?? access.OpenCustomEntityRegistrationSession;
        }

        internal string Run()
        {
            var session = openSession();
            int removed;
            try
            {
                string owner = session.OwnerUniqueId;
                string animalId = owner + ".Animal";
                string monsterId = owner + ".Monster";
                string attackId = owner + ".Attack";
                string droneId = owner + ".Drone";

                CustomAnimalRegistrationResult invalid = session.RegisterSpecies(new CustomAnimalSpeciesDefinition { SpeciesId = "NotNamespaced" });
                Ensure(!invalid.Succeeded && invalid.FailureReason == "invalid-definition", "Non-namespaced animal definition was not rejected.");

                CustomAnimalRegistrationResult animal = session.RegisterSpecies(new CustomAnimalSpeciesDefinition
                {
                    SpeciesId = animalId,
                    DisplayName = Text("QA animal"),
                    Diet = new CustomAnimalDietPolicy { AcceptedItemIds = new[] { "hay" }, UnitsPerFeeding = 1 },
                    Excrement = new CustomAnimalExcrementPolicy
                    {
                        Enabled = true,
                        Outputs = new[] { new CustomAnimalItemOutput { ItemId = "qa-poop", MinStack = 1, MaxStack = 1 } }
                    },
                    Breeding = new CustomAnimalBreedingPolicy
                    {
                        Enabled = true,
                        CompatibleSpeciesIds = new[] { animalId },
                        OffspringCount = 1
                    },
                    Persistence = Persistence()
                });
                Ensure(animal.Succeeded, "Animal registration failed: " + animal.FailureReason);
                CustomAnimalRegistrationResult duplicate = session.RegisterSpecies(new CustomAnimalSpeciesDefinition { SpeciesId = animalId });
                Ensure(!duplicate.Succeeded && duplicate.FailureReason == "duplicate-definition-id", "Duplicate animal definition was not rejected.");

                CustomAttackRegistrationResult attack = session.RegisterAttack(new CustomAttackDefinition
                {
                    AttackId = attackId,
                    Damage = new CustomDamagePayload { Amount = 1, DamageType = "qa" },
                    Pattern = new CustomBarragePatternDefinition
                    {
                        Kind = CustomAttackPatternKind.Projectile,
                        ProjectileCount = 1,
                        DeterministicRandomSeed = true
                    }
                });
                Ensure(attack.Succeeded, "Attack registration failed: " + attack.FailureReason);

                CustomMonsterRegistrationResult monster = session.RegisterMonster(new CustomMonsterDefinition
                {
                    MonsterId = monsterId,
                    SpawnRules = new[]
                    {
                        new CustomMonsterSpawnRule { RuleId = owner + ".Spawn", RoomTags = new[] { "qa" }, Probability = 1 }
                    },
                    AttackSlots = new[]
                    {
                        new CustomMonsterAttackSlot { SlotId = "primary", AttackId = attackId, CooldownSeconds = 1, Range = 5 }
                    },
                    Persistence = Persistence()
                });
                Ensure(monster.Succeeded, "Monster registration failed: " + monster.FailureReason);
                CustomMonsterRegistrationResult spawnTable = session.RegisterSpawnTable(new CustomMonsterSpawnTableDefinition
                {
                    SpawnTableId = owner + ".SpawnTable",
                    MonsterIds = new[] { monsterId }
                });
                Ensure(spawnTable.Succeeded, "Spawn-table registration failed: " + spawnTable.FailureReason);

                CustomDroneRegistrationResult drone = session.RegisterDrone(new CustomDroneDefinition
                {
                    DroneId = droneId,
                    SupportedModes = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard },
                    EquipmentSlots = new[]
                    {
                        new CustomDroneEquipmentSlotDefinition { SlotId = "module", AllowedItemTags = new[] { "qa" } }
                    },
                    AttackIds = new[] { attackId },
                    Persistence = Persistence()
                });
                Ensure(drone.Succeeded, "Drone registration failed: " + drone.FailureReason);

                Ensure(session.GetAnimalSnapshot().RegisteredDefinitionCount == 1, "Animal snapshot count was not one.");
                Ensure(session.GetMonsterSnapshot().RegisteredDefinitionCount == 1, "Monster snapshot count was not one.");
                Ensure(session.GetAttackSnapshot().RegisteredDefinitionCount == 1, "Attack snapshot count was not one.");
                Ensure(session.GetDroneSnapshot().RegisteredDefinitionCount == 1, "Drone snapshot count was not one.");
                Ensure(session.GetAnimalStatus().Status == "configured-no-runtime-instance", "Animal status was not configured-no-runtime-instance.");
                Ensure(session.GetMonsterStatus().Status == "configured-no-runtime-instance", "Monster status was not configured-no-runtime-instance.");
                Ensure(session.GetAttackStatus().Status == "configured-no-runtime-instance", "Attack status was not configured-no-runtime-instance.");
                Ensure(session.GetDroneStatus().Status == "configured-no-runtime-instance", "Drone status was not configured-no-runtime-instance.");
            }
            finally
            {
                DisposeWithBoundedRetry(session);
                removed = session.RemovedCount;
                session.VerifyClean();
            }

            return "owner=" + session.OwnerUniqueId + "; registered=animal,monster,attack,drone,spawn-table; runtimeVerbs=0; cleanupRemoved=" + removed + "; cleanupVerified=true; ownerBound=true";
        }

        private static void DisposeWithBoundedRetry(CustomEntityRegistrationFixtureSession session)
        {
            Exception? firstFailure = null;
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    session.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == 2)
                        throw new InvalidOperationException("Owner-bound CustomEntity cleanup failed after two attempts.", firstFailure ?? ex);
                    firstFailure = ex;
                }
            }
        }

        private static CustomEntityLocalizedText Text(string value) => new CustomEntityLocalizedText
        {
            Default = value,
            English = value,
            SimplifiedChinese = value
        };

        private static CustomEntityPersistencePolicy Persistence() => new CustomEntityPersistencePolicy
        {
            Kind = CustomEntityPersistenceKind.RuntimeOnly,
            SchemaVersion = 1,
            RestoreRuntimeInstancesOnSaveLoad = false,
            RemoveInstancesWhenOwnerMissing = true
        };

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
