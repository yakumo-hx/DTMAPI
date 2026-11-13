using System.Collections.Generic;
using System.Reflection;

namespace DTMAPI.Core.Runtime
{
    internal interface IModPatchCleanup
    {
        ModOwnerCleanupParticipantResult RemovePatches(
            IReadOnlyCollection<Assembly> exclusive,
            IReadOnlyCollection<Assembly> shared,
            string ownerHint);
    }
}
