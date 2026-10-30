namespace DTMAPI.ChestLocatorEnhancer
{
    internal sealed class ChestLocatorObservationLogGate
    {
        private bool hasObservation;
        private bool lastUseBox;
        private bool lastNativeAutoUseBox;
        private bool lastWidened;

        internal bool ShouldLog(
            bool useBox,
            bool nativeAutoUseBox,
            bool widened)
        {
            bool changed =
                !hasObservation ||
                lastUseBox != useBox ||
                lastNativeAutoUseBox !=
                    nativeAutoUseBox ||
                lastWidened != widened;
            hasObservation = true;
            lastUseBox = useBox;
            lastNativeAutoUseBox =
                nativeAutoUseBox;
            lastWidened = widened;
            return changed;
        }

        internal void Reset()
        {
            hasObservation = false;
            lastUseBox = false;
            lastNativeAutoUseBox = false;
            lastWidened = false;
        }
    }
}
