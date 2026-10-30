using System;

namespace DTMAPI.StrongPlantingGun
{
    public sealed class StrongPlantingGunConfig
    {
        public double Range { get; set; } = DefaultRange;
        internal const int DefaultRange = 7, MaximumRange = 15;

        internal static int Normalize(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Truncate(value) != value)
                return DefaultRange;
            return value < DefaultRange ? DefaultRange : value > MaximumRange ? MaximumRange : (int)value;
        }
    }
}
