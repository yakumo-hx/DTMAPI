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

        private readonly Dictionary<string, Entry> entries;
        private readonly HashSet<string> reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal string Id { get; } = Guid.NewGuid().ToString("N");
        internal IReadOnlyList<Entry> Entries { get; }

        internal FrozenModLoadPlan(IEnumerable<DiscoveredMod> ordered, Action<string> log)
        {
            Entry[] rows = ordered.Select((mod, index) => new Entry(mod, index)).ToArray();
            Entries = Array.AsReadOnly(rows);
            entries = rows.ToDictionary(row => row.Owner, StringComparer.OrdinalIgnoreCase);
            string[] initial = PackageLoadOrder.Initial(rows.Select(row => new PackagePlanInput
            { Id = row.Owner, OfficialPriority = Priority(row.Source) })).Select(node => node.Id).ToArray();
            foreach (Entry row in rows)
            {
                row.OfficialLoadIndex = Array.FindIndex(initial, id => string.Equals(id, row.Owner, StringComparison.OrdinalIgnoreCase));
                string reason = row.Source.OfficialPriority < 0 ? "OfficialOrderUnavailable-ID-fallback" :
                    rows.Count(other => other.Source.OfficialPriority == row.Source.OfficialPriority) > 1 ? "OfficialOrderTied-ID-fallback" : "OfficialOrder";
                if (!string.Equals(initial[row.LoadIndex], row.Owner, StringComparison.OrdinalIgnoreCase)) reason += "+DependencyAdjusted";
                log("ModLoadPlan frozen plan=" + Id + "; owner=" + row.Owner + "; loadIndex=" + row.LoadIndex +
                    "; officialPriority=" + row.Source.OfficialPriority + "; reason=" + reason +
                    "; dependencies=" + string.Join(",", ((IManifest)row.Source.Manifest).Dependencies.Select(dependency => dependency.UniqueID)) +
                    "; source=" + row.Source.Source + "; officialId=" + row.Source.OfficialId + "; version=" + row.Version + "; root=" + row.Source.RootPath);
            }
        }

        internal static int? Priority(DiscoveredMod mod) => mod.OfficialPriority < 0 ? (int?)null : mod.OfficialPriority;

        internal int? Resolve(DiscoveredMod mod) => entries.TryGetValue(mod.Manifest.UniqueID, out Entry? entry) && entry.Matches(mod)
            ? entry.LoadIndex : (int?)null;
        internal int? ResolveOfficial(DiscoveredMod mod) => entries.TryGetValue(mod.Manifest.UniqueID, out Entry? entry) && entry.Matches(mod)
            ? entry.OfficialLoadIndex : (int?)null;

        internal IReadOnlyList<DiscoveredMod> PreserveOrder(IEnumerable<DiscoveredMod> current) =>
            current.OrderBy(mod => Resolve(mod) ?? int.MaxValue).ToArray();

        internal void ObserveRefresh(IEnumerable<DiscoveredMod> current, Action<string> warn)
        {
            foreach (DiscoveredMod mod in current.Where(mod => mod.OfficialEnabled))
            {
                if (!entries.TryGetValue(mod.Manifest.UniqueID, out Entry? entry) || !entry.Matches(mod))
                {
                    if (reported.Add("unplanned:" + mod.Manifest.UniqueID + ":" + mod.RootPath + ":" + mod.Manifest.Version))
                        warn("ModLoadPlan unplanned-owner/restart-required: " + mod.Manifest.UniqueID +
                            "; normal admission remains independent; no trusted loadIndex is assigned in plan=" + Id + ".");
                }
                else if (entry.Source.OfficialPriority != mod.OfficialPriority && reported.Add("order:" + mod.Manifest.UniqueID + ":" + mod.OfficialPriority))
                    warn("ModLoadPlan official-order-changed/restart-required: " + mod.Manifest.UniqueID +
                        "; retained loadIndex=" + entry.LoadIndex + "; originalPriority=" + entry.Source.OfficialPriority +
                        "; currentPriority=" + mod.OfficialPriority + "; restart to apply the new order.");
            }
        }
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
