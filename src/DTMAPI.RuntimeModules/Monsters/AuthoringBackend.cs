using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent.Hosting;


namespace DTMAPI.MonsterContent.Hosting
{
    internal interface IMonsterBehaviorMetadataBackend
    {
        MonsterResult<MonsterLease> RegisterBehavior(string owner, string localId, MonsterBehaviorRequirements requirements, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report);
    }
}
