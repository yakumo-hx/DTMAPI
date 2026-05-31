using DTMAPI.Abstractions;

namespace HelloDtmMod
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.Monitor.Log("HelloDtmMod Entry OK");
            helper.Events.GameLoop.GameLaunched += (_, __) => helper.Monitor.Log("HelloDtmMod GameLaunched OK");
        }
    }
}
