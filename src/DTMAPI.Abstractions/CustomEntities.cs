using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    public enum CustomEntityFamily
    {
        Animal,
        Monster,
        Attack,
        Drone
    }

    public enum CustomEntityValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum CustomEntityRuntimeStatus
    {
        Unknown,
        Registered,
        ConfiguredNoRuntimeInstance,
        RuntimeCreationBlocked,
        Active,
        Removing,
        Removed,
        Failed
    }

    public enum CustomEntityLifecycleKind
    {
        Registered,
        Unregistered,
        SaveLoaded,
        SaveSaving,
        SaveSaved,
        ReturnedToTitle,
        SpawnRequested,
        Spawned,
        RemoveRequested,
        Removed,
        Tick,
        Damaged,
        Died,
        Expired,
        Failed
    }

    public enum CustomEntityTickPolicyKind
    {
        Disabled,
        OnGameUpdate,
        FixedInterval,
        OneSecond,
        SaveBoundaryOnly
    }

    public enum CustomEntityPersistenceKind
    {
        RuntimeOnly,
        SaveScoped,
        SaveAndRespawn,
        DefinitionOnly
    }

    public enum CustomEntityMovementKind
    {
        None,
        Stationary,
        Wander,
        FollowTarget,
        Patrol,
        Flee,
        ProviderControlled
    }

    public enum CustomEntityRelationKind
    {
        Neutral,
        PlayerAlly,
        PlayerHostile,
        OwnerAlly,
        ProviderControlled
    }

    public enum CustomHitboxShapeKind
    {
        Point,
        Circle,
        Rectangle,
        Capsule,
        ProviderControlled
    }

    public enum CustomAttackPatternKind
    {
        Instant,
        Projectile,
        Beam,
        Burst,
        Ring,
        Cone,
        Barrage,
        ProviderControlled
    }

    public enum CustomDroneBehaviorMode
    {
        Idle,
        Follow,
        Guard,
        Patrol,
        Return,
        Attack,
        Support,
        ProviderControlled
    }

    public sealed class CustomEntityLocalizedText
    {
        public string Default { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
        public string SimplifiedChinese { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomEntityAssetHandle
    {
        public string AssetId { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = new string[0];
    }

    public sealed class CustomEntityHandle
    {
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string RuntimeId { get; set; } = string.Empty;
        public int SaveSlot { get; set; } = -1;

        public bool IsEmpty => string.IsNullOrWhiteSpace(RuntimeId);

        public override string ToString()
        {
            return Family + ":" + OwnerUniqueId + ":" + DefinitionId + ":" + RuntimeId;
        }
    }

    public sealed class CustomEntityVector2
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public sealed class CustomEntityGridPosition
    {
        public string RoomId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Layer { get; set; }
    }

    public sealed class CustomEntityValidationMessage
    {
        public CustomEntityValidationMessage()
        {
        }

        public CustomEntityValidationMessage(CustomEntityValidationSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public CustomEntityValidationSeverity Severity { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
    }

    public sealed class CustomEntityCapabilityStatus
    {
        public CustomEntityFamily Family { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int RegisteredDefinitionCount { get; set; }
        public int ActiveRuntimeInstanceCount { get; set; }
        public IReadOnlyList<CustomEntityValidationMessage> Messages { get; set; } = new CustomEntityValidationMessage[0];
    }

    public sealed class CustomEntityFamilySnapshot
    {
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public int RegisteredDefinitionCount { get; set; }
        public int ActiveRuntimeInstanceCount { get; set; }
        public int SaveStateRecordCount { get; set; }
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public string StatusDetails { get; set; } = string.Empty;
        public IReadOnlyList<string> DefinitionIds { get; set; } = new string[0];
        public IReadOnlyList<CustomEntityHandle> RuntimeHandles { get; set; } = new CustomEntityHandle[0];
    }

    public sealed class CustomEntityLifecycleEventArgs : EventArgs
    {
        public CustomEntityFamily Family { get; set; }
        public CustomEntityLifecycleKind Kind { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public CustomEntityHandle? Handle { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;
    }

    public class CustomEntityRegistrationResult
    {
        public bool Succeeded { get; set; }
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public IReadOnlyList<CustomEntityValidationMessage> Messages { get; set; } = new CustomEntityValidationMessage[0];
    }

    public sealed class CustomEntityUnregisterResult
    {
        public bool Succeeded { get; set; }
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public int RemovedRuntimeInstanceCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public IReadOnlyList<CustomEntityValidationMessage> Messages { get; set; } = new CustomEntityValidationMessage[0];
    }

    public class CustomEntityRequestResult
    {
        public bool Succeeded { get; set; }
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public CustomEntityHandle? Handle { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public IReadOnlyList<CustomEntityValidationMessage> Messages { get; set; } = new CustomEntityValidationMessage[0];
    }

    public sealed class CustomEntitySaveDataKey
    {
        public string Key { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public sealed class CustomEntitySaveMigrationContext
    {
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public int FromVersion { get; set; }
        public int ToVersion { get; set; }
        public IReadOnlyDictionary<string, string> SaveState { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomEntityTickPolicy
    {
        public CustomEntityTickPolicyKind Kind { get; set; } = CustomEntityTickPolicyKind.OnGameUpdate;
        public double IntervalSeconds { get; set; } = 1;
        public bool DeterministicOrder { get; set; } = true;
        public int Order { get; set; }
    }

    public sealed class CustomEntityPersistencePolicy
    {
        public CustomEntityPersistenceKind Kind { get; set; } = CustomEntityPersistenceKind.SaveScoped;
        public int SchemaVersion { get; set; } = 1;
        public bool RemoveInstancesWhenOwnerMissing { get; set; } = true;
        public bool RestoreRuntimeInstancesOnSaveLoad { get; set; }
        public IReadOnlyList<CustomEntitySaveDataKey> SaveKeys { get; set; } = new CustomEntitySaveDataKey[0];
    }

    public sealed class CustomEntityBehaviorContext
    {
        public CustomEntityFamily Family { get; set; }
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public CustomEntityHandle? Handle { get; set; }
        public ulong UpdateTick { get; set; }
        public int? SaveSlot { get; set; }
        public string RoomId { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> State { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomEntityBehaviorResult
    {
        public bool Succeeded { get; set; } = true;
        public string FailureReason { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> UpdatedState { get; set; } = new Dictionary<string, string>();
    }

    public interface ICustomEntityBehaviorProvider
    {
        void OnRegistered(CustomEntityBehaviorContext context);
        void OnUnregistered(CustomEntityBehaviorContext context);
        void MigrateSaveState(CustomEntitySaveMigrationContext context);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.0", Notes = "Registry-only compatibility contract. Native runtime creation remains blocked and no new C# CustomEntity capability will be added through this surface.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for ABI compatibility; do not adopt for new mods. No native CustomEntity host is admitted.")]
    [Obsolete("The custom-entity C# API is experimental and frozen; no native host is admitted. Do not adopt it for new mods.", false)]
    public interface ICustomAnimalApi
    {
        event EventHandler<CustomAnimalLifecycleEventArgs>? LifecycleChanged;
        CustomAnimalRegistrationResult RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition);
        CustomEntityUnregisterResult UnregisterSpecies(IManifest owner, string speciesId);
        IReadOnlyList<CustomAnimalSpeciesDefinition> GetSpeciesDefinitions(string? ownerUniqueId = null);
        CustomAnimalSpeciesDefinition? GetSpeciesDefinition(string speciesId);
        CustomAnimalSpawnResult RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request);
        CustomEntityRequestResult RequestRemove(IManifest owner, CustomEntityHandle animalHandle, string reason);
        IReadOnlyList<CustomAnimalInstanceSnapshot> GetAnimalInstances(string? ownerUniqueId = null);
        CustomAnimalInstanceSnapshot? GetAnimalInstance(CustomEntityHandle handle);
        CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null);
        CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.0", Notes = "Registry-only compatibility contract. Native runtime creation remains blocked and no new C# CustomEntity capability will be added through this surface.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for ABI compatibility; do not adopt for new mods. No native CustomEntity host is admitted.")]
    [Obsolete("The custom-entity C# API is experimental and frozen; no native host is admitted. Do not adopt it for new mods.", false)]
    public interface ICustomMonsterApi
    {
        event EventHandler<CustomMonsterLifecycleEventArgs>? LifecycleChanged;
        CustomMonsterRegistrationResult RegisterMonster(IManifest owner, CustomMonsterDefinition definition);
        CustomMonsterRegistrationResult RegisterSpawnTable(IManifest owner, CustomMonsterSpawnTableDefinition spawnTable);
        CustomEntityUnregisterResult UnregisterMonster(IManifest owner, string monsterId);
        IReadOnlyList<CustomMonsterDefinition> GetMonsterDefinitions(string? ownerUniqueId = null);
        CustomMonsterDefinition? GetMonsterDefinition(string monsterId);
        CustomMonsterSpawnResult RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request);
        CustomEntityRequestResult RequestDespawn(IManifest owner, CustomEntityHandle monsterHandle, string reason);
        IReadOnlyList<CustomMonsterInstanceSnapshot> GetMonsterInstances(string? ownerUniqueId = null);
        CustomMonsterInstanceSnapshot? GetMonsterInstance(CustomEntityHandle handle);
        CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null);
        CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.0", Notes = "Registry-only compatibility contract. Native runtime creation remains blocked and no new C# CustomEntity capability will be added through this surface.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for ABI compatibility; do not adopt for new mods. No native CustomEntity host is admitted.")]
    [Obsolete("The custom-entity C# API is experimental and frozen; no native host is admitted. Do not adopt it for new mods.", false)]
    public interface ICustomAttackApi
    {
        event EventHandler<CustomAttackLifecycleEventArgs>? LifecycleChanged;
        CustomAttackRegistrationResult RegisterAttack(IManifest owner, CustomAttackDefinition definition);
        CustomEntityUnregisterResult UnregisterAttack(IManifest owner, string attackId);
        IReadOnlyList<CustomAttackDefinition> GetAttackDefinitions(string? ownerUniqueId = null);
        CustomAttackDefinition? GetAttackDefinition(string attackId);
        CustomAttackSpawnResult SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request);
        CustomAttackSpawnResult ExecuteAttack(IManifest owner, CustomAttackSpawnRequest request);
        CustomEntityRequestResult RequestExpire(IManifest owner, CustomEntityHandle attackHandle, string reason);
        IReadOnlyList<CustomAttackInstanceSnapshot> GetActiveAttacks(string? ownerUniqueId = null);
        CustomAttackInstanceSnapshot? GetAttackInstance(CustomEntityHandle handle);
        CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null);
        CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.0", Notes = "Registry-only compatibility contract. Native runtime creation remains blocked and no new C# CustomEntity capability will be added through this surface.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for ABI compatibility; do not adopt for new mods. No native CustomEntity host is admitted.")]
    [Obsolete("The custom-entity C# API is experimental and frozen; no native host is admitted. Do not adopt it for new mods.", false)]
    public interface ICustomDroneApi
    {
        event EventHandler<CustomDroneLifecycleEventArgs>? LifecycleChanged;
        CustomDroneRegistrationResult RegisterDrone(IManifest owner, CustomDroneDefinition definition);
        CustomEntityUnregisterResult UnregisterDrone(IManifest owner, string droneId);
        IReadOnlyList<CustomDroneDefinition> GetDroneDefinitions(string? ownerUniqueId = null);
        CustomDroneDefinition? GetDroneDefinition(string droneId);
        CustomDroneSummonResult RequestSummon(IManifest owner, CustomDroneSummonRequest request);
        CustomEntityRequestResult RequestDismiss(IManifest owner, CustomEntityHandle droneHandle, string reason);
        CustomDroneEquipmentResult Equip(IManifest owner, CustomEntityHandle droneHandle, CustomDroneEquipmentRequest request);
        CustomDroneCommandResult SetMode(IManifest owner, CustomEntityHandle droneHandle, CustomDroneCommandRequest request);
        IReadOnlyList<CustomDroneInstanceSnapshot> GetDroneInstances(string? ownerUniqueId = null);
        CustomDroneInstanceSnapshot? GetDroneInstance(CustomEntityHandle handle);
        CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null);
        CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null);
    }

    public interface ICustomAnimalBehaviorProvider : ICustomEntityBehaviorProvider
    {
        CustomEntityBehaviorResult OnTick(CustomAnimalBehaviorContext context);
        CustomEntityBehaviorResult OnFeedRequested(CustomAnimalBehaviorContext context, string itemId, int amount);
        void OnExcrementProduced(CustomAnimalLifecycleEventArgs args);
        void OnBreedingEvent(CustomAnimalLifecycleEventArgs args);
        void OnHiddenProductChanged(CustomAnimalLifecycleEventArgs args);
    }

    public sealed class CustomAnimalSpeciesDefinition
    {
        public string SpeciesId { get; set; } = string.Empty;
        public IReadOnlyList<string> VariantIds { get; set; } = new string[0];
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public CustomEntityLocalizedText Description { get; set; } = new CustomEntityLocalizedText();
        public CustomEntityAssetHandle Icon { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Sprite { get; set; } = new CustomEntityAssetHandle();
        public IReadOnlyList<CustomAnimalLifeStageDefinition> LifeStages { get; set; } = new CustomAnimalLifeStageDefinition[0];
        public CustomAnimalHabitatPolicy Habitat { get; set; } = new CustomAnimalHabitatPolicy();
        public CustomAnimalDietPolicy Diet { get; set; } = new CustomAnimalDietPolicy();
        public CustomAnimalConsumptionPolicy Consumption { get; set; } = new CustomAnimalConsumptionPolicy();
        public CustomAnimalExcrementPolicy Excrement { get; set; } = new CustomAnimalExcrementPolicy();
        public CustomAnimalBreedingPolicy Breeding { get; set; } = new CustomAnimalBreedingPolicy();
        public IReadOnlyList<CustomAnimalProductRule> HiddenProducts { get; set; } = new CustomAnimalProductRule[0];
        public IReadOnlyList<CustomAnimalProductRule> ProduceRules { get; set; } = new CustomAnimalProductRule[0];
        public CustomAnimalStats Stats { get; set; } = new CustomAnimalStats();
        public CustomEntityPersistencePolicy Persistence { get; set; } = new CustomEntityPersistencePolicy();
        public CustomEntityTickPolicy TickPolicy { get; set; } = new CustomEntityTickPolicy();
        public ICustomAnimalBehaviorProvider? Provider { get; set; }
    }

    public sealed class CustomAnimalLifeStageDefinition
    {
        public string StageId { get; set; } = string.Empty;
        public int MinimumAgeDays { get; set; }
        public int? MaximumAgeDays { get; set; }
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public CustomEntityAssetHandle Sprite { get; set; } = new CustomEntityAssetHandle();
    }

    public sealed class CustomAnimalHabitatPolicy
    {
        public IReadOnlyList<string> AllowedRoomIds { get; set; } = new string[0];
        public IReadOnlyList<string> AllowedBuildingTags { get; set; } = new string[0];
        public IReadOnlyList<string> AllowedHabitatTags { get; set; } = new string[0];
        public int PopulationLimitPerRoom { get; set; } = -1;
        public bool RequiresShelter { get; set; }
    }

    public sealed class CustomAnimalDietPolicy
    {
        public IReadOnlyList<string> AcceptedItemIds { get; set; } = new string[0];
        public IReadOnlyList<string> AcceptedItemTags { get; set; } = new string[0];
        public int UnitsPerFeeding { get; set; } = 1;
        public bool CanGraze { get; set; }
    }

    public sealed class CustomAnimalConsumptionPolicy
    {
        public double HungerIntervalHours { get; set; } = 24;
        public int MaxFeedCapacity { get; set; } = 1;
        public bool TrackFedState { get; set; } = true;
        public bool RaiseFeedEvents { get; set; } = true;
    }

    public sealed class CustomAnimalExcrementPolicy
    {
        public bool Enabled { get; set; }
        public double IntervalHours { get; set; } = 24;
        public int MaxPendingCount { get; set; } = 1;
        public IReadOnlyList<CustomAnimalItemOutput> Outputs { get; set; } = new CustomAnimalItemOutput[0];
        public bool RequiresManualCleanup { get; set; } = true;
    }

    public sealed class CustomAnimalBreedingPolicy
    {
        public bool Enabled { get; set; }
        public IReadOnlyList<string> CompatibleSpeciesIds { get; set; } = new string[0];
        public double CooldownHours { get; set; } = 72;
        public double PregnancyOrIncubationHours { get; set; } = 72;
        public int OffspringCount { get; set; } = 1;
        public int PopulationLimitPerOwner { get; set; } = -1;
    }

    public sealed class CustomAnimalProductRule
    {
        public string ProductId { get; set; } = string.Empty;
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public bool HiddenUntilReady { get; set; }
        public double ProgressPerGameHour { get; set; }
        public double RequiredProgress { get; set; } = 100;
        public IReadOnlyList<CustomAnimalItemOutput> Outputs { get; set; } = new CustomAnimalItemOutput[0];
        public IReadOnlyList<string> RequiredStateTags { get; set; } = new string[0];
    }

    public sealed class CustomAnimalItemOutput
    {
        public string ItemId { get; set; } = string.Empty;
        public int MinStack { get; set; } = 1;
        public int MaxStack { get; set; } = 1;
        public double Chance { get; set; } = 1;
    }

    public sealed class CustomAnimalStats
    {
        public int MaxHealth { get; set; } = 1;
        public int MaxMood { get; set; } = 100;
        public int MaxFriendship { get; set; } = 100;
        public double MoveSpeed { get; set; } = 1;
        public IReadOnlyDictionary<string, double> CustomValues { get; set; } = new Dictionary<string, double>();
    }

    public sealed class CustomAnimalSpawnRequest
    {
        public string SpeciesId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public string InitialLifeStageId { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> InitialState { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomAnimalInstanceSnapshot
    {
        public CustomEntityHandle Handle { get; set; } = new CustomEntityHandle { Family = CustomEntityFamily.Animal };
        public string SpeciesId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public int AgeDays { get; set; }
        public bool IsHungry { get; set; }
        public bool NeedsExcrementCleanup { get; set; }
        public double HiddenProductProgress { get; set; }
        public IReadOnlyDictionary<string, string> State { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomAnimalBehaviorContext
    {
        public CustomEntityBehaviorContext Entity { get; set; } = new CustomEntityBehaviorContext { Family = CustomEntityFamily.Animal };
        public CustomAnimalInstanceSnapshot Snapshot { get; set; } = new CustomAnimalInstanceSnapshot();
    }

    public sealed class CustomAnimalLifecycleEventArgs : EventArgs
    {
        public CustomEntityLifecycleEventArgs Entity { get; set; } = new CustomEntityLifecycleEventArgs { Family = CustomEntityFamily.Animal };
        public CustomAnimalInstanceSnapshot? Snapshot { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public sealed class CustomAnimalRegistrationResult : CustomEntityRegistrationResult
    {
    }

    public sealed class CustomAnimalSpawnResult : CustomEntityRequestResult
    {
        public CustomAnimalInstanceSnapshot? Snapshot { get; set; }
    }

    public interface ICustomMonsterBehaviorProvider : ICustomEntityBehaviorProvider
    {
        CustomEntityBehaviorResult OnTick(CustomMonsterBehaviorContext context);
        string SelectAttack(CustomMonsterBehaviorContext context, IReadOnlyList<string> availableAttackIds);
        CustomEntityMovementKind SelectMovement(CustomMonsterBehaviorContext context);
        void OnTargetChanged(CustomMonsterLifecycleEventArgs args);
        void OnDamaged(CustomMonsterLifecycleEventArgs args);
        void OnDeath(CustomMonsterLifecycleEventArgs args);
    }

    public sealed class CustomMonsterDefinition
    {
        public string MonsterId { get; set; } = string.Empty;
        public IReadOnlyList<string> VariantIds { get; set; } = new string[0];
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public CustomEntityLocalizedText Description { get; set; } = new CustomEntityLocalizedText();
        public IReadOnlyList<CustomMonsterSpawnRule> SpawnRules { get; set; } = new CustomMonsterSpawnRule[0];
        public int MaxCountPerRoom { get; set; } = -1;
        public CustomEntityRelationKind RelationToPlayer { get; set; } = CustomEntityRelationKind.PlayerHostile;
        public string FactionId { get; set; } = string.Empty;
        public CustomMonsterStats Stats { get; set; } = new CustomMonsterStats();
        public CustomMonsterTargetPolicy Targeting { get; set; } = new CustomMonsterTargetPolicy();
        public CustomMonsterMovementPolicy Movement { get; set; } = new CustomMonsterMovementPolicy();
        public IReadOnlyList<CustomMonsterAttackSlot> AttackSlots { get; set; } = new CustomMonsterAttackSlot[0];
        public IReadOnlyList<CustomMonsterLootRule> Loot { get; set; } = new CustomMonsterLootRule[0];
        public CustomEntityAssetHandle Icon { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Sprite { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Audio { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityPersistencePolicy Persistence { get; set; } = new CustomEntityPersistencePolicy();
        public CustomEntityTickPolicy TickPolicy { get; set; } = new CustomEntityTickPolicy();
        public ICustomMonsterBehaviorProvider? Provider { get; set; }
    }

    public sealed class CustomMonsterSpawnRule
    {
        public string RuleId { get; set; } = string.Empty;
        public IReadOnlyList<string> RoomIds { get; set; } = new string[0];
        public IReadOnlyList<string> RoomTags { get; set; } = new string[0];
        public IReadOnlyList<string> BiomeTags { get; set; } = new string[0];
        public IReadOnlyList<string> Seasons { get; set; } = new string[0];
        public IReadOnlyList<string> WeatherIds { get; set; } = new string[0];
        public double Probability { get; set; } = 1;
        public int MinGroupSize { get; set; } = 1;
        public int MaxGroupSize { get; set; } = 1;
        public int? EarliestHour { get; set; }
        public int? LatestHour { get; set; }
    }

    public sealed class CustomMonsterSpawnTableDefinition
    {
        public string SpawnTableId { get; set; } = string.Empty;
        public IReadOnlyList<string> MonsterIds { get; set; } = new string[0];
        public IReadOnlyList<CustomMonsterSpawnRule> Rules { get; set; } = new CustomMonsterSpawnRule[0];
    }

    public sealed class CustomMonsterStats
    {
        public int MaxHealth { get; set; } = 1;
        public int Armor { get; set; }
        public int ContactDamage { get; set; }
        public double MoveSpeed { get; set; } = 1;
        public IReadOnlyDictionary<string, double> Resistances { get; set; } = new Dictionary<string, double>();
        public IReadOnlyDictionary<string, double> CustomValues { get; set; } = new Dictionary<string, double>();
    }

    public sealed class CustomMonsterTargetPolicy
    {
        public IReadOnlyList<string> TargetTags { get; set; } = new string[0];
        public double AggroRange { get; set; } = 8;
        public bool RetargetWhenDamaged { get; set; } = true;
        public bool ProviderCanOverride { get; set; }
    }

    public sealed class CustomMonsterMovementPolicy
    {
        public CustomEntityMovementKind Kind { get; set; } = CustomEntityMovementKind.Wander;
        public double PreferredDistance { get; set; }
        public double PatrolRadius { get; set; } = 4;
        public bool ProviderCanOverride { get; set; }
    }

    public sealed class CustomMonsterAttackSlot
    {
        public string SlotId { get; set; } = string.Empty;
        public string AttackId { get; set; } = string.Empty;
        public double CooldownSeconds { get; set; } = 1;
        public double Range { get; set; } = 1;
        public int Priority { get; set; }
    }

    public sealed class CustomMonsterLootRule
    {
        public string ItemId { get; set; } = string.Empty;
        public int MinStack { get; set; } = 1;
        public int MaxStack { get; set; } = 1;
        public double Chance { get; set; } = 1;
    }

    public sealed class CustomMonsterSpawnRequest
    {
        public string MonsterId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public IReadOnlyDictionary<string, string> InitialState { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomMonsterInstanceSnapshot
    {
        public CustomEntityHandle Handle { get; set; } = new CustomEntityHandle { Family = CustomEntityFamily.Monster };
        public string MonsterId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public int Health { get; set; }
        public string CurrentTargetId { get; set; } = string.Empty;
        public string CurrentAttackId { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> State { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomMonsterBehaviorContext
    {
        public CustomEntityBehaviorContext Entity { get; set; } = new CustomEntityBehaviorContext { Family = CustomEntityFamily.Monster };
        public CustomMonsterInstanceSnapshot Snapshot { get; set; } = new CustomMonsterInstanceSnapshot();
    }

    public sealed class CustomMonsterLifecycleEventArgs : EventArgs
    {
        public CustomEntityLifecycleEventArgs Entity { get; set; } = new CustomEntityLifecycleEventArgs { Family = CustomEntityFamily.Monster };
        public CustomMonsterInstanceSnapshot? Snapshot { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public int DamageAmount { get; set; }
        public string AttackId { get; set; } = string.Empty;
    }

    public sealed class CustomMonsterRegistrationResult : CustomEntityRegistrationResult
    {
    }

    public sealed class CustomMonsterSpawnResult : CustomEntityRequestResult
    {
        public CustomMonsterInstanceSnapshot? Snapshot { get; set; }
    }

    public interface ICustomAttackBehaviorProvider : ICustomEntityBehaviorProvider
    {
        CustomEntityBehaviorResult OnPatternTick(CustomAttackBehaviorContext context);
        void OnCollision(CustomAttackLifecycleEventArgs args);
        void OnDamageApplied(CustomAttackLifecycleEventArgs args);
        void OnExpired(CustomAttackLifecycleEventArgs args);
    }

    public sealed class CustomAttackDefinition
    {
        public string AttackId { get; set; } = string.Empty;
        public string FactionId { get; set; } = string.Empty;
        public CustomEntityRelationKind RelationToPlayer { get; set; } = CustomEntityRelationKind.OwnerAlly;
        public CustomDamagePayload Damage { get; set; } = new CustomDamagePayload();
        public IReadOnlyList<string> EffectTags { get; set; } = new string[0];
        public CustomHitboxDefinition Hitbox { get; set; } = new CustomHitboxDefinition();
        public CustomTrajectoryDefinition Trajectory { get; set; } = new CustomTrajectoryDefinition();
        public CustomBarragePatternDefinition Pattern { get; set; } = new CustomBarragePatternDefinition();
        public double LifetimeSeconds { get; set; } = 5;
        public int PierceCount { get; set; }
        public int BounceCount { get; set; }
        public bool Homing { get; set; }
        public bool FriendlyFire { get; set; }
        public CustomEntityAssetHandle Visual { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Audio { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityPersistencePolicy Persistence { get; set; } = new CustomEntityPersistencePolicy { Kind = CustomEntityPersistenceKind.RuntimeOnly };
        public CustomEntityTickPolicy TickPolicy { get; set; } = new CustomEntityTickPolicy();
        public ICustomAttackBehaviorProvider? Provider { get; set; }
    }

    public sealed class CustomDamagePayload
    {
        public int Amount { get; set; }
        public string DamageType { get; set; } = string.Empty;
        public double Knockback { get; set; }
        public IReadOnlyDictionary<string, double> Scaling { get; set; } = new Dictionary<string, double>();
    }

    public sealed class CustomHitboxDefinition
    {
        public CustomHitboxShapeKind Shape { get; set; } = CustomHitboxShapeKind.Circle;
        public double Radius { get; set; } = 0.5;
        public double Width { get; set; } = 1;
        public double Height { get; set; } = 1;
        public bool ProviderCanOverride { get; set; }
    }

    public sealed class CustomTrajectoryDefinition
    {
        public CustomEntityMovementKind Kind { get; set; } = CustomEntityMovementKind.FollowTarget;
        public double Speed { get; set; } = 5;
        public double Acceleration { get; set; }
        public double TurnRateDegreesPerSecond { get; set; }
        public bool ProviderCanOverride { get; set; }
    }

    public sealed class CustomBarragePatternDefinition
    {
        public CustomAttackPatternKind Kind { get; set; } = CustomAttackPatternKind.Projectile;
        public int ProjectileCount { get; set; } = 1;
        public double ArcDegrees { get; set; }
        public double IntervalSeconds { get; set; }
        public int RepeatCount { get; set; } = 1;
        public bool DeterministicRandomSeed { get; set; } = true;
    }

    public sealed class CustomAttackSpawnRequest
    {
        public string AttackId { get; set; } = string.Empty;
        public CustomEntityHandle? Source { get; set; }
        public CustomEntityHandle? Target { get; set; }
        public CustomEntityGridPosition Origin { get; set; } = new CustomEntityGridPosition();
        public CustomEntityVector2 Direction { get; set; } = new CustomEntityVector2 { X = 1, Y = 0 };
        public IReadOnlyDictionary<string, string> InitialState { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomAttackInstanceSnapshot
    {
        public CustomEntityHandle Handle { get; set; } = new CustomEntityHandle { Family = CustomEntityFamily.Attack };
        public string AttackId { get; set; } = string.Empty;
        public CustomEntityHandle? Source { get; set; }
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public double AgeSeconds { get; set; }
        public int HitCount { get; set; }
        public IReadOnlyDictionary<string, string> State { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomAttackBehaviorContext
    {
        public CustomEntityBehaviorContext Entity { get; set; } = new CustomEntityBehaviorContext { Family = CustomEntityFamily.Attack };
        public CustomAttackInstanceSnapshot Snapshot { get; set; } = new CustomAttackInstanceSnapshot();
    }

    public sealed class CustomAttackLifecycleEventArgs : EventArgs
    {
        public CustomEntityLifecycleEventArgs Entity { get; set; } = new CustomEntityLifecycleEventArgs { Family = CustomEntityFamily.Attack };
        public CustomAttackInstanceSnapshot? Snapshot { get; set; }
        public CustomEntityHandle? HitTarget { get; set; }
        public int DamageAmount { get; set; }
    }

    public sealed class CustomAttackRegistrationResult : CustomEntityRegistrationResult
    {
    }

    public sealed class CustomAttackSpawnResult : CustomEntityRequestResult
    {
        public CustomAttackInstanceSnapshot? Snapshot { get; set; }
    }

    public interface ICustomDroneBehaviorProvider : ICustomEntityBehaviorProvider
    {
        CustomEntityBehaviorResult OnTick(CustomDroneBehaviorContext context);
        CustomDroneCommandResult SelectMode(CustomDroneBehaviorContext context);
        CustomEntityMovementKind SelectMovement(CustomDroneBehaviorContext context);
        string SelectAttack(CustomDroneBehaviorContext context, IReadOnlyList<string> availableAttackIds);
        void OnDamaged(CustomDroneLifecycleEventArgs args);
        void OnDestroyed(CustomDroneLifecycleEventArgs args);
        void OnRepaired(CustomDroneLifecycleEventArgs args);
    }

    public sealed class CustomDroneDefinition
    {
        public string DroneId { get; set; } = string.Empty;
        public IReadOnlyList<string> VariantIds { get; set; } = new string[0];
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public CustomEntityLocalizedText Description { get; set; } = new CustomEntityLocalizedText();
        public CustomDroneOwnerBindingPolicy OwnerBinding { get; set; } = new CustomDroneOwnerBindingPolicy();
        public IReadOnlyList<CustomDroneBehaviorMode> SupportedModes { get; set; } = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard };
        public IReadOnlyList<CustomDroneEquipmentSlotDefinition> EquipmentSlots { get; set; } = new CustomDroneEquipmentSlotDefinition[0];
        public IReadOnlyList<CustomDroneEquipmentSlotDefinition> ModuleSlots { get; set; } = new CustomDroneEquipmentSlotDefinition[0];
        public IReadOnlyList<string> AttackIds { get; set; } = new string[0];
        public CustomDroneStats Stats { get; set; } = new CustomDroneStats();
        public CustomDroneEnergyPolicy Energy { get; set; } = new CustomDroneEnergyPolicy();
        public CustomDroneMovementPolicy Movement { get; set; } = new CustomDroneMovementPolicy();
        public CustomDroneRepairPolicy Repair { get; set; } = new CustomDroneRepairPolicy();
        public CustomDroneSummonPolicy Summon { get; set; } = new CustomDroneSummonPolicy();
        public CustomEntityAssetHandle Icon { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Sprite { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityAssetHandle Audio { get; set; } = new CustomEntityAssetHandle();
        public CustomEntityPersistencePolicy Persistence { get; set; } = new CustomEntityPersistencePolicy();
        public CustomEntityTickPolicy TickPolicy { get; set; } = new CustomEntityTickPolicy();
        public ICustomDroneBehaviorProvider? Provider { get; set; }
    }

    public sealed class CustomDroneOwnerBindingPolicy
    {
        public bool BindToPlayer { get; set; } = true;
        public bool BindToOwnerMod { get; set; } = true;
        public IReadOnlyList<string> PermissionTags { get; set; } = new string[0];
    }

    public sealed class CustomDroneEquipmentSlotDefinition
    {
        public string SlotId { get; set; } = string.Empty;
        public CustomEntityLocalizedText DisplayName { get; set; } = new CustomEntityLocalizedText();
        public IReadOnlyList<string> AllowedItemIds { get; set; } = new string[0];
        public IReadOnlyList<string> AllowedItemTags { get; set; } = new string[0];
        public bool Required { get; set; }
    }

    public sealed class CustomDroneStats
    {
        public int MaxHealth { get; set; } = 1;
        public int MaxShield { get; set; }
        public int Armor { get; set; }
        public int ContactDamage { get; set; }
        public double MoveSpeed { get; set; } = 3;
        public IReadOnlyDictionary<string, double> Resistances { get; set; } = new Dictionary<string, double>();
    }

    public sealed class CustomDroneEnergyPolicy
    {
        public int MaxEnergy { get; set; }
        public int EnergyPerSecond { get; set; }
        public int AttackEnergyCost { get; set; }
        public IReadOnlyList<string> AcceptedFuelItemIds { get; set; } = new string[0];
    }

    public sealed class CustomDroneMovementPolicy
    {
        public CustomEntityMovementKind Kind { get; set; } = CustomEntityMovementKind.FollowTarget;
        public double FollowDistance { get; set; } = 2;
        public double PatrolRadius { get; set; } = 4;
        public int Layer { get; set; }
        public bool CollidesWithWorld { get; set; } = true;
        public bool ProviderCanOverride { get; set; }
    }

    public sealed class CustomDroneRepairPolicy
    {
        public bool CanRepair { get; set; } = true;
        public int RepairAmountPerItem { get; set; }
        public IReadOnlyList<string> RepairItemIds { get; set; } = new string[0];
    }

    public sealed class CustomDroneSummonPolicy
    {
        public bool CanSummonAnywhere { get; set; }
        public double CooldownSeconds { get; set; }
        public int MaxActiveInstances { get; set; } = 1;
    }

    public sealed class CustomDroneSummonRequest
    {
        public string DroneId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public IReadOnlyDictionary<string, string> InitialState { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomDroneEquipmentRequest
    {
        public string SlotId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public int Stack { get; set; } = 1;
        public bool Unequip { get; set; }
    }

    public sealed class CustomDroneCommandRequest
    {
        public CustomDroneBehaviorMode Mode { get; set; } = CustomDroneBehaviorMode.Follow;
        public CustomEntityHandle? Target { get; set; }
        public CustomEntityGridPosition? Destination { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class CustomDroneInstanceSnapshot
    {
        public CustomEntityHandle Handle { get; set; } = new CustomEntityHandle { Family = CustomEntityFamily.Drone };
        public string DroneId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public CustomEntityGridPosition Position { get; set; } = new CustomEntityGridPosition();
        public CustomEntityRuntimeStatus RuntimeStatus { get; set; }
        public CustomDroneBehaviorMode Mode { get; set; } = CustomDroneBehaviorMode.Follow;
        public int Health { get; set; }
        public int Shield { get; set; }
        public int Energy { get; set; }
        public IReadOnlyDictionary<string, string> Equipment { get; set; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> State { get; set; } = new Dictionary<string, string>();
    }

    public sealed class CustomDroneBehaviorContext
    {
        public CustomEntityBehaviorContext Entity { get; set; } = new CustomEntityBehaviorContext { Family = CustomEntityFamily.Drone };
        public CustomDroneInstanceSnapshot Snapshot { get; set; } = new CustomDroneInstanceSnapshot();
    }

    public sealed class CustomDroneLifecycleEventArgs : EventArgs
    {
        public CustomEntityLifecycleEventArgs Entity { get; set; } = new CustomEntityLifecycleEventArgs { Family = CustomEntityFamily.Drone };
        public CustomDroneInstanceSnapshot? Snapshot { get; set; }
        public int DamageAmount { get; set; }
        public int RepairAmount { get; set; }
        public string EquipmentSlotId { get; set; } = string.Empty;
    }

    public sealed class CustomDroneRegistrationResult : CustomEntityRegistrationResult
    {
    }

    public sealed class CustomDroneSummonResult : CustomEntityRequestResult
    {
        public CustomDroneInstanceSnapshot? Snapshot { get; set; }
    }

    public sealed class CustomDroneEquipmentResult : CustomEntityRequestResult
    {
        public string SlotId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
    }

    public sealed class CustomDroneCommandResult : CustomEntityRequestResult
    {
        public CustomDroneBehaviorMode Mode { get; set; }
    }
}
