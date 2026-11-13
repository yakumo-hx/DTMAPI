using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Runtime
{
    // Native producers and console adapters stay internal to the Runtime assemblies.
    public sealed partial class DtmApiRuntime
    {
        internal void NotifyPlatformNewGameStarting(int slot)
        {
            currentLoadingSlot = slot;
            platformServices.BeginSaveAttempt();
        }
        internal void NotifyPlatformNewGameCompleted()
        {
            platformServices.CompleteSaveAttempt(true);
        }
        internal void NotifyPlatformLoadFailure()
        {
            platformServices.CompleteSaveAttempt(false);
        }
        internal void NotifyPlatformWorldTransition()
        {
            platformServices.BeginWorldTransition();
        }
        internal void NotifyPlatformWorldReady()
        {
            platformServices.ObserveWorldReady();
        }
        internal DtmRuntimeContextSnapshot PlatformContextSnapshot => platformServices.Snapshot;
        internal IReadOnlyList<DtmCommandInfo> GetPlatformCommandHelp() => platformServices.GetCommandHelp();
        internal Task<DtmCommandResult> ExecutePlatformCommand(string commandLine, CancellationToken token = default)
            => platformServices.ExecuteCommand(commandLine, token);
    }
}
