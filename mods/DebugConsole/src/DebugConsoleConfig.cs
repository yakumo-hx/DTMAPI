using System;

namespace DTMAPI.DebugConsole
{
    public sealed class DebugConsoleConfig
    {
        public int CustomTimeMultiplier { get; set; } = 4;
        public int RightClickCount { get; set; } = 10;
        public int CropMaturityRadius { get; set; } = 25;

        internal static int Normalize(double value) =>
            double.IsNaN(value) || double.IsInfinity(value)
                ? 4 : (int)Math.Max(1, Math.Min(20, Math.Round(value)));

        internal static int NormalizeDispatchCount(double value) =>
            double.IsNaN(value) || double.IsInfinity(value)
                ? 10 : (int)Math.Max(1, Math.Min(100, Math.Round(value)));

        internal static int NormalizeCropRadius(double value) =>
            double.IsNaN(value) || double.IsInfinity(value)
                ? 25 : (int)Math.Max(5, Math.Min(100, Math.Round(value)));
    }
}
