using System;
using System.Collections.Generic;
namespace DTMAPI.MonsterContent
{
    internal interface IMonsterCatalogBackend
    {
        IReadOnlyList<MonsterDefinitionCatalogEntry> Definitions();
        MonsterResult<MonsterInstanceSnapshot> SpawnFromCatalog(MonsterSpawnRequest request);
    }
}
