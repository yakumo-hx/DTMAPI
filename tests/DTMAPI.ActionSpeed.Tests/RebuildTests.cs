using System;
using Yuuka.DTMAPI.ActionSpeed;

namespace DTMAPI.UnitTests;

internal static class RebuildTests
{
    private static int checks;
    public static void RunAll()
    {
        CategoriesFollowOperations();
        ControlsAreIndependent();
        AutoFillNeverCatchesUpOrBypassesEligibility();
        ContinuousFoodKeepsNativeCadence();
        AnimationOwnershipSurvivesLoopsAndInterruption();
        AnimationRecoveryRetriesEachClock();
        checks += LifecycleTests.RunAll();
        checks += NativeCallbackTests.RunAll();
        checks += ConfigMigrationTests.RunAll();
        Console.WriteLine("ActionSpeed rebuild: " + checks + " behavioral assertions passed.");
    }

    private static void CategoriesFollowOperations()
    {
        Equal(ActionKind.Tool, ActionRules.Classify(NativeAction.ManualWater), "can watering is a tool action");
        Equal(ActionKind.Fill, ActionRules.Classify(NativeAction.CanRefill), "can refill uses filling control");
        Equal(ActionKind.Fill, ActionRules.Classify(NativeAction.BottleFill), "bottling uses filling control");
        Equal(ActionKind.None, ActionRules.Classify(NativeAction.Other), "nearby water container alone is irrelevant");
        Equal(ActionKind.None, ActionRules.Classify(NativeAction.CropInteract, false), "crop inspection/bottled watering is not harvest");
        Equal(ActionKind.Gather, ActionRules.Classify(NativeAction.CropInteract, true), "mature crop harvest");
        Equal(ActionKind.Gather, ActionRules.Classify(NativeAction.Resin), "resin collection");
        Equal(ActionKind.Interact, ActionRules.Classify(NativeAction.Pet), "petting");
        Equal(ActionKind.Interact, ActionRules.Classify(NativeAction.Milk), "taking milk is an animal interaction, independent of water filling");
        Equal(ActionKind.Plant, ActionRules.Classify(NativeAction.Film), "crop film belongs to planting");
        Equal(ActionKind.Plant, ActionRules.Classify(NativeAction.FarmingGun), "native planting gun, not a chop tool");
        var scope = new ActionScope();
        var outer = scope.Push(ActionKind.Gather);
        var inner = scope.Push(ActionKind.Plant);
        Equal(ActionKind.Plant, scope.Current, "nested actual operation wins");
        scope.Restore(inner);
        Equal(ActionKind.Gather, scope.Current, "inner finalizer restores caller");
        scope.Restore(outer);
        Equal(ActionKind.None, scope.Current, "rejected/throwing operation cannot leak classification");
    }

    private static void ControlsAreIndependent()
    {
        var c = new ActionConfig();
        foreach (ActionKind kind in new[] { ActionKind.Tool, ActionKind.Fill, ActionKind.Eat, ActionKind.Interact, ActionKind.Gather, ActionKind.Plant })
            Near(3, c.Factor(kind), "six default multipliers");
        c.ToolMultiplier = 2; c.FillMultiplier = 4; c.EatMultiplier = 1.5;
        c.InteractMultiplier = 2.5; c.GatherMultiplier = 3.5; c.PlantMultiplier = 1;
        Near(2, c.Factor(ActionKind.Tool), "independent tool multiplier");
        Near(4, c.Factor(ActionKind.Fill), "independent fill multiplier");
        c.FillEnabled = false; c.AutoFill = true; c.StrongAutoFill = true;
        Near(1, c.Factor(ActionKind.Fill), "auto and strong must not enable fill speed");
        c.EatEnabled = false; c.ContinuousDrink = true;
        Near(1, c.Factor(ActionKind.Eat), "drink cadence must not enable animation speed");
        c.ToolMultiplier = double.NaN; c.InteractMultiplier = double.PositiveInfinity;
        c.GatherMultiplier = -4; c.PlantMultiplier = 100;
        Near(3, c.Factor(ActionKind.Tool), "NaN config falls back");
        Near(3, c.Factor(ActionKind.Interact), "infinite config falls back");
        Near(1, c.Factor(ActionKind.Gather), "minimum enforced");
        Near(4, c.Factor(ActionKind.Plant), "maximum enforced");
    }

