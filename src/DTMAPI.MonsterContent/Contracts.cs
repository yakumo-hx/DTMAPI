using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;

[assembly: DtmApiStatus(DtmApiStatus.Experimental)]
[assembly: InternalsVisibleTo("DTMAPI.Modules.Monsters")]
[assembly: InternalsVisibleTo("DTMAPI.RuntimeModules")]
[assembly: InternalsVisibleTo("DTMAPI.UnitTests")]
[assembly: InternalsVisibleTo("DTMAPI.AudioContent.Tests")]
[assembly: InternalsVisibleTo("DTMAPI.MonsterContent.Tests")]
[assembly: InternalsVisibleTo("DTMAPI.MonsterContent.Authoring")]

namespace DTMAPI.MonsterContent
{
    /// <summary>内置怪物模块。</summary>
    public static partial class MonsterModule
    {
        public const string ProviderId = "DTMAPI.Monsters";
    }

    public enum MonsterModuleState { NotRequested, WaitingNative, Active, Draining, DormantResident, Disabled, Failed }
    public enum MonsterDefinitionState { WaitingNative, Creatable, Rejected, Withdrawn, CleanupPending, RestartRequired }
    public enum MonsterRemovalReason { AuthorRequest, DefinitionReleased, OwnerStopped, WorldEnded, CreationFailed }

    public sealed class MonsterDiagnostic
    {
        public MonsterDiagnostic(string code, string message, string ownerId = "", string definitionId = "", string templateId = "", string stage = "", string field = "", string recovery = "")
            : this(code, message, ownerId, definitionId, templateId, stage, field, recovery, "", null, null) { }
        public MonsterDiagnostic(string code, string message, string ownerId, string definitionId, string templateId, string stage,
            string field, string recovery, string providerId, MonsterHandle? instance, MonsterActionHandle? action)
        { Code = code; Message = message; OwnerId = ownerId; DefinitionId = definitionId; TemplateId = templateId; Stage = stage; Field = field; Recovery = recovery; ProviderId = providerId; Instance = instance; Action = action; }
        public string Code { get; }
        public string Message { get; }
        public string OwnerId { get; }
        public string DefinitionId { get; }
        public string TemplateId { get; }
        public string Stage { get; }
        public string Field { get; }
        public string Recovery { get; }
        public string ProviderId { get; }
        public MonsterHandle? Instance { get; }
        public MonsterActionHandle? Action { get; }
    }

    public sealed class MonsterResult<T>
    {
        private MonsterResult(bool success, T? value, MonsterDiagnostic? diagnostic) { Success = success; Value = value; Diagnostic = diagnostic; }
        public bool Success { get; }
        public T? Value { get; }
        public MonsterDiagnostic? Diagnostic { get; }
        public static MonsterResult<T> Ok(T value) => new MonsterResult<T>(true, value, null);
        public static MonsterResult<T> Fail(MonsterDiagnostic diagnostic) => new MonsterResult<T>(false, default, diagnostic);
        internal static MonsterResult<T> FailedOwned(MonsterDiagnostic diagnostic, T value) => new MonsterResult<T>(false, value, diagnostic);
    }

