using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Modules.Audio
{
    // Loader fixture only; native constructor behavior is checked against the game separately.
    public sealed class CdUnlockProtection : IDisposable
    {
        public static int Active;
        public CdUnlockProtection(IDtmHelper helper, Action<Action> authorize) => authorize(() => Active++);
        public void Dispose() => Active--;
    }

    public sealed class NativeModule
    {
        public NativeModule(IDtmHelper helper) => throw new InvalidOperationException("Playback was not requested.");
    }
}
