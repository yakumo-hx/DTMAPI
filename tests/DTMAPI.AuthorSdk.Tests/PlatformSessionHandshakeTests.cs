using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.AuthorSdk;
using DTMAPI.Authoring.Contracts;
using DTMAPI.Core.Runtime;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private sealed record SessionFixture(string Game, string Source, CommandReport Prepared, JsonObject Secret)
    {
        public string Pipe => Secret["pipeName"]!.GetValue<string>();
        public string Token => Secret["token"]!.GetValue<string>();
    }

    private static async Task TestPlatformSessionHandshake(string temp)
    {
        await TestDescriptorCompatibilityMatrix(temp);
        await TestActualSdkCoreRoundTrip(temp);
        await TestNegotiatedHostBoundaries(temp);
        await TestNegotiatedQueueLifetime(temp);
        await TestAuthorCommandSchedulerAdapter(temp);
        await TestLegacyWireAdapter(temp);
        await TestSessionClientFailureWindows(temp);
        await TestActualLegacySdkWhenProvided(temp);
    }

    private static async Task<SessionFixture> PrepareSessionFixture(string temp, string name, Action<JsonObject>? change = null, bool enableCommands = false)
    {
        string game = NewGameRoot(temp, "platform-session-" + name);
        string source = Path.Combine(game, "selected-source");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "manifest.json"), "{\"UniqueID\":\"Tests.Session\",\"Name\":\"Session\",\"Author\":\"Tests\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\"}");
        CommandReport prepared = enableCommands
            ? await ExpectSuccess("actual CLI command prepare " + name, "session", "prepare", "--game-root", game, "--commands", "true")
            : await ExpectSuccess("actual CLI prepare " + name, "session", "prepare", "--game-root", game);
        JsonObject secret = JsonNode.Parse(File.ReadAllText(prepared.Values["descriptorPath"]))!.AsObject();
        True(!secret.ContainsKey("hostVersion") && !secret.ContainsKey("runtimeVersion"), "Offline prepare must not invent Host/wire identity.");
        Equal("0.7.0", secret["apiTarget"]!.GetValue<string>(), "Current default compilation target.");
        if (change != null)
        {
            change(secret);
            WriteJson(prepared.Values["descriptorPath"], secret);
            WriteJson(prepared.Values["credentialPath"], secret);
        }
        return new SessionFixture(game, source, prepared, secret);
    }

    private static async Task TestDescriptorCompatibilityMatrix(string temp)
    {
        foreach ((string Name, Action<JsonObject> Change, string Code) item in new (string, Action<JsonObject>, string)[]
        {
            ("future-major", j => j["protocolMajor"] = 99, "protocol-major-unsupported"),
            ("future-minor", j => { j["minimumMinor"] = 1; j["maximumMinor"] = 2; }, "protocol-minor-incompatible"),
            ("bad-range", j => j["minimumMinor"] = -1, "protocol-minor-incompatible"),
            ("required-unknown", j => j["requiredCapabilities"] = new JsonArray("future-operation/1"), "required-capability-unsupported"),
            ("duplicate-capability", j => j["optionalCapabilities"] = new JsonArray("reload-content/1", "reload-content/1"), "capabilities-invalid"),
            ("future-runtime", j => j["minimumRuntimeVersion"] = "99.0.0", "upgrade-required"),
            ("bad-target", j => j["apiTarget"] = "not-a-version", "descriptor-version-invalid"),
            ("missing-minor", j => j.Remove("minimumMinor"), "descriptor-json-invalid"),
            ("invented-host", j => j["hostVersion"] = "0.6.3", "descriptor-json-invalid"),
            ("wrong-root", j => j["gameRoot"] = Path.Combine(temp, "other-game"), "descriptor-root-mismatch"),
            ("wrong-pipe", j => j["pipeName"] = "unbound-pipe", "descriptor-pipe-mismatch"),
            ("bad-token", j => j["token"] = "invalid", "descriptor-token-invalid"),
            ("bad-session", j => j["sessionId"] = new string('0', 32), "descriptor-session-invalid"),
            ("expired", j => { j["createdAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(-12).ToString("O"); j["expiresAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(-2).ToString("O"); }, "descriptor-expired"),
            ("future-clock", j => { j["createdAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(3).ToString("O"); j["expiresAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"); }, "descriptor-not-yet-valid")
        })
        {
            SessionFixture fixture = await PrepareSessionFixture(temp, item.Name, item.Change);
            AuthorSessionDescriptorLoadResult result = AuthorSessionDescriptorStore.ConsumeStartup(fixture.Game, DtmApiRuntime.ApiVersion);
            Equal(item.Code, result.Code, "Core reader " + item.Name);
            True(!result.Accepted && !File.Exists(fixture.Prepared.Values["descriptorPath"]), "Rejected descriptor is consumed once.");
            Equal("descriptor-already-checked", AuthorSessionDescriptorStore.ConsumeStartup(fixture.Game, DtmApiRuntime.ApiVersion).Code, "No descriptor retry.");
        }
        SessionFixture duplicate = await PrepareSessionFixture(temp, "duplicate-json");
        File.WriteAllText(duplicate.Prepared.Values["descriptorPath"], duplicate.Secret.ToJsonString().Insert(1, "\"schemaVersion\":2,"));
        Equal("descriptor-json-invalid", AuthorSessionDescriptorStore.ConsumeStartup(duplicate.Game, DtmApiRuntime.ApiVersion).Code, "Duplicate descriptor field.");
        SessionFixture optional = await PrepareSessionFixture(temp, "unknown-optional", j =>
        { j["maximumMinor"] = 4; j["optionalCapabilities"] = new JsonArray("reload-content/1", "future-operation/1"); j["futureMetadata"] = new JsonObject { ["x"] = true }; });
        AuthorSessionDescriptorLoadResult selected = AuthorSessionDescriptorStore.ConsumeStartup(optional.Game, DtmApiRuntime.ApiVersion);
        True(selected.Accepted && selected.Descriptor!.SelectedMinor == 0, "Highest shared minor selected.");
        True(selected.Descriptor!.UnsupportedOptionalCapabilities.SequenceEqual(new[] { "future-operation/1" }), "Unknown optional capability remains disabled.");
        SessionFixture oldHost = await PrepareSessionFixture(temp, "new-descriptor-old-host-version");
        Equal("upgrade-required", AuthorSessionDescriptorStore.ConsumeStartup(oldHost.Game, AuthorSdkContract.TargetRuntimeVersion).Code, "Minimum Runtime is independent of API target.");
        AuthorSessionDescriptorLoadResult absent = AuthorSessionDescriptorStore.ConsumeStartup(NewGameRoot(temp, "no-session-descriptor"), DtmApiRuntime.ApiVersion);
        True(!absent.Present && absent.Descriptor == null, "Ordinary startup has no session to host.");
        Console.WriteLine("Session descriptor matrix: OK (actual CLI, versions, offers, identities, expiry, consumption, JSON).");
    }

    private static AuthorSessionHost CreateSessionHost(SessionFixture fixture)
    {
        AuthorSessionDescriptorLoadResult load = AuthorSessionDescriptorStore.ConsumeStartup(fixture.Game, DtmApiRuntime.ApiVersion);
        True(load.Accepted, "Actual SDK descriptor current Core: " + load.Code);
        True(AuthorSessionHost.TryCreate(load.Descriptor!, fixture.Game, DtmApiRuntime.ApiVersion, out AuthorSessionHost? host, out AuthorSessionValidationResult validation), "Actual Core Host: " + validation.Code);
        load.Descriptor!.Token = "caller-mutated-token"; // Host must snapshot its authenticated identity.
        True(host!.Start().Accepted, "Actual Core listener starts.");
        return host;
    }

    private static T PumpHost<T>(AuthorSessionHost host, Task<T> task, Func<AuthorSessionRequest, AuthorSessionOperationResult>? handler = null)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            host.ProcessPending(handler ?? (_ => AuthorSessionOperationResult.Success("source-snapshot", "snapshot complete")));
            Thread.Sleep(2);
        }
        True(task.IsCompleted, "Bounded actual Host/client test completed.");
        return task.GetAwaiter().GetResult();
    }

    private static async Task TestActualSdkCoreRoundTrip(string temp)
    {
        SessionFixture fixture = await PrepareSessionFixture(temp, "actual-round-trip", j =>
        { j["optionalCapabilities"] = new JsonArray("reload-content/1", "future-operation/1"); j["futureMetadata"] = true; });
        using AuthorSessionHost host = CreateSessionHost(fixture);
        int handlers = 0;
        CommandReport snapshot = PumpHost(host, ExpectSuccess("actual SDK snapshot", "session", "snapshot", "Tests.Session", fixture.Source, "--game-root", fixture.Game), request =>
        {
            handlers++;
            True(request.Token.Length == 0, "Business handler does not retain the wire token.");
            return AuthorSessionOperationResult.Success("source-snapshot", "snapshot complete", new KeyValuePair<string, string>("echo", fixture.Token));
        });
        Equal(DtmApiRuntime.ApiVersion, snapshot.Values["hostVersion"], "Actual Host version.");
        Equal("0.7.0", snapshot.Values["apiTarget"], "Current compiler target on current Host.");
        Equal("future-operation/1", snapshot.Values["unsupportedOptionalCapabilities"], "Actual SDK reports unknown optional capability without enabling it.");
        Equal("[redacted]", snapshot.Values["response.echo"], "Actual Host result redacted by client.");
        True(!JsonSerializer.Serialize(snapshot, JsonOptions).Contains(fixture.Token, StringComparison.Ordinal), "JSON report never includes token.");
        JsonObject credential = JsonNode.Parse(File.ReadAllText(fixture.Prepared.Values["credentialPath"]))!.AsObject();
        Equal(DtmApiRuntime.ApiVersion, credential["negotiated"]!["hostVersion"]!.GetValue<string>(), "Handshake persists across CLI invocations.");
        if (OperatingSystem.IsWindows()) AssertCurrentUserOnlyAcl(fixture.Prepared.Values["credentialPath"]);
        CommandReport reload = PumpHost(host, ExpectSuccess("actual SDK reload", "session", "reload", "Tests.Session", fixture.Source, "--game-root", fixture.Game), _ =>
        { handlers++; return AuthorSessionOperationResult.RestartRequired("content-restart-required", "Code and unknown content require restart."); });
        Equal("restart-required", reload.Values["runtimeStatus"], "Actual Core restart response reaches actual SDK validator.");
        CommandReport large = PumpHost(host, ExpectFailure("bounded large response", "session", "snapshot", "Tests.Session", fixture.Source, "--game-root", fixture.Game), _ =>
        {
            handlers++;
            return AuthorSessionOperationResult.Success("large-response", "large response", Enumerable.Range(0, 32)
                .Select(index => new KeyValuePair<string, string>("value" + index, new string('x', 2048))).ToArray());
        });
        Equal("response-too-large", large.Values["statusCode"], "Bounded response fallback retains negotiated envelope.");
        True(handlers == 3, "Hello retries never execute business handlers.");
        host.Close("test-close");
        True(host.GetSnapshot().Closed && host.GetSnapshot().Pending == 0, "Closed Host clears queue.");
        Task<JsonObject> switchedHost = RespondOnce(fixture.Pipe, request =>
        {
            JsonObject response = BuildSessionResponse(request, "ok", "hello-accepted", "hello complete", "");
            response["hostVersion"] = "0.7.1";
            response["unsupportedOptionalCapabilities"] = new JsonArray("future-operation/1");
            return response.ToJsonString();
        });
        CommandReport switched = await ExpectFailure("Host identity switch", "session", "snapshot", "Tests.Session", fixture.Source, "--game-root", fixture.Game, "--timeout-seconds", "2");
        await switchedHost;
        Equal("pipe-response-negotiation-mismatch", switched.Values["statusCode"], "Later CLI invocation cannot rebind Host identity.");
        Console.WriteLine("Actual SDK → Core reader/pipe/Host → SDK validator: OK (snapshot, retry, reload, ACL, redaction, close).");
    }

    private static JsonObject SessionFrame(SessionFixture fixture, string operation)
    {
        JsonObject frame = new()
        {
            ["schemaVersion"] = 2, ["protocolMajor"] = fixture.Secret["protocolMajor"]!.DeepClone(),
            ["minimumMinor"] = fixture.Secret["minimumMinor"]!.DeepClone(), ["maximumMinor"] = fixture.Secret["maximumMinor"]!.DeepClone(),
            ["apiTarget"] = fixture.Secret["apiTarget"]!.DeepClone(), ["minimumRuntimeVersion"] = fixture.Secret["minimumRuntimeVersion"]!.DeepClone(),
            ["requiredCapabilities"] = fixture.Secret["requiredCapabilities"]!.DeepClone(), ["optionalCapabilities"] = fixture.Secret["optionalCapabilities"]!.DeepClone(),
            ["gameRoot"] = fixture.Game, ["session"] = fixture.Secret["sessionId"]!.DeepClone(), ["token"] = fixture.Token,
            ["requestId"] = Guid.NewGuid().ToString("N"), ["operation"] = operation, ["uniqueId"] = "Tests.Session",
            ["selectedRoot"] = fixture.Source, ["expectedTreeSha256"] = new string('a', 64)
        };
        if (operation != "hello") { frame["hostVersion"] = DtmApiRuntime.ApiVersion; frame["protocolMinor"] = 0; }
        return frame;
    }

    private static Task<JsonObject> SendSessionFrame(string pipeName, string text) => Task.Run(async () =>
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(cancellation.Token);
        await pipe.WriteAsync(Encoding.UTF8.GetBytes(text + "\n"), cancellation.Token);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
        return JsonNode.Parse(await reader.ReadLineAsync(cancellation.Token) ?? throw new IOException("Host closed before response."))!.AsObject();
    });

    private static async Task TestNegotiatedHostBoundaries(string temp)
    {
        SessionFixture fixture = await PrepareSessionFixture(temp, "boundaries", j => j["optionalCapabilities"] = new JsonArray());
        using AuthorSessionHost host = CreateSessionHost(fixture);
        int processed = 0;
        JsonObject Send(JsonObject frame) => PumpHost(host, SendSessionFrame(fixture.Pipe, frame.ToJsonString()), _ =>
        { processed++; return AuthorSessionOperationResult.Success("source-snapshot", "snapshot complete"); });
        Equal("hello-required", Send(SessionFrame(fixture, "get-source-snapshot"))["code"]!.GetValue<string>(), "No business before hello.");
        JsonObject hello = SessionFrame(fixture, "hello");
        Equal("hello-accepted", Send(hello)["code"]!.GetValue<string>(), "First hello.");
        Equal("request-replay", Send(hello)["code"]!.GetValue<string>(), "Hello replay.");
        Equal("hello-accepted", Send(SessionFrame(fixture, "hello"))["code"]!.GetValue<string>(), "Identical fresh retry.");
        foreach ((string Key, JsonNode? Value, string Code) mutation in new (string, JsonNode?, string)[]
        {
            ("token", JsonValue.Create(new string('b', 43)), "authentication-failed"),
            ("session", JsonValue.Create(Guid.NewGuid().ToString("N")), "authentication-failed"),
            ("gameRoot", JsonValue.Create(Path.Combine(temp, "wrong-root")), "game-root-mismatch"),
            ("schemaVersion", JsonValue.Create(1), "schema-mismatch"),
            ("protocolMajor", JsonValue.Create(99), "negotiation-mismatch"),
            ("apiTarget", JsonValue.Create("99.0.0"), "negotiation-mismatch"),
            ("optionalCapabilities", new JsonArray("reload-content/1"), "negotiation-mismatch"),
            ("hostVersion", JsonValue.Create("0.5.5"), "protocol-mismatch"),
            ("protocolMinor", JsonValue.Create(2), "protocol-mismatch"),
            ("requestId", JsonValue.Create("invalid"), "request-id-invalid"),
            ("operation", JsonValue.Create("future-operation"), "operation-unsupported"),
            ("operation", JsonValue.Create("reload-content"), "capability-not-negotiated")
        })
        {
            JsonObject changed = SessionFrame(fixture, "get-source-snapshot"); changed[mutation.Key] = mutation.Value;
            if (mutation.Key == "schemaVersion") { changed["protocol"] = "dtmapi-author-session/1"; changed["runtime"] = "0.5.5"; }
            Equal(mutation.Code, Send(changed)["code"]!.GetValue<string>(), "Bound request " + mutation.Key);
        }
        JsonObject changedHello = SessionFrame(fixture, "hello"); changedHello["maximumMinor"] = 1;
        Equal("negotiation-mismatch", Send(changedHello)["code"]!.GetValue<string>(), "Hello cannot renegotiate.");
        JsonObject normal = SessionFrame(fixture, "get-source-snapshot"); normal["futureOptionalField"] = true;
        Equal("source-snapshot", Send(normal)["code"]!.GetValue<string>(), "Unknown optional field ignored.");
        Equal("request-replay", Send(normal)["code"]!.GetValue<string>(), "Business replay.");
        string duplicate = SessionFrame(fixture, "hello").ToJsonString().Insert(1, "\"schemaVersion\":2,");
        Equal("request-json-invalid", PumpHost(host, SendSessionFrame(fixture.Pipe, duplicate))["code"]!.GetValue<string>(), "Duplicate request rejected.");
        using var oversized = new MemoryStream(Encoding.UTF8.GetBytes(new string(' ', 32769) + "\n"));
        Equal("request-too-large", AuthorSessionWire.ReadRequest(oversized).Code, "Bounded request frame.");
        True(processed == 1, "Rejected frames never execute business handlers.");
        Console.WriteLine("Negotiated request matrix: OK (hello, replay, identities, capabilities, unknown/duplicate fields, frame bounds).");
    }

    private static JsonObject LegacyDescriptor(JsonObject secret)
    {
        JsonObject legacy = new() { ["schemaVersion"] = 1, ["runtimeVersion"] = "0.5.5" };
        foreach (string key in new[] { "gameRoot", "sessionId", "token", "pipeName", "createdAtUtc", "expiresAtUtc" }) legacy[key] = secret[key]!.DeepClone();
        return legacy;
    }

    private static async Task TestNegotiatedQueueLifetime(string temp)
    {
        SessionFixture f = await PrepareSessionFixture(temp, "queue-lifetime");
        AuthorSessionDescriptorLoadResult load = AuthorSessionDescriptorStore.ConsumeStartup(f.Game, DtmApiRuntime.ApiVersion);
        True(load.Accepted, "Queue fixture offer accepted.");
        True(AuthorSessionHost.TryCreate(load.Descriptor!, f.Game, DtmApiRuntime.ApiVersion, out AuthorSessionHost? created, out _,
            () => DateTimeOffset.UtcNow, 1, 2, TimeSpan.FromSeconds(1)), "Bounded queue Host created.");
        using AuthorSessionHost host = created!; True(host.Start().Accepted, "Queue Host starts.");
        Equal("hello-accepted", SendSessionFrame(f.Pipe, SessionFrame(f, "hello").ToJsonString()).GetAwaiter().GetResult()["code"]!.GetValue<string>(), "Queue hello.");
        void WaitQueued()
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(2);
            while (host.GetSnapshot().Pending == 0 && DateTime.UtcNow < deadline) Thread.Sleep(2);
            True(host.GetSnapshot().Pending == 1, "Business request queued without a Runtime pump.");
        }
        Task<JsonObject> pending = SendSessionFrame(f.Pipe, SessionFrame(f, "get-source-snapshot").ToJsonString());
        WaitQueued();
        JsonObject sameOwner = SessionFrame(f, "get-source-snapshot");
        Equal("owner-request-concurrent", SendSessionFrame(f.Pipe, sameOwner.ToJsonString()).GetAwaiter().GetResult()["code"]!.GetValue<string>(), "Concurrent owner rejected.");
        JsonObject otherOwner = SessionFrame(f, "get-source-snapshot"); otherOwner["uniqueId"] = "Tests.Other";
        Equal("request-queue-full", SendSessionFrame(f.Pipe, otherOwner.ToJsonString()).GetAwaiter().GetResult()["code"]!.GetValue<string>(), "Pending queue bound enforced.");
        Equal("runtime-timeout", pending.GetAwaiter().GetResult()["code"]!.GetValue<string>(), "Queued work times out without executing.");
        True(host.GetSnapshot().Pending == 0 && host.GetSnapshot().InFlight == 0, "Timed-out request is removed.");
        pending = SendSessionFrame(f.Pipe, SessionFrame(f, "get-source-snapshot").ToJsonString()); WaitQueued();
        host.Close("queued-test-close");
        try { pending.GetAwaiter().GetResult(); } catch (IOException) { } // closure may beat the final rejection frame
        True(host.GetSnapshot().Pending == 0 && host.GetSnapshot().InFlight == 0 && host.GetSnapshot().ReplayEntries == 0 && host.GetSnapshot().Closed, "Close clears pending work, ownership and replay state.");
        var held = (AuthorSessionDescriptor)typeof(AuthorSessionHost).GetField("descriptor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(host)!;
        True(held.Token.Length == 0, "Closed Host releases its credential.");
        Console.WriteLine("Negotiated queue lifecycle: OK (concurrency, queue bound, runtime timeout, close/replay/credential cleanup).");
    }

    private static async Task TestLegacyWireAdapter(string temp)
    {
        foreach (string hostVersion in new[] { AuthorSdkContract.TargetRuntimeVersion, DtmApiRuntime.ApiVersion })
        {
            SessionFixture fixture = await PrepareSessionFixture(temp, "legacy-" + hostVersion);
            WriteJson(fixture.Prepared.Values["descriptorPath"], LegacyDescriptor(fixture.Secret));
            AuthorSessionDescriptorLoadResult load = AuthorSessionDescriptorStore.ConsumeStartup(fixture.Game, hostVersion);
            True(load.Accepted, "Legacy descriptor accepted by " + hostVersion);
            True(AuthorSessionHost.TryCreate(load.Descriptor!, fixture.Game, hostVersion, out AuthorSessionHost? created, out _), "Legacy Host creation.");
            using AuthorSessionHost host = created!; True(host.Start().Accepted, "Legacy Host starts.");
            JsonObject request = new()
            {
                ["protocol"] = "dtmapi-author-session/1", ["runtime"] = "0.5.5", ["gameRoot"] = fixture.Game,
                ["session"] = fixture.Secret["sessionId"]!.DeepClone(), ["token"] = fixture.Token,
                ["requestId"] = Guid.NewGuid().ToString("N"), ["uniqueId"] = "Tests.Session", ["selectedRoot"] = fixture.Source,
                ["expectedTreeSha256"] = new string('a', 64), ["operation"] = "get-source-snapshot"
            };
            JsonObject response = PumpHost(host, SendSessionFrame(fixture.Pipe, request.ToJsonString()));
            Equal("0.5.5", response["runtime"]!.GetValue<string>(), "Legacy response wire identity.");
            True(!response.ContainsKey("schemaVersion") && !response.ContainsKey("hostVersion"), "Old strict reader sees only known envelope.");
            Equal(hostVersion, response["values"]!.AsArray().Single(v => v!["key"]!.GetValue<string>() == "hostVersion")!["value"]!.GetValue<string>(), "Legacy status separately reports actual Host.");
            request["requestId"] = Guid.NewGuid().ToString("N"); request["runtime"] = "99.0.0";
            Equal("runtime-mismatch", PumpHost(host, SendSessionFrame(fixture.Pipe, request.ToJsonString()))["code"]!.GetValue<string>(), "Cannot switch legacy alias.");
        }
        SessionFixture unknown = await PrepareSessionFixture(temp, "unknown-legacy");
        JsonObject legacyUnknown = LegacyDescriptor(unknown.Secret); legacyUnknown["runtimeVersion"] = "0.6.1";
        WriteJson(unknown.Prepared.Values["descriptorPath"], legacyUnknown);
        Equal("descriptor-runtime-mismatch", AuthorSessionDescriptorStore.ConsumeStartup(unknown.Game, DtmApiRuntime.ApiVersion).Code, "Arbitrary legacy aliases rejected.");
        Console.WriteLine("Legacy schema 1 adapter: OK (old/current Host, exact wire response, unsupported alias).");
    }

    private static Task<JsonObject> RespondOnce(string pipeName, Func<JsonObject, string> responseFactory) => Task.Run(async () =>
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipe.WaitForConnectionAsync(cancellation.Token);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
        JsonObject request = JsonNode.Parse((await reader.ReadLineAsync(cancellation.Token))!)!.AsObject();
        await pipe.WriteAsync(Encoding.UTF8.GetBytes(responseFactory(request) + "\n"), cancellation.Token);
        return request;
    });

    private static async Task TestSessionClientFailureWindows(string temp)
    {
        foreach (bool consumedByOldHost in new[] { false, true })
        {
            SessionFixture f = await PrepareSessionFixture(temp, "no-listener-" + consumedByOldHost);
            if (consumedByOldHost) File.Delete(f.Prepared.Values["descriptorPath"]); // old reader rejects schema 2 and creates no listener
            CommandReport r = await ExpectFailure("no listener", "session", "snapshot", "Tests.Session", f.Source, "--game-root", f.Game, "--timeout-seconds", "1");
            Equal(OperatingSystem.IsWindows() ? "host-unavailable" : "handshake-timeout", r.Values["statusCode"], "No reply cannot prove an old Host.");
            True(!r.Values.ContainsKey("hostVersion"), "No invented Host version.");
        }
        SessionFixture silent = await PrepareSessionFixture(temp, "silent-hello");
        Task<JsonObject> silentServer = StartSilentSessionServer(silent.Pipe, TimeSpan.FromMilliseconds(1400));
        CommandReport timeout = await ExpectFailure("silent hello", "session", "snapshot", "Tests.Session", silent.Source, "--game-root", silent.Game, "--timeout-seconds", "1");
        await silentServer;
        Equal("handshake-timeout", timeout.Values["statusCode"], "Connected silent Host is a handshake timeout.");
        foreach ((string Name, Action<JsonObject> Mutation, string Code) fault in new (string, Action<JsonObject>, string)[]
        {
            ("request", r => r["requestId"] = Guid.NewGuid().ToString("N"), "pipe-response-identity-mismatch"),
            ("session", r => r["session"] = Guid.NewGuid().ToString("N"), "pipe-response-identity-mismatch"),
            ("root", r => r["gameRoot"] = Path.Combine(temp, "other"), "pipe-response-identity-mismatch"),
            ("protocol", r => r["protocolMajor"] = 99, "pipe-response-identity-mismatch"),
            ("minor", r => r["protocolMinor"] = 5, "pipe-response-negotiation-invalid"),
            ("capability", r => r["acceptedCapabilities"] = new JsonArray("unrequested/1"), "pipe-response-negotiation-invalid"),
            ("upgrade", r => { r["hostVersion"] = "0.5.5"; r["status"] = "rejected"; r["code"] = "upgrade-required"; }, "upgrade-required")
        })
        {
            SessionFixture f = await PrepareSessionFixture(temp, "response-" + fault.Name);
            Task<JsonObject> server = RespondOnce(f.Pipe, request =>
            {
                True(request["token"]!.GetValue<string>() == f.Token, "Fault server receives authenticated hello.");
                JsonObject response = BuildSessionResponse(request, "ok", "hello-accepted", "hello complete", string.Empty);
                fault.Mutation(response); return response.ToJsonString();
            });
            CommandReport r = await ExpectFailure("response " + fault.Name, "session", "snapshot", "Tests.Session", f.Source, "--game-root", f.Game, "--timeout-seconds", "2");
            await server; Equal(fault.Code, r.Values["statusCode"], "Client validates " + fault.Name);
        }
        SessionFixture duplicate = await PrepareSessionFixture(temp, "response-duplicate");
        Task<JsonObject> duplicateServer = RespondOnce(duplicate.Pipe, request => BuildSessionResponse(request, "ok", "hello-accepted", "hello complete", "").ToJsonString().Insert(1, "\"schemaVersion\":2,"));
        CommandReport duplicateReport = await ExpectFailure("response duplicate", "session", "snapshot", "Tests.Session", duplicate.Source, "--game-root", duplicate.Game, "--timeout-seconds", "2");
        await duplicateServer; Equal("pipe-response-invalid", duplicateReport.Values["statusCode"], "Duplicate response cannot override identity.");
        Console.WriteLine("SDK failure windows: OK (absent/old-reject/silent Host, authenticated incompatibility, response identities/capabilities/duplicates).");
    }

    private static async Task<CommandReport> RunLegacySdk(string executable, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.ArgumentList.Add("--json");
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(); Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        True(process.ExitCode == 0, "Pre-change SDK subprocess succeeded; stderr=" + await error);
        return JsonSerializer.Deserialize<CommandReport>(await output, JsonOptions)!;
    }

    private static async Task TestActualLegacySdkWhenProvided(string temp)
    {
        string? executable = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_LEGACY_EXE");
        if (string.IsNullOrWhiteSpace(executable)) { Console.WriteLine("Pre-change SDK executable not provided; legacy wire matrix executed."); return; }
        string game = NewGameRoot(temp, "published-sdk-current-host"); string source = Path.Combine(game, "selected-source"); Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "manifest.json"), "{\"UniqueID\":\"Tests.Session\",\"Name\":\"Session\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\"}");
        CommandReport prepared = await RunLegacySdk(executable, "session", "prepare", "--game-root", game);
        JsonObject secret = JsonNode.Parse(File.ReadAllText(prepared.Values["descriptorPath"]))!.AsObject();
        True(secret["schemaVersion"]!.GetValue<int>() == 1, "Actual pre-change SDK emits schema 1.");
        using AuthorSessionHost host = CreateSessionHost(new SessionFixture(game, source, prepared, secret));
        CommandReport snapshot = PumpHost(host, RunLegacySdk(executable, "session", "snapshot", "Tests.Session", source, "--game-root", game));
        True(snapshot.Success, "Actual pre-change SDK response validator accepts current Core.");
        Equal(DtmApiRuntime.ApiVersion, snapshot.Values["response.hostVersion"], "Old SDK sees Host in compatible status.");
        Console.WriteLine("Pre-change SDK executable → current Core → pre-change SDK validator: OK.");
    }
}
