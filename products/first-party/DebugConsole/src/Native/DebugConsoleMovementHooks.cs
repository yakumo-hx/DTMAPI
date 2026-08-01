using System;
using System.Threading;

namespace DTMAPI.DebugConsole
{
    internal static class DebugConsoleMovementHooks
    {
        private static object? playerOwner;
        private static float multiplier = 1f;

        internal static void SetMultiplier(
            object? player,
            double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value < 0.5d ||
                value > 4d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "DebugConsole movement multiplier must be between 0.5 and 4.");
            }

            float requested = (float)value;
            if (Math.Abs(requested - 1f) < 0.0001f)
            {
                Volatile.Write(ref multiplier, 1f);
                Volatile.Write(ref playerOwner, null);
                return;
            }
            if (player == null)
            {
                throw new InvalidOperationException(
                    "The current player BodyController is unavailable.");
            }

            Volatile.Write(ref playerOwner, player);
            Volatile.Write(ref multiplier, requested);
        }

        internal static void Reset() => SetMultiplier(null, 1d);

        public static void MoveSpeedPostfix(
            object __instance,
            ref float __result)
        {
            float factor = Volatile.Read(ref multiplier);
            object? owner = Volatile.Read(ref playerOwner);
            if (Math.Abs(factor - 1f) >= 0.0001f &&
                ReferenceEquals(owner, __instance))
            {
                __result *= factor;
            }
        }

        internal static double MultiplierForTests =>
            Volatile.Read(ref multiplier);

        internal static object? PlayerOwnerForTests =>
            Volatile.Read(ref playerOwner);
    }
}
