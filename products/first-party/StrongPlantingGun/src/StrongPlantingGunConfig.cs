using System.Runtime.Serialization;

namespace DTMAPI.StrongPlantingGun
{
    [DataContract]
    internal sealed class StrongPlantingGunConfig
    {
        [DataMember]
        public bool Enabled { get; set; } = true;

        [DataMember]
        public bool IncludeSeeds { get; set; } = true;

        [DataMember]
        public bool IncludeFilms { get; set; } = true;

        [DataMember]
        public bool IncludeFertilizers { get; set; } = true;

        [DataMember]
        public bool VerboseLogging { get; set; }

        internal StrongPlantingGunConfig Copy() =>
            new StrongPlantingGunConfig
            {
                Enabled = Enabled,
                IncludeSeeds = IncludeSeeds,
                IncludeFilms = IncludeFilms,
                IncludeFertilizers = IncludeFertilizers,
                VerboseLogging = VerboseLogging
            };
    }
}