    public sealed class MonsterDefinition
    {
        public string LocalId { get; set; } = "";
        public string TemplateId { get; set; } = "";
        public int? ExpValue { get; set; }
        public int? Health { get; set; }
        public int? Defense { get; set; }
        /// <summary>Native hurt interval in game seconds (nonnegative); null inherits the template. Does not add a new hurt response.</summary>
        public float? HurtDuration { get; set; }
        public MonsterNativeParameters? NativeParameters { get; set; }
        public MonsterBodyParameters? Body { get; set; }
        public MonsterMovementParameters? Movement { get; set; }
        public MonsterSightParameters? Sight { get; set; }
        public MonsterSounds? Sounds { get; set; }
        public string Behavior { get; set; } = "inherit-native";
        public string BehaviorId { get; set; } = "";
        public MonsterSequence? Sequence { get; set; }
        public MonsterCombatPolicy? Combat { get; set; }
        public MonsterHealthPhases? HealthPhases { get; set; }
        public MonsterCompendium? Compendium { get; set; }
        public List<MonsterAttackComposition> AttackComposition { get; set; } = new List<MonsterAttackComposition>();
        public List<MonsterNativeAttackParameters> NativeAttacks { get; set; } = new List<MonsterNativeAttackParameters>();
        public List<MonsterNativePartParameters> NativeParts { get; set; } = new List<MonsterNativePartParameters>();
        public List<MonsterSkill> Skills { get; set; } = new List<MonsterSkill>();
        public MonsterAppearance Appearance { get; set; } = new MonsterAppearance();
        public MonsterDisplay Display { get; set; } = new MonsterDisplay();
        public MonsterDrops Drops { get; set; } = new MonsterDrops();
        internal MonsterDefinition Copy() => new MonsterDefinition { LocalId = LocalId, TemplateId = TemplateId, ExpValue = ExpValue, Health = Health, Defense = Defense, HurtDuration = HurtDuration, NativeParameters = NativeParameters?.Copy(), Body = Body?.Copy(), Movement = Movement?.Copy(), Sight = Sight?.Copy(), Sounds = Sounds?.Copy(), Behavior = Behavior, BehaviorId = BehaviorId, Sequence = Sequence?.Copy(), Combat = Combat?.Copy(), HealthPhases = HealthPhases?.Copy(), Compendium = Compendium?.Copy(), AttackComposition = AttackComposition.Select(a => a.Copy()).ToList(), NativeAttacks = NativeAttacks.Select(a => a.Copy()).ToList(), NativeParts = NativeParts.Select(p => p.Copy()).ToList(), Skills = Skills.Select(s => s.Copy()).ToList(), Appearance = Appearance.Copy(), Display = Display.Copy(), Drops = Drops.Copy() };
    }

