using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal enum ModOwnerLifecycleState
    {
        Entering,
        Active,
        Deactivating,
        InactiveRestartRequired,
        Shutdown,
    }

    internal enum ModLoadCheckpoint
    {
        AssemblyLoaded,
        InstanceCreated,
        ContextAttached,
        EntryReturned,
        LoadedRegistryPublished,
        InstancePublished,
        LoadedListPublished,
        BeforeCommit,
    }

    internal sealed class ModOwnerLifecycleCoordinator
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, ModOwnerLifecycleState> states =
            new Dictionary<string, ModOwnerLifecycleState>(StringComparer.OrdinalIgnoreCase);

        public ModOwnerLifecycleState BeginEntry(string ownerId)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                if (states.TryGetValue(ownerId, out ModOwnerLifecycleState existing))
                    throw new InvalidOperationException("Mod owner '" + ownerId + "' can't enter again in this process (state=" + existing + "). Restart is required.");

                states.Add(ownerId, ModOwnerLifecycleState.Entering);
                return ModOwnerLifecycleState.Entering;
            }
        }

        public void Activate(string ownerId)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                RequireStateNoLock(ownerId, ModOwnerLifecycleState.Entering);
                states[ownerId] = ModOwnerLifecycleState.Active;
            }
        }

        public bool BeginDeactivation(string ownerId)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state))
                    return false;
                if (state == ModOwnerLifecycleState.Deactivating)
                    return false;

                // Terminal owner states still accept another cleanup pass. This keeps
                // cleanup idempotent and lets a transient participant failure be retried
                // without ever allowing the assembly to enter again in this process.
                states[ownerId] = ModOwnerLifecycleState.Deactivating;
                return true;
            }
        }

        public void CompleteDeactivation(string ownerId, bool shutdown, bool restartRequired)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                RequireStateNoLock(ownerId, ModOwnerLifecycleState.Deactivating);
                if (shutdown)
                    states[ownerId] = ModOwnerLifecycleState.Shutdown;
                else if (restartRequired)
                    states[ownerId] = ModOwnerLifecycleState.InactiveRestartRequired;
                else
                    states.Remove(ownerId);
            }
        }

        public void CancelDeactivation(
            string ownerId,
            ModOwnerLifecycleState previousState)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                RequireStateNoLock(
                    ownerId,
                    ModOwnerLifecycleState.Deactivating);
                states[ownerId] = previousState;
            }
        }

        public void EnsureRegistrationAllowed(string ownerId)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state) ||
                    (state != ModOwnerLifecycleState.Entering && state != ModOwnerLifecycleState.Active))
                {
                    throw new InvalidOperationException(
                        "Mod owner '" + ownerId + "' is inactive; registrations require an Entering or Active owner. Restart the game before enabling it again.");
                }
            }
        }

        public bool IsActive(string ownerId)
        {
            lock (gate)
                return states.TryGetValue(ownerId, out ModOwnerLifecycleState state) && state == ModOwnerLifecycleState.Active;
        }

        public bool RequiresRestart(string ownerId)
        {
            lock (gate)
                return states.TryGetValue(ownerId, out ModOwnerLifecycleState state) && state == ModOwnerLifecycleState.InactiveRestartRequired;
        }

        public void RequireColdStart(string ownerId)
        {
            ValidateOwner(ownerId);
            lock (gate)
            {
                if (!states.ContainsKey(ownerId))
                    states.Add(ownerId, ModOwnerLifecycleState.InactiveRestartRequired);
            }
        }

        public bool TryGetState(string ownerId, out ModOwnerLifecycleState state)
        {
            lock (gate)
                return states.TryGetValue(ownerId, out state);
        }

        public int Count
        {
            get
            {
                lock (gate)
                    return states.Count;
            }
        }

        public IReadOnlyCollection<string> GetRestartRequiredOwners()
        {
            lock (gate)
            {
                return states
                    .Where(pair => pair.Value == ModOwnerLifecycleState.InactiveRestartRequired)
                    .Select(pair => pair.Key)
                    .ToArray();
            }
        }

        public IReadOnlyCollection<string> GetOwners()
        {
            lock (gate)
                return states.Keys.ToArray();
        }

        private void RequireStateNoLock(string ownerId, ModOwnerLifecycleState required)
        {
            if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state) || state != required)
                throw new InvalidOperationException("Mod owner '" + ownerId + "' must be " + required + " (current=" + (states.ContainsKey(ownerId) ? state.ToString() : "Unknown") + ").");
        }

        private static void ValidateOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }
    }
}
