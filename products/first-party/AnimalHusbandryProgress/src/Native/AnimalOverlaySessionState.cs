using System;
using System.Collections.Generic;
using System.Globalization;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalOverlaySessionState<T> where T : class
    {
        private readonly List<T> tracked = new List<T>();
        internal IReadOnlyList<T> Tracked => tracked;
        internal int Count => tracked.Count;
        internal void Track(T item) => tracked.Add(item ?? throw new ArgumentNullException(nameof(item)));
        internal void EnsureCount(int count, Func<int, T> create)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (create == null) throw new ArgumentNullException(nameof(create));
            while (tracked.Count < count) Track(create(tracked.Count));
        }
        internal int Clear(Action<T> destroy)
        {
            if (destroy == null) throw new ArgumentNullException(nameof(destroy));
            int removed = 0;
            var failures = new List<Exception>();
            for (int i = tracked.Count - 1; i >= 0; i--)
            {
                try { destroy(tracked[i]); tracked.RemoveAt(i); removed++; }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (failures.Count > 0) throw new AggregateException("Owned animal rows remain pending cleanup.", failures);
            return removed;
        }
    }
    internal static class AnimalStableRowPlan
    {
        internal static T[] CreateRows<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var rows = new List<T>(source);
            rows.Sort(comparison);
            return rows.ToArray();
        }
    }
    internal sealed class AnimalProgressRow
    {
        internal AnimalProgressRow(string id, string title, int current, int threshold)
        {
            if (threshold <= 0) throw new ArgumentOutOfRangeException(nameof(threshold));
            Id = id; Title = title; Current = current; Threshold = threshold;
        }
        internal string Id { get; }
        internal string Title { get; }
        internal int Current { get; }
        internal int Threshold { get; }
        internal double Ratio => Current / (double)Threshold;
        internal float Fill => (float)Math.Max(0, Math.Min(1, Ratio));
        internal string Text => Current.ToString(CultureInfo.InvariantCulture) + "/" + Threshold.ToString(CultureInfo.InvariantCulture);
        internal static int Compare(AnimalProgressRow left, AnimalProgressRow right)
        {
            int progress = right.Ratio.CompareTo(left.Ratio);
            if (progress != 0) return progress;
            int title = StringComparer.OrdinalIgnoreCase.Compare(left.Title, right.Title);
            return title != 0 ? title : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
