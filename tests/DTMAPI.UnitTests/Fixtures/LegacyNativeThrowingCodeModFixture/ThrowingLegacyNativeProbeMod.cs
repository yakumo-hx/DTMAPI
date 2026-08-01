using DTMAPI.Abstractions;
using DTMAPI.LegacyNativeHelperFixture;
using System;

namespace DTMAPI.LegacyNativeThrowingCodeModFixture
{
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
