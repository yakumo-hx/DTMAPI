using System;
using System.Reflection;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.AdvancedFixture
{
    public sealed class ModEntry : DtmMod
    {
        private const string ExpectedHarmonyOwner = "dtmapi.mod.dtmapi.advancedfixture";
        private const string WrongHarmonyOwner = "dtmapi.mod.dtmapi.advancedfixture.unexpected";
        private static IMonitor? monitor;
        private static bool nativeQueryLogged;
        private bool lateOwnerInstalled;

        public override void Entry(IDtmHelper helper)
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            monitor = helper.Monitor;
            AdvancedFixtureConfig config = helper.ReadConfig<AdvancedFixtureConfig>();
            string mode = NormalizeMode(config.Mode);
            string owner = mode == "WrongOwner" ? WrongHarmonyOwner : ExpectedHarmonyOwner;
            MethodInfo target = AccessTools.Method(typeof(DolocAPI), nameof(DolocAPI.Has087DemoData))
                ?? throw new MissingMethodException(typeof(DolocAPI).FullName, nameof(DolocAPI.Has087DemoData));
            MethodInfo postfix = AccessTools.Method(typeof(ModEntry), nameof(ObserveHas087DemoData))
                ?? throw new MissingMethodException(typeof(ModEntry).FullName, nameof(ObserveHas087DemoData));

            Harmony harmony = new Harmony(owner);
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            if (mode == "DuplicatePatch")
                harmony.Patch(target, postfix: new HarmonyMethod(postfix));

            helper.Monitor.Log("AdvancedFixture Entry mode=" + mode + " owner=" + owner);
            helper.Diagnostics.RecordEvidence(
                "ADVANCED-FIXTURE-ENTRY",
                "mode=" + mode + "; owner=" + owner + "; target=DolocAPI.Has087DemoData");

            if (mode == "EntryFailure")
                throw new InvalidOperationException("Synthetic AdvancedFixture EntryFailure after canonical-owner patch installation.");

            helper.Events.GameLoop.GameLaunched += (_, __) => RunNativeQuery(helper);
            if (mode == "LateOwnerDrift")
            {
                helper.Events.GameLoop.OneSecondUpdateTicked += (_, __) =>
                {
                    if (lateOwnerInstalled)
                        return;
                    lateOwnerInstalled = true;
                    Harmony lateHarmony = new Harmony(WrongHarmonyOwner);
                    lateHarmony.Patch(target, postfix: new HarmonyMethod(postfix));
                    helper.Monitor.Log("AdvancedFixture LateOwnerDrift installed owner=" + WrongHarmonyOwner, LogLevel.Warn);
                    helper.Diagnostics.RecordEvidence(
                        "ADVANCED-FIXTURE-LATE-OWNER",
                        "owner=" + WrongHarmonyOwner + "; target=DolocAPI.Has087DemoData");
                };
            }
        }

        private static void RunNativeQuery(IDtmHelper helper)
        {
            bool result = DolocAPI.Has087DemoData();
            helper.Monitor.Log("AdvancedFixture NativeQuery result=" + result);
            helper.Diagnostics.RecordEvidence(
                "ADVANCED-FIXTURE-NATIVE-QUERY",
                "DolocAPI.Has087DemoData result=" + result);
        }

        private static void ObserveHas087DemoData(bool __result)
        {
            if (nativeQueryLogged)
                return;
            nativeQueryLogged = true;
            monitor?.Log("AdvancedFixture PatchPostfix result=" + __result);
        }

        private static string NormalizeMode(string? value)
        {
            string mode = string.IsNullOrWhiteSpace(value) ? "Normal" : value.Trim();
            switch (mode)
            {
                case "Normal":
                case "WrongOwner":
                case "DuplicatePatch":
                case "EntryFailure":
                case "LateOwnerDrift":
                    return mode;
                default:
                    throw new InvalidOperationException("Unknown AdvancedFixture mode: " + mode);
            }
        }
    }

    public sealed class AdvancedFixtureConfig
    {
        public string Mode { get; set; } = "Normal";
    }
}
