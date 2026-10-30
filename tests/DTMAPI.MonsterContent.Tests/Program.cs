using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static MonsterDefinition Definition(string id = "variant") => new MonsterDefinition { LocalId = id, TemplateId = "test-template", Health = 60, Defense = 2 };
    static void Main()
    {
        var failures = new List<Exception>();
        void Run(Action check) { try { check(); } catch (Exception ex) { Console.Error.WriteLine(check.Method.Name + ": " + ex); failures.Add(ex); } }
        Run(ModuleLifecycleChecks);
        Run(RuntimeContentChecks);
        Run(OverrideLoadOrderChecks);
        Run(ContentChecks);
        Run(AuthoringChecks);
        Run(DirectionalAdmissionChecks);
        Run(RegistrationChecks);
        Run(JsonChecks);
        Run(AuthorFreedomChecks);
        Run(SequenceChecks);
        Run(SkillChecks);
        Run(HealthPhaseChecks);
        Run(CompendiumChecks);
        Run(NativeOverrideChecks);
        Run(ParameterTintChecks);
        Run(PhysicalChecks);
        Run(SoundChecks);
        Run(NativeSoundChecks);
        Run(FireSoundChecks);
        Run(DashSoundChecks);
        Run(AttackEventSoundChecks);
        Run(SoundAuthoringChecks);
#if !MONSTER_ONLY
        Run(CommonAudioChecks);
#endif
        Run(CommonAttackChecks);
        Run(RecoverySoundChecks);
        Run(NativeCallParameterChecks);
        Run(NativePartParameterChecks);
        Run(NativeAttackParameterChecks);
        Run(NativeContentChecks);
        Run(Completion075Checks);
        Run(CombatChecks);
        Run(InterruptionChecks);
        var publisher = new Publisher();
        var registry = new DefinitionRegistry(publisher, () => { });
        var dto = Definition();
        var waiting = registry.Register("A", dto);
        Check(waiting.Success && waiting.Value!.Snapshot.State == MonsterDefinitionState.WaitingNative && publisher.Publishes == 0, "Early registration must defer publication.");
        string nativeIdentity = waiting.Value!.Snapshot.NativeId;
        Check(nativeIdentity.Length > 0 && nativeIdentity == registry.Find("A", "A:variant")!.NativeId, "The lease identifies the actual native record even while publication is pending.");
        dto.Health = 999;
        publisher.Ready = true; publisher.Generation = 1; registry.Refresh();
        Check(waiting.Value!.Snapshot.State == MonsterDefinitionState.Creatable && publisher.Last!.Definition.Health == 60, "Published definition must freeze author input.");
        Check(!registry.Register("A", Definition()).Success && registry.Register("B", Definition()).Success, "Duplicate IDs are scoped to their real owner.");
        Check(registry.Find("B", "A:variant") == null, "Foreign definitions must be invisible.");
        Check(registry.Find("B", "B:variant")!.Snapshot.NativeId != nativeIdentity, "Equal local IDs from two owners have distinct native identities.");
        publisher.Generation = 2; registry.Refresh();
        Check(waiting.Value.Snapshot.Generation == 2 && publisher.Publishes == 4, "Native generation refresh republishes each active definition once.");
        registry.Refresh(); Check(publisher.Publishes == 4, "Unchanged generation does not republish.");
        waiting.Value.Dispose(); waiting.Value.Dispose();
        Check(waiting.Value.Snapshot.NativeId == nativeIdentity && waiting.Value.Snapshot.Generation == 2 && waiting.Value.Snapshot.State == MonsterDefinitionState.Withdrawn, "Native identity is stable across generations and remains diagnostic after withdrawal; state closes admission.");
        Check(publisher.Withdrawals == 1 && registry.CountOwnerResources("B") == 1, "Lease disposal is idempotent and isolated.");
        publisher.Failure = "RestartRequired";
        var failed = registry.Register("C", Definition());
        Check(!failed.Success && registry.CountOwnerResources("C") == 1, "Uncertain rollback retains a counted resource and restart diagnosis.");
        publisher.Failure = null; registry.RemoveOwner("C");
        Check(registry.CountOwnerResources("C") == 0, "Cleanup can clear retained failure state.");
        var invalid = Definition(); invalid.Health = 0;
        Check(registry.Register("D", invalid).Diagnostic!.Field == "health", "Invalid values are diagnosed before publication.");
        publisher.Throw = true;
        Check(registry.Register("Throw", Definition()).Diagnostic!.Code == "RestartRequired" && registry.CountOwnerResources("Throw") == 1, "Unexpected publisher failure closes creation and retains cleanup accounting.");
        publisher.Throw = false;
        var code = Definition("code"); code.Behavior = "code-provider";
        Check(MonsterValidation.Validate("A", code)!.Field == "behaviorId", "Code mode requires an independent qualified behavior ID.");
        code.BehaviorId = "A:patrol";
        Check(MonsterValidation.Validate("A", code) == null, "Qualified code-provider mode is admitted.");
        code.Behavior = "inherit-native";
        Check(MonsterValidation.Validate("A", code)!.Field == "behaviorId", "Inherited mode cannot silently consume a behavior provider.");
        code.Behavior = "code-provider";
        var codeLease = registry.Register("CodeOwner", code).Value!;
        code.BehaviorId = "mutated:behavior";
        Check(registry.Find("CodeOwner", "CodeOwner:code")!.Definition.BehaviorId == "A:patrol", "Behavior binding freezes DTO input.");
        registry.RemoveBehavior("A:patrol");
        Check(codeLease.Snapshot.State == MonsterDefinitionState.Withdrawn, "Provider lease release closes dependent definitions.");

        var visual = Definition("skin");
        visual.Appearance.Skin = new MonsterSkin { TemplateFingerprint = "fixture", Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg=="), Frames = new List<MonsterFrameMapping> { new MonsterFrameMapping { FrameId = "idle", Width = 1, Height = 1 } } };
        Check(registry.Register("Visual", visual).Success, "Structured PNG presentation is admitted through the shared registry.");
        visual.Display.Name = "mutated"; visual.Appearance.Tint = "#112233"; visual.Appearance.Skin.Png[0] = 0; visual.Appearance.Skin.Frames[0].Width = 99;
        var frozen = registry.Find("Visual", "Visual:skin")!.Definition;
        Check(frozen.Display.Name == "" && frozen.Display.Nameplate && frozen.Display.HealthBar == MonsterHealthBar.Damaged && frozen.Appearance.Tint == "#FFFFFF" && frozen.Appearance.Skin!.Png[0] == 137 && frozen.Appearance.Skin.Frames[0].Width == 1, "Nested appearance, bytes and rectangles are defensively copied; defaults match the editor.");
        var duplicate = Definition("duplicate"); duplicate.Appearance.Skin = frozen.Appearance.Skin;
        duplicate.Appearance.Skin!.Frames.Add(new MonsterFrameMapping { FrameId = "idle", Width = 1, Height = 1 });
        Check(MonsterValidation.Validate("Visual", duplicate)!.Message.Contains("Duplicate frame: idle"), "Duplicate mappings identify the exact frame before publication.");
        duplicate.Appearance.Skin.Frames.RemoveAt(1); duplicate.Appearance.Skin.Frames[0].Width = 2;
        Check(MonsterValidation.Validate("Visual", duplicate) == null && !SkinMappingAdmission.HasPixels(duplicate.Appearance.Skin.Frames[0], 1, 1), "Out-of-image frames are accepted but render no image.");
        var invalidTint = Definition("color"); invalidTint.Appearance.Tint = "blue";
        Check(MonsterValidation.Validate("Visual", invalidTint)!.Field == "appearance.tint", "Tint validation is shared by imperative and declarative callers.");
        duplicate.Appearance.Skin.Frames[0].Width = 1;
        var frame = new MonsterFrameInfo("idle", 1, 1, .5f, .5f, 32);
        var currentFrames = new MonsterPresentationInfo("fixture", new[] { frame });
        Check(SkinMappingAdmission.Validate("Visual", duplicate, currentFrames) == null, "Complete mapping is admitted using the same engine-free kernel as publication.");
        Check(SkinMappingAdmission.Validate("Visual", duplicate, new MonsterPresentationInfo("other", new[] { frame })) == null, "A stale fingerprint does not block authored frame mappings.");
        Check(SkinMappingAdmission.Validate("Visual", duplicate, new MonsterPresentationInfo("fixture", new[] { frame, new MonsterFrameInfo("attack", 1, 1, .5f, .5f, 32) })) == null, "Missing native frames do not block the monster.");
        Check(SkinMappingAdmission.Validate("Visual", duplicate, new MonsterPresentationInfo("fixture", new[] { new MonsterFrameInfo("idle", 2, 1, .5f, .5f, 32) })) == null, "Authored frame dimensions may differ from the template.");
        var metadataInput = new List<MonsterFrameInfo> { frame };
        var stableMetadata = new MonsterPresentationInfo("fixture", metadataInput); metadataInput.Clear();
        Check(stableMetadata.Frames.Count == 1, "Template metadata does not retain the caller's mutable list.");

        if (failures.Count != 0) throw new AggregateException(failures);
        Console.WriteLine("PASS monster contract checks=" + checks);
    }
    sealed class Publisher : IDefinitionPublisher
    {
        public bool Ready { get; set; } public long Generation { get; set; }
        public int Publishes, Withdrawals; public DefinitionRecord? Last; public string? Failure; public bool Throw;
        public MonsterDiagnostic? Publish(DefinitionRecord d) { Publishes++; Last = d; if (Throw) throw new InvalidOperationException("unexpected failure"); return Failure == null ? null : new MonsterDiagnostic(Failure, "injected"); }
        public MonsterDiagnostic? Withdraw(DefinitionRecord d) { Withdrawals++; return null; }
    }
    class Backend : IMonsterBackend, IMonsterOverrideBackend, IMonsterBehaviorMetadataBackend
    {
        public readonly NativeOverrideRegistry Overrides;
        public double OverrideBaseline = 2;
        public bool OverrideTargetPresent = true;
        public Backend() { Overrides = new NativeOverrideRegistry(() => { }, (_, field) => field == "defense" ? OverrideBaseline : .5,
            (_, _, _) => OverrideTargetPresent ? null : new MonsterDiagnostic("OverrideTargetMissing", "missing")); }
        public MonsterResult<MonsterOverrideLease> RegisterOverride(string owner, int loadIndex, MonsterNativeOverride value) => Overrides.Register(owner, loadIndex, value);
        public IReadOnlyList<MonsterOverrideSnapshot> GetOverrides() => Overrides.Snapshots();
        public string? BehaviorOwner; public Action? BehaviorGuard; public Action<string, string>? BehaviorReport;
        public List<string> Reports = new List<string>();
        public bool Disposed, ThrowDispose; public List<string> Owners = new List<string>(), Removed = new List<string>();
        public MonsterModuleState State { get; set; } = MonsterModuleState.Active;
        public IReadOnlyList<MonsterTemplate> Templates => Array.Empty<MonsterTemplate>();
        public MonsterContentCatalog Catalog => new MonsterContentCatalog(1, "test", "0.1.0", "fixture", Templates, Array.Empty<MonsterCatalogEntry>(), Array.Empty<MonsterDropPoolInfo>(), Array.Empty<MonsterRoomInfo>());
        public MonsterResult<MonsterContentLease> RegisterSpawnRule(string owner, MonsterSpawnRule rule) { Owners.Add(owner); return MonsterResult<MonsterContentLease>.Fail(new MonsterDiagnostic("fake", "captured")); }
        public MonsterResult<MonsterContentLease> RegisterDropPool(string owner, MonsterDropPool pool) { Owners.Add(owner); return MonsterResult<MonsterContentLease>.Fail(new MonsterDiagnostic("fake", "captured")); }
        public virtual MonsterResult<MonsterDefinitionLease> Register(string owner, MonsterDefinition d) { Owners.Add(owner); return MonsterResult<MonsterDefinitionLease>.Fail(new MonsterDiagnostic("fake", "captured")); }
        public MonsterResult<MonsterLease> RegisterBehavior(string owner, string id, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report)
        { BehaviorOwner = owner; BehaviorGuard = guard; BehaviorReport = report; return MonsterResult<MonsterLease>.Ok(new MonsterLease(() => { })); }
        public MonsterResult<MonsterLease> RegisterBehavior(string owner, string id, MonsterBehaviorRequirements requirements, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report) => RegisterBehavior(owner, id, factory, guard, report);
        public MonsterResult<MonsterInstanceSnapshot> Spawn(string owner, MonsterSpawnRequest r) => throw new NotSupportedException();
        public IReadOnlyList<MonsterInstanceSnapshot> Instances(string owner) => Array.Empty<MonsterInstanceSnapshot>();
        public MonsterResult<MonsterInstanceSnapshot> Instance(string owner, MonsterHandle handle) => MonsterResult<MonsterInstanceSnapshot>.Fail(new MonsterDiagnostic("StaleHandle", "unknown"));
        public MonsterResult<bool> Despawn(string owner, MonsterHandle handle) => throw new NotSupportedException();
        public MonsterResult<bool> CancelSequence(string owner, MonsterHandle handle) { Owners.Add(owner); return MonsterResult<bool>.Ok(true); }
        public virtual int RemoveOwner(string owner) { Removed.Add(owner); return Overrides.RemoveOwner(owner); }
        public virtual int CountOwnerResources(string owner) => Overrides.CountOwner(owner);
        public virtual void Dispose() { if (ThrowDispose) throw new InvalidOperationException("injected drain failure"); Overrides.Clear(); Disposed = true; }
    }
    sealed class Behavior : IMonsterBehavior
    { public void Start(IMonsterBehaviorContext c) { } public void Tick(float dt) { } public void Stop() { } }
    sealed class Manifest : IManifest
    {
        public Manifest(string id) { UniqueID = id; }
        public string UniqueID { get; } public string Name => UniqueID; public string Author => "QA"; public string Version => "1.0.0";
        public string Description => ""; public string EntryDll => ""; public string EntryType => ""; public string MinimumDTMApiVersion => ""; public string MinimumGameVersion => ""; public string Type => "CodeMod";
        public IReadOnlyList<IManifestDependency> Dependencies => Array.Empty<IManifestDependency>(); public IReadOnlyList<string> UpdateKeys => Array.Empty<string>();
    }
}
