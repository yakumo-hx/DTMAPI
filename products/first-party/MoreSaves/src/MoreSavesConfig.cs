using System.Runtime.Serialization;

namespace DTMAPI.MoreSaves
{
    [DataContract]
    internal sealed class MoreSavesConfig
    {
        [DataMember]
        public bool VerboseLogging { get; set; }

        internal MoreSavesConfig Copy() =>
            new MoreSavesConfig
            {
                VerboseLogging = VerboseLogging
            };
    }
}
