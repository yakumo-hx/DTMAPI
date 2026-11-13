using System;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleSaveSessionGate
    {
        internal bool IsActive { get; private set; }

        internal void Enter(
            Action restorePreviousSession,
            Action publishNewSession)
        {
            if (restorePreviousSession == null)
                throw new ArgumentNullException(
                    nameof(restorePreviousSession));
            if (publishNewSession == null)
                throw new ArgumentNullException(
                    nameof(publishNewSession));

            IsActive = false;
            restorePreviousSession();
            publishNewSession();
            IsActive = true;
        }

        internal void Leave()
        {
            IsActive = false;
        }
    }
}