    private static void AutoFillNeverCatchesUpOrBypassesEligibility()
    {
        var timer = new FillSchedule();
        Equal(false, timer.Tick(0.1f, false, false), "no eligible native operation");
        Equal(true, timer.Tick(0.01f, true, false), "first eligible attempt");
        Equal(false, timer.Tick(0.12f, true, false), "regular interval");
        Equal(true, timer.Tick(0.13f, true, false), "regular quarter second");
        Equal(false, timer.Tick(0.06f, true, true), "strong interval is still bounded");
        Equal(true, timer.Tick(0.06f, true, true), "strong 0.12 second");
        Equal(true, timer.Tick(30f, true, true), "one attempt after a stall");
        Equal(false, timer.Tick(0f, true, true), "no accumulated burst");
        Equal(false, timer.Tick(0.02f, false, false), "busy animation does not run native action");
        Equal(false, timer.Tick(0.02f, true, false), "returning idle must not reset the cooldown");
        timer.Reset();
        Equal(false, timer.Tick(float.NaN, true, false), "invalid delta cannot trigger");
        Equal(true, ActionRules.CanAutoFill(true, true, true, true, true, true, false, false), "valid in-water empty bottle");
        Equal(false, ActionRules.CanAutoFill(false, true, true, true, true, true, false, false), "auto switch");
        Equal(false, ActionRules.CanAutoFill(true, false, true, true, true, true, false, false), "UI/title");
        Equal(false, ActionRules.CanAutoFill(true, true, false, true, true, true, false, false), "out of water");
        Equal(false, ActionRules.CanAutoFill(true, true, true, false, true, true, false, false), "wrong item");
        Equal(false, ActionRules.CanAutoFill(true, true, true, true, false, true, false, false), "animation busy");
        Equal(false, ActionRules.CanAutoFill(true, true, true, true, true, false, false, false), "energy gate");
        Equal(false, ActionRules.CanAutoFill(true, true, true, true, true, true, true, false), "inventory buffer full");
        Equal(false, ActionRules.CanAutoFill(true, true, true, true, true, true, false, true), "pointer on UI");
    }

    private static void ContinuousFoodKeepsNativeCadence()
    {
        foreach (float interval in new[] { 0.5f, 0.3f, 0.2f })
        {
            Near(interval, interval / ActionRules.DrinkClockFactor(true, false), "ordinary food keeps each native stage");
            Near(interval, interval / ActionRules.DrinkClockFactor(false, true), "disabled water rhythm keeps each native stage");
            Near(interval / 3, interval / ActionRules.DrinkClockFactor(true, true), "water scales native progression only");
        }
    }

    private static void AnimationOwnershipSurvivesLoopsAndInterruption()
    {
        var clock = new FakeClock { Speed = 0.75f };
        var lease = new SpeedLease(clock);
        for (int i = 0; i < 100; i++)
        {
            lease.Apply(3); Near(2.25, clock.Speed, "tool loop never compounds");
        }
        lease.Restore(); Near(0.75, clock.Speed, "completion restores prior speed, not hardcoded one");
        lease.Apply(4); lease.Apply(1); Near(0.75, clock.Speed, "switch-off restores active animation");
        lease.Apply(3); clock.Speed = 0.4f; lease.Restore();
        Near(0.4, clock.Speed, "newer external writer retained");
        lease.Apply(2); lease.Restore(); lease.Restore(); Near(0.4, clock.Speed, "title/dispose repeat cleanup");
        clock.Speed = 0; lease.Apply(4); lease.Restore(); Near(0, clock.Speed, "native paused clock preserved");
    }

    private static void AnimationRecoveryRetriesEachClock()
    {
        var first = new FakeClock { Speed = 0.75f };
        var second = new FakeClock { Speed = 0.8f };
        var a = new SpeedLease(first); var b = new SpeedLease(second);
        a.Apply(3); b.Apply(4); first.FailNextWrite = true;
        try { SpeedLease.RestoreAll(new[] { a, b }); throw new Exception("Failure must be reported"); }
        catch (AggregateException ex) { Equal(1, ex.InnerExceptions.Count, "all clock failures reported"); }
        Near(0.8, second.Speed, "other clock restored after first setter failed");
        a.Apply(3); Near(2.25, first.Speed, "next action retries before multiplying");
        first.FailNextWrite = true;
        try { a.Restore(); } catch (InvalidOperationException) { }
        a.Restore(); Near(0.75, first.Speed, "second restoration retries original speed");
        a.Apply(3); first.FailNextWrite = true;
        try { a.Restore(); } catch (InvalidOperationException) { }
        first.Speed = 0.4f; a.Restore(); Near(0.4, first.Speed, "external later write wins after failure");
        a.Apply(3); first.Alive = false; a.Restore();
        first.Alive = true; first.Speed = 0.9f; a.Restore(); Near(0.9, first.Speed, "destroyed clock ends ownership");
    }
    private sealed class FakeClock : IAnimationClock
    {
        private float speed;
        internal bool FailNextWrite, Alive = true;
        public float Speed { get => speed; set { if (FailNextWrite) { FailNextWrite = false; throw new InvalidOperationException("setter"); } speed = value; } }
        public bool IsAlive => Alive;
    }
    private static void Equal<T>(T expected, T actual, string message)
    { checks++; if (!Equals(expected, actual)) throw new Exception(message + ": expected " + expected + ", got " + actual); }
    private static void Near(double expected, double actual, string message)
    { checks++; if (double.IsNaN(actual) || Math.Abs(expected - actual) > 0.00001) throw new Exception(message + ": expected " + expected + ", got " + actual); }
}
