using System;
using System.IO;
using System.Linq;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class ExternalPlayerInputObservation
    {
        private readonly string token;
        private readonly string markerPath;
        private readonly ContinuousHomePageObservation gameplay;

        internal ExternalPlayerInputObservation(string token, string markerPath, double stableSeconds)
        {
            this.token = token ?? string.Empty;
            this.markerPath = markerPath ?? string.Empty;
            gameplay = new ContinuousHomePageObservation(stableSeconds);
        }

        internal bool Ready { get; private set; }
        internal bool MarkerObserved { get; private set; }
        internal bool MarkerPassed { get; private set; }

        internal bool ObserveReady(bool normalGameplay, DateTimeOffset now)
        {
            if (Ready)
                return true;
            Ready = gameplay.Observe(normalGameplay ? "HomePageUiState" : string.Empty, now);
            return Ready;
        }

        internal bool ObserveMarker()
        {
            if (MarkerObserved)
                return true;
            if (!File.Exists(markerPath))
                return false;
            string[] lines = File.ReadAllLines(markerPath)
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0)
                .ToArray();
            bool tokenMatches = lines.Any(item => item.Equals("Token=" + token, StringComparison.Ordinal));
            bool terminal = lines.Any(item => item.Equals("Terminal=True", StringComparison.OrdinalIgnoreCase));
            if (!tokenMatches || !terminal)
                throw new InvalidDataException("External player-input marker did not contain the matching token and terminal receipt.");
            MarkerPassed = lines.Any(item => item.Equals("Passed=True", StringComparison.OrdinalIgnoreCase));
            MarkerObserved = true;
            return true;
        }
    }
}
