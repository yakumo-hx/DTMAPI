using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static void CombatTick(CombatSequence behavior, float dt) { behavior.BeforeAction(dt); behavior.Tick(dt); }
    static MonsterSequence CombatSequence() => new MonsterSequence { Steps = new List<MonsterSequenceStep> {
        new MonsterSequenceStep { Id = "fire", AttackId = "AtkNormal", MaxGameSeconds = 1 }
    } };
    static void CombatChecks()
    {
        var definition = SequenceDefinition(); definition.Combat = new MonsterCombatPolicy();
        Check(MonsterAdmission.Definition("A", definition, AuthorCatalog()) == null, "Active policy consumes the existing sequence capability admission.");
        var publisher = new Publisher { Ready = true }; new DefinitionRegistry(publisher, () => { }).Register("A", definition);
        definition.Combat.RepeatIntervalGameSeconds = 4;
        Check(publisher.Last!.Definition.Combat!.RepeatIntervalGameSeconds == 2, "Registered policy cannot be mutated by its author.");
        foreach (float invalid in new[] { 0, -1, float.NaN, float.PositiveInfinity })
        {
            definition.Combat.CheckIntervalGameSeconds = invalid;
            Check(MonsterValidation.Validate("A", definition)!.Field == "behavior.combat.checkIntervalGameSeconds", "Invalid target polling interval is rejected: " + invalid);
        }
        definition.Combat = new MonsterCombatPolicy { RepeatIntervalGameSeconds = 0 };
        Check(MonsterValidation.Validate("A", definition)!.Field.EndsWith("repeatIntervalGameSeconds"), "Zero-delay automatic repetition is rejected.");
        definition.Combat = new MonsterCombatPolicy { Mode = "unknown" };
        Check(MonsterValidation.Validate("A", definition)!.Field == "behavior.combat.mode", "Unknown policies cannot silently become active.");
        definition.Combat.Mode = "active"; definition.Sequence = null; definition.Behavior = "inherit-native";
        Check(MonsterValidation.Validate("A", definition)!.Code == "InvalidCombatPolicy", "Native inheritance cannot carry active policy data.");
        definition = SequenceDefinition(); definition.Combat = new MonsterCombatPolicy();
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(definition);
        string payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Active policy roundtrips through the actual strict runtime payload.");
        try { ContentCodec.ReadContent(payload.Replace("\"mode\":\"active\"", "\"mode\":\"active\",\"restartOnCancel\":true"), "A", _ => throw new Exception()); throw new Exception("Expected rejection"); }
        catch (AuthoringException e) { Check(e.Diagnostic.Code == "UnknownField" && e.Diagnostic.Field == "behavior.combat.restartOnCancel", "Unknown trigger rules cannot be silently discarded."); }

        var policy = new MonsterCombatPolicy(); var ctx = new CombatContext(); var active = new CombatSequence(CombatSequence(), policy);
        active.Start(ctx); policy.CheckIntervalGameSeconds = .1f;
        for (int i = 0; i < 20; i++) CombatTick(active, .1f);
        Check(ctx.Starts == 0 && ctx.TargetQueries >= 3 && ctx.TargetQueries <= 5 && active.CombatSnapshot.Round == 0,
            "No target means bounded polling, no empty rounds or action admissions; policy input is frozen.");
        int queries = ctx.TargetQueries; CombatTick(active, 0);
        Check(ctx.TargetQueries == queries, "Paused target waiting does not query or advance time.");
        ctx.Target = 1; CombatTick(active, .6f);
        Check(active.CombatSnapshot.State == MonsterCombatState.Running && active.CombatSnapshot.Round == 1 && ctx.Starts == 0, "A native target starts one finite round, with no same-tick action catch-up.");
        CombatTick(active, .1f); ctx.TargetX = 9; CombatTick(active, .1f);
        Check(ctx.Starts == 1 && ctx.Cancels == 0, "Moving the same target does not restart or cancel its round.");
        ctx.Target = 0; CombatTick(active, .1f);
        Check(active.CombatSnapshot.State == MonsterCombatState.WaitingInterval && active.CombatSnapshot.Reason == "TargetLost" && ctx.Cancels == 1 && active.CombatSnapshot.RemainingGameSeconds == 2,
            "Target loss cancels before native action advance and begins a full interval.");
        ctx.Target = 2; CombatTick(active, 1); CombatTick(active, 0);
        Check(ctx.Starts == 1 && active.CombatSnapshot.Round == 1, "Reacquisition and pause cannot bypass the interval.");
        CombatTick(active, 1); CombatTick(active, .1f);
        Check(active.CombatSnapshot.Round == 2 && ctx.Starts == 2 && active.CombatSnapshot.Target!.Value.Revision == 2, "After the interval, a fresh target starts a new finite round.");
        ctx.Target = 3; CombatTick(active, .1f);
        Check(active.CombatSnapshot.Reason == "TargetChanged" && ctx.Cancels == 2, "Target identity change cancels the old round without redirecting its running attack.");
        CombatTick(active, 100); CombatTick(active, .1f); ctx.ActionState = MonsterActionState.Completed; CombatTick(active, .1f);
        Check(active.CombatSnapshot.State == MonsterCombatState.WaitingInterval && active.CombatSnapshot.Reason == "Completed" && active.CombatSnapshot.Round == 3,
            "Successful completion waits; an oversized tick never starts multiple rounds.");
        float left = active.CombatSnapshot.RemainingGameSeconds; CombatTick(active, 0);
        Check(active.CombatSnapshot.RemainingGameSeconds == left, "Pause freezes the repeat interval.");
        Check(active.Cancel("Cancelled").Value && !active.Cancel("Cancelled").Value, "Waiting interval can be cancelled once even with no running action.");
        queries = ctx.TargetQueries; int starts = ctx.Starts; CombatTick(active, 100); active.Stop(); CombatTick(active, 100);
        Check(ctx.Starts == starts && ctx.TargetQueries == queries && active.CombatSnapshot.State == MonsterCombatState.Stopped, "Cancelled execution cannot be revived by old ticks or target changes.");

        var a = new CombatContext { Target = 1 }; var b = new CombatContext { Target = 1 };
        var one = new CombatSequence(CombatSequence(), new MonsterCombatPolicy()); var two = new CombatSequence(CombatSequence(), new MonsterCombatPolicy());
        one.Start(a); two.Start(b); CombatTick(one, .1f); CombatTick(two, .1f); CombatTick(one, .1f); CombatTick(two, .1f);
        a.ActionState = MonsterActionState.Cancelled; a.Target = 0; CombatTick(one, .1f);
        b.ActionState = MonsterActionState.Completed; CombatTick(two, .1f); CombatTick(two, 2); CombatTick(two, .1f);
        Check(one.CombatSnapshot.State == MonsterCombatState.Stopped && a.Starts == 1 && b.Starts == 2,
            "Explicit action cancellation wins over target loss; another instance continues repeating independently.");
        b.ActionState = MonsterActionState.TimedOut; b.Target = 0; CombatTick(two, .1f); CombatTick(two, 100);
        Check(two.CombatSnapshot.State == MonsterCombatState.Failed && two.CombatSnapshot.Reason == "TimedOut" && b.Starts == 2, "Native timeout remains terminal even when target disappears at the same boundary.");

        ctx = new CombatContext { Target = 1, Rejection = "TargetOutOfRange" }; active = new CombatSequence(CombatSequence(), new MonsterCombatPolicy()); active.Start(ctx);
        CombatTick(active, .1f); CombatTick(active, .5f); CombatTick(active, .5f); CombatTick(active, .1f); CombatTick(active, 100);
        Check(active.CombatSnapshot.State == MonsterCombatState.Failed && active.CombatSnapshot.Reason == "AdmissionTimedOut:TargetOutOfRange" && ctx.Starts == 2,
            "An acquired but unreachable target has a finite step budget and no failed-round retry loop.");
        ctx = new CombatContext(); active = new CombatSequence(CombatSequence(), new MonsterCombatPolicy()); active.Start(ctx); active.Cancel("Cancelled"); ctx.Target = 1; CombatTick(active, 100);
        Check(ctx.Starts == 0 && ctx.TargetQueries == 0, "Cancel before the first target check never starts a round.");
        ctx = new CombatContext { Target = 1 }; active = new CombatSequence(CombatSequence(), new MonsterCombatPolicy()); active.Start(ctx);
        CombatTick(active, .1f); CombatTick(active, .1f); ctx.CancelFailure = true; ctx.Target = 0; CombatTick(active, .1f); CombatTick(active, 100);
        Check(active.CombatSnapshot.State == MonsterCombatState.Failed && active.CombatSnapshot.Reason == "NativeActionFailed" && ctx.Starts == 1, "Native cleanup failure never becomes a rearmable target loss.");

        bool live = true; var backend = new Backend(); using var service = new MonsterModuleFixture(() => backend);
        var api = (IMonsterContentApi)service.Api(new Manifest("A"), () => { if (!live) throw new InvalidOperationException(); });
        Check(api.CancelSequence(default).Diagnostic!.Code == "ModuleNotRequested", "CancelSequence cannot activate a module implicitly.");
        using var demand = api.RequestModule().Value!; api.CancelSequence(default);
        Check(backend.Owners.Count == 1 && backend.Owners[0] == "A", "CancelSequence passes the authenticated owner to the native boundary.");
        live = false;
        Check(api.CancelSequence(default).Diagnostic!.Code == "OwnerClosed" && backend.Owners.Count == 1, "Stale owner cannot send a late cancellation.");
        ctx = new CombatContext { TargetError = "TargetingUnsupported" }; active = new CombatSequence(CombatSequence(), new MonsterCombatPolicy()); active.Start(ctx);
        CombatTick(active, .1f); CombatTick(active, 100);
        Check(active.CombatSnapshot.State == MonsterCombatState.Failed && active.CombatSnapshot.Reason == "TargetingUnsupported" && ctx.TargetQueries == 1 && ctx.Starts == 0,
            "A template without a native target provider fails once, rather than polling an impossible target forever.");
        RetaliationChecks();
        HurtAdmissionChecks();
    }
    static void RetaliationChecks()
    {
        var definition = SequenceDefinition(); definition.Combat = new MonsterCombatPolicy { Mode = "retaliate", TargetWaitGameSeconds = 3 };
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(definition);
        string payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Retaliation with its target deadline roundtrips through strict payload.");
        foreach (float invalid in new[] { 0, -1, float.NaN, float.PositiveInfinity })
        { definition.Combat.TargetWaitGameSeconds = invalid; Check(MonsterValidation.Validate("A", definition)!.Field == "behavior.combat.targetWaitGameSeconds", "Invalid retaliation window is rejected: " + invalid); }
        definition.Combat = new MonsterCombatPolicy { Mode = "passive" }; definition.Sequence = null;
        Check(MonsterAdmission.Definition("A", definition, AuthorCatalog()) == null, "Passive definitions need no unused action sequence or capability.");
        payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Sequence-free passive policy roundtrips through strict payload.");

        var policy = new MonsterCombatPolicy { Mode = "retaliate", TargetWaitGameSeconds = 3 };
        var a = new CombatContext { Target = 1 }; var one = new CombatSequence(CombatSequence(), policy); one.Start(a);
        CombatTick(one, 100);
        Check(one.CombatSnapshot.State == MonsterCombatState.WaitingHit && one.CombatSnapshot.Round == 0 && a.TargetQueries == 0 && a.Starts == 0, "Sight alone never triggers retaliation or idle polling.");
        one.OnEffectiveHit(); CombatTick(one, .1f); one.OnEffectiveHit(); CombatTick(one, .1f);
        var token = one.SequenceSnapshot!.Action;
        for (int i = 0; i < 5; i++) { one.OnEffectiveHit(); CombatTick(one, .1f); }
        Check(one.CombatSnapshot.Round == 1 && a.Starts == 1 && one.SequenceSnapshot!.Action.Equals(token), "A burst of effective hits cannot overlap or restart the current action.");
        a.ActionState = MonsterActionState.Completed; CombatTick(one, .1f); one.OnEffectiveHit(); CombatTick(one, 1); one.OnEffectiveHit(); CombatTick(one, 1);
        Check(one.CombatSnapshot.State == MonsterCombatState.WaitingHit && one.CombatSnapshot.Round == 1 && a.Starts == 1, "Completion disarms after cooldown; hits during cooldown leave no queued round.");
        CombatTick(one, 100); Check(a.Starts == 1, "Still seeing the player does not continue retaliation without a new hit.");
        one.OnEffectiveHit(); CombatTick(one, .1f); CombatTick(one, .1f);
        Check(one.CombatSnapshot.Round == 2 && a.Starts == 2, "A new hit after cooldown starts exactly one further round.");
        one.Cancel("Cancelled"); one.OnEffectiveHit(); CombatTick(one, 100);
        Check(one.CombatSnapshot.State == MonsterCombatState.Stopped && a.Cancels == 1 && a.Starts == 2, "Explicit cancellation wins over later hits and stale ticks.");

        a = new CombatContext(); one = new CombatSequence(CombatSequence(), policy); one.Start(a); one.OnEffectiveHit();
        CombatTick(one, 1); float remaining = one.CombatSnapshot.RemainingGameSeconds;
        one.OnEffectiveHit(); CombatTick(one, 0);
        Check(one.CombatSnapshot.RemainingGameSeconds == remaining && a.TargetQueries == 1, "Paused pending hits neither consume nor refresh the deadline.");
        CombatTick(one, 1); one.OnEffectiveHit(); CombatTick(one, 1);
        Check(one.CombatSnapshot.State == MonsterCombatState.WaitingInterval && one.CombatSnapshot.Reason == "TargetWaitExpired" && one.CombatSnapshot.Round == 0 && a.Starts == 0, "Repeated hits cannot extend an unavailable target indefinitely.");
        a.Target = 1; one.OnEffectiveHit(); CombatTick(one, 2); CombatTick(one, 100);
        Check(one.CombatSnapshot.State == MonsterCombatState.WaitingHit && a.Starts == 0, "A target acquired after the deadline cannot revive the discarded trigger.");
        one.OnEffectiveHit(); one.Cancel("Cancelled"); one.OnEffectiveHit(); CombatTick(one, 100);
        Check(one.CombatSnapshot.State == MonsterCombatState.Stopped && a.Starts == 0, "Cancellation also retires a not-yet-consumed hit.");

        a = new CombatContext { Target = 1 }; one = new CombatSequence(CombatSequence(), policy); one.Start(a); one.OnEffectiveHit();
        for (int i = 0; i < 3; i++) { one.OnEffectiveHit(); one.TickHurtWait(1); }
        Check(one.CombatSnapshot.Reason == "TargetWaitExpired" && a.Starts == 0 && a.TargetQueries == 0, "Native hurt lock consumes the target window without starting a round or extending it with repeated hits.");
        remaining = one.CombatSnapshot.RemainingGameSeconds; one.TickHurtWait(0);
        Check(one.CombatSnapshot.RemainingGameSeconds == remaining, "Paused hurt lock cannot consume cooldown.");
        one.TickHurtWait(2); one.OnEffectiveHit(); CombatTick(one, .1f); CombatTick(one, .1f);
        Check(one.CombatSnapshot.Round == 1 && a.Starts == 1, "Cooldown elapses during native hurt lock, then a fresh hit can trigger normally.");

        a = new CombatContext(); one = new CombatSequence(CombatSequence(), policy); one.Start(a); one.OnEffectiveHit(); CombatTick(one, .1f);
        a.Target = 1; CombatTick(one, .5f); CombatTick(one, .1f);
        Check(a.Starts == 1 && one.CombatSnapshot.Round == 1, "Natural target acquisition within the hit window permits one round.");
        a.Target = 0; CombatTick(one, .1f); a.Target = 1; one.OnEffectiveHit(); CombatTick(one, 2); CombatTick(one, 1);
        Check(one.CombatSnapshot.State == MonsterCombatState.WaitingHit && a.Cancels == 1 && a.Starts == 1, "Target loss cancels the old action and requires a fresh hit after cooldown.");

        var b = new CombatContext { Target = 1 }; var two = new CombatSequence(CombatSequence(), policy); two.Start(b);
        one.OnEffectiveHit(); two.OnEffectiveHit(); CombatTick(one, .1f); CombatTick(two, .1f); CombatTick(one, .1f); CombatTick(two, .1f);
        a.ActionState = MonsterActionState.Cancelled; a.Target = 0; one.OnEffectiveHit(); CombatTick(one, .1f);
        b.ActionState = MonsterActionState.Completed; CombatTick(two, .1f); CombatTick(two, 2); two.OnEffectiveHit(); CombatTick(two, .1f); CombatTick(two, .1f);
        Check(one.CombatSnapshot.State == MonsterCombatState.Stopped && b.Starts == 2, "An action cancel plus same-frame hit and target loss cannot stop or rearm the other instance.");
        b.ActionState = MonsterActionState.TimedOut; two.OnEffectiveHit(); CombatTick(two, .1f); two.OnEffectiveHit(); CombatTick(two, 100);
        Check(two.CombatSnapshot.State == MonsterCombatState.Failed && two.CombatSnapshot.Reason == "TimedOut" && b.Starts == 2, "Timeout remains terminal under repeated hits.");
        two.Stop(); two.OnEffectiveHit(); CombatTick(two, 100); Check(b.Starts == 2, "Retired behavior objects cannot receive another hit lease.");
        b = new CombatContext { Target = 1 }; two = new CombatSequence(null, new MonsterCombatPolicy { Mode = "passive" }); two.Start(b);
        for (int i = 0; i < 4; i++) { two.OnEffectiveHit(); CombatTick(two, 100); }
        Check(two.CombatSnapshot.State == MonsterCombatState.Passive && two.SequenceSnapshot == null && b.Starts == 0 && b.TargetQueries == 0, "Passive is idle even when hurt and is distinct from run-once.");
        Check(two.Cancel("Cancelled").Value && !two.Cancel("Cancelled").Value, "Passive execution can still be terminated idempotently.");
    }
    sealed class CombatContext : IMonsterBehaviorContext
    {
        readonly MonsterHandle instance = new MonsterHandle(Guid.NewGuid(), 1);
        MonsterActionHandle action;
        public int Target, TargetQueries, Starts, Cancels;
        public float TargetX, LastLimit, SelfX, SelfY, LastX, LastY;
        public readonly List<string> Calls = new List<string>();
        public string Rejection = "";
        public string TargetError = "";
        public string ActionError = "";
        public bool CancelFailure;
        public MonsterActionState ActionState;
        public MonsterActionInterruptionReason InterruptionReason;
        public MonsterResult<MonsterTargetSnapshot?> GetTarget() { TargetQueries++; return TargetError.Length > 0 ? MonsterResult<MonsterTargetSnapshot?>.Fail(new MonsterDiagnostic(TargetError, TargetError)) : MonsterResult<MonsterTargetSnapshot?>.Ok(Target == 0 ? null : new MonsterTargetSnapshot(new MonsterTargetHandle(instance, Target), TargetX, 0)); }
        public MonsterResult<MonsterInstanceSnapshot> GetSelf() => MonsterResult<MonsterInstanceSnapshot>.Ok(new MonsterInstanceSnapshot(instance, "A:active", "test", 10, 10, SelfX, SelfY));
        public MonsterResult<IReadOnlyList<MonsterAttackOption>> GetAttacks() => throw new NotSupportedException();
        public MonsterResult<MonsterActionHandle> UseSkill(string id, float limit) => Attack(id, limit);
        public MonsterResult<MonsterActionHandle> Attack(string id, float limit)
        {
            Starts++; Calls.Add(id); LastLimit = limit;
            if (Rejection != "") return MonsterResult<MonsterActionHandle>.Fail(new MonsterDiagnostic(Rejection, Rejection));
            ActionState = MonsterActionState.Running; InterruptionReason = MonsterActionInterruptionReason.None;
            action = new MonsterActionHandle(instance, Starts); return MonsterResult<MonsterActionHandle>.Ok(action);
        }
        public MonsterResult<MonsterActionHandle> MoveTo(float x, float y, float limit, float travel) { LastX = x; LastY = y; return Attack("move-to", limit); }
        public MonsterResult<MonsterActionSnapshot> GetAction(MonsterActionHandle h) => ActionError.Length > 0
            ? MonsterResult<MonsterActionSnapshot>.Fail(new MonsterDiagnostic(ActionError, ActionError))
            : h.Equals(action) ? MonsterResult<MonsterActionSnapshot>.Ok(new MonsterActionSnapshot(h, ActionState, InterruptionReason, 0, 0, 0, 0, 0, 0)) : MonsterResult<MonsterActionSnapshot>.Fail(new MonsterDiagnostic("StaleAction", "wrong token"));
        public MonsterResult<bool> Cancel(MonsterActionHandle h)
        {
            if (CancelFailure) return MonsterResult<bool>.Fail(new MonsterDiagnostic("NativeActionFailed", "cleanup failed"));
            if (!h.Equals(action)) return MonsterResult<bool>.Fail(new MonsterDiagnostic("StaleAction", "wrong token"));
            if (ActionState != MonsterActionState.Running) return MonsterResult<bool>.Ok(false);
            Cancels++; ActionState = MonsterActionState.Cancelled; return MonsterResult<bool>.Ok(true);
        }
    }
}
