using System;
using DTMAPI.Abstractions;

namespace DTMAPI.AdvancedRuntimeFailureFixture
{
    public sealed class FailureProbeMod : DtmMod
    {
        public Type[] NativeReferenceTypes => new[]
        {
            typeof(HarmonyLib.Harmony),
            typeof(SyntheticGameApi.NativeProbe)
        };

        public override void Entry(IDtmHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += (_, __) => { };
            helper.Input.RegisterButton("F10");
            throw new InvalidOperationException("throwing-advanced-sdk01-entry-probe");
        }
    }
}
