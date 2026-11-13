using System.Collections.Generic;

namespace DTMAPI.MonsterContent
{
    public sealed class MonsterDefinitionCatalogEntry
    {
        public MonsterDefinitionCatalogEntry(string id, string nativeId, string ownerId, string sourceName, string displayName, string templateId, MonsterDefinitionState state)
        { Id = id; NativeId = nativeId; OwnerId = ownerId; SourceName = sourceName; DisplayName = displayName; TemplateId = templateId; State = state; }
        public string Id { get; }
        public string NativeId { get; }
        public string OwnerId { get; }
        public string SourceName { get; }
        public string DisplayName { get; }
        public string TemplateId { get; }
        public MonsterDefinitionState State { get; }
    }
    /// <summary>Optional inspection/debug integration. Does not activate the module. Spawned entities retain the definition owner's lifetime.</summary>
    public interface IMonsterCatalogApi
    {
        MonsterResult<IReadOnlyList<MonsterDefinitionCatalogEntry>> GetDefinitions();
        MonsterResult<MonsterInstanceSnapshot> SpawnFromCatalog(MonsterSpawnRequest request);
    }

}
