using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Services
{
    internal sealed class ModRegistryService : IModRegistry
    {
        private readonly Dictionary<string, IManifest> loaded = new Dictionary<string, IManifest>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> apis = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public void AddLoaded(IManifest manifest) => loaded[manifest.UniqueID] = manifest;
        public bool IsLoaded(string uniqueId) => loaded.ContainsKey(uniqueId);
        public IManifest? Get(string uniqueId) => loaded.TryGetValue(uniqueId, out IManifest manifest) ? manifest : null;
        public IReadOnlyList<IManifest> GetAll() => loaded.Values.OrderBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();

        public TApi? GetApi<TApi>(string uniqueId) where TApi : class
        {
            return apis.TryGetValue(uniqueId + "|" + typeof(TApi).FullName, out object api) ? api as TApi : null;
        }

        public void RegisterApi<TApi>(TApi api) where TApi : class
        {
            throw new InvalidOperationException("API registration must use an owner-bound mod helper registry.");
        }

        public IModRegistry CreateOwnerBoundRegistry(IManifest owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            return new OwnerBoundModRegistry(this, owner);
        }

        internal void RegisterApiForOwner<TApi>(IManifest owner, TApi api) where TApi : class
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (api == null)
                throw new ArgumentNullException(nameof(api));
            apis[owner.UniqueID + "|" + typeof(TApi).FullName] = api;
        }

        private sealed class OwnerBoundModRegistry : IModRegistry
        {
            private readonly ModRegistryService inner;
            private readonly IManifest owner;

            public OwnerBoundModRegistry(ModRegistryService inner, IManifest owner)
            {
                this.inner = inner;
                this.owner = owner;
            }

            public bool IsLoaded(string uniqueId) => inner.IsLoaded(uniqueId);
            public IManifest? Get(string uniqueId) => inner.Get(uniqueId);
            public IReadOnlyList<IManifest> GetAll() => inner.GetAll();
            public TApi? GetApi<TApi>(string uniqueId) where TApi : class => inner.GetApi<TApi>(uniqueId);
            public void RegisterApi<TApi>(TApi api) where TApi : class => inner.RegisterApiForOwner(owner, api);
        }
    }

    internal sealed class DtmHelper : IDtmHelper
    {
        public DtmHelper(
            IManifest manifest,
            IMonitor monitor,
            IEventsHelper events,
            IConfigHelper config,
            IModRegistry modRegistry,
            IWorkshopHelper workshop,
            IUiHelper ui,
            IDiagnosticsHelper diagnostics,
            IContentQueryHelper content,
            IInputHelper input,
            ITranslationHelper translation)
        {
            ModManifest = manifest;
            Monitor = monitor;
            Events = events;
            Config = config;
            ModRegistry = modRegistry;
            Workshop = workshop;
            UI = ui;
            Diagnostics = diagnostics;
            Content = content;
            Input = input;
            Translation = translation;
        }

        public IManifest ModManifest { get; }
        public IMonitor Monitor { get; }
        public IEventsHelper Events { get; }
        public IConfigHelper Config { get; }
        public IModRegistry ModRegistry { get; }
        public IWorkshopHelper Workshop { get; }
        public IUiHelper UI { get; }
        public IDiagnosticsHelper Diagnostics { get; }
        public IContentQueryHelper Content { get; }
        public IInputHelper Input { get; }
        public ITranslationHelper Translation { get; }
        public TConfig ReadConfig<TConfig>() where TConfig : new() => Config.ReadConfig<TConfig>(ModManifest);
        public void WriteConfig<TConfig>(TConfig config) => Config.WriteConfig(ModManifest, config);
    }

    public sealed class WorkshopModInfo : IWorkshopModInfo
    {
        public WorkshopModInfo(DiscoveredMod mod)
        {
            UniqueID = mod.Manifest.UniqueID;
            Name = mod.Manifest.Name;
            Source = mod.Source;
            RootPath = mod.RootPath;
            IsEnabledByOfficialPath = mod.OfficialEnabled;
            CanDTMApiToggle = mod.CanDtmApiToggle;
            WorkshopId = mod.WorkshopId;
        }

        public string UniqueID { get; }
        public string Name { get; }
        public string Source { get; }
        public string RootPath { get; }
        public bool IsEnabledByOfficialPath { get; }
        public bool CanDTMApiToggle { get; }
        public ulong? WorkshopId { get; }
    }
}
