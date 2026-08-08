using System.Runtime.Serialization;

namespace DTMAPI.MoreSaves
{
    [DataContract]
    internal sealed class MoreSavesConfig
    {
        [DataMember]
        public bool Enabled { get; set; } = true;

        // Migration-only. Earlier releases exposed this setting; the product contract is fixed at twelve.
        [DataMember]
        public int SlotCount { get; set; } = MoreSavesNativeRuntime.ExpandedSlotCount;

        [DataMember]
        public bool VerboseLogging { get; set; }

        internal void Normalize() => SlotCount = MoreSavesNativeRuntime.ExpandedSlotCount;

        internal MoreSavesConfig Copy() =>
            new MoreSavesConfig
            {
                Enabled = Enabled,
                SlotCount = MoreSavesNativeRuntime.ExpandedSlotCount,
                VerboseLogging = VerboseLogging
            };
    }
}
