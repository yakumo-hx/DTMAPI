using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal enum FishingMiniGameInputDecision
    {
        Release,
        HoldStable,
        TapBonus
    }

    internal sealed class FishingCompatibilityInputOverride
    {
        internal bool IsActive { get; private set; }
        internal object? GameHandle { get; private set; }
        internal FishingMiniGameInputDecision Decision { get; private set; }
        internal DateTimeOffset ExpiresAtUtc { get; private set; }

        internal void Set(object gameHandle, FishingMiniGameInputDecision decision, DateTimeOffset expiresAtUtc)
        {
            IsActive = true;
            GameHandle = gameHandle;
            Decision = decision;
            ExpiresAtUtc = expiresAtUtc;
        }

        internal void Clear()
        {
            IsActive = false;
            GameHandle = null;
            Decision = FishingMiniGameInputDecision.Release;
            ExpiresAtUtc = DateTimeOffset.MinValue;
        }
    }
}
