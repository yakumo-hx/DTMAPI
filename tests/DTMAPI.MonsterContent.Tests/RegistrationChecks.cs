using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static void RegistrationChecks()
    {
        foreach (bool compensation in new[] { false, true })
        foreach (bool throws in new[] { false, true })
        {
            var api = new RegistrationApi { RejectSecond = compensation, FailCleanupOnce = true, ThrowCleanup = throws };
            var other = api.Registry.Register("Other", Definition()).Value!;
            var batch = ContentRegistration.Register(api, "Author", AuthorInput());
            if (!compensation)
            {
                bool failed = false;
                try { batch.Dispose(); } catch (InvalidOperationException) { failed = true; }
                Check(failed, "Direct disposal exposes a native cleanup failure.");
            }
            Check(batch.Accepted == !compensation && (batch.RegistrationDiagnostic?.Code == "InjectedFailure") == compensation,
                "Admission remains independent of cleanup failure (publisher throws=" + throws + ").");
            Check(batch.CleanupDiagnostic?.Code == "CleanupPending" && batch.Diagnostic == batch.CleanupDiagnostic && batch.CleanupDiagnostic.OwnerId == "Author",
                "Batch exposes the retained cleanup failure with its actual owner.");
            Check(batch.RemainingLeases == (compensation ? 3 : 5) && batch.Definitions.Any(d => d.Snapshot.State == (throws ? MonsterDefinitionState.CleanupPending : MonsterDefinitionState.RestartRequired)),
                "Real definition failure retains a callable lease and all later dependencies.");
            Check(api.Registry.CountOwnerResources("Author") == (compensation ? 2 : 4) && api.PoolState == MonsterContentState.Ready && !api.Releases.Contains("pool"),
                "Resource accounting and the pool remain held until native withdrawal succeeds.");
            Check(other.Snapshot.State == MonsterDefinitionState.Creatable && api.Registry.CountOwnerResources("Other") == 1,
                "One owner's failed cleanup does not close another owner.");
            int attempts = api.Withdrawals;
            batch.Dispose();
            Check(api.Withdrawals > attempts && batch.RemainingLeases == 0 && batch.Definitions.All(d => d.Snapshot.State == MonsterDefinitionState.Withdrawn),
                "Second disposal retries real registry withdrawal and confirms completion.");
            Check(api.Registry.CountOwnerResources("Author") == 0 && api.PoolState == MonsterContentState.Withdrawn && api.Releases.Last() == "pool",
                "Pool releases only after its definitions; owner accounting clears.");
            Check(batch.CleanupDiagnostic == null && batch.Diagnostic == batch.RegistrationDiagnostic && batch.Accepted == !compensation,
                "Successful retry clears only the cleanup diagnostic, preserving the original admission result.");
            attempts = api.Withdrawals;
            batch.Dispose();
            foreach (var d in batch.Definitions) d.Dispose();
            Check(api.Withdrawals == attempts && other.Snapshot.State == MonsterDefinitionState.Creatable,
                "Repeated batch and direct lease disposal are idempotent and isolated.");
            other.Dispose();
        }
        foreach (string kind in new[] { "pool", "rule" })
        {
            int attempts = 0;
            var state = MonsterContentState.Ready;
            var lease = new MonsterContentLease(() => new MonsterContentSnapshot("Author:" + kind, state, 1),
                () => state = ++attempts == 1 ? MonsterContentState.RestartRequired : MonsterContentState.Withdrawn);
            bool failed = false;
            try { lease.Dispose(); } catch (InvalidOperationException) { failed = true; }
            Check(failed && state == MonsterContentState.RestartRequired, "State-only " + kind + " cleanup failure cannot consume the release callback.");
            lease.Dispose(); lease.Dispose();
            Check(attempts == 2 && state == MonsterContentState.Withdrawn, "State-only " + kind + " failure remains retryable.");
        }
    }
}
