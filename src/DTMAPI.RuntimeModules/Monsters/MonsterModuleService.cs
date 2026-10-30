using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.MonsterContent.Hosting;

namespace DTMAPI.MonsterContent
{
    internal sealed class MonsterService : IRuntimeModule
    {
        private IRuntimeModuleContext context = null!;
        private IMonsterBackend? backend;
        private readonly List<Contribution> active = new List<Contribution>();
        private bool closed, wasActive;
        public void Register(IRuntimeModuleContext value)
        { context = value; value.RegisterApi<IMonsterContentApi>(owner => new Facade(this, owner)); value.RegisterApi<IMonsterCatalogApi>(owner => new Facade(this, owner)); }
        public void BeginClose() { closed = true; }
        public RuntimeModuleCleanup TryClose()
        {
            BeginClose();
            try { backend?.Dispose(); backend = null; return new RuntimeModuleCleanup(0); }
            catch (Exception ex) { return new RuntimeModuleCleanup(1, ex.GetBaseException().Message); }
        }
        private IMonsterBackend Backend()
        {
            if (backend != null) return backend;
            backend = context.CreateBackend<IMonsterBackend>("DTMAPI.Modules.Monsters", context,
                (Func<string, IManifest?>)(id => active.FirstOrDefault(c => c.Owner.Manifest.UniqueID == id)?.Owner.Manifest));
            wasActive = true; return backend;
        }
        private sealed class Contribution : IDisposable
        {
            private readonly MonsterService host;
            internal readonly IRuntimeModuleOwnerContext Owner;
            internal IDisposable? Native, Sound;
            internal Contribution(MonsterService host, IRuntimeModuleOwnerContext owner) { this.host = host; Owner = owner; host.active.Add(this); }
            public void Dispose()
            {
                Native?.Dispose(); Native = null; // Skills/instances precede sound bindings.
                Sound?.Dispose(); Sound = null;
                if (host.active.All(c => c == this)) { host.backend?.Dispose(); host.backend = null; }
                host.active.Remove(this);
            }
        }
        private sealed class Facade : IMonsterContentApi, IMonsterCatalogApi
        {
            private readonly MonsterService host;
            private readonly IRuntimeModuleOwnerContext owner;
            private string Id => owner.Manifest.UniqueID;
            internal Facade(MonsterService host, IRuntimeModuleOwnerContext owner) { this.host = host; this.owner = owner; }
            private MonsterDiagnostic Error(string code, string message) => new MonsterDiagnostic(code, message, Id, stage: "Module");
            private MonsterDiagnostic? Check(bool native = false)
            {
                if (!host.context.Runtime.IsMainThread) return Error("WrongThread", "Use the Runtime scheduler.");
                if (host.closed) return Error("OwnerClosed", "Monster provider is closed.");
                try { owner.Guard(); } catch (Exception ex) { return Error("OwnerClosed", ex.GetBaseException().Message); }
                if (native && host.backend == null) return Error("ModuleNotRequested", "Register content or acquire an optional prewarm lease first.");
                return null;
            }
            private MonsterDiagnostic? Dependency(string qualified, string code)
            {
                string provider = qualified.Split(':')[0];
                return provider == Id || owner.HasRequiredDependency(provider) ? null : Error(code, "Cross-Owner use requires an available Required package dependency with its declared version constraint.");
            }
            private MonsterResult<T> Register<T>(string name, Func<IMonsterBackend, MonsterResult<T>> prepare, Func<T, Action, T> wrap, IDisposable? sounds = null, IEnumerable<string>? references = null) where T : class, IDisposable
            {
                var error = Check(); if (error != null) { sounds?.Dispose(); return MonsterResult<T>.Fail(error); }
                var scope = owner.CreateContribution(name); scope.UseProvider(MonsterModule.ProviderId);
                if (references != null) foreach (string provider in references.Where(v => !string.IsNullOrEmpty(v)).Select(v => v.Split(':')[0]).Where(v => v != Id).Distinct(StringComparer.OrdinalIgnoreCase)) scope.UseProvider(provider);
                T? native = null;
                try
                {
                    scope.Activate(() => new Contribution(host, owner) { Sound = sounds }, contribution =>
                    {
                        var result = prepare(host.Backend()); contribution.Native = result.Value;
                        if (!result.Success) { error = result.Diagnostic; throw new InvalidOperationException(error?.Message); }
                        native = result.Value!;
                    });
                    return MonsterResult<T>.Ok(wrap(native!, () => { scope.Close(); if (!scope.TryClose(out string failure)) throw new InvalidOperationException("MonsterCleanupPending: " + failure); }));
                }
                catch (Exception ex) { scope.Close(); scope.TryClose(out _); return MonsterResult<T>.Fail(error ?? Error("RegistrationFailed", ex.GetBaseException().Message)); }
            }
            public MonsterResult<MonsterLease> RequestModule() => Register("monster-prewarm", _ => MonsterResult<MonsterLease>.Ok(new MonsterLease(() => { })), (_, release) => new MonsterLease(release));
            public MonsterResult<MonsterModuleState> GetState()
            { var error = Check(); return error == null ? MonsterResult<MonsterModuleState>.Ok(host.backend?.State ?? (host.wasActive ? MonsterModuleState.DormantResident : MonsterModuleState.NotRequested)) : MonsterResult<MonsterModuleState>.Fail(error); }
            public MonsterResult<MonsterDefinitionLease> RegisterDefinition(MonsterDefinition value)
            {
                var error = Check() ?? MonsterValidation.Validate(Id, value);
                if (error?.Code == "InvalidSound") { owner.Report("monsters.sounds", error.Message); value = value.Copy(); SoundValidation.ReleaseInputs(value); error = MonsterValidation.Validate(Id, value); }
                if (error == null && value.Behavior == "code-provider") error = Dependency(value.BehaviorId, "BehaviorDependencyRequired");
                if (error == null && value.Drops.ReferencesPool && value.Drops.Source == MonsterPoolSource.Managed) error = Dependency(value.Drops.PoolId, "DropPoolDependencyRequired");
                if (error != null) return MonsterResult<MonsterDefinitionLease>.Fail(error);
                var copy = value.Copy(); var sound = BindSounds(SoundValidation.CommonEvents(copy));
                return Register("monster-definition", backend => backend is IMonsterOrderedDefinitionBackend ordered ? ordered.Register(Id, owner.LoadOrder, copy) : backend.Register(Id, copy),
                    (native, release) => new MonsterDefinitionLease(() => native.Snapshot, release), sound, new[] { copy.Behavior == "code-provider" ? copy.BehaviorId : "", copy.Drops.ReferencesPool && copy.Drops.Source == MonsterPoolSource.Managed ? copy.Drops.PoolId : "" });
            }
            public MonsterResult<MonsterOverrideLease> RegisterOverride(MonsterNativeOverride value)
            {
                var error = Check() ?? MonsterOverrideAdmission.Validate(Id, value);
                if (error?.Code == "InvalidSound") { owner.Report("monsters.sounds", error.Message); value = value.Copy(); value.Sounds = null; SoundValidation.ReleaseInputs(new MonsterDefinition { NativeAttacks = value.NativeAttacks }); error = MonsterOverrideAdmission.Validate(Id, value); }
                if (error == null && MonsterValidation.ValidQualifiedId(value.TargetId)) error = Dependency(value.TargetId, "DefinitionDependencyRequired");
                if (error == null && value.Drops?.ReferencesPool == true && value.Drops.Source == MonsterPoolSource.Managed) error = Dependency(value.Drops.PoolId, "DropPoolDependencyRequired");
                if (error != null) return MonsterResult<MonsterOverrideLease>.Fail(error);
                var copy = value.Copy(); var sound = BindSounds(SoundValidation.CommonEvents(copy));
                return Register("monster-override", backend => ((IMonsterOverrideBackend)backend).RegisterOverride(Id, owner.LoadOrder, copy), (native, release) => new MonsterOverrideLease(() => native.Snapshot, release), sound, new[] { MonsterValidation.ValidQualifiedId(copy.TargetId) ? copy.TargetId : "", copy.Drops?.ReferencesPool == true && copy.Drops.Source == MonsterPoolSource.Managed ? copy.Drops.PoolId : "" });
            }
            private IDisposable? BindSounds(IEnumerable<KeyValuePair<string, MonsterSound>> entries)
            {
                var rows = entries.ToArray(); if (rows.Length == 0) return null;
                var accepted = new List<IAudioSound>();
                var binding = new SoundContribution(host);
                foreach (var row in rows)
                {
                    var value = row.Value;
                    string use = SoundValidation.CommonUseKey(row.Key);
                    var route = use == "dead" ? AudioRoute.NativeMonsterDeath : use.StartsWith("explosion.", StringComparison.Ordinal) ? AudioRoute.NativeMonsterExplosion : AudioRoute.NativeMonster;
                    if (!(value.Sound is IAudioNativeSound sound) || !(value.Sound is IAudioSoundBinding) || sound.Owner != Id || sound.Route != route || sound.Loop && use != "humming")
                    { owner.Report("monsters.sounds", row.Key + ": require a same-Owner Audio handle with the native route and a finite effect (Humming may loop)."); value.Sound = null; value.RegisteredSound = () => null; continue; }
                    try { sound.Guard(); accepted.Add(sound); value.RegisteredSound = () => binding.Closed ? null : sound; }
                    catch (Exception ex) { owner.Report("monsters.sounds", row.Key + ": " + ex.GetBaseException().Message); value.Sound = null; value.RegisteredSound = () => null; }
                }
                if (accepted.Count == 0) return null;
                binding.Sounds = accepted.Distinct().ToArray();
                var scope = owner.CreateContribution("monster-sounds"); scope.UseProvider(MonsterModule.ProviderId); scope.UseProvider(AudioModule.ProviderId);
                var release = new ScopeRelease(scope);
                try
                {
                    scope.Activate(() => binding, value => { foreach (var sound in value.Sounds) value.Bindings.Add(((IAudioSoundBinding)sound).Bind(release)); });
                    return release;
                }
                catch (Exception ex) { scope.Close(); scope.TryClose(out _); owner.Report("monsters.sounds", ex.GetBaseException().Message); return null; }
            }
            public MonsterResult<MonsterContentLease> RegisterSpawnRule(MonsterSpawnRule rule)
            {
                var error = Check() ?? ContentValidation.Rule(Id, rule); if (error == null) error = Dependency(rule.DefinitionId, "DefinitionDependencyRequired");
                return error != null ? MonsterResult<MonsterContentLease>.Fail(error) : Register("monster-spawn-rule", b => b.RegisterSpawnRule(Id, rule), (native, release) => new MonsterContentLease(() => native.Snapshot, release), references: new[] { rule.DefinitionId });
            }
            public MonsterResult<MonsterContentLease> RegisterDropPool(MonsterDropPool pool)
            {
                var error = Check() ?? ContentValidation.Pool(Id, pool);
                return error != null ? MonsterResult<MonsterContentLease>.Fail(error) : Register("monster-drop-pool", b => b.RegisterDropPool(Id, pool), (native, release) => new MonsterContentLease(() => native.Snapshot, release));
            }
            public MonsterResult<MonsterLease> RegisterBehavior(string localId, Func<IMonsterBehavior> factory) => RegisterBehavior(localId, new MonsterBehaviorRequirements(), factory);
            public MonsterResult<MonsterLease> RegisterBehavior(string localId, MonsterBehaviorRequirements requirements, Func<IMonsterBehavior> factory)
            {
                var error = Check();
                if (error == null && (!MonsterValidation.ValidId(localId) || factory == null || requirements?.AttackIds == null || requirements.AttackIds.Any(string.IsNullOrWhiteSpace))) error = Error("InvalidBehavior", "Require a local ID, factory and valid attack requirements.");
                if (error != null) return MonsterResult<MonsterLease>.Fail(error);
                return Register("monster-behavior", b => ((IMonsterBehaviorMetadataBackend)b).RegisterBehavior(Id, localId, requirements!.Copy(), factory!, owner.Guard, (stage, detail) => owner.Report("behavior." + stage, detail)), (_, release) => new MonsterLease(release));
            }
            private MonsterResult<T> Query<T>(Func<IMonsterBackend, MonsterResult<T>> action) { var error = Check(true); return error == null ? action(host.backend!) : MonsterResult<T>.Fail(error); }
            public MonsterResult<IReadOnlyList<MonsterTemplate>> GetTemplates() => Query(b => b.State == MonsterModuleState.Active ? MonsterResult<IReadOnlyList<MonsterTemplate>>.Ok(b.Templates) : MonsterResult<IReadOnlyList<MonsterTemplate>>.Fail(Error("NativeNotReady", "Native catalog is preparing.")));
            public MonsterResult<MonsterContentCatalog> GetCatalog() => Query(b => b.State == MonsterModuleState.Active ? MonsterResult<MonsterContentCatalog>.Ok(b.Catalog) : MonsterResult<MonsterContentCatalog>.Fail(Error("NativeNotReady", "Native catalog is preparing.")));
            public MonsterResult<IReadOnlyList<MonsterOverrideSnapshot>> GetOverrides() => Query(b => MonsterResult<IReadOnlyList<MonsterOverrideSnapshot>>.Ok(((IMonsterOverrideBackend)b).GetOverrides()));
            public MonsterResult<IReadOnlyList<MonsterDefinitionCatalogEntry>> GetDefinitions() { var error = Check(); return error == null ? MonsterResult<IReadOnlyList<MonsterDefinitionCatalogEntry>>.Ok((host.backend as IMonsterCatalogBackend)?.Definitions() ?? Array.Empty<MonsterDefinitionCatalogEntry>()) : MonsterResult<IReadOnlyList<MonsterDefinitionCatalogEntry>>.Fail(error); }
            public MonsterResult<MonsterInstanceSnapshot> SpawnFromCatalog(MonsterSpawnRequest request) => Query(b => ((IMonsterCatalogBackend)b).SpawnFromCatalog(request));
            public MonsterResult<MonsterInstanceSnapshot> Spawn(MonsterSpawnRequest request) => Query(b => b.Spawn(Id, request));
            public MonsterResult<IReadOnlyList<MonsterInstanceSnapshot>> GetInstances() => Query(b => MonsterResult<IReadOnlyList<MonsterInstanceSnapshot>>.Ok(b.Instances(Id)));
            public MonsterResult<MonsterInstanceSnapshot> GetInstance(MonsterHandle handle) => Query(b => b.Instance(Id, handle));
            public MonsterResult<bool> Despawn(MonsterHandle handle) => Query(b => b.Despawn(Id, handle));
            public MonsterResult<bool> CancelSequence(MonsterHandle handle) => Query(b => b.CancelSequence(Id, handle));
        }
        private sealed class ScopeRelease : IDisposable
        {
            private readonly IRuntimeContribution scope;
            internal ScopeRelease(IRuntimeContribution scope) { this.scope = scope; }
            public void Dispose() { scope.Close(); if (!scope.TryClose(out string failure)) throw new InvalidOperationException(failure); }
        }
        private sealed class SoundContribution : IDisposable
        {
            private readonly MonsterService host;
            internal IAudioSound[] Sounds = Array.Empty<IAudioSound>();
            internal readonly List<IDisposable> Bindings = new List<IDisposable>();
            internal bool Closed;
            internal SoundContribution(MonsterService host) { this.host = host; }
            public void Dispose()
            {
                Closed = true; (host.backend as IMonsterAudioBackend)?.WithdrawSounds(Sounds);
                foreach (var binding in Bindings) binding.Dispose(); Bindings.Clear();
            }
        }
    }
}
