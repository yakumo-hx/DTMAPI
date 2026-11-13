using System;
using System.Runtime.Serialization;
namespace DTMAPI.Zoom
{
    [DataContract]
    internal sealed class ZoomConfig
    {
        [DataMember] public double MaxViewScale { get; set; } = 4d;
        [DataMember] public double Step { get; set; } = 0.5d;
        [DataMember] public string IncreaseKey { get; set; } = "Equals, KeypadPlus";
        [DataMember] public string DecreaseKey { get; set; } = "Minus, KeypadMinus";
        [DataMember] public string MaximumKey { get; set; } = "Shift+Equals, Shift+KeypadPlus";
        [DataMember] public string ResetKey { get; set; } = "Shift+Minus, Shift+KeypadMinus";
        [DataMember] public bool VerboseLogging { get; set; }
        [OnDeserializing]
        private void BeforeRead(StreamingContext context)
        {
            // DataContract construction bypasses property initializers. Missing
            // fields get defaults; an explicitly empty saved binding stays empty.
            MaxViewScale = 4d; Step = 0.5d;
            IncreaseKey = "Equals, KeypadPlus"; DecreaseKey = "Minus, KeypadMinus";
            MaximumKey = "Shift+Equals, Shift+KeypadPlus"; ResetKey = "Shift+Minus, Shift+KeypadMinus";
        }
        internal ZoomConfig Copy() => new ZoomConfig
        {
            MaxViewScale = Clamp(MaxViewScale, 1d, 10d), Step = Clamp(Step, 0.05d, 2d),
            IncreaseKey = IncreaseKey ?? string.Empty, DecreaseKey = DecreaseKey ?? string.Empty,
            MaximumKey = MaximumKey ?? string.Empty, ResetKey = ResetKey ?? string.Empty, VerboseLogging = VerboseLogging
        };
        private static double Clamp(double value, double min, double max) =>
            double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
    }
}
