using DTMAPI.Authoring.Contracts;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using DTMAPI.Internal;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.AuthorSdk;

internal static class AuthorSessionService
{
    private const string SnapshotOperation = "get-source-snapshot";
    private const string ReloadOperation = "reload-content";
    private const string CommandOperation = "execute-command";
    private const int MaximumResponseBytes = 64 * 1024;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MaximumSessionLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(1);
    private static readonly Regex UniqueIdPattern = new(@"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions WireJson = CreateWireJson();

    public static async Task<CommandReport> ExecuteAsync(ParsedCommand command)
    {
        if (command.Positionals.Count == 0)
            throw new CommandLineException("session requires prepare, snapshot, reload, command, or clear.");
        string action = command.Positionals[0].ToLowerInvariant();
        if (action is not ("prepare" or "snapshot" or "reload" or "command" or "clear"))
            throw new CommandLineException("Unknown session operation. Use prepare, snapshot, reload, command, or clear.");
        command.RequireOnlyOptions(action == "command" ? new[] { "game-root", "timeout-seconds", "command-line" } : action is "snapshot" or "reload"
            ? new[] { "game-root", "timeout-seconds" }
            : action == "prepare" ? new[] { "game-root", "api-target", "commands" } : new[] { "game-root" });
        string gameRoot = RequireGameRoot(command);
        var report = new CommandReport { Command = "session " + action, RootPath = gameRoot };
        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            if (action == "prepare")
                Prepare(command, gameRoot, report);
            else if (action == "clear")
                Clear(command, gameRoot, report);
            else
                await RequestAsync(command, gameRoot, action, report).ConfigureAwait(false);
        }
        catch (CommandLineException)
        {
            throw;
        }
        catch (SessionCommandException ex)
        {
            report.Values["statusCode"] = ex.Code;
            report.Diagnostics.Add(Error("SDK601", ex.Message, ex.Path));
        }
        catch (Exception ex)
        {
            report.Values["statusCode"] = "session-io-error";
            report.Diagnostics.Add(Error("SDK601", "Author session operation failed without starting the game or changing Mod files: " + ex.Message, AuthorStatePaths.InstallationStateRoot(gameRoot)));
        }
        report.Success = report.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        return report;
    }

    private static void Prepare(ParsedCommand command, string gameRoot, CommandReport report)
    {
        RequirePositionals(command, 1, "session prepare takes no additional arguments.");
        if (!bool.TryParse(command.Option("commands", "false"), out bool enableCommands)) throw new CommandLineException("--commands must be true or false.");
        AuthorApiTarget apiTarget = SdkApiTargets.ForCommand(command.Option("api-target", AuthorApiTargetCatalog.Current.DefaultTarget));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        EnsureSessionSlotAvailable(gameRoot, now);
        string sessionId = Guid.NewGuid().ToString("N");
        string token = CreateToken();
        string pipeName = CreatePipeName(gameRoot, sessionId);
        var secret = new AuthorSessionSecret
        {
            SchemaVersion = AuthorSessionContract.SchemaVersion,
            GameRoot = gameRoot,
            ApiTarget = apiTarget.ApiTarget,
            MinimumRuntimeVersion = Version.Parse(apiTarget.MinimumRuntimeVersion) > Version.Parse(AuthorSessionContract.MinimumRuntimeVersion)
                ? apiTarget.MinimumRuntimeVersion : AuthorSessionContract.MinimumRuntimeVersion,
            SessionId = sessionId,
            Token = token,
            PipeName = pipeName,
            CreatedAtUtc = now.ToString("O", CultureInfo.InvariantCulture),
            ExpiresAtUtc = now.Add(SessionLifetime).ToString("O", CultureInfo.InvariantCulture)
        };
        if (enableCommands) secret.OptionalCapabilities = secret.OptionalCapabilities.Append(AuthorSessionContract.CommandCapability).ToArray();
        string clientPath = AuthorStatePaths.AuthorSessionClientPath(gameRoot);
        string descriptorPath = AuthorStatePaths.AuthorSessionDescriptorPath(gameRoot);
        bool clientWritten = false;
        bool descriptorWritten = false;
        try
        {
            SecureSecretFile.WriteNew(clientPath, secret);
            clientWritten = true;
            AuthorFaultInjector.Hit("session.prepare.after-client");
            SecureSecretFile.WriteNew(descriptorPath, secret);
            descriptorWritten = true;
            AuthorFaultInjector.Hit("session.prepare.after-descriptor");
        }
        catch
        {
            if (clientWritten)
                TryDelete(clientPath);
            if (descriptorWritten)
                TryDelete(descriptorPath);
            throw;
        }

        report.Success = true;
        report.Values["statusCode"] = "session-prepared";
        report.Values["sessionId"] = sessionId;
        report.Values["pipeName"] = pipeName;
        report.Values["createdAtUtc"] = secret.CreatedAtUtc;
        report.Values["expiresAtUtc"] = secret.ExpiresAtUtc;
        report.Values["descriptorPath"] = descriptorPath;
        report.Values["credentialPath"] = clientPath;
        report.Diagnostics.Add(Info("SDK600", "Wrote one short-lived startup descriptor and a protected client credential. Start the game explicitly before sending a request; the SDK did not launch or poll it."));
    }

