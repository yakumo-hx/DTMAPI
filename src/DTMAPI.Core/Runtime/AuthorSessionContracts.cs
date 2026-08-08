using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DTMAPI.Core.Runtime
{
    internal static class AuthorSessionProtocol
    {
        public const int DescriptorSchemaVersion = 1;
        public const string ProtocolVersion = "dtmapi-author-session/1";
        public const string GetSourceSnapshotOperation = "get-source-snapshot";
        public const string ReloadContentOperation = "reload-content";
        public const int MaximumDescriptorBytes = 32 * 1024;
        public const int MaximumRequestBytes = 32 * 1024;
        public const int MaximumResponseBytes = 64 * 1024;
        public const int MaximumReplayEntries = 4096;
        public static readonly TimeSpan MaximumDescriptorLifetime = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(1);

        public static string CreatePipeName(string gameRoot, string sessionId)
        {
            Guid session = ParseSessionId(sessionId);
            return "dtmapi-author-" + AuthorSourceStateStore.ComputeGameRootKey(gameRoot) + "-" + session.ToString("N");
        }

        public static bool IsSupportedOperation(string operation)
        {
            return string.Equals(operation, GetSourceSnapshotOperation, StringComparison.Ordinal) ||
                string.Equals(operation, ReloadContentOperation, StringComparison.Ordinal);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be empty.", nameof(path));
            return Path.GetFullPath(path ?? string.Empty)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool PathsEqual(string left, string right)
        {
            try
            {
                return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static Guid ParseSessionId(string sessionId)
        {
            if (!Guid.TryParseExact(sessionId ?? string.Empty, "N", out Guid parsed) || parsed == Guid.Empty)
                throw new InvalidDataException("sessionId must be a non-empty GUID in N format");
            return parsed;
        }

        public static bool IsValidRequestId(string requestId)
        {
            return Guid.TryParseExact(requestId ?? string.Empty, "N", out Guid parsed) && parsed != Guid.Empty;
        }

        public static bool IsValidToken(string token)
        {
            string value = token ?? string.Empty;
            if (value.Length < 43 || value.Length > 128)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character >= 'a' && character <= 'z') ||
                    (character >= 'A' && character <= 'Z') ||
                    (character >= '0' && character <= '9') ||
                    character == '-' || character == '_')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool FixedTimeEquals(string left, string right)
        {
            string first = left ?? string.Empty;
            string second = right ?? string.Empty;
            int maximum = Math.Max(first.Length, second.Length);
            int difference = first.Length ^ second.Length;
            for (int index = 0; index < maximum; index++)
            {
                char firstCharacter = index < first.Length ? first[index] : '\0';
                char secondCharacter = index < second.Length ? second[index] : '\0';
                difference |= firstCharacter ^ secondCharacter;
            }

            return difference == 0;
        }

        public static bool TryParseUtc(string value, out DateTimeOffset parsed)
        {
            if (!DateTimeOffset.TryParseExact(
                value ?? string.Empty,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
            {
                return false;
            }

            return parsed.Offset == TimeSpan.Zero;
        }

        public static string BoundedSingleLine(string value, int maximumLength)
        {
            string result = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return result.Length <= maximumLength ? result : result.Substring(0, maximumLength);
        }

        public static bool IsSafeIdentifier(string value, int maximumLength)
        {
            string text = value ?? string.Empty;
            if (text.Length == 0 || text.Length > maximumLength || !string.Equals(text, text.Trim(), StringComparison.Ordinal))
                return false;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (char.IsControl(character) || character == '/' || character == '\\')
                    return false;
            }

            return true;
        }

        public static bool IsSha256(string value)
        {
            string text = value ?? string.Empty;
            if (text.Length != 64)
                return false;
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                bool hexadecimal = (character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f') ||
                    (character >= 'A' && character <= 'F');
                if (!hexadecimal)
                    return false;
            }

            return true;
        }
    }

    [DataContract]
    internal sealed class AuthorSessionDescriptor
    {
        [DataMember(Name = "schemaVersion", Order = 0, IsRequired = true)]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "gameRoot", Order = 1, IsRequired = true)]
        public string GameRoot { get; set; } = string.Empty;

        [DataMember(Name = "runtimeVersion", Order = 2, IsRequired = true)]
        public string RuntimeVersion { get; set; } = string.Empty;

        [DataMember(Name = "sessionId", Order = 3, IsRequired = true)]
        public string SessionId { get; set; } = string.Empty;

        [DataMember(Name = "token", Order = 4, IsRequired = true)]
        public string Token { get; set; } = string.Empty;

        [DataMember(Name = "pipeName", Order = 5, IsRequired = true)]
        public string PipeName { get; set; } = string.Empty;

        [DataMember(Name = "createdAtUtc", Order = 6, IsRequired = true)]
        public string CreatedAtUtc { get; set; } = string.Empty;

        [DataMember(Name = "expiresAtUtc", Order = 7, IsRequired = true)]
        public string ExpiresAtUtc { get; set; } = string.Empty;

        [IgnoreDataMember]
        public DateTimeOffset CreatedAt { get; internal set; }

        [IgnoreDataMember]
        public DateTimeOffset ExpiresAt { get; internal set; }
    }

    [DataContract]
    internal sealed class AuthorSessionRequest
    {
        [DataMember(Name = "protocol", Order = 0, IsRequired = true)]
        public string Protocol { get; set; } = string.Empty;

        [DataMember(Name = "runtime", Order = 1, IsRequired = true)]
        public string Runtime { get; set; } = string.Empty;

        [DataMember(Name = "gameRoot", Order = 2, IsRequired = true)]
        public string GameRoot { get; set; } = string.Empty;

        [DataMember(Name = "session", Order = 3, IsRequired = true)]
        public string Session { get; set; } = string.Empty;

        [DataMember(Name = "token", Order = 4, IsRequired = true)]
        public string Token { get; set; } = string.Empty;

        [DataMember(Name = "requestId", Order = 5, IsRequired = true)]
        public string RequestId { get; set; } = string.Empty;

        [DataMember(Name = "uniqueId", Order = 6, IsRequired = true)]
        public string UniqueId { get; set; } = string.Empty;

        [DataMember(Name = "selectedRoot", Order = 7, IsRequired = true)]
        public string SelectedRoot { get; set; } = string.Empty;

        [DataMember(Name = "expectedTreeSha256", Order = 8, IsRequired = true)]
        public string ExpectedTreeSha256 { get; set; } = string.Empty;

        [DataMember(Name = "operation", Order = 9, IsRequired = true)]
        public string Operation { get; set; } = string.Empty;

        internal void NormalizeAuthenticatedValues()
        {
            GameRoot = AuthorSessionProtocol.NormalizePath(GameRoot);
            SelectedRoot = AuthorSessionProtocol.NormalizePath(SelectedRoot);
            ExpectedTreeSha256 = ExpectedTreeSha256.ToUpperInvariant();
        }
    }

    [DataContract]
    internal sealed class AuthorSessionResponse
    {
        [DataMember(Name = "protocol", Order = 0)]
        public string Protocol { get; set; } = AuthorSessionProtocol.ProtocolVersion;

        [DataMember(Name = "runtime", Order = 1)]
        public string Runtime { get; set; } = string.Empty;

        [DataMember(Name = "session", Order = 2)]
        public string Session { get; set; } = string.Empty;

        [DataMember(Name = "requestId", Order = 3)]
        public string RequestId { get; set; } = string.Empty;

        [DataMember(Name = "operation", Order = 4)]
        public string Operation { get; set; } = string.Empty;

        [DataMember(Name = "uniqueId", Order = 5)]
        public string UniqueId { get; set; } = string.Empty;

        [DataMember(Name = "status", Order = 6)]
        public string Status { get; set; } = string.Empty;

        [DataMember(Name = "code", Order = 7)]
        public string Code { get; set; } = string.Empty;

        [DataMember(Name = "message", Order = 8)]
        public string Message { get; set; } = string.Empty;

        [DataMember(Name = "values", Order = 9)]
        public List<AuthorSessionResponseValue> Values { get; set; } = new List<AuthorSessionResponseValue>();
    }

    [DataContract]
    internal sealed class AuthorSessionResponseValue
    {
        public AuthorSessionResponseValue()
        {
        }

        public AuthorSessionResponseValue(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }

        [DataMember(Name = "key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        [DataMember(Name = "value", Order = 1)]
        public string Value { get; set; } = string.Empty;
    }

    internal sealed class AuthorSessionOperationResult
    {
        private static readonly string[] AllowedStatuses = { "ok", "rejected", "restart-required", "error" };
        private readonly IReadOnlyList<AuthorSessionResponseValue> values;

        private AuthorSessionOperationResult(string status, string code, string message, IEnumerable<KeyValuePair<string, string>> values)
        {
            string normalizedStatus = status ?? string.Empty;
            Status = AllowedStatuses.Contains(normalizedStatus, StringComparer.Ordinal) ? normalizedStatus : "error";
            Code = AuthorSessionProtocol.BoundedSingleLine(code, 96);
            Message = AuthorSessionProtocol.BoundedSingleLine(message, 512);
            this.values = (values ?? Array.Empty<KeyValuePair<string, string>>())
                .Where(pair => AuthorSessionProtocol.IsSafeIdentifier(pair.Key, 64))
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(group => group.Last())
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(32)
                .Select(pair => new AuthorSessionResponseValue(pair.Key, AuthorSessionProtocol.BoundedSingleLine(pair.Value, 2048)))
                .ToArray();
        }

        public string Status { get; }

        public string Code { get; }

        public string Message { get; }

        public IReadOnlyList<AuthorSessionResponseValue> Values => values;

        public static AuthorSessionOperationResult Success(string code, string message, params KeyValuePair<string, string>[] values) =>
            new AuthorSessionOperationResult("ok", code, message, values);

        public static AuthorSessionOperationResult Rejected(string code, string message, params KeyValuePair<string, string>[] values) =>
            new AuthorSessionOperationResult("rejected", code, message, values);

        public static AuthorSessionOperationResult RestartRequired(string code, string message, params KeyValuePair<string, string>[] values) =>
            new AuthorSessionOperationResult("restart-required", code, message, values);

        public static AuthorSessionOperationResult Error(string code, string message, params KeyValuePair<string, string>[] values) =>
            new AuthorSessionOperationResult("error", code, message, values);
    }

    internal sealed class AuthorSessionValidationResult
    {
        public AuthorSessionValidationResult(bool accepted, string code, string message)
        {
            Accepted = accepted;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Accepted { get; }

        public string Code { get; }

        public string Message { get; }

        public static AuthorSessionValidationResult Accept(string code, string message) => new AuthorSessionValidationResult(true, code, message);

        public static AuthorSessionValidationResult Reject(string code, string message) => new AuthorSessionValidationResult(false, code, message);
    }
}
