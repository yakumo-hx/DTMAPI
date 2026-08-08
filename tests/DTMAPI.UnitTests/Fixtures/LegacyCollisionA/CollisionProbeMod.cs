using DTMAPI.Abstractions;

namespace DTMAPI.LegacyCollisionFixture
{
    public sealed class CollisionProbeMod : DtmMod
    {
        public static int EntryCount { get; private set; }

        public override void Entry(IDtmHelper helper)
        {
            EntryCount++;
            helper.Monitor.Log("legacy-collision-A");
        }
    }
}
