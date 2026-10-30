using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.MonsterContent.Authoring
{
    public static class ContentCompiler
    {
        private static MonsterDiagnostic Error(ContentInput input, string code, string field, string message) => new MonsterDiagnostic(code, message, input.OwnerId, stage: "ValidateReferences", field: field);
        public static IReadOnlyList<MonsterDiagnostic> Validate(ContentInput input, MonsterContentCatalog catalog)
        {
            var errors = new List<MonsterDiagnostic>();
            if (!MonsterAdmission.ValidLocalId(input.OwnerId)) errors.Add(Error(input, "InvalidOwner", "pack.id", "Expected a valid author owner ID."));
            if (input.Definitions.Count + input.NativeOverrides.Count == 0) errors.Add(Error(input, "InvalidDefinitionCount", "monsters", "Expected at least one definition or native override."));
            void Unique(IEnumerable<string> ids, string field) { var seen = new HashSet<string>(StringComparer.Ordinal); foreach (string id in ids) if (!seen.Add(id)) errors.Add(Error(input, "DuplicateId", field, "Duplicate local identity: " + id)); }
            Unique(input.Definitions.Select(d => d.LocalId), "monsters.id"); Unique(input.DropPools.Select(p => p.LocalId), "dropPools.id"); Unique(input.SpawnRules.Select(r => r.LocalId), "spawnRules.id"); Unique(input.Dependencies.Select(d => d.Id.ToUpperInvariant()), "dependencies.id");
            Unique(input.NativeOverrides.Select(v => v.LocalId), "nativeOverrides.id");
            foreach (var dep in input.Dependencies)
            {
                try { if (!MonsterAdmission.ValidLocalId(dep.Id)) throw new FormatException("Invalid provider ID."); new PackageVersionRange(dep.MinimumInclusive, dep.MaximumExclusive, dep.IncludePrerelease); }
                catch (Exception e) when (e is FormatException || e is ArgumentException || e is System.IO.InvalidDataException) { errors.Add(Error(input, "InvalidDependency", "dependencies", e.Message)); }
            }
            if (errors.Count != 0) return errors;
            void Dependency(string qualified, string code, string field)
            {
                if (qualified.Split(':').Length != 2) return; string owner = qualified.Split(':')[0];
                if (string.Equals(owner, input.OwnerId, StringComparison.OrdinalIgnoreCase)) return;
                var dep = input.Dependencies.FirstOrDefault(d => string.Equals(d.Id, owner, StringComparison.OrdinalIgnoreCase));
                if (dep == null) { errors.Add(Error(input, code, field, "A required provider version dependency is missing: " + owner)); return; }
                string? version = catalog.Behaviors.FirstOrDefault(b => b.Id == qualified)?.Version ?? catalog.DropPools.FirstOrDefault(p => p.Reference.Id == qualified)?.Reference.Version;
                if (version != null)
                {
                    try { if (!new PackageVersionRange(dep.MinimumInclusive, dep.MaximumExclusive, dep.IncludePrerelease).Contains(PackageSemanticVersion.Parse(version))) errors.Add(Error(input, "DependencyVersionMismatch", field, "Provider version is outside the declared range: " + owner)); }
                    catch (Exception e) when (e is FormatException || e is System.IO.InvalidDataException) { errors.Add(Error(input, "DependencyVersionUnavailable", field, "Provider version cannot be checked: " + owner)); }
                }
            }
            var pools = catalog.DropPools.ToList();
            foreach (var pool in input.DropPools)
            {
                var error = MonsterAdmission.Pool(input.OwnerId, pool, catalog); if (error != null) { errors.Add(error); continue; }
                string id = input.OwnerId + ":" + pool.LocalId;
                if (pools.Any(p => p.Reference.Id == id && p.Source == MonsterPoolSource.Managed)) { errors.Add(Error(input, "DuplicateId", "dropPools.id", "Pool already exists: " + id)); continue; }
                pools.Add(new MonsterDropPoolInfo(new MonsterCatalogEntry(id, "DropPool", input.OwnerId, "0.1.0", "author-input"), MonsterPoolSource.Managed, pool.Entries.Select(e => new MonsterDropEntryInfo(e.ItemId, e.Weight, e.MinCount, e.MaxCount))));
            }
            var working = CatalogCodec.WithPools(catalog, pools);
            foreach (var value in input.NativeOverrides)
            {
                if (value.TargetId.Contains(":"))
                {
                    Dependency(value.TargetId, "DefinitionDependencyRequired", "targetId");
                    if (value.TargetId.StartsWith(input.OwnerId + ":", StringComparison.Ordinal) && !input.Definitions.Any(d => input.OwnerId + ":" + d.LocalId == value.TargetId))
                        errors.Add(Error(input, "DefinitionUnavailable", "targetId", "The local drop target must exist in this project."));
                }
                if (value.Drops?.ReferencesPool == true && value.Drops.Source == MonsterPoolSource.Managed) Dependency(value.Drops.PoolId, "DropPoolDependencyRequired", "fields.drops.poolId");
                var error = MonsterOverrideAdmission.Validate(input.OwnerId, value, working); if (error != null) errors.Add(error);
            }
            foreach (var d in input.Definitions.Where(d => d.Drops.Entries != null))
            { var error = MonsterAdmission.Pool(input.OwnerId, new MonsterDropPool { LocalId = d.LocalId, Entries = d.Drops.Entries! }, working); if (error != null) errors.Add(error); }
            foreach (var d in input.Definitions)
            {
                if (d.Behavior == "code-provider") Dependency(d.BehaviorId, "BehaviorDependencyRequired", "behaviorId");
                if (d.Drops.ReferencesPool && d.Drops.Source == MonsterPoolSource.Managed) Dependency(d.Drops.PoolId, "DropPoolDependencyRequired", "drops.poolId");
                var error = MonsterAdmission.Definition(input.OwnerId, d, working); if (error != null) errors.Add(error);
            }
            foreach (var rule in input.SpawnRules)
            {
                var definition = input.Definitions.FirstOrDefault(d => input.OwnerId + ":" + d.LocalId == rule.DefinitionId);
                if (definition == null) { errors.Add(Error(input, "DefinitionUnavailable", "definitionId", "This candidate document requires a definition from the same project.")); continue; }
                var template = catalog.Templates.FirstOrDefault(t => t.Id == definition.TemplateId); if (template == null) continue;
                var error = MonsterAdmission.Rule(input.OwnerId, rule, template, catalog, definition.Body); if (error != null) errors.Add(error);
            }
            return errors;
        }
    }

    // Candidate adapter only: never discovers packs or assigns an arbitrary content owner.
    public sealed class ContentRegistration : IDisposable
    {
        private readonly List<IDisposable> leases = new List<IDisposable>();
        private string owner = "";
        public List<MonsterDefinitionLease> Definitions { get; } = new List<MonsterDefinitionLease>();
        public List<MonsterContentLease> Pools { get; } = new List<MonsterContentLease>();
        public List<MonsterContentLease> Rules { get; } = new List<MonsterContentLease>();
        public List<MonsterOverrideLease> Overrides { get; } = new List<MonsterOverrideLease>();
        public MonsterDiagnostic? RegistrationDiagnostic { get; private set; }
        public MonsterDiagnostic? CleanupDiagnostic { get; private set; }
        public MonsterDiagnostic? Diagnostic => CleanupDiagnostic ?? RegistrationDiagnostic;
        // Historical admission result, independent of a later release attempt.
        public bool Accepted => RegistrationDiagnostic == null;
        public int RemainingLeases => leases.Count;
        public static ContentRegistration Register(IMonsterContentApi api, string canonicalOwner, ContentInput input)
        {
            var r = new ContentRegistration { owner = canonicalOwner };
            if (!string.Equals(canonicalOwner, input.OwnerId, StringComparison.Ordinal)) { r.RegistrationDiagnostic = new MonsterDiagnostic("OwnerMismatch", "Payload owner must equal the actual caller's identity.", canonicalOwner, stage: "Admission", field: "ownerId"); return r; }
            // Runtime facade revalidates every operation against the caller's current
            // manifest/dependencies and native identities. Offline reports grant no admission.
            bool Keep<T>(MonsterResult<T> result, Action<T> save) where T : IDisposable
            {
                if (result.Value != null) { r.leases.Add(result.Value); save(result.Value); }
                if (!result.Success) { r.RegistrationDiagnostic = result.Diagnostic; return false; }
                return true;
            }
            foreach (var pool in input.DropPools) if (!Keep(api.RegisterDropPool(pool), v => r.Pools.Add(v))) { r.Compensate(); return r; }
            foreach (var d in input.Definitions) if (!Keep(api.RegisterDefinition(d), v => r.Definitions.Add(v))) { r.Compensate(); return r; }
            foreach (var value in input.NativeOverrides) if (!Keep(api.RegisterOverride(value), v => r.Overrides.Add(v))) { r.Compensate(); return r; }
            foreach (var rule in input.SpawnRules) if (!Keep(api.RegisterSpawnRule(rule), v => r.Rules.Add(v))) { r.Compensate(); return r; }
            return r;
        }
        private void Compensate()
        {
            try { Dispose(); } catch { /* Dispose retained the failed lease and its diagnostic. */ }
        }
        public void Dispose()
        {
            // Stop at the first failed dependency release; never release its resources underneath it.
            while (leases.Count != 0)
            {
                int i = leases.Count - 1;
                try { leases[i].Dispose(); }
                catch (Exception ex)
                {
                    string id = "", template = "";
                    if (leases[i] is MonsterDefinitionLease definition) { id = definition.Snapshot.Id; template = definition.Snapshot.TemplateId; }
                    else if (leases[i] is MonsterContentLease content) id = content.Snapshot.Id;
                    CleanupDiagnostic = new MonsterDiagnostic("CleanupPending", ex.Message, owner, id, template,
                        RegistrationDiagnostic == null ? "Withdraw" : "Rollback", "leases", "Retry cleanup; restart if the native resource cannot be released.");
                    throw;
                }
                leases.RemoveAt(i);
            }
            CleanupDiagnostic = null;
        }
    }
}
