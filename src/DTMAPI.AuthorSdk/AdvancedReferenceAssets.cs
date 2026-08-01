using DTMAPI.Authoring.Contracts;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

internal sealed class ResolvedAdvancedReferenceSet
{
    public required string GameRoot { get; init; }
    public required AdvancedReferencePolicy Policy { get; init; }
    public required AdvancedReferencePolicyRegistration Registration { get; init; }
    public required string PolicySha256 { get; init; }
    public required IReadOnlyList<string> ReferencePaths { get; init; }
}

internal static class AdvancedReferenceAssets
{
    private const string RegistryResourceName = "DTMAPI.AuthorSdk.advanced-reference-policy-registry.json";
    private const string PolicyResourcePrefix = "DTMAPI.AuthorSdk.advanced-reference-policy.";
    private const string PolicyResourceSuffix = ".json";
    private const string SteamAppId = "2285550";
    private static readonly string[] RegistryProperties = { "schemaVersion", "policies" };
    private static readonly string[] RegistryPolicyProperties =
    {
        "policyId", "policySha256", "requiredUniqueId", "compilerSurfaceSha256"
    };
    private static readonly string[] ReceiptProperties =
    {
        "schemaVersion", "receiptKind", "referencePolicyId", "referencePolicyVersion", "referencePolicySha256",
        "uniqueId", "codeModKind", "targetFramework", "gameBuildId", "gameAssemblyRelativePath",
        "gameAssemblySha256", "manifestPath", "manifestSha256", "entryDllPath", "entryDllLength",
        "entryDllSha256", "harmonyOwner", "references"
    };
    private static readonly string[] ReceiptReferenceProperties =
    {
        "gameRelativePath", "assemblyName", "length", "sha256", "copyLocal"
    };
    private static readonly Regex Sha256Pattern = new("^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ResolvedAdvancedReferenceSet Resolve(AuthorProjectContext context, string requestedGameRoot)
    {
        if (context.CodeModKind != AuthorCodeModKind.Advanced || context.AuthorProject.Advanced == null)
            throw new InvalidDataException("Advanced native references can be resolved only for an explicit Advanced CodeMod project.");

        AdvancedAuthorProject advanced = context.AuthorProject.Advanced;
        AdvancedReferencePolicyRegistration registration = ResolveRegistration(advanced.ReferencePolicyId);
        RequireRegisteredUniqueId(registration, context.Manifest.UniqueID);
        (AdvancedReferencePolicy policy, string policySha256) = LoadTrackedPolicy(registration.PolicyId);
        string[] requested = advanced.References.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] allowed = policy.References.Select(reference => reference.AssemblyName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!requested.SequenceEqual(allowed, StringComparer.Ordinal))
            throw new InvalidDataException("Advanced references must exactly match tracked policy " + policy.PolicyId + ": " + string.Join(", ", allowed) + ".");
        return ResolveGameRoot(requestedGameRoot, registration, policy, policySha256);
    }

