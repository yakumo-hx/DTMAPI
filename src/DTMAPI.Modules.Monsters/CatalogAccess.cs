using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule : IMonsterCatalogBackend
    {
        public IReadOnlyList<MonsterDefinitionCatalogEntry> Definitions()
        {
            RequireThread();
            return definitions.Records.Where(r => r.State != MonsterDefinitionState.Withdrawn).OrderBy(r => r.Id, StringComparer.Ordinal).Select(r =>
                new MonsterDefinitionCatalogEntry(r.Id, r.NativeId, r.Owner, ownerMetadata(r.Owner)?.Name ?? r.Owner,
                    string.IsNullOrWhiteSpace(r.Definition.Display.Name) ? r.Definition.LocalId : r.Definition.Display.Name, r.Definition.TemplateId, r.State)).ToArray();
        }
        public MonsterResult<MonsterInstanceSnapshot> SpawnFromCatalog(MonsterSpawnRequest request)
        {
            RequireThread();
            var record = request == null ? null : definitions.Records.FirstOrDefault(r => r.Id == request.DefinitionId && r.State == MonsterDefinitionState.Creatable);
            return record == null ? MonsterResult<MonsterInstanceSnapshot>.Fail(new MonsterDiagnostic("DefinitionUnavailable", "The definition is no longer available in the current catalog.")) : Spawn(record.Owner, request!);
        }
    }
}
