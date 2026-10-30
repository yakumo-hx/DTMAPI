using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;

namespace DTMAPI.Core.Runtime
{
    public sealed partial class DtmApiRuntime
    {
        internal bool AppendBgmGroups { get; set; } = true;
        private readonly List<string> runtimeModuleIds = new List<string>();
        private readonly RuntimeModuleHost runtimeModules = new RuntimeModuleHost();
        private RuntimeComponentFiles? runtimeComponents;
        private IRuntimeModuleCatalog? moduleCatalog;
        private readonly Dictionary<string, ModuleContentSource> moduleContentSources = new Dictionary<string, ModuleContentSource>();
        internal void RegisterRuntimeModule(string provider)
        {
            if (runtimeModuleIds.Contains(provider, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Duplicate Runtime module: " + provider);
            runtimeModuleIds.Add(provider); ModRegistry.ReserveRuntimeProvider(provider);
        }
        private void InitializeRuntimeModules()
        {
            if (runtimeModuleIds.Count == 0) return;
            runtimeComponents = new RuntimeComponentFiles(Paths.GamePath);
            EnsureAdvancedHarmonyCleanupParticipantRegistered();
            RegisterModOwnerCleanupParticipant(runtimeModules);
            try { moduleCatalog = (IRuntimeModuleCatalog)Activator.CreateInstance(runtimeComponents.Load("DTMAPI.RuntimeModules").GetType("DTMAPI.RuntimeModules.Entry", true)!)!; }
            catch (Exception ex) { Diagnostics.RecordError("DTMAPI", "Runtime modules unavailable.", ex.GetBaseException().Message); return; }
            foreach (string provider in runtimeModuleIds)
            {
                try
                {
                    modOwnerLifecycle.BeginEntry(provider);
                    advancedHarmonySupervisor.RegisterRuntimeModule(provider);
                    var module = moduleCatalog.Create(provider);
                    runtimeModules.Register(module, new ModuleContext(this, provider));
                    modOwnerLifecycle.Activate(provider);
                }
                catch (Exception ex)
                {
                    Diagnostics.RecordError(provider, "Runtime module unavailable.", ex.GetBaseException().Message);
                    DeactivateOwner(provider, ModOwnerCleanupReason.EntryFailed, true, "");
                }
            }
        }
        private sealed class ModuleContext : IRuntimeModuleContext
        {
            private readonly DtmApiRuntime runtime;
            private readonly List<Assembly> nativeAssemblies = new List<Assembly>();
            internal ModuleContext(DtmApiRuntime runtime, string provider)
            {
                this.runtime = runtime; ProviderId = provider;
                Monitor = new FileMonitor(runtime.host, provider, runtime.Diagnostics.LatestLogPath, runtime.Diagnostics.RecordError);
                Runtime = (IDtmRuntimeContext)runtime.platformServices.Resolve(provider, typeof(IDtmRuntimeContext))!;
                Scheduler = (IDtmScheduler)runtime.platformServices.Resolve(provider, typeof(IDtmScheduler))!;
            }
            public string ProviderId { get; }
            public IMonitor Monitor { get; }
            public IDtmRuntimeContext Runtime { get; }
            public IDtmScheduler Scheduler { get; }
            public string ComponentDirectory => runtime.runtimeComponents!.DirectoryFor("DTMAPI.Modules." + ProviderId.Substring("DTMAPI.".Length));
            public string CacheDirectory => Path.Combine(runtime.Paths.DtmApiPath, "cache", ProviderId);
            public T ReadConfig<T>() where T : new() => runtime.Config.ReadConfig<T>(ProviderId);
            public void RegisterApi<T>(Func<IRuntimeModuleOwnerContext, T> create) where T : class
                => runtime.ModRegistry.RegisterRuntimeApiFactory<T>(ProviderId, ApiVersion, (owner, guard) => create(new ModuleOwnerContext(runtime, owner, guard)));
            public T CreateBackend<T>(string component, params object[] arguments) where T : class
            {
                runtime.EnsureModOwnerRegistrationAllowed(ProviderId);
                var assembly = runtime.runtimeComponents!.Load(component);
                nativeAssemblies.Add(assembly);
                return (T)runtime.advancedHarmonySupervisor.ActivateInternalModule(ProviderId, assembly,
                    () => Activator.CreateInstance(assembly.GetType(component + ".NativeModule", true)!, arguments)!);
            }
            public void AuthorizePatches(Action install)
            {
                runtime.EnsureModOwnerRegistrationAllowed(ProviderId);
                var assembly = nativeAssemblies.LastOrDefault() ?? throw new InvalidOperationException("No native module was constructed.");
                runtime.advancedHarmonySupervisor.ActivateInternalModule(ProviderId, assembly, () => { install(); return new object(); });
            }
            public void PublishStatus(string status, string detail) => Monitor.Log("Module " + status + "; " + detail);
        }
        private sealed class ModuleOwnerContext : IRuntimeModuleOwnerContext
        {
            private readonly DtmApiRuntime runtime;
            private readonly Action guard;
            private readonly DiscoveredMod source;
            internal ModuleOwnerContext(DtmApiRuntime runtime, IManifest manifest, Action guard)
            {
                this.runtime = runtime; this.guard = guard; Manifest = manifest;
                source = runtime.loadedModsSnapshot.Concat(runtime.discoveredMods).FirstOrDefault(m => string.Equals(m.Manifest.UniqueID, manifest.UniqueID, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Module requires a real package Owner: " + manifest.UniqueID);
                Resources = new ModResourceService(source.RootPath, Guard);
                Files = new OwnerFileService(source.RootPath, Guard);
            }
            public IManifest Manifest { get; }
            public int LoadOrder => runtime.ResolveOwnerLoadIndex(source.Manifest) ?? throw new InvalidOperationException("Owner load order unavailable.");
            public int OfficialOrder => runtime.ResolveOfficialOwnerOrder(source.Manifest) ?? throw new InvalidOperationException("Official Owner order unavailable.");
            public IModResourceHelper Resources { get; }
            public IOwnerFileHelper Files { get; }
            public void Guard()
            {
                runtime.EnsureModHelperRuntimeThread(Manifest.UniqueID); guard();
                runtime.EnsureModOwnerRegistrationAllowed(Manifest.UniqueID);
                if (!runtime.discoveredById.TryGetValue(Manifest.UniqueID, out var current) || !current.OfficialEnabled || !HasSameSourceIdentity(source, current))
                    throw new ObjectDisposedException("Package source", Manifest.UniqueID);
            }
            public PackageResourceReference Reference(string key) => ((ModResourceService)Resources).BackgroundReference(key,
                string.Join("\n", Manifest.UniqueID, source.Source, source.OfficialId, source.RootPath, source.Manifest.Version, source.Classification.SourceFingerprint));
            public T? GetApi<T>(string provider) where T : class => runtime.ModRegistry.GetContributionApi<T>(Manifest, provider, Guard);
            public bool HasRequiredDependency(string provider) => runtime.ModuleDependencyAllowed(source, provider);
            public void Report(string section, string message) => runtime.Diagnostics.RecordError(Manifest.UniqueID, "Content contribution: " + section, message);
            public IRuntimeContribution CreateContribution(string id) { Guard(); return runtime.ownerDependencies.CreateContribution(Manifest.UniqueID, id, Guard); }
        }
        private bool ModuleDependencyAllowed(DiscoveredMod source, string provider)
        {
            if (!TryGetAvailableDependencyVersion(provider, null, out string version)) return false;
            if (source.Manifest.DependencyContract != null)
                return source.Manifest.DependencyContract.Dependencies.Any(d => d.Required && string.Equals(d.Id, provider, StringComparison.OrdinalIgnoreCase) && PackageRangeSatisfied(d, version));
            return ((IManifest)source.Manifest).Dependencies.Any(d => d.Required && string.Equals(d.UniqueID, provider, StringComparison.OrdinalIgnoreCase) && IsVersionRequirementSatisfied(d.MinimumVersion, version, out _));
        }
        private void CoordinateRuntimeModuleContent()
        {
            if (moduleCatalog == null) return;
            const string contentPath = "PackData/content.json";
            foreach (string retired in moduleContentSources.Where(p => p.Value.Scope.Released).Select(p => p.Key).ToArray()) moduleContentSources.Remove(retired);
            foreach (var mod in loadedModsSnapshot)
            {
                string key = mod.Manifest.UniqueID + "/" + contentPath;
                string path = OwnerFilePath.Resolve(mod.RootPath, contentPath);
                var file = new FileInfo(path);
                moduleContentSources.TryGetValue(key, out var previous);
                if (!file.Exists)
                {
                    if (previous != null && previous.Scope.TryClose(out _)) moduleContentSources.Remove(key);
                    continue;
                }
                string signature = file.Length + ":" + file.LastWriteTimeUtc.Ticks;
                if (previous != null)
                {
                    if (previous.Signature == signature) continue;
                    try { previous.Source.Refresh(); previous.Signature = signature; }
                    catch (Exception ex) { Diagnostics.RecordError(mod.Manifest.UniqueID, "Runtime content candidate rejected; previous contributions retained.", ex.GetBaseException().Message); }
                    continue;
                }
                var scope = ownerDependencies.CreateContribution(mod.Manifest.UniqueID, contentPath, () =>
                {
                    EnsureModOwnerRegistrationAllowed(mod.Manifest.UniqueID);
                });
                try
                {
                    var owner = new ModuleOwnerContext(this, mod.Manifest, scope.Guard);
                    var source = scope.Activate(() => moduleCatalog.Prepare(owner), activation => activation.Activate());
                    moduleContentSources[key] = new ModuleContentSource { Signature = signature, Scope = scope, Source = source };
                }
                catch (Exception ex)
                {
                    scope.Close(); scope.TryClose(out _);
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "Runtime content contribution failed.", ex.GetBaseException().Message);
                }
            }
        }
        private sealed class ModuleContentSource { internal string Signature = ""; internal ContributionScope Scope = null!; internal IRuntimeContentSource Source = null!; }
    }
}
