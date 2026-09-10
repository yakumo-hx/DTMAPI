using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.Internal.Authoring
{
    // Linked into existing tooling/Runtime assemblies; no public API or extra Runtime DLL.
    internal sealed class AuthorApiTargetCatalog
    {
        public const string ResourceName = "DTMAPI.Authoring.target-catalog.json";
        private readonly IReadOnlyDictionary<string, AuthorApiTarget> targets;

        public static AuthorApiTargetCatalog Current { get; } = Load();
        public string DefaultTarget { get; }
        public IReadOnlyList<AuthorApiTarget> Targets { get; }

        private AuthorApiTargetCatalog(CatalogData data)
        {
            if (data.SchemaVersion != 1 || data.Targets == null || data.Targets.Count == 0)
                throw new InvalidDataException("Unsupported or empty author API target catalog.");
            var entries = new Dictionary<string, AuthorApiTarget>(StringComparer.Ordinal);
            foreach (TargetData row in data.Targets)
            {
                var target = new AuthorApiTarget(row);
                if (entries.ContainsKey(target.ApiTarget)) throw new InvalidDataException("Duplicate API target: " + target.ApiTarget);
                entries.Add(target.ApiTarget, target);
            }
            targets = entries;
            Targets = Array.AsReadOnly(entries.Values.OrderBy(value => value.ApiTarget, StringComparer.Ordinal).ToArray());
            DefaultTarget = data.DefaultTarget;
            GetAvailable(DefaultTarget);
        }

        public bool TryGet(string apiTarget, out AuthorApiTarget target) => targets.TryGetValue(apiTarget ?? string.Empty, out target!);

        public AuthorApiTarget GetAvailable(string apiTarget)
        {
            if (!TryGet(apiTarget, out AuthorApiTarget target))
                throw new InvalidDataException("Unknown API target " + apiTarget + "; select a target from the SDK target catalog.");
            if (target.State != "available")
                throw new InvalidDataException("API target " + apiTarget + " is " + target.State + "; its compatibility payload is not available for build or packaging.");
            return target;
        }

        public bool TryValidatePackageTarget(string apiTarget, string sdkVersion, string minimumRuntime, out string diagnosticCode, out string reason)
        {
            diagnosticCode = string.Empty;
            reason = string.Empty;
            if (!TryGet(apiTarget, out AuthorApiTarget target))
                return Reject("package-api-target-unknown", "Unknown package API target: " + apiTarget + ".", out diagnosticCode, out reason);
            if (target.State != "available")
                return Reject("package-api-target-unavailable", "API target " + apiTarget + " is " + target.State + "; no supported package payload has been delivered.", out diagnosticCode, out reason);
            if (!target.SdkVersions.Contains(sdkVersion, StringComparer.Ordinal))
                return Reject("package-sdk-target-mismatch", "Author SDK " + sdkVersion + " is not registered for API target " + apiTarget + ".", out diagnosticCode, out reason);
            if (!TryVersion(minimumRuntime, out Version floor) || floor < Version.Parse(target.MinimumRuntimeVersion) || floor > Version.Parse(target.MaximumPackageRuntimeVersion))
                return Reject("package-runtime-floor-invalid", "MinimumDTMApiVersion '" + minimumRuntime + "' must be within the supported package range " + target.MinimumRuntimeVersion + " through " + target.MaximumPackageRuntimeVersion + " for API target " + apiTarget + ".", out diagnosticCode, out reason);
            return true;
        }

        // The delivered SDK warned, but still issued Strict/ContentPack packages below its API floor.
        // Preserve reading those exact historical combinations; new writers use the strict method above.
        public bool TryValidateReadablePackageTarget(string apiTarget, string sdkVersion, string minimumRuntime, out string diagnosticCode, out string reason)
        {
            if (TryValidatePackageTarget(apiTarget, sdkVersion, minimumRuntime, out diagnosticCode, out reason)) return true;
            if (apiTarget == "0.5.5" && sdkVersion == "0.1.0" && TryGet(apiTarget, out AuthorApiTarget target) &&
                target.State == "available" && TryVersion(minimumRuntime, out Version floor) && floor < Version.Parse(target.MinimumRuntimeVersion))
            {
                diagnosticCode = string.Empty;
                reason = string.Empty;
                return true;
            }
            return false;
        }

        private static bool Reject(string code, string message, out string diagnosticCode, out string reason)
        { diagnosticCode = code; reason = message; return false; }

        internal static bool TryVersion(string value, out Version version)
        {
            version = new Version(0, 0, 0);
            string[] parts = (value ?? string.Empty).Split('.');
            return parts.Length == 3 && parts.All(part => part.Length > 0 && part.All(c => c >= '0' && c <= '9')) && Version.TryParse(value, out version!);
        }

        private static AuthorApiTargetCatalog Load()
        {
            using (Stream stream = typeof(AuthorApiTargetCatalog).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("The embedded author API target catalog is missing."))
            {
                return new AuthorApiTargetCatalog((CatalogData)(new DataContractJsonSerializer(typeof(CatalogData)).ReadObject(stream)
                    ?? throw new InvalidDataException("The embedded author API target catalog is empty.")));
            }
        }

        [DataContract]
        private sealed class CatalogData
        {
            [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion { get; set; }
            [DataMember(Name = "defaultTarget", IsRequired = true)] public string DefaultTarget { get; set; } = string.Empty;
            [DataMember(Name = "targets", IsRequired = true)] public List<TargetData> Targets { get; set; } = new List<TargetData>();
        }

        [DataContract]
        internal sealed class TargetData
        {
            [DataMember(Name = "apiTarget", IsRequired = true)] public string ApiTarget { get; set; } = string.Empty;
            [DataMember(Name = "state", IsRequired = true)] public string State { get; set; } = string.Empty;
            [DataMember(Name = "minimumRuntimeVersion", IsRequired = true)] public string MinimumRuntimeVersion { get; set; } = string.Empty;
            [DataMember(Name = "maximumPackageRuntimeVersion", IsRequired = true)] public string MaximumPackageRuntimeVersion { get; set; } = string.Empty;
            [DataMember(Name = "sdkVersions", IsRequired = true)] public string[] SdkVersions { get; set; } = Array.Empty<string>();
            [DataMember(Name = "payloadSdkVersion", IsRequired = true)] public string PayloadSdkVersion { get; set; } = string.Empty;
            [DataMember(Name = "targetFramework", IsRequired = true)] public string TargetFramework { get; set; } = string.Empty;
            [DataMember(Name = "payloadPath", IsRequired = true)] public string PayloadPath { get; set; } = string.Empty;
            [DataMember(Name = "contractPath", IsRequired = true)] public string ContractPath { get; set; } = string.Empty;
            [DataMember(Name = "contractResourceName", IsRequired = true)] public string ContractResourceName { get; set; } = string.Empty;
            [DataMember(Name = "contractSha256", IsRequired = true)] public string ContractSha256 { get; set; } = string.Empty;
            [DataMember(Name = "capabilities", IsRequired = true)] public string[] Capabilities { get; set; } = Array.Empty<string>();
        }
    }

    internal sealed class AuthorApiTarget
    {
        public string ApiTarget { get; }
        public string State { get; }
        public string MinimumRuntimeVersion { get; }
        public string MaximumPackageRuntimeVersion { get; }
        public IReadOnlyList<string> SdkVersions { get; }
        public string PayloadSdkVersion { get; }
        public string TargetFramework { get; }
        public string PayloadPath { get; }
        public string ContractPath { get; }
        public string ContractResourceName { get; }
        public string ContractSha256 { get; }
        public IReadOnlyList<string> Capabilities { get; }

        internal AuthorApiTarget(AuthorApiTargetCatalog.TargetData row)
        {
            if (!AuthorApiTargetCatalog.TryVersion(row.ApiTarget, out _) ||
                !AuthorApiTargetCatalog.TryVersion(row.MinimumRuntimeVersion, out Version minimum) ||
                !AuthorApiTargetCatalog.TryVersion(row.MaximumPackageRuntimeVersion, out Version maximum) || maximum < minimum ||
                (row.State != "available" && row.State != "planned") || row.TargetFramework != "netstandard2.0" ||
                row.SdkVersions == null || row.SdkVersions.Length == 0 || row.SdkVersions.Any(value => !AuthorApiTargetCatalog.TryVersion(value, out _)) ||
                row.SdkVersions.Distinct(StringComparer.Ordinal).Count() != row.SdkVersions.Length ||
                !AuthorApiTargetCatalog.TryVersion(row.PayloadSdkVersion, out _) || !SafePath(row.PayloadPath) || !SafePath(row.ContractPath) ||
                !row.ContractPath.StartsWith(row.PayloadPath + "/", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(row.ContractResourceName) ||
                row.Capabilities == null || row.Capabilities.Any(string.IsNullOrWhiteSpace) || row.Capabilities.Distinct(StringComparer.Ordinal).Count() != row.Capabilities.Length ||
                (row.State == "available" && (row.ContractSha256 == null || row.ContractSha256.Length != 64 || !row.ContractSha256.All(Uri.IsHexDigit))) ||
                (row.State == "planned" && row.ContractSha256 != string.Empty))
                throw new InvalidDataException("Invalid author API target catalog row: " + row.ApiTarget + ".");
            ApiTarget = row.ApiTarget; State = row.State; MinimumRuntimeVersion = row.MinimumRuntimeVersion;
            MaximumPackageRuntimeVersion = row.MaximumPackageRuntimeVersion; SdkVersions = Array.AsReadOnly((string[])row.SdkVersions.Clone());
            PayloadSdkVersion = row.PayloadSdkVersion; TargetFramework = row.TargetFramework; PayloadPath = row.PayloadPath;
            ContractPath = row.ContractPath; ContractResourceName = row.ContractResourceName; ContractSha256 = row.ContractSha256;
            Capabilities = Array.AsReadOnly((string[])row.Capabilities.Clone());
        }

        private static bool SafePath(string path) => !string.IsNullOrWhiteSpace(path) && !path.Contains("\\") && !path.Contains(":") &&
            path.Split('/').All(part => part.Length > 0 && part != "." && part != "..");
    }
}
