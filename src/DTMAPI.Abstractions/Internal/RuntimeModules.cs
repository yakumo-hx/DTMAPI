using System;

namespace DTMAPI.Abstractions.Internal
{
    // Runtime implementation protocol, not an author module SDK.
    internal interface IRuntimeModule
    {
        void Register(IRuntimeModuleContext context);
        void BeginClose();
        RuntimeModuleCleanup TryClose();
    }
    internal interface IRuntimeModuleCatalog
    {
        IRuntimeModule Create(string provider);
        IRuntimeContentSource Prepare(IRuntimeModuleOwnerContext owner);
    }
    internal interface IRuntimeContentSource : IContentPackActivation { void Refresh(); }
    internal readonly struct RuntimeModuleCleanup
    {
        internal RuntimeModuleCleanup(int remaining, string diagnostic = "", bool restartRequired = false)
        { Remaining = remaining; Diagnostic = diagnostic; RestartRequired = restartRequired; }
        internal int Remaining { get; }
        internal string Diagnostic { get; }
        internal bool RestartRequired { get; }
        internal bool Released => Remaining == 0 && !RestartRequired;
    }
    internal interface IRuntimeModuleContext
    {
        string ProviderId { get; }
        IMonitor Monitor { get; }
        IDtmRuntimeContext Runtime { get; }
        IDtmScheduler Scheduler { get; }
        string ComponentDirectory { get; }
        string CacheDirectory { get; }
        void AuthorizePatches(Action install);
        T ReadConfig<T>() where T : new();
        void RegisterApi<T>(Func<IRuntimeModuleOwnerContext, T> create) where T : class;
        T CreateBackend<T>(string component, params object[] arguments) where T : class;
        void PublishStatus(string status, string detail);
    }
    internal interface IRuntimeModuleOwnerContext
    {
        IManifest Manifest { get; }
        int LoadOrder { get; }
        int OfficialOrder { get; }
        IModResourceHelper Resources { get; }
        IOwnerFileHelper Files { get; }
        PackageResourceReference Reference(string key);
        void Report(string section, string message);
        void Guard();
        T? GetApi<T>(string provider) where T : class;
        bool HasRequiredDependency(string provider);
        IRuntimeContribution CreateContribution(string id);
    }
    internal interface IRuntimeContribution
    {
        string Owner { get; }
        object Generation { get; }
        void Guard();
        T Own<T>(T value) where T : IDisposable;
        T Activate<T>(Func<T> prepare, Action<T> activate) where T : IDisposable;
        void UseProvider(string provider);
        void Close();
        bool TryClose(out string failure);
    }
}
