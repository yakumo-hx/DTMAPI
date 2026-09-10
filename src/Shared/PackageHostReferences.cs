using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.Internal.Authoring
{
    internal static class PackageHostReferences
    {
        private static readonly HashSet<string> Keys;
        private static readonly HashSet<string> Names;
        static PackageHostReferences()
        {
            using (Stream stream = typeof(PackageHostReferences).Assembly.GetManifestResourceStream("DTMAPI.Authoring.bcl-reference-identities.json")
                ?? throw new InvalidDataException("BCL identity inventory resource missing."))
            {
                var rows = (IdentityRow[])new DataContractJsonSerializer(typeof(IdentityRow[])).ReadObject(stream)!;
                Keys = new HashSet<string>(rows.Select(row => row.Identity.Key), StringComparer.Ordinal);
                Names = new HashSet<string>(rows.Select(row => row.Name), StringComparer.OrdinalIgnoreCase);
            }
        }
        public static bool IsStrictHostReference(PackageAssemblyIdentity identity) => Keys.Contains(identity.Key)
            || (identity.Name == "DTMAPI.Abstractions" && identity.AssemblyVersion == "0.5.3.0" && identity.Culture == "" && identity.PublicKeyToken == "");
        public static bool IsReserved(string name) => Names.Contains(name)
            || new[] { "DTMAPI.Abstractions", "DTMAPI.Core", "DTMAPI.BepInExBootstrap", "DTMAPI.GameBridge.DolocTown", "DTMAPI.ModConfigMenu" }.Contains(name, StringComparer.OrdinalIgnoreCase)
            || name.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Assembly-CSharp-firstpass", StringComparison.OrdinalIgnoreCase)
            || name.Equals("0Harmony", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase);
        public static ISet<string> ReservedFor(IEnumerable<PackageAssemblyRecord> records) => new HashSet<string>(records.Select(r => r.Identity.Name).Where(IsReserved), StringComparer.OrdinalIgnoreCase);

        [DataContract]
        private sealed class IdentityRow
        {
            [DataMember(Name = "name")] public string Name { get; set; } = "";
            [DataMember(Name = "assemblyVersion")] public string Version { get; set; } = "";
            [DataMember(Name = "culture")] public string Culture { get; set; } = "";
            [DataMember(Name = "publicKeyToken")] public string Token { get; set; } = "";
            public PackageAssemblyIdentity Identity => new PackageAssemblyIdentity { Name = Name, AssemblyVersion = Version, Culture = Culture, PublicKeyToken = Token };
        }
    }
}
