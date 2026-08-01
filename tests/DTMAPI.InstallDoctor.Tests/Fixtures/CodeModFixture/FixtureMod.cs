#pragma warning disable CS0618
using DTMAPI.Abstractions;

namespace DoctorCodeModFixture;

public sealed class FixtureMod : DtmMod
{
    public ILampControlApi? LegacyLampApi { get; set; }

    public override void Entry(IDtmHelper helper)
    {
    }
}
