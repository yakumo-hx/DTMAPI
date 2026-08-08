using System;
using System.Runtime.Serialization;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    [DataContract]
    internal sealed class FishBreedingConfig
    {
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public bool LabelFishRoeTitle { get; set; } = true;
        [DataMember] public bool LabelFishRoeDetails { get; set; }
        [DataMember] public int CacheSeconds { get; set; } = 30;
        [DataMember] public bool VerboseLogging { get; set; }

        internal void Normalize()
        {
            CacheSeconds = Math.Max(0, Math.Min(300, CacheSeconds));
            LabelFishRoeTitle = true;
            LabelFishRoeDetails = false;
            VerboseLogging = false;
        }

        internal FishBreedingConfig Copy()
        {
            return new FishBreedingConfig
            {
                Enabled = Enabled,
                LabelFishRoeTitle = LabelFishRoeTitle,
                LabelFishRoeDetails = LabelFishRoeDetails,
                CacheSeconds = CacheSeconds,
                VerboseLogging = VerboseLogging
            };
        }
    }
}
