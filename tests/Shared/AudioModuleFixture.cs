using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

// Actual managed module and Core scopes, with a native engine supplied by each test.
internal sealed class AudioModuleFixture : IRuntimeModuleContext, IDisposable
{
    private readonly AudioService service = new AudioService();
    private readonly OwnerDependencyStore scopes = new OwnerDependencyStore();
    private readonly Func<IAudioBackend> backend;
    private readonly Func<IManifest, string> roots;
    private readonly Func<IManifest, int> order;
    private Func<IRuntimeModuleOwnerContext, IAudioContentApi> factory = null!;
    internal AudioModuleFixture(Func<IAudioBackend> backend, Func<IManifest, string> roots, Func<IManifest, int>? order = null)
    { this.backend = backend; this.roots = roots; this.order = order ?? (_ => 1); service.Register(this); }
    internal IAudioContentApi Api(IManifest owner, Action? guard = null) => factory(new Owner(this, owner, roots(owner), order(owner), guard ?? (() => { })));
    internal void CloseAdmission() => service.BeginClose();
    internal bool ReleaseOwner(string owner) => scopes.PrepareContributionsExit(owner, out _);
    internal void RetryPending() => scopes.RetryPendingContributions();
    public void Dispose()
    {
        if (!scopes.PrepareContributionsExit(ProviderId, out string error)) throw new InvalidOperationException(error);
        var result = service.TryClose(); if (!result.Released) throw new InvalidOperationException(result.Diagnostic);
    }
    public string ProviderId => AudioModule.ProviderId;
    public IMonitor Monitor { get; } = new SilentMonitor();
    public IDtmRuntimeContext Runtime { get; } = new Context();
    public IDtmScheduler Scheduler => throw new NotSupportedException("Fake native backend has no scheduler.");
    public string ComponentDirectory => Path.GetTempPath();
    public string CacheDirectory { get; } = Path.Combine(Path.GetTempPath(), "dtmapi-module-fixture-" + Guid.NewGuid().ToString("N"));
    internal string StreamingDirectory => Path.Combine(CacheDirectory, "v1", "wwise2023.1.16-v150-pcm16");
    public void AuthorizePatches(Action install) => install();
    public T ReadConfig<T>() where T : new() => new T();
    public void RegisterApi<T>(Func<IRuntimeModuleOwnerContext, T> create) where T : class
        => factory = owner => (IAudioContentApi)(object)create(owner);
    public T CreateBackend<T>(string component, params object[] arguments) where T : class => (T)(object)backend();
    public void PublishStatus(string status, string detail) { }
    private sealed class Owner : IRuntimeModuleOwnerContext
    {
        private readonly AudioModuleFixture host;
        private readonly Action guard;
        internal Owner(AudioModuleFixture host, IManifest manifest, string root, int order, Action guard)
        { this.host = host; Manifest = manifest; LoadOrder = OfficialOrder = order; this.guard = guard; Resources = new ModResourceService(root, Guard); Files = new OwnerFileService(root, Guard); }
        public IManifest Manifest { get; }
        public int LoadOrder { get; }
        public int OfficialOrder { get; }
        public IModResourceHelper Resources { get; }
        public IOwnerFileHelper Files { get; }
        public void Guard() => guard();
        public PackageResourceReference Reference(string key) => ((ModResourceService)Resources).BackgroundReference(key, Manifest.UniqueID);
        public T? GetApi<T>(string provider) where T : class => throw new NotSupportedException();
        public bool HasRequiredDependency(string provider) => false;
        public void Report(string section, string message) { }
        public IRuntimeContribution CreateContribution(string id) => host.scopes.CreateContribution(Manifest.UniqueID, id, Guard);
    }
    private sealed class SilentMonitor : IMonitor
    {
        public void Log(string message, LogLevel level = LogLevel.Info) { }
        public void LogOnce(string key, string message, LogLevel level = LogLevel.Info) { }
        public void LogException(Exception exception, string message) { }
    }
    private sealed class Context : IDtmRuntimeContext
    {
        private readonly int thread = Environment.CurrentManagedThreadId;
        public bool IsMainThread => thread == Environment.CurrentManagedThreadId;
        public DtmRuntimeContextSnapshot Snapshot => new DtmRuntimeContextSnapshot(Guid.Empty, 1, DtmRuntimePhase.WorldReady, 1, 1);
        public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed) => throw new NotSupportedException();
    }
}
