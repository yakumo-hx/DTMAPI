using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DTMAPI.Core.Runtime
{
    internal static class AuthorSessionDescriptorStore
    {
        private static readonly object AttemptGate = new object();
        private static readonly HashSet<string> AttemptedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static AuthorSessionDescriptorLoadResult ConsumeStartup(string gameRoot, string runtimeVersion)
        {
            return ConsumeStartup(gameRoot, runtimeVersion, () => DateTimeOffset.UtcNow);
        }

        internal static AuthorSessionDescriptorLoadResult ConsumeStartup(string gameRoot, string runtimeVersion, Func<DateTimeOffset> utcNow)
        {
            string canonicalRoot;
            string descriptorPath;
            try
            {
                canonicalRoot = AuthorSessionProtocol.NormalizePath(gameRoot);
                descriptorPath = AuthorSourceStateStore.GetAuthorSessionPath(canonicalRoot);
            }
            catch (Exception ex)
            {
                return AuthorSessionDescriptorLoadResult.Rejected(
                    string.Empty,
                    "invalid-runtime-root",
                    "Runtime game root is invalid: " + ex.GetType().Name + ".");
            }

            string attemptKey;
            try
            {
                attemptKey = AuthorSessionProtocol.NormalizePath(descriptorPath);
            }
            catch
            {
                attemptKey = descriptorPath;
            }

            lock (AttemptGate)
            {
                if (!AttemptedPaths.Add(attemptKey))
                {
                    return AuthorSessionDescriptorLoadResult.Rejected(
                        descriptorPath,
                        "descriptor-already-checked",
                        "The startup descriptor path was already checked in this process.");
                }
            }

            if (!File.Exists(descriptorPath))
                return AuthorSessionDescriptorLoadResult.NotPresent(descriptorPath);

            string consumedPath = descriptorPath + ".consuming-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Move(descriptorPath, consumedPath);
            }
            catch (Exception ex)
            {
                return AuthorSessionDescriptorLoadResult.Rejected(
                    descriptorPath,
                    "descriptor-consume-failed",
                    "The startup descriptor could not be consumed atomically: " + ex.GetType().Name + ".");
            }

            try
            {
                AuthorSessionDescriptor descriptor;
                using (var stream = new FileStream(consumedPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (stream.Length <= 0 || stream.Length > AuthorSessionProtocol.MaximumDescriptorBytes)
                    {
                        return AuthorSessionDescriptorLoadResult.Rejected(
                            descriptorPath,
                            "descriptor-size-invalid",
                            "The startup descriptor must be non-empty and no larger than " +
                                AuthorSessionProtocol.MaximumDescriptorBytes.ToString(CultureInfo.InvariantCulture) + " bytes.");
                    }

                    var serializer = new DataContractJsonSerializer(typeof(AuthorSessionDescriptor));
                    descriptor = serializer.ReadObject(stream) as AuthorSessionDescriptor ?? new AuthorSessionDescriptor();
                }

                AuthorSessionValidationResult validation = ValidateDescriptor(descriptor, canonicalRoot, runtimeVersion, utcNow());
                return validation.Accepted
                    ? AuthorSessionDescriptorLoadResult.FromAccepted(descriptorPath, descriptor)
                    : AuthorSessionDescriptorLoadResult.Rejected(descriptorPath, validation.Code, validation.Message);
            }
            catch (Exception ex)
            {
                return AuthorSessionDescriptorLoadResult.Rejected(
                    descriptorPath,
                    "descriptor-json-invalid",
                    "The startup descriptor is not valid schemaVersion 1 JSON: " + ex.GetType().Name + ".");
            }
            finally
            {
                try
                {
                    if (File.Exists(consumedPath))
                        File.Delete(consumedPath);
                }
                catch
                {
                    // The original descriptor name was already consumed atomically. A failed
                    // best-effort cleanup must not make the descriptor reusable or trigger polling.
                }
            }
        }

        internal static AuthorSessionValidationResult ValidateDescriptor(
            AuthorSessionDescriptor descriptor,
            string expectedGameRoot,
            string expectedRuntimeVersion,
            DateTimeOffset utcNow)
        {
            if (descriptor == null)
                return AuthorSessionValidationResult.Reject("descriptor-null", "The startup descriptor is empty.");
            if (descriptor.SchemaVersion != AuthorSessionProtocol.DescriptorSchemaVersion)
            {
                return AuthorSessionValidationResult.Reject(
                    "descriptor-schema-unsupported",
                    "The startup descriptor schemaVersion is unsupported.");
            }

            if (!AuthorSessionProtocol.PathsEqual(descriptor.GameRoot, expectedGameRoot))
                return AuthorSessionValidationResult.Reject("descriptor-root-mismatch", "The descriptor gameRoot does not match this Runtime installation.");
            if (string.IsNullOrWhiteSpace(expectedRuntimeVersion) ||
                !string.Equals(descriptor.RuntimeVersion, expectedRuntimeVersion, StringComparison.Ordinal))
            {
                return AuthorSessionValidationResult.Reject("descriptor-runtime-mismatch", "The descriptor runtimeVersion does not match the running Runtime.");
            }

            Guid sessionId;
            try
            {
                sessionId = AuthorSessionProtocol.ParseSessionId(descriptor.SessionId);
            }
            catch
            {
                return AuthorSessionValidationResult.Reject("descriptor-session-invalid", "The descriptor sessionId is invalid.");
            }

            if (!AuthorSessionProtocol.IsValidToken(descriptor.Token))
                return AuthorSessionValidationResult.Reject("descriptor-token-invalid", "The descriptor token format is invalid.");

            string expectedPipeName;
            try
            {
                expectedPipeName = AuthorSessionProtocol.CreatePipeName(expectedGameRoot, sessionId.ToString("N"));
            }
            catch
            {
                return AuthorSessionValidationResult.Reject("descriptor-pipe-invalid", "The descriptor pipeName could not be derived.");
            }

            if (!string.Equals(descriptor.PipeName, expectedPipeName, StringComparison.Ordinal))
            {
                return AuthorSessionValidationResult.Reject(
                    "descriptor-pipe-mismatch",
                    "The descriptor pipeName is not bound to this game root and session.");
            }

            if (!AuthorSessionProtocol.TryParseUtc(descriptor.CreatedAtUtc, out DateTimeOffset createdAt) ||
                !AuthorSessionProtocol.TryParseUtc(descriptor.ExpiresAtUtc, out DateTimeOffset expiresAt))
            {
                return AuthorSessionValidationResult.Reject(
                    "descriptor-time-invalid",
                    "The descriptor timestamps must be round-trip UTC timestamps.");
            }

            if (createdAt > utcNow + AuthorSessionProtocol.MaximumFutureClockSkew)
                return AuthorSessionValidationResult.Reject("descriptor-not-yet-valid", "The descriptor creation time is in the future.");
            if (expiresAt <= createdAt || expiresAt - createdAt > AuthorSessionProtocol.MaximumDescriptorLifetime)
            {
                return AuthorSessionValidationResult.Reject(
                    "descriptor-lifetime-invalid",
                    "The descriptor lifetime is invalid or exceeds the short-lived session limit.");
            }
            if (expiresAt <= utcNow)
                return AuthorSessionValidationResult.Reject("descriptor-expired", "The descriptor has expired.");

            descriptor.GameRoot = AuthorSessionProtocol.NormalizePath(descriptor.GameRoot);
            descriptor.SessionId = sessionId.ToString("N");
            descriptor.CreatedAt = createdAt;
            descriptor.ExpiresAt = expiresAt;
            return AuthorSessionValidationResult.Accept("descriptor-accepted", "The startup descriptor was consumed and validated.");
        }
    }

    internal sealed class AuthorSessionDescriptorLoadResult
    {
        private AuthorSessionDescriptorLoadResult(
            bool present,
            bool accepted,
            string descriptorPath,
            string code,
            string message,
            AuthorSessionDescriptor? descriptor)
        {
            Present = present;
            Accepted = accepted;
            DescriptorPath = descriptorPath ?? string.Empty;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Descriptor = descriptor;
        }

        public bool Present { get; }

        public bool Accepted { get; }

        public string DescriptorPath { get; }

        public string Code { get; }

        public string Message { get; }

        public AuthorSessionDescriptor? Descriptor { get; }

        public static AuthorSessionDescriptorLoadResult NotPresent(string path) =>
            new AuthorSessionDescriptorLoadResult(false, false, path, "descriptor-not-present", "No startup author-session descriptor is present.", null);

        public static AuthorSessionDescriptorLoadResult FromAccepted(string path, AuthorSessionDescriptor descriptor) =>
            new AuthorSessionDescriptorLoadResult(true, true, path, "descriptor-accepted", "The startup descriptor was consumed and validated.", descriptor);

        public static AuthorSessionDescriptorLoadResult Rejected(string path, string code, string message) =>
            new AuthorSessionDescriptorLoadResult(true, false, path, code, message, null);
    }
}
