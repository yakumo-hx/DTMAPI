using System;
using System.Threading;

namespace DTMAPI.Core.Runtime
{
    /// <summary>
    /// Copy-on-write multicast storage for internal runtime boundaries. Publications
    /// reuse one immutable array; add/remove follows normal event duplicate semantics.
    /// </summary>
    internal sealed class RuntimeBoundarySlot<TDelegate> where TDelegate : Delegate
    {
        private readonly object gate = new object();
        private TDelegate[] snapshot = Array.Empty<TDelegate>();

        internal TDelegate[] Snapshot => Volatile.Read(ref snapshot);

        internal void Add(TDelegate? handler)
        {
            if (handler == null)
                return;

            lock (gate)
            {
                TDelegate[] current = snapshot;
                var next = new TDelegate[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = handler;
                Volatile.Write(ref snapshot, next);
            }
        }

        internal void Remove(TDelegate? handler)
        {
            if (handler == null)
                return;

            lock (gate)
            {
                TDelegate[] current = snapshot;
                int index = -1;
                for (int candidate = current.Length - 1; candidate >= 0; candidate--)
                {
                    if (Delegate.Equals(current[candidate], handler))
                    {
                        index = candidate;
                        break;
                    }
                }
                if (index < 0)
                    return;

                if (current.Length == 1)
                {
                    Volatile.Write(ref snapshot, Array.Empty<TDelegate>());
                    return;
                }

                var next = new TDelegate[current.Length - 1];
                if (index > 0)
                    Array.Copy(current, 0, next, 0, index);
                if (index < current.Length - 1)
                    Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                Volatile.Write(ref snapshot, next);
            }
        }
    }
}