    private static void Clear(ParsedCommand command, string gameRoot, CommandReport report)
    {
        RequirePositionals(command, 1, "session clear takes no additional arguments.");
        string descriptorPath = AuthorStatePaths.AuthorSessionDescriptorPath(gameRoot);
        string clientPath = AuthorStatePaths.AuthorSessionClientPath(gameRoot);
        int removed = 0;
        if (File.Exists(descriptorPath))
        {
            File.Delete(descriptorPath);
            removed++;
        }
        if (File.Exists(clientPath))
        {
            File.Delete(clientPath);
            removed++;
        }
        report.Success = true;
        report.Values["statusCode"] = "session-cleared";
        report.Values["removedFiles"] = removed.ToString(CultureInfo.InvariantCulture);
        report.Diagnostics.Add(Info("SDK600", "Cleared only the reserved startup descriptor/client credential paths. No Runtime, game, or Mod file was changed."));
    }

    private static async Task RequestAsync(ParsedCommand command, string gameRoot, string action, CommandReport report)
    {
        RequirePositionals(command, 3, "session " + action + " requires UniqueID and selectedRoot.");
        string uniqueId = command.Positionals[1];
        if (!UniqueIdPattern.IsMatch(uniqueId))
            throw new SessionCommandException("unique-id-invalid", "UniqueID must be dotted and path-safe.", command.Positionals[1]);
        string selectedRoot = Path.GetFullPath(command.Positionals[2]);
        PathSafety.RejectBepInExPluginDestination(selectedRoot);
        if (!Directory.Exists(selectedRoot))
            throw new SessionCommandException("selected-root-missing", "The selected source root does not exist.", selectedRoot);
        ValidateSelectedManifest(selectedRoot, uniqueId);

        AuthorSessionSecret secret = LoadClientSecret(gameRoot, DateTimeOffset.UtcNow);
        int responseTimeoutSeconds = ParseTimeoutSeconds(command.Option("timeout-seconds", "35"));
        int connectTimeoutMilliseconds = Math.Min(5000, checked(responseTimeoutSeconds * 1000));
        string treeSha256 = AuthorFileTreeDigest.Compute(selectedRoot);
        string operation = action == "snapshot" ? SnapshotOperation : action == "command" ? CommandOperation : ReloadOperation;
        if (secret.SchemaVersion != AuthorSessionContract.SchemaVersion)
            throw new SessionCommandException("session-prepare-required", "Clear the legacy credential and prepare a new explicit session; the SDK does not downgrade its protocol.", gameRoot);
        AuthorSessionRequestFrame hello = CreateRequest(secret, "hello");
        AuthorSessionResponseFrame helloResponse = await SendAsync(secret, hello, connectTimeoutMilliseconds, responseTimeoutSeconds * 1000).ConfigureAwait(false);
        ValidateResponse(helloResponse, hello, secret);
        if (helloResponse.Status != "ok")
        {
            bool incompatible = helloResponse.Code is "upgrade-required" or "protocol-major-unsupported" or "protocol-minor-incompatible" or "required-capability-unsupported";
            throw new SessionCommandException(incompatible ? "upgrade-required" : Redact(helloResponse.Code, secret.Token),
                "Authenticated Host rejected hello: " + Redact(helloResponse.Message, secret.Token), secret.PipeName);
        }
        if (secret.Negotiated == null)
        {
            secret.Negotiated = new AuthorSessionNegotiation
            {
                HostVersion = helloResponse.HostVersion!, ProtocolMinor = helloResponse.ProtocolMinor!.Value,
                AcceptedCapabilities = helloResponse.AcceptedCapabilities!.ToArray(),
                UnsupportedOptionalCapabilities = helloResponse.UnsupportedOptionalCapabilities!.ToArray()
            };
            SecureSecretFile.Replace(AuthorStatePaths.AuthorSessionClientPath(gameRoot), secret);
        }
        if (!secret.Negotiated.AcceptedCapabilities.Contains(operation + "/1", StringComparer.Ordinal))
            throw new SessionCommandException("capability-unsupported", "The Host did not enable this optional operation.", secret.PipeName);
        AuthorSessionRequestFrame request = CreateRequest(secret, operation);
        request.UniqueId = uniqueId;
        request.SelectedRoot = selectedRoot;
        request.ExpectedTreeSha256 = treeSha256;
        if (action == "command")
        {
            request.CommandLine = command.Option("command-line", "help");
            if (request.CommandLine.Length > 4096) throw new CommandLineException("--command-line is limited to 4096 characters.");
        }
        AuthorSessionResponseFrame response = await SendAsync(secret, request, connectTimeoutMilliseconds, responseTimeoutSeconds * 1000).ConfigureAwait(false);
        ValidateResponse(response, request, secret);

        report.OutputPath = selectedRoot;
        report.Sha256 = treeSha256;
        string safeCode = Redact(response.Code, secret.Token);
        report.Values["statusCode"] = safeCode;
        report.Values["runtimeStatus"] = response.Status;
        report.Values["runtimeCode"] = safeCode;
        report.Values["runtimeMessage"] = Redact(response.Message, secret.Token);
        report.Values["sessionId"] = secret.SessionId;
        report.Values["requestId"] = request.RequestId;
        report.Values["operation"] = operation;
        report.Values["hostVersion"] = secret.Negotiated.HostVersion;
        report.Values["apiTarget"] = secret.ApiTarget;
        report.Values["protocol"] = secret.ProtocolMajor + "." + secret.Negotiated.ProtocolMinor;
        report.Values["acceptedCapabilities"] = string.Join(",", secret.Negotiated.AcceptedCapabilities);
        report.Values["unsupportedOptionalCapabilities"] = string.Join(",", secret.Negotiated.UnsupportedOptionalCapabilities);
        report.Values["uniqueID"] = uniqueId;
        report.Values["selectedRoot"] = selectedRoot;
        report.Values["expectedTreeSha256"] = treeSha256;
        report.Values["treeDigestAlgorithm"] = AuthorFileTreeDigest.AlgorithmId;
        report.Values["connectTimeoutMs"] = connectTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture);
        report.Values["responseTimeoutMs"] = (responseTimeoutSeconds * 1000).ToString(CultureInfo.InvariantCulture);
        int responseValueIndex = 0;
        foreach (AuthorSessionResponseValueFrame value in response.Values.OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            string key = value.Key.Contains(secret.Token, StringComparison.Ordinal)
                ? "redacted-key-" + responseValueIndex.ToString(CultureInfo.InvariantCulture)
                : value.Key;
            report.Values["response." + key] = Redact(value.Value, secret.Token);
            responseValueIndex++;
        }

