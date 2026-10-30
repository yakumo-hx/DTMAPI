using System;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;
using DTMAPI.RuntimeModules.Animals;

namespace DTMAPI.RuntimeModules
{
    internal sealed class Entry : IRuntimeModuleCatalog
    {
        public Entry() { }
        public IRuntimeModule Create(string provider)
        {
            switch (provider)
            {
                case "DTMAPI.Audio": return new AudioService();
                case "DTMAPI.Animals": return new AnimalService();
                case "DTMAPI.Monsters": return new DTMAPI.MonsterContent.MonsterService();
                default: throw new InvalidOperationException("Unknown Runtime module: " + provider);
            }
        }
        public IRuntimeContentSource Prepare(IRuntimeModuleOwnerContext owner) => Content.RuntimeContent.Prepare(owner);
    }
}