    public sealed class MonsterTemplate
    {
        public MonsterTemplate(string id, long generation, bool canInherit, bool runtimeVerified, string body, string ai, int health, int defense, string gameVersion, IEnumerable<MonsterDiagnostic> diagnostics)
        { Id = id; Generation = generation; CanInherit = canInherit; RuntimeVerified = runtimeVerified; Body = body; EffectiveAi = ai; Health = health; Defense = defense; GameVersion = gameVersion; Diagnostics = new List<MonsterDiagnostic>(diagnostics).AsReadOnly(); }
        public string Id { get; }
        public long Generation { get; }
        public bool CanInherit { get; }
        public bool RuntimeVerified { get; }
        /// <summary>Reference inspection only; does not imply lifecycle or combat acceptance.</summary>
        public bool ReferenceIntegrityComplete => !Diagnostics.Any(d => d.Code == "ResourceMissing");
        /// <summary>RuntimeVerified records creation postconditions, not full gameplay validation.</summary>
        public string RuntimeValidation => RuntimeVerified ? "CreationPostconditionsVerified" : "NotObserved";
        /// <summary>Runtime creation is not a combat/exit test. Mechanism acceptance is recorded separately.</summary>
        public string LifecycleValidation => "NotObserved";
        public string Body { get; }
        public string EffectiveAi { get; }
        public int? ExpValue { get; internal set; }
        public int Health { get; }
        public int Defense { get; }
        /// <summary>Raw template interval; null when this catalog does not advertise definition hurt-duration editing.</summary>
        public float? HurtDuration { get; internal set; }
        public string GameVersion { get; }
        public string GenerationType { get; internal set; } = "";
        public int Volume { get; internal set; }
        internal MonsterTemplate WithActions(MonsterActionCapabilities actions) { var copy = (MonsterTemplate)MemberwiseClone(); copy.Actions = actions; return copy; }
        public MonsterActionCapabilities Actions { get; internal set; } = new MonsterActionCapabilities(Array.Empty<string>(), false, "Action metadata unavailable.");
        public MonsterCatalogEntry? Reference { get; internal set; }
        public MonsterNativeParameters? NativeParameters { get; internal set; }
        public MonsterPhysicalInfo? Physical { get; internal set; }
        public bool? HummingLoop { get; internal set; } = false;
        public IReadOnlyList<string> SoundEvents { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<string> EditableFields => !CanInherit ? Array.Empty<string>() : (HurtDuration.HasValue
            ? (Presentation.CanMapAllFrames ? HurtMappedFields : HurtBaseFields) : Presentation.CanMapAllFrames ? MappedFields : BaseFields)
            .Concat(NativeParameters != null ? new[] { "nativeParameters" } : Array.Empty<string>())
            .Concat(SoundEvents.Select(e => "sounds." + e))
            .Concat(Physical?.BodyScale == true ? new[] { "body" } : Array.Empty<string>())
            .Concat(Physical?.Movement != null ? new[] { "movement" } : Array.Empty<string>())
            .Concat(Physical?.Sight != null ? new[] { "sight" } : Array.Empty<string>())
            .Concat(Actions.NativeAttacks.Any(a => a.Fields.Any(f => f.Definition) || a.Sounds.Any(s => s.Definition)) ? new[] { "nativeAttacks" } : Array.Empty<string>())
            .Concat(Actions.NativeParts.Any(p => p.SoundEvents.Count > 0 || p.NativeAttacks.Any(a => a.Fields.Any(f => f.Definition) || a.Sounds.Any(s => s.Definition))) ? new[] { "nativeParts" } : Array.Empty<string>()).ToArray();
        private static readonly IReadOnlyList<string> BaseFields = Array.AsReadOnly(new[] { "health", "defense", "expValue", "appearance.scale", "appearance.tint", "display", "drops" });
        private static readonly IReadOnlyList<string> MappedFields = Array.AsReadOnly(new[] { "health", "defense", "expValue", "appearance.scale", "appearance.tint", "appearance.skin", "display", "drops" });
        private static readonly IReadOnlyList<string> HurtBaseFields = Array.AsReadOnly(new[] { "health", "defense", "expValue", "appearance.scale", "hurtDuration", "appearance.tint", "display", "drops" });
        private static readonly IReadOnlyList<string> HurtMappedFields = Array.AsReadOnly(new[] { "health", "defense", "expValue", "appearance.scale", "hurtDuration", "appearance.tint", "appearance.skin", "display", "drops" });
        public IReadOnlyList<MonsterDiagnostic> Diagnostics { get; }
        public MonsterPresentationInfo Presentation { get; internal set; } = new MonsterPresentationInfo("", Array.Empty<MonsterFrameInfo>(), "Template frame metadata unavailable.");
        public IReadOnlyList<string> NativeOverrideFields { get; internal set; } = Array.Empty<string>();
    }

    public sealed class MonsterDefinitionSnapshot
    {
        public MonsterDefinitionSnapshot(string id, string templateId, long generation, MonsterDefinitionState state, MonsterDiagnostic? diagnostic = null)
        { Id = id; TemplateId = templateId; Generation = generation; State = state; Diagnostic = diagnostic; }
        public string Id { get; }
        public string TemplateId { get; }
        /// <summary>Opaque native table identity for this registered definition. Read with State/Generation; it grants no native or cross-owner operations.</summary>
        public string NativeId { get; internal set; } = "";
        public long Generation { get; }
        public MonsterDefinitionState State { get; }
        public MonsterDiagnostic? Diagnostic { get; }
    }

