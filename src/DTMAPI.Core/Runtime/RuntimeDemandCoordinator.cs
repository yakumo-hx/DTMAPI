using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Runtime
{
    internal enum RuntimeDemandSourceType
    {
        EventSubscription,
        CapabilityRegistration,
        CapabilityLease,
        CapabilitySession,
        CapabilityOperation,
        ContentDefinition,
        MandatoryFrameworkRepair,
        ExplicitQa
    }

    internal enum RuntimeDemandLifetime
    {
        Operation,
        Frame,
        Session,
        Save,
        Title,
        Owner,
        Process
    }

    internal enum RuntimeCapabilityLifecycleState
    {
        Cold,
        Activating,
        Active,
        Quiescing,
        Stopped,
        ProcessPinnedDormant,
        RestartRequired
    }

    internal enum RuntimeCapabilityPatchState
    {
        NotInstalled,
        Installing,
        Installed,
        ProcessPinned,
        Removing,
        Removed,
        Failed
    }

    internal enum RuntimeCapabilityRestartPolicy
    {
        None,
        RestartRequired
    }

    internal enum RuntimeCapabilityOutcome
    {
        DemandActivated,
        Mandatory,
        StoppedRemovable,
        ProcessPinnedDormant,
        RestartRequired,
        Deferred,
        Removed
    }

    internal sealed class RuntimeCapabilityDescriptor
    {
        public RuntimeCapabilityDescriptor(
            string capabilityId,
            RuntimeCapabilityOutcome outcome,
            string updaterCadence,
            string hookRouteId,
            string details)
        {
            CapabilityId = Normalize(capabilityId, nameof(capabilityId));
            Outcome = outcome;
            UpdaterCadence = SingleLine(updaterCadence, 64);
            HookRouteId = SingleLine(hookRouteId, 128);
            Details = SingleLine(details, 512);
        }

        public string CapabilityId { get; }

        public RuntimeCapabilityOutcome Outcome { get; }

        public string UpdaterCadence { get; }

        public string HookRouteId { get; }

        public string Details { get; }

        private static string Normalize(string value, string parameterName)
        {
            string result = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("A capability id is required.", parameterName);
            return result;
        }

        internal static string SingleLine(string value, int maxLength)
        {
            string result = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return result.Length <= maxLength ? result : result.Substring(0, maxLength);
        }
    }

    internal static class RuntimeDemandDiagnosticBounds
    {
        internal static string DisplayOwner(string value)
        {
            string scalar = value ?? string.Empty;
            return scalar.Length <= BoundedDiagnosticScalar.OwnerIdChars
                ? RuntimeCapabilityDescriptor.SingleLine(scalar, BoundedDiagnosticScalar.OwnerIdChars)
                : BoundedDiagnosticScalar.StableIdentity(scalar);
        }

        internal static string DisplayIdentifier(string value)
        {
            string scalar = value ?? string.Empty;
            return scalar.Length <= BoundedDiagnosticScalar.IdentifierChars
                ? RuntimeCapabilityDescriptor.SingleLine(scalar, BoundedDiagnosticScalar.IdentifierChars)
                : BoundedDiagnosticScalar.StableIdentity(scalar);
        }

        internal static string Summary(string value, int maxChars)
        {
            long trimmedBytes = 0;
            return BoundedDiagnosticScalar.Sanitize(value, maxChars, ref trimmedBytes);
        }
    }

    internal sealed class RuntimeDemandCoordinator
    {
        internal const int DefaultReceiptCapacity = 256;
        internal const int DefaultDiagnosticOwnerCapacity = 128;
        private readonly object gate = new object();
        private readonly int receiptCapacity;
        private readonly int diagnosticOwnerCapacity;
        private readonly Dictionary<string, CapabilityState> capabilities = new Dictionary<string, CapabilityState>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<RuntimeDemandReceipt> receipts = new Queue<RuntimeDemandReceipt>();
        private readonly Queue<RuntimeDemandTransition> pendingTransitions = new Queue<RuntimeDemandTransition>();
        private readonly RuntimeBoundarySlot<Action<string>> synchronousStateObservers = new RuntimeBoundarySlot<Action<string>>();
        private readonly RuntimeBoundarySlot<Action<RuntimeDemandTransition>> transitionListeners = new RuntimeBoundarySlot<Action<RuntimeDemandTransition>>();
        private long sequence;
        private long trimmedReceipts;
        private int demandEntryCount;
        private bool transitionDrainActive;

        public RuntimeDemandCoordinator(
            int receiptCapacity = DefaultReceiptCapacity,
            int diagnosticOwnerCapacity = DefaultDiagnosticOwnerCapacity)
        {
            if (receiptCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(receiptCapacity));
            if (diagnosticOwnerCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(diagnosticOwnerCapacity));
            this.receiptCapacity = receiptCapacity;
            this.diagnosticOwnerCapacity = diagnosticOwnerCapacity;
        }

        internal event Action<RuntimeDemandTransition>? DemandTransitioned
        {
            add => transitionListeners.Add(value);
            remove => transitionListeners.Remove(value);
        }

        /// <summary>
        /// A scalar-state projection boundary invoked under the coordinator gate after
        /// every authoritative mutation and before that mutation returns. Observers must
        /// be non-blocking and may perform read-only coordinator queries through the
        /// re-entrant monitor, but must not mutate demand; failures are isolated from demand state.
        /// Ordered public transition publication remains independently serialized by
        /// <see cref="DemandTransitioned"/>.
        /// </summary>
        internal event Action<string>? DemandStateChangedSynchronously
        {
            add => synchronousStateObservers.Add(value);
            remove => synchronousStateObservers.Remove(value);
        }

        internal void RegisterCapability(RuntimeCapabilityDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            lock (gate)
            {
                CapabilityState state = GetOrCreateCapability(descriptor.CapabilityId);
                if (state.Descriptor != null && !DescriptorsEqual(state.Descriptor, descriptor))
                    throw new InvalidOperationException("Capability '" + descriptor.CapabilityId + "' was registered with a different descriptor.");
                state.Descriptor = descriptor;
            }
        }

        internal bool SetDemand(
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            int count,
            string reason)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return ChangeDemand(capabilityId, ownerId, sourceType, lifetime, demandKey, count, absolute: true, reason: reason);
        }

        private bool ChangeDemand(
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            int value,
            bool absolute,
            string reason)
        {

            capabilityId = Required(capabilityId, nameof(capabilityId));
            ownerId = Required(ownerId, nameof(ownerId));
            demandKey = Required(demandKey, nameof(demandKey));
            var key = new RuntimeDemandKey(ownerId, sourceType, lifetime, demandKey);
            RuntimeDemandTransition? transition = null;
            bool drainTransitions = false;
            bool changed = false;

            lock (gate)
            {
                if (!absolute && value < 0 && !capabilities.TryGetValue(capabilityId, out CapabilityState state))
                    return false;
                state = GetOrCreateCapability(capabilityId);
                int previousCapabilityCount = state.TotalDemand;
                int previousCount = state.Demands.TryGetValue(key, out int existing) ? existing : 0;
                int count = absolute ? value : checked(previousCount + value);
                if (count < 0)
                    return false;
                if (previousCount == count)
                    return false;

                if (count == 0)
                {
                    state.Demands.Remove(key);
                    if (previousCount > 0)
                        demandEntryCount--;
                }
                else
                {
                    state.Demands[key] = count;
                    if (previousCount == 0)
                        demandEntryCount++;
                }
                state.TotalDemand += count - previousCount;
                state.AdjustOwnerDemand(ownerId, count - previousCount);
                if (state.TotalDemand < 0)
                    throw new InvalidOperationException("Demand count underflow for capability '" + capabilityId + "'.");

                changed = true;
                long receiptSequence = ++sequence;
                AddReceiptUnsafe(new RuntimeDemandReceipt(
                    receiptSequence,
                    DateTimeOffset.UtcNow,
                    capabilityId,
                    ownerId,
                    sourceType,
                    lifetime,
                    demandKey,
                    previousCapabilityCount,
                    state.TotalDemand,
                    count == 0 ? "Released" : (previousCount == 0 ? "Acquired" : "Changed"),
                    reason));
                NotifySynchronousStateObserversUnsafe(capabilityId);
                if ((previousCapabilityCount == 0) != (state.TotalDemand == 0))
                {
                    transition = new RuntimeDemandTransition(
                        receiptSequence,
                        capabilityId,
                        previousCapabilityCount,
                        state.TotalDemand,
                        ownerId,
                        sourceType,
                        lifetime,
                        demandKey,
                        reason);
                    drainTransitions = QueueTransitionUnsafe(transition);
                }
            }

            if (drainTransitions)
                DrainTransitions();
            return changed;
        }

        internal bool AcquireDemand(
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            string reason)
        {
            return ChangeDemand(capabilityId, ownerId, sourceType, lifetime, demandKey, 1, absolute: false, reason: reason);
        }

        internal bool ReleaseDemand(
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            string reason)
        {
            return ChangeDemand(capabilityId, ownerId, sourceType, lifetime, demandKey, -1, absolute: false, reason: reason);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            return RemoveOwner(ownerId, capabilityScope: null, reason: reason);
        }

        internal int RemoveOwnerDemandsForCapabilities(
            string ownerId,
            IReadOnlyCollection<string> capabilityIds,
            string reason)
        {
            return RemoveOwner(ownerId, NormalizeCapabilityScope(capabilityIds), reason);
        }

        private int RemoveOwner(string ownerId, HashSet<string>? capabilityScope, string reason)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;
            ownerId = ownerId.Trim();
            bool drainTransitions = false;
            int removed = 0;

            lock (gate)
            {
                foreach (KeyValuePair<string, CapabilityState> capability in capabilities)
                {
                    if (capabilityScope != null && !capabilityScope.Contains(capability.Key))
                        continue;

                    CapabilityState state = capability.Value;
                    RuntimeDemandKey[] owned = state.Demands.Keys.Where(key => key.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (owned.Length == 0)
                        continue;

                    int previousCapabilityCount = state.TotalDemand;
                    foreach (RuntimeDemandKey key in owned)
                    {
                        int count = state.Demands[key];
                        state.Demands.Remove(key);
                        state.AdjustOwnerDemand(key.OwnerId, -count);
                        demandEntryCount--;
                        state.TotalDemand -= count;
                        removed += count;
                        long receiptSequence = ++sequence;
                        AddReceiptUnsafe(new RuntimeDemandReceipt(
                            receiptSequence,
                            DateTimeOffset.UtcNow,
                            capability.Key,
                            ownerId,
                            key.SourceType,
                            key.Lifetime,
                            key.DemandKey,
                            previousCapabilityCount,
                            state.TotalDemand,
                            "OwnerRemoved",
                            reason));
                    }

                    NotifySynchronousStateObserversUnsafe(capability.Key);

                    if (previousCapabilityCount > 0 && state.TotalDemand == 0)
                    {
                        drainTransitions |= QueueTransitionUnsafe(new RuntimeDemandTransition(
                            sequence,
                            capability.Key,
                            previousCapabilityCount,
                            0,
                            ownerId,
                            RuntimeDemandSourceType.CapabilityRegistration,
                            RuntimeDemandLifetime.Owner,
                            "owner-cleanup",
                            reason));
                    }
                }
            }

            if (drainTransitions)
                DrainTransitions();
            return removed;
        }

        internal int RemoveCapabilityDemands(string capabilityId, string reason)
        {
            if (string.IsNullOrWhiteSpace(capabilityId))
                return 0;

            capabilityId = capabilityId.Trim();
            RuntimeDemandTransition? transition = null;
            bool drainTransitions = false;
            int removed = 0;
            lock (gate)
            {
                if (!capabilities.TryGetValue(capabilityId, out CapabilityState state) || state.Demands.Count == 0)
                    return 0;

                int previousCapabilityCount = state.TotalDemand;
                int removedEntries = state.Demands.Count;
                foreach (KeyValuePair<RuntimeDemandKey, int> demand in state.Demands)
                {
                    removed += demand.Value;
                    AddReceiptUnsafe(new RuntimeDemandReceipt(
                        ++sequence,
                        DateTimeOffset.UtcNow,
                        capabilityId,
                        demand.Key.OwnerId,
                        demand.Key.SourceType,
                        demand.Key.Lifetime,
                        demand.Key.DemandKey,
                        previousCapabilityCount,
                        previousCapabilityCount - removed,
                        "CapabilityCleared",
                        reason));
                }

                state.Demands.Clear();
                state.OwnerDemandTotals.Clear();
                state.TotalDemand = 0;
                demandEntryCount -= removedEntries;
                NotifySynchronousStateObserversUnsafe(capabilityId);
                transition = new RuntimeDemandTransition(
                    sequence,
                    capabilityId,
                    previousCapabilityCount,
                    0,
                    "DTMAPI.Runtime",
                    RuntimeDemandSourceType.CapabilityOperation,
                    RuntimeDemandLifetime.Process,
                    "capability-clear",
                    reason);
                drainTransitions = QueueTransitionUnsafe(transition);
            }

            if (drainTransitions)
                DrainTransitions();
            return removed;
        }

        internal bool HasDemand(string capabilityId)
        {
            if (string.IsNullOrWhiteSpace(capabilityId))
                return false;
            lock (gate)
                return capabilities.TryGetValue(capabilityId, out CapabilityState state) && state.TotalDemand > 0;
        }

        internal int GetDemandCount(string capabilityId)
        {
            if (string.IsNullOrWhiteSpace(capabilityId))
                return 0;
            lock (gate)
                return capabilities.TryGetValue(capabilityId, out CapabilityState state) ? state.TotalDemand : 0;
        }

        internal int GetOwnerDemandCount(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;
            ownerId = ownerId.Trim();
            lock (gate)
            {
                int total = 0;
                foreach (CapabilityState state in capabilities.Values)
                {
                    foreach (KeyValuePair<RuntimeDemandKey, int> demand in state.Demands)
                    {
                        if (demand.Key.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                            total = checked(total + demand.Value);
                    }
                }
                return total;
            }
        }

        internal int GetOwnerDemandCountForCapabilities(
            string ownerId,
            IReadOnlyCollection<string> capabilityIds)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;

            ownerId = ownerId.Trim();
            HashSet<string> capabilityScope = NormalizeCapabilityScope(capabilityIds);
            lock (gate)
            {
                int total = 0;
                foreach (string capabilityId in capabilityScope)
                {
                    if (capabilities.TryGetValue(capabilityId, out CapabilityState state) &&
                        state.OwnerDemandTotals.TryGetValue(ownerId, out int ownerDemand))
                    {
                        total = checked(total + ownerDemand);
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Returns whether one authoritative owner currently holds both a parent
        /// capability root and the named product-owned child root. This keeps shared
        /// physical hook demand from being mistaken for another product's logical
        /// callback closure.
        /// </summary>
        internal bool HasCoOwnedDemand(
            string parentCapabilityId,
            string childCapabilityId,
            string childDemandKey)
        {
            if (string.IsNullOrWhiteSpace(parentCapabilityId) ||
                string.IsNullOrWhiteSpace(childCapabilityId) ||
                string.IsNullOrWhiteSpace(childDemandKey))
                return false;

            lock (gate)
            {
                if (!capabilities.TryGetValue(parentCapabilityId, out CapabilityState parent) ||
                    parent.TotalDemand <= 0 ||
                    !capabilities.TryGetValue(childCapabilityId, out CapabilityState child) ||
                    child.TotalDemand <= 0)
                    return false;

                foreach (KeyValuePair<RuntimeDemandKey, int> demand in child.Demands)
                {
                    if (demand.Value > 0 &&
                        demand.Key.DemandKey.Equals(childDemandKey, StringComparison.OrdinalIgnoreCase) &&
                        parent.OwnerDemandTotals.TryGetValue(demand.Key.OwnerId, out int parentDemand) &&
                        parentDemand > 0)
                        return true;
                }

                return false;
            }
        }

        internal int DemandEntryCount
        {
            get
            {
                lock (gate)
                    return demandEntryCount;
            }
        }

        internal void SetRouteState(
            string capabilityId,
            RuntimeCapabilityLifecycleState lifecycleState,
            RuntimeCapabilityPatchState patchState,
            RuntimeCapabilityRestartPolicy restartPolicy,
            bool updaterActive,
            string reason)
        {
            capabilityId = Required(capabilityId, nameof(capabilityId));
            lock (gate)
            {
                CapabilityState state = GetOrCreateCapability(capabilityId);
                if (state.LifecycleState == lifecycleState &&
                    state.PatchState == patchState &&
                    state.RestartPolicy == restartPolicy &&
                    state.UpdaterActive == updaterActive)
                    return;

                state.LifecycleState = lifecycleState;
                state.PatchState = patchState;
                state.RestartPolicy = restartPolicy;
                state.UpdaterActive = updaterActive;
                AddReceiptUnsafe(new RuntimeDemandReceipt(
                    ++sequence,
                    DateTimeOffset.UtcNow,
                    capabilityId,
                    "DTMAPI.Runtime",
                    RuntimeDemandSourceType.MandatoryFrameworkRepair,
                    RuntimeDemandLifetime.Process,
                    "route-state",
                    state.TotalDemand,
                    state.TotalDemand,
                    "RouteState",
                    lifecycleState + "/" + patchState + "/" + restartPolicy + "/updater=" + updaterActive.ToString(CultureInfo.InvariantCulture) + ": " + reason));
            }
        }

        internal RuntimeDemandSnapshot GetSnapshot()
        {
            lock (gate)
            {
                RuntimeCapabilityDemandSnapshot[] capabilitySnapshots = capabilities
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => pair.Value.ToSnapshot(pair.Key, diagnosticOwnerCapacity))
                    .ToArray();
                return new RuntimeDemandSnapshot(
                    diagnosticOwnerCapacity,
                    receiptCapacity,
                    demandEntryCount,
                    capabilitySnapshots.Sum(item => item.TotalDemand),
                    capabilitySnapshots.Count(item => item.UpdaterActive),
                    trimmedReceipts,
                    capabilitySnapshots,
                    receipts.ToArray());
            }
        }

        private CapabilityState GetOrCreateCapability(string capabilityId)
        {
            if (!capabilities.TryGetValue(capabilityId, out CapabilityState state))
            {
                state = new CapabilityState();
                capabilities.Add(capabilityId, state);
            }
            return state;
        }

        private static HashSet<string> NormalizeCapabilityScope(IReadOnlyCollection<string> capabilityIds)
        {
            if (capabilityIds == null)
                throw new ArgumentNullException(nameof(capabilityIds));

            var scope = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string capabilityId in capabilityIds)
            {
                if (string.IsNullOrWhiteSpace(capabilityId))
                    throw new ArgumentException("Capability scopes cannot contain an empty capability id.", nameof(capabilityIds));
                scope.Add(capabilityId.Trim());
            }
            return scope;
        }

        private void AddReceiptUnsafe(RuntimeDemandReceipt receipt)
        {
            while (receipts.Count >= receiptCapacity)
            {
                receipts.Dequeue();
                trimmedReceipts++;
            }
            receipts.Enqueue(receipt);
        }

        private void NotifySynchronousStateObserversUnsafe(string capabilityId)
        {
            foreach (Action<string> observer in synchronousStateObservers.Snapshot)
            {
                try
                {
                    observer(capabilityId);
                }
                catch
                {
                    // The authoritative mutation must never be rolled back or exposed as
                    // failed because an internal scalar projection could not refresh.
                }
            }
        }

        private bool QueueTransitionUnsafe(RuntimeDemandTransition transition)
        {
            pendingTransitions.Enqueue(transition);
            if (transitionDrainActive)
                return false;

            transitionDrainActive = true;
            return true;
        }

        private void DrainTransitions()
        {
            while (true)
            {
                RuntimeDemandTransition transition;
                lock (gate)
                {
                    if (pendingTransitions.Count == 0)
                    {
                        transitionDrainActive = false;
                        return;
                    }
                    transition = pendingTransitions.Dequeue();
                }

                foreach (Action<RuntimeDemandTransition> handler in transitionListeners.Snapshot)
                {
                    try
                    {
                        handler(transition);
                    }
                    catch
                    {
                        // Demand state is authoritative. Subscribers diagnose their own failed activation.
                    }
                }
            }
        }

        private static bool DescriptorsEqual(RuntimeCapabilityDescriptor left, RuntimeCapabilityDescriptor right)
        {
            return left.CapabilityId.Equals(right.CapabilityId, StringComparison.OrdinalIgnoreCase) &&
                left.Outcome == right.Outcome &&
                left.UpdaterCadence.Equals(right.UpdaterCadence, StringComparison.Ordinal) &&
                left.HookRouteId.Equals(right.HookRouteId, StringComparison.Ordinal) &&
                left.Details.Equals(right.Details, StringComparison.Ordinal);
        }

        private static string Required(string value, string parameterName)
        {
            string result = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("A non-empty value is required.", parameterName);
            return result;
        }

        private sealed class CapabilityState
        {
            public RuntimeCapabilityDescriptor? Descriptor { get; set; }

            public Dictionary<RuntimeDemandKey, int> Demands { get; } = new Dictionary<RuntimeDemandKey, int>();

            public Dictionary<string, int> OwnerDemandTotals { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public int TotalDemand { get; set; }

            public RuntimeCapabilityLifecycleState LifecycleState { get; set; } = RuntimeCapabilityLifecycleState.Cold;

            public RuntimeCapabilityPatchState PatchState { get; set; } = RuntimeCapabilityPatchState.NotInstalled;

            public RuntimeCapabilityRestartPolicy RestartPolicy { get; set; } = RuntimeCapabilityRestartPolicy.None;

            public bool UpdaterActive { get; set; }

            public RuntimeCapabilityDemandSnapshot ToSnapshot(string capabilityId, int diagnosticOwnerCapacity)
            {
                var bySource = new Dictionary<RuntimeDemandSourceType, int>();
                var byOwner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<RuntimeDemandKey, int> demand in Demands)
                {
                    bySource[demand.Key.SourceType] = (bySource.TryGetValue(demand.Key.SourceType, out int sourceCount) ? sourceCount : 0) + demand.Value;
                }

                int namedOwnerDemand = 0;
                int projectedOwnerCount = 0;
                foreach (KeyValuePair<string, int> owner in OwnerDemandTotals)
                {
                    if (projectedOwnerCount >= diagnosticOwnerCapacity)
                        break;
                    string displayOwner = RuntimeDemandDiagnosticBounds.DisplayOwner(owner.Key);
                    byOwner[displayOwner] = (byOwner.TryGetValue(displayOwner, out int current) ? current : 0) + owner.Value;
                    namedOwnerDemand += owner.Value;
                    projectedOwnerCount++;
                }
                int trimmedOwnerCount = Math.Max(0, OwnerDemandTotals.Count - projectedOwnerCount);
                int trimmedOwnerDemand = Math.Max(0, TotalDemand - namedOwnerDemand);

                return new RuntimeCapabilityDemandSnapshot(
                    capabilityId,
                    Descriptor,
                    TotalDemand,
                    Demands.Count,
                    LifecycleState,
                    PatchState,
                    RestartPolicy,
                    UpdaterActive,
                    bySource,
                    byOwner,
                    trimmedOwnerCount,
                    trimmedOwnerDemand);
            }

            public void AdjustOwnerDemand(string ownerId, int delta)
            {
                int previous = OwnerDemandTotals.TryGetValue(ownerId, out int count) ? count : 0;
                int current = checked(previous + delta);
                if (current < 0)
                    throw new InvalidOperationException("Owner demand count underflow for '" + ownerId + "'.");
                if (current == 0)
                    OwnerDemandTotals.Remove(ownerId);
                else
                    OwnerDemandTotals[ownerId] = current;
            }
        }
    }

    internal sealed class RuntimeDemandTransition
    {
        public RuntimeDemandTransition(
            long sequence,
            string capabilityId,
            int previousCount,
            int currentCount,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            string reason)
        {
            Sequence = sequence;
            CapabilityId = capabilityId ?? string.Empty;
            PreviousCount = previousCount;
            CurrentCount = currentCount;
            OwnerId = ownerId ?? string.Empty;
            SourceType = sourceType;
            Lifetime = lifetime;
            DemandKey = demandKey ?? string.Empty;
            Reason = RuntimeCapabilityDescriptor.SingleLine(reason, 512);
        }

        public long Sequence { get; }

        public string CapabilityId { get; }

        public int PreviousCount { get; }

        public int CurrentCount { get; }

        public string OwnerId { get; }

        public RuntimeDemandSourceType SourceType { get; }

        public RuntimeDemandLifetime Lifetime { get; }

        public string DemandKey { get; }

        public string Reason { get; }

        public bool IsFirstDemand => PreviousCount == 0 && CurrentCount > 0;

        public bool IsLastRelease => PreviousCount > 0 && CurrentCount == 0;
    }

    internal sealed class RuntimeDemandReceipt
    {
        public RuntimeDemandReceipt(
            long sequence,
            DateTimeOffset recordedAtUtc,
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            int previousCapabilityCount,
            int currentCapabilityCount,
            string action,
            string reason)
        {
            Sequence = sequence;
            RecordedAtUtc = recordedAtUtc;
            CapabilityId = RuntimeCapabilityDescriptor.SingleLine(capabilityId, 128);
            OwnerId = RuntimeDemandDiagnosticBounds.DisplayOwner(ownerId);
            SourceType = sourceType;
            Lifetime = lifetime;
            DemandKey = RuntimeCapabilityDescriptor.SingleLine(demandKey, 128);
            PreviousCapabilityCount = previousCapabilityCount;
            CurrentCapabilityCount = currentCapabilityCount;
            Action = RuntimeCapabilityDescriptor.SingleLine(action, 64);
            Reason = RuntimeCapabilityDescriptor.SingleLine(reason, 512);
        }

        public long Sequence { get; }
        public DateTimeOffset RecordedAtUtc { get; }
        public string CapabilityId { get; }
        public string OwnerId { get; }
        public RuntimeDemandSourceType SourceType { get; }
        public RuntimeDemandLifetime Lifetime { get; }
        public string DemandKey { get; }
        public int PreviousCapabilityCount { get; }
        public int CurrentCapabilityCount { get; }
        public string Action { get; }
        public string Reason { get; }

        public string FormatSummary()
        {
            return "seq=" + Sequence.ToString(CultureInfo.InvariantCulture) +
                ", capability=" + CapabilityId +
                ", owner=" + OwnerId +
                ", source=" + SourceType +
                ", lifetime=" + Lifetime +
                ", key=" + DemandKey +
                ", count=" + PreviousCapabilityCount.ToString(CultureInfo.InvariantCulture) + "->" + CurrentCapabilityCount.ToString(CultureInfo.InvariantCulture) +
                ", action=" + Action +
                ", reason=" + Reason;
        }
    }

    internal sealed class RuntimeCapabilityDemandSnapshot
    {
        private const int MaxSummaryOwnerSamples = 16;
        internal const int MaxFormatSummaryChars = 4096;

        public RuntimeCapabilityDemandSnapshot(
            string capabilityId,
            RuntimeCapabilityDescriptor? descriptor,
            int totalDemand,
            int demandEntryCount,
            RuntimeCapabilityLifecycleState lifecycleState,
            RuntimeCapabilityPatchState patchState,
            RuntimeCapabilityRestartPolicy restartPolicy,
            bool updaterActive,
            IReadOnlyDictionary<RuntimeDemandSourceType, int> bySource,
            IReadOnlyDictionary<string, int> byOwner,
            int trimmedOwnerCount,
            int trimmedOwnerDemand)
        {
            CapabilityId = capabilityId ?? string.Empty;
            Descriptor = descriptor;
            TotalDemand = totalDemand;
            DemandEntryCount = demandEntryCount;
            LifecycleState = lifecycleState;
            PatchState = patchState;
            RestartPolicy = restartPolicy;
            UpdaterActive = updaterActive;
            BySource = bySource;
            ByOwner = byOwner;
            TrimmedOwnerCount = trimmedOwnerCount;
            TrimmedOwnerDemand = trimmedOwnerDemand;
        }

        public string CapabilityId { get; }
        public RuntimeCapabilityDescriptor? Descriptor { get; }
        public int TotalDemand { get; }
        public int DemandEntryCount { get; }
        public RuntimeCapabilityLifecycleState LifecycleState { get; }
        public RuntimeCapabilityPatchState PatchState { get; }
        public RuntimeCapabilityRestartPolicy RestartPolicy { get; }
        public bool UpdaterActive { get; }
        public IReadOnlyDictionary<RuntimeDemandSourceType, int> BySource { get; }
        public IReadOnlyDictionary<string, int> ByOwner { get; }
        public int TrimmedOwnerCount { get; }
        public int TrimmedOwnerDemand { get; }

        public string FormatSummary()
        {
            string ownerSamples = string.Join(",", ByOwner
                .Take(MaxSummaryOwnerSamples)
                .Select(pair => pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture))
                .ToArray());
            int omittedFromSummary = Math.Max(0, ByOwner.Count - MaxSummaryOwnerSamples) + TrimmedOwnerCount;
            return RuntimeDemandDiagnosticBounds.Summary(
                RuntimeDemandDiagnosticBounds.DisplayIdentifier(CapabilityId) + "={demand=" + TotalDemand.ToString(CultureInfo.InvariantCulture) +
                "; entries=" + DemandEntryCount.ToString(CultureInfo.InvariantCulture) +
                "; lifecycle=" + LifecycleState +
                "; patch=" + PatchState +
                "; restart=" + RestartPolicy +
                "; updater=" + UpdaterActive.ToString(CultureInfo.InvariantCulture) +
                "; owners=" + ByOwner.Count.ToString(CultureInfo.InvariantCulture) +
                "; trimmedOwners=" + TrimmedOwnerCount.ToString(CultureInfo.InvariantCulture) +
                "; trimmedOwnerDemand=" + TrimmedOwnerDemand.ToString(CultureInfo.InvariantCulture) +
                "; ownerSampleOmitted=" + omittedFromSummary.ToString(CultureInfo.InvariantCulture) +
                "; outcome=" + (Descriptor?.Outcome.ToString() ?? "Unclassified") +
                "; ownerSample={" + (ownerSamples.Length == 0 ? "none" : ownerSamples) + "}}",
                MaxFormatSummaryChars);
        }
    }

    internal sealed class RuntimeDemandSnapshot
    {
        private const int MaxSummaryReceiptSamples = 16;
        private const int MaxSummaryRouteSamples = 128;
        internal const int MaxFormatSummaryChars = 32 * 1024;

        public RuntimeDemandSnapshot(
            int diagnosticOwnerCapacity,
            int receiptCapacity,
            int demandEntryCount,
            int totalDemand,
            int activeUpdaterCount,
            long trimmedReceipts,
            IReadOnlyList<RuntimeCapabilityDemandSnapshot> capabilities,
            IReadOnlyList<RuntimeDemandReceipt> receipts)
        {
            DiagnosticOwnerCapacity = diagnosticOwnerCapacity;
            ReceiptCapacity = receiptCapacity;
            DemandEntryCount = demandEntryCount;
            TotalDemand = totalDemand;
            ActiveUpdaterCount = activeUpdaterCount;
            TrimmedReceipts = trimmedReceipts;
            Capabilities = capabilities;
            Receipts = receipts;
        }

        public int DiagnosticOwnerCapacity { get; }
        public int ReceiptCapacity { get; }
        public int DemandEntryCount { get; }
        public int TotalDemand { get; }
        public int ActiveUpdaterCount { get; }
        public long TrimmedReceipts { get; }
        public IReadOnlyList<RuntimeCapabilityDemandSnapshot> Capabilities { get; }
        public IReadOnlyList<RuntimeDemandReceipt> Receipts { get; }
        public int TrimmedOwnerCount => Capabilities.Sum(item => item.TrimmedOwnerCount);
        public int TrimmedOwnerDemand => Capabilities.Sum(item => item.TrimmedOwnerDemand);

        public string FormatSummary()
        {
            RuntimeCapabilityDemandSnapshot[] routeSamples = Capabilities.Take(MaxSummaryRouteSamples).ToArray();
            string routes = string.Join("; ", routeSamples.Select(item => item.FormatSummary()).ToArray());
            int receiptStart = Math.Max(0, Receipts.Count - MaxSummaryReceiptSamples);
            string receiptSamples = string.Join(" || ", Receipts
                .Skip(receiptStart)
                .Take(MaxSummaryReceiptSamples)
                .Select(item => item.FormatSummary())
                .ToArray());
            return RuntimeDemandDiagnosticBounds.Summary(
                "status=" + (TrimmedOwnerCount == 0 ? "ok" : "bounded-detail") +
                "; demand=" + TotalDemand.ToString(CultureInfo.InvariantCulture) +
                "; entries=" + DemandEntryCount.ToString(CultureInfo.InvariantCulture) +
                "; activeUpdaters=" + ActiveUpdaterCount.ToString(CultureInfo.InvariantCulture) +
                "; diagnosticOwnersPerRoute=" + DiagnosticOwnerCapacity.ToString(CultureInfo.InvariantCulture) +
                "; trimmedOwners=" + TrimmedOwnerCount.ToString(CultureInfo.InvariantCulture) +
                "; trimmedOwnerDemand=" + TrimmedOwnerDemand.ToString(CultureInfo.InvariantCulture) +
                "; receipts=" + Receipts.Count.ToString(CultureInfo.InvariantCulture) + "/" + ReceiptCapacity.ToString(CultureInfo.InvariantCulture) +
                "; trimmedReceipts=" + TrimmedReceipts.ToString(CultureInfo.InvariantCulture) +
                "; receiptSample={" + (receiptSamples.Length == 0 ? "none" : receiptSamples) + "}" +
                "; receiptSampleOmitted=" + Math.Max(0, Receipts.Count - MaxSummaryReceiptSamples).ToString(CultureInfo.InvariantCulture) +
                "; routeSamples=" + routeSamples.Length.ToString(CultureInfo.InvariantCulture) +
                "; routeSampleOmitted=" + Math.Max(0, Capabilities.Count - routeSamples.Length).ToString(CultureInfo.InvariantCulture) +
                "; routes={" + (string.IsNullOrWhiteSpace(routes) ? "none" : routes) + "}",
                MaxFormatSummaryChars);
        }
    }

    internal sealed class RuntimeDemandKey : IEquatable<RuntimeDemandKey>
    {
        public RuntimeDemandKey(string ownerId, RuntimeDemandSourceType sourceType, RuntimeDemandLifetime lifetime, string demandKey)
        {
            OwnerId = ownerId ?? string.Empty;
            SourceType = sourceType;
            Lifetime = lifetime;
            DemandKey = demandKey ?? string.Empty;
        }

        public string OwnerId { get; }
        public RuntimeDemandSourceType SourceType { get; }
        public RuntimeDemandLifetime Lifetime { get; }
        public string DemandKey { get; }

        public bool Equals(RuntimeDemandKey? other)
        {
            return other != null &&
                SourceType == other.SourceType &&
                Lifetime == other.Lifetime &&
                OwnerId.Equals(other.OwnerId, StringComparison.OrdinalIgnoreCase) &&
                DemandKey.Equals(other.DemandKey, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as RuntimeDemandKey);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerId);
                hash = (hash * 397) ^ (int)SourceType;
                hash = (hash * 397) ^ (int)Lifetime;
                hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(DemandKey);
                return hash;
            }
        }
    }
}
