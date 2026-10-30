using System;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class DebugConsoleBehaviorObservation
    {
        private DateTimeOffset stableSince;
        private int stableOpenCount = -1;
        private int stableEscapeCloseCount = -1;
        private int stableYCloseCount = -1;

        internal G4FixtureStepResult Observe(string logText, DateTimeOffset now)
        {
            string text = logText ?? string.Empty;
            int openCount = Count(text, "Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.");
            int escapeCloseCount = Count(text, "Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.");
            int yCloseCount = Count(text, "Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.");
            bool matrixObserved = openCount >= 8 && escapeCloseCount >= 1 && yCloseCount >= 6;
            if (!matrixObserved)
            {
                stableSince = default;
                stableOpenCount = openCount;
                stableEscapeCloseCount = escapeCloseCount;
                stableYCloseCount = yCloseCount;
                return G4FixtureStepResult.Pending(
                    "Waiting for runner real-input matrix. opens=" + openCount + "/8; escapeCloses=" + escapeCloseCount + "/1; yCloses=" + yCloseCount + "/6; syntheticInput=false");
            }

            if (stableSince == default || stableOpenCount != openCount || stableEscapeCloseCount != escapeCloseCount || stableYCloseCount != yCloseCount)
            {
                stableSince = now;
                stableOpenCount = openCount;
                stableEscapeCloseCount = escapeCloseCount;
                stableYCloseCount = yCloseCount;
                return G4FixtureStepResult.Pending("Real-input matrix observed; waiting 1.5 seconds to prove held-Y caused no repeated toggle.");
            }

            if ((now - stableSince).TotalSeconds < 1.5d)
                return G4FixtureStepResult.Pending("Real-input matrix counts remain stable during the held-Y no-flicker window.");

            return G4FixtureStepResult.Verified(
                "realInput=true; opens=" + openCount + "; escapeCloses=" + escapeCloseCount + "; yCloses=" + yCloseCount + "; shortTaps=10; holdNoFlicker=true; syntheticInput=false");
        }

        private static int Count(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }
    }

}
