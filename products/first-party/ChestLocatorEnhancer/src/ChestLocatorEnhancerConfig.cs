using System.Runtime.Serialization;

namespace DTMAPI.ChestLocatorEnhancer
{
    [DataContract]
    internal sealed class ChestLocatorEnhancerConfig
    {
        [DataMember]
        public bool Enabled { get; set; } = true;

        [DataMember]
        public bool IncludeSharedCases { get; set; } = true;

        [DataMember]
        public bool IncludeSharedStorageShelfBoxes { get; set; } = true;

        [DataMember]
        public bool RespectNativeAutoUseBoxSetting { get; set; } = true;

        [DataMember]
        public bool VerboseLogging { get; set; }

        internal ChestLocatorEnhancerConfig Copy() =>
            new ChestLocatorEnhancerConfig
            {
                Enabled = Enabled,
                IncludeSharedCases = IncludeSharedCases,
                IncludeSharedStorageShelfBoxes = IncludeSharedStorageShelfBoxes,
                RespectNativeAutoUseBoxSetting = RespectNativeAutoUseBoxSetting,
                VerboseLogging = VerboseLogging
            };
    }
}
