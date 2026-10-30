using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Services
{
    internal sealed class ContentHostManager : IModOwnerCleanupParticipant
    {
        private readonly Action ensureRuntimeThread;
        private readonly Action<string, string>? report;
        private readonly Dictionary<string, Host> hosts = new Dictionary<string, Host>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Pack> packs = new Dictionary<string, Pack>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal ContentHostManager(Action ensureRuntimeThread, Action<string, string>? report = null)
        { this.ensureRuntimeThread = ensureRuntimeThread; this.report = report; }
        public string ParticipantId => "Core.ContentHosts";
        internal string[] Describe() => states.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + ": " + pair.Value).ToArray();
        internal string[] PendingCleanupOwners() => packs.Values.Where(pack => pack.Closed && !pack.Busy).Select(pack => pack.Owner).ToArray();
        internal IContentHostService CreateService(string owner, Action guard) => new HostService(this, owner, guard);

        internal void Activate(IManifest manifest, ContentPackBinding binding, string packageRoot, Action ensurePackActive,
            ModRegistryService registry, TranslationService translation)
        {
            ensureRuntimeThread();
            ensurePackActive();
            if (packs.ContainsKey(manifest.UniqueID)) throw new InvalidOperationException("content-host-cleanup-pending: " + manifest.UniqueID);
            if (!hosts.TryGetValue(binding.HostId, out Host host))
            { SetState(manifest.UniqueID, "Rejected; Host=" + binding.HostId + "; content-host-missing-handler"); throw new InvalidOperationException("content-host-missing-handler: " + binding.HostId); }
            host.Guard();
            if (!binding.ContractRange.Contains(host.Version))
            { SetState(manifest.UniqueID, "Rejected; Host=" + binding.HostId + "; content-host-contract-version"); throw new InvalidOperationException("content-host-contract-version: " + binding.HostId + " offers " + host.Version.Text); }
            ContentPackBinding.ValidatePackage(packageRoot);
            var scope = new ContributionScope(manifest.UniqueID, binding.HostId, () =>
            {
                ensureRuntimeThread();
                ensurePackActive();
                host.Guard();
                if (!hosts.TryGetValue(binding.HostId, out Host current) || !ReferenceEquals(current, host))
                    throw new ObjectDisposedException(nameof(IContentHostService), "Host execution has closed: " + binding.HostId);
            });
            var pack = new Pack(host, manifest.UniqueID, scope);
            packs.Add(pack.Owner, pack);
            var context = scope.Own(new PackContext(manifest, binding, OwnerFilePath.Resolve(packageRoot, ContentPackBinding.DataDirectory),
                scope.Guard, registry.CreateContentBoundRegistry(manifest, scope.Guard), translation));
            try
            {
                SetState(pack.Owner, "Preparing; Host=" + host.Owner + "; schema=" + binding.Schema);
                scope.Activate(() => host.Prepare(context) ?? throw new InvalidOperationException("content-host-null-activation: " + host.Owner),
                    activation => activation.Activate());
                SetState(pack.Owner, "Active; Host=" + host.Owner + "; schema=" + binding.Schema);
            }
            catch (Exception ex)
            {
                scope.Close();
                SetState(pack.Owner, "Failed; Host=" + host.Owner + "; " + ex.Message);
                throw;
            }
        }

        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            ensureRuntimeThread();
            int removed = 0, remaining = 0, failures = 0;
            string details = "Host/Pack cleanup complete.";
            // Runtime closes required dependents before their provider. Keep this guard for entry compensation and reentry.
            Pack[] owned = packs.Values.Where(p => p.Owner.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ||
                p.Host.Owner.Equals(ownerId, StringComparison.OrdinalIgnoreCase)).Reverse().ToArray();
            foreach (Pack pack in owned) pack.Scope.Close();
            foreach (Pack pack in owned)
            {
                if (pack.Scope.TryClose(out string failure))
                {
                    packs.Remove(pack.Owner);
                    removed++;
                    SetState(pack.Owner, "Withdrawn; Host=" + pack.Host.Owner + (pack.Scope.Failure.Length == 0 ? "" : "; activationFailure=" + pack.Scope.Failure));
                }
                else
                {
                    remaining++; failures++;
                    details = "content-host-cleanup-failed: " + pack.Owner + "; " + failure;
                    SetState(pack.Owner, "CleanupPending; Host=" + pack.Host.Owner + "; " + details);
                }
            }
            if (remaining == 0 && hosts.Remove(ownerId)) removed++;
            return new ModOwnerCleanupParticipantResult(removed, remaining, failures, details);
        }

        private void SetState(string owner, string state)
        {
            if (state.Length > 2048) state = state.Substring(0, 2048);
            states[owner] = state;
            try { report?.Invoke(owner, state); } catch { /* Diagnostics cannot alter the activation transaction. */ }
        }

        private sealed class HostService : IContentHostService
        {
            private readonly ContentHostManager manager;
            private readonly string owner;
            private readonly Action guard;
            internal HostService(ContentHostManager manager, string owner, Action guard)
            { this.manager = manager; this.owner = owner; this.guard = guard; }
            public void Register(string contractVersion, Func<IContentPack, IContentPackActivation> prepare)
            {
                manager.ensureRuntimeThread(); guard();
                if (prepare == null) throw new ArgumentNullException(nameof(prepare));
                var version = PackageSemanticVersion.Parse(contractVersion);
                if (manager.hosts.ContainsKey(owner)) throw new InvalidOperationException("content-host-already-registered: " + owner);
                manager.hosts.Add(owner, new Host(owner, version, guard, prepare));
            }
        }

        private sealed class Host
        {
            internal readonly string Owner;
            internal readonly PackageSemanticVersion Version;
            internal readonly Action Guard;
            internal readonly Func<IContentPack, IContentPackActivation> Prepare;
            internal Host(string owner, PackageSemanticVersion version, Action guard, Func<IContentPack, IContentPackActivation> prepare)
            { Owner = owner; Version = version; Guard = guard; Prepare = prepare; }
        }
        private sealed class Pack
        {
            internal readonly Host Host;
            internal readonly string Owner;
            internal readonly ContributionScope Scope;
            internal bool Closed => Scope.Closed;
            internal bool Busy => Scope.Busy;
            internal Pack(Host host, string owner, ContributionScope scope) { Host = host; Owner = owner; Scope = scope; }
        }
        internal sealed class PackContext : IContentPack, IDisposable
        {
            private readonly Action guard;
            private readonly IModRegistry registry;
            internal PackContext(IManifest manifest, ContentPackBinding binding, string root, Action guard,
                IModRegistry registry, TranslationService translation)
                : this(manifest, binding.HostId, binding.Schema, root, guard, registry, translation) { }
            internal PackContext(IManifest manifest, string hostId, string schema, string root, Action guard,
                IModRegistry registry, TranslationService translation)
            {
                this.guard = guard; this.registry = registry;
                Manifest = new ManifestSnapshot(manifest); HostId = hostId; Schema = schema;
                Files = new OwnerFileService(root, guard); Resources = new ModResourceService(root, guard);
                Translation = new BoundTranslation(translation, guard);
            }
            public IManifest Manifest { get; }
            public string HostId { get; }
            public string Schema { get; }
            public IOwnerFileHelper Files { get; }
            public IModResourceHelper Resources { get; }
            public ITranslationHelper Translation { get; }
            public IManifest? GetRequiredDependency(string providerId)
            {
                guard();
                if (!Manifest.Dependencies.Any(d => d.Required && d.UniqueID.Equals(providerId, StringComparison.OrdinalIgnoreCase))) return null;
                IManifest? provider = registry.Get(providerId);
                return provider == null ? null : new ManifestSnapshot(provider);
            }
            public TApi? GetApi<TApi>(string providerId) where TApi : class { guard(); return registry.GetApi<TApi>(providerId); }
            internal void CloseApis() => (registry as IDisposable)?.Dispose();
            public void Dispose() => CloseApis();
        }
        private sealed class BoundTranslation : ITranslationHelper
        {
            private readonly ITranslationHelper inner;
            private readonly Action guard;
            internal BoundTranslation(ITranslationHelper inner, Action guard) { this.inner = inner; this.guard = guard; }
            public string Language { get { guard(); return inner.Language; } }
            public string Get(string key, string fallback = "") { guard(); return inner.Get(key, fallback); }
        }
        private sealed class ManifestSnapshot : IManifest
        {
            internal ManifestSnapshot(IManifest m)
            {
                Name = m.Name; Author = m.Author; Version = m.Version; Description = m.Description; UniqueID = m.UniqueID;
                EntryDll = m.EntryDll; EntryType = m.EntryType; MinimumDTMApiVersion = m.MinimumDTMApiVersion;
                MinimumGameVersion = m.MinimumGameVersion; Type = m.Type;
                Dependencies = Array.AsReadOnly(m.Dependencies.Select(d => (IManifestDependency)new DependencySnapshot(d)).ToArray());
                UpdateKeys = Array.AsReadOnly(m.UpdateKeys.ToArray());
            }
            public string Name { get; } public string Author { get; } public string Version { get; }
            public string Description { get; } public string UniqueID { get; } public string EntryDll { get; }
            public string EntryType { get; } public string MinimumDTMApiVersion { get; } public string MinimumGameVersion { get; }
            public string Type { get; } public IReadOnlyList<IManifestDependency> Dependencies { get; }
            public IReadOnlyList<string> UpdateKeys { get; }
        }
        private sealed class DependencySnapshot : IManifestDependency
        {
            internal DependencySnapshot(IManifestDependency d) { UniqueID = d.UniqueID; MinimumVersion = d.MinimumVersion; Required = d.Required; }
            public string UniqueID { get; } public string MinimumVersion { get; } public bool Required { get; }
        }
    }
}
