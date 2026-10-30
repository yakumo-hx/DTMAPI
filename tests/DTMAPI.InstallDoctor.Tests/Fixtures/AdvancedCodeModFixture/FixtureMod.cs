using System;
using DTMAPI.Abstractions;

namespace DoctorAdvancedCodeModFixture;

public sealed class FixtureMod : DtmMod
{
    public Type[] NativeReferenceTypes => new[]
    {
        typeof(HarmonyLib.Harmony),
        typeof(SyntheticGameApi.NativeProbe)
    };

    public override void Entry(IDtmHelper helper)
    {
    }
}
