namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private readonly struct FishingSmokeCaseResult
        {
            internal FishingSmokeCaseResult(string scope, FixtureAttemptResult attempt)
            {
                Scope = scope ?? string.Empty;
                Attempt = attempt;
            }

            internal string Scope { get; }
            internal FixtureAttemptResult Attempt { get; }
        }
    }
}
