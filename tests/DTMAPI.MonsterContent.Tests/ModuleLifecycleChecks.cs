using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ModuleLifecycleChecks()
    {
        bool main = true;
        var backends = new List<ScopedBackend>();
        using var host = new MonsterModuleFixture(() => { var b = new ScopedBackend(); backends.Add(b); return b; }, () => main);
        var a = host.Api(new Manifest("A")); var bApi = host.Api(new Manifest("B"));
        Check(a.GetState().Value == MonsterModuleState.NotRequested && a.GetTemplates().Diagnostic!.Code == "ModuleNotRequested" && backends.Count == 0, "Querying a cold API has no native demand.");
        main = false; Check(!a.RegisterDefinition(Definition()).Success && backends.Count == 0, "Thread admission precedes backend construction."); main = true;
        var first = a.RegisterDefinition(Definition("one")).Value!;
        var second = a.RegisterDefinition(Definition("two")).Value!;
        var other = bApi.RegisterDefinition(Definition("one")).Value!;
        Check(backends.Count == 1 && first.Snapshot.State == MonsterDefinitionState.Creatable, "Accepted registration creates demand without RequestModule.");
        var warm = a.RequestModule().Value!; warm.Dispose();
        Check(first.Snapshot.State == MonsterDefinitionState.Creatable && !backends[0].Disposed, "Prewarm release preserves accepted definitions.");
        first.Dispose();
        Check(second.Snapshot.State == MonsterDefinitionState.Creatable && other.Snapshot.State == MonsterDefinitionState.Creatable, "Single registration release isolates sibling and other Owner.");
        var replacement = a.RegisterDefinition(Definition("one")).Value!; first.Dispose();
        Check(replacement.Snapshot.State == MonsterDefinitionState.Creatable, "Released ID can be reused and old handle cannot withdraw it.");
        Check(host.ReleaseOwner("A") && second.Snapshot.State == MonsterDefinitionState.Withdrawn && replacement.Snapshot.State == MonsterDefinitionState.Withdrawn && other.Snapshot.State == MonsterDefinitionState.Creatable, "Owner exit closes all its contributions, preserving other Owners.");
        other.Dispose();
        Check(backends[0].Disposed && a.GetState().Value == MonsterModuleState.DormantResident, "Last contribution drains native resources.");
        var again = a.RegisterDefinition(Definition("one")).Value!;
        Check(backends.Count == 2 && again.Snapshot.State == MonsterDefinitionState.Creatable, "Successful drain permits a fresh backend in the same process.");
        backends[1].FailWithdraw = true;
        try { again.Dispose(); } catch (InvalidOperationException) { }
        Check(again.Snapshot.State == MonsterDefinitionState.CleanupPending && a.RegisterDefinition(Definition("one")).Diagnostic!.Code == "DuplicateId", "Pending native cleanup keeps identity and retry responsibility.");
        backends[1].FailWithdraw = false; host.RetryPending();
        Check(again.Snapshot.State == MonsterDefinitionState.Withdrawn && backends[1].Disposed, "Core cleanup pump retries retained contribution without a new call-order gate.");
        using var restored = a.RegisterDefinition(Definition("one")).Value!;
        backends[2].DuringPublish = () => host.ReleaseOwner("B");
        var reentered = bApi.RegisterDefinition(Definition("reentered"));
        Check(!reentered.Success && backends[2].Definitions.CountOwnerResources("B") == 0, "Owner exit during publication rolls back the late lease.");
        backends[2].DuringPublish = null;
        host.Required = (_, provider) => provider == "Tools";
        var dependent = Definition("dependent"); dependent.Behavior = "code-provider"; dependent.BehaviorId = "Tools:patrol";
        using var usesProvider = bApi.RegisterDefinition(dependent).Value!;
        Check(host.ReleaseOwner("Tools") && usesProvider.Snapshot.State == MonsterDefinitionState.Withdrawn && restored.Snapshot.State == MonsterDefinitionState.Creatable, "Actual cross-Owner uses close only the dependent contribution before its Provider exits.");
        host.BeginClose(); Check(a.RegisterDefinition(Definition("late")).Diagnostic!.Code == "OwnerClosed", "Provider closure stops new admission.");
    }

    sealed class ScopedBackend : Backend, IDefinitionPublisher, IMonsterAudioBackend, IMonsterOrderedDefinitionBackend
    {
        internal readonly DefinitionRegistry Definitions;
        internal readonly List<int> Orders = new List<int>();
        internal bool FailWithdraw, FailSoundStop;
        internal int SoundStops;
        internal Action? DuringPublish;
        public bool Ready { get; set; } = true;
        public long Generation { get; set; } = 1;
        internal ScopedBackend() { Definitions = new DefinitionRegistry(this, () => { }); }
        public MonsterDiagnostic? Publish(DefinitionRecord definition) { DuringPublish?.Invoke(); return null; }
        public MonsterDiagnostic? Withdraw(DefinitionRecord definition) => FailWithdraw ? new MonsterDiagnostic("CleanupPending", "native release pending") : null;
        public override MonsterResult<MonsterDefinitionLease> Register(string owner, MonsterDefinition value) => Definitions.Register(owner, value);
        public MonsterResult<MonsterDefinitionLease> Register(string owner, int order, MonsterDefinition value) { Orders.Add(order); return Register(owner, value); }
        public void WithdrawSounds(IReadOnlyCollection<IAudioSound> sounds) { SoundStops++; if (FailSoundStop) throw new InvalidOperationException("sound End pending"); }
        public override int CountOwnerResources(string owner) => Definitions.CountOwnerResources(owner) + base.CountOwnerResources(owner);
    }
}
