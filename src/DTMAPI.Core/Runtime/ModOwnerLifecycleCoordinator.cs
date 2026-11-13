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

    internal sealed class ModOwnerLifecycleCoordinator
    {
        private readonly Dictionary<string, ModOwnerLifecycleState> states =
            new Dictionary<string, ModOwnerLifecycleState>(StringComparer.OrdinalIgnoreCase);

        public ModOwnerLifecycleState BeginEntry(string ownerId)
        {
            if (states.TryGetValue(ownerId, out ModOwnerLifecycleState existing))
                throw new InvalidOperationException("Mod owner '" + ownerId + "' can't enter again in this process (state=" + existing + "). Restart is required.");

            states.Add(ownerId, ModOwnerLifecycleState.Entering);
            return ModOwnerLifecycleState.Entering;
        }

        public void Activate(string ownerId)
        {
            RequireState(ownerId, ModOwnerLifecycleState.Entering);
            states[ownerId] = ModOwnerLifecycleState.Active;
        }

        public bool BeginDeactivation(string ownerId)
        {
            if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state))
                return false;
            if (state == ModOwnerLifecycleState.Deactivating)
                return false;

            // Terminal states accept another cleanup pass after a participant failure.
            states[ownerId] = ModOwnerLifecycleState.Deactivating;
            return true;
        }

        public void CompleteDeactivation(string ownerId, bool shutdown, bool restartRequired)
        {
            RequireState(ownerId, ModOwnerLifecycleState.Deactivating);
            if (shutdown)
                states[ownerId] = ModOwnerLifecycleState.Shutdown;
            else if (restartRequired)
                states[ownerId] = ModOwnerLifecycleState.InactiveRestartRequired;
            else
                states.Remove(ownerId);
        }

        public void CancelDeactivation(
            string ownerId,
            ModOwnerLifecycleState previousState)
        {
            RequireState(ownerId, ModOwnerLifecycleState.Deactivating);
            states[ownerId] = previousState;
        }

        public void EnsureRegistrationAllowed(string ownerId)
        {
            if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state) ||
                (state != ModOwnerLifecycleState.Entering && state != ModOwnerLifecycleState.Active))
            {
                throw new InvalidOperationException(
                    "Mod owner '" + ownerId + "' is inactive; registrations require an Entering or Active owner. Restart the game before enabling it again.");
            }
        }

        public bool IsActive(string ownerId)
        {
            return states.TryGetValue(ownerId, out ModOwnerLifecycleState state) && state == ModOwnerLifecycleState.Active;
        }

        public bool RequiresRestart(string ownerId)
        {
            return states.TryGetValue(ownerId, out ModOwnerLifecycleState state) && state == ModOwnerLifecycleState.InactiveRestartRequired;
        }

        public void RequireColdStart(string ownerId)
        {
            if (!states.ContainsKey(ownerId))
                states.Add(ownerId, ModOwnerLifecycleState.InactiveRestartRequired);
        }

        public bool TryGetState(string ownerId, out ModOwnerLifecycleState state)
        {
            return states.TryGetValue(ownerId, out state);
        }

        public int Count => states.Count;

        public IReadOnlyCollection<string> GetRestartRequiredOwners()
        {
            return states
                .Where(pair => pair.Value == ModOwnerLifecycleState.InactiveRestartRequired)
                .Select(pair => pair.Key)
                .ToArray();
        }

        public IReadOnlyCollection<string> GetOwners()
        {
            return states.Keys.ToArray();
        }

        private void RequireState(string ownerId, ModOwnerLifecycleState required)
        {
            if (!states.TryGetValue(ownerId, out ModOwnerLifecycleState state) || state != required)
                throw new InvalidOperationException("Mod owner '" + ownerId + "' must be " + required + " (current=" + (states.ContainsKey(ownerId) ? state.ToString() : "Unknown") + ").");
        }
    }
}
