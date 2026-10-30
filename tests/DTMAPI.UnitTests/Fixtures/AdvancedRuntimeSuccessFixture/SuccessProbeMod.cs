using System;
using DTMAPI.Abstractions;

namespace DTMAPI.AdvancedRuntimeSuccessFixture
{
    public sealed class SuccessProbeMod : DtmMod
    {
        public static int EntryCount { get; private set; }

        public Type[] NativeReferenceTypes => new[]
        {
            typeof(HarmonyLib.Harmony),
            typeof(SyntheticGameApi.NativeProbe)
        };

        public override void Entry(IDtmHelper helper)
        {
            EntryCount++;
            helper.Monitor.Log("advanced-sdk01-success-entry");
        }
    }
}
