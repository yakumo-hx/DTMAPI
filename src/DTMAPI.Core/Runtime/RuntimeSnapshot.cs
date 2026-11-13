using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{
    public sealed class RuntimeSnapshot
    {
        public RuntimeSnapshot(
            DateTimeOffset startedAt,
            RuntimePaths paths,
            IReadOnlyList<DiscoveredMod> discoveredMods,
            IReadOnlyList<DiscoveredMod> loadedMods,
            IReadOnlyList<IManifest> registry,
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyList<IDtmWarningInfo> warnings,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IDtmFeatureStatusInfo> featureStatuses,
            IReadOnlyList<IConfigMenuPage> configPages,
            string latestLogPath,
            string latestReportPath,
            string lastExportPath)
        {
            StartedAt = startedAt;
            Paths = paths;
            DiscoveredMods = discoveredMods;
            LoadedMods = loadedMods;
            Registry = registry;
            Errors = errors;
            Warnings = warnings;
            HookStatuses = hookStatuses;
            FeatureStatuses = featureStatuses;
            ConfigPages = configPages;
            LatestLogPath = latestLogPath;
            LatestReportPath = latestReportPath;
            LastExportPath = lastExportPath;
        }

        public DateTimeOffset StartedAt { get; }
        public RuntimePaths Paths { get; }
        public IReadOnlyList<DiscoveredMod> DiscoveredMods { get; }
        public IReadOnlyList<DiscoveredMod> LoadedMods { get; }
        public IReadOnlyList<IManifest> Registry { get; }
        public IReadOnlyList<IDtmErrorInfo> Errors { get; }
        public IReadOnlyList<IDtmWarningInfo> Warnings { get; }
        public IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        public IReadOnlyList<IDtmFeatureStatusInfo> FeatureStatuses { get; }
        public IReadOnlyList<IConfigMenuPage> ConfigPages { get; }
        public string LatestLogPath { get; }
        public string LatestReportPath { get; }
        public string LastExportPath { get; }
    }
}