        if (response.Status.Equals("ok", StringComparison.Ordinal))
        {
            report.Success = true;
            report.Diagnostics.Add(Info("SDK600", "Runtime completed the authenticated explicit-session request."));
        }
        else if (response.Status.Equals("restart-required", StringComparison.Ordinal))
        {
            report.Success = true;
            report.Diagnostics.Add(Warning("SDK602", "Runtime accepted the request boundary but requires restart: " + Redact(response.Message, secret.Token), selectedRoot));
        }
        else
        {
            report.Diagnostics.Add(Error("SDK603", "Runtime returned " + response.Status + "/" + safeCode + ": " + Redact(response.Message, secret.Token), selectedRoot));
        }
    }

    private static async Task<AuthorSessionResponseFrame> SendAsync(AuthorSessionSecret secret, AuthorSessionRequestFrame request, int connectTimeoutMilliseconds, int responseTimeoutMilliseconds)
    {
        using var pipe = new NamedPipeClientStream(".", secret.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            using var connectCancellation = new CancellationTokenSource(connectTimeoutMilliseconds);
            await pipe.ConnectAsync(connectCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw ConnectionTimeout(secret, ex);
        }
        catch (TimeoutException ex)
        {
            throw ConnectionTimeout(secret, ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SessionCommandException("host-unavailable", "Could not connect to the explicit Runtime session. Start the game, check its logs and verify versions: " + ex.GetType().Name + ".", secret.PipeName, ex);
        }

        byte[] payload = new UTF8Encoding(false, true).GetBytes(JsonSerializer.Serialize(request, WireJson) + "\n");
        try
        {
            using var responseCancellation = new CancellationTokenSource(responseTimeoutMilliseconds);
            await pipe.WriteAsync(payload.AsMemory(), responseCancellation.Token).ConfigureAwait(false);
            await pipe.FlushAsync(responseCancellation.Token).ConfigureAwait(false);
            byte[] responseBytes = await ReadFrameAsync(pipe, responseCancellation.Token).ConfigureAwait(false);
            AuthorSessionJson.Validate(responseBytes, "response");
            return JsonSerializer.Deserialize<AuthorSessionResponseFrame>(responseBytes, WireJson)
                ?? throw new SessionCommandException("pipe-response-invalid", "Runtime returned an empty JSON response.", secret.PipeName);
        }
        catch (OperationCanceledException ex)
        {
            throw new SessionCommandException(request.Operation == "hello" ? "handshake-timeout" : "pipe-response-timeout", "Timed out waiting for the Runtime response. Check game logs and versions; no Host version was inferred.", secret.PipeName, ex);
        }
        catch (SessionCommandException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or DecoderFallbackException or System.Xml.XmlException or FormatException or OverflowException)
        {
            throw new SessionCommandException("pipe-response-invalid", "Runtime response framing/JSON was invalid: " + ex.GetType().Name + ".", secret.PipeName, ex);
        }
    }

    private static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        byte[] single = new byte[1];
        while (true)
        {
            int read = await stream.ReadAsync(single.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new SessionCommandException("pipe-response-incomplete", "Runtime closed the pipe before the newline response delimiter.", string.Empty);
            if (single[0] == (byte)'\n')
                break;
            if (single[0] == (byte)'\r')
                continue;
            if (buffer.Length >= MaximumResponseBytes)
                throw new SessionCommandException("pipe-response-too-large", "Runtime response exceeded the 64 KiB frame limit.", string.Empty);
            buffer.WriteByte(single[0]);
        }
        if (buffer.Length == 0)
            throw new SessionCommandException("pipe-response-invalid", "Runtime returned an empty response frame.", string.Empty);
        return buffer.ToArray();
    }

    private static void ValidateResponse(AuthorSessionResponseFrame response, AuthorSessionRequestFrame request, AuthorSessionSecret secret)
    {
        if (response.SchemaVersion != AuthorSessionContract.SchemaVersion || !string.IsNullOrEmpty(response.Protocol) || !string.IsNullOrEmpty(response.Runtime)
            || response.ProtocolMajor != secret.ProtocolMajor || !response.ProtocolMinor.HasValue
            || !PathsEqual(response.GameRoot ?? string.Empty, secret.GameRoot)
            || response.ApiTarget != secret.ApiTarget || !Version.TryParse(response.HostVersion, out _)
            || !string.Equals(response.Session, secret.SessionId, StringComparison.Ordinal)
            || !string.Equals(response.RequestId, request.RequestId, StringComparison.Ordinal)
            || !string.Equals(response.Operation, request.Operation, StringComparison.Ordinal)
            || !string.Equals(response.UniqueId, request.UniqueId, StringComparison.Ordinal))
            throw new SessionCommandException("pipe-response-identity-mismatch", "Runtime response protocol/session/request/owner identity did not exactly match the request.", secret.PipeName);
        if (response.Status is not ("ok" or "rejected" or "restart-required" or "error"))
            throw new SessionCommandException("pipe-response-status-invalid", "Runtime response status was unsupported.", secret.PipeName);
        if (!IsBoundedSingleLine(response.Code, 96) || !IsBoundedSingleLine(response.Message, 512))
            throw new SessionCommandException("pipe-response-fields-invalid", "Runtime response code/message was empty, multiline, or oversized.", secret.PipeName);
        if (response.Values == null || response.Values.Count > 32)
            throw new SessionCommandException("pipe-response-values-invalid", "Runtime response values exceeded the bounded protocol.", secret.PipeName);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (AuthorSessionResponseValueFrame? value in response.Values)
        {
            if (value == null || !IsSafeIdentifier(value.Key, 64) || value.Value == null || value.Value.Length > 2048 || !seen.Add(value.Key))
                throw new SessionCommandException("pipe-response-values-invalid", "Runtime response values contained an invalid or duplicate key/value.", secret.PipeName);
        }
        string[]? accepted = response.AcceptedCapabilities;
        string[]? unsupported = response.UnsupportedOptionalCapabilities;
        if (accepted == null || unsupported == null || accepted.Length > 32 || unsupported.Length > 32 ||
            accepted.Distinct(StringComparer.Ordinal).Count() != accepted.Length || unsupported.Distinct(StringComparer.Ordinal).Count() != unsupported.Length ||
            accepted.Any(value => value == null) || unsupported.Any(value => value == null))
            throw new SessionCommandException("pipe-response-capabilities-invalid", "Host capability response is invalid.", secret.PipeName);
        if (response.Status == "ok" || request.Operation != "hello")
        {
            if (response.ProtocolMinor < secret.MinimumMinor || response.ProtocolMinor > secret.MaximumMinor ||
                Version.Parse(response.HostVersion!) < Version.Parse(secret.MinimumRuntimeVersion) ||
                secret.RequiredCapabilities.Except(accepted, StringComparer.Ordinal).Any() ||
                accepted.Except(secret.RequiredCapabilities.Concat(secret.OptionalCapabilities), StringComparer.Ordinal).Any() ||
                !unsupported.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(secret.OptionalCapabilities.Except(accepted, StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new SessionCommandException("pipe-response-negotiation-invalid", "Host selection does not satisfy the requested protocol and capabilities.", secret.PipeName);
        }
        if (secret.Negotiated != null && (response.HostVersion != secret.Negotiated.HostVersion || response.ProtocolMinor != secret.Negotiated.ProtocolMinor ||
            !accepted.SequenceEqual(secret.Negotiated.AcceptedCapabilities, StringComparer.Ordinal) ||
            !unsupported.SequenceEqual(secret.Negotiated.UnsupportedOptionalCapabilities, StringComparer.Ordinal)))
            throw new SessionCommandException("pipe-response-negotiation-mismatch", "Host response changed the established session negotiation.", secret.PipeName);
    }

    private static AuthorSessionSecret LoadClientSecret(string gameRoot, DateTimeOffset now)
    {
        string path = AuthorStatePaths.AuthorSessionClientPath(gameRoot);
        if (!File.Exists(path))
            throw new SessionCommandException("session-credential-missing", "The protected session client credential is missing. Run session prepare before starting the game.", path);
        AuthorSessionSecret secret;
        try
        {
            secret = ReadSecret(path);
        }
        catch (Exception ex)
        {
            throw new SessionCommandException("session-credential-invalid", "The protected session client credential is invalid; use session clear before preparing another.", path, ex);
        }
        DateTimeOffset expires = ValidateSecret(secret, gameRoot, now);
        if (expires <= now)
        {
            File.Delete(path);
            DeleteMatchingDescriptor(gameRoot, secret);
            throw new SessionCommandException("session-expired", "The explicit Author session expired and its client credential was cleared.", path);
        }
        return secret;
    }

    private static void EnsureSessionSlotAvailable(string gameRoot, DateTimeOffset now)
    {
        string descriptorPath = AuthorStatePaths.AuthorSessionDescriptorPath(gameRoot);
        string clientPath = AuthorStatePaths.AuthorSessionClientPath(gameRoot);
        AuthorSessionSecret? descriptor = ReadExisting(descriptorPath, gameRoot, now);
        AuthorSessionSecret? client = ReadExisting(clientPath, gameRoot, now);
        if (descriptor == null && client == null)
            return;
        if (descriptor != null && client != null && !SameSecret(descriptor, client))
            throw new SessionCommandException("session-state-mismatch", "Startup descriptor and client credential do not describe the same session; use session clear explicitly.", descriptorPath);
        DateTimeOffset descriptorExpiry = descriptor == null ? DateTimeOffset.MinValue : ParseUtc(descriptor.ExpiresAtUtc, "expiresAtUtc");
        DateTimeOffset clientExpiry = client == null ? DateTimeOffset.MinValue : ParseUtc(client.ExpiresAtUtc, "expiresAtUtc");
        if (descriptorExpiry > now || clientExpiry > now)
            throw new SessionCommandException("session-already-prepared", "An unexpired explicit Author session already exists. Use it or run session clear explicitly.", client != null ? clientPath : descriptorPath);
        if (descriptor != null)
            File.Delete(descriptorPath);
        if (client != null)
            File.Delete(clientPath);
    }

    private static AuthorSessionSecret? ReadExisting(string path, string gameRoot, DateTimeOffset now)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            AuthorSessionSecret value = ReadSecret(path);
            ValidateSecret(value, gameRoot, now);
            return value;
        }
        catch (SessionCommandException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SessionCommandException("session-state-invalid", "Existing reserved session state is invalid; use session clear explicitly.", path, ex);
        }
    }

    private static DateTimeOffset ValidateSecret(AuthorSessionSecret secret, string gameRoot, DateTimeOffset now)
    {
        if ((secret.SchemaVersion != 1 && secret.SchemaVersion != AuthorSessionContract.SchemaVersion)
            || !PathsEqual(secret.GameRoot, gameRoot)
            || (secret.SchemaVersion == 1 && secret.RuntimeVersion != AuthorSessionContract.LegacyWireVersion)
            || (secret.SchemaVersion == 2 && (!string.IsNullOrEmpty(secret.RuntimeVersion) || secret.ProtocolMajor < 1 || secret.MinimumMinor < 0 || secret.MaximumMinor < secret.MinimumMinor ||
                !Version.TryParse(secret.ApiTarget, out _) || !Version.TryParse(secret.MinimumRuntimeVersion, out _) || secret.RequiredCapabilities == null || secret.OptionalCapabilities == null))
            || !Guid.TryParseExact(secret.SessionId, "N", out Guid sessionId)
            || sessionId == Guid.Empty
            || !IsValidToken(secret.Token)
            || !secret.PipeName.Equals(CreatePipeName(gameRoot, secret.SessionId), StringComparison.Ordinal))
            throw new SessionCommandException("session-credential-invalid", "Session credential schema/root/runtime/session/token/pipe identity is invalid.", AuthorStatePaths.AuthorSessionClientPath(gameRoot));
        DateTimeOffset created = ParseUtc(secret.CreatedAtUtc, "createdAtUtc");
        DateTimeOffset expires = ParseUtc(secret.ExpiresAtUtc, "expiresAtUtc");
        if (created > now + MaximumFutureClockSkew || expires <= created || expires - created > MaximumSessionLifetime)
            throw new SessionCommandException("session-time-invalid", "Session credential timestamps are outside the short-lived boundary.", AuthorStatePaths.AuthorSessionClientPath(gameRoot));
        return expires;
    }

    private static void DeleteMatchingDescriptor(string gameRoot, AuthorSessionSecret secret)
    {
        string path = AuthorStatePaths.AuthorSessionDescriptorPath(gameRoot);
        if (!File.Exists(path))
            return;
        try
        {
            AuthorSessionSecret descriptor = ReadSecret(path);
            if (SameSecret(descriptor, secret))
                File.Delete(path);
        }
        catch
        {
            // Preserve mismatched or damaged descriptor evidence; session clear is the explicit authority.
        }
    }

    private static void ValidateSelectedManifest(string selectedRoot, string uniqueId)
    {
        string rootManifest = Path.Combine(selectedRoot, "manifest.json");
        string packageManifest = Path.Combine(selectedRoot, "Content", "DTMAPI", "manifest.json");
        string[] present = new[] { rootManifest, packageManifest }.Where(File.Exists).ToArray();
        if (present.Length != 1)
            throw new SessionCommandException("selected-manifest-invalid", "selectedRoot must contain exactly one root or Content/DTMAPI manifest.json.", selectedRoot);
        RuntimeManifest manifest = JsonSerializer.Deserialize<RuntimeManifest>(File.ReadAllText(present[0], Encoding.UTF8), JsonSupport.RuntimeManifest)
            ?? throw new SessionCommandException("selected-manifest-invalid", "selectedRoot manifest did not contain one object.", present[0]);
        if (!manifest.UniqueID.Equals(uniqueId, StringComparison.Ordinal) || manifest.Type is not ("CodeMod" or "ContentPack"))
            throw new SessionCommandException("selected-manifest-mismatch", "selectedRoot manifest UniqueID/type does not exactly match this request.", present[0]);
    }

    private static string CreateToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string CreatePipeName(string gameRoot, string sessionId)
    {
        if (!Guid.TryParseExact(sessionId, "N", out Guid parsed) || parsed == Guid.Empty)
            throw new SessionCommandException("session-id-invalid", "sessionId must be a non-empty GUID in N format.", sessionId);
        return "dtmapi-author-" + AuthorStatePaths.GameRootKey(gameRoot) + "-" + parsed.ToString("N");
    }

    private static bool IsValidToken(string token)
    {
        if (token == null || token.Length < 43 || token.Length > 128)
            return false;
        return token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static DateTimeOffset ParseUtc(string value, string label)
    {
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed) || parsed.Offset != TimeSpan.Zero)
            throw new SessionCommandException("session-time-invalid", label + " must be a round-trip UTC timestamp.", string.Empty);
        return parsed;
    }

    private static int ParseTimeoutSeconds(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 1 || parsed > 60)
            throw new CommandLineException("--timeout-seconds must be an integer from 1 through 60.");
        return parsed;
    }

    private static bool SameSecret(AuthorSessionSecret left, AuthorSessionSecret right) =>
        left.SchemaVersion == right.SchemaVersion
        && PathsEqual(left.GameRoot, right.GameRoot)
        && string.Equals(left.RuntimeVersion, right.RuntimeVersion, StringComparison.Ordinal)
        && left.ProtocolMajor == right.ProtocolMajor && left.MinimumMinor == right.MinimumMinor && left.MaximumMinor == right.MaximumMinor
        && left.ApiTarget == right.ApiTarget && left.MinimumRuntimeVersion == right.MinimumRuntimeVersion
        && left.RequiredCapabilities.SequenceEqual(right.RequiredCapabilities, StringComparer.Ordinal)
        && left.OptionalCapabilities.SequenceEqual(right.OptionalCapabilities, StringComparer.Ordinal)
        && left.SessionId.Equals(right.SessionId, StringComparison.Ordinal)
        && left.Token.Equals(right.Token, StringComparison.Ordinal)
        && left.PipeName.Equals(right.PipeName, StringComparison.Ordinal)
        && left.CreatedAtUtc.Equals(right.CreatedAtUtc, StringComparison.Ordinal)
        && left.ExpiresAtUtc.Equals(right.ExpiresAtUtc, StringComparison.Ordinal);

    private static JsonSerializerOptions CreateWireJson()
    {
        var options = new JsonSerializerOptions(JsonSupport.Tool) { WriteIndented = false, PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, MaxDepth = 32 };
        return options;
    }

    private static AuthorSessionSecret ReadSecret(string path)
    {
        if (new FileInfo(path).Length > 32 * 1024) throw new InvalidDataException("Session credential exceeds its size limit.");
        byte[] bytes = File.ReadAllBytes(path);
        AuthorSessionJson.Validate(bytes, "descriptor");
        return JsonSerializer.Deserialize<AuthorSessionSecret>(bytes, WireJson) ?? throw new InvalidDataException("Empty credential.");
    }

    private static AuthorSessionRequestFrame CreateRequest(AuthorSessionSecret secret, string operation) => new()
    {
        SchemaVersion = secret.SchemaVersion, ProtocolMajor = secret.ProtocolMajor,
        MinimumMinor = secret.MinimumMinor, MaximumMinor = secret.MaximumMinor,
        ApiTarget = secret.ApiTarget, MinimumRuntimeVersion = secret.MinimumRuntimeVersion,
        RequiredCapabilities = secret.RequiredCapabilities.ToArray(), OptionalCapabilities = secret.OptionalCapabilities.ToArray(),
        GameRoot = secret.GameRoot, Session = secret.SessionId, Token = secret.Token,
        RequestId = Guid.NewGuid().ToString("N"), Operation = operation,
        HostVersion = secret.Negotiated?.HostVersion, ProtocolMinor = secret.Negotiated?.ProtocolMinor
    };

    private static SessionCommandException ConnectionTimeout(AuthorSessionSecret secret, Exception exception)
    {
        bool absent = false;
        if (OperatingSystem.IsWindows())
        {
            bool available = WaitNamedPipe("\\\\.\\pipe\\" + secret.PipeName, 0);
            int error = Marshal.GetLastWin32Error();
            absent = !available && (error == 2 || error == 3);
        }
        return new SessionCommandException(absent ? "host-unavailable" : "handshake-timeout",
            "The Host did not answer. Start the game, check its logs and verify versions; no Host version or upgrade requirement was inferred.", secret.PipeName, exception);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "WaitNamedPipeW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WaitNamedPipe(string name, uint timeout);

    private static bool IsBoundedSingleLine(string? value, int maximum) => value != null && value.Length > 0 && value.Length <= maximum && !value.Any(char.IsControl);

    private static bool IsSafeIdentifier(string? value, int maximum) =>
        value != null && value.Length > 0 && value.Length <= maximum && value.Equals(value.Trim(), StringComparison.Ordinal) && !value.Any(character => char.IsControl(character) || character is '/' or '\\');

    private static string Redact(string value, string token) => string.IsNullOrEmpty(token) ? value : value.Replace(token, "[redacted]", StringComparison.Ordinal);

    private static string RequireGameRoot(ParsedCommand command)
    {
        string value = command.Option("game-root");
        if (string.IsNullOrWhiteSpace(value))
            throw new CommandLineException("session requires --game-root.");
        string root = AuthorStatePaths.CanonicalGameRoot(value);
        if (!Directory.Exists(root))
            throw new CommandLineException("Game root does not exist: " + root);
        return root;
    }

    private static void RequirePositionals(ParsedCommand command, int count, string message)
    {
        if (command.Positionals.Count != count)
            throw new CommandLineException(message);
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(AuthorStatePaths.CanonicalGameRoot(left), AuthorStatePaths.CanonicalGameRoot(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // The caller reports the original failure; a retained protected file is explicit recovery evidence.
        }
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Warning(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DiagnosticSeverity.Info, Message = message };
}

internal static class SecureSecretFile
{
    public static void WriteNew<T>(string path, T value) => Write(path, value, false);
    public static void Replace<T>(string path, T value) => Write(path, value, true);
    private static void Write<T>(string path, T value, bool replace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }
            Harden(temporary);
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonSupport.SerializeTool(value));
            using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            File.Move(temporary, path, overwrite: replace);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static void Harden(string path)
    {
        if (OperatingSystem.IsWindows())
            HardenWindows(path);
        else
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [SupportedOSPlatform("windows")]
    private static void HardenWindows(string path)
    {
        SecurityIdentifier owner = WindowsIdentity.GetCurrent().User
            ?? throw new UnauthorizedAccessException("Current Windows user SID is unavailable for Author session secret ACL.");
        var security = new FileSecurity();
        security.SetOwner(owner);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }
}

internal class AuthorSessionOffer
{
    public int SchemaVersion { get; set; } = AuthorSessionContract.SchemaVersion;
    public int ProtocolMajor { get; set; } = AuthorSessionContract.ProtocolMajor;
    public int MinimumMinor { get; set; } = AuthorSessionContract.MinimumMinor;
    public int MaximumMinor { get; set; } = AuthorSessionContract.MaximumMinor;
    public string ApiTarget { get; set; } = AuthorSdkContract.TargetRuntimeVersion;
    public string MinimumRuntimeVersion { get; set; } = AuthorSessionContract.MinimumRuntimeVersion;
    public string[] RequiredCapabilities { get; set; } = { AuthorSessionContract.SnapshotCapability };
    public string[] OptionalCapabilities { get; set; } = { AuthorSessionContract.ReloadCapability };
}

internal sealed class AuthorSessionSecret : AuthorSessionOffer
{
    public string GameRoot { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? RuntimeVersion { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string PipeName { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string ExpiresAtUtc { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public AuthorSessionNegotiation? Negotiated { get; set; }
}

internal sealed class AuthorSessionNegotiation
{
    public string HostVersion { get; set; } = string.Empty;
    public int ProtocolMinor { get; set; }
    public string[] AcceptedCapabilities { get; set; } = Array.Empty<string>();
    public string[] UnsupportedOptionalCapabilities { get; set; } = Array.Empty<string>();
}

internal sealed class AuthorSessionRequestFrame : AuthorSessionOffer
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CommandLine { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Protocol { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Runtime { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? HostVersion { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? ProtocolMinor { get; set; }
    public string GameRoot { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string SelectedRoot { get; set; } = string.Empty;
    public string ExpectedTreeSha256 { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
}

internal sealed class AuthorSessionResponseFrame
{
    public string Protocol { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<AuthorSessionResponseValueFrame> Values { get; set; } = new();
    public int SchemaVersion { get; set; }
    public int ProtocolMajor { get; set; }
    public int? ProtocolMinor { get; set; }
    public string? GameRoot { get; set; }
    public string? HostVersion { get; set; }
    public string? ApiTarget { get; set; }
    public string[]? AcceptedCapabilities { get; set; }
    public string[]? UnsupportedOptionalCapabilities { get; set; }
}

internal sealed class AuthorSessionResponseValueFrame
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

internal sealed class SessionCommandException : Exception
{
    public SessionCommandException(string code, string message, string path, Exception? innerException = null) : base(message, innerException)
    {
        Code = code;
        Path = path;
    }

    public string Code { get; }
    public string Path { get; }
}
