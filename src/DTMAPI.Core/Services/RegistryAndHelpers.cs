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
        private readonly Dictionary<ApiRegistrationKey, object> apis = new Dictionary<ApiRegistrationKey, object>(ApiRegistrationKeyComparer.Instance);
        private readonly Dictionary<ApiRegistrationKey, IOwnerBoundApiFactory> apiFactories = new Dictionary<ApiRegistrationKey, IOwnerBoundApiFactory>(ApiRegistrationKeyComparer.Instance);
        private readonly Dictionary<OwnerBoundFacadeKey, object> ownerBoundFacades = new Dictionary<OwnerBoundFacadeKey, object>(OwnerBoundFacadeKeyComparer.Instance);
        private readonly Action<string, string, string, string>? recordOwnerRegistration;
        private readonly Action<string, string, int, string>? recordOwnerCleanup;
        private readonly Action<string, string, string>? reportDeprecatedApi;
        private readonly Dictionary<string, HashSet<Type>> deprecatedApiWarnings = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);

        public ModRegistryService(
            Action<string, string, string, string>? recordOwnerRegistration = null,
            Action<string, string, int, string>? recordOwnerCleanup = null,
            Action<string, string, string>? reportDeprecatedApi = null)
        {
            this.recordOwnerRegistration = recordOwnerRegistration;
            this.recordOwnerCleanup = recordOwnerCleanup;
            this.reportDeprecatedApi = reportDeprecatedApi;
        }

        public void AddLoaded(IManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(manifest.UniqueID))
                throw new ArgumentException("Manifest UniqueID is required.", nameof(manifest));
            if (loaded.TryGetValue(manifest.UniqueID, out IManifest existing))
            {
                if (!HasSameCanonicalManifest(existing, manifest))
                    throw new InvalidOperationException("Owner '" + manifest.UniqueID + "' already has a different canonical loaded manifest.");
                return;
            }
            loaded.Add(manifest.UniqueID, manifest);
        }
        public bool IsLoaded(string uniqueId) => loaded.ContainsKey(uniqueId);
        public IManifest? Get(string uniqueId) => loaded.TryGetValue(uniqueId, out IManifest manifest) ? manifest : null;
        public IReadOnlyList<IManifest> GetAll() => loaded.Values.OrderBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();

        public TApi? GetApi<TApi>(string uniqueId) where TApi : class
        {
            return apis.TryGetValue(new ApiRegistrationKey(uniqueId, typeof(TApi)), out object api) ? api as TApi : null;
        }

        public void RegisterApi<TApi>(TApi api) where TApi : class
        {
            throw new InvalidOperationException("API registration must use an owner-bound mod helper registry.");
        }

        public IModRegistry CreateOwnerBoundRegistry(IManifest owner, Action ensureOwnerActive)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ensureOwnerActive == null)
                throw new ArgumentNullException(nameof(ensureOwnerActive));
            return new OwnerBoundModRegistry(this, owner, ensureOwnerActive);
        }

        internal void RegisterApiForOwner<TApi>(IManifest owner, TApi api, IOwnerBoundApiFactory? ownerBoundFactory = null) where TApi : class
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (api == null)
                throw new ArgumentNullException(nameof(api));
            var key = new ApiRegistrationKey(owner.UniqueID, typeof(TApi));
            if (apis.ContainsKey(key))
                throw new InvalidOperationException("Owner '" + owner.UniqueID + "' already registered API contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
            IOwnerBoundApiFactory? factory = ownerBoundFactory ?? api as IOwnerBoundApiFactory;
            if (apiFactories.ContainsKey(key))
                throw new InvalidOperationException("Owner '" + owner.UniqueID + "' already has an API factory for contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
            try
            {
                apis.Add(key, api);
                if (factory != null)
                    apiFactories.Add(key, factory);
            }
            catch
            {
                apis.Remove(key);
                apiFactories.Remove(key);
                throw;
            }
            try
            {
                recordOwnerRegistration?.Invoke(owner.UniqueID, "Api", typeof(TApi).FullName ?? typeof(TApi).Name, "Owner-bound API registration.");
            }
            catch
            {
                // Diagnostics are observational and must not turn a completed API
                // registration into a partial caller-visible failure.
            }
        }

        internal void RegisterProcessLifetimeApiForOwner<TApi>(IManifest owner, TApi api, IOwnerBoundApiFactory? ownerBoundFactory = null) where TApi : class
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (api == null)
                throw new ArgumentNullException(nameof(api));

            bool addedLoadedManifest = false;
            if (loaded.TryGetValue(owner.UniqueID, out IManifest canonical))
            {
                if (!HasSameCanonicalManifest(canonical, owner))
                    throw new InvalidOperationException("Process-lifetime owner '" + owner.UniqueID + "' already has a different canonical loaded manifest.");
            }
            else
            {
                AddLoaded(owner);
                addedLoadedManifest = true;
            }

            try
            {
                RegisterApiForOwner(owner, api, ownerBoundFactory);
            }
            catch
            {
                if (addedLoadedManifest)
                    loaded.Remove(owner.UniqueID);
                throw;
            }
        }

        internal int RemoveOwner(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                return 0;

            int removed = loaded.Remove(uniqueId) ? 1 : 0;
            deprecatedApiWarnings.Remove(uniqueId);
            foreach (ApiRegistrationKey key in apis.Keys.Where(k => OwnerEquals(k.OwnerId, uniqueId)).ToArray())
            {
                if (apis.Remove(key))
                    removed++;
                apiFactories.Remove(key);
            }

            foreach (OwnerBoundFacadeKey key in ownerBoundFacades.Keys.Where(k =>
                OwnerEquals(k.ConsumerId, uniqueId) || OwnerEquals(k.ProviderId, uniqueId)).ToArray())
            {
                if (ownerBoundFacades.TryGetValue(key, out object facade) && ownerBoundFacades.Remove(key))
                {
                    TryDeactivateFacade(facade);
                    removed++;
                }
            }

            if (removed > 0)
                recordOwnerCleanup?.Invoke(uniqueId, "ApiOrLoadedMod", removed, "Owner cleanup removed loaded registry/API entries.");
            return removed;
        }

        private sealed class OwnerBoundModRegistry : IModRegistry
        {
            private readonly ModRegistryService inner;
            private readonly IManifest owner;
            private readonly Action ensureOwnerActive;

            public OwnerBoundModRegistry(ModRegistryService inner, IManifest owner, Action ensureOwnerActive)
            {
                this.inner = inner;
                this.owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public bool IsLoaded(string uniqueId) { ensureOwnerActive(); return inner.IsLoaded(uniqueId); }
            public IManifest? Get(string uniqueId) { ensureOwnerActive(); return inner.Get(uniqueId); }
            public IReadOnlyList<IManifest> GetAll() { ensureOwnerActive(); return inner.GetAll(); }
            public TApi? GetApi<TApi>(string uniqueId) where TApi : class => inner.GetApiForOwner<TApi>(owner, uniqueId, ensureOwnerActive);
            public void RegisterApi<TApi>(TApi api) where TApi : class
            {
                ensureOwnerActive();
                inner.RegisterApiForOwner(owner, api);
            }
        }


        private TApi? GetApiForOwner<TApi>(IManifest consumer, string providerId, Action ensureOwnerActive) where TApi : class
        {
            ensureOwnerActive();
            ReportDeprecatedApi<TApi>(consumer.UniqueID);
            var providerKey = new ApiRegistrationKey(providerId, typeof(TApi));
            if (!apis.TryGetValue(providerKey, out object api))
                return null;
            if (!apiFactories.TryGetValue(providerKey, out IOwnerBoundApiFactory factory))
                return api as TApi;

            var facadeKey = new OwnerBoundFacadeKey(consumer.UniqueID, providerId, typeof(TApi));
            if (!ownerBoundFacades.TryGetValue(facadeKey, out object facade))
            {
                Action ensureFacadeActive = () =>
                {
                    ensureOwnerActive();
                    if (!apis.ContainsKey(providerKey))
                        throw new InvalidOperationException("API provider '" + providerId + "' is inactive.");
                };
                facade = factory.CreateOwnerBoundApi(typeof(TApi), consumer, ensureFacadeActive);
                if (!(facade is TApi))
                    throw new InvalidOperationException("Owner-bound API factory for provider '" + providerId + "' returned an incompatible facade for contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
                ownerBoundFacades.Add(facadeKey, facade);
            }
            return facade as TApi;
        }

        private void ReportDeprecatedApi<TApi>(string owner)
        {
            string message = DeprecatedContract<TApi>.Message;
            if (message.Length == 0 || reportDeprecatedApi == null)
                return;
            if (!deprecatedApiWarnings.TryGetValue(owner, out HashSet<Type> warned))
                deprecatedApiWarnings.Add(owner, warned = new HashSet<Type>());
            if (!warned.Add(typeof(TApi)))
                return;
            try { reportDeprecatedApi(owner, typeof(TApi).FullName ?? typeof(TApi).Name, message); }
            catch { /* A diagnostic sink cannot change the existing API result. */ }
        }

        private static class DeprecatedContract<TApi>
        {
            internal static readonly string Message = ReadMessage();
            private static string ReadMessage()
            {
                Type type = typeof(TApi);
                if (!ReferenceEquals(type.Assembly, typeof(IDtmHelper).Assembly)) return string.Empty;
                var obsolete = (ObsoleteAttribute?)Attribute.GetCustomAttribute(type, typeof(ObsoleteAttribute), false);
                return obsolete?.Message ?? string.Empty;
            }
        }

        internal int CountOwner(string uniqueId)
        {
            return (loaded.ContainsKey(uniqueId) ? 1 : 0) +
                apis.Keys.Count(k => OwnerEquals(k.OwnerId, uniqueId)) +
                ownerBoundFacades.Keys.Count(k => OwnerEquals(k.ConsumerId, uniqueId) || OwnerEquals(k.ProviderId, uniqueId));
        }

        internal int TotalRootCount => loaded.Count + apis.Count + ownerBoundFacades.Count;

        private static bool OwnerEquals(string left, string right) => StringComparer.OrdinalIgnoreCase.Equals(left, right);

        private static bool HasSameCanonicalManifest(IManifest left, IManifest right)
        {
            return OwnerEquals(left.UniqueID, right.UniqueID) &&
                string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
                string.Equals(left.Author, right.Author, StringComparison.Ordinal) &&
                string.Equals(left.Version, right.Version, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.Type, right.Type, StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDeactivateFacade(object facade)
        {
            try
            {
                (facade as IOwnerBoundApiFacade)?.Deactivate();
            }
            catch
            {
                // Registry cleanup must still sever the cache root. Provider-specific owner cleanup
                // remains authoritative for native/platform resources.
            }
        }

        private readonly struct ApiRegistrationKey
        {
            public ApiRegistrationKey(string ownerId, Type contract)
            {
                OwnerId = ownerId ?? string.Empty;
                Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            }

            public string OwnerId { get; }
            public Type Contract { get; }
        }

        private sealed class ApiRegistrationKeyComparer : IEqualityComparer<ApiRegistrationKey>
        {
            public static readonly ApiRegistrationKeyComparer Instance = new ApiRegistrationKeyComparer();

            public bool Equals(ApiRegistrationKey x, ApiRegistrationKey y) =>
                OwnerEquals(x.OwnerId, y.OwnerId) && x.Contract == y.Contract;

            public int GetHashCode(ApiRegistrationKey obj)
            {
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.OwnerId) * 397) ^ obj.Contract.GetHashCode();
                }
            }
        }

        private readonly struct OwnerBoundFacadeKey
        {
            public OwnerBoundFacadeKey(string consumerId, string providerId, Type contract)
            {
                ConsumerId = consumerId ?? string.Empty;
                ProviderId = providerId ?? string.Empty;
                Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            }

            public string ConsumerId { get; }
            public string ProviderId { get; }
            public Type Contract { get; }
        }

        private sealed class OwnerBoundFacadeKeyComparer : IEqualityComparer<OwnerBoundFacadeKey>
        {
            public static readonly OwnerBoundFacadeKeyComparer Instance = new OwnerBoundFacadeKeyComparer();

            public bool Equals(OwnerBoundFacadeKey x, OwnerBoundFacadeKey y) =>
                OwnerEquals(x.ConsumerId, y.ConsumerId) &&
                OwnerEquals(x.ProviderId, y.ProviderId) &&
                x.Contract == y.Contract;

            public int GetHashCode(OwnerBoundFacadeKey obj)
            {
                unchecked
                {
                    int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ConsumerId);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProviderId);
                    return (hash * 397) ^ obj.Contract.GetHashCode();
                }
            }
        }
    }

    internal sealed class DtmHelper : IDtmHelper, IDtmHelperServices
    {
        private readonly OwnerServiceScope? services;
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
            ITranslationHelper translation,
            OwnerServiceScope? services = null)
        {
            this.services = services;
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
        public TService? GetService<TService>() where TService : class => services?.GetService<TService>();
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
