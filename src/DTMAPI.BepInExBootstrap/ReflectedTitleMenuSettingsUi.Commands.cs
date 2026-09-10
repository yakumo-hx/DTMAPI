using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed partial class ReflectedTitleMenuSettingsUi
    {
        private string platformCommandLine = "help";
        private string[] platformCommandOutput = Array.Empty<string>();
        private Task<DtmCommandResult>? pendingPlatformCommand;
        private CancellationTokenSource? platformCommandCancellation;

        private void RenderPlatformCommands()
        {
            if (runtime.GetPlatformCommandHelp().Count == 0) return;
            AddText(panelContentRoot!, "DTMAPI.Commands.Label", T("commands.label", "Mod commands (help lists names; full output is in the log)"), 15,
                Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -418, 920, 26);
            CreateInput(panelContentRoot!, "DTMAPI.Commands.Input", platformCommandLine,
                value => platformCommandLine = value.Length <= 4096 ? value : value.Substring(0, 4096), 52, -454, 764, 30);
            CreateButton(panelContentRoot!, "DTMAPI.Commands.Run", T("commands.run", "Run"), () =>
            {
                if (pendingPlatformCommand != null) return;
                platformCommandCancellation = new CancellationTokenSource();
                pendingPlatformCommand = runtime.ExecutePlatformCommand(platformCommandLine, platformCommandCancellation.Token);
                statusMessage = T("commands.queued", "Command queued."); dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 832, -454, 120, 30);
            for (int index = 0; index < platformCommandOutput.Length; index++)
                AddText(panelContentRoot!, "DTMAPI.Commands.Output." + index, Truncate(platformCommandOutput[index], 140), 13,
                    Color(0.88f, 0.93f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -494 - index * 29, 910, 25);
        }

        private void PollPlatformCommand()
        {
            if (pendingPlatformCommand == null || !pendingPlatformCommand.IsCompleted) return;
            DtmCommandResult result = pendingPlatformCommand.GetAwaiter().GetResult();
            pendingPlatformCommand = null;
            platformCommandCancellation?.Dispose(); platformCommandCancellation = null;
            statusMessage = result.Work.Status + (result.Work.ErrorCode.Length == 0 ? "" : ": " + result.Work.ErrorCode);
            platformCommandOutput = result.Output.Take(4).ToArray();
            runtime.RuntimeMonitor.Log("Mod command request=" + result.RequestId + "; owner=" + result.OwnerId + "; status=" + statusMessage);
            foreach (string line in result.Output) runtime.RuntimeMonitor.Log("Mod command output: " + line);
            if (result.Work.Message.Length != 0) runtime.RuntimeMonitor.Log("Mod command detail: " + result.Work.Message);
            dirty = true;
        }

        private void CancelPlatformCommand()
        {
            platformCommandCancellation?.Cancel(); platformCommandCancellation?.Dispose(); platformCommandCancellation = null;
            pendingPlatformCommand = null; platformCommandOutput = Array.Empty<string>();
        }
    }
}
