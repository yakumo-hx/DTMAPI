using System;
using System.Linq;
using System.Reflection;
using DolocTown;
using HarmonyLib;
using UnityEngine;
using Yuuka.DTMAPI.ActionSpeed;

internal static class Program
{
    private const string OtherOwner = "Tests.ActionSpeed.Unrelated";

    private static int Main()
    {
        NativeActions? native = null;
        var other = new Harmony(OtherOwner);
        try
        {
            var otherTarget = typeof(SimpleWell).GetMethod("OnInteract")!;
            other.Patch(otherTarget, prefix: new HarmonyMethod(typeof(Program), nameof(Unrelated)));
            var config = new ActionConfig();
            native = new NativeActions(() => config);
            native.Install();
            ToolAnimationsStayTogether(config);

            var scope = (ActionScope)typeof(NativeActions).GetField("scope", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(native)!;
            NativeHost.DuringCall = () => { Require(scope.Current == ActionKind.Fill, "prefix establishes operation"); throw new ApplicationException("native fixture failure"); };
            try { new SimpleWell().OnInteract(); throw new Exception("native exception was swallowed"); }
            catch (ApplicationException ex) { Require(ex.Message == "native fixture failure", "original failure"); }
            Require(scope.Current == ActionKind.None, "finalizer restores operation on native failure");
            NativeHost.DuringCall = () => { };
            native.Dispose(); native.Dispose();
            Require(!Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method).Owners.Contains(NativeActions.HarmonyOwner)), "product patches removed");
            Require(Harmony.GetPatchInfo(otherTarget).Owners.Contains(OtherOwner), "unrelated patch survives");
            Console.WriteLine("ActionSpeed native callbacks: PASS (tool and watering child animations, completion, interruption, operation cleanup). Unity physics and player behavior require game testing.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { NativeHost.DuringCall = () => { }; native?.Dispose(); other.UnpatchSelf(); }
    }

    private static void ToolAnimationsStayTogether(ActionConfig config)
    {
        var bodyClock = new Animator { speed = 0.8f };
        var toolClock = new Animator();
        var collisionClock = new Animator { speed = 0.75f };
        var hatClock = new Animator();
        var body = new BodyController
        {
            AttachedAnimator = bodyClock,
            ToolRenderer = new ToolRenderer { AttachedAnimator = toolClock, ChildAnimators = new[] { collisionClock } },
            HatRenderer = new AgentHatRenderer { AttachedAnimator = hatClock }
        };
        DolocAPI.agent = body;
        foreach (var state in new AgentStateBase[] { new AgentStateTool(), new AgentStateWater() })
        {
            typeof(AgentStateBase).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(state, body);
            var enter = state.GetType().GetMethod("OnEnter")!;
            foreach (float factor in new[] { 1f, 1.5f, 2f, 3f, 4f })
            {
                config.ToolMultiplier = factor;
                enter.Invoke(state, null);
                Near(0.8f * factor, bodyClock.speed, "body speed");
                Near(factor, toolClock.speed, "tool appearance speed");
                Near(0.75f * factor, collisionClock.speed, "tool collision child speed");
                Near(factor, hatClock.speed, "hat speed");
                state.OnExit();
                Restored();
                enter.Invoke(state, null);
                body.PlayAnimation("idle", 0);
                Restored();
            }
        }
        void Restored()
        {
            Near(0.8f, bodyClock.speed, "body restored");
            Near(1, toolClock.speed, "tool appearance restored");
            Near(0.75f, collisionClock.speed, "tool collision child restored");
            Near(1, hatClock.speed, "hat restored");
        }
    }

    private static void Near(float expected, float actual, string label) =>
        Require(Math.Abs(expected - actual) < 0.00001f, label + ": expected=" + expected + ", actual=" + actual);
    private static void Unrelated() { }
    private static void Require(bool condition, string label) { if (!condition) throw new Exception(label); }
}
