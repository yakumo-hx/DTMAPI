using System;
using System.IO;

namespace DTMAPI.Core.Manifesting
{
    public enum ManagedModIdentity
    {
        ContentPack = 3,
        CodeMod = 4
    }

    public sealed class ManagedModClassification
    {
        internal ManagedModClassification(ManagedModIdentity identity, string expectedHarmonyOwner)
        {
            Identity = identity;
            ExpectedHarmonyOwner = expectedHarmonyOwner;
        }

        public ManagedModIdentity Identity { get; }
        public string IdentityName => IsCodeMod ? "CodeMod" : "ContentPack";
        public string ExpectedHarmonyOwner { get; }
        public bool IsCodeMod => Identity == ManagedModIdentity.CodeMod;
        public bool IsContentPack => Identity == ManagedModIdentity.ContentPack;
    }

    internal sealed class ManagedModClassificationException : IOException
    {
        public ManagedModClassificationException(string code, string message)
            : base(code + ": " + message)
        {
            Code = code;
        }

        public string Code { get; }
    }

    internal sealed class ManagedModClassifier
    {
        internal static string GetExpectedHarmonyOwner(string id) => "dtmapi.mod." + id.ToLowerInvariant();

        internal static ManagedModClassification ClassifyDeclaredIdentity(ManifestModel manifest)
        {
            if (manifest.Type != "CodeMod" && manifest.Type != "ContentPack")
                throw new ManagedModClassificationException("manifest-type-invalid", manifest.Type);
            bool code = manifest.Type == "CodeMod";
            string harmonyOwner = code
                ? string.IsNullOrWhiteSpace(manifest.HarmonyOwner)
                    ? manifest.ManifestVersion == 2 || manifest.NativeContractSelected
                        ? manifest.UniqueID + ".Native"
                        : GetExpectedHarmonyOwner(manifest.UniqueID)
                    : manifest.HarmonyOwner
                : string.Empty;
            return new ManagedModClassification(code ? ManagedModIdentity.CodeMod : ManagedModIdentity.ContentPack, harmonyOwner);
        }

        public ManagedModClassification Classify(ManifestModel manifest, string source, bool nativeWorkshopSourceVerified)
        {
            ManagedModClassification declaration = ClassifyDeclaredIdentity(manifest);
            if (!source.StartsWith("Local", StringComparison.OrdinalIgnoreCase) &&
                !(source.StartsWith("Workshop", StringComparison.OrdinalIgnoreCase) && nativeWorkshopSourceVerified))
                throw new ManagedModClassificationException("managed-source-invalid", source);
            if (!declaration.IsCodeMod)
            {
                if (!string.IsNullOrWhiteSpace(manifest.EntryDll) || !string.IsNullOrWhiteSpace(manifest.EntryType))
                    throw new ManagedModClassificationException("content-pack-with-entry", manifest.UniqueID);
            }
            else if (string.IsNullOrWhiteSpace(manifest.EntryDll))
                throw new ManagedModClassificationException("entry-dll-missing", manifest.UniqueID);
            return declaration;
        }
    }
}