    public sealed class MonsterLease : IDisposable
    {
        private Action? release;
        internal MonsterLease(Action release) { this.release = release; }
        public void Dispose() { Action? action = release; if (action == null) return; action(); release = null; }
    }
    public sealed class MonsterDefinitionLease : IDisposable
    {
        private readonly Func<MonsterDefinitionSnapshot> snapshot;
        private readonly MonsterLease lease;
        internal MonsterDefinitionLease(Func<MonsterDefinitionSnapshot> snapshot, Action release)
        {
            this.snapshot = snapshot;
            lease = new MonsterLease(() =>
            {
                release();
                var result = snapshot();
                if (result.State != MonsterDefinitionState.Withdrawn)
                    throw new InvalidOperationException("Definition cleanup is incomplete: " + result.Id + "; " + result.State + "; " + result.Diagnostic?.Message);
            });
        }
        public MonsterDefinitionSnapshot Snapshot => snapshot();
        public void Dispose() => lease.Dispose();
    }

    public readonly struct MonsterHandle : IEquatable<MonsterHandle>
    {
        internal MonsterHandle(Guid token, long generation) { Token = token; Generation = generation; }
        internal Guid Token { get; }
        public long Generation { get; }
        public bool Equals(MonsterHandle other) => Token == other.Token && Generation == other.Generation;
        public override bool Equals(object? obj) => obj is MonsterHandle h && Equals(h);
        public override int GetHashCode() => Token.GetHashCode() ^ Generation.GetHashCode();
        public override string ToString() => Token.ToString("N") + ":" + Generation;
    }
    public sealed class MonsterSpawnRequest
    {
        public string DefinitionId { get; set; } = "";
        // Coordinates are current-room world coordinates, never a persistent room identifier.
        public float X { get; set; }
        public float Y { get; set; }
        public MonsterPositionPolicy PositionPolicy { get; set; }
        public float PositionTolerance { get; set; } = 0.01f;
    }
    public sealed class MonsterInstanceSnapshot
    {
        public MonsterInstanceSnapshot(MonsterHandle handle, string definitionId, string templateId, int health, int maxHealth, float x, float y)
        { Handle = handle; DefinitionId = definitionId; TemplateId = templateId; Health = health; MaxHealth = maxHealth; X = x; Y = y; }
        public MonsterHandle Handle { get; }
        public string DefinitionId { get; }
        public string TemplateId { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public float X { get; }
        public float Y { get; }
        public MonsterPlacement? Placement { get; internal set; }
        public MonsterSequenceSnapshot? Sequence { get; internal set; }
        public MonsterCombatSnapshot? Combat { get; internal set; }
        public MonsterHealthPhaseSnapshot? HealthPhase { get; internal set; }
    }

    public interface IMonsterContentApi
    {
        MonsterResult<MonsterOverrideLease> RegisterOverride(MonsterNativeOverride value);
        MonsterResult<IReadOnlyList<MonsterOverrideSnapshot>> GetOverrides();
        MonsterResult<MonsterModuleState> GetState();
        MonsterResult<MonsterLease> RequestModule();
        MonsterResult<IReadOnlyList<MonsterTemplate>> GetTemplates();
        MonsterResult<MonsterContentCatalog> GetCatalog();
        MonsterResult<MonsterContentLease> RegisterSpawnRule(MonsterSpawnRule rule);
        MonsterResult<MonsterContentLease> RegisterDropPool(MonsterDropPool pool);
        MonsterResult<MonsterDefinitionLease> RegisterDefinition(MonsterDefinition definition);
        MonsterResult<MonsterLease> RegisterBehavior(string localId, Func<IMonsterBehavior> factory);
        MonsterResult<MonsterLease> RegisterBehavior(string localId, MonsterBehaviorRequirements requirements, Func<IMonsterBehavior> factory);
        MonsterResult<MonsterInstanceSnapshot> Spawn(MonsterSpawnRequest request);
        MonsterResult<IReadOnlyList<MonsterInstanceSnapshot>> GetInstances();
        MonsterResult<MonsterInstanceSnapshot> GetInstance(MonsterHandle handle);
        MonsterResult<bool> Despawn(MonsterHandle handle);
        /// <summary>Stop this visible execution's configured sequence, including target/interval waits. No automatic restart.</summary>
        MonsterResult<bool> CancelSequence(MonsterHandle handle);
    }
}
