using System;
using System.Globalization;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal readonly struct FishingNativeMovementGateResult
    {
        internal FishingNativeMovementGateResult(bool armed, bool shouldCancel, bool deferActions, string reason)
        {
            Armed = armed;
            ShouldCancel = shouldCancel;
            DeferActions = deferActions;
            Reason = reason ?? string.Empty;
        }

        internal bool Armed { get; }
        internal bool ShouldCancel { get; }
        internal bool DeferActions { get; }
        internal string Reason { get; }
    }

    internal static class FishingNativeMovementPolicy
    {
        internal const float NativePreBaseVelocityThreshold = 0.001f;

        internal static FishingNativeMovementGateResult Evaluate(
            bool armed,
            bool nativeMovementAvailable,
            double inputMultiplier,
            double velocityX,
            double offsetX)
        {
            if (!nativeMovementAvailable)
                return new FishingNativeMovementGateResult(false, true, true, "native-movement-unavailable");
            if (double.IsNaN(inputMultiplier) || double.IsInfinity(inputMultiplier) ||
                double.IsNaN(velocityX) || double.IsInfinity(velocityX) ||
                double.IsNaN(offsetX) || double.IsInfinity(offsetX))
            {
                return new FishingNativeMovementGateResult(false, true, true, "native-movement-invalid");
            }

            string movementReason = string.Empty;
            if (inputMultiplier != 0d)
                movementReason = "manual-move inputMultiplier=" + inputMultiplier.ToString("R", CultureInfo.InvariantCulture);
            else if (Math.Abs(velocityX) > NativePreBaseVelocityThreshold)
                movementReason = "native-move VelocityX=" + velocityX.ToString("R", CultureInfo.InvariantCulture);
            else if (offsetX != 0d)
                movementReason = "native-offset OffsetX=" + offsetX.ToString("R", CultureInfo.InvariantCulture);

            if (movementReason.Length == 0)
            {
                return armed
                    ? new FishingNativeMovementGateResult(true, false, false, string.Empty)
                    : new FishingNativeMovementGateResult(true, false, false, "neutral-armed");
            }
            if (!armed)
                return new FishingNativeMovementGateResult(false, false, true, "neutral-arming " + movementReason);
            return new FishingNativeMovementGateResult(true, true, true, movementReason);
        }

        internal static bool ShouldCancel(
            bool nativeMovementAvailable,
            double inputMultiplier,
            double velocityX,
            double offsetX,
            out string reason)
        {
            FishingNativeMovementGateResult result = Evaluate(
                true,
                nativeMovementAvailable,
                inputMultiplier,
                velocityX,
                offsetX);
            reason = result.Reason;
            return result.ShouldCancel;
        }
    }
}
