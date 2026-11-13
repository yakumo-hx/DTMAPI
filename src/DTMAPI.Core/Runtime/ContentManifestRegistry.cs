using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{
    internal sealed class ContentManifestRegistry
    {
        private static readonly string[] OfficialJsonFiles =
        {
            "animal_tbanimal.json",
            "animal_tbanimaldocument.json",
            "item_tbitem.json",
            "item_tbitemspawn.json",
            "mod_tbmodstoreextension.json",
            "husbandry_tbhusbandry.json",
            "husbandry_tbhusbandryenergy.json"
        };

        public ContentManifestRegistrySnapshot Build(
            IReadOnlyList<DiscoveredMod> discoveredMods,
            IReadOnlyList<DiscoveredMod> loadedMods,
            IReadOnlyList<string> scannerErrors,
            IReadOnlyList<string> scannerWarnings,
            string reason,
            IReadOnlyList<IManifest>? registeredManifests = null)
        {
            DiscoveredMod[] discovered = discoveredMods.ToArray();
            DiscoveredMod[] loaded = loadedMods.ToArray();
            var loadedById = loaded
                .Where(m => !string.IsNullOrWhiteSpace(m.Manifest.UniqueID))
                .GroupBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var registeredById = (registeredManifests ?? Array.Empty<IManifest>())
                .Where(m => !string.IsNullOrWhiteSpace(m.UniqueID))
                .GroupBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var discoveredById = discovered
                .Where(m => !string.IsNullOrWhiteSpace(m.Manifest.UniqueID))
                .GroupBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var rows = new List<ContentManifestRegistryRow>();
            var diagnostics = new List<ContentManifestRegistryDiagnostic>();
            var diffs = new List<string>();

            IReadOnlyList<string> errorSamples = scannerErrors;
            IReadOnlyList<string> warningSamples = scannerWarnings;
            foreach (string error in errorSamples)
                diagnostics.Add(ContentManifestRegistryDiagnostic.Error("bad-manifest", "DTMAPI.ModScanner", error));

            foreach (string warning in warningSamples)
            {
                bool duplicateUniqueId = IsDuplicateUniqueIdWarning(warning);
                string kind = duplicateUniqueId
                    ? "duplicate-unique-id"
                    : "scanner-warning";
                diagnostics.Add(ContentManifestRegistryDiagnostic.Warning(kind, "DTMAPI.ModScanner", warning));
            }

            foreach (DiscoveredMod mod in discovered.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                bool loadedByOldSystem = loadedById.ContainsKey(mod.Manifest.UniqueID);
                var dependencyRows = BuildDependencyRows(mod, loadedById, registeredById, discoveredById);
                var capabilities = InferCapabilities(mod);
                var row = new ContentManifestRegistryRow(
                    mod.Manifest.UniqueID,
                    mod.Manifest.Name,
                    mod.Manifest.Version,
                    mod.Manifest.Type,
                    mod.Manifest.EntryDll,
                    mod.Manifest.EntryType,
                    mod.Manifest.MinimumDTMApiVersion,
                    mod.Manifest.MinimumGameVersion,
                    mod.Source,
                    mod.OfficialId,
                    mod.OfficialEnabled,
                    mod.OfficialEnablementManaged,
                    mod.CanDtmApiToggle,
                    loadedByOldSystem,
                    mod.ManifestPath,
                    mod.RootPath,
                    mod.Manifest.UniqueID,
                    capabilities,
                    dependencyRows);
                rows.Add(row);

                if (!IsDtmApiVersionCompatible(mod.Manifest.MinimumDTMApiVersion, out string apiReason))
                    diagnostics.Add(ContentManifestRegistryDiagnostic.Error(
                        "api-too-new",
                        mod.Manifest.UniqueID,
                        "MinimumDTMApiVersion=" + mod.Manifest.MinimumDTMApiVersion + "; runtime=" + DtmApiRuntime.ApiVersion + ". " + apiReason));

                foreach (ContentManifestDependencyRow dependency in dependencyRows)
                {
                    if (dependency.Severity.Equals("error", StringComparison.OrdinalIgnoreCase))
                    {
                        diagnostics.Add(ContentManifestRegistryDiagnostic.Error(
                            "dependency",
                            mod.Manifest.UniqueID,
                            dependency.UniqueID + " status=" + dependency.Status + "; required=" + dependency.Required + "; minimum=" + dependency.MinimumVersion + "; " + dependency.Details));
                    }
                    else if (dependency.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                    {
                        diagnostics.Add(ContentManifestRegistryDiagnostic.Warning(
                            "dependency",
                            mod.Manifest.UniqueID,
                            dependency.UniqueID + " status=" + dependency.Status + "; required=" + dependency.Required + "; minimum=" + dependency.MinimumVersion + "; " + dependency.Details));
                    }
                }
            }

            foreach (DiscoveredMod loadedMod in loaded)
            {
                if (!discoveredById.ContainsKey(loadedMod.Manifest.UniqueID))
                    diffs.Add("loaded-missing-from-index:" + loadedMod.Manifest.UniqueID);
            }

            HashSet<string> indexedContentPacks = new HashSet<string>(
                rows.Where(r => r.IsContentPack).Select(r => r.UniqueID),
                StringComparer.OrdinalIgnoreCase);
            foreach (DiscoveredMod legacyContentPack in discovered.Where(IsContentPack))
            {
                if (!indexedContentPacks.Contains(legacyContentPack.Manifest.UniqueID))
                    diffs.Add("legacy-content-pack-missing:" + legacyContentPack.Manifest.UniqueID);
            }

            return new ContentManifestRegistrySnapshot(reason, DateTimeOffset.Now, rows, diagnostics, diffs);
        }

        private static bool IsDuplicateUniqueIdWarning(string warning)
        {
            return warning.IndexOf("Duplicate UniqueID", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<ContentManifestDependencyRow> BuildDependencyRows(
            DiscoveredMod mod,
            IReadOnlyDictionary<string, DiscoveredMod> loadedById,
            IReadOnlyDictionary<string, IManifest> registeredById,
            IReadOnlyDictionary<string, DiscoveredMod> discoveredById)
        {
            var rows = new List<ContentManifestDependencyRow>();
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                string id = dependency.UniqueID;
                string minimum = dependency.MinimumVersion;
                if (string.IsNullOrWhiteSpace(id))
                {
                    rows.Add(new ContentManifestDependencyRow(id, dependency.Required, minimum, "empty-unique-id", "error", "Dependency UniqueID is empty."));
                    continue;
                }

                IManifest? resolvedDependency = null;
                string resolvedSource = string.Empty;
                if (registeredById.TryGetValue(id, out IManifest registeredDependency))
                {
                    resolvedDependency = registeredDependency;
                    resolvedSource = "registeredManifest";
                }
                else if (loadedById.TryGetValue(id, out DiscoveredMod loadedDependency))
                {
                    resolvedDependency = loadedDependency.Manifest;
                    resolvedSource = "legacyLoaded";
                }

                if (resolvedDependency == null)
                {
                    bool discovered = discoveredById.ContainsKey(id);
                    string status = dependency.Required
                        ? (discovered ? "required-not-loaded" : "required-missing")
                        : (discovered ? "optional-not-loaded" : "optional-missing");
                    string severity = dependency.Required ? "error" : "ok";
                    string details = discovered
                        ? "Dependency was discovered by the legacy scanner but is not loaded by the legacy loader."
                        : "Dependency was not discovered by the legacy scanner.";
                    rows.Add(new ContentManifestDependencyRow(id, dependency.Required, minimum, status, severity, details));
                    continue;
                }

                if (!IsVersionCompatible(minimum, resolvedDependency.Version, out string reason))
                {
                    rows.Add(new ContentManifestDependencyRow(
                        id,
                        dependency.Required,
                        minimum,
                        dependency.Required ? "required-version-too-low" : "optional-version-too-low",
                        dependency.Required ? "error" : "warning",
                        "loadedVersion=" + resolvedDependency.Version + "; source=" + resolvedSource + ". " + reason));
                    continue;
                }

                rows.Add(new ContentManifestDependencyRow(id, dependency.Required, minimum, "satisfied", "ok", "loadedVersion=" + resolvedDependency.Version + "; source=" + resolvedSource));
            }

            return rows;
        }

        private static IReadOnlyList<string> InferCapabilities(DiscoveredMod mod)
        {
            var capabilities = new List<string>();
            if (mod.Classification.IsCodeMod)
                capabilities.Add("CodeMod");
            if (IsContentPack(mod))
                capabilities.Add("ContentPack");
            if (mod.OfficialEnablementManaged)
                capabilities.Add("OfficialEnablement");
            if (mod.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                capabilities.Add("Workshop");
            if (mod.Source.Equals("OfficialLocal", StringComparison.OrdinalIgnoreCase))
                capabilities.Add("OfficialLocal");
            if (mod.Source.Equals("Local", StringComparison.OrdinalIgnoreCase))
                capabilities.Add("Local");

            string dtmapiContentRoot = Path.Combine(mod.RootPath, "Content", "DTMAPI");
            if (Directory.Exists(dtmapiContentRoot))
                capabilities.Add("DTMAPIContent");

            string contentRoot = Path.Combine(mod.RootPath, "Content");
            if (OfficialJsonFiles.Any(file => File.Exists(Path.Combine(contentRoot, file))))
                capabilities.Add("OfficialJson");

            return capabilities.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static bool IsContentPack(DiscoveredMod mod)
        {
            return mod.Classification.IsContentPack;
        }

        private static bool IsDtmApiVersionCompatible(string minimumVersion, out string reason)
        {
            return IsVersionCompatible(minimumVersion, DtmApiRuntime.ApiVersion, out reason);
        }

        private static bool IsVersionCompatible(string minimumVersion, string actualVersion, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(minimumVersion))
                return true;

            if (!TryParseVersion(minimumVersion, out Version minimum))
            {
                reason = "Cannot parse required version.";
                return false;
            }

            if (!TryParseVersion(actualVersion, out Version actual))
            {
                reason = "Cannot parse actual version.";
                return false;
            }

            bool satisfied = CompareVersions(actual, minimum) >= 0;
            if (!satisfied)
                reason = "Actual version is older than the required minimum.";
            return satisfied;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = new Version(0, 0, 0, 0);
            string text = (value).Trim();
            int suffixIndex = text.IndexOfAny(new[] { '-', '+' });
            if (suffixIndex >= 0)
                text = text.Substring(0, suffixIndex);
            return Version.TryParse(text, out version);
        }

        private static int CompareVersions(Version actual, Version minimum)
        {
            int[] left = { actual.Major, actual.Minor, Math.Max(actual.Build, 0), Math.Max(actual.Revision, 0) };
            int[] right = { minimum.Major, minimum.Minor, Math.Max(minimum.Build, 0), Math.Max(minimum.Revision, 0) };
            for (int i = 0; i < left.Length; i++)
            {
                int comparison = left[i].CompareTo(right[i]);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }

    internal sealed class ContentManifestRegistrySnapshot
    {
        public ContentManifestRegistrySnapshot(
            string reason,
            DateTimeOffset capturedAt,
            IReadOnlyList<ContentManifestRegistryRow> rows,
            IReadOnlyList<ContentManifestRegistryDiagnostic> diagnostics,
            IReadOnlyList<string> diffs)
        {
            Reason = reason;
            CapturedAt = capturedAt;
            Rows = rows;
            Diagnostics = diagnostics;
            Diffs = diffs;
        }

        public string Reason { get; }
        public DateTimeOffset CapturedAt { get; }
        public IReadOnlyList<ContentManifestRegistryRow> Rows { get; }
        public IReadOnlyList<ContentManifestRegistryDiagnostic> Diagnostics { get; }
        public IReadOnlyList<string> Diffs { get; }
        public int DiagnosticCount => Diagnostics.Count;
        public int DiffCount => Diffs.Count;
        public int BadManifestCount => Diagnostics.Count(diagnostic => diagnostic.Kind == "bad-manifest");
        public int DuplicateUniqueIdCount => Diagnostics.Count(diagnostic => diagnostic.Kind == "duplicate-unique-id");
        public int ManifestDiagnosticCount => BadManifestCount + DuplicateUniqueIdCount;
        public int DependencyErrorCount => Diagnostics.Count(diagnostic => diagnostic.Kind == "dependency" && diagnostic.Severity == "error");
        public int DependencyWarningCount => Diagnostics.Count(diagnostic => diagnostic.Kind == "dependency" && diagnostic.Severity == "warning");
        public int ApiTooNewCount => Diagnostics.Count(diagnostic => diagnostic.Kind == "api-too-new");
        public int ContentPackCount => Rows.Count(r => r.IsContentPack);
        public int LoadedContentPackCount => Rows.Count(r => r.IsContentPack && r.LoadedByOldSystem);

        public string FormatSummary()
        {
            return "reason=" + Reason +
                "; rows=" + Rows.Count +
                "; loadedRows=" + Rows.Count(r => r.LoadedByOldSystem) +
                "; contentPacks=" + ContentPackCount +
                "; loadedContentPacks=" + LoadedContentPackCount +
                "; codeMods=" + Rows.Count(r => r.Capabilities.Contains("CodeMod", StringComparer.OrdinalIgnoreCase)) +
                "; dependencyErrors=" + DependencyErrorCount +
                "; dependencyWarnings=" + DependencyWarningCount +
                "; apiTooNew=" + ApiTooNewCount +
                "; manifestDiagnostics=" + ManifestDiagnosticCount +
                "; diagnostics=" + DiagnosticCount +
                "; diffs=" + DiffCount +
                "; firstDiffs=" + FormatFirstDiffs();
        }

        public string FormatManifestSummary()
        {
            string duplicates = string.Join("|", Diagnostics
                .Where(d => d.Kind.Equals("duplicate-unique-id", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Details)
                .ToArray());
            string bad = string.Join("|", Diagnostics
                .Where(d => d.Kind.Equals("bad-manifest", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Details)
                .ToArray());
            return "rows=" + Rows.Count +
                "; badManifests=" + BadManifestCount +
                "; duplicateUniqueIds=" + DuplicateUniqueIdCount +
                "; firstBad=" + (string.IsNullOrWhiteSpace(bad) ? "none" : bad) +
                "; firstDuplicates=" + (string.IsNullOrWhiteSpace(duplicates) ? "none" : duplicates);
        }

        public string FormatDependencySummary()
        {
            return "dependencyErrors=" + DependencyErrorCount +
                "; dependencyWarnings=" + DependencyWarningCount +
                "; apiTooNew=" + ApiTooNewCount +
                "; rowsWithDependencies=" + Rows.Count(r => r.Dependencies.Count > 0) +
                "; firstDiagnostics=" + FormatFirstDiagnostics("dependency", "api-too-new");
        }

        public string FormatOwnershipSummary()
        {
            return "contentPacks=" + ContentPackCount +
                "; loadedContentPacks=" + LoadedContentPackCount +
                "; officialManaged=" + Rows.Count(r => r.OfficialEnablementManaged) +
                "; dtmapiToggleable=" + Rows.Count(r => r.CanDtmApiToggle) +
                "; ownerlessRows=" + Rows.Count(r => string.IsNullOrWhiteSpace(r.OwnerId)) +
                "; capabilities=" + FormatCapabilityDistribution();
        }

        public string FormatDiffSummary()
        {
            return "diffs=" + DiffCount +
                "; firstDiffs=" + FormatFirstDiffs();
        }

        private string FormatFirstDiagnostics(params string[] kinds)
        {
            HashSet<string> requested = new HashSet<string>(kinds, StringComparer.OrdinalIgnoreCase);
            string text = string.Join(" || ", Diagnostics
                .Where(d => requested.Contains(d.Kind))
                .Select(d => d.Kind + ":" + d.OwnerId + ":" + d.Details)
                .ToArray());
            return string.IsNullOrWhiteSpace(text) ? "none" : text;
        }

        private string FormatFirstDiffs()
        {
            if (Diffs.Count == 0)
                return "none";
            return string.Join(" || ", Diffs.ToArray());
        }

        private string FormatCapabilityDistribution()
        {
            string text = string.Join(",", Rows
                .SelectMany(r => r.Capabilities)
                .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Key + ":" + g.Count())
                .ToArray());
            return string.IsNullOrWhiteSpace(text) ? "none" : text;
        }
    }

    internal sealed class ContentManifestRegistryRow
    {
        public ContentManifestRegistryRow(
            string uniqueId,
            string name,
            string version,
            string type,
            string entryDll,
            string entryType,
            string minimumDtmApiVersion,
            string minimumGameVersion,
            string source,
            string officialId,
            bool officialEnabled,
            bool officialEnablementManaged,
            bool canDtmApiToggle,
            bool loadedByOldSystem,
            string manifestPath,
            string rootPath,
            string ownerId,
            IReadOnlyList<string> capabilities,
            IReadOnlyList<ContentManifestDependencyRow> dependencies)
        {
            UniqueID = uniqueId;
            Name = name;
            Version = version;
            Type = type;
            EntryDll = entryDll;
            EntryType = entryType;
            MinimumDTMApiVersion = minimumDtmApiVersion;
            MinimumGameVersion = minimumGameVersion;
            Source = source;
            OfficialId = officialId;
            OfficialEnabled = officialEnabled;
            OfficialEnablementManaged = officialEnablementManaged;
            CanDtmApiToggle = canDtmApiToggle;
            LoadedByOldSystem = loadedByOldSystem;
            ManifestPath = manifestPath;
            RootPath = rootPath;
            OwnerId = ownerId;
            Capabilities = capabilities;
            Dependencies = dependencies;
        }

        public string UniqueID { get; }
        public string Name { get; }
        public string Version { get; }
        public string Type { get; }
        public string EntryDll { get; }
        public string EntryType { get; }
        public string MinimumDTMApiVersion { get; }
        public string MinimumGameVersion { get; }
        public string Source { get; }
        public string OfficialId { get; }
        public bool OfficialEnabled { get; }
        public bool OfficialEnablementManaged { get; }
        public bool CanDtmApiToggle { get; }
        public bool LoadedByOldSystem { get; }
        public string ManifestPath { get; }
        public string RootPath { get; }
        public string OwnerId { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public IReadOnlyList<ContentManifestDependencyRow> Dependencies { get; }
        public bool IsContentPack => Capabilities.Contains("ContentPack", StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class ContentManifestDependencyRow
    {
        public ContentManifestDependencyRow(string uniqueId, bool required, string minimumVersion, string status, string severity, string details)
        {
            UniqueID = uniqueId;
            Required = required;
            MinimumVersion = minimumVersion;
            Status = status;
            Severity = severity;
            Details = details;
        }

        public string UniqueID { get; }
        public bool Required { get; }
        public string MinimumVersion { get; }
        public string Status { get; }
        public string Severity { get; }
        public string Details { get; }
    }

    internal sealed class ContentManifestRegistryDiagnostic
    {
        private ContentManifestRegistryDiagnostic(string severity, string kind, string ownerId, string details)
        {
            Severity = severity;
            Kind = kind;
            OwnerId = ownerId;
            Details = details;
        }

        public string Severity { get; }
        public string Kind { get; }
        public string OwnerId { get; }
        public string Details { get; }

        public static ContentManifestRegistryDiagnostic Error(string kind, string ownerId, string details) => new ContentManifestRegistryDiagnostic("error", kind, ownerId, details);

        public static ContentManifestRegistryDiagnostic Warning(string kind, string ownerId, string details) => new ContentManifestRegistryDiagnostic("warning", kind, ownerId, details);
    }
}
