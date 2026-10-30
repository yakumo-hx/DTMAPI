using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Runtime
{
    // Native producers and console/session adapters stay internal to the Runtime assemblies.
    public sealed partial class DtmApiRuntime
    {
        internal void NotifyPlatformNewGameStarting(int slot)
        {
            if (RejectOffThreadRuntimeProducer("Platform.NewGameStarting", "DTMAPI.GameBridge")) return;
            currentLoadingSlot = slot;
            platformServices.BeginSaveAttempt();
        }
        internal void NotifyPlatformNewGameCompleted()
        {
            if (RejectOffThreadRuntimeProducer("Platform.NewGameCompleted", "DTMAPI.GameBridge")) return;
            platformServices.CompleteSaveAttempt(true);
        }
        internal void NotifyPlatformLoadFailure()
        {
            if (RejectOffThreadRuntimeProducer("Platform.LoadFailure", "DTMAPI.GameBridge")) return;
            platformServices.CompleteSaveAttempt(false);
        }
        internal void NotifyPlatformWorldTransition()
        {
            if (RejectOffThreadRuntimeProducer("Platform.WorldTransition", "DTMAPI.GameBridge")) return;
            platformServices.BeginWorldTransition();
        }
        internal void NotifyPlatformWorldReady()
        {
            if (RejectOffThreadRuntimeProducer("Platform.WorldReady", "DTMAPI.GameBridge")) return;
            platformServices.ObserveWorldReady();
        }
        internal DtmRuntimeContextSnapshot PlatformContextSnapshot => platformServices.Snapshot;
        internal IReadOnlyList<DtmCommandInfo> GetPlatformCommandHelp() => platformServices.GetCommandHelp();
        internal Task<DtmCommandResult> ExecutePlatformCommand(string commandLine, CancellationToken token = default)
            => platformServices.ExecuteCommand(commandLine, token);

        internal AuthorSessionOperationResult ExecuteAuthorPlatformCommand(AuthorSessionRequest request)
        {
            Task<DtmCommandResult> completion = platformServices.ExecuteAuthorCommand(request.UniqueId, request.CommandLine ?? "", request.RequestId, out IDtmScheduledWork? scheduled);
            return AuthorSessionOperationResult.Deferred(ToAuthorCommandResult(completion), () => scheduled?.TryCancel() ?? false);
        }

        private static async Task<AuthorSessionOperationResult> ToAuthorCommandResult(Task<DtmCommandResult> completion)
        {
            DtmCommandResult result = await completion.ConfigureAwait(false);
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("commandOwner", result.OwnerId),
                new KeyValuePair<string, string>("commandRequestId", result.RequestId),
                new KeyValuePair<string, string>("workStatus", result.Work.Status.ToString())
            };
            // The existing wire result owns its 32-field/2048-character limits. Chunk rather than silently truncate a line.
            string output = string.Join("\n", result.Output);
            for (int offset = 0, chunk = 0; offset < output.Length; offset += 2000, chunk++)
                values.Add(new KeyValuePair<string, string>("output" + chunk, output.Substring(offset, System.Math.Min(2000, output.Length - offset))));
            return result.Work.Status == DtmWorkStatus.Succeeded
                ? AuthorSessionOperationResult.Success("command-completed", "Command completed.", values.ToArray())
                : AuthorSessionOperationResult.Rejected(result.Work.ErrorCode, result.Work.Message, values.ToArray());
        }
    }
}
