using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ContentChecks()
    {
        var pool = new MonsterDropPool { LocalId = "loot", Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "iron", Weight = 0, MinCount = 1, MaxCount = 2 } } };
        Check(ContentValidation.Pool("A", pool) == null, "Zero weight does not disable a guaranteed minimum entry.");
        var copy = pool.Copy(); pool.Entries[0].ItemId = "changed";
        Check(copy.Entries[0].ItemId == "iron", "Pool inputs are deeply copied before registration.");
        pool.Entries[0].MaxCount = 0;
        Check(ContentValidation.Pool("A", pool)!.Field == "entries[0].countRange", "Zero maximum is not a disable flag; unlimited is explicit null.");
        pool.Entries[0].MaxCount = null;
        Check(ContentValidation.Pool("A", pool) == null, "Explicit unlimited per-item maximum is supported.");
        pool.Entries[0].Weight = float.NaN;
        Check(ContentValidation.Pool("A", pool)!.Field == "entries[0].weight", "Nonfinite weights rejected before publication.");
        pool.Entries[0].Weight = 1;
        pool.Entries.Add(pool.Entries[0].Copy());
        Check(ContentValidation.Pool("A", pool)!.Field == "entries[1].itemId", "Duplicate item IDs are rejected before native index construction.");
        pool.Entries.RemoveAt(1);
        pool.Entries[0].MinCount = int.MaxValue;
        Check(ContentValidation.Pool("A", pool)!.Field == "entries.countRange", "Unlimited maxima cannot overflow native minima accumulation.");
        pool.Entries[0].MinCount = 1;
        var definition = Definition(); definition.Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, Source = MonsterPoolSource.Managed, PoolId = "B:loot", MinCount = 2, MaxCount = 1 };
        Check(MonsterValidation.Validate("A", definition)!.Field == "drops.countRange", "Total draws and item limits have independent valid ranges.");
        definition.Drops.MaxCount = 3;
        Check(MonsterValidation.Validate("A", definition) == null, "Well-formed managed pool references are admitted structurally.");
        var publisher = new Publisher { Ready = true };
        var registry = new DefinitionRegistry(publisher, () => { });
        var lease = registry.Register("A", definition).Value!;
        definition.Drops.PoolId = "mutated:pool";
        Check(registry.Find("A", "A:variant")!.Definition.Drops.PoolId == "B:loot", "Monster drop settings are immutable after admission.");
        registry.RemoveDropPool("B:loot");
        Check(lease.Snapshot.State == MonsterDefinitionState.Withdrawn, "Pool retirement withdraws dependent definitions.");
        var ledger = new EncounterLedger();
        Check(ledger.TryBegin("A:rule") && !ledger.TryBegin("A:rule"), "Repeated ready/render/death observations cannot refill one encounter.");
        Check(ledger.TryBegin("B:rule"), "Independent owners keep separate rule identities.");
        long first = ledger.Generation; ledger.ConfirmClear();
        Check(ledger.Generation == first + 1 && ledger.TryBegin("A:rule"), "An actual native list rebuild permits a fresh batch.");
        Check(!ledger.TryBegin("A:rule"), "A failed attempt also consumes the generation's attempt.");
        SpawnBoundaryChecks();
        var rule = new MonsterSpawnRule { LocalId = "r", DefinitionId = "A:variant", RoomId = "room", Count = 1 };
        Check(ContentValidation.Rule("A", rule) == null, "Room rules have explicit definition and room identity.");
        rule.Count = 0; Check(ContentValidation.Rule("A", rule)!.Field == "count", "Nonpositive counts do not become ambiguous disable operations."); rule.Count = 1;
        bool main = true;
        var backend = new Backend();
        using var service = new MonsterModuleFixture(() => backend, () => main);
        var api = (IMonsterContentApi)service.Api(new Manifest("A"), () => { });
        Check(api.GetCatalog().Diagnostic!.Code == "ModuleNotRequested", "Catalog reads cannot activate the module.");
        using var demand = api.RequestModule().Value!;
        main = false; Check(api.RegisterDropPool(pool).Diagnostic!.Code == "WrongThread", "Pool publication obeys the native thread boundary."); main = true;
        definition.Drops.PoolId = "B:loot";
        Check(api.RegisterDefinition(definition).Diagnostic!.Code == "DropPoolDependencyRequired", "Cross-owner pools cannot bypass required manifest dependencies.");
        service.Required = (_, provider) => provider == "B";
        Check(api.RegisterDefinition(definition).Diagnostic!.Code == "fake", "Satisfied pool dependencies reach the backend.");
        rule.DefinitionId = "C:variant";
        Check(api.RegisterSpawnRule(rule).Diagnostic!.Code == "DefinitionDependencyRequired", "Cross-owner rules require the definition provider dependency.");
        rule.DefinitionId = "A:variant";
        Check(api.RegisterSpawnRule(rule).Diagnostic!.Code == "fake" && api.RegisterDropPool(pool).Diagnostic!.Code == "fake", "Rules and pools use the facade's canonical owner.");
        Check(backend.Owners.All(o => o == "A"), "Content DTOs cannot impersonate another owner.");
        int releaseAttempts = 0;
        var cleanupState = MonsterContentState.Ready;
        var contentLease = new MonsterContentLease(() => new MonsterContentSnapshot("A:pool", cleanupState, 1), () =>
        {
            if (++releaseAttempts == 1) { cleanupState = MonsterContentState.RestartRequired; throw new InvalidOperationException("native cleanup pending"); }
            cleanupState = MonsterContentState.Withdrawn;
        });
        try { contentLease.Dispose(); } catch (InvalidOperationException) { }
        Check(contentLease.Snapshot.State == MonsterContentState.RestartRequired, "Failed pool/rule cleanup remains visible through its lease.");
        contentLease.Dispose(); contentLease.Dispose();
        Check(releaseAttempts == 2 && contentLease.Snapshot.State == MonsterContentState.Withdrawn, "Failed release retains responsibility for retry; successful release is idempotent.");
        var closedPublisher = new Publisher { Ready = true, Failure = "RestartRequired" };
        var closedRegistry = new DefinitionRegistry(closedPublisher, () => { });
        closedRegistry.Register("A", Definition()); closedPublisher.Failure = null; closedPublisher.Generation++; closedRegistry.Refresh();
        Check(closedPublisher.Publishes == 1 && closedRegistry.Find("A", "A:variant")!.State == MonsterDefinitionState.RestartRequired, "A new catalog generation cannot re-admit a definition closed for restart.");
        backend.State = MonsterModuleState.WaitingNative;
        Check(api.GetCatalog().Diagnostic!.Code == "NativeNotReady", "Unready references are not represented as a valid empty catalog.");
        backend.State = MonsterModuleState.Active;
        Check(api.GetCatalog().Value!.Generation == 1, "Catalog reports its effective generation.");
        var spawn = new MonsterSpawnRequest();
        Check(spawn.PositionPolicy == MonsterPositionPolicy.Native && spawn.PositionTolerance == .01f, "Default spawning preserves native placement; strict tolerance is explicit.");
    }
}
