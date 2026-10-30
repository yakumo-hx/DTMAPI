using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static MonsterContentCatalog AuthorCatalog()
    {
        var template = new MonsterTemplate("test-template", 1, true, false, "fixture", "fixture", 100, 2, "fixture", Array.Empty<MonsterDiagnostic>())
        { GenerationType = "Ground", Volume = 2, Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, true) };
        return new MonsterContentCatalog(1, "fixture", "0.1.0", "static-fingerprint", new[] { template }, new[] { new MonsterCatalogEntry("iron", "Item", "fixture", "1", "i") },
            new[] { new MonsterDropPoolInfo(new MonsterCatalogEntry("effective", "DropPool", "NativeMergedTables", "1", "p"), MonsterPoolSource.Effective, new[] { new MonsterDropEntryInfo("iron", 0, 1, null) }) },
            new[] { new MonsterRoomInfo(new MonsterCatalogEntry("room", "Room", "fixture", "1", "r"), new[] { new MonsterSpawnRegion("ground", "Ground", 8) }, false) })
        { Behaviors = new[] { new MonsterBehaviorInfo("Provider:patrol", "0.1.0", true, new MonsterBehaviorRequirements { Movement = true, AttackIds = new List<string> { "AtkNormal" } }) }, Observation = new MonsterCatalogObservation(1, true) };
    }
    static ContentInput AuthorInput()
    {
        var input = new ContentInput { OwnerId = "Author" };
        input.Dependencies.Add(new ContentDependency { Id = "Provider", MinimumInclusive = "0.1.0", MaximumExclusive = "0.2.0" });
        input.DropPools.Add(new MonsterDropPool { LocalId = "loot", Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "iron", Weight = 0, MinCount = 1, MaxCount = null } } });
        input.Definitions.Add(new MonsterDefinition { LocalId = "inherit", TemplateId = "test-template" });
        input.Definitions.Add(new MonsterDefinition { LocalId = "effective", TemplateId = "test-template", Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, PoolId = "effective", MinCount = 1, MaxCount = 3 } });
        input.Definitions.Add(new MonsterDefinition { LocalId = "own", TemplateId = "test-template", Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, Source = MonsterPoolSource.Managed, PoolId = "Author:loot" } });
        input.Definitions.Add(new MonsterDefinition { LocalId = "code", TemplateId = "test-template", Behavior = "code-provider", BehaviorId = "Provider:patrol", Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, Source = MonsterPoolSource.Managed, PoolId = "Author:loot", MinCount = 2, MaxCount = 4 } });
        input.SpawnRules.Add(new MonsterSpawnRule { LocalId = "rule", DefinitionId = "Author:own", RoomId = "room", RegionId = "ground", Count = 1 });
        return input;
    }
    static void AuthoringChecks()
    {
        var catalog = AuthorCatalog(); var input = AuthorInput();
        Check(ContentCompiler.Validate(input, catalog).Count == 0, "Four definitions share an owned pool with zero weight and unlimited per-item maximum.");
        string payload = ContentCodec.WriteContent(input);
        var parsed = ContentCodec.ReadContent(payload, "Author", _ => throw new Exception("Unexpected image read."));
        parsed.Dependencies.AddRange(input.Dependencies); // Required dependencies are package manifest facts.
        Check(ContentCodec.WriteContent(parsed) == payload, "Normalized writer and runtime reader roundtrip byte-for-byte.");
        Check(ContentCompiler.Validate(parsed, catalog).Count == 0, "Declarative and direct inputs share reference admission.");
        var copy = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(CatalogCodec.Write(copy) == CatalogCodec.Write(catalog), "Catalog metadata including action requirements roundtrips deterministically.");
        catalog.Observation = new MonsterCatalogObservation(2, true);
        Check(catalog.Fingerprint == copy.Fingerprint && catalog.Observation.SaveSessionEpoch != copy.Observation.SaveSessionEpoch, "Unchanged static fingerprint is not a save-session identity.");
        Check(ContentCompiler.Validate(input, catalog).Count == 0 && !catalog.Rooms[0].HostAvailable, "Offline absence of a host cannot reject a valid room prototype.");
        void EquivalentFailure(Action<ContentInput> change, string code, string field)
        {
            var direct = AuthorInput(); change(direct);
            var declaration = ContentCodec.ReadContent(ContentCodec.WriteContent(direct), "Author", _ => throw new Exception());
            declaration.Dependencies.AddRange(direct.Dependencies);
            var a = ContentCompiler.Validate(direct, catalog); var b = ContentCompiler.Validate(declaration, catalog);
            Check(a.Any(d => d.Code == code && d.Field == field) && a.Select(d => d.Code + ":" + d.Field + ":" + d.Stage).SequenceEqual(b.Select(d => d.Code + ":" + d.Field + ":" + d.Stage)), "Equivalent failure: " + code);
        }
        EquivalentFailure(i => i.Definitions[0].TemplateId = "missing", "TemplateMissing", "templateId");
        EquivalentFailure(i => i.DropPools[0].Entries[0].ItemId = "missing", "ItemMissing", "entries[0].itemId");
        EquivalentFailure(i => i.Definitions[1].Drops.PoolId = "missing", "DropPoolMissing", "drops.poolId");
        EquivalentFailure(i => i.Definitions[3].BehaviorId = "Provider:missing", "BehaviorUnavailable", "behaviorId");
        EquivalentFailure(i => i.Dependencies.Clear(), "BehaviorDependencyRequired", "behaviorId");
        EquivalentFailure(i => i.Dependencies[0].MinimumInclusive = "0.1.1", "DependencyVersionMismatch", "behaviorId");
        EquivalentFailure(i => i.SpawnRules[0].Count = 5, "SpawnCapacityExceeded", "count");
        EquivalentFailure(i => i.SpawnRules[0].RoomId = "missing", "RoomMissing", "roomId");
        EquivalentFailure(i => i.DropPools[0].Entries[0].MaxCount = 0, "InvalidDropPool", "entries[0].countRange");
        EquivalentFailure(i => i.Definitions[1].Drops.MaxCount = null, "InvalidDrops", "drops.countRange");
        EquivalentFailure(i => i.Definitions[1].LocalId = "inherit", "DuplicateId", "monsters.id");
        catalog.Templates[0].Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, false, "Unsupported mover.");
        Check(ContentCompiler.Validate(input, catalog).Any(d => d.Code == "MovementUnsupported"), "Behavior move requirements are checked against template capabilities.");
        catalog.Templates[0].Actions = new MonsterActionCapabilities(Array.Empty<string>(), true);
        Check(ContentCompiler.Validate(input, catalog).Any(d => d.Code == "AttackMissing"), "Behavior attack requirements reject an incompatible replacement template.");
        catalog = AuthorCatalog(); catalog.Behaviors = Array.Empty<MonsterBehaviorInfo>();
        Check(ContentCompiler.Validate(parsed, catalog).Any(d => d.Code == "BehaviorUnavailable"), "Previously compiled bytes are revalidated after provider withdrawal.");
        void ParseFailure(string text, string code)
        { try { ContentCodec.ReadContent(text, "Author", _ => throw new Exception()); throw new Exception("Expected rejection."); } catch (AuthoringException e) { Check(e.Diagnostic.Code == code, "Strict reader: " + code); } }
        ParseFailure(payload.Replace("\"schemaVersion\":1", "\"schemaVersion\":9"), "UnsupportedVersion");
        ParseFailure(payload.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1"), "DuplicateField");
        ParseFailure(payload.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"future\":0"), "UnknownField");
        ParseFailure(payload.Replace("\"inherit-native\"", "\"typo\""), "UnknownEnum");
        ParseFailure(payload.Replace("\"inherit-native\"", "0"), "InvalidType");
        foreach (string path in new[] { "../outside.png", "C:/private.png", "images/../secret.png", "images/file.png" })
        { try { ContentCodec.ValidateResourcePath(path); throw new Exception("Accepted unsafe path."); } catch (AuthoringException e) { Check(e.Diagnostic.Code == "InvalidResourcePath", "Unsafe resource rejected: " + path); } }
        var tree = (Dictionary<string, object?>)((Dictionary<string, object?>)Json.Read(payload)!)["monsters"]!;
        string project = Json.Write(Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "Author", "name", "Test"), "monsters", tree["definitions"], "dropPools", tree["dropPools"], "spawnRules", tree["spawnRules"], "dependencies", input.Dependencies.Select(ContentCodec.DependencyData).ToArray(), "assets", Array.Empty<object>()));
        var draft = ContentCodec.ReadProject(project);
        Check(ContentCodec.WriteContent(draft) == payload, "Author v4 maps directly to normalized candidate data.");
        void ProjectFailure(string text, string code, string field)
        {
            try { ContentCodec.ReadProject(text); throw new Exception("Expected project rejection."); }
            catch (AuthoringException e) { Check(e.Diagnostic.Code == code && e.Diagnostic.Field.Contains(field), "Current project rejects " + field + ": " + code); }
        }
        foreach (int version in new[] { 1, 2, 3, 999 })
            ProjectFailure(project.Replace("\"version\":4", "\"version\":" + version), "UnsupportedVersion", "version");
        ProjectFailure(project.Replace("\"version\":4", "\"version\":4,\"legacyDraft\":null"), "UnknownField", "legacyDraft");
        ProjectFailure(project.Replace("\"version\":4", "\"version\":4,\"legacyDraft\":{}"), "UnknownField", "legacyDraft");
        ProjectFailure(project.Replace("\"version\":4", "\"version\":4,\"priority\":0"), "UnknownField", "priority");
        var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !(System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "DTMAPI.sln")) && System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "global.json")))) directory = directory.Parent;
        string sampleRoot = System.IO.Path.Combine((directory ?? throw new System.IO.DirectoryNotFoundException("Repository root was not found.")).FullName, "examples/monster-authoring/mc08/compiled");
        string savedPayload = System.IO.File.ReadAllText(System.IO.Path.Combine(sampleRoot, "PackData/content.json"));
        var sampleImages = ContentCodec.ReadProject(System.IO.File.ReadAllText(System.IO.Path.Combine(sampleRoot, "../browser-saved.dtmonster.json"))).Images;
        var sampleOwner = ContentCodec.ReadProject(System.IO.File.ReadAllText(System.IO.Path.Combine(sampleRoot, "../browser-saved.dtmonster.json"))).OwnerId;
        var sampleInput = ContentCodec.ReadContent(savedPayload, sampleOwner, path => sampleImages[path]);
        string normalizedSavedPayload = ContentCodec.WriteContent(sampleInput);
        var directSample = MonsterAuthor.SampleDefinitions.Create(sampleInput.OwnerId, sampleRoot, skinPng: sampleImages.Values.Single());
        sampleInput.Definitions.Clear(); sampleInput.Definitions.AddRange(directSample.Definitions);
        sampleInput.DropPools.Clear(); sampleInput.DropPools.AddRange(directSample.Pools);
        sampleInput.SpawnRules.Clear(); sampleInput.SpawnRules.AddRange(directSample.Rules);
        Check(ContentCodec.WriteContent(sampleInput) == normalizedSavedPayload, "Delivered direct C# DTOs exactly match browser-saved normalized payload regardless of checkout line endings.");
        var api = new RegistrationApi();
        var reg = ContentRegistration.Register(api, "Author", AuthorInput());
        Check(reg.Diagnostic!.Code == "InjectedFailure" && reg.RemainingLeases == 0 && api.Releases.SequenceEqual(new[] { "definition", "definition", "pool" }), "Partial batch compensates definitions before pools.");
        api = new RegistrationApi { FailCleanupOnce = true };
        reg = ContentRegistration.Register(api, "Author", AuthorInput());
        Check(reg.Diagnostic!.Code == "CleanupPending" && reg.RemainingLeases == 3 && api.Releases.Count == 0, "Failed cleanup retains its lease and dependencies.");
        reg.Dispose(); reg.Dispose();
        Check(reg.RemainingLeases == 0 && api.Releases.SequenceEqual(new[] { "definition", "definition", "pool" }), "Retained cleanup retries once without releasing dependencies early.");
        Check(ContentRegistration.Register(api, "Other", AuthorInput()).Diagnostic!.Code == "OwnerMismatch", "Payload cannot impersonate another owner.");
    }
    sealed class RegistrationApi : IMonsterContentApi
    {
        int definitions;
        public bool RejectSecond = true, FailCleanupOnce, ThrowCleanup;
        public int Withdrawals;
        public MonsterContentState PoolState = MonsterContentState.Ready;
        public readonly List<string> Releases = new List<string>();
        public readonly DefinitionRegistry Registry;
        private string rejectedId = "";
        public RegistrationApi() { Registry = new DefinitionRegistry(new RegistrationPublisher(this), () => { }); }
        private sealed class RegistrationPublisher : IDefinitionPublisher
        {
            private readonly RegistrationApi api;
            internal RegistrationPublisher(RegistrationApi api) { this.api = api; }
            public bool Ready => true;
            public long Generation => 1;
            public MonsterDiagnostic? Publish(DefinitionRecord d) => d.Id == api.rejectedId ? new MonsterDiagnostic("InjectedFailure", "Rejected second definition", d.Owner, d.Id) : null;
            public MonsterDiagnostic? Withdraw(DefinitionRecord d)
            {
                api.Withdrawals++;
                if (api.FailCleanupOnce)
                {
                    api.FailCleanupOnce = false;
                    if (api.ThrowCleanup) throw new InvalidOperationException("transient native cleanup failure");
                    return new MonsterDiagnostic("RestartRequired", "transient native cleanup failure", d.Owner, d.Id, stage: "Withdraw");
                }
                api.Releases.Add("definition"); return null;
            }
        }
        public MonsterResult<MonsterContentCatalog> GetCatalog() => MonsterResult<MonsterContentCatalog>.Ok(AuthorCatalog());
        public MonsterResult<MonsterContentLease> RegisterDropPool(MonsterDropPool pool) => MonsterResult<MonsterContentLease>.Ok(new MonsterContentLease(() => new MonsterContentSnapshot("Author:" + pool.LocalId, PoolState, 1), () => { PoolState = MonsterContentState.Withdrawn; Releases.Add("pool"); }));
        public MonsterResult<MonsterDefinitionLease> RegisterDefinition(MonsterDefinition d)
        {
            if (++definitions == 2 && RejectSecond) rejectedId = "Author:" + d.LocalId;
            return Registry.Register("Author", d);
        }
        public MonsterResult<MonsterContentLease> RegisterSpawnRule(MonsterSpawnRule r)
        {
            var state = MonsterContentState.Ready;
            return MonsterResult<MonsterContentLease>.Ok(new MonsterContentLease(() => new MonsterContentSnapshot("Author:" + r.LocalId, state, 1), () => { state = MonsterContentState.Withdrawn; Releases.Add("rule"); }));
        }
        public MonsterResult<MonsterOverrideLease> RegisterOverride(MonsterNativeOverride value) => throw new NotSupportedException();
        public MonsterResult<IReadOnlyList<MonsterOverrideSnapshot>> GetOverrides() => throw new NotSupportedException();
        public MonsterResult<MonsterModuleState> GetState() => throw new NotImplementedException();
        public MonsterResult<MonsterLease> RequestModule() => throw new NotImplementedException();
        public MonsterResult<IReadOnlyList<MonsterTemplate>> GetTemplates() => throw new NotImplementedException();
        public MonsterResult<MonsterLease> RegisterBehavior(string id, Func<IMonsterBehavior> f) => throw new NotImplementedException();
        public MonsterResult<MonsterLease> RegisterBehavior(string id, MonsterBehaviorRequirements r, Func<IMonsterBehavior> f) => throw new NotImplementedException();
        public MonsterResult<MonsterInstanceSnapshot> Spawn(MonsterSpawnRequest r) => throw new NotImplementedException();
        public MonsterResult<IReadOnlyList<MonsterInstanceSnapshot>> GetInstances() => throw new NotImplementedException();
        public MonsterResult<MonsterInstanceSnapshot> GetInstance(MonsterHandle h) => throw new NotImplementedException();
        public MonsterResult<bool> Despawn(MonsterHandle h) => throw new NotImplementedException();
        public MonsterResult<bool> CancelSequence(MonsterHandle h) => throw new NotImplementedException();
    }
}
