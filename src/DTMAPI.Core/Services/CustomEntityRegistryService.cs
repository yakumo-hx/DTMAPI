using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Services
{
    public sealed class CustomEntityRegistryService : ICustomAnimalApi, ICustomMonsterApi, ICustomAttackApi, ICustomDroneApi
    {
        private const string RuntimeCreationBlocked = "runtime-creation-blocked";
        private readonly DiagnosticsService diagnostics;
        private readonly object gate = new object();
        private readonly Dictionary<string, DefinitionRecord<CustomAnimalSpeciesDefinition>> animals = new Dictionary<string, DefinitionRecord<CustomAnimalSpeciesDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DefinitionRecord<CustomMonsterDefinition>> monsters = new Dictionary<string, DefinitionRecord<CustomMonsterDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DefinitionRecord<CustomMonsterSpawnTableDefinition>> monsterSpawnTables = new Dictionary<string, DefinitionRecord<CustomMonsterSpawnTableDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DefinitionRecord<CustomAttackDefinition>> attacks = new Dictionary<string, DefinitionRecord<CustomAttackDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DefinitionRecord<CustomDroneDefinition>> drones = new Dictionary<string, DefinitionRecord<CustomDroneDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomAnimalInstanceSnapshot> animalInstances = new Dictionary<string, CustomAnimalInstanceSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomMonsterInstanceSnapshot> monsterInstances = new Dictionary<string, CustomMonsterInstanceSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomAttackInstanceSnapshot> attackInstances = new Dictionary<string, CustomAttackInstanceSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomDroneInstanceSnapshot> droneInstances = new Dictionary<string, CustomDroneInstanceSnapshot>(StringComparer.OrdinalIgnoreCase);
        private int? currentSaveSlot;
        private long saveSession;

        public CustomEntityRegistryService(DiagnosticsService diagnostics)
        {
            this.diagnostics = diagnostics;
        }

        public event EventHandler<CustomAnimalLifecycleEventArgs>? AnimalLifecycleChanged;
        public event EventHandler<CustomMonsterLifecycleEventArgs>? MonsterLifecycleChanged;
        public event EventHandler<CustomAttackLifecycleEventArgs>? AttackLifecycleChanged;
        public event EventHandler<CustomDroneLifecycleEventArgs>? DroneLifecycleChanged;

        event EventHandler<CustomAnimalLifecycleEventArgs>? ICustomAnimalApi.LifecycleChanged
        {
            add { AnimalLifecycleChanged += value; }
            remove { AnimalLifecycleChanged -= value; }
        }

        event EventHandler<CustomMonsterLifecycleEventArgs>? ICustomMonsterApi.LifecycleChanged
        {
            add { MonsterLifecycleChanged += value; }
            remove { MonsterLifecycleChanged -= value; }
        }

        event EventHandler<CustomAttackLifecycleEventArgs>? ICustomAttackApi.LifecycleChanged
        {
            add { AttackLifecycleChanged += value; }
            remove { AttackLifecycleChanged -= value; }
        }

        event EventHandler<CustomDroneLifecycleEventArgs>? ICustomDroneApi.LifecycleChanged
        {
            add { DroneLifecycleChanged += value; }
            remove { DroneLifecycleChanged -= value; }
        }

        public CustomAnimalRegistrationResult RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition)
        {
            string ownerId = OwnerId(owner);
            List<CustomEntityValidationMessage> messages = ValidateDefinition(owner, definition, definition?.SpeciesId, "SpeciesId");
            if (HasErrors(messages))
                return AnimalRegistration(false, ownerId, definition?.SpeciesId ?? string.Empty, "invalid-definition", messages);
            CustomAnimalSpeciesDefinition validDefinition = definition ?? throw new InvalidOperationException("Validated animal definition was null.");

            lock (gate)
            {
                if (animals.ContainsKey(validDefinition.SpeciesId))
                    return AnimalRegistration(false, ownerId, validDefinition.SpeciesId, "duplicate-definition-id", DuplicateMessages(validDefinition.SpeciesId, CustomEntityFamily.Animal));
                animals[validDefinition.SpeciesId] = new DefinitionRecord<CustomAnimalSpeciesDefinition>(ownerId, validDefinition.SpeciesId, validDefinition);
            }

            InvokeProvider(ownerId, validDefinition.SpeciesId, "animal provider OnRegistered", () => validDefinition.Provider?.OnRegistered(BuildBehaviorContext(CustomEntityFamily.Animal, ownerId, validDefinition.SpeciesId)));
            RaiseAnimal(ownerId, validDefinition.SpeciesId, CustomEntityLifecycleKind.Registered, "registered");
            return AnimalRegistration(true, ownerId, validDefinition.SpeciesId, string.Empty, messages);
        }

        public CustomEntityUnregisterResult UnregisterSpecies(IManifest owner, string speciesId)
        {
            return Unregister(animals, animalInstances, CustomEntityFamily.Animal, owner, speciesId, "animal unregister", record =>
            {
                InvokeProvider(record.OwnerUniqueId, record.DefinitionId, "animal provider OnUnregistered", () => record.Definition.Provider?.OnUnregistered(BuildBehaviorContext(CustomEntityFamily.Animal, record.OwnerUniqueId, record.DefinitionId)));
                RaiseAnimal(record.OwnerUniqueId, record.DefinitionId, CustomEntityLifecycleKind.Unregistered, "unregistered");
            });
        }

        public IReadOnlyList<CustomAnimalSpeciesDefinition> GetSpeciesDefinitions(string? ownerUniqueId = null)
        {
            lock (gate)
                return animals.Values.Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId)).Select(r => r.Definition).ToArray();
        }

        public CustomAnimalSpeciesDefinition? GetSpeciesDefinition(string speciesId)
        {
            lock (gate)
                return animals.TryGetValue(speciesId ?? string.Empty, out DefinitionRecord<CustomAnimalSpeciesDefinition> record) ? record.Definition : null;
        }

        public CustomAnimalSpawnResult RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request)
        {
            string ownerId = OwnerId(owner);
            string definitionId = request?.SpeciesId ?? string.Empty;
            DefinitionRecord<CustomAnimalSpeciesDefinition>? record = FindRecord(animals, definitionId);
            IReadOnlyList<CustomEntityValidationMessage> messages = ValidateRequest(owner, definitionId, "SpeciesId", record);
            if (HasErrors(messages))
                return AnimalRequest(false, ownerId, definitionId, "invalid-request", "The custom animal spawn request is invalid.", messages);
            if (!OwnerMatches(record, ownerId))
                return AnimalRequest(false, ownerId, definitionId, "owner-mismatch", "Only the owning mod may request this custom animal species.", new CustomEntityValidationMessage[0]);

            RaiseAnimal(ownerId, definitionId, CustomEntityLifecycleKind.SpawnRequested, RuntimeCreationBlocked);
            return AnimalRequest(false, ownerId, definitionId, RuntimeCreationBlocked, BuildBlockedDetails(CustomEntityFamily.Animal), new CustomEntityValidationMessage[0]);
        }

        public CustomEntityRequestResult RequestRemove(IManifest owner, CustomEntityHandle animalHandle, string reason)
        {
            return RemoveRuntimeHandle(CustomEntityFamily.Animal, owner, animalHandle, reason, animalInstances);
        }

        public IReadOnlyList<CustomAnimalInstanceSnapshot> GetAnimalInstances(string? ownerUniqueId = null)
        {
            lock (gate)
                return animalInstances.Values.Where(s => MatchesOwner(s.Handle.OwnerUniqueId, ownerUniqueId)).ToArray();
        }

        public CustomAnimalInstanceSnapshot? GetAnimalInstance(CustomEntityHandle handle)
        {
            lock (gate)
                return handle != null && animalInstances.TryGetValue(handle.RuntimeId ?? string.Empty, out CustomAnimalInstanceSnapshot snapshot) ? snapshot : null;
        }

        public CustomEntityFamilySnapshot GetAnimalSnapshot(string? ownerUniqueId = null) => BuildSnapshot(CustomEntityFamily.Animal, ownerUniqueId, animals, animalInstances);
        public CustomEntityCapabilityStatus GetAnimalStatus(string? ownerUniqueId = null) => BuildStatus(CustomEntityFamily.Animal, ownerUniqueId, animals.CountForOwner(ownerUniqueId), animalInstances.CountForOwner(ownerUniqueId));
        CustomEntityFamilySnapshot ICustomAnimalApi.GetSnapshot(string? ownerUniqueId) => GetAnimalSnapshot(ownerUniqueId);
        CustomEntityCapabilityStatus ICustomAnimalApi.GetStatus(string? ownerUniqueId) => GetAnimalStatus(ownerUniqueId);

        public CustomMonsterRegistrationResult RegisterMonster(IManifest owner, CustomMonsterDefinition definition)
        {
            string ownerId = OwnerId(owner);
            List<CustomEntityValidationMessage> messages = ValidateDefinition(owner, definition, definition?.MonsterId, "MonsterId");
            if (HasErrors(messages))
                return MonsterRegistration(false, ownerId, definition?.MonsterId ?? string.Empty, "invalid-definition", messages);
            CustomMonsterDefinition validDefinition = definition ?? throw new InvalidOperationException("Validated monster definition was null.");

            lock (gate)
            {
                if (monsters.ContainsKey(validDefinition.MonsterId))
                    return MonsterRegistration(false, ownerId, validDefinition.MonsterId, "duplicate-definition-id", DuplicateMessages(validDefinition.MonsterId, CustomEntityFamily.Monster));
                monsters[validDefinition.MonsterId] = new DefinitionRecord<CustomMonsterDefinition>(ownerId, validDefinition.MonsterId, validDefinition);
            }

            InvokeProvider(ownerId, validDefinition.MonsterId, "monster provider OnRegistered", () => validDefinition.Provider?.OnRegistered(BuildBehaviorContext(CustomEntityFamily.Monster, ownerId, validDefinition.MonsterId)));
            RaiseMonster(ownerId, validDefinition.MonsterId, CustomEntityLifecycleKind.Registered, "registered");
            return MonsterRegistration(true, ownerId, validDefinition.MonsterId, string.Empty, messages);
        }

        public CustomMonsterRegistrationResult RegisterSpawnTable(IManifest owner, CustomMonsterSpawnTableDefinition spawnTable)
        {
            string ownerId = OwnerId(owner);
            List<CustomEntityValidationMessage> messages = ValidateDefinition(owner, spawnTable, spawnTable?.SpawnTableId, "SpawnTableId");
            if (HasErrors(messages))
                return MonsterRegistration(false, ownerId, spawnTable?.SpawnTableId ?? string.Empty, "invalid-definition", messages);
            CustomMonsterSpawnTableDefinition validSpawnTable = spawnTable ?? throw new InvalidOperationException("Validated monster spawn table was null.");

            lock (gate)
            {
                if (monsterSpawnTables.ContainsKey(validSpawnTable.SpawnTableId))
                    return MonsterRegistration(false, ownerId, validSpawnTable.SpawnTableId, "duplicate-spawn-table-id", DuplicateMessages(validSpawnTable.SpawnTableId, CustomEntityFamily.Monster));
                monsterSpawnTables[validSpawnTable.SpawnTableId] = new DefinitionRecord<CustomMonsterSpawnTableDefinition>(ownerId, validSpawnTable.SpawnTableId, validSpawnTable);
            }

            RaiseMonster(ownerId, validSpawnTable.SpawnTableId, CustomEntityLifecycleKind.Registered, "spawn-table-registered");
            return MonsterRegistration(true, ownerId, validSpawnTable.SpawnTableId, string.Empty, messages);
        }

        public CustomEntityUnregisterResult UnregisterMonster(IManifest owner, string monsterId)
        {
            return Unregister(monsters, monsterInstances, CustomEntityFamily.Monster, owner, monsterId, "monster unregister", record =>
            {
                InvokeProvider(record.OwnerUniqueId, record.DefinitionId, "monster provider OnUnregistered", () => record.Definition.Provider?.OnUnregistered(BuildBehaviorContext(CustomEntityFamily.Monster, record.OwnerUniqueId, record.DefinitionId)));
                RaiseMonster(record.OwnerUniqueId, record.DefinitionId, CustomEntityLifecycleKind.Unregistered, "unregistered");
            });
        }

        public IReadOnlyList<CustomMonsterDefinition> GetMonsterDefinitions(string? ownerUniqueId = null)
        {
            lock (gate)
                return monsters.Values.Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId)).Select(r => r.Definition).ToArray();
        }

        public CustomMonsterDefinition? GetMonsterDefinition(string monsterId)
        {
            lock (gate)
                return monsters.TryGetValue(monsterId ?? string.Empty, out DefinitionRecord<CustomMonsterDefinition> record) ? record.Definition : null;
        }

        public CustomMonsterSpawnResult RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request)
        {
            string ownerId = OwnerId(owner);
            string definitionId = request?.MonsterId ?? string.Empty;
            DefinitionRecord<CustomMonsterDefinition>? record = FindRecord(monsters, definitionId);
            IReadOnlyList<CustomEntityValidationMessage> messages = ValidateRequest(owner, definitionId, "MonsterId", record);
            if (HasErrors(messages))
                return MonsterRequest(false, ownerId, definitionId, "invalid-request", "The custom monster spawn request is invalid.", messages);
            if (!OwnerMatches(record, ownerId))
                return MonsterRequest(false, ownerId, definitionId, "owner-mismatch", "Only the owning mod may request this custom monster.", new CustomEntityValidationMessage[0]);

            RaiseMonster(ownerId, definitionId, CustomEntityLifecycleKind.SpawnRequested, RuntimeCreationBlocked);
            return MonsterRequest(false, ownerId, definitionId, RuntimeCreationBlocked, BuildBlockedDetails(CustomEntityFamily.Monster), new CustomEntityValidationMessage[0]);
        }

        public CustomEntityRequestResult RequestDespawn(IManifest owner, CustomEntityHandle monsterHandle, string reason)
        {
            return RemoveRuntimeHandle(CustomEntityFamily.Monster, owner, monsterHandle, reason, monsterInstances);
        }

        public IReadOnlyList<CustomMonsterInstanceSnapshot> GetMonsterInstances(string? ownerUniqueId = null)
        {
            lock (gate)
                return monsterInstances.Values.Where(s => MatchesOwner(s.Handle.OwnerUniqueId, ownerUniqueId)).ToArray();
        }

        public CustomMonsterInstanceSnapshot? GetMonsterInstance(CustomEntityHandle handle)
        {
            lock (gate)
                return handle != null && monsterInstances.TryGetValue(handle.RuntimeId ?? string.Empty, out CustomMonsterInstanceSnapshot snapshot) ? snapshot : null;
        }

        public CustomEntityFamilySnapshot GetMonsterSnapshot(string? ownerUniqueId = null) => BuildSnapshot(CustomEntityFamily.Monster, ownerUniqueId, monsters, monsterInstances);
        public CustomEntityCapabilityStatus GetMonsterStatus(string? ownerUniqueId = null) => BuildStatus(CustomEntityFamily.Monster, ownerUniqueId, monsters.CountForOwner(ownerUniqueId), monsterInstances.CountForOwner(ownerUniqueId));
        CustomEntityFamilySnapshot ICustomMonsterApi.GetSnapshot(string? ownerUniqueId) => GetMonsterSnapshot(ownerUniqueId);
        CustomEntityCapabilityStatus ICustomMonsterApi.GetStatus(string? ownerUniqueId) => GetMonsterStatus(ownerUniqueId);

        public CustomAttackRegistrationResult RegisterAttack(IManifest owner, CustomAttackDefinition definition)
        {
            string ownerId = OwnerId(owner);
            List<CustomEntityValidationMessage> messages = ValidateDefinition(owner, definition, definition?.AttackId, "AttackId");
            if (definition != null && definition.Damage.Amount <= 0)
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Warning, "damage-not-positive", "Custom attack damage is not positive; the attack may only be useful for effects."));
            if (HasErrors(messages))
                return AttackRegistration(false, ownerId, definition?.AttackId ?? string.Empty, "invalid-definition", messages);
            CustomAttackDefinition validDefinition = definition ?? throw new InvalidOperationException("Validated attack definition was null.");

            lock (gate)
            {
                if (attacks.ContainsKey(validDefinition.AttackId))
                    return AttackRegistration(false, ownerId, validDefinition.AttackId, "duplicate-definition-id", DuplicateMessages(validDefinition.AttackId, CustomEntityFamily.Attack));
                attacks[validDefinition.AttackId] = new DefinitionRecord<CustomAttackDefinition>(ownerId, validDefinition.AttackId, validDefinition);
            }

            InvokeProvider(ownerId, validDefinition.AttackId, "attack provider OnRegistered", () => validDefinition.Provider?.OnRegistered(BuildBehaviorContext(CustomEntityFamily.Attack, ownerId, validDefinition.AttackId)));
            RaiseAttack(ownerId, validDefinition.AttackId, CustomEntityLifecycleKind.Registered, "registered");
            return AttackRegistration(true, ownerId, validDefinition.AttackId, string.Empty, messages);
        }

        public CustomEntityUnregisterResult UnregisterAttack(IManifest owner, string attackId)
        {
            return Unregister(attacks, attackInstances, CustomEntityFamily.Attack, owner, attackId, "attack unregister", record =>
            {
                InvokeProvider(record.OwnerUniqueId, record.DefinitionId, "attack provider OnUnregistered", () => record.Definition.Provider?.OnUnregistered(BuildBehaviorContext(CustomEntityFamily.Attack, record.OwnerUniqueId, record.DefinitionId)));
                RaiseAttack(record.OwnerUniqueId, record.DefinitionId, CustomEntityLifecycleKind.Unregistered, "unregistered");
            });
        }

        public IReadOnlyList<CustomAttackDefinition> GetAttackDefinitions(string? ownerUniqueId = null)
        {
            lock (gate)
                return attacks.Values.Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId)).Select(r => r.Definition).ToArray();
        }

        public CustomAttackDefinition? GetAttackDefinition(string attackId)
        {
            lock (gate)
                return attacks.TryGetValue(attackId ?? string.Empty, out DefinitionRecord<CustomAttackDefinition> record) ? record.Definition : null;
        }

        public CustomAttackSpawnResult SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request)
        {
            return RequestAttack(owner, request, CustomEntityLifecycleKind.SpawnRequested, "projectile-spawn-requested");
        }

        public CustomAttackSpawnResult ExecuteAttack(IManifest owner, CustomAttackSpawnRequest request)
        {
            return RequestAttack(owner, request, CustomEntityLifecycleKind.SpawnRequested, "attack-execute-requested");
        }

        public CustomEntityRequestResult RequestExpire(IManifest owner, CustomEntityHandle attackHandle, string reason)
        {
            return RemoveRuntimeHandle(CustomEntityFamily.Attack, owner, attackHandle, reason, attackInstances);
        }

        public IReadOnlyList<CustomAttackInstanceSnapshot> GetActiveAttacks(string? ownerUniqueId = null)
        {
            lock (gate)
                return attackInstances.Values.Where(s => MatchesOwner(s.Handle.OwnerUniqueId, ownerUniqueId)).ToArray();
        }

        public CustomAttackInstanceSnapshot? GetAttackInstance(CustomEntityHandle handle)
        {
            lock (gate)
                return handle != null && attackInstances.TryGetValue(handle.RuntimeId ?? string.Empty, out CustomAttackInstanceSnapshot snapshot) ? snapshot : null;
        }

        public CustomEntityFamilySnapshot GetAttackSnapshot(string? ownerUniqueId = null) => BuildSnapshot(CustomEntityFamily.Attack, ownerUniqueId, attacks, attackInstances);
        public CustomEntityCapabilityStatus GetAttackStatus(string? ownerUniqueId = null) => BuildStatus(CustomEntityFamily.Attack, ownerUniqueId, attacks.CountForOwner(ownerUniqueId), attackInstances.CountForOwner(ownerUniqueId));
        CustomEntityFamilySnapshot ICustomAttackApi.GetSnapshot(string? ownerUniqueId) => GetAttackSnapshot(ownerUniqueId);
        CustomEntityCapabilityStatus ICustomAttackApi.GetStatus(string? ownerUniqueId) => GetAttackStatus(ownerUniqueId);

        public CustomDroneRegistrationResult RegisterDrone(IManifest owner, CustomDroneDefinition definition)
        {
            string ownerId = OwnerId(owner);
            List<CustomEntityValidationMessage> messages = ValidateDefinition(owner, definition, definition?.DroneId, "DroneId");
            if (HasErrors(messages))
                return DroneRegistration(false, ownerId, definition?.DroneId ?? string.Empty, "invalid-definition", messages);
            CustomDroneDefinition validDefinition = definition ?? throw new InvalidOperationException("Validated drone definition was null.");

            lock (gate)
            {
                if (drones.ContainsKey(validDefinition.DroneId))
                    return DroneRegistration(false, ownerId, validDefinition.DroneId, "duplicate-definition-id", DuplicateMessages(validDefinition.DroneId, CustomEntityFamily.Drone));
                drones[validDefinition.DroneId] = new DefinitionRecord<CustomDroneDefinition>(ownerId, validDefinition.DroneId, validDefinition);
            }

            InvokeProvider(ownerId, validDefinition.DroneId, "drone provider OnRegistered", () => validDefinition.Provider?.OnRegistered(BuildBehaviorContext(CustomEntityFamily.Drone, ownerId, validDefinition.DroneId)));
            RaiseDrone(ownerId, validDefinition.DroneId, CustomEntityLifecycleKind.Registered, "registered");
            return DroneRegistration(true, ownerId, validDefinition.DroneId, string.Empty, messages);
        }

        public CustomEntityUnregisterResult UnregisterDrone(IManifest owner, string droneId)
        {
            return Unregister(drones, droneInstances, CustomEntityFamily.Drone, owner, droneId, "drone unregister", record =>
            {
                InvokeProvider(record.OwnerUniqueId, record.DefinitionId, "drone provider OnUnregistered", () => record.Definition.Provider?.OnUnregistered(BuildBehaviorContext(CustomEntityFamily.Drone, record.OwnerUniqueId, record.DefinitionId)));
                RaiseDrone(record.OwnerUniqueId, record.DefinitionId, CustomEntityLifecycleKind.Unregistered, "unregistered");
            });
        }

        public IReadOnlyList<CustomDroneDefinition> GetDroneDefinitions(string? ownerUniqueId = null)
        {
            lock (gate)
                return drones.Values.Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId)).Select(r => r.Definition).ToArray();
        }

        public CustomDroneDefinition? GetDroneDefinition(string droneId)
        {
            lock (gate)
                return drones.TryGetValue(droneId ?? string.Empty, out DefinitionRecord<CustomDroneDefinition> record) ? record.Definition : null;
        }

        public CustomDroneSummonResult RequestSummon(IManifest owner, CustomDroneSummonRequest request)
        {
            string ownerId = OwnerId(owner);
            string definitionId = request?.DroneId ?? string.Empty;
            DefinitionRecord<CustomDroneDefinition>? record = FindRecord(drones, definitionId);
            IReadOnlyList<CustomEntityValidationMessage> messages = ValidateRequest(owner, definitionId, "DroneId", record);
            if (HasErrors(messages))
                return DroneRequest(false, ownerId, definitionId, "invalid-request", "The custom drone summon request is invalid.", messages);
            if (!OwnerMatches(record, ownerId))
                return DroneRequest(false, ownerId, definitionId, "owner-mismatch", "Only the owning mod may summon this custom drone.", new CustomEntityValidationMessage[0]);

            RaiseDrone(ownerId, definitionId, CustomEntityLifecycleKind.SpawnRequested, RuntimeCreationBlocked);
            return DroneRequest(false, ownerId, definitionId, RuntimeCreationBlocked, BuildBlockedDetails(CustomEntityFamily.Drone), new CustomEntityValidationMessage[0]);
        }

        public CustomEntityRequestResult RequestDismiss(IManifest owner, CustomEntityHandle droneHandle, string reason)
        {
            return RemoveRuntimeHandle(CustomEntityFamily.Drone, owner, droneHandle, reason, droneInstances);
        }

        public CustomDroneEquipmentResult Equip(IManifest owner, CustomEntityHandle droneHandle, CustomDroneEquipmentRequest request)
        {
            CustomEntityRequestResult result = RemoveRuntimeHandle(CustomEntityFamily.Drone, owner, droneHandle, "equipment request cannot target a native drone yet", droneInstances);
            return new CustomDroneEquipmentResult
            {
                Succeeded = false,
                Family = CustomEntityFamily.Drone,
                OwnerUniqueId = OwnerId(owner),
                DefinitionId = droneHandle?.DefinitionId ?? string.Empty,
                Handle = droneHandle,
                FailureReason = result.FailureReason == "runtime-instance-not-found" ? RuntimeCreationBlocked : result.FailureReason,
                Details = BuildBlockedDetails(CustomEntityFamily.Drone),
                RuntimeStatus = CustomEntityRuntimeStatus.RuntimeCreationBlocked,
                Messages = result.Messages,
                SlotId = request?.SlotId ?? string.Empty,
                ItemId = request?.ItemId ?? string.Empty
            };
        }

        public CustomDroneCommandResult SetMode(IManifest owner, CustomEntityHandle droneHandle, CustomDroneCommandRequest request)
        {
            CustomEntityRequestResult result = RemoveRuntimeHandle(CustomEntityFamily.Drone, owner, droneHandle, "mode request cannot target a native drone yet", droneInstances);
            return new CustomDroneCommandResult
            {
                Succeeded = false,
                Family = CustomEntityFamily.Drone,
                OwnerUniqueId = OwnerId(owner),
                DefinitionId = droneHandle?.DefinitionId ?? string.Empty,
                Handle = droneHandle,
                FailureReason = result.FailureReason == "runtime-instance-not-found" ? RuntimeCreationBlocked : result.FailureReason,
                Details = BuildBlockedDetails(CustomEntityFamily.Drone),
                RuntimeStatus = CustomEntityRuntimeStatus.RuntimeCreationBlocked,
                Messages = result.Messages,
                Mode = request?.Mode ?? CustomDroneBehaviorMode.Follow
            };
        }

        public IReadOnlyList<CustomDroneInstanceSnapshot> GetDroneInstances(string? ownerUniqueId = null)
        {
            lock (gate)
                return droneInstances.Values.Where(s => MatchesOwner(s.Handle.OwnerUniqueId, ownerUniqueId)).ToArray();
        }

        public CustomDroneInstanceSnapshot? GetDroneInstance(CustomEntityHandle handle)
        {
            lock (gate)
                return handle != null && droneInstances.TryGetValue(handle.RuntimeId ?? string.Empty, out CustomDroneInstanceSnapshot snapshot) ? snapshot : null;
        }

        public CustomEntityFamilySnapshot GetDroneSnapshot(string? ownerUniqueId = null) => BuildSnapshot(CustomEntityFamily.Drone, ownerUniqueId, drones, droneInstances);
        public CustomEntityCapabilityStatus GetDroneStatus(string? ownerUniqueId = null) => BuildStatus(CustomEntityFamily.Drone, ownerUniqueId, drones.CountForOwner(ownerUniqueId), droneInstances.CountForOwner(ownerUniqueId));
        CustomEntityFamilySnapshot ICustomDroneApi.GetSnapshot(string? ownerUniqueId) => GetDroneSnapshot(ownerUniqueId);
        CustomEntityCapabilityStatus ICustomDroneApi.GetStatus(string? ownerUniqueId) => GetDroneStatus(ownerUniqueId);

        public int RemoveOwner(string ownerUniqueId, string reason)
        {
            if (string.IsNullOrWhiteSpace(ownerUniqueId))
                return 0;

            List<CustomEntityLifecycleEventArgs> events = new List<CustomEntityLifecycleEventArgs>();
            int removed;
            lock (gate)
            {
                removed = RemoveOwnerFrom(animals, ownerUniqueId, events, CustomEntityFamily.Animal) +
                    RemoveOwnerFrom(monsters, ownerUniqueId, events, CustomEntityFamily.Monster) +
                    RemoveOwnerFrom(monsterSpawnTables, ownerUniqueId, events, CustomEntityFamily.Monster) +
                    RemoveOwnerFrom(attacks, ownerUniqueId, events, CustomEntityFamily.Attack) +
                    RemoveOwnerFrom(drones, ownerUniqueId, events, CustomEntityFamily.Drone) +
                    RemoveOwnerInstances(animalInstances, ownerUniqueId) +
                    RemoveOwnerInstances(monsterInstances, ownerUniqueId) +
                    RemoveOwnerInstances(attackInstances, ownerUniqueId) +
                    RemoveOwnerInstances(droneInstances, ownerUniqueId);
            }

            foreach (CustomEntityLifecycleEventArgs e in events)
                RaiseByFamily(e.Family, e.OwnerUniqueId, e.DefinitionId, CustomEntityLifecycleKind.Unregistered, reason);
            return removed;
        }

        public void BeginSaveSession(int? saveSlot, bool isNewGame)
        {
            currentSaveSlot = saveSlot;
            saveSession++;
            ClearRuntimeInstances(isNewGame ? "new-game-save-boundary" : "save-loaded-boundary");
        }

        public int ClearRuntimeInstances(string reason)
        {
            int count;
            lock (gate)
            {
                count = animalInstances.Count + monsterInstances.Count + attackInstances.Count + droneInstances.Count;
                animalInstances.Clear();
                monsterInstances.Clear();
                attackInstances.Clear();
                droneInstances.Clear();
            }
            return count;
        }

        public int PruneOrphanedSaveState(IReadOnlyCollection<string> loadedOwnerIds)
        {
            return 0;
        }

        private CustomAttackSpawnResult RequestAttack(IManifest owner, CustomAttackSpawnRequest request, CustomEntityLifecycleKind kind, string reason)
        {
            string ownerId = OwnerId(owner);
            string definitionId = request?.AttackId ?? string.Empty;
            DefinitionRecord<CustomAttackDefinition>? record = FindRecord(attacks, definitionId);
            IReadOnlyList<CustomEntityValidationMessage> messages = ValidateRequest(owner, definitionId, "AttackId", record);
            if (HasErrors(messages))
                return AttackRequest(false, ownerId, definitionId, "invalid-request", "The custom attack request is invalid.", messages);
            if (!OwnerMatches(record, ownerId))
                return AttackRequest(false, ownerId, definitionId, "owner-mismatch", "Only the owning mod may request this custom attack.", new CustomEntityValidationMessage[0]);

            RaiseAttack(ownerId, definitionId, kind, reason);
            return AttackRequest(false, ownerId, definitionId, RuntimeCreationBlocked, BuildBlockedDetails(CustomEntityFamily.Attack), new CustomEntityValidationMessage[0]);
        }

        private CustomEntityRequestResult RemoveRuntimeHandle<TSnapshot>(CustomEntityFamily family, IManifest owner, CustomEntityHandle handle, string reason, Dictionary<string, TSnapshot> instances)
            where TSnapshot : class
        {
            string ownerId = OwnerId(owner);
            if (handle == null || handle.Family != family || string.IsNullOrWhiteSpace(handle.RuntimeId))
                return Request(false, family, ownerId, handle?.DefinitionId ?? string.Empty, "invalid-handle", "The custom entity handle is empty or belongs to another family.", null, CustomEntityRuntimeStatus.Failed, InvalidHandleMessages());
            if (!MatchesOwner(handle.OwnerUniqueId, ownerId))
                return Request(false, family, ownerId, handle.DefinitionId, "owner-mismatch", "Only the owner that created the custom entity handle may remove it.", handle, CustomEntityRuntimeStatus.Failed, new CustomEntityValidationMessage[0]);

            bool removed;
            lock (gate)
                removed = instances.Remove(handle.RuntimeId);
            if (removed)
            {
                RaiseByFamily(family, ownerId, handle.DefinitionId, CustomEntityLifecycleKind.Removed, reason);
                return Request(true, family, ownerId, handle.DefinitionId, string.Empty, reason ?? string.Empty, handle, CustomEntityRuntimeStatus.Removed, new CustomEntityValidationMessage[0]);
            }

            return Request(false, family, ownerId, handle.DefinitionId, "runtime-instance-not-found", "No active runtime instance is known for this handle; the 0.4.0 adapter currently exposes configured definitions before native runtime creation.", handle, CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance, new CustomEntityValidationMessage[0]);
        }

        private CustomEntityUnregisterResult Unregister<TDefinition, TSnapshot>(
            Dictionary<string, DefinitionRecord<TDefinition>> definitions,
            Dictionary<string, TSnapshot> instances,
            CustomEntityFamily family,
            IManifest owner,
            string definitionId,
            string reason,
            Action<DefinitionRecord<TDefinition>> afterRemove)
            where TDefinition : class
            where TSnapshot : class
        {
            string ownerId = OwnerId(owner);
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return new CustomEntityUnregisterResult
                {
                    Succeeded = false,
                    Family = family,
                    OwnerUniqueId = ownerId,
                    DefinitionId = definitionId ?? string.Empty,
                    FailureReason = "missing-definition-id",
                    Messages = new[] { new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "missing-definition-id", "A definition ID is required.") }
                };
            }

            DefinitionRecord<TDefinition>? record;
            int removedInstances = 0;
            lock (gate)
            {
                if (!definitions.TryGetValue(definitionId, out record))
                {
                    return new CustomEntityUnregisterResult
                    {
                        Succeeded = false,
                        Family = family,
                        OwnerUniqueId = ownerId,
                        DefinitionId = definitionId,
                        FailureReason = "definition-not-found"
                    };
                }

                if (!MatchesOwner(record.OwnerUniqueId, ownerId))
                {
                    return new CustomEntityUnregisterResult
                    {
                        Succeeded = false,
                        Family = family,
                        OwnerUniqueId = ownerId,
                        DefinitionId = definitionId,
                        FailureReason = "owner-mismatch"
                    };
                }

                definitions.Remove(definitionId);
                removedInstances = RemoveInstancesForDefinition(instances, record.OwnerUniqueId, definitionId);
            }

            afterRemove(record);
            return new CustomEntityUnregisterResult
            {
                Succeeded = true,
                Family = family,
                OwnerUniqueId = ownerId,
                DefinitionId = definitionId,
                RemovedRuntimeInstanceCount = removedInstances
            };
        }

        private CustomEntityFamilySnapshot BuildSnapshot<TDefinition, TSnapshot>(
            CustomEntityFamily family,
            string? ownerUniqueId,
            Dictionary<string, DefinitionRecord<TDefinition>> definitions,
            Dictionary<string, TSnapshot> instances)
            where TDefinition : class
            where TSnapshot : class
        {
            lock (gate)
            {
                string[] definitionIds = definitions.Values
                    .Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId))
                    .Select(r => r.DefinitionId)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                CustomEntityHandle[] handles = GetHandles(instances, ownerUniqueId);
                return new CustomEntityFamilySnapshot
                {
                    Family = family,
                    OwnerUniqueId = ownerUniqueId ?? string.Empty,
                    RegisteredDefinitionCount = definitionIds.Length,
                    ActiveRuntimeInstanceCount = handles.Length,
                    SaveStateRecordCount = 0,
                    RuntimeStatus = handles.Length > 0 ? CustomEntityRuntimeStatus.Active : definitionIds.Length > 0 ? CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance : CustomEntityRuntimeStatus.Unknown,
                    StatusDetails = definitionIds.Length > 0 && handles.Length == 0 ? BuildBlockedDetails(family) : string.Empty,
                    DefinitionIds = definitionIds,
                    RuntimeHandles = handles
                };
            }
        }

        private static CustomEntityCapabilityStatus BuildStatus(CustomEntityFamily family, string? ownerUniqueId, int definitions, int instances)
        {
            string status = instances > 0 ? "active" : definitions > 0 ? "configured-no-runtime-instance" : "empty";
            return new CustomEntityCapabilityStatus
            {
                Family = family,
                Status = status,
                FailureReason = definitions > 0 && instances == 0 ? RuntimeCreationBlocked : string.Empty,
                Details = definitions > 0 && instances == 0 ? BuildBlockedDetails(family) : "No custom " + family + " definitions are registered.",
                RegisteredDefinitionCount = definitions,
                ActiveRuntimeInstanceCount = instances
            };
        }

        private static CustomEntityHandle[] GetHandles<TSnapshot>(Dictionary<string, TSnapshot> instances, string? ownerUniqueId)
            where TSnapshot : class
        {
            return instances.Values
                .Select(TryGetHandle)
                .Where(h => h != null && MatchesOwner(h.OwnerUniqueId, ownerUniqueId))
                .Select(h => h!)
                .OrderBy(h => h.RuntimeId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static CustomEntityHandle? TryGetHandle<TSnapshot>(TSnapshot snapshot)
            where TSnapshot : class
        {
            switch (snapshot)
            {
                case CustomAnimalInstanceSnapshot animal:
                    return animal.Handle;
                case CustomMonsterInstanceSnapshot monster:
                    return monster.Handle;
                case CustomAttackInstanceSnapshot attack:
                    return attack.Handle;
                case CustomDroneInstanceSnapshot drone:
                    return drone.Handle;
                default:
                    return null;
            }
        }

        private static int RemoveOwnerFrom<TDefinition>(Dictionary<string, DefinitionRecord<TDefinition>> definitions, string ownerUniqueId, List<CustomEntityLifecycleEventArgs> events, CustomEntityFamily family)
            where TDefinition : class
        {
            string[] ids = definitions.Values.Where(r => MatchesOwner(r.OwnerUniqueId, ownerUniqueId)).Select(r => r.DefinitionId).ToArray();
            foreach (string id in ids)
            {
                definitions.Remove(id);
                events.Add(new CustomEntityLifecycleEventArgs
                {
                    Family = family,
                    Kind = CustomEntityLifecycleKind.Unregistered,
                    OwnerUniqueId = ownerUniqueId,
                    DefinitionId = id,
                    Reason = "owner-removed"
                });
            }
            return ids.Length;
        }

        private static int RemoveOwnerInstances<TSnapshot>(Dictionary<string, TSnapshot> instances, string ownerUniqueId)
            where TSnapshot : class
        {
            string[] ids = instances.Where(p => MatchesOwner(TryGetHandle(p.Value)?.OwnerUniqueId ?? string.Empty, ownerUniqueId)).Select(p => p.Key).ToArray();
            foreach (string id in ids)
                instances.Remove(id);
            return ids.Length;
        }

        private static int RemoveInstancesForDefinition<TSnapshot>(Dictionary<string, TSnapshot> instances, string ownerUniqueId, string definitionId)
            where TSnapshot : class
        {
            string[] ids = instances
                .Where(p =>
                {
                    CustomEntityHandle? handle = TryGetHandle(p.Value);
                    return handle != null && MatchesOwner(handle.OwnerUniqueId, ownerUniqueId) && string.Equals(handle.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase);
                })
                .Select(p => p.Key)
                .ToArray();
            foreach (string id in ids)
                instances.Remove(id);
            return ids.Length;
        }

        private List<CustomEntityValidationMessage> ValidateDefinition<TDefinition>(IManifest owner, TDefinition? definition, string? definitionId, string idField)
            where TDefinition : class
        {
            var messages = new List<CustomEntityValidationMessage>();
            ValidateOwner(owner, messages);
            if (definition == null)
            {
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "missing-definition", "A definition object is required.") { Field = idField });
                return messages;
            }
            ValidateNamespacedId(definitionId, idField, messages);
            return messages;
        }

        private IReadOnlyList<CustomEntityValidationMessage> ValidateRequest<TDefinition>(IManifest owner, string definitionId, string idField, DefinitionRecord<TDefinition>? record)
            where TDefinition : class
        {
            var messages = new List<CustomEntityValidationMessage>();
            ValidateOwner(owner, messages);
            ValidateNamespacedId(definitionId, idField, messages);
            if (!HasErrors(messages) && record == null)
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "definition-not-found", "No registered definition exists for " + definitionId + ".") { Field = idField });
            return messages;
        }

        private static void ValidateOwner(IManifest owner, List<CustomEntityValidationMessage> messages)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.UniqueID))
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "missing-owner", "A valid owner manifest is required.") { Field = "Owner" });
        }

        private static void ValidateNamespacedId(string? value, string field, List<CustomEntityValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "missing-definition-id", field + " is required.") { Field = field });
                return;
            }
            if (value.Any(char.IsWhiteSpace))
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "definition-id-contains-whitespace", field + " must not contain whitespace.") { Field = field });
            string id = value ?? string.Empty;
            if (id.IndexOf('.') < 0 && id.IndexOf(':') < 0)
                messages.Add(new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "definition-id-not-namespaced", field + " must be namespaced, for example Author.Mod.Entity.") { Field = field });
        }

        private static bool HasErrors(IEnumerable<CustomEntityValidationMessage> messages)
        {
            return messages.Any(m => m.Severity == CustomEntityValidationSeverity.Error);
        }

        private static IReadOnlyList<CustomEntityValidationMessage> DuplicateMessages(string id, CustomEntityFamily family)
        {
            return new[]
            {
                new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "duplicate-definition-id", "A custom " + family + " definition with ID " + id + " is already registered.")
            };
        }

        private static IReadOnlyList<CustomEntityValidationMessage> InvalidHandleMessages()
        {
            return new[]
            {
                new CustomEntityValidationMessage(CustomEntityValidationSeverity.Error, "invalid-handle", "The stable runtime handle is empty or belongs to another custom entity family.")
            };
        }

        private static string OwnerId(IManifest owner)
        {
            return owner?.UniqueID ?? string.Empty;
        }

        private static bool MatchesOwner(string ownerUniqueId, string? requestedOwnerUniqueId)
        {
            return string.IsNullOrWhiteSpace(requestedOwnerUniqueId) || string.Equals(ownerUniqueId, requestedOwnerUniqueId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool OwnerMatches<TDefinition>(DefinitionRecord<TDefinition>? record, string ownerUniqueId)
            where TDefinition : class
        {
            return record != null && MatchesOwner(record.OwnerUniqueId, ownerUniqueId);
        }

        private DefinitionRecord<TDefinition>? FindRecord<TDefinition>(Dictionary<string, DefinitionRecord<TDefinition>> definitions, string definitionId)
            where TDefinition : class
        {
            lock (gate)
                return definitions.TryGetValue(definitionId ?? string.Empty, out DefinitionRecord<TDefinition> record) ? record : null;
        }

        private static string BuildBlockedDetails(CustomEntityFamily family)
        {
            switch (family)
            {
                case CustomEntityFamily.Animal:
                    return "Definition registry is active, but native AnimalInfo/proto, room/home, food, excrement, breeding, and product adapters are not verified enough to create a Doloc Town runtime animal.";
                case CustomEntityFamily.Monster:
                    return "Definition registry is active, but native MonsterController, spawn group, AI, movement, attack, damage, and drop adapters are not verified enough to create a Doloc Town runtime monster.";
                case CustomEntityFamily.Attack:
                    return "Definition registry is active, but native BulletManager/BulletFactory, hitbox, collision, and damage ownership adapters are not verified enough to create a Doloc Town runtime attack.";
                case CustomEntityFamily.Drone:
                    return "Definition registry is active, but native DroneController, weapon, equipment, movement, and persistence adapters are not verified enough to create a Doloc Town runtime drone.";
                default:
                    return "Definition registry is active, but no runtime creation adapter is verified.";
            }
        }

        private CustomEntityBehaviorContext BuildBehaviorContext(CustomEntityFamily family, string ownerUniqueId, string definitionId)
        {
            return new CustomEntityBehaviorContext
            {
                Family = family,
                OwnerUniqueId = ownerUniqueId,
                DefinitionId = definitionId,
                SaveSlot = currentSaveSlot,
                UpdateTick = (ulong)Math.Max(0, saveSession)
            };
        }

        private void InvokeProvider(string ownerUniqueId, string definitionId, string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                diagnostics.RecordError(ownerUniqueId, "Custom entity " + operation + " failed for " + definitionId + ".", ex.ToString());
            }
        }

        private void RaiseByFamily(CustomEntityFamily family, string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            switch (family)
            {
                case CustomEntityFamily.Animal:
                    RaiseAnimal(ownerUniqueId, definitionId, kind, reason);
                    break;
                case CustomEntityFamily.Monster:
                    RaiseMonster(ownerUniqueId, definitionId, kind, reason);
                    break;
                case CustomEntityFamily.Attack:
                    RaiseAttack(ownerUniqueId, definitionId, kind, reason);
                    break;
                case CustomEntityFamily.Drone:
                    RaiseDrone(ownerUniqueId, definitionId, kind, reason);
                    break;
            }
        }

        private void RaiseAnimal(string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            Dispatch(AnimalLifecycleChanged, ownerUniqueId, new CustomAnimalLifecycleEventArgs { Entity = Lifecycle(CustomEntityFamily.Animal, ownerUniqueId, definitionId, kind, reason) });
        }

        private void RaiseMonster(string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            Dispatch(MonsterLifecycleChanged, ownerUniqueId, new CustomMonsterLifecycleEventArgs { Entity = Lifecycle(CustomEntityFamily.Monster, ownerUniqueId, definitionId, kind, reason) });
        }

        private void RaiseAttack(string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            Dispatch(AttackLifecycleChanged, ownerUniqueId, new CustomAttackLifecycleEventArgs { Entity = Lifecycle(CustomEntityFamily.Attack, ownerUniqueId, definitionId, kind, reason) });
        }

        private void RaiseDrone(string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            Dispatch(DroneLifecycleChanged, ownerUniqueId, new CustomDroneLifecycleEventArgs { Entity = Lifecycle(CustomEntityFamily.Drone, ownerUniqueId, definitionId, kind, reason) });
        }

        private static CustomEntityLifecycleEventArgs Lifecycle(CustomEntityFamily family, string ownerUniqueId, string definitionId, CustomEntityLifecycleKind kind, string reason)
        {
            return new CustomEntityLifecycleEventArgs
            {
                Family = family,
                Kind = kind,
                OwnerUniqueId = ownerUniqueId,
                DefinitionId = definitionId,
                Reason = reason ?? string.Empty,
                Time = DateTimeOffset.Now
            };
        }

        private void Dispatch<TArgs>(EventHandler<TArgs>? handler, string ownerUniqueId, TArgs args)
            where TArgs : EventArgs
        {
            if (handler == null)
                return;
            foreach (Delegate listener in handler.GetInvocationList())
            {
                try
                {
                    ((EventHandler<TArgs>)listener)(this, args);
                }
                catch (Exception ex)
                {
                    diagnostics.RecordError(ownerUniqueId, "Custom entity lifecycle listener failed.", ex.ToString());
                }
            }
        }

        private static CustomAnimalRegistrationResult AnimalRegistration(bool success, string owner, string definitionId, string failure, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            return new CustomAnimalRegistrationResult
            {
                Succeeded = success,
                Family = CustomEntityFamily.Animal,
                OwnerUniqueId = owner,
                DefinitionId = definitionId,
                FailureReason = failure,
                RuntimeStatus = success ? CustomEntityRuntimeStatus.Registered : CustomEntityRuntimeStatus.Failed,
                Messages = messages
            };
        }

        private static CustomMonsterRegistrationResult MonsterRegistration(bool success, string owner, string definitionId, string failure, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            return new CustomMonsterRegistrationResult
            {
                Succeeded = success,
                Family = CustomEntityFamily.Monster,
                OwnerUniqueId = owner,
                DefinitionId = definitionId,
                FailureReason = failure,
                RuntimeStatus = success ? CustomEntityRuntimeStatus.Registered : CustomEntityRuntimeStatus.Failed,
                Messages = messages
            };
        }

        private static CustomAttackRegistrationResult AttackRegistration(bool success, string owner, string definitionId, string failure, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            return new CustomAttackRegistrationResult
            {
                Succeeded = success,
                Family = CustomEntityFamily.Attack,
                OwnerUniqueId = owner,
                DefinitionId = definitionId,
                FailureReason = failure,
                RuntimeStatus = success ? CustomEntityRuntimeStatus.Registered : CustomEntityRuntimeStatus.Failed,
                Messages = messages
            };
        }

        private static CustomDroneRegistrationResult DroneRegistration(bool success, string owner, string definitionId, string failure, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            return new CustomDroneRegistrationResult
            {
                Succeeded = success,
                Family = CustomEntityFamily.Drone,
                OwnerUniqueId = owner,
                DefinitionId = definitionId,
                FailureReason = failure,
                RuntimeStatus = success ? CustomEntityRuntimeStatus.Registered : CustomEntityRuntimeStatus.Failed,
                Messages = messages
            };
        }

        private static CustomAnimalSpawnResult AnimalRequest(bool success, string owner, string definitionId, string failure, string details, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            CustomEntityRequestResult result = Request(success, CustomEntityFamily.Animal, owner, definitionId, failure, details, null, success ? CustomEntityRuntimeStatus.Active : CustomEntityRuntimeStatus.RuntimeCreationBlocked, messages);
            return new CustomAnimalSpawnResult
            {
                Succeeded = result.Succeeded,
                Family = result.Family,
                OwnerUniqueId = result.OwnerUniqueId,
                DefinitionId = result.DefinitionId,
                Handle = result.Handle,
                FailureReason = result.FailureReason,
                Details = result.Details,
                RuntimeStatus = result.RuntimeStatus,
                Messages = result.Messages
            };
        }

        private static CustomMonsterSpawnResult MonsterRequest(bool success, string owner, string definitionId, string failure, string details, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            CustomEntityRequestResult result = Request(success, CustomEntityFamily.Monster, owner, definitionId, failure, details, null, success ? CustomEntityRuntimeStatus.Active : CustomEntityRuntimeStatus.RuntimeCreationBlocked, messages);
            return new CustomMonsterSpawnResult
            {
                Succeeded = result.Succeeded,
                Family = result.Family,
                OwnerUniqueId = result.OwnerUniqueId,
                DefinitionId = result.DefinitionId,
                Handle = result.Handle,
                FailureReason = result.FailureReason,
                Details = result.Details,
                RuntimeStatus = result.RuntimeStatus,
                Messages = result.Messages
            };
        }

        private static CustomAttackSpawnResult AttackRequest(bool success, string owner, string definitionId, string failure, string details, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            CustomEntityRequestResult result = Request(success, CustomEntityFamily.Attack, owner, definitionId, failure, details, null, success ? CustomEntityRuntimeStatus.Active : CustomEntityRuntimeStatus.RuntimeCreationBlocked, messages);
            return new CustomAttackSpawnResult
            {
                Succeeded = result.Succeeded,
                Family = result.Family,
                OwnerUniqueId = result.OwnerUniqueId,
                DefinitionId = result.DefinitionId,
                Handle = result.Handle,
                FailureReason = result.FailureReason,
                Details = result.Details,
                RuntimeStatus = result.RuntimeStatus,
                Messages = result.Messages
            };
        }

        private static CustomDroneSummonResult DroneRequest(bool success, string owner, string definitionId, string failure, string details, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            CustomEntityRequestResult result = Request(success, CustomEntityFamily.Drone, owner, definitionId, failure, details, null, success ? CustomEntityRuntimeStatus.Active : CustomEntityRuntimeStatus.RuntimeCreationBlocked, messages);
            return new CustomDroneSummonResult
            {
                Succeeded = result.Succeeded,
                Family = result.Family,
                OwnerUniqueId = result.OwnerUniqueId,
                DefinitionId = result.DefinitionId,
                Handle = result.Handle,
                FailureReason = result.FailureReason,
                Details = result.Details,
                RuntimeStatus = result.RuntimeStatus,
                Messages = result.Messages
            };
        }

        private static CustomEntityRequestResult Request(bool success, CustomEntityFamily family, string owner, string definitionId, string failure, string details, CustomEntityHandle? handle, CustomEntityRuntimeStatus status, IReadOnlyList<CustomEntityValidationMessage> messages)
        {
            return new CustomEntityRequestResult
            {
                Succeeded = success,
                Family = family,
                OwnerUniqueId = owner,
                DefinitionId = definitionId,
                Handle = handle,
                FailureReason = failure,
                Details = details,
                RuntimeStatus = status,
                Messages = messages
            };
        }

        private sealed class DefinitionRecord<TDefinition>
            where TDefinition : class
        {
            public DefinitionRecord(string ownerUniqueId, string definitionId, TDefinition definition)
            {
                OwnerUniqueId = ownerUniqueId;
                DefinitionId = definitionId;
                Definition = definition;
            }

            public string OwnerUniqueId { get; }
            public string DefinitionId { get; }
            public TDefinition Definition { get; }
        }
    }

    internal static class CustomEntityRegistryExtensions
    {
        public static int CountForOwner<TValue>(this Dictionary<string, TValue> values, string? ownerUniqueId)
        {
            if (string.IsNullOrWhiteSpace(ownerUniqueId))
                return values.Count;

            int count = 0;
            foreach (TValue value in values.Values)
            {
                string owner = string.Empty;
                object? obj = value;
                Type type = obj?.GetType() ?? typeof(object);
                System.Reflection.PropertyInfo? ownerProperty = type.GetProperty("OwnerUniqueId");
                if (ownerProperty != null)
                    owner = ownerProperty.GetValue(obj, null)?.ToString() ?? string.Empty;
                else
                {
                    System.Reflection.PropertyInfo? handleProperty = type.GetProperty("Handle");
                    object? handle = handleProperty?.GetValue(obj, null);
                    owner = handle?.GetType().GetProperty("OwnerUniqueId")?.GetValue(handle, null)?.ToString() ?? string.Empty;
                }

                if (string.Equals(owner, ownerUniqueId, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }
    }
}
