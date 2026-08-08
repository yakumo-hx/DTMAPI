using System;
using System.Runtime.Serialization;

namespace DTMAPI.DebugConsole
{
    [DataContract]
    internal sealed class DebugConsoleConfig
    {
        [DataMember]
        public string Language { get; set; } = "Auto";

        internal void Normalize()
        {
            if (!Language.Equals("Auto", StringComparison.OrdinalIgnoreCase) &&
                !Language.Equals("schinese", StringComparison.OrdinalIgnoreCase) &&
                !Language.Equals("english", StringComparison.OrdinalIgnoreCase))
                Language = "Auto";
        }
    }
}
