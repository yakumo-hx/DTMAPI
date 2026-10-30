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
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Manifesting
{
    public enum ManagedModIdentity
    {
        CodeMod = 4,
        StrictCodeMod = 0,
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
        public string IdentityName => Identity == ManagedModIdentity.CodeMod ? "CodeMod" : Identity == ManagedModIdentity.StrictCodeMod
            ? "Strict CodeMod"
            : Identity == ManagedModIdentity.LegacyNativeCodeMod
                ? "Third-party native compatibility CodeMod"
                : Identity == ManagedModIdentity.AdvancedCodeMod
                    ? "Advanced CodeMod"
                    : "ContentPack";
        public string DeclaredKind { get; }
        public string EffectiveKind => Identity == ManagedModIdentity.CodeMod ? "CodeMod" : Identity == ManagedModIdentity.AdvancedCodeMod
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
        public bool IsCodeMod => Identity == ManagedModIdentity.CodeMod ||
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
        public ManagedModClassifier(string gamePath) { }
        internal static string GetExpectedHarmonyOwner(string id) => "dtmapi.mod." + id.ToLowerInvariant();
        internal static ManagedModClassification ClassifyDeclaredIdentity(ManifestModel manifest)
        {
            if (manifest.Type != "CodeMod" && manifest.Type != "ContentPack")
                throw new ManagedModClassificationException("manifest-type-invalid", manifest.Type);
            bool code = manifest.Type == "CodeMod";
            return new ManagedModClassification(code ? ManagedModIdentity.CodeMod : ManagedModIdentity.ContentPack,
                manifest.CodeModKind, "runtime-declaration", "official-source", code ? "author-code" : "declarative-content",
                "actual-runtime-binding", code ? "restart-required-after-load" : "in-process-owner-lifecycle",
                code ? (string.IsNullOrWhiteSpace(manifest.HarmonyOwner)
                    ? (manifest.ManifestVersion == 2 || manifest.NativeContractSelected ? manifest.UniqueID + ".Native" : GetExpectedHarmonyOwner(manifest.UniqueID))
                    : manifest.HarmonyOwner) : "",
                false, "", "", 0, "", "", "");
        }
        public ManagedModClassification Classify(ManifestModel manifest, string modRoot, string manifestPath,
            string source, bool nativeWorkshopSourceVerified, ulong? workshopId, StartupPackageReadTransaction? packageReads = null)
        {
            var declaration = ClassifyDeclaredIdentity(manifest);
            if (!source.StartsWith("Local", StringComparison.OrdinalIgnoreCase) &&
                !(source.StartsWith("Workshop", StringComparison.OrdinalIgnoreCase) && nativeWorkshopSourceVerified))
                throw new ManagedModClassificationException("managed-source-invalid", source);
            if (!declaration.IsCodeMod)
            {
                if (!string.IsNullOrWhiteSpace(manifest.EntryDll) || !string.IsNullOrWhiteSpace(manifest.EntryType))
                    throw new ManagedModClassificationException("content-pack-with-entry", manifest.UniqueID);
                return declaration;
            }
            if (string.IsNullOrWhiteSpace(manifest.EntryDll)) throw new ManagedModClassificationException("entry-dll-missing", manifest.UniqueID);
            return new ManagedModClassification(ManagedModIdentity.CodeMod, manifest.CodeModKind, "runtime-declaration",
                source, "author-code", "actual-runtime-binding", "restart-required-after-load", declaration.ExpectedHarmonyOwner,
                false, "", "", 0, "", "", "");
        }
    }
}
