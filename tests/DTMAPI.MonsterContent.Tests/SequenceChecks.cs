using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static MonsterDefinition SequenceDefinition()
    {
        var d = Definition("sequence"); d.Behavior = "configured-sequence";
        d.Sequence = new MonsterSequence { RepeatCount = 2, Steps = new List<MonsterSequenceStep> {
            new MonsterSequenceStep { Id = "out", Action = "move-to", OffsetX = 2, MaxTravel = 8, MaxGameSeconds = 2 },
            new MonsterSequenceStep { Id = "fire", AttackId = "AtkNormal", MaxGameSeconds = 2 },
            new MonsterSequenceStep { Id = "home", Action = "move-to", MaxTravel = 8, MaxGameSeconds = 2 }
        } }; return d;
    }
    static void SequenceChecks()
    {
        var d = SequenceDefinition();
        Check(MonsterAdmission.Definition("A", d, AuthorCatalog()) == null, "Configured sequence uses native capabilities without a code provider.");
        var publisher = new Publisher { Ready = true }; var registry = new DefinitionRegistry(publisher, () => { });
        registry.Register("A", d); d.Sequence!.Steps[0].OffsetX = 50;
        Check(publisher.Last!.Definition.Sequence!.Steps[0].OffsetX == 2, "Definition freezes nested sequence parameters.");
        var bad = SequenceDefinition(); bad.Sequence!.RepeatCount = 0;
        Check(MonsterValidation.Validate("A", bad)!.Field.EndsWith("repeatCount"), "Unbounded/empty repetition is rejected.");
        bad = SequenceDefinition(); bad.Sequence!.Steps[1].Id = "out";
        Check(MonsterValidation.Validate("A", bad)!.Field.EndsWith("steps[1].id"), "Step identities are unique.");
        bad = SequenceDefinition(); bad.Sequence!.Steps[0].MaxTravel = float.NaN;
        Check(MonsterValidation.Validate("A", bad)!.Field.EndsWith("maxTravel"), "Nonfinite motion limits are rejected.");
        bad = SequenceDefinition(); bad.Behavior = "inherit-native";
        Check(MonsterValidation.Validate("A", bad)!.Code == "InvalidSequence", "Inherited AI cannot accidentally execute attached sequence data.");
        bad = SequenceDefinition(); bad.Sequence!.Steps[1].AttackId = "Absent";
        Check(MonsterAdmission.Definition("A", bad, AuthorCatalog())!.Field == "behavior.sequence.steps[1].attackId", "Missing attacks report the exact step.");
        var catalog = AuthorCatalog(); catalog.Templates[0].Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, false);
        Check(MonsterAdmission.Definition("A", SequenceDefinition(), catalog)!.Code == "MovementUnsupported", "Changing to an immobile template rejects moves before publication.");

        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(SequenceDefinition());
        string payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Runtime payload preserves every step and repetition.");
        var tree = (Dictionary<string, object?>)((Dictionary<string, object?>)Json.Read(payload)!)["monsters"]!;
        string project = Json.Write(Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "A", "name", "Sequence"), "monsters", tree["definitions"], "dropPools", tree["dropPools"], "spawnRules", tree["spawnRules"], "dependencies", Array.Empty<object>(), "assets", Array.Empty<object>()));
        Check(ContentCodec.WriteContent(ContentCodec.ReadProject(project)) == payload, "Project and payload share the sequence schema.");
        try { ContentCodec.ReadContent(payload.Replace("\"attackId\":\"AtkNormal\"", "\"attackId\":\"AtkNormal\",\"offsetX\":0"), "A", _ => throw new Exception()); throw new Exception("Expected rejection"); }
        catch (AuthoringException e) { Check(e.Diagnostic.Code == "UnknownField" && e.Diagnostic.Field.EndsWith("steps[1].offsetX"), "Cross-action parameters cannot be silently ignored."); }

        var definition = SequenceDefinition(); var a = new SequenceContext(10, 5); var b = new SequenceContext(30, 9);
        var one = new ConfiguredSequence(definition.Sequence!); var two = new ConfiguredSequence(definition.Sequence!);
        one.Start(a); two.Start(b); definition.Sequence!.Steps[0].OffsetX = 99;
        one.Tick(0); Check(a.Calls.Count == 0, "Paused tick cannot admit a new action.");
        one.Tick(.1f); two.Tick(.1f);
        Check(a.LastX == 12 && b.LastX == 32 && a.LastY == 5 && b.LastY == 9, "Instances capture independent origins and immutable input.");
        for (int i = 0; i < 10; i++) one.Tick(.1f);
        Check(a.Calls.Count == 1, "A running move prevents attack overlap.");
        a.State = MonsterActionState.Cancelled; one.Tick(.1f);
        Check(one.Snapshot.State == MonsterSequenceState.Stopped && two.Snapshot.State == MonsterSequenceState.Running, "Explicit action cancellation halts only its sequence.");
        one.Stop(); one.Stop(); one.Tick(1); Check(a.Calls.Count == 1, "Repeated stop and stale ticks cannot start more actions.");
        for (int i = 0; i < 6; i++) { b.State = MonsterActionState.Completed; two.Tick(.1f); two.Tick(.1f); }
        Check(two.Snapshot.State == MonsterSequenceState.Completed && b.Calls.SequenceEqual(new[] { "move", "AtkNormal", "move", "move", "AtkNormal", "move" }), "Finite two-pass sequence executes exactly the configured order.");
        Check(b.LastX == 30 && b.LastY == 9, "Return movement always uses execution origin, without accumulated drift.");
        two.Tick(1); Check(b.Calls.Count == 6, "Completed sequences do not restart.");

        var fire = new MonsterSequence { Steps = new List<MonsterSequenceStep> { new MonsterSequenceStep { Id = "fire", AttackId = "AtkNormal", MaxGameSeconds = 1 } } };
        var waiting = new SequenceContext(0, 0) { Rejection = "TargetUnavailable" }; var bounded = new ConfiguredSequence(fire); bounded.Start(waiting);
        bounded.Tick(.5f); bounded.Tick(0); bounded.Tick(.5f); bounded.Tick(.1f);
        Check(bounded.Snapshot.State == MonsterSequenceState.Failed && bounded.Snapshot.Reason == "AdmissionTimedOut:TargetUnavailable" && waiting.Calls.Count == 2, "Target admission waiting is finite and pause consumes no budget.");
        waiting = new SequenceContext(0, 0) { Rejection = "AttackCoolingDown" }; bounded = new ConfiguredSequence(fire); bounded.Start(waiting); bounded.Tick(.5f); waiting.Rejection = ""; bounded.Tick(.1f);
        Check(waiting.LastLimit == .5f, "Admission waiting is deducted from the native action timeout.");
        waiting.State = MonsterActionState.TimedOut; bounded.Tick(.1f);
        Check(bounded.Snapshot.State == MonsterSequenceState.Failed && bounded.Snapshot.Reason == "TimedOut", "Native timeout is terminal, not successful step completion.");
        waiting = new SequenceContext(0, 0) { Rejection = "NativeActionFailed" }; bounded = new ConfiguredSequence(fire); bounded.Start(waiting); bounded.Tick(.1f); bounded.Tick(.1f);
        Check(bounded.Snapshot.Reason == "NativeActionFailed" && waiting.Calls.Count == 1, "Native faults do not retry indefinitely.");
    }
    sealed class SequenceContext : IMonsterBehaviorContext
    {
        readonly float x, y; readonly MonsterHandle instance = new MonsterHandle(Guid.NewGuid(), 1);
        MonsterActionHandle action; public string Rejection = ""; public float LastX, LastY, LastLimit;
        public List<string> Calls = new List<string>(); public MonsterActionState State;
        public SequenceContext(float x, float y) { this.x = x; this.y = y; }
        public MonsterResult<MonsterInstanceSnapshot> GetSelf() => MonsterResult<MonsterInstanceSnapshot>.Ok(new MonsterInstanceSnapshot(instance, "A:sequence", "test-template", 10, 10, x, y));
        public MonsterResult<IReadOnlyList<MonsterAttackOption>> GetAttacks() => throw new Exception("No alternate attack selection.");
        public MonsterResult<MonsterTargetSnapshot?> GetTarget() => throw new Exception("One-shot sequences do not acquire targets before movement.");
        public MonsterResult<MonsterActionHandle> Attack(string id, float limit) => Begin(id, limit);
        public MonsterResult<MonsterActionHandle> UseSkill(string id, float limit) => Begin("skill:" + id, limit);
        public MonsterResult<MonsterActionHandle> MoveTo(float x, float y, float limit, float travel) { LastX = x; LastY = y; return Begin("move", limit); }
        MonsterResult<MonsterActionHandle> Begin(string name, float limit) { Calls.Add(name); LastLimit = limit; if (Rejection != "") return MonsterResult<MonsterActionHandle>.Fail(new MonsterDiagnostic(Rejection, Rejection)); State = MonsterActionState.Running; action = new MonsterActionHandle(instance, Calls.Count); return MonsterResult<MonsterActionHandle>.Ok(action); }
        public MonsterResult<MonsterActionSnapshot> GetAction(MonsterActionHandle h) => h.Equals(action) ? MonsterResult<MonsterActionSnapshot>.Ok(new MonsterActionSnapshot(h, State, MonsterActionInterruptionReason.None, 0, 0, 0, 0, 0, 0)) : MonsterResult<MonsterActionSnapshot>.Fail(new MonsterDiagnostic("StaleAction", "wrong token"));
        public MonsterResult<bool> Cancel(MonsterActionHandle h) => throw new Exception("Native lifecycle owns cleanup after context closes.");
    }
}
