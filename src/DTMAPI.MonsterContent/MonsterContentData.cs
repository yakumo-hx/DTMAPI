using System;
using System.Collections.Generic;

namespace DTMAPI.MonsterContent
{
    internal sealed class MonsterContentData
    {
        internal string OwnerId { get; set; } = "";
        internal List<MonsterDefinition> Definitions { get; } = new List<MonsterDefinition>();
        internal List<MonsterNativeOverride> NativeOverrides { get; } = new List<MonsterNativeOverride>();
        internal List<MonsterDropPool> DropPools { get; } = new List<MonsterDropPool>();
        internal List<MonsterSpawnRule> SpawnRules { get; } = new List<MonsterSpawnRule>();
        internal Dictionary<string, byte[]> Images { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
    }
}
