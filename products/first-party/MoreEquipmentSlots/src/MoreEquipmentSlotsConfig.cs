namespace DTMAPI.MoreEquipmentSlots
{
    public sealed class MoreEquipmentSlotsConfig
    {
        // Retained only to deserialize old config files. Product activation belongs
        // to the official owner lifecycle; this historical value is never read.
        public bool Enabled { get; set; } = true;

        public bool VerboseLogging { get; set; }
    }
}
