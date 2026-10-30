using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    // Dependency/exit ownership also applies to content-only owners and built-in providers.
    // It must not depend on construction of a DtmMod instance.
    internal sealed class OwnerDependencyStore
    {
        private readonly Dictionary<string, Owner> owners = new Dictionary<string, Owner>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ContributionScope> contributions = new List<ContributionScope>();
        private readonly HashSet<ContributionScope> pendingCleanup = new HashSet<ContributionScope>();
        internal ContributionScope CreateContribution(string owner, string id, Action guard)
        {
            contributions.RemoveAll(c => c.Released);
            var scope = new ContributionScope(owner, id, guard, changed =>
            {
                if (changed.Released) { pendingCleanup.Remove(changed); contributions.Remove(changed); }
                else if (changed.Closed) pendingCleanup.Add(changed);
            });
            contributions.Add(scope);
            return scope;
        }
        internal void RetryPendingContributions()
        {
            if (pendingCleanup.Count == 0) return;
            foreach (var scope in pendingCleanup.ToArray()) scope.TryClose(out _);
            contributions.RemoveAll(c => c.Released);
        }
        internal int CountContributions(string owner) => contributions.Count(c => !c.Released &&
            (string.Equals(c.Owner, owner, StringComparison.OrdinalIgnoreCase) || c.Providers.Contains(owner)));
        internal bool PrepareContributionsExit(string owner, out string failure)
        {
            var selected = contributions.Where(c => !c.Released &&
                (string.Equals(c.Owner, owner, StringComparison.OrdinalIgnoreCase) || c.Providers.Contains(owner))).Reverse().ToArray();
            foreach (var scope in selected) scope.Close();
            foreach (var scope in selected)
                if (!scope.TryClose(out string error)) { failure = scope.Owner + "/" + scope.Id + ": " + error; return false; }
            contributions.RemoveAll(c => c.Released);
            failure = "";
            return true;
        }
        internal void SetRequiredProviders(string ownerId, PackageDependencyManifest manifest)
            => Get(ownerId).RequiredProviders = manifest.Dependencies.Where(d => d.Required).Select(d => d.Id).ToArray();
        internal string[] RequiredDependents(string providerId) => owners
            .Where(p => p.Value.RequiredProviders.Contains(providerId, StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Key).OrderByDescending(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        internal bool CleanupComplete(string ownerId) => owners.TryGetValue(ownerId, out var owner) && owner.CleanupComplete;
        internal void SetCleanupComplete(string ownerId, bool complete)
        {
            var owner = Get(ownerId); owner.CleanupComplete = complete;
            if (complete) owner.RequiredProviders = Array.Empty<string>();
        }
        internal bool BeginCleanupRequest(string ownerId)
        {
            var owner = Get(ownerId);
            if (owner.CleanupRequested) return false;
            owner.CleanupRequested = true; owner.CleanupComplete = false; return true;
        }
        internal void EndCleanupRequest(string ownerId) => Get(ownerId).CleanupRequested = false;
        private Owner Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Owner id is required.", nameof(id));
            if (!owners.TryGetValue(id, out var owner)) owners.Add(id, owner = new Owner());
            return owner;
        }
        private sealed class Owner
        {
            internal bool CleanupComplete, CleanupRequested;
            internal string[] RequiredProviders = Array.Empty<string>();
        }
    }
}
