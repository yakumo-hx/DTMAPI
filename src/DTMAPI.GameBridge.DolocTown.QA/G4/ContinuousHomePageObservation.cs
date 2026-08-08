using System;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class ContinuousHomePageObservation
    {
        private readonly TimeSpan required;
        private DateTimeOffset observedAt;

        internal ContinuousHomePageObservation(double requiredSeconds)
        {
            required = TimeSpan.FromSeconds(requiredSeconds);
        }

        internal bool Terminal { get; private set; }

        internal DateTimeOffset ObservedAt => observedAt;

        internal bool Observe(string inputContext, DateTimeOffset now)
        {
            if (Terminal)
                return true;
            if (!string.Equals(inputContext, "HomePageUiState", StringComparison.OrdinalIgnoreCase))
            {
                observedAt = DateTimeOffset.MinValue;
                return false;
            }
            if (observedAt == DateTimeOffset.MinValue)
                observedAt = now;
            Terminal = now - observedAt >= required;
            return Terminal;
        }
    }
}
