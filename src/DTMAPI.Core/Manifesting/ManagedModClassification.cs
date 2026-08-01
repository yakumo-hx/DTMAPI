using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Manifesting
{
    public enum ManagedModIdentity
    {
        StrictCodeMod,
        LegacyNativeCodeMod,
        AdvancedCodeMod,
        ContentPack
    }

    public sealed class ManagedModClassification
    {
        internal ManagedModClassification(
            ManagedModIdentity identity,
            string declaredKind,
            string declarationProvenance,
            string placement,
            string nativeRisk,
            string gameCompatibility,
            string restartPolicy,
            string expectedHarmonyOwner,
            bool advancedReferenceVerified,
            string referenceReceiptPath,
            string referencePolicyId,
            int referencePolicyVersion,
            string referencePolicySha256,
            string targetFramework,
            string gameBuildId,
            string sourceFingerprint = "",
            string entryDllSha256 = "",
            string entryModuleMvid = "")
        {
            Identity = identity;
            DeclaredKind = declaredKind ?? string.Empty;
            DeclarationProvenance = declarationProvenance ?? string.Empty;
            Placement = placement ?? string.Empty;
            NativeRisk = nativeRisk ?? string.Empty;
            GameCompatibility = gameCompatibility ?? string.Empty;
            RestartPolicy = restartPolicy ?? string.Empty;
            ExpectedHarmonyOwner = expectedHarmonyOwner ?? string.Empty;
            AdvancedReferenceVerified = advancedReferenceVerified;
            ReferenceReceiptPath = referenceReceiptPath ?? string.Empty;
            ReferencePolicyId = referencePolicyId ?? string.Empty;
            ReferencePolicyVersion = referencePolicyVersion;
            ReferencePolicySha256 = referencePolicySha256 ?? string.Empty;
            TargetFramework = targetFramework ?? string.Empty;
            GameBuildId = gameBuildId ?? string.Empty;
            SourceFingerprint = sourceFingerprint ?? string.Empty;
            EntryDllSha256 = entryDllSha256 ?? string.Empty;
            EntryModuleMvid = entryModuleMvid ?? string.Empty;
        }

        public ManagedModIdentity Identity { get; }
        public string IdentityName => Identity == ManagedModIdentity.StrictCodeMod
            ? "Strict CodeMod"
            : Identity == ManagedModIdentity.LegacyNativeCodeMod
                ? "Third-party native compatibility CodeMod"
                : Identity == ManagedModIdentity.AdvancedCodeMod
                    ? "Advanced CodeMod"
                    : "ContentPack";
        public string DeclaredKind { get; }
        public string EffectiveKind => Identity == ManagedModIdentity.AdvancedCodeMod
            ? "Advanced"
            : Identity == ManagedModIdentity.StrictCodeMod
                ? "Strict"
                : Identity == ManagedModIdentity.LegacyNativeCodeMod
                    ? "LegacyNativeCompatibility"
                    : "ContentPack";
        public string DeclarationProvenance { get; }
        public string Placement { get; }
        public string NativeRisk { get; }
        public string GameCompatibility { get; }
        public string RestartPolicy { get; }
        public string ExpectedHarmonyOwner { get; }
        public bool AdvancedReferenceVerified { get; }
        public string ReferenceReceiptPath { get; }
        public string ReferencePolicyId { get; }
        public int ReferencePolicyVersion { get; }
        public string ReferencePolicySha256 { get; }
        public string TargetFramework { get; }
        public string GameBuildId { get; }
        public string SourceFingerprint { get; }
        public string EntryDllSha256 { get; }
        public string EntryModuleMvid { get; }
        public bool IsCodeMod =>
            Identity == ManagedModIdentity.StrictCodeMod ||
            Identity == ManagedModIdentity.LegacyNativeCodeMod ||
            Identity == ManagedModIdentity.AdvancedCodeMod;
        public bool IsAdvanced => Identity == ManagedModIdentity.AdvancedCodeMod;
        public bool IsLegacyNativeCompatibility => Identity == ManagedModIdentity.LegacyNativeCodeMod;
        public bool IsContentPack => Identity == ManagedModIdentity.ContentPack;
        internal bool IsLegacyExternalCompatibility =>
            IsLegacyNativeCompatibility &&
            DeclarationProvenance.Equals("legacy-external-catalog-verified", StringComparison.Ordinal);
    }

    internal sealed class ManagedModClassificationException : IOException
    {
        public ManagedModClassificationException(string code, string message)
            : base((code ?? "managed-mod-classification-error") + ": " + (message ?? string.Empty))
        {
            Code = code ?? "managed-mod-classification-error";
        }

        public string Code { get; }
    }

    internal sealed class ManagedModClassifier
    {
        internal const string AdvancedReceiptRelativePath = "Content/DTMAPI/dtmapi-advanced-references.json";
        internal const string AdvancedReceiptKind = "DTMAPI.AdvancedCodeMod.ReferenceReceipt";
        internal const int AdvancedReceiptSchemaVersion = 1;
        internal const string AdvancedTargetFramework = "netstandard2.0";
        private const string SteamAppId = "2285550";
        private static readonly AdvancedReferencePolicyAuthority AdvancedPolicy = AdvancedReferencePolicyAuthority.Load();
        private static readonly LegacyExternalCompatibilityAuthority LegacyExternalPolicy = LegacyExternalCompatibilityAuthority.Load();
        private static readonly string[] AdvancedReceiptProperties =
        {
            "schemaVersion",
            "receiptKind",
            "uniqueId",
            "codeModKind",
            "referencePolicyId",
            "referencePolicyVersion",
            "referencePolicySha256",
            "targetFramework",
            "gameBuildId",
            "gameAssemblyRelativePath",
            "gameAssemblySha256",
            "manifestPath",
            "manifestSha256",
            "entryDllPath",
            "entryDllLength",
            "entryDllSha256",
            "harmonyOwner",
            "references"
        };
        private static readonly string[] AdvancedReceiptReferenceProperties =
        {
            "gameRelativePath",
            "assemblyName",
            "length",
            "sha256",
            "copyLocal"
        };

        private readonly string gamePath;
        private readonly LegacyExternalCompatibilityAuthority legacyExternalPolicy;

        public ManagedModClassifier(string gamePath)
            : this(gamePath, LegacyExternalPolicy)
        {
        }

        internal ManagedModClassifier(string gamePath, LegacyExternalCompatibilityAuthority legacyExternalPolicy)
        {
            this.gamePath = Path.GetFullPath(gamePath ?? throw new ArgumentNullException(nameof(gamePath)));
            this.legacyExternalPolicy = legacyExternalPolicy ?? throw new ArgumentNullException(nameof(legacyExternalPolicy));
        }

        internal static ManagedModClassification ClassifyDeclaredIdentity(ManifestModel manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            ManagedModIdentity identity = ResolveDeclaredIdentity(manifest, out string declaredKind, out string provenance);
            string owner = identity == ManagedModIdentity.AdvancedCodeMod
                ? GetExpectedHarmonyOwner(manifest.UniqueID)
                : string.Empty;
            return new ManagedModClassification(
                identity,
                declaredKind,
                provenance,
                "managed-source-unchecked",
                identity == ManagedModIdentity.AdvancedCodeMod
                    ? "native-code/receipt-unverified"
                    : identity == ManagedModIdentity.LegacyNativeCodeMod
                        ? "native-code/third-party-author-managed"
                        : identity == ManagedModIdentity.StrictCodeMod
                            ? "strict-managed-code"
                            : "declarative-content",
                identity == ManagedModIdentity.AdvancedCodeMod
                    ? "unverified"
                    : identity == ManagedModIdentity.LegacyNativeCodeMod
                        ? "author-managed/game-build-unbound"
                        : "not-applicable",
                identity == ManagedModIdentity.LegacyNativeCodeMod
                    ? "author-managed/restart-required-after-load/no-managed-hook-cleanup"
                    : identity == ManagedModIdentity.AdvancedCodeMod
                        ? "restart-required-after-load"
                        : identity == ManagedModIdentity.StrictCodeMod
                            ? "restart-required-after-assembly-load"
                            : "in-process-owner-lifecycle",
                owner,
                advancedReferenceVerified: false,
                referenceReceiptPath: string.Empty,
                referencePolicyId: string.Empty,
                referencePolicyVersion: 0,
                referencePolicySha256: string.Empty,
                targetFramework: string.Empty,
                gameBuildId: string.Empty);
        }

        public ManagedModClassification Classify(
            ManifestModel manifest,
            string modRoot,
            string manifestPath,
            string source,
            bool nativeWorkshopSourceVerified,
            ulong? workshopId)
        {
            // Every classifier check in this method completes before DtmApiRuntime reaches Assembly.LoadFrom.
            ManagedModClassification declaration = ClassifyDeclaredIdentity(manifest);
            string placement = ClassifyPlacement(source, nativeWorkshopSourceVerified, declaration.IsAdvanced);
            if (!declaration.IsAdvanced)
            {
                LegacyExternalCompatibilityMatch? legacyMatch = declaration.IsLegacyNativeCompatibility
                    ? legacyExternalPolicy.Match(
                        manifest,
                        modRoot,
                        source,
                        nativeWorkshopSourceVerified,
                        workshopId)
                    : null;
                if (declaration.IsCodeMod)
                    ValidateCodeModAssemblyBoundary(
                        manifest,
                        modRoot,
                        advanced: false,
                        Array.Empty<string>(),
                        strict: declaration.Identity == ManagedModIdentity.StrictCodeMod);
                string legacyEntryPath = declaration.IsLegacyNativeCompatibility
                    ? TryResolveLegacyEntryPath(modRoot, manifest.EntryDll)
                    : string.Empty;
                return new ManagedModClassification(
                    declaration.Identity,
                    declaration.DeclaredKind,
                    legacyMatch.HasValue ? "legacy-external-catalog-verified" : declaration.DeclarationProvenance,
                    placement,
                    legacyMatch.HasValue ? "native-code/legacy-exact-workshop-entry-hash-bound/author-managed" : declaration.NativeRisk,
                    legacyMatch.HasValue ? "legacy-input-identity-verified/game-build-unbound/author-managed" : declaration.GameCompatibility,
                    declaration.RestartPolicy,
                    declaration.ExpectedHarmonyOwner,
                    advancedReferenceVerified: false,
                    referenceReceiptPath: string.Empty,
                    referencePolicyId: string.Empty,
                    referencePolicyVersion: 0,
                    referencePolicySha256: string.Empty,
                    targetFramework: string.Empty,
                    gameBuildId: string.Empty,
                    sourceFingerprint: declaration.IsLegacyNativeCompatibility
                        ? ComputeLegacySourceFingerprint(modRoot, manifestPath)
                        : string.Empty,
                    entryDllSha256: legacyEntryPath.Length > 0
                        ? ComputeSha256(legacyEntryPath)
                        : string.Empty,
                    entryModuleMvid: legacyEntryPath.Length > 0
                        ? PortableAssemblyReferenceInspector.Inspect(legacyEntryPath).ModuleMvid
                        : string.Empty);
            }

            AdvancedReferenceVerification verification = VerifyAdvancedReceipt(manifest, modRoot, manifestPath);
            ValidateCodeModAssemblyBoundary(
                manifest,
                modRoot,
                advanced: true,
                verification.NativeReferenceAssemblyNames,
                strict: false);
            return new ManagedModClassification(
                ManagedModIdentity.AdvancedCodeMod,
                declaration.DeclaredKind,
                "sdk-reference-receipt-verified",
                placement,
                "native-code/hash-and-build-bound",
                "verified build=" + verification.GameBuildId + "; gameAssemblySha256=" + verification.GameAssemblySha256,
                "restart-required-after-load",
                verification.HarmonyOwner,
                advancedReferenceVerified: true,
                referenceReceiptPath: verification.ReceiptPath,
                referencePolicyId: verification.ReferencePolicyId,
                referencePolicyVersion: verification.ReferencePolicyVersion,
                referencePolicySha256: verification.ReferencePolicySha256,
                targetFramework: verification.TargetFramework,
                gameBuildId: verification.GameBuildId,
                sourceFingerprint: verification.SourceFingerprint,
                entryDllSha256: verification.EntryDllSha256,
                entryModuleMvid: verification.EntryModuleMvid);
        }

        internal static string GetExpectedHarmonyOwner(string uniqueId)
        {
            string id = (uniqueId ?? string.Empty).Trim();
            if (id.Length == 0)
                throw Failure("advanced-harmony-owner-mismatch", "Advanced CodeMod UniqueID is required before deriving its Harmony owner.");
            return "dtmapi.mod." + id.ToLowerInvariant();
        }

        private static ManagedModIdentity ResolveDeclaredIdentity(ManifestModel manifest, out string declaredKind, out string provenance)
        {
            bool legacyType = !manifest.TypeWasDeclared;
            string effectiveType = legacyType ? "CodeMod" : manifest.DeclaredTypeValue;
            if (!effectiveType.Equals("CodeMod", StringComparison.Ordinal) && !effectiveType.Equals("ContentPack", StringComparison.Ordinal))
                throw Failure("unknown-manifest-type", "Type must be exactly CodeMod or ContentPack; received '" + effectiveType + "'.");

            if (effectiveType.Equals("ContentPack", StringComparison.Ordinal))
            {
                if (manifest.CodeModKindWasDeclared)
                    throw Failure("code-mod-kind-on-content-pack", "ContentPack cannot declare CodeModKind.");
                if (!string.IsNullOrWhiteSpace(manifest.EntryDll) || !string.IsNullOrWhiteSpace(manifest.EntryType))
                    throw Failure("content-pack-with-code-fields", "ContentPack cannot declare EntryDll or EntryType.");
                declaredKind = "ContentPack";
                provenance = "declared-content-pack";
                return ManagedModIdentity.ContentPack;
            }

            if (!manifest.CodeModKindWasDeclared)
            {
                declaredKind = "omitted";
                provenance = "legacy-native-compatibility-unverified";
                return ManagedModIdentity.LegacyNativeCodeMod;
            }

            if (legacyType)
                throw Failure("unknown-code-mod-kind", "CodeModKind requires an explicit Type=CodeMod declaration.");

            string kind = manifest.DeclaredCodeModKindValue;
            if (kind.Equals("Strict", StringComparison.Ordinal))
            {
                declaredKind = "Strict";
                provenance = "declared-strict-unverified";
                return ManagedModIdentity.StrictCodeMod;
            }
            if (kind.Equals("Advanced", StringComparison.Ordinal))
            {
                declaredKind = "Advanced";
                provenance = "declared-advanced-receipt-pending";
                return ManagedModIdentity.AdvancedCodeMod;
            }

            throw Failure("unknown-code-mod-kind", "CodeModKind must be exactly Strict or Advanced; received '" + kind + "'.");
        }

        private static string ClassifyPlacement(string source, bool nativeWorkshopSourceVerified, bool advanced)
        {
            string value = source ?? string.Empty;
            if (value.Equals("Local", StringComparison.OrdinalIgnoreCase))
                return "managed-local";
            if (value.Equals("OfficialLocal", StringComparison.OrdinalIgnoreCase))
                return "managed-official-local";
            if (value.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
            {
                if (advanced && !nativeWorkshopSourceVerified)
                    throw Failure("advanced-placement-unverified", "Advanced Workshop CodeMods require a native-verified subscription/install root.");
                return nativeWorkshopSourceVerified ? "managed-workshop-native-verified" : "managed-workshop-compatibility";
            }

            throw Failure("unsupported-managed-placement", "Managed Mod source is unsupported: '" + value + "'.");
        }

        private AdvancedReferenceVerification VerifyAdvancedReceipt(ManifestModel manifest, string modRoot, string manifestPath)
        {
            string root = Path.GetFullPath(modRoot ?? string.Empty);
            string actualManifestPath = Path.GetFullPath(manifestPath ?? string.Empty);
            string receiptPath = ResolveContainedPath(root, AdvancedReceiptRelativePath, "advanced-reference-receipt-invalid");
            if (!File.Exists(receiptPath))
                throw Failure("advanced-reference-receipt-missing", "Advanced reference receipt is missing: " + AdvancedReceiptRelativePath + ".");

            ValidateExactTopLevelProperties(receiptPath, AdvancedReceiptProperties, "advanced-reference-receipt-invalid");
            ValidateExactReferenceProperties(receiptPath);

            AdvancedReferenceReceipt receipt;
            try
            {
                receipt = JsonFile.Read<AdvancedReferenceReceipt>(receiptPath);
            }
            catch (Exception ex)
            {
                throw Failure("advanced-reference-receipt-invalid", "Advanced reference receipt could not be parsed: " + ex.GetType().Name + ": " + ex.Message);
            }

            if (receipt.SchemaVersion != AdvancedReceiptSchemaVersion)
                throw Failure("advanced-reference-receipt-invalid", "Unsupported Advanced receipt schemaVersion=" + receipt.SchemaVersion.ToString(CultureInfo.InvariantCulture) + ".");
            RequireExact(receipt.ReceiptKind, AdvancedReceiptKind, "advanced-reference-receipt-invalid", "receiptKind");
            RequireExact(receipt.UniqueId, manifest.UniqueID, "advanced-reference-receipt-invalid", "uniqueId");
            RequireExact(receipt.CodeModKind, "Advanced", "advanced-reference-receipt-invalid", "codeModKind");
            RequireExact(receipt.TargetFramework, AdvancedTargetFramework, "wrong-target-framework", "targetFramework");
            if (!AdvancedPolicy.Matches(receipt, out string policyMismatch))
                throw Failure("advanced-reference-policy-mismatch", policyMismatch);

            string expectedOwner = GetExpectedHarmonyOwner(manifest.UniqueID);
            RequireExact(receipt.HarmonyOwner, expectedOwner, "advanced-harmony-owner-mismatch", "harmonyOwner");

            string receiptManifestPath = ResolveContainedPath(root, receipt.ManifestPath, "advanced-reference-receipt-invalid");
            if (!PathsEqual(receiptManifestPath, actualManifestPath))
                throw Failure("advanced-reference-receipt-invalid", "manifestPath does not name the manifest selected by Core discovery.");
            VerifyFile(receiptManifestPath, expectedLength: null, receipt.ManifestSha256, "advanced-reference-hash-mismatch", "manifest");

            string entryPath = ResolveContainedPath(root, receipt.EntryDllPath, "advanced-reference-receipt-invalid");
            string manifestEntryPath = ResolveContainedPath(root, manifest.EntryDll, "advanced-reference-receipt-invalid");
            if (!PathsEqual(entryPath, manifestEntryPath))
                throw Failure("advanced-reference-receipt-invalid", "entryDllPath does not match manifest EntryDll.");
            VerifyFile(entryPath, receipt.EntryDllLength, receipt.EntryDllSha256, "advanced-reference-hash-mismatch", "entry DLL");
            ValidateAdvancedPackageShape(root, entryPath);
            PortableAssemblyMetadata entryMetadata = PortableAssemblyReferenceInspector.Inspect(entryPath);

            if (receipt.References == null || receipt.References.Count == 0)
                throw Failure("advanced-reference-receipt-invalid", "Advanced receipt references must contain at least one tracked game reference.");

            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AdvancedReferenceReceiptItem? gameAssemblyRow = null;
            foreach (AdvancedReferenceReceiptItem reference in receipt.References)
            {
                if (reference == null)
                    throw Failure("advanced-reference-receipt-invalid", "Advanced receipt contains a null reference row.");
                if (reference.CopyLocal)
                    throw Failure("advanced-reference-receipt-invalid", "Advanced references must declare copyLocal=false.");
                if (string.IsNullOrWhiteSpace(reference.AssemblyName) || !paths.Add(reference.GameRelativePath ?? string.Empty) || !assemblyNames.Add(reference.AssemblyName))
                    throw Failure("advanced-reference-receipt-invalid", "Advanced reference paths and assembly names must be non-empty and unique.");

                string referencePath = ResolveContainedPath(gamePath, reference.GameRelativePath ?? string.Empty, "advanced-reference-receipt-invalid");
                if (IsInsideRoot(referencePath, root))
                    throw Failure("advanced-reference-receipt-invalid", "Advanced native references cannot resolve inside the managed Mod package.");
                if (!Path.GetExtension(referencePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    throw Failure("advanced-reference-receipt-invalid", "Advanced reference must name a DLL: " + reference.GameRelativePath + ".");
                VerifyFile(referencePath, reference.Length, reference.Sha256, "advanced-reference-hash-mismatch", "reference " + reference.AssemblyName);
                if (string.Equals(reference.GameRelativePath, receipt.GameAssemblyRelativePath, StringComparison.OrdinalIgnoreCase))
                    gameAssemblyRow = reference;
            }

            if (gameAssemblyRow == null)
                throw Failure("advanced-reference-receipt-invalid", "gameAssemblyRelativePath must name one row in references.");
            RequireExactHash(receipt.GameAssemblySha256, gameAssemblyRow.Sha256, "advanced-reference-receipt-invalid", "gameAssemblySha256");

            if (!TryReadSteamBuildId(gamePath, out string currentBuildId, out string buildFailure))
                throw Failure("advanced-game-build-unverifiable", buildFailure);
            if (!string.Equals(currentBuildId, receipt.GameBuildId, StringComparison.Ordinal))
                throw Failure("advanced-game-build-incompatible", "Advanced receipt gameBuildId=" + receipt.GameBuildId + "; installed buildId=" + currentBuildId + ".");

            VerifyAdvancedPackageMarker(manifest, root, actualManifestPath, entryPath, receiptPath, receipt);
            string sourceFingerprint = ComputeSourceFingerprint(root);

            return new AdvancedReferenceVerification(
                receiptPath,
                receipt.ReferencePolicyId,
                receipt.ReferencePolicyVersion,
                receipt.ReferencePolicySha256,
                receipt.TargetFramework,
                receipt.GameBuildId,
                receipt.GameAssemblySha256,
                receipt.HarmonyOwner,
                receipt.References.Select(reference => reference.AssemblyName).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                sourceFingerprint,
                receipt.EntryDllSha256,
                entryMetadata.ModuleMvid);
        }

        private static void ValidateCodeModAssemblyBoundary(
            ManifestModel manifest,
            string modRoot,
            bool advanced,
            IReadOnlyList<string> allowedNativeReferences,
            bool strict)
        {
            if (string.IsNullOrWhiteSpace(manifest.EntryDll))
                return;
            string root = Path.GetFullPath(modRoot ?? string.Empty);
            string entryPath;
            if (advanced)
            {
                entryPath = ResolveContainedPath(root, manifest.EntryDll, "advanced-reference-receipt-invalid");
            }
            else
            {
                if (Path.IsPathRooted(manifest.EntryDll))
                    return; // The existing owner-specific loader diagnostic remains authoritative.
                entryPath = Path.GetFullPath(Path.Combine(root, manifest.EntryDll));
                if (!IsInsideRoot(entryPath, root) || !Path.GetExtension(entryPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    return; // Preserve the existing path/extension diagnostics before any assembly load.
            }
            if (!File.Exists(entryPath))
                return;

            PortableAssemblyMetadata metadata = PortableAssemblyReferenceInspector.Inspect(entryPath);
            if (advanced && !metadata.TargetFramework.Equals(".NETStandard,Version=v2.0", StringComparison.Ordinal))
            {
                throw Failure(
                    "wrong-target-framework",
                    "CodeMod entry DLL TargetFramework must be exactly netstandard2.0; observed " +
                    (metadata.TargetFramework.Length == 0 ? "missing" : metadata.TargetFramework) + ".");
            }
            if (IsForbiddenBundledPlatformAssemblyName(metadata.AssemblyName))
                throw Failure(
                    strict ? "strict-native-reference-forbidden" : "bundled-native-runtime-dependency",
                    "CodeMod entry DLL impersonates a native/runtime assembly: " + metadata.AssemblyName + ".");

            string[] native = metadata.AssemblyReferences
                .Where(reference => !IsPlatformManagedReference(reference))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!advanced)
            {
                if (!strict)
                {
                    ValidateLegacyAssemblyClosure(root);
                    return;
                }
                ValidateStrictAssemblyClosure(root);
                if (native.Length > 0)
                {
                    throw Failure(
                        "strict-native-reference-forbidden",
                        "Strict CodeMod entry DLL contains SDK160-forbidden native AssemblyRefs: " + string.Join(", ", native) + ".");
                }
                return;
            }

            string[] expected = (allowedNativeReferences ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (!native.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            {
                throw Failure(
                    "advanced-native-reference-policy-mismatch",
                    "Advanced native AssemblyRefs must exactly match the tracked receipt policy; expected=" +
                    string.Join(",", expected) + "; actual=" + string.Join(",", native) + ".");
            }
        }

        private static bool IsNativeReference(string name)
        {
            string value = name ?? string.Empty;
            return value.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("0Harmony", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForbiddenBundledPlatformAssemblyName(string name)
        {
            string value = name ?? string.Empty;
            return IsNativeReference(value) ||
                value.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("DTMAPI.Core", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("DTMAPI.BepInExBootstrap", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("DTMAPI.GameBridge.DolocTown.Compatibility", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("DTMAPI.ModConfigMenu", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlatformManagedReference(string name)
        {
            string value = name ?? string.Empty;
            return value.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateStrictAssemblyClosure(string root)
        {
            string[] files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw Failure("strict-native-reference-forbidden", "Strict CodeMod DLL closure could not be enumerated before load: " + ex.GetType().Name + ": " + ex.Message);
            }

            foreach (string assemblyPath in files)
            {
                if (AdvancedPolicy.TryMatchTrackedPayload(assemblyPath, out string trackedIdentity))
                {
                    throw Failure(
                        "bundled-native-runtime-dependency",
                        "Strict package contains tracked game/runtime bytes under an arbitrary payload name: file=" +
                        Path.GetFileName(assemblyPath) + "; identity=" + trackedIdentity + ".");
                }
                if (!LooksLikePortableExecutable(assemblyPath))
                    continue;
                PortableAssemblyMetadata candidate = PortableAssemblyReferenceInspector.Inspect(assemblyPath);
                string[] forbidden = candidate.AssemblyReferences.Where(reference => !IsPlatformManagedReference(reference)).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
                if (IsForbiddenBundledPlatformAssemblyName(candidate.AssemblyName) || forbidden.Length > 0)
                {
                    throw Failure(
                        "strict-native-reference-forbidden",
                        "Strict CodeMod DLL closure contains an SDK160-forbidden native/runtime assembly or AssemblyRef: file=" +
                        Path.GetFileName(assemblyPath) + "; assembly=" + candidate.AssemblyName + "; refs=" + string.Join(", ", forbidden) + ".");
                }
            }
        }

        private static void ValidateLegacyAssemblyClosure(string root)
        {
            string[] files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw Failure(
                    "legacy-native-package-unreadable",
                    "Legacy native compatibility package could not be enumerated before cold load: " +
                    ex.GetType().Name + ": " + ex.Message);
            }

            foreach (string file in files)
            {
                try
                {
                    if (AdvancedPolicy.TryMatchTrackedPayload(file, out string trackedIdentity))
                    {
                        throw Failure(
                            "bundled-native-runtime-dependency",
                            "Legacy native compatibility package contains tracked game/runtime bytes instead of an author-owned helper: file=" +
                            Path.GetFileName(file) + "; identity=" + trackedIdentity + ".");
                    }
                }
                catch (ManagedModClassificationException)
                {
                    throw;
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is InvalidDataException)
                {
                    // Legacy dependencies are author-managed. Failure to compare an
                    // unrelated helper with the optional tracked-byte catalog must not
                    // turn the compatibility lane back into Strict.
                }

                bool portableExecutable;
                try
                {
                    portableExecutable = LooksLikePortableExecutable(file);
                }
                catch (ManagedModClassificationException)
                {
                    continue;
                }
                if (!portableExecutable)
                    continue;

                PortableAssemblyMetadata candidate;
                try
                {
                    candidate = PortableAssemblyReferenceInspector.Inspect(file);
                }
                catch (ManagedModClassificationException ex) when (
                    ex.Code.Equals("entry-assembly-metadata-invalid", StringComparison.Ordinal))
                {
                    // A private unmanaged helper remains the third-party author's
                    // responsibility. Only managed platform/runtime impersonation and
                    // exact tracked bytes are rejected here.
                    continue;
                }
                if (IsForbiddenBundledPlatformAssemblyName(candidate.AssemblyName))
                {
                    throw Failure(
                        "bundled-native-runtime-dependency",
                        "Legacy native compatibility package carries a copied platform/game assembly instead of resolving it from the game: file=" +
                        Path.GetFileName(file) + "; assembly=" + candidate.AssemblyName + ".");
                }
            }
        }

        private static void ValidateAdvancedPackageShape(string root, string entryPath)
        {
            string[] files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw Failure("bundled-native-runtime-dependency", "Advanced package DLL inventory could not be enumerated: " + ex.GetType().Name + ": " + ex.Message);
            }
            string[] dlls = files.Where(path => Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (dlls.Length != 1 || !PathsEqual(dlls[0], entryPath))
            {
                throw Failure(
                    "bundled-native-runtime-dependency",
                    "Advanced package must contain exactly its declared entry DLL and no bundled native/runtime DLLs; observed=" +
                    string.Join(",", dlls.Select(path => Path.GetFileName(path)).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) + ".");
            }
            foreach (string file in files)
            {
                if (AdvancedPolicy.TryMatchTrackedPayload(file, out string trackedIdentity))
                {
                    throw Failure(
                        "bundled-native-runtime-dependency",
                        "Advanced package contains tracked game/runtime bytes under an arbitrary payload name: file=" +
                        Path.GetFileName(file) + "; identity=" + trackedIdentity + ".");
                }
                if (!LooksLikePortableExecutable(file))
                    continue;
                PortableAssemblyMetadata candidate = PortableAssemblyReferenceInspector.Inspect(file);
                if (!PathsEqual(file, entryPath) || IsForbiddenBundledPlatformAssemblyName(candidate.AssemblyName))
                {
                    throw Failure(
                        "bundled-native-runtime-dependency",
                        "Advanced package contains a managed executable payload other than its declared non-native entry: file=" +
                        Path.GetFileName(file) + "; assembly=" + candidate.AssemblyName + ".");
                }
            }
        }

        private static bool LooksLikePortableExecutable(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    return stream.Length >= 2 && stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw Failure("bundled-native-runtime-dependency", "Managed package payload could not be inspected before load: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static string ComputeSourceFingerprint(string root)
        {
            try
            {
                string fullRoot = Path.GetFullPath(root ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string[] files = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => GetNormalizedRelativePath(fullRoot, path), StringComparer.Ordinal)
                    .ToArray();
                var canonical = new StringBuilder();
                foreach (string file in files)
                {
                    string relative = GetNormalizedRelativePath(fullRoot, file);
                    var info = new FileInfo(file);
                    canonical.Append(relative).Append('\0')
                        .Append(info.Length.ToString(CultureInfo.InvariantCulture)).Append('\0')
                        .Append(ComputeSha256(file)).Append('\n');
                }
                byte[] bytes = new UTF8Encoding(false).GetBytes(canonical.ToString());
                using (SHA256 sha256 = SHA256.Create())
                    return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty);
            }
            catch (ManagedModClassificationException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                throw Failure("advanced-source-fingerprint-invalid", "Advanced package source fingerprint could not be computed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static string ComputeLegacySourceFingerprint(string root, string manifestPath)
        {
            try
            {
                string fullRoot = Path.GetFullPath(root ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string candidateManifestPath = manifestPath ?? string.Empty;
                string fullManifestPath = Path.IsPathRooted(candidateManifestPath)
                    ? Path.GetFullPath(candidateManifestPath)
                    : Path.GetFullPath(Path.Combine(fullRoot, candidateManifestPath));
                if (!IsInsideRoot(fullManifestPath, fullRoot) || !File.Exists(fullManifestPath))
                    throw new IOException("Legacy manifest is unavailable or outside its package root.");

                string[] files = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .Where(path =>
                        PathsEqual(path, fullManifestPath) ||
                        Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => GetNormalizedRelativePath(fullRoot, path), StringComparer.Ordinal)
                    .ToArray();
                var canonical = new StringBuilder();
                foreach (string file in files)
                {
                    string relative = GetNormalizedRelativePath(fullRoot, file);
                    var info = new FileInfo(file);
                    canonical.Append(relative).Append('\0')
                        .Append(info.Length.ToString(CultureInfo.InvariantCulture)).Append('\0')
                        .Append(ComputeSha256(file)).Append('\n');
                }
                byte[] bytes = new UTF8Encoding(false).GetBytes(canonical.ToString());
                using (SHA256 sha256 = SHA256.Create())
                    return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty);
            }
            catch (ManagedModClassificationException)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is ArgumentException)
            {
                throw Failure(
                    "legacy-native-source-fingerprint-invalid",
                    "Legacy native compatibility code closure fingerprint could not be computed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static string TryResolveLegacyEntryPath(string root, string relativePath)
        {
            string fullRoot = Path.GetFullPath(root ?? string.Empty);
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                return string.Empty;
            string entryPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
            if (!IsInsideRoot(entryPath, fullRoot) ||
                !Path.GetExtension(entryPath).Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(entryPath))
                return string.Empty;
            return entryPath;
        }

        private static void VerifyAdvancedPackageMarker(
            ManifestModel manifest,
            string root,
            string manifestPath,
            string entryPath,
            string receiptPath,
            AdvancedReferenceReceipt receipt)
        {
            const string markerRelativePath = "Content/DTMAPI/dtmapi-package.json";
            string markerPath = ResolveContainedPath(root, markerRelativePath, "advanced-package-marker-invalid");
            if (!File.Exists(markerPath))
                throw Failure("advanced-package-marker-missing", "Advanced package is missing the SDK package-binding marker: " + markerRelativePath + ".");
            ValidateExactTopLevelProperties(markerPath, AdvancedPackageMarker.Properties, "advanced-package-marker-invalid");
            AdvancedPackageMarker marker;
            try
            {
                marker = JsonFile.Read<AdvancedPackageMarker>(markerPath);
            }
            catch (Exception ex)
            {
                throw Failure("advanced-package-marker-invalid", "Advanced package marker could not be parsed: " + ex.GetType().Name + ": " + ex.Message);
            }

            string manifestRelative = GetNormalizedRelativePath(root, manifestPath);
            string entryRelative = GetNormalizedRelativePath(root, entryPath);
            string receiptRelative = GetNormalizedRelativePath(root, receiptPath);
            if (marker.SchemaVersion != 2 ||
                !marker.Owner.Equals("DTMAPI", StringComparison.Ordinal) ||
                !marker.UniqueId.Equals(manifest.UniqueID, StringComparison.Ordinal) ||
                !marker.Version.Equals(manifest.Version, StringComparison.Ordinal) ||
                !marker.PackageKind.Equals("CodeMod", StringComparison.Ordinal) ||
                !marker.CodeModKind.Equals("Advanced", StringComparison.Ordinal) ||
                !marker.AuthorSdkVersion.Equals("0.1.0", StringComparison.Ordinal) ||
                !marker.TargetDtmApiVersion.Equals(DtmApiRuntime.ApiVersion, StringComparison.Ordinal) ||
                !marker.ManifestPath.Equals(manifestRelative, StringComparison.Ordinal) ||
                !marker.ManifestPath.Equals(receipt.ManifestPath, StringComparison.Ordinal) ||
                !marker.ManifestSha256.Equals(receipt.ManifestSha256, StringComparison.OrdinalIgnoreCase) ||
                !marker.EntryDllPath.Equals(entryRelative, StringComparison.Ordinal) ||
                !marker.EntryDllPath.Equals(receipt.EntryDllPath, StringComparison.Ordinal) ||
                !marker.EntryDllSha256.Equals(receipt.EntryDllSha256, StringComparison.OrdinalIgnoreCase) ||
                !marker.AdvancedReferenceReceiptPath.Equals(receiptRelative, StringComparison.Ordinal) ||
                !marker.AdvancedReferenceReceiptSha256.Equals(ComputeSha256(receiptPath), StringComparison.OrdinalIgnoreCase) ||
                !marker.Authority.Equals("dtmapi-author-sdk-package-binding", StringComparison.Ordinal))
            {
                throw Failure("advanced-package-marker-invalid", "dtmapi-package.json does not bind the exact Advanced manifest/entry/reference receipt and Runtime target.");
            }
        }

        private static string GetNormalizedRelativePath(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path);
            if (!IsInsideRoot(fullPath, fullRoot))
                throw Failure("advanced-package-marker-invalid", "Package binding path escapes its managed root.");
            return fullPath.Substring(fullRoot.Length + 1).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static void ValidateExactTopLevelProperties(string path, IEnumerable<string> allowedProperties, string code)
        {
            IReadOnlyList<string> names;
            try
            {
                names = JsonTopLevelPropertyReader.Read(path);
            }
            catch (Exception ex)
            {
                throw Failure(code, "JSON object property scan failed: " + ex.Message);
            }

            var allowed = new HashSet<string>(allowedProperties, StringComparer.Ordinal);
            string[] unknown = names.Where(name => !allowed.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
            string[] duplicate = names.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() != 1).Select(group => group.Key).ToArray();
            string[] missing = allowed.Where(name => !names.Contains(name, StringComparer.Ordinal)).ToArray();
            if (unknown.Length > 0 || duplicate.Length > 0 || missing.Length > 0)
            {
                throw Failure(
                    code,
                    "Receipt properties must exactly match schema; unknown=" + string.Join(",", unknown) +
                    "; duplicate=" + string.Join(",", duplicate) +
                    "; missing=" + string.Join(",", missing) + ".");
            }
        }

        private static void ValidateExactReferenceProperties(string path)
        {
            IReadOnlyList<IReadOnlyList<string>> rows;
            try
            {
                rows = JsonTopLevelPropertyReader.ReadObjectArrayProperty(path, "references");
            }
            catch (Exception ex)
            {
                throw Failure("advanced-reference-receipt-invalid", "Receipt references property scan failed: " + ex.Message);
            }
            var expected = new HashSet<string>(AdvancedReceiptReferenceProperties, StringComparer.Ordinal);
            for (int index = 0; index < rows.Count; index++)
            {
                IReadOnlyList<string> names = rows[index];
                string[] unknown = names.Where(name => !expected.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
                string[] duplicate = names.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() != 1).Select(group => group.Key).ToArray();
                string[] missing = expected.Where(name => !names.Contains(name, StringComparer.Ordinal)).ToArray();
                if (unknown.Length > 0 || duplicate.Length > 0 || missing.Length > 0)
                {
                    throw Failure(
                        "advanced-reference-receipt-invalid",
                        "Receipt reference row " + index.ToString(CultureInfo.InvariantCulture) +
                        " must exactly match schema; unknown=" + string.Join(",", unknown) +
                        "; duplicate=" + string.Join(",", duplicate) +
                        "; missing=" + string.Join(",", missing) + ".");
                }
            }
        }

        private static void VerifyFile(string path, long? expectedLength, string expectedSha256, string code, string label)
        {
            if (!File.Exists(path))
                throw Failure(code, label + " is missing: " + path + ".");
            long actualLength = new FileInfo(path).Length;
            if (expectedLength.HasValue && (expectedLength.Value < 0 || actualLength != expectedLength.Value))
                throw Failure(code, label + " length mismatch; expected=" + expectedLength.Value.ToString(CultureInfo.InvariantCulture) + "; actual=" + actualLength.ToString(CultureInfo.InvariantCulture) + ".");
            if (!IsSha256(expectedSha256))
                throw Failure(code, label + " SHA-256 is malformed.");
            string actualSha256 = ComputeSha256(path);
            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw Failure(code, label + " SHA-256 mismatch; expected=" + expectedSha256 + "; actual=" + actualSha256 + ".");
        }

        private static string ResolveContainedPath(string root, string relativePath, string code)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw Failure(code, "A non-empty relative path is required; received '" + (relativePath ?? string.Empty) + "'.");
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string resolved = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsInsideRoot(resolved, fullRoot))
                throw Failure(code, "Path escapes its authority root: " + relativePath + ".");
            return resolved;
        }

        private static bool IsInsideRoot(string path, string root)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        internal static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char c = value[index];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        internal static bool IsSha256ForPolicy(string value) => IsSha256(value);

        private static void RequireExact(string actual, string expected, string code, string field)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw Failure(code, field + " mismatch; expected='" + expected + "'; actual='" + (actual ?? string.Empty) + "'.");
        }

        private static void RequireExactHash(string actual, string expected, string code, string field)
        {
            if (!IsSha256(actual) || !IsSha256(expected) || !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw Failure(code, field + " mismatch; expected='" + expected + "'; actual='" + (actual ?? string.Empty) + "'.");
        }

        private static bool TryReadSteamBuildId(string gameRoot, out string buildId, out string failure)
        {
            buildId = string.Empty;
            failure = string.Empty;
            string[] candidates =
            {
                Path.GetFullPath(Path.Combine(gameRoot, "..", "..", "appmanifest_" + SteamAppId + ".acf")),
                Path.GetFullPath(Path.Combine(gameRoot, "steamapps", "appmanifest_" + SteamAppId + ".acf"))
            };
            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(candidate))
                    continue;
                try
                {
                    string text = File.ReadAllText(candidate);
                    Match match = Regex.Match(text, "\\\"buildid\\\"\\s+\\\"(?<id>\\d+)\\\"", RegexOptions.CultureInvariant);
                    if (match.Success)
                    {
                        buildId = match.Groups["id"].Value;
                        return true;
                    }
                    failure = "Steam appmanifest does not contain a numeric buildid: " + candidate + ".";
                    return false;
                }
                catch (Exception ex)
                {
                    failure = "Steam appmanifest could not be read: " + candidate + "; " + ex.GetType().Name + ": " + ex.Message;
                    return false;
                }
            }

            failure = "Steam appmanifest_" + SteamAppId + ".acf was not found from the explicit game root; Advanced game build is unverifiable.";
            return false;
        }

        private static ManagedModClassificationException Failure(string code, string message) => new ManagedModClassificationException(code, message);
    }

    [DataContract]
    internal sealed class AdvancedPackageMarker
    {
        internal static readonly string[] Properties =
        {
            "advancedReferenceReceiptPath",
            "advancedReferenceReceiptSha256",
            "authorSdkVersion",
            "authority",
            "codeModKind",
            "entryDllPath",
            "entryDllSha256",
            "manifestPath",
            "manifestSha256",
            "owner",
            "packageKind",
            "schemaVersion",
            "targetDtmApiVersion",
            "uniqueId",
            "version"
        };

        [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion { get; set; }
        [DataMember(Name = "owner", IsRequired = true)] public string Owner { get; set; } = string.Empty;
        [DataMember(Name = "uniqueId", IsRequired = true)] public string UniqueId { get; set; } = string.Empty;
        [DataMember(Name = "version", IsRequired = true)] public string Version { get; set; } = string.Empty;
        [DataMember(Name = "packageKind", IsRequired = true)] public string PackageKind { get; set; } = string.Empty;
        [DataMember(Name = "codeModKind", IsRequired = true)] public string CodeModKind { get; set; } = string.Empty;
        [DataMember(Name = "authorSdkVersion", IsRequired = true)] public string AuthorSdkVersion { get; set; } = string.Empty;
        [DataMember(Name = "targetDtmApiVersion", IsRequired = true)] public string TargetDtmApiVersion { get; set; } = string.Empty;
        [DataMember(Name = "manifestPath", IsRequired = true)] public string ManifestPath { get; set; } = string.Empty;
        [DataMember(Name = "manifestSha256", IsRequired = true)] public string ManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "entryDllPath", IsRequired = true)] public string EntryDllPath { get; set; } = string.Empty;
        [DataMember(Name = "entryDllSha256", IsRequired = true)] public string EntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "advancedReferenceReceiptPath", IsRequired = true)] public string AdvancedReferenceReceiptPath { get; set; } = string.Empty;
        [DataMember(Name = "advancedReferenceReceiptSha256", IsRequired = true)] public string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
        [DataMember(Name = "authority", IsRequired = true)] public string Authority { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class AdvancedReferencePolicy
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion { get; set; }
        [DataMember(Name = "policyId", IsRequired = true)] public string PolicyId { get; set; } = string.Empty;
        [DataMember(Name = "policyVersion", IsRequired = true)] public int PolicyVersion { get; set; }
        [DataMember(Name = "gameBuildId", IsRequired = true)] public string GameBuildId { get; set; } = string.Empty;
        [DataMember(Name = "gameAssemblyRelativePath", IsRequired = true)] public string GameAssemblyRelativePath { get; set; } = string.Empty;
        [DataMember(Name = "gameAssemblySha256", IsRequired = true)] public string GameAssemblySha256 { get; set; } = string.Empty;
        [DataMember(Name = "references", IsRequired = true)] public List<AdvancedReferenceReceiptItem> References { get; set; } = new List<AdvancedReferenceReceiptItem>();
    }

    [DataContract]
    internal sealed class AdvancedReferencePolicyRegistry
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion { get; set; }
        [DataMember(Name = "policies", IsRequired = true)] public List<AdvancedReferencePolicyRegistration> Policies { get; set; } = new List<AdvancedReferencePolicyRegistration>();
    }

    [DataContract]
    internal sealed class AdvancedReferencePolicyRegistration
    {
        [DataMember(Name = "policyId", IsRequired = true)] public string PolicyId { get; set; } = string.Empty;
        [DataMember(Name = "policySha256", IsRequired = true)] public string PolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "requiredUniqueId", IsRequired = true)] public string RequiredUniqueId { get; set; } = string.Empty;
        [DataMember(Name = "compilerSurfaceSha256", IsRequired = true)] public string CompilerSurfaceSha256 { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class LegacyExternalCompatibilityCatalog
    {
        [DataMember(Name = "externalCompatibilityBaseline", IsRequired = true)]
        public List<LegacyExternalCompatibilityCatalogRow> ExternalCompatibilityBaseline { get; set; } =
            new List<LegacyExternalCompatibilityCatalogRow>();
    }

    [DataContract]
    internal sealed class LegacyExternalCompatibilityCatalogRow
    {
        public LegacyExternalCompatibilityCatalogRow()
        {
        }

        internal LegacyExternalCompatibilityCatalogRow(
            string workshopId,
            string uniqueId,
            string releaseGate,
            string entryDllRelativePath,
            string entryDllSha256)
        {
            WorkshopId = workshopId;
            UniqueId = uniqueId;
            Classification = "ExternalApiConsumer";
            ReleaseGate = releaseGate;
            LegacyNativeAdmission = new LegacyExternalNativeAdmission
            {
                Scope = "NativeVerifiedWorkshopExactEntry",
                EntryDllRelativePath = entryDllRelativePath,
                EntryDllSha256 = entryDllSha256
            };
        }

        [DataMember(Name = "workshopId", IsRequired = true)] public string WorkshopId { get; set; } = string.Empty;
        [DataMember(Name = "uniqueId", IsRequired = true)] public string UniqueId { get; set; } = string.Empty;
        [DataMember(Name = "classification", IsRequired = true)] public string Classification { get; set; } = string.Empty;
        [DataMember(Name = "releaseGate", IsRequired = true)] public string ReleaseGate { get; set; } = string.Empty;
        [DataMember(Name = "legacyNativeAdmission", EmitDefaultValue = false)] public LegacyExternalNativeAdmission? LegacyNativeAdmission { get; set; }
    }

    [DataContract]
    internal sealed class LegacyExternalNativeAdmission
    {
        [DataMember(Name = "scope", IsRequired = true)] public string Scope { get; set; } = string.Empty;
        [DataMember(Name = "entryDllRelativePath", IsRequired = true)] public string EntryDllRelativePath { get; set; } = string.Empty;
        [DataMember(Name = "entryDllSha256", IsRequired = true)] public string EntryDllSha256 { get; set; } = string.Empty;
    }

    internal readonly struct LegacyExternalCompatibilityMatch
    {
        public LegacyExternalCompatibilityMatch(string entryPath, string entryDllSha256, string entryModuleMvid)
        {
            EntryPath = entryPath ?? string.Empty;
            EntryDllSha256 = entryDllSha256 ?? string.Empty;
            EntryModuleMvid = entryModuleMvid ?? string.Empty;
        }

        public string EntryPath { get; }
        public string EntryDllSha256 { get; }
        public string EntryModuleMvid { get; }
    }

    internal sealed class LegacyExternalCompatibilityAuthority
    {
        private const string ResourceName = "DTMAPI.Core.ProductCatalog.json";
        private const string AdmissionScope = "NativeVerifiedWorkshopExactEntry";
        private readonly IReadOnlyDictionary<ulong, LegacyExternalCompatibilityCatalogRow> rows;
        private readonly string error;

        private LegacyExternalCompatibilityAuthority(
            IReadOnlyDictionary<ulong, LegacyExternalCompatibilityCatalogRow>? rows,
            string error)
        {
            this.rows = rows ?? new Dictionary<ulong, LegacyExternalCompatibilityCatalogRow>();
            this.error = error ?? string.Empty;
        }

        public static LegacyExternalCompatibilityAuthority Load()
        {
            try
            {
                Assembly assembly = typeof(LegacyExternalCompatibilityAuthority).GetTypeInfo().Assembly;
                using Stream resource = assembly.GetManifestResourceStream(ResourceName)
                    ?? throw new InvalidDataException("Embedded product Catalog is unavailable.");
                var serializer = new DataContractJsonSerializer(typeof(LegacyExternalCompatibilityCatalog));
                var catalog = serializer.ReadObject(resource) as LegacyExternalCompatibilityCatalog
                    ?? throw new InvalidDataException("Embedded product Catalog deserialized to null.");
                return FromRows(catalog.ExternalCompatibilityBaseline);
            }
            catch (Exception ex)
            {
                return new LegacyExternalCompatibilityAuthority(null, ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static LegacyExternalCompatibilityAuthority FromRows(
            IEnumerable<LegacyExternalCompatibilityCatalogRow> sourceRows)
        {
            var result = new Dictionary<ulong, LegacyExternalCompatibilityCatalogRow>();
            foreach (LegacyExternalCompatibilityCatalogRow row in sourceRows ?? Array.Empty<LegacyExternalCompatibilityCatalogRow>())
            {
                if (row?.LegacyNativeAdmission == null)
                    continue;
                if (!ulong.TryParse(row.WorkshopId, NumberStyles.None, CultureInfo.InvariantCulture, out ulong workshopId) ||
                    workshopId == 0 ||
                    string.IsNullOrWhiteSpace(row.UniqueId) ||
                    !row.Classification.Equals("ExternalApiConsumer", StringComparison.Ordinal) ||
                    (row.ReleaseGate != "ActualLoadLanePending" && row.ReleaseGate != "ActualLoadLaneVerified") ||
                    !row.LegacyNativeAdmission.Scope.Equals(AdmissionScope, StringComparison.Ordinal) ||
                    !IsSafeRelativeEntryPath(row.LegacyNativeAdmission.EntryDllRelativePath) ||
                    !ManagedModClassifier.IsSha256ForPolicy(row.LegacyNativeAdmission.EntryDllSha256) ||
                    result.ContainsKey(workshopId))
                {
                    throw new InvalidDataException("Legacy external compatibility Catalog row is invalid or duplicated.");
                }
                result.Add(workshopId, row);
            }
            return new LegacyExternalCompatibilityAuthority(result, string.Empty);
        }

        public LegacyExternalCompatibilityMatch? Match(
            ManifestModel manifest,
            string modRoot,
            string source,
            bool nativeWorkshopSourceVerified,
            ulong? workshopId)
        {
            if (error.Length > 0 ||
                manifest == null ||
                !source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) ||
                !nativeWorkshopSourceVerified ||
                !workshopId.HasValue ||
                manifest.CodeModKindWasDeclared ||
                !rows.TryGetValue(workshopId.Value, out LegacyExternalCompatibilityCatalogRow? row))
            {
                return null;
            }

            LegacyExternalNativeAdmission admission = row.LegacyNativeAdmission
                ?? throw new InvalidDataException("legacy-external-admission-authority-invalid: Catalog admission row is missing.");
            if (!manifest.UniqueID.Equals(row.UniqueId, StringComparison.Ordinal) ||
                !NormalizeRelativePath(manifest.EntryDll).Equals(
                    NormalizeRelativePath(admission.EntryDllRelativePath),
                    StringComparison.Ordinal))
            {
                return null;
            }

            string root = Path.GetFullPath(modRoot ?? string.Empty)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string entryPath = Path.GetFullPath(Path.Combine(
                root,
                admission.EntryDllRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string containedPrefix = root + Path.DirectorySeparatorChar;
            if (!entryPath.StartsWith(containedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(entryPath))
            {
                return null;
            }

            try
            {
                string actualSha256 = ManagedModClassifier.ComputeSha256(entryPath);
                if (!actualSha256.Equals(admission.EntryDllSha256, StringComparison.OrdinalIgnoreCase))
                    return null;
                PortableAssemblyMetadata metadata = PortableAssemblyReferenceInspector.Inspect(entryPath);
                return new LegacyExternalCompatibilityMatch(entryPath, actualSha256, metadata.ModuleMvid);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is BadImageFormatException)
            {
                return null;
            }
        }

        private static bool IsSafeRelativeEntryPath(string value)
        {
            string normalized = NormalizeRelativePath(value);
            return normalized.Length > 4 &&
                !Path.IsPathRooted(value ?? string.Empty) &&
                normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                normalized.Split('/').All(segment => segment.Length > 0 && segment != "." && segment != "..");
        }

        private static string NormalizeRelativePath(string value) =>
            (value ?? string.Empty).Trim().Replace('\\', '/');
    }

    internal sealed class AdvancedReferencePolicyAuthority
    {
        private const string RegistryResourceName = "DTMAPI.Core.AdvancedReferencePolicyRegistry.json";
        private const string PolicyResourcePrefix = "DTMAPI.Core.AdvancedReferencePolicy.";
        private const string PolicyResourceSuffix = ".json";
        private static readonly string[] PolicyProperties =
        {
            "schemaVersion",
            "policyId",
            "policyVersion",
            "gameBuildId",
            "gameAssemblyRelativePath",
            "gameAssemblySha256",
            "references"
        };
        private static readonly string[] RegistryProperties = { "schemaVersion", "policies" };
        private static readonly string[] RegistryPolicyProperties =
        {
            "policyId",
            "policySha256",
            "requiredUniqueId",
            "compilerSurfaceSha256"
        };

        private AdvancedReferencePolicyAuthority(
            IReadOnlyDictionary<string, RegisteredAdvancedReferencePolicy>? policies,
            string error)
        {
            Policies = policies ?? new Dictionary<string, RegisteredAdvancedReferencePolicy>(StringComparer.Ordinal);
            Error = error ?? string.Empty;
        }

        private IReadOnlyDictionary<string, RegisteredAdvancedReferencePolicy> Policies { get; }
        private string Error { get; }

        public static AdvancedReferencePolicyAuthority Load()
        {
            try
            {
                Assembly assembly = typeof(AdvancedReferencePolicyAuthority).GetTypeInfo().Assembly;
                string registryJson;
                AdvancedReferencePolicyRegistry registry;
                using (Stream resource = assembly.GetManifestResourceStream(RegistryResourceName)
                    ?? throw new InvalidDataException("Embedded Advanced reference policy registry is unavailable."))
                using (var registryMemory = new MemoryStream())
                {
                    resource.CopyTo(registryMemory);
                    byte[] registryBytes = registryMemory.ToArray();
                    registryJson = new UTF8Encoding(false, true).GetString(registryBytes);
                    ValidateExactProperties(JsonTopLevelPropertyReader.ReadJson(registryJson), RegistryProperties);
                    foreach (IReadOnlyList<string> row in JsonTopLevelPropertyReader.ReadObjectArrayPropertyJson(registryJson, "policies"))
                        ValidateExactProperties(row, RegistryPolicyProperties);
                    registryMemory.Position = 0;
                    var registrySerializer = new DataContractJsonSerializer(typeof(AdvancedReferencePolicyRegistry));
                    registry = registrySerializer.ReadObject(registryMemory) as AdvancedReferencePolicyRegistry
                        ?? throw new InvalidDataException("Embedded Advanced reference policy registry deserialized to null.");
                }

                ValidateRegistry(registry);
                var policies = new Dictionary<string, RegisteredAdvancedReferencePolicy>(StringComparer.Ordinal);
                foreach (AdvancedReferencePolicyRegistration registration in registry.Policies)
                {
                    string resourceName = PolicyResourcePrefix + registration.PolicyId + PolicyResourceSuffix;
                    using Stream policyResource = assembly.GetManifestResourceStream(resourceName)
                        ?? throw new InvalidDataException("Embedded Advanced reference policy is unavailable: " + registration.PolicyId + ".");
                    using var policyMemory = new MemoryStream();
                    policyResource.CopyTo(policyMemory);
                    byte[] policyBytes = policyMemory.ToArray();
                    string policySha256;
                    using (SHA256 sha256 = SHA256.Create())
                        policySha256 = BitConverter.ToString(sha256.ComputeHash(policyBytes)).Replace("-", string.Empty);
                    if (!policySha256.Equals(registration.PolicySha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Embedded Advanced reference policy hash does not match its registry entry: " + registration.PolicyId + ".");

                    string policyJson = new UTF8Encoding(false, true).GetString(policyBytes);
                    ValidateExactProperties(JsonTopLevelPropertyReader.ReadJson(policyJson), PolicyProperties);
                    policyMemory.Position = 0;
                    var serializer = new DataContractJsonSerializer(typeof(AdvancedReferencePolicy));
                    var policy = serializer.ReadObject(policyMemory) as AdvancedReferencePolicy
                        ?? throw new InvalidDataException("Embedded Advanced reference policy deserialized to null.");
                    ValidatePolicy(policy, registration.PolicyId);
                    policies.Add(registration.PolicyId, new RegisteredAdvancedReferencePolicy(registration, policy, policySha256));
                }
                return new AdvancedReferencePolicyAuthority(policies, string.Empty);
            }
            catch (Exception ex)
            {
                return new AdvancedReferencePolicyAuthority(null, ex.GetType().Name + ": " + ex.Message);
            }
        }

        public bool Matches(AdvancedReferenceReceipt receipt, out string mismatch)
        {
            mismatch = string.Empty;
            if (Error.Length > 0)
            {
                mismatch = "Runtime embedded Advanced reference policy registry is invalid: " + Error;
                return false;
            }
            if (!Policies.TryGetValue(receipt.ReferencePolicyId ?? string.Empty, out RegisteredAdvancedReferencePolicy? registered))
            {
                mismatch = "Receipt referencePolicyId is not present in the Runtime-embedded exact registry.";
                return false;
            }
            AdvancedReferencePolicy policy = registered.Policy;
            if (!string.Equals(receipt.UniqueId, registered.Registration.RequiredUniqueId, StringComparison.Ordinal))
            {
                mismatch = "Receipt UniqueID does not match the Runtime-registered policy binding.";
                return false;
            }
            if (!string.Equals(receipt.ReferencePolicyId, policy.PolicyId, StringComparison.Ordinal) ||
                receipt.ReferencePolicyVersion != policy.PolicyVersion ||
                !string.Equals(receipt.ReferencePolicySha256, registered.PolicySha256, StringComparison.OrdinalIgnoreCase))
            {
                mismatch = "Receipt policy identity/hash does not match the Runtime-embedded tracked policy.";
                return false;
            }
            if (!string.Equals(receipt.GameBuildId, policy.GameBuildId, StringComparison.Ordinal) ||
                !string.Equals(receipt.GameAssemblyRelativePath, policy.GameAssemblyRelativePath, StringComparison.Ordinal) ||
                !string.Equals(receipt.GameAssemblySha256, policy.GameAssemblySha256, StringComparison.OrdinalIgnoreCase))
            {
                mismatch = "Receipt game build/assembly identity does not match the Runtime-embedded tracked policy.";
                return false;
            }

            AdvancedReferenceReceiptItem[] expected = policy.References.OrderBy(row => row.GameRelativePath, StringComparer.Ordinal).ToArray();
            AdvancedReferenceReceiptItem[] actual = (receipt.References ?? new List<AdvancedReferenceReceiptItem>())
                .OrderBy(row => row?.GameRelativePath, StringComparer.Ordinal)
                .ToArray();
            if (actual.Length != expected.Length)
            {
                mismatch = "Receipt reference count does not match the Runtime-embedded tracked policy.";
                return false;
            }
            for (int index = 0; index < expected.Length; index++)
            {
                AdvancedReferenceReceiptItem expectedRow = expected[index];
                AdvancedReferenceReceiptItem? actualRow = actual[index];
                if (actualRow == null ||
                    !string.Equals(actualRow.GameRelativePath, expectedRow.GameRelativePath, StringComparison.Ordinal) ||
                    !string.Equals(actualRow.AssemblyName, expectedRow.AssemblyName, StringComparison.Ordinal) ||
                    actualRow.Length != expectedRow.Length ||
                    !string.Equals(actualRow.Sha256, expectedRow.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    actualRow.CopyLocal != expectedRow.CopyLocal)
                {
                    mismatch = "Receipt reference row does not match the Runtime-embedded tracked policy at index " + index.ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }
            }
            return true;
        }

        public bool TryMatchTrackedPayload(string path, out string identity)
        {
            identity = string.Empty;
            if (Error.Length > 0)
                throw new InvalidDataException("Runtime embedded Advanced reference policy registry is invalid: " + Error);
            var info = new FileInfo(path);
            AdvancedReferenceReceiptItem[] possible = Policies.Values.SelectMany(value => value.Policy.References)
                .Where(row => row.Length == info.Length)
                .ToArray();
            if (possible.Length == 0)
                return false;
            string sha256;
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
                sha256 = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
            AdvancedReferenceReceiptItem? match = possible.FirstOrDefault(row => row.Sha256.Equals(sha256, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return false;
            identity = match.AssemblyName + "@" + match.GameRelativePath;
            return true;
        }

        private static void ValidateRegistry(AdvancedReferencePolicyRegistry registry)
        {
            if (registry.SchemaVersion != 1 || registry.Policies == null || registry.Policies.Count == 0)
                throw new InvalidDataException("Embedded Advanced reference policy registry header is invalid.");
            string[] ids = registry.Policies.Select(value => value.PolicyId).ToArray();
            if (!ids.SequenceEqual(ids.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length ||
                registry.Policies.Select(value => value.RequiredUniqueId).Distinct(StringComparer.Ordinal).Count() != registry.Policies.Count ||
                registry.Policies.Any(value => value == null || string.IsNullOrWhiteSpace(value.PolicyId) ||
                    !ManagedModClassifier.IsSha256ForPolicy(value.PolicySha256) ||
                    !ManagedModClassifier.IsSha256ForPolicy(value.CompilerSurfaceSha256) ||
                    string.IsNullOrWhiteSpace(value.RequiredUniqueId)))
                throw new InvalidDataException("Embedded Advanced reference policy registry rows are invalid.");
        }

        private static void ValidatePolicy(AdvancedReferencePolicy policy, string registeredPolicyId)
        {
            if (policy.SchemaVersion != 1 || string.IsNullOrWhiteSpace(policy.PolicyId) || policy.PolicyVersion <= 0 ||
                !policy.PolicyId.Equals(registeredPolicyId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(policy.GameBuildId) || string.IsNullOrWhiteSpace(policy.GameAssemblyRelativePath) ||
                !ManagedModClassifier.IsSha256ForPolicy(policy.GameAssemblySha256))
            {
                throw new InvalidDataException("Embedded Advanced reference policy header is invalid.");
            }
            if (policy.References == null || policy.References.Count == 0 ||
                policy.References.Any(row => row == null || string.IsNullOrWhiteSpace(row.GameRelativePath) || string.IsNullOrWhiteSpace(row.AssemblyName) || row.Length <= 0 || row.CopyLocal || !ManagedModClassifier.IsSha256ForPolicy(row.Sha256)) ||
                policy.References.Select(row => row.GameRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != policy.References.Count ||
                policy.References.Select(row => row.AssemblyName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != policy.References.Count)
            {
                throw new InvalidDataException("Embedded Advanced reference policy rows are invalid.");
            }
        }

        private sealed class RegisteredAdvancedReferencePolicy
        {
            public RegisteredAdvancedReferencePolicy(
                AdvancedReferencePolicyRegistration registration,
                AdvancedReferencePolicy policy,
                string policySha256)
            {
                Registration = registration;
                Policy = policy;
                PolicySha256 = policySha256;
            }

            public AdvancedReferencePolicyRegistration Registration { get; }
            public AdvancedReferencePolicy Policy { get; }
            public string PolicySha256 { get; }
        }

        private static void ValidateExactProperties(IReadOnlyList<string> names, IEnumerable<string> expectedProperties)
        {
            var expected = new HashSet<string>(expectedProperties, StringComparer.Ordinal);
            string[] unknown = names.Where(name => !expected.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
            string[] duplicate = names.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() != 1).Select(group => group.Key).ToArray();
            string[] missing = expected.Where(name => !names.Contains(name, StringComparer.Ordinal)).ToArray();
            if (unknown.Length > 0 || duplicate.Length > 0 || missing.Length > 0)
                throw new InvalidDataException("Embedded Advanced reference policy fields do not exactly match its schema.");
        }
    }

    [DataContract]
    internal sealed class AdvancedReferenceReceipt
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion { get; set; }
        [DataMember(Name = "receiptKind", IsRequired = true)] public string ReceiptKind { get; set; } = string.Empty;
        [DataMember(Name = "uniqueId", IsRequired = true)] public string UniqueId { get; set; } = string.Empty;
        [DataMember(Name = "codeModKind", IsRequired = true)] public string CodeModKind { get; set; } = string.Empty;
        [DataMember(Name = "referencePolicyId", IsRequired = true)] public string ReferencePolicyId { get; set; } = string.Empty;
        [DataMember(Name = "referencePolicyVersion", IsRequired = true)] public int ReferencePolicyVersion { get; set; }
        [DataMember(Name = "referencePolicySha256", IsRequired = true)] public string ReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "targetFramework", IsRequired = true)] public string TargetFramework { get; set; } = string.Empty;
        [DataMember(Name = "gameBuildId", IsRequired = true)] public string GameBuildId { get; set; } = string.Empty;
        [DataMember(Name = "gameAssemblyRelativePath", IsRequired = true)] public string GameAssemblyRelativePath { get; set; } = string.Empty;
        [DataMember(Name = "gameAssemblySha256", IsRequired = true)] public string GameAssemblySha256 { get; set; } = string.Empty;
        [DataMember(Name = "manifestPath", IsRequired = true)] public string ManifestPath { get; set; } = string.Empty;
        [DataMember(Name = "manifestSha256", IsRequired = true)] public string ManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "entryDllPath", IsRequired = true)] public string EntryDllPath { get; set; } = string.Empty;
        [DataMember(Name = "entryDllLength", IsRequired = true)] public long EntryDllLength { get; set; }
        [DataMember(Name = "entryDllSha256", IsRequired = true)] public string EntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "harmonyOwner", IsRequired = true)] public string HarmonyOwner { get; set; } = string.Empty;
        [DataMember(Name = "references", IsRequired = true)] public List<AdvancedReferenceReceiptItem> References { get; set; } = new List<AdvancedReferenceReceiptItem>();
    }

    [DataContract]
    internal sealed class AdvancedReferenceReceiptItem
    {
        [DataMember(Name = "gameRelativePath", IsRequired = true)] public string GameRelativePath { get; set; } = string.Empty;
        [DataMember(Name = "assemblyName", IsRequired = true)] public string AssemblyName { get; set; } = string.Empty;
        [DataMember(Name = "length", IsRequired = true)] public long Length { get; set; }
        [DataMember(Name = "sha256", IsRequired = true)] public string Sha256 { get; set; } = string.Empty;
        [DataMember(Name = "copyLocal", IsRequired = true)] public bool CopyLocal { get; set; }
    }

    internal readonly struct AdvancedReferenceVerification
    {
        public AdvancedReferenceVerification(
            string receiptPath,
            string referencePolicyId,
            int referencePolicyVersion,
            string referencePolicySha256,
            string targetFramework,
            string gameBuildId,
            string gameAssemblySha256,
            string harmonyOwner,
            IReadOnlyList<string> nativeReferenceAssemblyNames,
            string sourceFingerprint,
            string entryDllSha256,
            string entryModuleMvid)
        {
            ReceiptPath = receiptPath;
            ReferencePolicyId = referencePolicyId;
            ReferencePolicyVersion = referencePolicyVersion;
            ReferencePolicySha256 = referencePolicySha256;
            TargetFramework = targetFramework;
            GameBuildId = gameBuildId;
            GameAssemblySha256 = gameAssemblySha256;
            HarmonyOwner = harmonyOwner;
            NativeReferenceAssemblyNames = nativeReferenceAssemblyNames ?? Array.Empty<string>();
            SourceFingerprint = sourceFingerprint ?? string.Empty;
            EntryDllSha256 = entryDllSha256 ?? string.Empty;
            EntryModuleMvid = entryModuleMvid ?? string.Empty;
        }

        public string ReceiptPath { get; }
        public string ReferencePolicyId { get; }
        public int ReferencePolicyVersion { get; }
        public string ReferencePolicySha256 { get; }
        public string TargetFramework { get; }
        public string GameBuildId { get; }
        public string GameAssemblySha256 { get; }
        public string HarmonyOwner { get; }
        public IReadOnlyList<string> NativeReferenceAssemblyNames { get; }
        public string SourceFingerprint { get; }
        public string EntryDllSha256 { get; }
        public string EntryModuleMvid { get; }
    }

    internal static class JsonTopLevelPropertyReader
    {
        public static IReadOnlyList<string> Read(string path)
        {
            string json = File.ReadAllText(path, new UTF8Encoding(false, true));
            return ReadJson(json);
        }

        public static IReadOnlyList<string> ReadJson(string json)
        {
            int index = 0;
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index++] != '{')
                throw new InvalidDataException("JSON root must be an object.");

            var names = new List<string>();
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
                string name = ReadString(json, ref index);
                names.Add(name);
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index++] != ':')
                    throw new InvalidDataException("JSON property is missing ':'.");
                SkipValue(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
                throw new InvalidDataException("JSON object is missing ',' or '}'.");
            }
            SkipWhitespace(json, ref index);
            if (index != json.Length)
                throw new InvalidDataException("JSON contains trailing data.");
            return names;
        }

        public static IReadOnlyList<IReadOnlyList<string>> ReadObjectArrayProperty(string path, string propertyName)
        {
            string json = File.ReadAllText(path, new UTF8Encoding(false, true));
            return ReadObjectArrayPropertyJson(json, propertyName);
        }

        public static IReadOnlyList<IReadOnlyList<string>> ReadObjectArrayPropertyJson(string json, string propertyName)
        {
            int index = 0;
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index++] != '{')
                throw new InvalidDataException("JSON root must be an object.");
            IReadOnlyList<IReadOnlyList<string>>? result = null;
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
                string name = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index++] != ':')
                    throw new InvalidDataException("JSON property is missing ':'.");
                if (name.Equals(propertyName, StringComparison.Ordinal))
                {
                    if (result != null)
                        throw new InvalidDataException("JSON array property is duplicated: " + propertyName + ".");
                    result = ReadObjectArray(json, ref index);
                }
                else
                {
                    SkipValue(json, ref index);
                }
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
                throw new InvalidDataException("JSON object is missing ',' or '}'.");
            }
            SkipWhitespace(json, ref index);
            if (index != json.Length)
                throw new InvalidDataException("JSON contains trailing data.");
            return result ?? throw new InvalidDataException("JSON array property is missing: " + propertyName + ".");
        }

        private static IReadOnlyList<IReadOnlyList<string>> ReadObjectArray(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index++] != '[')
                throw new InvalidDataException("JSON property must be an array.");
            var rows = new List<IReadOnlyList<string>>();
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ']')
                {
                    index++;
                    return rows;
                }
                rows.Add(ReadObjectPropertyNames(json, ref index));
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == ']')
                {
                    index++;
                    return rows;
                }
                throw new InvalidDataException("JSON array is missing ',' or ']'.");
            }
        }

        private static IReadOnlyList<string> ReadObjectPropertyNames(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index++] != '{')
                throw new InvalidDataException("JSON array item must be an object.");
            var names = new List<string>();
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    return names;
                }
                names.Add(ReadString(json, ref index));
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index++] != ':')
                    throw new InvalidDataException("JSON property is missing ':'.");
                SkipValue(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    return names;
                }
                throw new InvalidDataException("JSON object is missing ',' or '}'.");
            }
        }

        private static void SkipValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
                throw new InvalidDataException("JSON value is missing.");
            if (json[index] == '"')
            {
                ReadString(json, ref index);
                return;
            }
            if (json[index] == '{' || json[index] == '[')
            {
                char open = json[index++];
                char close = open == '{' ? '}' : ']';
                int depth = 1;
                while (index < json.Length && depth > 0)
                {
                    if (json[index] == '"')
                    {
                        ReadString(json, ref index);
                        continue;
                    }
                    if (json[index] == open)
                        depth++;
                    else if (json[index] == close)
                        depth--;
                    index++;
                }
                if (depth != 0)
                    throw new InvalidDataException("JSON container is not closed.");
                return;
            }
            int start = index;
            while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']')
                index++;
            if (index == start)
                throw new InvalidDataException("JSON scalar value is missing.");
        }

        private static string ReadString(string json, ref int index)
        {
            if (index >= json.Length || json[index++] != '"')
                throw new InvalidDataException("JSON string is required.");
            var builder = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                    return builder.ToString();
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }
                if (index >= json.Length)
                    throw new InvalidDataException("JSON escape is incomplete.");
                char escaped = json[index++];
                if (escaped == 'u')
                {
                    if (index + 4 > json.Length || !int.TryParse(json.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
                        throw new InvalidDataException("JSON unicode escape is invalid.");
                    builder.Append((char)value);
                    index += 4;
                }
                else
                {
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        default: throw new InvalidDataException("JSON escape is invalid.");
                    }
                }
            }
            throw new InvalidDataException("JSON string is not closed.");
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
        }
    }
}
