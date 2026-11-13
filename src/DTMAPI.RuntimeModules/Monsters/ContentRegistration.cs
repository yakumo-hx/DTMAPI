using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;

namespace DTMAPI.RuntimeModules.Monsters
{
    internal sealed class ContentRegistration : IDisposable
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
        public static ContentRegistration Register(IMonsterContentApi api, string canonicalOwner, MonsterContentData input)
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
        private void Compensate() => Release();
        public void Dispose()
        {
            var error = Release();
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
        private Exception? Release()
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
                    return ex;
                }
                leases.RemoveAt(i);
            }
            CleanupDiagnostic = null;
            return null;
        }
    }
}
