namespace DTMAPI.StrongPlantingGun
{
    internal sealed class StrongPlantingGunLogGate
    {
        private const int RepeatedObservationInterval = 128;
        private int observations;
        private string lastKind = string.Empty;

        internal bool ShouldLog(
            bool verbose,
            string kind,
            bool materialChange)
        {
            observations++;
            string current = kind ?? string.Empty;
            bool changed = current != lastKind;
            lastKind = current;
            return materialChange ||
                (verbose &&
                 (changed ||
                  observations == 1 ||
                  observations %
                      RepeatedObservationInterval == 0));
        }

        internal void Reset()
        {
            observations = 0;
            lastKind = string.Empty;
        }
    }
}
