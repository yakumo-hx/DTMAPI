using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using DolocTown.Config.Item;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule : IMonsterBackend, IDefinitionPublisher, IMonsterBehaviorMetadataBackend, IMonsterAudioBackend
    {
        private const string HarmonyOwner = "DTMAPI.Monsters.Native";
        private static NativeModule? current;
        private readonly IRuntimeModuleContext moduleContext;
        private readonly Func<string, IManifest?> ownerMetadata;
        private readonly IDtmRuntimeContext context;
        private readonly Harmony harmony = new Harmony(HarmonyOwner);
        private readonly DefinitionRegistry definitions;
        private readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
        private readonly Dictionary<string, Template> templates = new Dictionary<string, Template>(StringComparer.Ordinal);
        private readonly Dictionary<Monster, InstanceScope> instances = new Dictionary<Monster, InstanceScope>();
        private readonly Dictionary<MonsterController, InstanceScope> controllers = new Dictionary<MonsterController, InstanceScope>();
        private readonly Dictionary<MonsterHandle, InstanceScope> handles = new Dictionary<MonsterHandle, InstanceScope>();
        private readonly HashSet<string> verified = new HashSet<string>(StringComparer.Ordinal);
        private IDisposable? contextSubscription;
        private object? tablesIdentity, assetsIdentity, entityIdentity;
        private long generation, instanceSequence;
        private bool disposed, nativeStateFaulted, contentMergeDirty, retiringGeneration;
        private IDtmScheduledWork? generationRetry;
        private InstanceScope? creating;
        internal string FaultStage = "";
        public bool Ready => !retiringGeneration && NativeAvailable;
        private bool NativeAvailable => !disposed && !nativeStateFaulted && DolocConfig.Tables != null && DolocAPI.assets?.monsters != null && DolocAPI.assets.bullets != null && DolocAPI.assets.bulletMovers != null && DolocAPI.assets.skills != null && DolocAPI.EntitySystem != null;
        public long Generation => generation;
        public MonsterModuleState State => disposed ? MonsterModuleState.DormantResident : nativeStateFaulted ? MonsterModuleState.Failed : retiringGeneration ? MonsterModuleState.WaitingNative : Ready ? MonsterModuleState.Active : MonsterModuleState.WaitingNative;
        public IReadOnlyList<MonsterTemplate> Templates => templates.Values.Select(t => { var value = t.Snapshot(generation, verified.Contains(t.Proto.Name)); value.Presentation = PresentationInfo(t); value.Actions = DescribeActions(t); value.NativeOverrideFields = NativeFields(t); return value; }).OrderBy(t => t.Id, StringComparer.Ordinal).ToArray();

        public NativeModule(IRuntimeModuleContext moduleContext, Func<string, IManifest?> ownerMetadata)
        {
            if (current != null) throw new InvalidOperationException("Monster module already active.");
            this.moduleContext = moduleContext; this.ownerMetadata = ownerMetadata;
            context = moduleContext.Runtime;
            definitions = new DefinitionRegistry(this, RequireThread);
            nativeOverrides = new NativeOverrideRegistry(RequireThread, OverrideBaseline, AdmitNativeOverride, RefreshNativeContent);
            current = this;
            try
            {
                using (OptimizationTrace.Begin(0)) moduleContext.AuthorizePatches(InstallHooks);
                contextSubscription = context.Subscribe(OnContext);
                Observe("Module.LateAttach");
            }
            catch { ClearNativeContent(); contextSubscription?.Dispose(); harmony.UnpatchSelf(); current = null; throw; }
        }
        private void RequireThread() { if (!context.IsMainThread) throw new InvalidOperationException("WrongThread"); if (disposed) throw new ObjectDisposedException(nameof(NativeModule)); }
        internal void Log(string value) => moduleContext.Monitor.Log("MC " + value + "; thread=" + Thread.CurrentThread.ManagedThreadId + "; generation=" + generation);
        private void OnContext(DtmRuntimeContextSnapshot value)
        {
            Log("Context " + value.Phase + " world=" + value.WorldEpoch);
            if (value.Phase == DtmRuntimePhase.ReturningToTitle || value.Phase == DtmRuntimePhase.Title || value.Phase == DtmRuntimePhase.ShuttingDown)
            { CancelNianDeathEffects(_ => true); StopCommonSounds(r => r.Use == "dead"); }
            if (value.Phase == DtmRuntimePhase.ReturningToTitle || value.Phase == DtmRuntimePhase.Title || value.Phase == DtmRuntimePhase.ShuttingDown)
            { RemoveInstances(_ => true); encounters.Clear(); nativeAssignments = new System.Runtime.CompilerServices.ConditionalWeakTable<Monster, SpawnSlotAssignment>(); allocatingMonsters = new System.Runtime.CompilerServices.ConditionalWeakTable<Monster, NativeAllocation>(); pendingAllocations.Clear(); foreach (var rule in spawnRules.Values) { rule.Generation = 0; rule.Spawned = 0; } }
            if (value.Phase == DtmRuntimePhase.ReturningToTitle || value.Phase == DtmRuntimePhase.Title || value.Phase == DtmRuntimePhase.ShuttingDown) { ClearMissingDocuments(); ClearNativeInstances(); }
            if (value.Phase == DtmRuntimePhase.WorldReady) { Observe("WorldReady"); RefreshRoomRules(); }
        }
        private void Observe(string source)
        {
            using var trace = OptimizationTrace.Begin(1);
            if (disposed) return;
            Log("Observe " + source + " ready=" + Ready);
            if (!NativeAvailable) return;
            bool changed = contentMergeDirty || !ReferenceEquals(tablesIdentity, DolocConfig.Tables) || !ReferenceEquals(assetsIdentity, DolocAPI.assets) || !ReferenceEquals(entityIdentity, DolocAPI.EntitySystem);
            if (!changed) return;
            ClearNativeContent();
            nativeTints.Clear();
            ClearMissingDocuments();
            if (assetsIdentity != null)
            {
                // A table-only reload may change inherited fields too. Rebuild all owned bindings
                // against the new generation instead of republishing a row from the old table.
                var retiring = bindings.Values.ToArray();
                // A reference refresh must not reopen a definition closed by failed cleanup.
                foreach (var binding in retiring)
                    if (binding.Definition.State == MonsterDefinitionState.Creatable) binding.Definition.State = MonsterDefinitionState.WaitingNative;
                try
                {
                    CancelNianDeathEffects(_ => true);
                    RemoveInstances(_ => true);
                    foreach (var binding in retiring) RemoveBinding(binding);
                    RetireDropPools();
                }
                catch (Exception ex)
                {
                    nativeStateFaulted = ex is NativeIdentityConflictException;
                    retiringGeneration = !nativeStateFaulted;
                    contentMergeDirty = true;
                    foreach (var binding in retiring)
                    {
                        if (binding.Definition.State != MonsterDefinitionState.WaitingNative) continue;
                        binding.Definition.State = nativeStateFaulted ? MonsterDefinitionState.RestartRequired : MonsterDefinitionState.WaitingNative;
                        binding.Definition.Diagnostic = Error(binding.Definition, nativeStateFaulted ? "RestartRequired" : "CleanupPending", "generation", ex.Message);
                    }
                    if (retiringGeneration && generationRetry == null)
                        generationRetry = moduleContext.Scheduler.Delay(() => { generationRetry = null; Observe("Generation.CleanupRetry"); }, TimeSpan.FromMilliseconds(100));
                    Log("Native generation retirement: " + (nativeStateFaulted ? "RestartRequired" : "CleanupPending") + "; " + ex.Message);
                    return;
                }
            }
            tablesIdentity = DolocConfig.Tables; assetsIdentity = DolocAPI.assets; entityIdentity = DolocAPI.EntitySystem;
            contentMergeDirty = false; retiringGeneration = false; generationRetry?.TryCancel(); generationRetry = null;
            generation++; verified.Clear(); templates.Clear();
            // A new native resource generation must revalidate the installed source hashes.
            framesLoaded = false; framesError = ""; frameTemplates.Clear();
            foreach (MonsterProto proto in DolocAPI.assets.monsters.TotalProtos.ToArray())
            {
                if (bindings.ContainsKey(proto.Name)) continue;
                var template = Inspect(proto);
                templates.Add(proto.Name, template);
                Log("Template id=" + proto.Name + " canInherit=" + (template.Errors.Count == 0) + " ai=" + template.Ai.FullName + " body=" + proto.MoverProto?.GetType().Name + " errors=" + string.Join(",", template.Errors.Select(e => e.Code)));
            }
            ValidateNativeShips();
            ValidateNativeNians();
            ValidateTrafficLights();
            ValidateBastions();
            ValidateTraining();
            ValidateNativeCalls();
            RefreshNativeOverrides();
            RefreshDropPools();
            definitions.Refresh();
            foreach (var rule in spawnRules.Values.Where(r => r.State == MonsterContentState.Ready || r.State == MonsterContentState.WaitingNative)) ValidateRule(rule);
        }
        private Template Inspect(MonsterProto proto)
        {
            using var trace = OptimizationTrace.Begin(2);
            var errors = new List<MonsterDiagnostic>();
            var row = DolocConfig.Tables.TbMonster.GetOrDefault(proto.Name);
            GameObject? prefab = null;
            var pools = NativeAccess.Dictionary(DolocAPI.EntitySystem.GetGameEntityManager<MonsterController>(), "nestedPools");
            if (pools.Contains(proto.Name)) prefab = (GameObject?)pools[proto.Name]!.GetType().GetProperty("Prefab")!.GetValue(pools[proto.Name]);
            var aiTypes = (IDictionary)NativeAccess.Field(typeof(MonsterAI), "_aiTypes").GetValue(null)!;
            var ai = (Type?)aiTypes[proto.Name] ?? typeof(MonsterAI_Default);
            void Error(string code, string field, string message) => errors.Add(new MonsterDiagnostic(code, message, templateId: proto.Name, stage: "Template", field: field, recovery: "Check the current native resource/factory boundary."));
            if (typeof(MonsterAI_Group).IsAssignableFrom(ai) && ai.GetMethod("OnAddedToGroup")!.DeclaringType != typeof(MonsterAI_Group))
                Error("AdditionalLifecycleOwner", "ai.group", "This group admission overrides the common membership lifetime.");
            if (row == null) Error("ResourceMissing", "table", "Template has no current TbMonster row.");
            if (prefab == null) Error("ResourceMissing", "pool", "Template has no registered controller pool/prefab.");
            if (proto.MoverProto == null || proto.MoverProto.pathFinderProto == null) Error("ResourceMissing", "mover", "Template mover/pathfinder is incomplete.");
            if (proto.AttackBehaviourProtos == null || proto.AttackBehaviourProtos.Any(a => a == null)) Error("ResourceMissing", "attacks", "Template attack inputs are incomplete.");
            foreach (var attack in proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>())
            {
                // MonsterController.InitAttackBehaviours never constructs Unset entries.
                // Validate the array above, but only admit dependencies that native execution owns.
                if (attack == null || attack.AttackId == MonsterAttackId.Unset) continue;
                if (attack is IHasBullet bullet)
                {
                    if (!DolocAPI.assets.bullets.QueryData(bullet.BulletId, out _)) Error("ResourceMissing", "attacks.bullet", "Missing bullet: " + bullet.BulletId);
                    if (!DolocAPI.assets.bulletMovers.QueryData(bullet.BulletMoverId, out _)) Error("ResourceMissing", "attacks.bulletMover", "Missing bullet mover: " + bullet.BulletMoverId);
                }
                if (attack is IMonsterAttackBehaviourSkill skill)
                {
                    if (!DolocAPI.assets.skills.QueryData(skill.SkillId, out _)) Error("ResourceMissing", "attacks.skill", "Missing skill: " + skill.SkillId);
                    else if (NativeSkillAdmission(skill) is string reason) Error("AdditionalLifecycleOwner", "attacks.skill", reason);
                }
            }
            if (prefab != null)
            {
                if (!prefab.GetComponent<MonsterController>() || !prefab.GetComponent<MonsterRenderer>() || !prefab.GetComponent<Collider2D>()) Error("ResourceMissing", "prefab", "Required controller/renderer/collider missing.");
                if ((proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>()).Any(a => a != null && a.AttackId != MonsterAttackId.Unset && a is IShadowImpact)
                    && (prefab.GetComponent<SpriteRenderer>() == null || prefab.GetComponentInChildren<PhysicalDamageBox>(true) == null))
                    Error("ResourceMissing", "attacks.shadowBody", "ShadowImpact requires the native root sprite and body damage box.");
                if ((proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>()).Any(a => a != null && a.AttackId != MonsterAttackId.Unset && a is IChomperMimicry)
                    && (prefab.GetComponent<Animator>()?.runtimeAnimatorController == null || prefab.GetComponent<SpriteRenderer>() == null))
                    Error("ResourceMissing", "attacks.mimicryBody", "ChomperMimicry requires the native root sprite and drill animation controller.");
                var decorator = prefab.GetComponent<MonsterDecorator>();
                if (!decorator) Error("ResourceMissing", "decorator", "No decorator.");
                else if (!IsShipDecorator(decorator) && !IsNianDecorator(decorator) && !IsBastionDecorator(decorator) && !IsTrainingDecorator(decorator) && new[] { "OnMonsterLoaded", "OnDead" }.Any(name => decorator.GetType().GetMethod(name)!.DeclaringType != typeof(MonsterDecorator)))
                    Error("AdditionalLifecycleOwner", "decorator", "Template overrides creation/death ownership; its child/story boundary has not been admitted.");
            }
            return new Template(proto, row, prefab, ai, errors);
        }
        public MonsterResult<MonsterDefinitionLease> Register(string owner, MonsterDefinition definition) => Register(owner, 0, definition);
        public MonsterResult<MonsterDefinitionLease> Register(string owner, int loadIndex, MonsterDefinition definition)
        {
            RequireThread();
            if (nativeStateFaulted) return MonsterResult<MonsterDefinitionLease>.Fail(new MonsterDiagnostic("RestartRequired", "Native generation cleanup failed; restart before registering content.", owner, stage: "Generation"));
            var result = definitions.Register(owner, definition);
            if (!result.Success) return result;
            MonsterOverrideLease? policy = null;
            var lease = new MonsterDefinitionLease(() => result.Value!.Snapshot, () =>
            { result.Value!.Dispose(); policy?.Dispose(); RefreshNativeOverrides(); });
            try
            {
                RefreshNativeOverrides();
                if (definition.Drops.Mode != MonsterDropMode.Inherit)
                {
                    var drops = nativeOverrides.Register(owner, loadIndex, new MonsterNativeOverride { LocalId = definition.LocalId, TargetId = owner + ":" + definition.LocalId, Drops = definition.Drops }, definitionPolicy: true);
                    policy = drops.Value;
                    if (!drops.Success) return MonsterResult<MonsterDefinitionLease>.FailedOwned(drops.Diagnostic!, lease);
                }
                return MonsterResult<MonsterDefinitionLease>.Ok(lease);
            }
            catch (Exception ex) { return MonsterResult<MonsterDefinitionLease>.FailedOwned(new MonsterDiagnostic("NativeRegistrationFailed", ex.Message, owner), lease); }
        }

        public MonsterDiagnostic? Publish(DefinitionRecord definition)
        {
            RequireThread();
            if (definition.Definition.Behavior == "code-provider" && (!behaviors.TryGetValue(definition.Definition.BehaviorId, out var behavior) || behavior.Closed))
                return Error(definition, "BehaviorUnavailable", "behaviorId", "Register the behavior factory before its definitions.");
            if (!templates.TryGetValue(definition.Definition.TemplateId, out var template)) return Error(definition, "TemplateMissing", "templateId", "Template does not exist in the current native directory.");
            var composition = ComposeTemplate(definition, ref template); if (composition != null) return composition;
            if (template.Errors.Count > 0) return Error(definition, template.Errors[0].Code, template.Errors[0].Field, template.Errors[0].Message);
            MonsterDiagnostic? admission;
            using (OptimizationTrace.Begin(10)) admission = AdmitDefinition(definition, template);
            if (admission != null) return admission;
            if (bindings.TryGetValue(definition.NativeId, out var existing))
            {
                var table = DolocConfig.Tables.TbMonster;
                if (table.DataMap.TryGetValue(definition.NativeId, out var other) && !ReferenceEquals(other, existing.Row)) return Error(definition, "DuplicateId", "nativeId", "Another owner replaced the native row.");
                if (!table.DataMap.ContainsKey(definition.NativeId)) { table.DataMap.Add(definition.NativeId, existing.Row); table.DataList.Add(existing.Row); }
                existing.Table = table;
                return null;
            }
            var row = template.Row!;
            var dropError = ResolveDropEntry(definition.Definition.Drops, row.DropSpawnEntry, definition.Owner, definition.Id, out var dropEntry);
            if (dropError != null) return dropError;
            var body = definition.Definition.Body;
            var copy = new MonsterInfo(definition.NativeId, row.GenType, PhysicalValidation.Volume(body, row.Size),
                new Vector2(body?.SpawnOffsetX ?? row.Offset.x, body?.SpawnOffsetY ?? row.Offset.y), row.Prefab,
                definition.Definition.Health ?? row.Health, definition.Definition.Defense ?? row.Defense, definition.Definition.HurtDuration ?? row.HurtDuration,
                row.DropSpawnEntry!, definition.Definition.ExpValue ?? row.ExpValue, row.HummingSound, row.HurtSound, row.DeadSound, row.DeadEffects);
            var binding = new Binding(definition, template, copy, DolocConfig.Tables.TbMonster, DolocAPI.assets.monsters,
                DolocAPI.EntitySystem.GetGameEntityManager<MonsterController>());
            if (binding.Table.DataMap.ContainsKey(definition.NativeId) || NativeAccess.Dictionary(binding.Database, "protos").Contains(definition.NativeId) || NativeAccess.Dictionary(binding.Manager, "nestedPools").Contains(definition.NativeId))
                return Error(definition, "DuplicateId", "nativeId", "Native identity is already occupied; no publication was attempted.");
            string stage = "Prepare";
            try
            {
                using var publicationTrace = OptimizationTrace.Begin(11);
                var presentationError = PreparePresentation(binding);
                if (presentationError != null) { ReleasePresentation(binding); return presentationError; }
                var animationError = PrepareAnimations(binding);
                if (animationError != null) { ReleasePresentation(binding); return animationError; }
                var compendiumError = PrepareCompendium(binding);
                if (compendiumError != null) { ReleaseCompendium(binding); ReleasePresentation(binding); return compendiumError; }
                foreach (var sound in SoundValidation.CommonEvents(definition.Definition)) binding.CommonSounds.Add(sound.Key, sound.Value);
                binding.Container = new GameObject(definition.NativeId);
                binding.Container.SetActive(false);
                binding.Container.transform.SetParent(DolocAPI.EntitySystem.root, false);
                binding.Prefab = Object.Instantiate(template.Prefab!, binding.Container.transform);
                binding.Prefab.name = definition.NativeId + "_template";
                binding.Prefab.SetActive(false);
                PreparePhysical(binding);
                PrepareTemplateParameters(binding);
                binding.Container.SetActive(true);
                binding.PoolManager = new GameEntityManager<MonsterController>(binding.Container.transform, binding.Prefab, 5, 3, true, definition.NativeId);
                stage = "Table"; Fault(stage);
                binding.Table.DataMap.Add(definition.NativeId, copy); binding.Table.DataList.Add(copy);
                stage = "Compendium"; PublishCompendium(binding);
                stage = "Proto"; Fault(stage);
                if (!binding.Database.AddMonster(binding.Proto)) throw new InvalidOperationException("Native proto publication rejected.");
                stage = "Pool"; Fault(stage);
                if (!binding.Manager.ComposeManager(binding.PoolManager, definition.NativeId)) throw new InvalidOperationException("Native pool publication rejected.");
                PreparePrivateParts(binding);
                stage = "Binding"; Fault(stage);
                bindings.Add(definition.NativeId, binding);
                RefreshOpenCompendium();
                Log("Published definition=" + definition.Id + " template=" + template.Proto.Name);
                return null;
            }
            catch (Exception ex)
            {
                try { RemoveBinding(binding); }
                catch (Exception cleanup) { bindings[definition.NativeId] = binding; return Error(definition, "RestartRequired", stage, ex.Message + "; cleanup=" + cleanup.Message); }
                return Error(definition, "RegistrationFailed", stage, ex.Message);
            }
        }
        private void Fault(string stage)
        {
            if (FaultStage != stage && FaultStage != stage + ":persistent") return;
            if (FaultStage == stage) FaultStage = "";
            throw new InvalidOperationException("Injected failure at " + stage);
        }
        private static MonsterDiagnostic Error(DefinitionRecord definition, string code, string field, string message) => new MonsterDiagnostic(code, message, definition.Owner, definition.Id, definition.Definition.TemplateId, "Publish", field, code == "RestartRequired" ? "Restart the game." : "Correct the definition or native input.");
        public MonsterDiagnostic? Withdraw(DefinitionRecord definition)
        {
            try
            {
                MarkSoundWithdrawal(s => s.Binding.Definition == definition);
                RemoveDefinitionRules(definition.Id);
                CancelNianDeathEffects(s => s.Binding.Definition == definition);
                RemoveInstances(s => s.Binding.Definition == definition);
                if (instances.Values.Any(s => s.Binding.Definition == definition && (s.NativeCallDepth > 0 || s.NativeSkillDepth > 0 || s.PendingGroupRelease)))
                    throw new InvalidOperationException("Native continuation or group broadcast is still returning; retry withdrawal after its callback returns.");
                if (bindings.TryGetValue(definition.NativeId, out var binding)) RemoveBinding(binding);
                RefreshNativeOverrides();
                return null;
            }
            catch (Exception ex) { return Error(definition, ex is NativeIdentityConflictException ? "RestartRequired" : "CleanupPending", "withdraw", ex.Message); }
        }
        private void RemoveBinding(Binding binding)
        {
            foreach (var part in binding.PrivateParts.Values.ToArray()) RemoveBinding(part);
            binding.PrivateParts.Clear();
            string key = binding.Proto.Name;
            var pools = NativeAccess.Dictionary(binding.Manager, "nestedPools");
            var protos = NativeAccess.Dictionary(binding.Database, "protos");
            CheckCompendiumOwnership(binding);
            // Validate the entire identity set before removing any registration. If another
            // owner replaced one entry, retain the others and close the definition for restart.
            if (binding.PoolManager != null && pools.Contains(key))
            {
                if (!ReferenceEquals(pools[key], NativeAccess.Get(binding.PoolManager, "pool"))) throw new NativeIdentityConflictException("Pool ownership changed; refusing removal.");
            }
            if (protos.Contains(key))
            {
                var proto = (MonsterProto)protos[key]!;
                if (!ReferenceEquals(proto.MoverProto, binding.Proto.MoverProto) || !ReferenceEquals(proto.AttackBehaviourProtos, binding.Proto.AttackBehaviourProtos)) throw new NativeIdentityConflictException("Proto ownership changed; refusing removal.");
            }
            if (binding.Table.DataMap.TryGetValue(key, out var row))
            {
                if (!ReferenceEquals(row, binding.Row)) throw new NativeIdentityConflictException("Table ownership changed; refusing removal.");
            }
            ReleaseSounds(binding); // Keep native identity registrations intact if sound retirement needs a retry.
            if (binding.PoolManager != null && pools.Contains(key)) { ((GameEntityManager)binding.PoolManager).Clear(); pools.Remove(key); }
            if (protos.Contains(key)) protos.Remove(key);
            if (row != null) { binding.Table.DataMap.Remove(key); binding.Table.DataList.Remove(row); }
            if (binding.Container) Object.Destroy(binding.Container);
            foreach (var resource in binding.ParameterResources) if (resource != null) Object.Destroy(resource);
            binding.ParameterResources.Clear();
            ReleaseCompendium(binding);
            ReleasePresentation(binding);
            bindings.Remove(key);
            RefreshOpenCompendium();
        }
        public int RemoveOwner(string owner)
        {
            RequireThread();
            MarkSoundWithdrawal(s => s.Binding.Definition.Owner == owner);
            foreach (var rule in spawnRules.Values.Where(r => r.Owner == owner && r.State != MonsterContentState.Withdrawn).ToArray()) WithdrawRule(rule);
            RemoveInstances(s => s.Binding.Definition.Owner == owner);
            int count = definitions.RemoveOwner(owner) + nativeOverrides.RemoveOwner(owner);
            foreach (var behavior in behaviors.Values.Where(b => b.Owner == owner && b.Factory != null).ToArray()) { ReleaseBehavior(behavior); count++; }
            foreach (var pool in dropPools.Values.Where(p => p.Owner == owner && p.State != MonsterContentState.Withdrawn).ToArray()) { WithdrawDropPool(pool); count++; }
            foreach (var manager in nativeSkillManagers.Values.Where(m => m.Leases.Count == 0).ToArray()) DrainNativeSkillManager(manager);
            return count;
        }
        public int CountOwnerResources(string owner) => nativeOverrides.CountOwner(owner) + definitions.CountOwnerResources(owner) + instances.Values.Count(s => s.Binding.Definition.Owner == owner) + behaviors.Values.Count(b => b.Owner == owner && b.Factory != null)
            + nativeSkills.Values.Count(s => s.Scope.Binding.Definition.Owner == owner) + nativeBooms.Values.Count(b => b.Source.Scope.Binding.Definition.Owner == owner)
            + nianDeathEffects.Values.Count(s => s.Binding.Definition.Owner == owner) + CountSoundResources(owner)
            + dropPools.Values.Count(p => p.Owner == owner && p.State != MonsterContentState.Withdrawn)
            + spawnRules.Values.Count(r => r.Owner == owner && r.State != MonsterContentState.Withdrawn);
        public void Dispose()
        {
            if (disposed) return;
            RequireThread();
            nativeOverrides.Clear();
            ClearNativeContent();
            RemoveInstances(_ => true);
            foreach (var manager in nativeSkillManagers.Values.ToArray()) DrainNativeSkillManager(manager);
            foreach (var definition in definitions.Records)
            {
                var error = Withdraw(definition);
                if (error != null) throw new InvalidOperationException(error.Code + ": " + error.Message);
            }
            foreach (var pool in dropPools.Values.ToArray()) WithdrawDropPool(pool);
            foreach (var rule in spawnRules.Values.ToArray()) WithdrawRule(rule);
            encounters.Clear();
            DisposeSounds();
            DisposeCompendium();
            contextSubscription?.Dispose();
            generationRetry?.TryCancel(); generationRetry = null;
            behaviors.Clear();
            if (overlay != null) Object.Destroy(overlay);
            if (overlayFont != null) Object.Destroy(overlayFont);
            overlay = null; overlayFont = null; overlayDriver = null;
            harmony.UnpatchSelf(); current = null; disposed = true;
            OptimizationTrace.Stop();
            Log("Stopped instances=" + instances.Count + " bindings=" + bindings.Count);
        }

        private sealed class Template
        {
            internal readonly MonsterProto Proto;
            internal readonly MonsterInfo? Row;
            internal readonly GameObject? Prefab;
            internal readonly Type Ai;
            internal readonly List<MonsterDiagnostic> Errors;
            internal Template(MonsterProto proto, MonsterInfo? row, GameObject? prefab, Type ai, List<MonsterDiagnostic> errors) { Proto = proto; Row = row; Prefab = prefab; Ai = ai; Errors = errors; }
            internal MonsterTemplate Snapshot(long generation, bool verified) => new MonsterTemplate(Proto.Name, generation, Errors.Count == 0, verified, Proto.MoverProto?.GetType().Name ?? "", Ai.FullName!, Row?.Health ?? 0, Row?.Defense ?? 0, Application.version, Errors)
            { GenerationType = Proto.GenType.ToString(), Volume = Proto.Volume, ExpValue = Row?.ExpValue, HurtDuration = Row?.HurtDuration, NativeParameters = DescribeTemplateParameters(this), Physical = PhysicalInfo(this), SoundEvents = TemplateSoundEvents(this), HummingLoop = HummingLoopDefaults.Resolve(Row?.HummingSound ?? ""),
                Reference = new MonsterCatalogEntry(Proto.Name, "Template", EffectiveSource, Application.version, Fingerprint(Row + "|" + Ai.FullName + "|" + Proto.GenType + "|" + Proto.Volume), Errors) };
        }
        private sealed class Binding
        {
            internal readonly DefinitionRecord Definition;
            internal readonly Template Template;
            internal readonly MonsterInfo Row;
            internal TbMonster Table;
            internal readonly MonsterDatabase Database;
            internal readonly GameEntityManager<MonsterController> Manager;
            internal readonly MonsterProto Proto;
            internal readonly bool NativeChild;
            internal readonly Dictionary<string, Binding> PrivateParts = new Dictionary<string, Binding>(StringComparer.Ordinal);
            internal readonly IMonsterAttackBehaviour[]? NativePartAttacks;
            internal GameObject? Container, Prefab;
            internal GameEntityManager<MonsterController>? PoolManager;
            internal SkinResources? Skin;
            internal readonly List<Object> ParameterResources = new List<Object>();
            internal readonly Dictionary<string, MonsterSound> CommonSounds = new Dictionary<string, MonsterSound>(StringComparer.Ordinal);
            internal CompendiumResources? Compendium;
            internal readonly Dictionary<string, AnimationResources> Animations = new Dictionary<string, AnimationResources>(StringComparer.Ordinal);
            internal Binding(DefinitionRecord definition, Template template, MonsterInfo row, TbMonster table, MonsterDatabase database, GameEntityManager<MonsterController> manager, bool nativeChild = false, IReadOnlyList<MonsterNativeAttackParameters>? nativePartAttacks = null, string? privatePartId = null)
            { Definition = definition; Template = template; Row = row; Table = table; Database = database; Manager = manager; NativeChild = nativeChild;
                NativePartAttacks = nativeChild && nativePartAttacks != null ? DefinitionAttacks(template, nativePartAttacks) : null;
                Proto = privatePartId != null ? new MonsterProto(privatePartId, template.Proto.MoverProto, NativePartAttacks ?? template.Proto.AttackBehaviourProtos) : nativeChild ? template.Proto : new MonsterProto(definition.NativeId, DefinitionMover(template, definition.Definition), DefinitionAttacks(template, definition.Definition.NativeAttacks)); }
        }
    }
}
