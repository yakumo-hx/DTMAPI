using System;
using System.Collections.Generic;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalOverlaySessionState<T> where T : class
    {
        private readonly List<T> tracked = new List<T>();
        private bool nextFrameGuardPending;

        internal IReadOnlyList<T> Tracked => tracked;
        internal int Count => tracked.Count;
        internal bool NextFrameGuardPending => nextFrameGuardPending;

        internal void Track(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            tracked.Add(item);
        }

        internal void ArmNextFrameGuard()
            => nextFrameGuardPending = tracked.Count > 0;

        internal bool TryConsumeNextFrameGuard()
        {
            if (!nextFrameGuardPending)
                return false;
            nextFrameGuardPending = false;
            return true;
        }

        internal int Clear(Action<T> destroy)
        {
            if (destroy == null)
                throw new ArgumentNullException(nameof(destroy));
            int count = tracked.Count;
            for (int i = tracked.Count - 1; i >= 0; i--)
                destroy(tracked[i]);
            tracked.Clear();
            nextFrameGuardPending = false;
            return count;
        }
    }

    internal static class AnimalStableRowPlan
    {
        internal static T[] CreateTopRows<T>(IEnumerable<T> source, Comparison<T> comparison, int maximumRows)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            if (maximumRows <= 0)
                return Array.Empty<T>();

            var rows = new List<T>(source);
            rows.Sort(comparison);
            if (rows.Count > maximumRows)
                rows.RemoveRange(maximumRows, rows.Count - maximumRows);
            return rows.ToArray();
        }
    }
}
