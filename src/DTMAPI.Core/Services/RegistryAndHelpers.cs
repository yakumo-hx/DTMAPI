using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Services
{
    internal sealed class ModRegistryService : IModRegistry
    {
        private readonly Dictionary<string, ProviderRegistration> providers = new Dictionary<string, ProviderRegistration>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ApiRegistrationKey, ApiRegistration> apis = new Dictionary<ApiRegistrationKey, ApiRegistration>(ApiRegistrationKeyComparer.Instance);
        private readonly Dictionary<OwnerBoundFacadeKey, object> ownerBoundFacades = new Dictionary<OwnerBoundFacadeKey, object>(OwnerBoundFacadeKeyComparer.Instance);

        public void AddLoaded(IManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.UniqueID))
                throw new ArgumentException("Manifest UniqueID is required.", nameof(manifest));
            if (providers.TryGetValue(manifest.UniqueID, out var existing))
            {
                if (existing.Origin != ProviderOrigin.Package || existing.Metadata == null || !HasSameCanonicalManifest(existing.Metadata, manifest))
                    throw new InvalidOperationException("Owner '" + manifest.UniqueID + "' already has a different canonical loaded manifest.");
                return;
            }
            providers.Add(manifest.UniqueID, new ProviderRegistration(manifest.UniqueID, ProviderOrigin.Package, manifest));
        }
        internal bool IsRuntimeProvider(string id) => providers.TryGetValue(id, out var provider) && provider.Origin == ProviderOrigin.Runtime;
        internal IEnumerable<KeyValuePair<string, string>> RuntimeProviders => providers.Values.Where(p => p.Origin == ProviderOrigin.Runtime && p.Available).Select(p => new KeyValuePair<string, string>(p.Id, p.Version));
        internal bool TryGetVersion(string id, out string version)
        {
            if (providers.TryGetValue(id, out var provider) && provider.Available) { version = provider.Version; return true; }
            version = ""; return false;
        }
        internal void ReserveRuntimeProvider(string id)
        {
            if (providers.TryGetValue(id, out var existing))
            {
                if (existing.Origin != ProviderOrigin.Runtime) throw new InvalidOperationException("Runtime provider conflicts with package: " + id);
            }
            else providers.Add(id, new ProviderRegistration(id, ProviderOrigin.Runtime));
        }
        public bool IsLoaded(string uniqueId) => providers.TryGetValue(uniqueId, out var provider) && provider.Available;
        public IManifest? Get(string uniqueId) => providers.TryGetValue(uniqueId, out var provider) && provider.Available ? provider.Metadata : null;
        public IReadOnlyList<IManifest> GetAll() => providers.Values.Where(p => p.Available && p.Metadata != null).Select(p => p.Metadata!).OrderBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();

        public TApi? GetApi<TApi>(string uniqueId) where TApi : class
        {
            return apis.TryGetValue(new ApiRegistrationKey(uniqueId, typeof(TApi)), out ApiRegistration api) ? api.Value as TApi : null;
        }

        public void RegisterApi<TApi>(TApi api) where TApi : class
        {
            throw new InvalidOperationException("API registration must use an owner-bound mod helper registry.");
        }

        public IModRegistry CreateOwnerBoundRegistry(IManifest owner, Action ensureOwnerActive)
        {
            return new OwnerBoundModRegistry(this, owner, ensureOwnerActive);
        }

        // A content activation has a narrower lifetime than its package. Do not
        // reuse a facade captured under a sibling unit or retired Host guard.
        internal IModRegistry CreateContentBoundRegistry(IManifest owner, Action guard)
            => new OwnerBoundModRegistry(this, owner, guard, new object());

        internal void RegisterApiForOwner<TApi>(IManifest owner, TApi api, IOwnerBoundApiFactory? ownerBoundFactory = null) where TApi : class
        {
            var key = new ApiRegistrationKey(owner.UniqueID, typeof(TApi));
            if (apis.ContainsKey(key))
                throw new InvalidOperationException("Owner '" + owner.UniqueID + "' already registered API contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
            apis.Add(key, new ApiRegistration(api, ownerBoundFactory ?? api as IOwnerBoundApiFactory));
        }

        internal void RegisterProcessLifetimeApiForOwner<TApi>(IManifest owner, TApi api, IOwnerBoundApiFactory? ownerBoundFactory = null) where TApi : class
        {

            ReserveRuntimeProvider(owner.UniqueID);
            var provider = providers[owner.UniqueID];
            if (provider.Metadata != null && !HasSameCanonicalManifest(provider.Metadata, owner))
                throw new InvalidOperationException("Runtime provider metadata conflicts: " + owner.UniqueID);
            bool wasAvailable = provider.Available;
            try
            {
                RegisterApiForOwner(owner, api, ownerBoundFactory);
                provider.Metadata = owner;
                provider.Version = owner.Version;
                provider.Available = true;
            }
            catch
            {
                provider.Available = wasAvailable;
                throw;
            }
        }

        internal void RegisterRuntimeApiFactory<T>(string id, string version, Func<IManifest, Action, T> create) where T : class
        {
            ReserveRuntimeProvider(id);
            var key = new ApiRegistrationKey(id, typeof(T));
            var factory = new RuntimeApiFactory<T>(create);
            apis.Add(key, new ApiRegistration(factory, factory));
            var provider = providers[id];
            provider.Version = version; provider.Available = true;
        }
        // Internal domain composition needs a guarded façade, not another cached
        // registry root. The accepted contribution owns its resources and uses.
        internal T? GetContributionApi<T>(IManifest owner, string provider, Action guard) where T : class
            => GetApiForOwner<T>(owner, provider, guard, cache: false);
        private sealed class RuntimeApiFactory<T> : IOwnerBoundApiFactory where T : class
        {
            private readonly Func<IManifest, Action, T> create;
            internal RuntimeApiFactory(Func<IManifest, Action, T> create) { this.create = create; }
            public object CreateOwnerBoundApi(Type contract, IManifest owner, Action guard) => create(owner, guard);
        }

        internal int RemoveOwner(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                return 0;

            int removed = 0;
            if (providers.TryGetValue(uniqueId, out var provider))
            {
                removed = provider.Available ? 1 : 0;
                if (provider.Origin == ProviderOrigin.Runtime) provider.Available = false;
                else providers.Remove(uniqueId);
            }
            foreach (ApiRegistrationKey key in apis.Keys.Where(k => OwnerEquals(k.OwnerId, uniqueId)).ToArray())
            {
                if (apis.Remove(key))
                    removed++;
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

            return removed;
        }

        private sealed class OwnerBoundModRegistry : IModRegistry, IDisposable
        {
            private readonly ModRegistryService inner;
            private readonly IManifest owner;
            private readonly Action ensureOwnerActive;
            private readonly object? scope;

            public OwnerBoundModRegistry(ModRegistryService inner, IManifest owner, Action ensureOwnerActive, object? scope = null)
            {
                this.inner = inner;
                this.owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
                this.scope = scope;
            }

            public bool IsLoaded(string uniqueId) { ensureOwnerActive(); return inner.IsLoaded(uniqueId); }
            public IManifest? Get(string uniqueId) { ensureOwnerActive(); return inner.Get(uniqueId); }
            public IReadOnlyList<IManifest> GetAll() { ensureOwnerActive(); return inner.GetAll(); }
            public TApi? GetApi<TApi>(string uniqueId) where TApi : class => inner.GetApiForOwner<TApi>(owner, uniqueId, ensureOwnerActive, scope);
            public void Dispose()
            {
                if (scope == null) return;
                foreach (var key in inner.ownerBoundFacades.Keys.Where(k => ReferenceEquals(k.Scope, scope)).ToArray())
                {
                    var facade = inner.ownerBoundFacades[key]; inner.ownerBoundFacades.Remove(key); TryDeactivateFacade(facade);
                }
            }
            public void RegisterApi<TApi>(TApi api) where TApi : class
            {
                ensureOwnerActive();
                inner.RegisterApiForOwner(owner, api);
            }
        }


        private TApi? GetApiForOwner<TApi>(IManifest consumer, string providerId, Action ensureOwnerActive, object? scope = null, bool cache = true) where TApi : class
        {
            ensureOwnerActive();
            var providerKey = new ApiRegistrationKey(providerId, typeof(TApi));
            if (!apis.TryGetValue(providerKey, out ApiRegistration api))
                return null;
            IOwnerBoundApiFactory? factory = api.Factory;
            if (factory == null)
                return api.Value as TApi;

            var facadeKey = new OwnerBoundFacadeKey(consumer.UniqueID, providerId, typeof(TApi), scope);
            object? facade = null;
            if (!cache || !ownerBoundFacades.TryGetValue(facadeKey, out facade))
            {
                Action ensureFacadeActive = () =>
                {
                    ensureOwnerActive();
                    if (!apis.TryGetValue(providerKey, out ApiRegistration current) || !ReferenceEquals(current, api))
                        throw new InvalidOperationException("API provider '" + providerId + "' is inactive.");
                };
                facade = factory.CreateOwnerBoundApi(typeof(TApi), consumer, ensureFacadeActive);
                try
                {
                    // A factory can reenter owner removal. Never publish its late result into the cache.
                    ensureFacadeActive();
                    if (!(facade is TApi))
                        throw new InvalidOperationException("Owner-bound API factory for provider '" + providerId + "' returned an incompatible facade for contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
                    if (cache) ownerBoundFacades.Add(facadeKey, facade);
                }
                catch
                {
                    TryDeactivateFacade(facade);
                    throw;
                }
            }
            return facade as TApi;
        }

        internal int CountOwner(string uniqueId)
        {
            return (IsLoaded(uniqueId) ? 1 : 0) +
                apis.Keys.Count(k => OwnerEquals(k.OwnerId, uniqueId)) +
                ownerBoundFacades.Keys.Count(k => OwnerEquals(k.ConsumerId, uniqueId) || OwnerEquals(k.ProviderId, uniqueId));
        }

        internal int TotalRootCount => providers.Values.Count(p => p.Available) + apis.Count + ownerBoundFacades.Count;

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

        private sealed class ApiRegistration
        {
            internal ApiRegistration(object value, IOwnerBoundApiFactory? factory) { Value = value; Factory = factory; }
            internal object Value { get; }
            internal IOwnerBoundApiFactory? Factory { get; }
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
            public OwnerBoundFacadeKey(string consumerId, string providerId, Type contract, object? scope)
            {
                ConsumerId = consumerId ?? string.Empty;
                ProviderId = providerId ?? string.Empty;
                Contract = contract ?? throw new ArgumentNullException(nameof(contract));
                Scope = scope;
            }

            public string ConsumerId { get; }
            public string ProviderId { get; }
            public Type Contract { get; }
            public object? Scope { get; }
        }

        private sealed class OwnerBoundFacadeKeyComparer : IEqualityComparer<OwnerBoundFacadeKey>
        {
            public static readonly OwnerBoundFacadeKeyComparer Instance = new OwnerBoundFacadeKeyComparer();

            public bool Equals(OwnerBoundFacadeKey x, OwnerBoundFacadeKey y) =>
                OwnerEquals(x.ConsumerId, y.ConsumerId) &&
                OwnerEquals(x.ProviderId, y.ProviderId) &&
                x.Contract == y.Contract && ReferenceEquals(x.Scope, y.Scope);

            public int GetHashCode(OwnerBoundFacadeKey obj)
            {
                unchecked
                {
                    int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ConsumerId);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProviderId);
                    hash = (hash * 397) ^ obj.Contract.GetHashCode();
                    return (hash * 397) ^ (obj.Scope == null ? 0 : RuntimeHelpers.GetHashCode(obj.Scope));
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
