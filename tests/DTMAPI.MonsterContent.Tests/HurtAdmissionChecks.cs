using System;
using DTMAPI.MonsterContent;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static void HurtAdmissionChecks()
    {
        foreach (string mode in new[] { "retaliate", "active" })
        foreach (string rejection in new[] { "", "TargetOutOfRange", "AttackCoolingDown" })
        {
            var context = new CombatContext { Target = 1, Rejection = rejection };
            var behavior = new CombatSequence(CombatSequence(), new MonsterCombatPolicy { Mode = mode });
            behavior.Start(context); behavior.OnEffectiveHit(); CombatTick(behavior, .02f);
            if (rejection != "") CombatTick(behavior, .1f);
            int attempts = context.Starts;
            for (int i = 0; i < 500; i++) { behavior.OnEffectiveHit(); behavior.TickHurtWait(.02f); }
            Check(behavior.CombatSnapshot.State == MonsterCombatState.Failed && behavior.SequenceSnapshot!.Reason == "AdmissionTimedOut:HurtLocked" && context.Starts == attempts,
                mode + ": ten seconds of hurt exhaust a one-second unaccepted step without admitting an action (" + rejection + ").");
            context.Rejection = ""; behavior.OnEffectiveHit(); CombatTick(behavior, .02f); CombatTick(behavior, 100);
            Check(behavior.CombatSnapshot.State == MonsterCombatState.Failed && context.Starts == attempts && behavior.CombatSnapshot.Round == 1,
                mode + ": hurt recovery and later hits cannot resurrect the expired round (" + rejection + ").");
        }
        foreach (string mode in new[] { "once", "active", "retaliate" })
        {
            var context = new CombatContext { Target = 1, Rejection = "TargetOutOfRange" };
            IConfiguredBehavior behavior = WaitingBehavior(mode, context);
            behavior.Tick(.25f);
            for (int i = 0; i < 50; i++) { behavior.TickHurtWait(0); behavior.Tick(0); }
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Running && context.Starts == 1,
                mode + ": pause does not spend admission budget or attempt an action.");
            behavior.TickHurtWait(.25f);
            behavior.TickHurtWait(.25f); // Native recovery tick still skips AI.
            Check(context.Starts == 1 && behavior.SequenceSnapshot!.State == MonsterSequenceState.Running,
                mode + ": no admission occurs in hurt or its recovery tick.");
            context.Rejection = ""; behavior.Tick(.02f);
            Check(context.Starts == 2 && Math.Abs(context.LastLimit - .25f) < .00001f,
                mode + ": range wait plus both hurt ticks are deducted exactly once from the accepted action limit.");
            behavior.TickHurtWait(10); behavior.Tick(.02f);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Running && context.Starts == 2 && context.Cancels == 0,
                mode + ": the sequence cannot spend the accepted action's separately owned native budget again.");
            context.ActionState = MonsterActionState.TimedOut; behavior.BeforeAction(.02f); behavior.Tick(.02f);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Failed && behavior.SequenceSnapshot.Reason == "TimedOut",
                mode + ": the native action owner still decides an accepted action's timeout.");

            context = new CombatContext { Target = 1 };
            behavior = WaitingBehavior(mode, context);
            for (int i = 0; i < 4; i++) behavior.TickHurtWait(.25f);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Failed && context.Starts == 0,
                mode + ": an unaccepted first step expires during hurt at its budget, before recovery.");
            behavior.Tick(.02f); behavior.TickHurtWait(10);
            Check(context.Starts == 0 && !behavior.Cancel("Cancelled").Value,
                mode + ": an expired first step stays terminal after recovery and cancellation.");
        }
        var waiting = new CombatContext { Target = 1, Rejection = "TargetOutOfRange" };
        var once = WaitingBehavior("once", waiting); once.Tick(.1f); once.TickHurtWait(10);
        waiting.Rejection = ""; once.Tick(.02f);
        Check(once.SequenceSnapshot!.State == MonsterSequenceState.Failed && waiting.Starts == 1,
            "A run-once step already waiting for range also expires in hurt and cannot later attack.");

        var a = new CombatContext { Target = 1 }; var b = new CombatContext { Target = 1 };
        var one = WaitingBehavior("retaliate", a); var two = WaitingBehavior("retaliate", b);
        one.TickHurtWait(1); two.TickHurtWait(.25f); two.Tick(.02f);
        Check(one.SequenceSnapshot!.State == MonsterSequenceState.Failed && b.Starts == 1 && b.LastLimit == .75f && a.Starts == 0,
            "Admission waiting and timeout remain local to each instance.");

        var sequence = CombatSequence(); sequence.Steps.Add(new MonsterSequenceStep { Id = "second", AttackId = "AtkNormal", MaxGameSeconds = 1 });
        var steps = new ConfiguredSequence(sequence); var contextSteps = new CombatContext { Target = 1 };
        steps.Start(contextSteps); steps.TickHurtWait(.75f); steps.Tick(.02f);
        contextSteps.ActionState = MonsterActionState.Completed; steps.Tick(.02f); steps.TickHurtWait(.25f); steps.Tick(.02f);
        Check(contextSteps.Starts == 2 && contextSteps.LastLimit == .75f,
            "A completed action advances once; the next step owns a fresh admission budget.");
    }
    static IConfiguredBehavior WaitingBehavior(string mode, CombatContext context)
    {
        if (mode == "once") { var once = new ConfiguredSequence(CombatSequence()); once.Start(context); return once; }
        var combat = new CombatSequence(CombatSequence(), new MonsterCombatPolicy { Mode = mode });
        combat.Start(context); combat.OnEffectiveHit(); CombatTick(combat, .02f); return combat;
    }
}
