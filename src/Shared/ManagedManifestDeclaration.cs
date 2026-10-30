using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.Internal.Authoring
{
    internal enum DeclaredManagedIdentity { Unknown, LegacyNativeCodeMod, StrictCodeMod, AdvancedCodeMod, ContentPack }

    /// <summary>Declared identity only. Placement, receipts and assembly verification remain separate checks.</summary>
    internal sealed class ManagedManifestDeclaration
    {
        private ManagedManifestDeclaration(DeclaredManagedIdentity identity, string kind, string provenance, DeclarationError[] errors)
        {
            Identity = identity;
            DeclaredKind = kind;
            Provenance = provenance;
            Errors = errors;
        }

        internal DeclaredManagedIdentity Identity { get; }
        internal string DeclaredKind { get; }
        internal string Provenance { get; }
        internal IReadOnlyList<DeclarationError> Errors { get; }

        internal static ManagedManifestDeclaration Read(bool hasType, string type, bool hasKind, string kind, string entryDll, string entryType)
        {
            string effectiveType = hasType ? type : "CodeMod";
            if (effectiveType != "CodeMod" && effectiveType != "ContentPack")
                return Invalid("unknown-manifest-type", "Type must be exactly CodeMod or ContentPack; received '" + effectiveType + "'.");

            if (effectiveType == "ContentPack")
            {
                var errors = new List<DeclarationError>();
                if (hasKind)
                    errors.Add(new DeclarationError("code-mod-kind-on-content-pack", "ContentPack cannot declare CodeModKind."));
                if (!string.IsNullOrWhiteSpace(entryDll) || !string.IsNullOrWhiteSpace(entryType))
                    errors.Add(new DeclarationError("content-pack-with-code-fields", "ContentPack cannot declare EntryDll or EntryType."));
                return new ManagedManifestDeclaration(DeclaredManagedIdentity.ContentPack, "ContentPack", "declared-content-pack", errors.ToArray());
            }

            if (!hasKind)
                return Valid(DeclaredManagedIdentity.LegacyNativeCodeMod, "omitted", "legacy-native-compatibility-unverified");
            if (!hasType)
                return Invalid("code-mod-kind-requires-code-mod-type", "CodeModKind requires an explicit Type=CodeMod declaration.");
            if (kind == "Strict")
                return Valid(DeclaredManagedIdentity.StrictCodeMod, kind, "declared-strict-unverified");
            if (kind == "Advanced")
                return Valid(DeclaredManagedIdentity.AdvancedCodeMod, kind, "declared-advanced-receipt-pending");
            return Invalid("unknown-code-mod-kind", "CodeModKind must be exactly Strict or Advanced; received '" + kind + "'.");
        }

        internal static void ValidateWireFields(IEnumerable<string> properties)
        {
            string[] names = properties.ToArray();
            foreach (string canonical in new[] { "Type", "CodeModKind" })
            {
                string[] matches = names.Where(name => name.Equals(canonical, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length > 0 && (matches.Length != 1 || matches[0] != canonical))
                    throw new InvalidDataException("manifest field " + canonical + " is case-sensitive and may appear exactly once; received " + string.Join(", ", matches) + ".");
            }
        }

        private static ManagedManifestDeclaration Valid(DeclaredManagedIdentity identity, string kind, string provenance)
            => new ManagedManifestDeclaration(identity, kind, provenance, Array.Empty<DeclarationError>());

        private static ManagedManifestDeclaration Invalid(string code, string message)
            => new ManagedManifestDeclaration(DeclaredManagedIdentity.Unknown, string.Empty, string.Empty, new[] { new DeclarationError(code, message) });

        internal sealed class DeclarationError
        {
            internal DeclarationError(string code, string message) { Code = code; Message = message; }
            internal string Code { get; }
            internal string Message { get; }
        }
    }
}
