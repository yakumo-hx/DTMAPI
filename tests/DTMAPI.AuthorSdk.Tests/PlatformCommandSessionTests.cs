using DTMAPI.Abstractions;
using DTMAPI.Authoring.Contracts;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using System.Text.Json.Nodes;
using System.Threading;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestAuthorCommandSchedulerAdapter(string temp)
    {
        SessionFixture fixture = await PrepareSessionFixture(temp, "command-scheduler", enableCommands: true);
        AuthorSessionDescriptorLoadResult load = AuthorSessionDescriptorStore.ConsumeStartup(fixture.Game, DtmApiRuntime.ApiVersion);
        True(load.Accepted, "Command capability offer accepted.");
        True(AuthorSessionHost.TryCreate(load.Descriptor!, fixture.Game, DtmApiRuntime.ApiVersion, out AuthorSessionHost? created, out _,
            () => DateTimeOffset.UtcNow, 2, 3, TimeSpan.FromMilliseconds(300)), "Command host created.");
        using AuthorSessionHost host = created!;
        True(host.Start().Accepted, "Command host starts.");
        int mainThread = Thread.CurrentThread.ManagedThreadId;
        var runtime = new PlatformRuntimeServices(() => Thread.CurrentThread.ManagedThreadId == mainThread, (_, _, _) => { });
        var commands = (IDtmCommands)runtime.Resolve("Tests.Session", typeof(IDtmCommands))!;
        var other = (IDtmCommands)runtime.Resolve("Tests.Other", typeof(IDtmCommands))!;
        int executions = 0;
        using var registration = commands.Register("echo", context =>
        {
            True(mainThread == Thread.CurrentThread.ManagedThreadId, "Pipe command executes on the Runtime thread.");
            executions++;
            context.WriteLine(string.Join("|", context.Arguments));
        });
        using var otherRegistration = other.Register("echo", _ => throw new Exception("Cross-owner command must not run."));
        IDtmScheduledWork? last = null;
        AuthorSessionOperationResult Handle(AuthorSessionRequest request)
        {
            Equal("execute-command", request.Operation, "CLI sends the actual negotiated operation.");
            Task<DtmCommandResult> task = runtime.ExecuteAuthorCommand(request.UniqueId, request.CommandLine!, request.RequestId, out var scheduled);
            last = scheduled;
            return AuthorSessionOperationResult.Deferred(Convert(task), () => scheduled?.TryCancel() ?? false);
        }
        async Task<AuthorSessionOperationResult> Convert(Task<DtmCommandResult> task)
        {
            DtmCommandResult result = await task.ConfigureAwait(false);
            var values = new[] { new KeyValuePair<string, string>("commandRequestId", result.RequestId), new KeyValuePair<string, string>("output", string.Join("|", result.Output)) };
            return result.Work.Status == DtmWorkStatus.Succeeded
                ? AuthorSessionOperationResult.Success("command-completed", "Completed.", values)
                : AuthorSessionOperationResult.Rejected(result.Work.ErrorCode, result.Work.Message, values);
        }
        ulong tick = 0;
        T Pump<T>(Task<T> task, bool drain)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(6);
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            {
                host.ProcessPending(Handle);
                if (drain) { runtime.BeginTick(++tick); runtime.Drain(); }
                Thread.Sleep(2);
            }
            True(task.IsCompleted, "Bounded deferred command completed or cancelled.");
            return task.GetAwaiter().GetResult();
        }
        CommandReport result = Pump(ExpectSuccess("SDK command round trip", "session", "command", "Tests.Session", fixture.Source,
            "--game-root", fixture.Game, "--command-line", "Tests.Session/echo \"two words\" tail"), true);
        Equal("two words|tail", result.Values["response.output"], "Actual SDK carries parser input and deferred output.");
        Equal(result.Values["requestId"], result.Values["response.commandRequestId"], "Transport and command share the same request ID.");
        True(executions == 1, "Command ran exactly once.");
        CommandReport cross = Pump(ExpectFailure("command owner mismatch", "session", "command", "Tests.Session", fixture.Source,
            "--game-root", fixture.Game, "--command-line", "Tests.Other/echo"), true);
        Equal("command-owner-mismatch", cross.Values["runtimeCode"], "Selected owner cannot invoke another owner's command.");
        CommandReport timeout = Pump(ExpectFailure("queued command timeout", "session", "command", "Tests.Session", fixture.Source,
            "--game-root", fixture.Game, "--command-line", "Tests.Session/echo never"), false);
        True(timeout.Values["runtimeCode"] is "runtime-timeout" or "cancelled", "Timeout requests cancellation of unstarted scheduler work.");
        True(last?.Status == DtmWorkStatus.Cancelled && executions == 1 && runtime.PendingWorkCount == 0, "Timed-out command cannot run on a later frame.");
        Task<JsonObject> closing = SendSessionFrame(fixture.Pipe, CommandFrame("Tests.Session/echo close").ToJsonString());
        DateTime closeDeadline = DateTime.UtcNow.AddSeconds(2);
        while (runtime.PendingWorkCount == 0 && DateTime.UtcNow < closeDeadline) { host.ProcessPending(Handle); Thread.Sleep(2); }
        True(runtime.PendingWorkCount == 1, "Close control queued a real scheduler command.");
        host.Close("command-close");
        try { closing.GetAwaiter().GetResult(); } catch (IOException) { }
        True(last?.Status == DtmWorkStatus.Cancelled && runtime.PendingWorkCount == 0 && host.GetSnapshot().InFlight == 0, "Session close cancels deferred work and releases host ownership.");
        Console.WriteLine("Author command adapter: OK (actual CLI/Core pipe, main-thread scheduler, owner binding, timeout and close cancellation).");

        JsonObject CommandFrame(string line)
        {
            JsonObject frame = SessionFrame(fixture, "execute-command");
            frame["commandLine"] = line;
            return frame;
        }
    }
}
