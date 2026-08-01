using System;
using System.Runtime.Serialization;

namespace DTMAPI.Mine
{
    [DataContract]
    public sealed class MineConfig
    {
        [DataMember]
        public bool Enabled { get; set; } = true;

        [DataMember]
        public bool UseOilRecipeReplacement { get; set; } = true;

        [DataMember]
        public int CycleMinutes { get; set; } = 120;

        [DataMember]
        public double CoalWeight { get; set; } = 18d;

        [DataMember]
        public double CopperOreWeight { get; set; } = 10d;

        [DataMember]
        public double IronOreWeight { get; set; } = 6d;

        [DataMember]
        public double OilWeight { get; set; } = 2d;

        [DataMember]
        public bool VerboseLogging { get; set; }

        internal void Normalize()
        {
            CycleMinutes = Math.Max(5, Math.Min(720, CycleMinutes));
            CoalWeight = NormalizeWeight(CoalWeight);
            CopperOreWeight = NormalizeWeight(CopperOreWeight);
            IronOreWeight = NormalizeWeight(IronOreWeight);
            OilWeight = NormalizeWeight(OilWeight);
        }

        internal MineConfig Copy() =>
            new MineConfig
            {
                Enabled = Enabled,
                UseOilRecipeReplacement = UseOilRecipeReplacement,
                CycleMinutes = CycleMinutes,
                CoalWeight = CoalWeight,
                CopperOreWeight = CopperOreWeight,
                IronOreWeight = IronOreWeight,
                OilWeight = OilWeight,
                VerboseLogging = VerboseLogging
            };

        private static double NormalizeWeight(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0d;
            return Math.Max(0d, Math.Min(100d, value));
        }
    }
}
