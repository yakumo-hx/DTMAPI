using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.Core.Services;
using DTMAPI.Core.Runtime;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;

internal sealed class MonsterModuleFixture : IRuntimeModuleContext, IDisposable
{
    private readonly MonsterService service = new MonsterService();
    private readonly OwnerDependencyStore scopes = new OwnerDependencyStore();
    private readonly Func<IMonsterBackend> backend;
    private readonly Func<IManifest, int> order;
    private Func<IRuntimeModuleOwnerContext, IMonsterContentApi> factory = null!;
    internal Func<IManifest, string, bool> Required = (_, _) => false;
    internal Func<IManifest, string> Root = _ => Path.GetTempPath();
    internal readonly List<string> Reports = new List<string>();
    internal MonsterModuleFixture(Func<IMonsterBackend> backend, Func<bool>? main = null, Func<IManifest, int>? order = null)
    { this.backend = backend; this.order = order ?? (_ => 0); Runtime = new Context(main ?? (() => true)); service.Register(this); }
    internal IMonsterContentApi Api(IManifest manifest, Action? guard = null) => factory(Owner(manifest, guard));
    internal IRuntimeModuleOwnerContext Owner(IManifest manifest, Action? guard = null) => new OwnerContext(this, manifest, guard ?? (() => { }));
    internal bool ReleaseOwner(string owner) => scopes.PrepareContributionsExit(owner, out _);
    internal void RetryPending() => scopes.RetryPendingContributions();
    internal void BeginClose() => service.BeginClose();
    public void Dispose()
    {
        if (!scopes.PrepareContributionsExit(ProviderId, out string failure)) throw new InvalidOperationException(failure);
        var result = service.TryClose(); if (!result.Released) throw new InvalidOperationException(result.Diagnostic);
    }
    public string ProviderId => MonsterModule.ProviderId;
    public IDtmRuntimeContext Runtime { get; }
    public IMonitor Monitor { get; } = new SilentMonitor();
    public IDtmScheduler Scheduler => throw new NotSupportedException();
    public string ComponentDirectory => Path.GetTempPath();
    public string CacheDirectory => Path.GetTempPath();
    public void AuthorizePatches(Action install) => install();
    public T ReadConfig<T>() where T : new() => new T();
    public void RegisterApi<T>(Func<IRuntimeModuleOwnerContext, T> create) where T : class
    { if (typeof(T) == typeof(IMonsterContentApi)) factory = owner => (IMonsterContentApi)(object)create(owner); }
    public T CreateBackend<T>(string component, params object[] arguments) where T : class => (T)(object)backend();
    public void PublishStatus(string status, string detail) { }
    private sealed class OwnerContext : IRuntimeModuleOwnerContext
    {
        private readonly MonsterModuleFixture host; private readonly Action guard;
        internal OwnerContext(MonsterModuleFixture host, IManifest manifest, Action guard)
        { this.host = host; Manifest = manifest; this.guard = guard; Resources = new ModResourceService(host.Root(manifest), Guard); Files = new OwnerFileService(host.Root(manifest), Guard); }
        public IManifest Manifest { get; }
        public int LoadOrder => host.order(Manifest);
        public int OfficialOrder => LoadOrder;
        public IModResourceHelper Resources { get; }
        public IOwnerFileHelper Files { get; }
        public PackageResourceReference Reference(string key) => ((ModResourceService)Resources).BackgroundReference(key, Manifest.UniqueID);
        public void Guard() => guard();
        public void Report(string section, string message) => host.Reports.Add(section + ": " + message);
        public T? GetApi<T>(string provider) where T : class => typeof(T) == typeof(IMonsterContentApi) && provider == MonsterModule.ProviderId ? (T)(object)host.factory(this) : null;
        public bool HasRequiredDependency(string provider) => host.Required(Manifest, provider);
        public IRuntimeContribution CreateContribution(string id) => host.scopes.CreateContribution(Manifest.UniqueID, id, Guard);
    }
    private sealed class Context : IDtmRuntimeContext
    {
        private readonly Func<bool> main;
        internal Context(Func<bool> main) { this.main = main; }
        public bool IsMainThread => main();
        public DtmRuntimeContextSnapshot Snapshot => new DtmRuntimeContextSnapshot(Guid.Empty, 1, DtmRuntimePhase.WorldReady, 1, 1);
        public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed) => throw new NotSupportedException();
    }
    private sealed class SilentMonitor : IMonitor
    {
        public void Log(string message, LogLevel level = LogLevel.Info) { }
        public void LogOnce(string key, string message, LogLevel level = LogLevel.Info) { }
        public void LogException(Exception exception, string message) { }
    }
}
