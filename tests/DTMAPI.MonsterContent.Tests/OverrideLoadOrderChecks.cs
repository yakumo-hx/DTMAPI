using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static void OverrideLoadOrderChecks()
    {
        foreach (bool reverse in new[] { false, true })
        {
            var owners = new[] { new Manifest("Host"), new Manifest("PackA"), new Manifest("Code"), new Manifest("PackB") };
            var backend = new Backend { State = MonsterModuleState.WaitingNative };
            using var service = new MonsterModuleFixture(() => backend, order: manifest => Array.FindIndex(owners, owner => ReferenceEquals(manifest, owner)));
            var apis = owners.Select(owner => (IMonsterContentApi)service.Api(owner, () => { })).ToArray();
            foreach (var api in apis) Check(api.RequestModule().Success, "Each actual Pack/Code facade acquires its own demand.");
            MonsterNativeOverride Value(string id, int? defense, float? hurt = null) => new MonsterNativeOverride
                { LocalId = id, TargetId = "test-template", Defense = defense, HurtDuration = hurt };
            foreach (int index in reverse ? new[] { 3, 2, 1 } : new[] { 1, 2, 3 })
            {
                var value = Value("armor", index * 6, index == 1 ? .6f : (float?)null);
                if (index == 2) Check(apis[index].RegisterOverride(value).Success, "C# uses the same registration path.");
                else
                {
                    var input = new ContentInput { OwnerId = owners[index].UniqueID }; input.NativeOverrides.Add(value);
                    Check(ContentRegistration.Register(apis[index], input.OwnerId, input).Accepted, "Pack registration uses its own facade.");
                }
            }
            Check(apis[3].GetOverrides().Value!.All(row => row.State == MonsterOverrideState.WaitingNative && row.DeclarationOrder == 0 && row.LoadIndex == Array.FindIndex(owners, owner => owner.UniqueID == row.OwnerId)), "Successful early declarations retain their own frozen owner index and declaration order.");
            backend.State = MonsterModuleState.Active; backend.Overrides.Refresh(true, 1);
            Check(backend.Overrides.Resolve("test-template", "defense", 2) == 18 && Math.Abs(backend.Overrides.Resolve("test-template", "hurtDuration", .5) - .6) < .00001,
                "Frozen Host->PackA->Code->PackB order must choose PackB defense and PackA hurt, independent of registration order.");
            var packA = apis[1].GetOverrides().Value!.Single(row => row.OwnerId == "PackA");
            Check(packA.Fields.Single(field => field.Field == "defense").WinnerId == "PackB:armor" && packA.Fields.Single(field => field.Field == "defense").State == MonsterOverrideFieldState.Shadowed && packA.Fields.Single(field => field.Field == "hurtDuration").State == MonsterOverrideFieldState.Effective,
                "Diagnostics explain distinct winners for fields on the same target.");
            var lateLow = apis[1].RegisterOverride(Value("late", 99)).Value!;
            Check(backend.Overrides.Resolve("test-template", "defense", 2) == 18, "A lower Owner registering late cannot outrank PackB.");
            var latest = apis[3].RegisterOverride(Value("zero", 0)).Value!;
            Check(backend.Overrides.Resolve("test-template", "defense", 2) == 0 && latest.Snapshot.DeclarationOrder == 1 && latest.Snapshot.Fields.Single().WinnerId == "PackB:zero", "Last successful declaration wins within one Owner; zero participates.");
            Check(!apis[3].RegisterOverride(Value("bad", -1, .1f)).Success && backend.Overrides.Resolve("test-template", "defense", 2) == 0,
                "An invalid declaration is rejected as a whole without changing another field.");
            using (var afterBad = apis[3].RegisterOverride(Value("after_bad", 19)).Value!)
                Check(afterBad.Snapshot.DeclarationOrder == 2, "Rejected declarations do not consume a declaration order.");
            latest.Dispose(); service.ReleaseOwner("PackB");
            Check(backend.Overrides.Resolve("test-template", "defense", 2) == 12, "Withdrawing PackB reveals the remaining Code owner.");
            service.ReleaseOwner("Code");
            Check(backend.Overrides.Resolve("test-template", "defense", 2) == 99, "The remaining lower Owner keeps its own last declaration.");
            backend.OverrideTargetPresent = false; backend.Overrides.Refresh(true, 2);
            Check(lateLow.Snapshot.State == MonsterOverrideState.Rejected && backend.Overrides.Resolve("test-template", "defense", 7) == 7, "Missing target disables surviving contributions.");
            backend.OverrideTargetPresent = true; backend.OverrideBaseline = 7; backend.Overrides.Refresh(false, 3); backend.Overrides.Refresh(true, 4);
            Check(lateLow.Snapshot.State == MonsterOverrideState.Ready && lateLow.Snapshot.LoadIndex == 1 && lateLow.Snapshot.DeclarationOrder == 1 && backend.Overrides.Resolve("test-template", "defense", 7) == 99, "Readiness/generation recovery preserves declaration order.");
            lateLow.Dispose(); var replacement = apis[1].RegisterOverride(Value("late", 23)).Value!; lateLow.Dispose();
            Check(replacement.Snapshot.State == MonsterOverrideState.Ready && backend.Overrides.Resolve("test-template", "defense", 7) == 23, "An old lease cannot remove its replacement.");
            service.ReleaseOwner("PackA");
            Check(backend.Overrides.Resolve("test-template", "defense", 7) == 7 && apis[0].GetOverrides().Value!.Count == 0, "All owners withdrawn use the current native baseline.");

        }
    }
}
