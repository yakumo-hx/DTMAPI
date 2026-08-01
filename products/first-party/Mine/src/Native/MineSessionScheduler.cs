using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DTMAPI.Mine
{
    internal sealed class MineSessionScheduler
    {
        private readonly Dictionary<object, MineScheduleEntry> entries =
            new Dictionary<object, MineScheduleEntry>(
                ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> observed =
            new HashSet<object>(ReferenceEqualityComparer.Instance);
        private readonly List<object> stale = new List<object>();

        internal int Count => entries.Count;
        internal IEnumerable<MineScheduleEntry> Entries => entries.Values;

        internal void BeginAuthoritativeScan() =>
            observed.Clear();

        internal bool Observe(
            object equipment,
            int totalTus,
            int cycleTus,
            out MineScheduleEntry entry)
        {
            if (!observed.Add(equipment))
            {
                entry = entries[equipment];
                return false;
            }
            if (entries.TryGetValue(equipment, out entry!))
            {
                entry.LastObservedTotalTus = totalTus;
                return false;
            }
            entry = new MineScheduleEntry(
                equipment,
                checked(totalTus + Math.Max(1, cycleTus)),
                totalTus);
            entries.Add(equipment, entry);
            return true;
        }

        internal int EndAuthoritativeScan()
        {
            stale.Clear();
            foreach (KeyValuePair<object, MineScheduleEntry> pair in entries)
            {
                if (!observed.Contains(pair.Key))
                    stale.Add(pair.Key);
            }
            foreach (object equipment in stale)
                entries.Remove(equipment);
            int removed = stale.Count;
            stale.Clear();
            return removed;
        }

        internal bool Contains(object equipment) =>
            entries.ContainsKey(equipment);

        internal bool TryGet(
            object equipment,
            out MineScheduleEntry entry) =>
            entries.TryGetValue(equipment, out entry!);

        internal void Clear()
        {
            entries.Clear();
            observed.Clear();
            stale.Clear();
        }

        private sealed class ReferenceEqualityComparer :
            IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer Instance =
                new ReferenceEqualityComparer();

            public new bool Equals(object? x, object? y) =>
                ReferenceEquals(x, y);

            public int GetHashCode(object obj) =>
                RuntimeHelpers.GetHashCode(obj);
        }
    }
}