    public static ResolvedAdvancedReferenceSet ResolveForReceipt(string requestedGameRoot, AdvancedReferenceReceipt receipt)
    {
        AdvancedReferencePolicyRegistration registration = ResolveRegistration(receipt.ReferencePolicyId);
        RequireRegisteredUniqueId(registration, receipt.UniqueId);
        (AdvancedReferencePolicy policy, string policySha256) = LoadTrackedPolicy(registration.PolicyId);
        if (!receipt.ReferencePolicyId.Equals(policy.PolicyId, StringComparison.Ordinal)
            || receipt.ReferencePolicyVersion != policy.PolicyVersion
            || !receipt.ReferencePolicySha256.Equals(policySha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Advanced receipt reference-policy identity/hash does not match the SDK-tracked policy.");
        return ResolveGameRoot(requestedGameRoot, registration, policy, policySha256);
    }

    public static AdvancedReferenceReceipt CreateReceipt(
        AuthorProjectContext context,
        ResolvedAdvancedReferenceSet resolved,
        string manifestPath,
        ReadOnlySpan<byte> manifestBytes,
        string entryDllPath,
        ReadOnlySpan<byte> entryDllBytes)
    {
        if (context.CodeModKind != AuthorCodeModKind.Advanced)
            throw new InvalidDataException("Only Advanced CodeMods receive an Advanced reference receipt.");
        return new AdvancedReferenceReceipt
        {
            SchemaVersion = AuthorSdkContract.AdvancedReferenceReceiptSchemaVersion,
            ReceiptKind = AuthorSdkContract.AdvancedReferenceReceiptKind,
            ReferencePolicyId = resolved.Policy.PolicyId,
            ReferencePolicyVersion = resolved.Policy.PolicyVersion,
            ReferencePolicySha256 = resolved.PolicySha256,
            UniqueId = context.Manifest.UniqueID,
            CodeModKind = AuthorCodeModKind.Advanced.ToString(),
            TargetFramework = "netstandard2.0",
            GameBuildId = resolved.Policy.GameBuildId,
            GameAssemblyRelativePath = resolved.Policy.GameAssemblyRelativePath,
            GameAssemblySha256 = resolved.Policy.GameAssemblySha256.ToLowerInvariant(),
            ManifestPath = NormalizePackagePath(manifestPath, "manifestPath"),
            ManifestSha256 = PathSafety.Sha256Bytes(manifestBytes),
            EntryDllPath = NormalizePackagePath(entryDllPath, "entryDllPath"),
            EntryDllLength = entryDllBytes.Length,
            EntryDllSha256 = PathSafety.Sha256Bytes(entryDllBytes),
            HarmonyOwner = ExpectedHarmonyOwner(context.Manifest.UniqueID),
            References = resolved.Policy.References.Select(reference => new AdvancedReferenceReceiptEntry
            {
                GameRelativePath = reference.GameRelativePath,
                AssemblyName = reference.AssemblyName,
                Length = reference.Length,
                Sha256 = reference.Sha256.ToLowerInvariant(),
                CopyLocal = false
            }).ToList()
        };
    }

    public static AdvancedReferenceReceipt ReadReceipt(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        ValidateExactReceiptJson(text, path);
        return JsonSerializer.Deserialize<AdvancedReferenceReceipt>(text, JsonSupport.StrictToolContract)
            ?? throw new InvalidDataException("dtmapi-advanced-references.json must contain one object.");
    }

    public static void ValidateReceipt(
        AdvancedReferenceReceipt receipt,
        RuntimeManifest manifest,
        string manifestRelativePath,
        ReadOnlySpan<byte> manifestBytes,
        string entryDllRelativePath,
        ReadOnlySpan<byte> entryDllBytes,
        string gameRoot)
    {
        ResolvedAdvancedReferenceSet resolved = ResolveForReceipt(gameRoot, receipt);
        if (receipt.SchemaVersion != AuthorSdkContract.AdvancedReferenceReceiptSchemaVersion
            || !receipt.ReceiptKind.Equals(AuthorSdkContract.AdvancedReferenceReceiptKind, StringComparison.Ordinal)
            || !receipt.UniqueId.Equals(manifest.UniqueID, StringComparison.Ordinal)
            || !receipt.CodeModKind.Equals(AuthorCodeModKind.Advanced.ToString(), StringComparison.Ordinal)
            || !receipt.TargetFramework.Equals("netstandard2.0", StringComparison.Ordinal)
            || !receipt.GameBuildId.Equals(resolved.Policy.GameBuildId, StringComparison.Ordinal)
            || !receipt.GameAssemblyRelativePath.Equals(resolved.Policy.GameAssemblyRelativePath, StringComparison.Ordinal)
            || !receipt.GameAssemblySha256.Equals(resolved.Policy.GameAssemblySha256, StringComparison.OrdinalIgnoreCase)
            || !receipt.ManifestPath.Equals(NormalizePackagePath(manifestRelativePath, "manifestPath"), StringComparison.Ordinal)
            || !receipt.ManifestPath.Equals(AuthorSdkContract.PackageManifestPath, StringComparison.Ordinal)
            || !receipt.ManifestSha256.Equals(PathSafety.Sha256Bytes(manifestBytes), StringComparison.OrdinalIgnoreCase)
            || !receipt.EntryDllPath.Equals(NormalizePackagePath(entryDllRelativePath, "entryDllPath"), StringComparison.Ordinal)
            || !receipt.EntryDllPath.Equals(manifest.EntryDll.Replace('\\', '/'), StringComparison.Ordinal)
            || receipt.EntryDllLength != entryDllBytes.Length
            || !receipt.EntryDllSha256.Equals(PathSafety.Sha256Bytes(entryDllBytes), StringComparison.OrdinalIgnoreCase)
            || !receipt.HarmonyOwner.Equals(ExpectedHarmonyOwner(manifest.UniqueID), StringComparison.Ordinal))
            throw new InvalidDataException("Advanced receipt identity/build/manifest/entry/Harmony binding is invalid.");

        if (receipt.References.Count != resolved.Policy.References.Count)
            throw new InvalidDataException("Advanced receipt reference count does not match the tracked policy.");
        for (int index = 0; index < resolved.Policy.References.Count; index++)
        {
            AdvancedReferencePolicyEntry expected = resolved.Policy.References[index];
            AdvancedReferenceReceiptEntry actual = receipt.References[index];
            if (!actual.GameRelativePath.Equals(expected.GameRelativePath, StringComparison.Ordinal)
                || !actual.AssemblyName.Equals(expected.AssemblyName, StringComparison.Ordinal)
                || actual.Length != expected.Length
                || !actual.Sha256.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase)
                || actual.CopyLocal)
                throw new InvalidDataException("Advanced receipt references do not exactly match the tracked policy at index " + index + ".");
        }
    }

    public static string ExpectedHarmonyOwner(string uniqueId) => "dtmapi.mod." + uniqueId.ToLowerInvariant();

    public static (AdvancedReferencePolicy Policy, string Sha256) LoadTrackedPolicy(string policyId)
    {
        AdvancedReferencePolicyRegistration registration = ResolveRegistration(policyId);
        string resourceName = PolicyResourcePrefix + registration.PolicyId + PolicyResourceSuffix;
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException("The SDK-embedded Advanced reference policy is missing: " + registration.PolicyId + ".");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] bytes = memory.ToArray();
        string policySha256 = PathSafety.Sha256Bytes(bytes);
        if (!policySha256.Equals(registration.PolicySha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The SDK-embedded Advanced reference policy hash does not match its tracked registry entry: " + registration.PolicyId + ".");
        AdvancedReferencePolicy policy = JsonSerializer.Deserialize<AdvancedReferencePolicy>(bytes, JsonSupport.StrictToolContract)
            ?? throw new InvalidDataException("The SDK-embedded Advanced reference policy is invalid.");
        ValidatePolicy(policy, registration.PolicyId);
        return (policy, policySha256);
    }

    public static AdvancedReferencePolicyRegistration ResolveRegistration(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new InvalidDataException("Advanced referencePolicyId must name one exact SDK-tracked policy.");
        AdvancedReferencePolicyRegistry registry = LoadRegistry();
        AdvancedReferencePolicyRegistration? registration = registry.Policies.SingleOrDefault(value =>
            value.PolicyId.Equals(policyId, StringComparison.Ordinal));
        return registration ?? throw new InvalidDataException("Advanced referencePolicyId is not registered by this SDK: " + policyId + ".");
    }

    public static AdvancedReferencePolicyRegistration ResolveRegistrationForUniqueId(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            throw new InvalidDataException("Advanced CodeMod UniqueID must match one exact SDK-tracked policy binding.");
        AdvancedReferencePolicyRegistry registry = LoadRegistry();
        AdvancedReferencePolicyRegistration? registration = registry.Policies.SingleOrDefault(value =>
            value.RequiredUniqueId.Equals(uniqueId, StringComparison.Ordinal));
        return registration ?? throw new InvalidDataException(
            "Advanced CodeMod UniqueID is not admitted by this SDK policy registry: " + uniqueId + ".");
    }

    public static IReadOnlyList<(AdvancedReferencePolicy Policy, string Sha256)> LoadAllTrackedPolicies()
    {
        AdvancedReferencePolicyRegistry registry = LoadRegistry();
        return registry.Policies.Select(registration => LoadTrackedPolicy(registration.PolicyId)).ToArray();
    }

    public static void RequireRegisteredUniqueId(AdvancedReferencePolicyRegistration registration, string uniqueId)
    {
        if (!registration.RequiredUniqueId.Equals(uniqueId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Advanced reference policy " + registration.PolicyId + " is bound to UniqueID " +
                registration.RequiredUniqueId + "; received " + uniqueId + ".");
        }
    }

    private static ResolvedAdvancedReferenceSet ResolveGameRoot(
        string requestedGameRoot,
        AdvancedReferencePolicyRegistration registration,
        AdvancedReferencePolicy policy,
        string policySha256)
    {
        if (string.IsNullOrWhiteSpace(requestedGameRoot))
            throw new InvalidDataException("Advanced build/pack requires an explicit --game-root. Ambient game or assembly probing is forbidden.");
        string gameRoot = Path.GetFullPath(requestedGameRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(gameRoot) || !File.Exists(Path.Combine(gameRoot, "DolocTown.exe")))
            throw new InvalidDataException("Advanced --game-root must be an explicit Doloc Town root containing DolocTown.exe.");
        string buildId = ReadSteamBuildId(gameRoot);
        if (!buildId.Equals(policy.GameBuildId, StringComparison.Ordinal))
            throw new InvalidDataException("Doloc Town Steam build " + buildId + " does not match tracked Advanced reference policy build " + policy.GameBuildId + ".");

        var paths = new List<string>();
        foreach (AdvancedReferencePolicyEntry reference in policy.References)
        {
            string path = PathSafety.ResolveUnderRoot(gameRoot, reference.GameRelativePath, "Advanced game reference");
            if (!File.Exists(path))
                throw new InvalidDataException("Tracked Advanced game reference is missing: " + reference.GameRelativePath);
            var info = new FileInfo(path);
            if (info.Length != reference.Length || !PathSafety.Sha256File(path).Equals(reference.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Tracked Advanced game reference length/hash mismatch: " + reference.GameRelativePath);
            string assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(path).Name ?? string.Empty;
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                throw new InvalidDataException("Tracked Advanced reference is not the declared managed assembly: " + reference.GameRelativePath, ex);
            }
            if (!assemblyName.Equals(reference.AssemblyName, StringComparison.Ordinal))
                throw new InvalidDataException("Tracked Advanced reference assembly identity mismatch: " + reference.GameRelativePath);
            paths.Add(path);
        }
        return new ResolvedAdvancedReferenceSet
        {
            GameRoot = gameRoot,
            Policy = policy,
            Registration = registration,
            PolicySha256 = policySha256,
            ReferencePaths = paths
        };
    }

    private static string ReadSteamBuildId(string gameRoot)
    {
        DirectoryInfo root = new(gameRoot);
        DirectoryInfo common = root.Parent ?? throw new InvalidDataException("Advanced game root has no Steam common parent.");
        DirectoryInfo steamApps = common.Parent ?? throw new InvalidDataException("Advanced game root has no Steam steamapps parent.");
        if (!common.Name.Equals("common", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Advanced game root must be the explicit Steam steamapps/common game root.");
        string manifestPath = Path.Combine(steamApps.FullName, "appmanifest_" + SteamAppId + ".acf");
        if (!File.Exists(manifestPath))
            throw new InvalidDataException("Steam appmanifest_" + SteamAppId + ".acf is required beside the explicit game root.");
        string text = File.ReadAllText(manifestPath, Encoding.UTF8);
        Match appId = Regex.Match(text, "\\\"appid\\\"\\s+\\\"(?<value>[0-9]+)\\\"", RegexOptions.CultureInvariant);
        Match buildId = Regex.Match(text, "\\\"buildid\\\"\\s+\\\"(?<value>[0-9]+)\\\"", RegexOptions.CultureInvariant);
        if (!appId.Success || !appId.Groups["value"].Value.Equals(SteamAppId, StringComparison.Ordinal) || !buildId.Success)
            throw new InvalidDataException("Steam appmanifest identity/build is invalid for Doloc Town app " + SteamAppId + ".");
        return buildId.Groups["value"].Value;
    }

    private static void ValidateExactReceiptJson(string text, string path)
    {
        using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(path + " must contain one JSON object.");
        ValidateExactObject(document.RootElement, ReceiptProperties, path);
        JsonElement references = document.RootElement.GetProperty("references");
        if (references.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException(path + " references must be one JSON array.");
        int index = 0;
        foreach (JsonElement reference in references.EnumerateArray())
        {
            if (reference.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException(path + " references[" + index + "] must be one JSON object.");
            ValidateExactObject(reference, ReceiptReferenceProperties, path + " references[" + index + "]");
            index++;
        }
    }

    private static void ValidateExactObject(JsonElement element, IEnumerable<string> requiredProperties, string label)
    {
        var required = new HashSet<string>(requiredProperties, StringComparer.Ordinal);
        var observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!observed.Add(property.Name))
                throw new InvalidDataException(label + " contains duplicate field " + property.Name + ".");
            if (!required.Contains(property.Name))
                throw new InvalidDataException(label + " contains unknown field " + property.Name + ".");
        }
        string[] missing = required.Where(property => !observed.Contains(property)).OrderBy(property => property, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException(label + " is missing required fields: " + string.Join(", ", missing) + ".");
    }

    private static AdvancedReferencePolicyRegistry LoadRegistry()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RegistryResourceName)
            ?? throw new InvalidDataException("The SDK-embedded Advanced reference policy registry is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] bytes = memory.ToArray();
        using (JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The Advanced reference policy registry must contain one object.");
            ValidateExactObject(document.RootElement, RegistryProperties, "Advanced reference policy registry");
            JsonElement policies = document.RootElement.GetProperty("policies");
            if (policies.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("The Advanced reference policy registry policies field must be one array.");
            int index = 0;
            foreach (JsonElement row in policies.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("Advanced reference policy registry policies[" + index + "] must be one object.");
                ValidateExactObject(row, RegistryPolicyProperties, "Advanced reference policy registry policies[" + index + "]");
                index++;
            }
        }

        AdvancedReferencePolicyRegistry registry = JsonSerializer.Deserialize<AdvancedReferencePolicyRegistry>(bytes, JsonSupport.StrictToolContract)
            ?? throw new InvalidDataException("The SDK-embedded Advanced reference policy registry is invalid.");
        ValidateRegistry(registry);
        return registry;
    }

    private static void ValidateRegistry(AdvancedReferencePolicyRegistry registry)
    {
        if (registry.SchemaVersion != AuthorSdkContract.AdvancedReferencePolicyRegistrySchemaVersion ||
            registry.Policies == null || registry.Policies.Count == 0)
            throw new InvalidDataException("The SDK-embedded Advanced reference policy registry header is invalid.");
        if (registry.Policies.Any(registration => registration == null
            || string.IsNullOrWhiteSpace(registration.PolicyId)
            || !Regex.IsMatch(registration.PolicyId, "^[a-z0-9][a-z0-9.-]+$", RegexOptions.CultureInvariant)
            || !Sha256Pattern.IsMatch(registration.PolicySha256 ?? string.Empty)
            || !Sha256Pattern.IsMatch(registration.CompilerSurfaceSha256 ?? string.Empty)
            || string.IsNullOrWhiteSpace(registration.RequiredUniqueId)))
            throw new InvalidDataException("Advanced reference policy registry contains an invalid entry.");
        string[] ids = registry.Policies.Select(value => value.PolicyId).ToArray();
        if (!ids.SequenceEqual(ids.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal)
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || registry.Policies.Select(value => value.RequiredUniqueId).Distinct(StringComparer.Ordinal).Count() != registry.Policies.Count)
            throw new InvalidDataException("Advanced reference policy registry entries must have unique, canonical ordinal policyId order.");
    }

    private static void ValidatePolicy(AdvancedReferencePolicy policy, string registeredPolicyId)
    {
        if (policy.SchemaVersion != AuthorSdkContract.AdvancedReferencePolicySchemaVersion
            || !policy.PolicyId.Equals(registeredPolicyId, StringComparison.Ordinal)
            || policy.PolicyVersion != 1
            || !Regex.IsMatch(policy.GameBuildId, "^[0-9]+$", RegexOptions.CultureInvariant)
            || !Sha256Pattern.IsMatch(policy.GameAssemblySha256)
            || policy.References.Count == 0)
            throw new InvalidDataException("The SDK-embedded Advanced reference policy header is invalid.");

        string[] orderedPaths = policy.References.Select(reference => reference.GameRelativePath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (!policy.References.Select(reference => reference.GameRelativePath).SequenceEqual(orderedPaths, StringComparer.Ordinal))
            throw new InvalidDataException("Advanced reference policy entries must use canonical ordinal path order.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (AdvancedReferencePolicyEntry reference in policy.References)
        {
            ValidateRelativePath(reference.GameRelativePath, "Advanced policy reference path");
            if (!paths.Add(reference.GameRelativePath) || !names.Add(reference.AssemblyName) || reference.AssemblyName.Length == 0
                || reference.Length <= 0 || !Sha256Pattern.IsMatch(reference.Sha256) || reference.CopyLocal)
                throw new InvalidDataException("Advanced reference policy contains a duplicate or invalid reference entry.");
        }
        AdvancedReferencePolicyEntry gameAssembly = policy.References.SingleOrDefault(reference => reference.GameRelativePath.Equals(policy.GameAssemblyRelativePath, StringComparison.Ordinal))
            ?? throw new InvalidDataException("Advanced reference policy game assembly is not present in its exact reference list.");
        if (!gameAssembly.AssemblyName.Equals("Assembly-CSharp", StringComparison.Ordinal)
            || !gameAssembly.Sha256.Equals(policy.GameAssemblySha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Advanced reference policy game assembly identity/hash is inconsistent.");
    }

    private static string NormalizePackagePath(string value, string label)
    {
        ValidateRelativePath(value, label);
        return value.Replace('\\', '/');
    }

    private static void ValidateRelativePath(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\\') || value.Contains(':') || value.StartsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(value) || value.Split('/').Any(segment => segment is "" or "." or ".."))
            throw new InvalidDataException(label + " must be a canonical forward-slash relative path: " + value);
    }
}
