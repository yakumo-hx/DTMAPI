using DTMAPI.Abstractions;
using System;

namespace DTMAPI.LegacyCollisionFixture
{
    public sealed class CollisionProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            throw new InvalidOperationException("legacy-collision-B-executed");
        }
    }
}
