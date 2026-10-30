using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Runtime
{
    /// <summary>Runtime-thread-owned instance lifetime and publication; one retained instance per owner.</summary>
    internal sealed class ModOwnerInstanceStore
    {
        private readonly Dictionary<string, Owner> owners = new Dictionary<string, Owner>(StringComparer.OrdinalIgnoreCase);

        internal void Capture(string ownerId, DtmMod instance)
        {
            Owner owner = GetOrCreate(ownerId);
            if (owner.Instance != null) throw new InvalidOperationException("Owner already has a constructed instance: " + ownerId);
            owner.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        internal void Publish(string ownerId)
        {
            Owner owner = GetOrCreate(ownerId);
            if (owner.Instance == null || owner.Published) throw new InvalidOperationException("Owner instance cannot be published: " + ownerId);
            owner.Published = true;
        }

        internal DtmMod? GetPublished(string ownerId) => owners.TryGetValue(ownerId, out Owner owner) && owner.Published ? owner.Instance : null;
        internal bool HasPublished(string ownerId) => owners.TryGetValue(ownerId, out Owner owner) && owner.Published;
        internal string[] PublishedOwners => owners.Where(pair => pair.Value.Published).Select(pair => pair.Key).ToArray();
        internal void ClearPublications() { foreach (Owner owner in owners.Values) owner.Published = false; }

        internal bool TryGetConstructed(string ownerId, out DtmMod instance)
        {
            instance = owners.TryGetValue(ownerId, out Owner owner) ? owner.Instance! : null!;
            return instance != null;
        }

        internal void ReleaseConstructed(string ownerId) { if (owners.TryGetValue(ownerId, out Owner owner)) owner.Instance = null; }
        internal bool Unpublish(string ownerId)
        {
            if (!owners.TryGetValue(ownerId, out Owner owner) || !owner.Published) return false;
            owner.Published = false;
            return true;
        }

        internal int CountRoots(string ownerId) => owners.TryGetValue(ownerId, out Owner owner)
            ? (owner.Instance != null ? 1 : 0) + (owner.Published ? 1 : 0) : 0;
        internal void MarkAssemblyLoadAttempt(string ownerId) => GetOrCreate(ownerId).AssemblyLoaded = true;
        internal bool HasAssemblyLoadAttempt(string ownerId) => owners.TryGetValue(ownerId, out Owner owner) && owner.AssemblyLoaded;

        private Owner GetOrCreate(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner id is required.", nameof(ownerId));
            if (!owners.TryGetValue(ownerId, out Owner owner)) owners.Add(ownerId, owner = new Owner());
            return owner;
        }

        private sealed class Owner
        {
            internal DtmMod? Instance;
            internal bool Published;
            internal bool AssemblyLoaded;
        }
    }
}
