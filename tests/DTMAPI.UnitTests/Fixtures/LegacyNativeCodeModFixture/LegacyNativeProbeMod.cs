using DTMAPI.Abstractions;
using DTMAPI.LegacyNativeHelperFixture;
using System;

namespace DTMAPI.LegacyNativeCodeModFixture
{
    public sealed class LegacyNativeProbeMod : DtmMod
    {
        public static int EntryCount { get; private set; }

        public override void Entry(IDtmHelper helper)
        {
            EntryCount++;
            var harmony = new HarmonyLib.Harmony();
            var native = new SyntheticGameApi.NativeProbe();
            string message = LegacyNativeHelper.Describe(BepInEx.Paths.GameRootPath);
            helper.Monitor.Log(message + "|" + harmony.GetType().FullName + "|" + native.GetType().FullName);
        }
    }

    public sealed class ThrowingLegacyNativeProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            _ = new HarmonyLib.Harmony();
            _ = new SyntheticGameApi.NativeProbe();
            _ = LegacyNativeHelper.Describe(BepInEx.Paths.GameRootPath);
            throw new InvalidOperationException("legacy-native-entry-probe");
        }
    }
}
