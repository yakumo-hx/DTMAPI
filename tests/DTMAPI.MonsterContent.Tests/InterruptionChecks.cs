using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    // These drive the actual finite/combat runners. The context supplies action
    // outcomes in the native advancement slot; it does not execute native skills.
    static void InterruptionTick(IConfiguredBehavior behavior, float dt, Action? advance = null)
    {
        behavior.BeforeAction(dt);
        if (dt > 0) advance?.Invoke();
        behavior.Tick(dt);
    }
    static MonsterSequence InterruptedRound() => new MonsterSequence { RepeatCount = 2, Steps = new List<MonsterSequenceStep> {
        new MonsterSequenceStep { Id = "out", Action = "move-to", OffsetX = 2, MaxTravel = 8, MaxGameSeconds = 5 },
        new MonsterSequenceStep { Id = "next", Action = "skill", SkillId = "fan", MaxGameSeconds = 5 }
    } };
    static IConfiguredBehavior StartedInterruptionBehavior(string mode, CombatContext context)
    {
        IConfiguredBehavior behavior;
        if (mode == "once") behavior = new ConfiguredSequence(InterruptedRound());
        else behavior = new CombatSequence(InterruptedRound(), new MonsterCombatPolicy { Mode = mode, RepeatIntervalGameSeconds = 2 });
        behavior.Start(context);
        if (behavior is CombatSequence combat)
        {
            combat.OnEffectiveHit();
            InterruptionTick(behavior, .1f);
        }
        InterruptionTick(behavior, .1f);
        Check(context.Starts == 1 && context.LastX == context.SelfX + 2 && behavior.SequenceSnapshot!.StepId == "out",
            mode + ": first step admits exactly once before the interruption boundary.");
        return behavior;
    }
    static void Interrupt(CombatContext context, MonsterActionInterruptionReason cause)
    { context.ActionState = MonsterActionState.Interrupted; context.InterruptionReason = cause; }
    static void InterruptionChecks()
    {
        int initial = checks;
        foreach (string mode in new[] { "once", "active", "retaliate" })
        foreach (var cause in new[] { MonsterActionInterruptionReason.TargetInvalidated, MonsterActionInterruptionReason.Hurt, MonsterActionInterruptionReason.AimUnavailable })
        foreach (bool before in new[] { true, false })
        {
            string label = mode + "/" + cause + (before ? "/BeforeAction" : "/AdvanceAction");
            var context = new CombatContext { Target = 1, SelfX = 10, SelfY = 5 };
            var behavior = StartedInterruptionBehavior(mode, context);
            var oldToken = behavior.SequenceSnapshot!.Action!.Value;
            if (before) Interrupt(context, cause);
            InterruptionTick(behavior, 100, before ? null : () => Interrupt(context, cause));
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Interrupted && behavior.SequenceSnapshot.Reason == "Interrupted:" + cause &&
                behavior.SequenceSnapshot.StepId == "out" && behavior.SequenceSnapshot.Iteration == 1 && behavior.SequenceSnapshot.Action!.Value.Equals(oldToken) && context.Starts == 1,
                label + ": the finite round reports interruption, retires its token and neither skips nor replays a step, even with large dt.");
            Check(context.GetAction(oldToken).Value!.InterruptionReason == cause && !context.Cancel(oldToken).Value && context.ActionState == MonsterActionState.Interrupted,
                label + ": the typed action cause is readable, and a late action Cancel cannot rewrite its terminal outcome.");
            int queries = context.TargetQueries;
            InterruptionTick(behavior, 0);
            if (mode == "once")
            {
                Check(!behavior.Cancel("Cancelled").Value && context.Cancels == 0, label + ": cancelling a terminal one-shot is a no-op.");
                InterruptionTick(behavior, 100); behavior.TickHurtWait(100); behavior.Stop(); InterruptionTick(behavior, 100);
                Check(context.Starts == 1 && context.TargetQueries == queries && behavior.SequenceSnapshot.State == MonsterSequenceState.Interrupted,
                    label + ": a terminal one-shot stays interrupted under later ticks, hurt and Stop.");
                continue;
            }
            var combat = (CombatSequence)behavior;
            Check(combat.CombatSnapshot.State == MonsterCombatState.WaitingInterval && combat.CombatSnapshot.RemainingGameSeconds == 2 && combat.CombatSnapshot.Round == 1,
                label + ": observation starts a full interval; the interruption tick consumes none of it.");
            context.Target = 2; context.SelfX = 30; context.SelfY = 9;
            combat.OnEffectiveHit(); InterruptionTick(behavior, 1.25f); combat.OnEffectiveHit(); InterruptionTick(behavior, 0);
            Check(combat.CombatSnapshot.RemainingGameSeconds == .75f && context.Starts == 1 && context.TargetQueries == queries,
                label + ": hits and target changes cannot shorten or bypass the interval; pause freezes it.");
            combat.OnEffectiveHit(); InterruptionTick(behavior, .75f);
            if (mode == "retaliate")
            {
                Check(combat.CombatSnapshot.State == MonsterCombatState.WaitingHit && combat.CombatSnapshot.Round == 1 && context.Starts == 1,
                    label + ": a hit before the cooldown-ending tick is discarded.");
                InterruptionTick(behavior, 100); behavior.TickHurtWait(100);
                Check(context.Starts == 1 && context.TargetQueries == queries, label + ": no new hit means no polling or automatic round.");
                combat.OnEffectiveHit(); InterruptionTick(behavior, .1f);
            }
            Check(combat.CombatSnapshot.State == MonsterCombatState.Running && combat.CombatSnapshot.Round == 2 && context.Starts == 1 &&
                context.TargetQueries == queries + 1 && combat.CombatSnapshot.Target!.Value.Revision == 2,
                label + ": after the full gate, one fresh round queries the new target without admitting in the same tick.");
            InterruptionTick(behavior, 100);
            Check(context.Starts == 2 && context.Calls[0] == "move-to" && context.Calls[1] == "move-to" && context.LastX == 32 && context.LastY == 9 &&
                behavior.SequenceSnapshot!.StepId == "out" && behavior.SequenceSnapshot.Iteration == 1 && !behavior.SequenceSnapshot.Action!.Value.Equals(oldToken),
                label + ": a new token starts at step one using the new round origin; no same-tick catch-up.");
        }
        foreach (string mode in new[] { "once", "active", "retaliate" })
        foreach (var cause in new[] { MonsterActionInterruptionReason.None, MonsterActionInterruptionReason.ExternalDisplacement, MonsterActionInterruptionReason.NativeAttackStopped, (MonsterActionInterruptionReason)99 })
        foreach (bool before in new[] { true, false })
        {
            var context = new CombatContext { Target = 1 };
            var behavior = StartedInterruptionBehavior(mode, context);
            if (before) { Interrupt(context, cause); context.Target = 0; }
            InterruptionTick(behavior, .1f, before ? null : () => { Interrupt(context, cause); context.Target = 0; });
            context.Target = 2; (behavior as CombatSequence)?.OnEffectiveHit(); InterruptionTick(behavior, 100);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Failed && behavior.SequenceSnapshot.Reason == "Interrupted:" + cause && context.Starts == 1 &&
                (behavior.CombatSnapshot == null || behavior.CombatSnapshot.State == MonsterCombatState.Failed),
                mode + "/" + cause + ": an unapproved cause fails with detail, ahead of target loss, and cannot rearm.");
        }
        InterruptionPriorityChecks();
        Console.WriteLine("PASS interruption runner checks=" + (checks - initial));
    }
    static void InterruptionPriorityChecks()
    {
        foreach (string mode in new[] { "once", "active", "retaliate" })
        foreach (var terminal in new[] { MonsterActionState.Cancelled, MonsterActionState.Failed, MonsterActionState.TimedOut, MonsterActionState.TravelLimit })
        foreach (bool before in new[] { true, false })
        {
            var context = new CombatContext { Target = 1 };
            var behavior = StartedInterruptionBehavior(mode, context);
            Action end = () => { context.ActionState = terminal; context.InterruptionReason = MonsterActionInterruptionReason.Hurt; context.Target = 0; };
            if (before) end();
            InterruptionTick(behavior, .1f, before ? null : end);
            context.Target = 2; (behavior as CombatSequence)?.OnEffectiveHit(); InterruptionTick(behavior, 100);
            var expected = terminal == MonsterActionState.Cancelled ? MonsterSequenceState.Stopped : MonsterSequenceState.Failed;
            Check(behavior.SequenceSnapshot!.State == expected && behavior.SequenceSnapshot.Reason == terminal.ToString() && context.Starts == 1 &&
                (behavior.CombatSnapshot == null || behavior.CombatSnapshot.State == (terminal == MonsterActionState.Cancelled ? MonsterCombatState.Stopped : MonsterCombatState.Failed)),
                mode + "/" + terminal + ": actual terminal state wins over a normal-cause field and simultaneous target loss.");
        }
        foreach (string mode in new[] { "once", "active", "retaliate" })
        foreach (bool before in new[] { true, false })
        {
            var context = new CombatContext { Target = 1 };
            var behavior = StartedInterruptionBehavior(mode, context);
            if (before) Interrupt(context, MonsterActionInterruptionReason.Hurt);
            else behavior.BeforeAction(.1f);
            Check(behavior.Cancel("ExplicitCancel").Value && !behavior.Cancel("ExplicitCancel").Value,
                mode + ": explicit sequence cancellation retires decisions before interruption observation.");
            Interrupt(context, MonsterActionInterruptionReason.Hurt);
            context.Target = 2; (behavior as CombatSequence)?.OnEffectiveHit(); InterruptionTick(behavior, 100);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Stopped && context.Starts == 1 && context.Cancels == (before ? 0 : 1),
                mode + ": neither a later normal outcome nor target/hit can revive explicit cancellation.");
        }
        foreach (string mode in new[] { "once", "active", "retaliate" })
        foreach (string error in new[] { "NativeActionFailed", "StaleHandle", "OwnerClosed" })
        foreach (bool before in new[] { true, false })
        {
            var context = new CombatContext { Target = 1 };
            var behavior = StartedInterruptionBehavior(mode, context);
            Interrupt(context, MonsterActionInterruptionReason.Hurt);
            if (before) { context.ActionError = error; InterruptionTick(behavior, .1f); }
            else { context.ActionState = MonsterActionState.Running; behavior.BeforeAction(.1f); Interrupt(context, MonsterActionInterruptionReason.Hurt); context.ActionError = error; behavior.Tick(.1f); }
            (behavior as CombatSequence)?.OnEffectiveHit(); InterruptionTick(behavior, 100);
            Check(behavior.SequenceSnapshot!.State == MonsterSequenceState.Failed && behavior.SequenceSnapshot.Reason == error && context.Starts == 1 &&
                (behavior.CombatSnapshot == null || behavior.CombatSnapshot.State == MonsterCombatState.Failed),
                mode + "/" + error + ": query/cleanup failure or revoked handle cannot be mistaken for normal interruption.");
        }
        foreach (string mode in new[] { "active", "retaliate" })
        {
            var actionContext = new CombatContext { Target = 1 };
            var actionCancelled = (CombatSequence)StartedInterruptionBehavior(mode, actionContext);
            Check(actionContext.Cancel(actionCancelled.SequenceSnapshot!.Action!.Value).Value,
                mode + ": cancelling the live current action succeeds through the context API.");
            actionContext.InterruptionReason = MonsterActionInterruptionReason.Hurt; actionContext.Target = 0;
            actionCancelled.OnEffectiveHit(); InterruptionTick(actionCancelled, 100); actionContext.Target = 2; actionCancelled.OnEffectiveHit(); InterruptionTick(actionCancelled, 100);
            Check(actionCancelled.CombatSnapshot.State == MonsterCombatState.Stopped && actionCancelled.SequenceSnapshot!.State == MonsterSequenceState.Stopped && actionContext.Starts == 1,
                mode + ": successful action cancellation wins over target loss, hits and a normal-cause field.");

            var context = new CombatContext { Target = 1 };
            var behavior = (CombatSequence)StartedInterruptionBehavior(mode, context);
            context.TargetError = "NativeActionFailed"; InterruptionTick(behavior, .1f);
            Interrupt(context, MonsterActionInterruptionReason.Hurt); context.TargetError = ""; behavior.OnEffectiveHit(); InterruptionTick(behavior, 100);
            Check(behavior.CombatSnapshot.State == MonsterCombatState.Failed && behavior.CombatSnapshot.Reason == "NativeActionFailed" && context.Starts == 1 && context.Cancels == 1,
                mode + ": a real target query fault cancels and stays failed after a later normal outcome.");

            context = new CombatContext { Target = 1 }; behavior = (CombatSequence)StartedInterruptionBehavior(mode, context);
            context.CancelFailure = true;
            var cancelled = behavior.Cancel("ExplicitCancel");
            Interrupt(context, MonsterActionInterruptionReason.Hurt); behavior.OnEffectiveHit(); InterruptionTick(behavior, 100);
            Check(!cancelled.Success && cancelled.Diagnostic!.Code == "NativeActionFailed" && behavior.CombatSnapshot.State == MonsterCombatState.Failed &&
                behavior.SequenceSnapshot!.State == MonsterSequenceState.Failed && context.Starts == 1,
                mode + ": failed native cleanup is not reported as successful cancellation or a rearmable interruption.");

            context = new CombatContext { Target = 1 }; behavior = (CombatSequence)StartedInterruptionBehavior(mode, context);
            Interrupt(context, MonsterActionInterruptionReason.Hurt); behavior.TickHurtWait(100);
            Check(behavior.CombatSnapshot.State == MonsterCombatState.Running && context.Starts == 1,
                mode + ": hurt fixed steps cannot start an interval before the runner observes an accepted action's interruption.");
            InterruptionTick(behavior, .1f);
            Check(behavior.CombatSnapshot.RemainingGameSeconds == 2, mode + ": observation after hurt starts the full interval without wall-clock compensation.");
            Check(behavior.Cancel("ExplicitCancel").Value && behavior.CombatSnapshot.State == MonsterCombatState.Stopped && behavior.SequenceSnapshot!.State == MonsterSequenceState.Interrupted,
                mode + ": CancelSequence can stop the waiting strategy while preserving its already terminal round.");

            context = new CombatContext { Target = 1 }; behavior = (CombatSequence)StartedInterruptionBehavior(mode, context);
            Interrupt(context, MonsterActionInterruptionReason.Hurt); InterruptionTick(behavior, .1f); behavior.Stop(); behavior.OnEffectiveHit(); InterruptionTick(behavior, 100);
            var freshContext = new CombatContext { Target = 2 };
            var fresh = new CombatSequence(InterruptedRound(), new MonsterCombatPolicy { Mode = mode }); fresh.Start(freshContext); InterruptionTick(fresh, .1f);
            Check(context.Starts == 1 && behavior.CombatSnapshot.State == MonsterCombatState.Stopped && freshContext.Starts == 0 &&
                fresh.CombatSnapshot.State == (mode == "retaliate" ? MonsterCombatState.WaitingHit : MonsterCombatState.Running),
                mode + ": Stop retires old waiting and hit state; a new execution has no inherited trigger or action.");
        }
        var hurtContext = new CombatContext { Target = 1 };
        var hurtRetaliation = (CombatSequence)StartedInterruptionBehavior("retaliate", hurtContext);
        Interrupt(hurtContext, MonsterActionInterruptionReason.Hurt); InterruptionTick(hurtRetaliation, .1f);
        hurtRetaliation.OnEffectiveHit(); hurtRetaliation.TickHurtWait(1); hurtRetaliation.TickHurtWait(0);
        Check(hurtRetaliation.CombatSnapshot.RemainingGameSeconds == 1 && hurtContext.Starts == 1,
            "Retaliation: after observation, hurt fixed steps consume the existing cooldown once; pause does not.");
        hurtRetaliation.OnEffectiveHit(); hurtRetaliation.TickHurtWait(1); InterruptionTick(hurtRetaliation, 100);
        Check(hurtRetaliation.CombatSnapshot.State == MonsterCombatState.WaitingHit && hurtContext.Starts == 1,
            "Retaliation: hurt recovery keeps the new-hit gate and does not queue cooldown hits.");
    }
}
