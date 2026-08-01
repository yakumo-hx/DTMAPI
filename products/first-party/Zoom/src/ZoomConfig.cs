using System;
using System.Runtime.Serialization;

namespace DTMAPI.Zoom
{
    [DataContract]
    internal sealed class ZoomConfig
    {
        [DataMember]
        public bool Enabled { get; set; } = true;

        [DataMember]
        public double MaxViewScale { get; set; } = 4d;

        [DataMember]
        public double Step { get; set; } = 0.25d;

        [DataMember]
        public string IncreaseKey { get; set; } =
            "Equals, KeypadPlus";

        [DataMember]
        public string DecreaseKey { get; set; } =
            "Minus, KeypadMinus";

        [DataMember]
        public bool VerboseLogging { get; set; }

        internal ZoomConfig Copy() =>
            new ZoomConfig
            {
                Enabled = Enabled,
                MaxViewScale = Clamp(
                    MaxViewScale,
                    1d,
                    4d),
                Step = Clamp(
                    Step,
                    0.05d,
                    1d),
                IncreaseKey = IncreaseKey ?? string.Empty,
                DecreaseKey = DecreaseKey ?? string.Empty,
                VerboseLogging = VerboseLogging
            };

        private static double Clamp(
            double value,
            double min,
            double max)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return min;
            }
            return Math.Min(max, Math.Max(min, value));
        }
    }
}
