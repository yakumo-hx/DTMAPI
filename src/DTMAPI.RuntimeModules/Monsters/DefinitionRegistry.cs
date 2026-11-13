using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTMAPI.MonsterContent.Hosting
{
    internal interface IMonsterAudioBackend
    {
        void WithdrawSounds(IReadOnlyCollection<DTMAPI.AudioContent.IAudioSound> sounds);
    }
    // Hosting protocol, not an author native escape hatch. Contains only managed data.
    internal interface IMonsterBackend : IDisposable
    {
        MonsterModuleState State { get; }
        IReadOnlyList<MonsterTemplate> Templates { get; }
        MonsterContentCatalog Catalog { get; }
        MonsterResult<MonsterContentLease> RegisterSpawnRule(string owner, MonsterSpawnRule rule);
        MonsterResult<MonsterContentLease> RegisterDropPool(string owner, MonsterDropPool pool);
        MonsterResult<MonsterDefinitionLease> Register(string owner, MonsterDefinition definition);
        MonsterResult<MonsterLease> RegisterBehavior(string owner, string localId, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report);
        MonsterResult<MonsterInstanceSnapshot> Spawn(string owner, MonsterSpawnRequest request);
        IReadOnlyList<MonsterInstanceSnapshot> Instances(string owner);
        MonsterResult<MonsterInstanceSnapshot> Instance(string owner, MonsterHandle handle);
        MonsterResult<bool> Despawn(string owner, MonsterHandle handle);
        MonsterResult<bool> CancelSequence(string owner, MonsterHandle handle);
        int RemoveOwner(string owner);
        int CountOwnerResources(string owner);
    }

    internal interface IDefinitionPublisher
    {
        bool Ready { get; }
        long Generation { get; }
        MonsterDiagnostic? Publish(DefinitionRecord definition);
        MonsterDiagnostic? Withdraw(DefinitionRecord definition);
    }

    internal sealed class DefinitionRecord
    {
        internal DefinitionRecord(string owner, MonsterDefinition definition, long generation)
        { Owner = owner; Definition = definition.Copy(); Id = owner + ":" + definition.LocalId; NativeId = "dtm_mc_" + Hex(owner) + "_" + Hex(definition.LocalId); Generation = generation; }
        private static string Hex(string value) => BitConverter.ToString(Encoding.UTF8.GetBytes(value)).Replace("-", "").ToLowerInvariant();
        public string Owner { get; }
        public string Id { get; }
        public string NativeId { get; }
        public MonsterDefinition Definition { get; }
        public long Generation { get; internal set; }
        public MonsterDefinitionState State { get; internal set; } = MonsterDefinitionState.WaitingNative;
        public MonsterDiagnostic? Diagnostic { get; internal set; }
        public MonsterDefinitionSnapshot Snapshot => new MonsterDefinitionSnapshot(Id, Definition.TemplateId, Generation, State, Diagnostic) { NativeId = NativeId };
    }

    internal sealed class DefinitionRegistry
    {
        private readonly Dictionary<string, DefinitionRecord> records = new Dictionary<string, DefinitionRecord>(StringComparer.Ordinal);
        private readonly IDefinitionPublisher publisher;
        private readonly Action requireThread;
        public DefinitionRegistry(IDefinitionPublisher publisher, Action requireThread) { this.publisher = publisher; this.requireThread = requireThread; }
        public IEnumerable<DefinitionRecord> Records => records.Values.ToArray();
        public DefinitionRecord? Find(string owner, string id) => records.TryGetValue(id, out var value) && value.Owner == owner ? value : null;
        public MonsterResult<MonsterDefinitionLease> Register(string owner, MonsterDefinition definition)
        {
            requireThread();
            var error = MonsterValidation.Validate(owner, definition);
            if (error != null) return MonsterResult<MonsterDefinitionLease>.Fail(error);
            var record = new DefinitionRecord(owner, definition, publisher.Generation);
            if (records.ContainsKey(record.Id)) return MonsterResult<MonsterDefinitionLease>.Fail(new MonsterDiagnostic("DuplicateId", "This owner/local ID still has an accepted or retiring registration.", owner, record.Id, definition.TemplateId, "Validate", "localId", "Release the existing registration and finish its cleanup."));
            records.Add(record.Id, record);
            Publish(record);
            var lease = new MonsterDefinitionLease(() => record.Snapshot, () => Withdraw(record));
            if (record.State == MonsterDefinitionState.Rejected || record.State == MonsterDefinitionState.RestartRequired)
                return MonsterResult<MonsterDefinitionLease>.FailedOwned(record.Diagnostic!, lease);
            return MonsterResult<MonsterDefinitionLease>.Ok(lease);
        }
        public void Refresh()
        {
            requireThread();
            foreach (var record in records.Values.ToArray())
                if (record.State == MonsterDefinitionState.WaitingNative || (record.State == MonsterDefinitionState.Creatable && record.Generation != publisher.Generation)) Publish(record);
        }
        private void Publish(DefinitionRecord record)
        {
            if (!publisher.Ready) { record.State = MonsterDefinitionState.WaitingNative; return; }
            record.Generation = publisher.Generation;
            try { record.Diagnostic = publisher.Publish(record); }
            catch (Exception ex) { record.Diagnostic = new MonsterDiagnostic("RestartRequired", ex.Message, record.Owner, record.Id, record.Definition.TemplateId, "Publish", recovery: "Publication state is uncertain; restart the game."); }
            record.State = record.Diagnostic == null ? MonsterDefinitionState.Creatable : record.Diagnostic.Code == "WaitingNative" ? MonsterDefinitionState.WaitingNative : record.Diagnostic.Code == "RestartRequired" ? MonsterDefinitionState.RestartRequired : MonsterDefinitionState.Rejected;
            if (record.State == MonsterDefinitionState.Rejected) SoundValidation.ReleaseInputs(record.Definition);
        }
        private void Withdraw(DefinitionRecord record)
        {
            requireThread();
            if (record.State == MonsterDefinitionState.Withdrawn) return;
            // Close admission before beginning a possibly partial release.
            record.State = MonsterDefinitionState.CleanupPending;
            try { record.Diagnostic = publisher.Withdraw(record); }
            catch (Exception ex) { record.Diagnostic = new MonsterDiagnostic("CleanupPending", ex.Message, record.Owner, record.Id, record.Definition.TemplateId, "Withdraw", recovery: "Retry after native cleanup completes."); }
            record.State = record.Diagnostic == null ? MonsterDefinitionState.Withdrawn : record.Diagnostic.Code == "RestartRequired" ? MonsterDefinitionState.RestartRequired : MonsterDefinitionState.CleanupPending;
            if (record.State == MonsterDefinitionState.Withdrawn && records.TryGetValue(record.Id, out var current) && ReferenceEquals(current, record)) records.Remove(record.Id);
            if (record.State == MonsterDefinitionState.Withdrawn) record.Definition.Appearance.Skin = null;
            if (record.State == MonsterDefinitionState.Withdrawn) record.Definition.Compendium = null;
            if (record.State == MonsterDefinitionState.Withdrawn) SoundValidation.ReleaseInputs(record.Definition);
        }
        public int RemoveOwner(string owner)
        {
            int count = 0;
            foreach (var record in records.Values.Where(r => r.Owner == owner && r.State != MonsterDefinitionState.Withdrawn).ToArray()) { Withdraw(record); count++; }
            return count;
        }
        internal void RemoveBehavior(string behaviorId)
        {
            foreach (var record in records.Values.Where(r => r.Definition.BehaviorId == behaviorId && r.State != MonsterDefinitionState.Withdrawn).ToArray()) Withdraw(record);
        }
        internal void RemoveDropPool(string poolId)
        {
            foreach (var record in records.Values.Where(r => r.Definition.Drops.ReferencesPool && r.Definition.Drops.Source == MonsterPoolSource.Managed && r.Definition.Drops.PoolId == poolId && r.State != MonsterDefinitionState.Withdrawn).ToArray()) Withdraw(record);
        }
        public int CountOwnerResources(string owner) => records.Values.Count(r => r.Owner == owner);
    }
}
