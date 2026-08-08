using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace DTMAPI.InstallDoctor;

internal sealed class AdvancedReferencePolicyAuthority
{
    private const string RegistryResourceName = "DTMAPI.InstallDoctor.AdvancedReferencePolicyRegistry.json";
    private const string HistoricalRegistryResourceName = "DTMAPI.InstallDoctor.HistoricalAdvancedReferencePolicyRegistry.json";
    private const string PolicyResourcePrefix = "DTMAPI.InstallDoctor.AdvancedReferencePolicy.";
    private const string HistoricalPolicyResourcePrefix = "DTMAPI.InstallDoctor.HistoricalAdvancedReferencePolicy.";
    private const string PolicyResourceSuffix = ".json";

    private IReadOnlyDictionary<string, RegisteredPolicy> Policies { get; init; } =
        new Dictionary<string, RegisteredPolicy>(StringComparer.Ordinal);

    public IReadOnlyList<AdvancedReferenceReceiptRow> References { get; init; } = Array.Empty<AdvancedReferenceReceiptRow>();
    public string Error { get; init; } = string.Empty;

    public static AdvancedReferencePolicyAuthority Load()
    {
        try
        {
            Assembly assembly = typeof(AdvancedReferencePolicyAuthority).Assembly;
            var policies = new Dictionary<string, RegisteredPolicy>(StringComparer.Ordinal);
            IReadOnlyList<PolicyRegistration> current = LoadRegistry(
                assembly,
                RegistryResourceName,
                PolicyResourcePrefix,
                policies);
            IReadOnlyList<PolicyRegistration> historical = LoadRegistry(
                assembly,
                HistoricalRegistryResourceName,
                HistoricalPolicyResourcePrefix,
                policies);
            var currentUniqueIds = new HashSet<string>(current.Select(value => value.RequiredUniqueId), StringComparer.Ordinal);
            if (historical.Any(value => !currentUniqueIds.Contains(value.RequiredUniqueId)))
                throw new InvalidDataException("historical Runtime policy acceptance may only retain a current admitted product UniqueID");

            return new AdvancedReferencePolicyAuthority
            {
                Policies = policies,
                References = policies.Values
                    .SelectMany(value => value.References)
                    .GroupBy(value => value.Sha256, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray()
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new AdvancedReferencePolicyAuthority
            {
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static IReadOnlyList<PolicyRegistration> LoadRegistry(
        Assembly assembly,
        string registryResourceName,
        string policyResourcePrefix,
        IDictionary<string, RegisteredPolicy> policies)
    {
        using Stream registryStream = assembly.GetManifestResourceStream(registryResourceName)
            ?? throw new InvalidDataException("embedded Advanced reference policy registry is unavailable: " + registryResourceName);
        using var registryMemory = new MemoryStream();
        registryStream.CopyTo(registryMemory);
        using JsonDocument registryDocument = JsonDocument.Parse(registryMemory.ToArray(), new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        JsonElement registryRoot = registryDocument.RootElement;
        RequireExactProperties(registryRoot, "policy registry root", "schemaVersion", "policies");
        if (RequireInt32(registryRoot, "schemaVersion") != 2)
            throw new InvalidDataException("unsupported policy registry schemaVersion");
        JsonElement registrationArray = registryRoot.GetProperty("policies");
        if (registrationArray.ValueKind != JsonValueKind.Array || registrationArray.GetArrayLength() == 0)
            throw new InvalidDataException("policy registry must contain at least one tracked policy row");

        var registrations = new List<PolicyRegistration>();
        foreach (JsonElement row in registrationArray.EnumerateArray())
        {
            RequireExactProperties(
                row,
                "policy registry row",
                "policyId",
                "policySha256",
                "requiredUniqueId",
                "minimumDtmApiVersion",
                "compilerSurfaceSha256");
            string minimumDtmApiVersion = RequireString(row, "minimumDtmApiVersion");
            if (!IsExactNumericVersion(minimumDtmApiVersion))
                throw new InvalidDataException("minimumDtmApiVersion must be an exact numeric major.minor.patch version");
            registrations.Add(new PolicyRegistration(
                RequireString(row, "policyId"),
                RequireSha256(row, "policySha256"),
                RequireString(row, "requiredUniqueId"),
                minimumDtmApiVersion,
                RequireSha256(row, "compilerSurfaceSha256")));
        }

        string[] policyIds = registrations.Select(value => value.PolicyId).ToArray();
        if (!policyIds.SequenceEqual(policyIds.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) ||
            policyIds.Distinct(StringComparer.Ordinal).Count() != policyIds.Length ||
            registrations.Select(value => value.RequiredUniqueId).Distinct(StringComparer.Ordinal).Count() != registrations.Count)
            throw new InvalidDataException("policy registry rows must have unique canonical policy IDs and UniqueID bindings");

        foreach (PolicyRegistration registration in registrations)
        {
            string resourceName = policyResourcePrefix + registration.PolicyId + PolicyResourceSuffix;
            using Stream policyStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidDataException("embedded Advanced reference policy is unavailable: " + registration.PolicyId);
            using var policyMemory = new MemoryStream();
            policyStream.CopyTo(policyMemory);
            byte[] bytes = policyMemory.ToArray();
            string policySha256 = Convert.ToHexString(SHA256.HashData(bytes));
            if (!policySha256.Equals(registration.PolicySha256, StringComparison.Ordinal))
                throw new InvalidDataException("embedded Advanced reference policy hash does not match its registry row: " + registration.PolicyId);
            RegisteredPolicy policy = ParsePolicy(bytes, policySha256, registration);
            policies.Add(registration.PolicyId, policy);
        }
        return registrations;
    }

    public bool Matches(AdvancedReferenceReceiptProbe receipt, string minimumDtmApiVersion, out string mismatch)
    {
        mismatch = string.Empty;
        if (Error.Length > 0)
        {
            mismatch = "tracked policy registry authority is invalid: " + Error;
            return false;
        }
        if (!Policies.TryGetValue(receipt.ReferencePolicyId, out RegisteredPolicy? policy))
        {
            mismatch = "referencePolicyId is absent from the Doctor-embedded exact registry";
            return false;
        }
        if (!receipt.UniqueId.Equals(policy.Registration.RequiredUniqueId, StringComparison.Ordinal))
        {
            mismatch = "receipt UniqueID does not match the policy registry binding";
            return false;
        }
        if (!IsVersionAtLeast(minimumDtmApiVersion, policy.Registration.MinimumDtmApiVersion))
        {
            mismatch = "manifest MinimumDTMApiVersion is lower than the Runtime floor registered for this exact policy";
            return false;
        }
        if (receipt.ReferencePolicyVersion != policy.PolicyVersion ||
            !receipt.ReferencePolicySha256.Equals(policy.PolicySha256, StringComparison.OrdinalIgnoreCase))
        {
            mismatch = "reference policy identity/hash does not match the Doctor-embedded tracked policy";
            return false;
        }
        if (!receipt.GameBuildId.Equals(policy.GameBuildId, StringComparison.Ordinal) ||
            !receipt.GameAssemblyRelativePath.Equals(policy.GameAssemblyRelativePath, StringComparison.Ordinal) ||
            !receipt.GameAssemblySha256.Equals(policy.GameAssemblySha256, StringComparison.OrdinalIgnoreCase))
        {
            mismatch = "game build/assembly identity does not match the tracked policy";
            return false;
        }
        if (receipt.References.Count != policy.References.Count)
        {
            mismatch = "reference count does not match the tracked policy";
            return false;
        }
        for (int index = 0; index < policy.References.Count; index++)
        {
            AdvancedReferenceReceiptRow expected = policy.References[index];
            AdvancedReferenceReceiptRow actual = receipt.References[index];
            if (!actual.GameRelativePath.Equals(expected.GameRelativePath, StringComparison.Ordinal) ||
                !actual.AssemblyName.Equals(expected.AssemblyName, StringComparison.Ordinal) ||
                actual.Length != expected.Length ||
                !actual.Sha256.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase) ||
                actual.CopyLocal != expected.CopyLocal)
            {
                mismatch = "reference row does not match the tracked policy: " + actual.GameRelativePath;
                return false;
            }
        }
        return true;
    }

    private static RegisteredPolicy ParsePolicy(byte[] bytes, string policySha256, PolicyRegistration registration)
    {
        using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        JsonElement root = document.RootElement;
        RequireExactProperties(
            root,
            "policy root",
            "schemaVersion",
            "policyId",
            "policyVersion",
            "gameBuildId",
            "gameAssemblyRelativePath",
            "gameAssemblySha256",
            "references");
        if (RequireInt32(root, "schemaVersion") != 1)
            throw new InvalidDataException("unsupported policy schemaVersion");

        var references = new List<AdvancedReferenceReceiptRow>();
        JsonElement referenceArray = root.GetProperty("references");
        if (referenceArray.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("policy references must be an array");
        foreach (JsonElement row in referenceArray.EnumerateArray())
        {
            RequireExactProperties(row, "policy reference row", "gameRelativePath", "assemblyName", "length", "sha256", "copyLocal");
            if (row.GetProperty("copyLocal").ValueKind is not JsonValueKind.False)
                throw new InvalidDataException("policy reference copyLocal must be exactly false");
            references.Add(new AdvancedReferenceReceiptRow
            {
                GameRelativePath = RequireSafeRelativeDllPath(row, "gameRelativePath"),
                AssemblyName = RequireString(row, "assemblyName"),
                Length = RequirePositiveInt64(row, "length"),
                Sha256 = RequireSha256(row, "sha256"),
                CopyLocal = false
            });
        }
        string[] paths = references.Select(value => value.GameRelativePath).ToArray();
        if (references.Count == 0 ||
            !paths.SequenceEqual(paths.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) ||
            references.Select(value => value.GameRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != references.Count ||
            references.Select(value => value.AssemblyName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != references.Count)
            throw new InvalidDataException("policy references must be non-empty, canonical, and unique by path and assemblyName");

        string policyId = RequireString(root, "policyId");
        int policyVersion = RequireInt32(root, "policyVersion");
        string gameBuildId = RequireString(root, "gameBuildId");
        string gameAssemblyRelativePath = RequireSafeRelativeDllPath(root, "gameAssemblyRelativePath");
        string gameAssemblySha256 = RequireSha256(root, "gameAssemblySha256");
        AdvancedReferenceReceiptRow? gameAssembly = references.SingleOrDefault(reference =>
            reference.AssemblyName.Equals("Assembly-CSharp", StringComparison.Ordinal));
        if (!policyId.Equals(registration.PolicyId, StringComparison.Ordinal) ||
            policyVersion <= 0 ||
            gameBuildId.Any(character => character < '0' || character > '9') ||
            gameAssembly == null ||
            !gameAssemblyRelativePath.Equals(gameAssembly.GameRelativePath, StringComparison.Ordinal) ||
            !gameAssemblySha256.Equals(gameAssembly.Sha256, StringComparison.Ordinal))
            throw new InvalidDataException("embedded Advanced reference policy identity/build drifted from its registry authority");

        return new RegisteredPolicy(
            registration,
            policyVersion,
            policySha256,
            gameBuildId,
            gameAssemblyRelativePath,
            gameAssemblySha256,
            references.ToArray());
    }

    private static void RequireExactProperties(JsonElement value, string context, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(context + " must be a JSON object");
        var allowed = new HashSet<string>(expected, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name) || !seen.Add(property.Name))
                throw new InvalidDataException(context + " contains unknown or duplicate field " + property.Name);
        }
        string[] missing = expected.Where(name => !seen.Contains(name)).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException(context + " is missing field(s): " + string.Join(", ", missing));
    }

    private static string RequireString(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException(name + " must be a non-empty string");
        return value.GetString()!;
    }

    private static bool IsExactNumericVersion(string value)
    {
        string[] parts = (value ?? string.Empty).Split('.');
        return parts.Length == 3 &&
            parts.All(part => part.Length > 0 && part.All(character => character >= '0' && character <= '9')) &&
            Version.TryParse(value, out _);
    }

    private static bool IsVersionAtLeast(string actual, string minimum)
    {
        return IsExactNumericVersion(actual) &&
            IsExactNumericVersion(minimum) &&
            Version.Parse(actual).CompareTo(Version.Parse(minimum)) >= 0;
    }

    private static int RequireInt32(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result) || result <= 0)
            throw new InvalidDataException(name + " must be a positive integer");
        return result;
    }

    private static long RequirePositiveInt64(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long result) || result <= 0)
            throw new InvalidDataException(name + " must be a positive integer");
        return result;
    }

    private static string RequireSha256(JsonElement root, string name)
    {
        string value = RequireString(root, name);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException(name + " must be a 64-character hexadecimal SHA-256");
        return value.ToUpperInvariant();
    }

    private static string RequireSafeRelativeDllPath(JsonElement root, string name)
    {
        string value = RequireString(root, name);
        if (value.Contains('\\') || Path.IsPathRooted(value) || value.StartsWith("/", StringComparison.Ordinal) ||
            value.Split('/').Any(segment => segment is "" or "." or "..") ||
            value.Split('/').Any(segment => segment.Contains(':')) ||
            !value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(name + " must be one normalized safe relative DLL path");
        return value;
    }

    private sealed record PolicyRegistration(
        string PolicyId,
        string PolicySha256,
        string RequiredUniqueId,
        string MinimumDtmApiVersion,
        string CompilerSurfaceSha256);

    private sealed record RegisteredPolicy(
        PolicyRegistration Registration,
        int PolicyVersion,
        string PolicySha256,
        string GameBuildId,
        string GameAssemblyRelativePath,
        string GameAssemblySha256,
        IReadOnlyList<AdvancedReferenceReceiptRow> References);
}
