using System;
using System.Collections.Generic;
using DTMAPI.Abstractions.Internal;

namespace DTMAPI.Core.Runtime
{
    // One registration lifetime, shared by file adapters and direct API registrations.
    // A late callback result remains owned even when that callback closes its parent.
    internal sealed class ContributionScope : IRuntimeContribution
    {
        private readonly Action parentGuard;
        private readonly Action<ContributionScope>? cleanupChanged;
        private readonly List<IDisposable> resources = new List<IDisposable>();
        internal HashSet<string> Providers { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal ContributionScope(string owner, string id, Action parentGuard, Action<ContributionScope>? cleanupChanged = null)
        { Owner = owner; Id = id; this.parentGuard = parentGuard; this.cleanupChanged = cleanupChanged; }
        internal string Owner { get; }
        internal string Id { get; }
        internal object Generation { get; } = new object();
        string IRuntimeContribution.Owner => Owner;
        object IRuntimeContribution.Generation => Generation;
        void IRuntimeContribution.Guard() => Guard();
        T IRuntimeContribution.Own<T>(T value) => Own(value);
        T IRuntimeContribution.Activate<T>(Func<T> prepare, Action<T> activate) => Activate(prepare, activate);
        void IRuntimeContribution.UseProvider(string provider) { Guard(); Providers.Add(provider); }
        void IRuntimeContribution.Close() => Close();
        bool IRuntimeContribution.TryClose(out string failure) => TryClose(out failure);
        internal bool Closed { get; private set; }
        internal bool Busy { get; private set; }
        internal bool Released { get; private set; }
        internal string Failure { get; private set; } = "";
        internal void Guard()
        {
            if (Closed) throw new ObjectDisposedException("Contribution", Id);
            parentGuard();
        }
        internal T Own<T>(T value) where T : IDisposable
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (Released || Closed && !Busy) throw new ObjectDisposedException("Contribution", Id);
            resources.Add(value);
            return value;
        }
        internal T Activate<T>(Func<T> prepare, Action<T> activate) where T : IDisposable
        {
            Guard();
            if (Busy) throw new InvalidOperationException("Contribution callback is already running: " + Id);
            Busy = true;
            try
            {
                T value = Own(prepare());
                Guard();
                activate(value);
                Guard();
                return value;
            }
            catch (Exception ex) { Closed = true; Failure = ex.GetBaseException().Message; throw; }
            finally { Busy = false; }
        }
        internal void Close() { Closed = true; }
        internal bool TryClose(out string failure)
        {
            Close();
            if (Busy) { failure = "callback-running"; cleanupChanged?.Invoke(this); return false; }
            Busy = true;
            try
            {
                // Stop at the first pending child. Its provider/context must stay alive.
                while (resources.Count != 0)
                {
                    int last = resources.Count - 1;
                    resources[last].Dispose();
                    resources.RemoveAt(last);
                }
                Released = true;
                Providers.Clear();
                failure = "";
                return true;
            }
            catch (Exception ex) { failure = ex.GetBaseException().Message; return false; }
            finally { Busy = false; cleanupChanged?.Invoke(this); }
        }
    }
}
