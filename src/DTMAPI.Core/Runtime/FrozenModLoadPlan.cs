using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    // Process-lifetime order metadata only. It contains no read transaction, package bundle,
    // loaded Assembly or admission result and cannot authorize a load or registration.
    internal sealed class FrozenModLoadPlan
    {
        internal sealed class Entry
        {
            internal Entry(DiscoveredMod source, int index)
            { Source = source; Owner = source.Manifest.UniqueID; Version = source.Manifest.Version; LoadIndex = index; }
            internal DiscoveredMod Source { get; }
            internal string Owner { get; }
            internal string Version { get; }
            internal int LoadIndex { get; }
            internal int OfficialLoadIndex { get; set; }
            internal bool Matches(DiscoveredMod current) =>
                string.Equals(Version, current.Manifest.Version, StringComparison.OrdinalIgnoreCase) &&
                DtmApiRuntime.HasSameSourceIdentity(Source, current);
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Entry> rows = new List<Entry>();
        internal string Id { get; } = Guid.NewGuid().ToString("N");
        internal IReadOnlyList<Entry> Entries => rows.AsReadOnly();

        internal FrozenModLoadPlan(IEnumerable<DiscoveredMod> ordered, Action<string> log)
        {
            Append(ordered, log);
        }

        internal void Append(IEnumerable<DiscoveredMod> ordered, Action<string> log)
        {
            var added = new List<Entry>();
            foreach (var mod in ordered)
            {
                if (entries.TryGetValue(mod.Manifest.UniqueID, out Entry? existing))
                {
                    if (existing.Matches(mod)) continue;
                    var replacement = new Entry(mod, existing.LoadIndex) { OfficialLoadIndex = existing.OfficialLoadIndex };
                    rows[existing.LoadIndex] = replacement;
                    entries[mod.Manifest.UniqueID] = replacement;
                    continue;
                }
                var row = new Entry(mod, rows.Count);
                rows.Add(row);
                entries.Add(row.Owner, row);
                added.Add(row);
            }
            int officialStart = rows.Count - added.Count;
            var official = PackageLoadOrder.Initial(added.Select(row => new PackagePlanInput
                { Id = row.Owner, OfficialPriority = Priority(row.Source) })).ToArray();
            for (int index = 0; index < official.Length; index++)
                entries[official[index].Id].OfficialLoadIndex = officialStart + index;
            foreach (Entry row in added)
                log("ModLoadPlan plan=" + Id + "; owner=" + row.Owner + "; loadIndex=" + row.LoadIndex +
                    "; officialOrder=" + row.OfficialLoadIndex + "; officialPriority=" + row.Source.OfficialPriority);
        }

        internal static int? Priority(DiscoveredMod mod) => mod.OfficialPriority < 0 ? (int?)null : mod.OfficialPriority;

        internal int? Resolve(DiscoveredMod mod) => entries.TryGetValue(mod.Manifest.UniqueID, out Entry? entry) && entry.Matches(mod)
            ? entry.LoadIndex : (int?)null;
        internal int? ResolveOfficial(DiscoveredMod mod) => entries.TryGetValue(mod.Manifest.UniqueID, out Entry? entry) && entry.Matches(mod)
            ? entry.OfficialLoadIndex : (int?)null;


    }

    public sealed partial class DtmApiRuntime
    {
        private FrozenModLoadPlan? frozenLoadPlan;

        // The owner-bound registry passes a canonical manifest, including a Pack before AddLoaded.
        // A same-ID object supplied by an author does not prove this identity.
        internal int? ResolveOwnerLoadIndex(IManifest owner)
        {
            DiscoveredMod? mod = loadedMods.FirstOrDefault(item => ReferenceEquals(item.Manifest, owner)) ??
                discoveredMods.FirstOrDefault(item => ReferenceEquals(item.Manifest, owner));
            return mod == null ? null : frozenLoadPlan?.Resolve(mod);
        }
        internal int? ResolveOfficialOwnerOrder(IManifest owner)
        {
            DiscoveredMod? mod = loadedMods.FirstOrDefault(item => ReferenceEquals(item.Manifest, owner)) ??
                discoveredMods.FirstOrDefault(item => ReferenceEquals(item.Manifest, owner));
            return mod == null ? null : frozenLoadPlan?.ResolveOfficial(mod);
        }
    }
}
