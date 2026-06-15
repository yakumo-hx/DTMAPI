using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{
    internal static class RuntimeSnapshotFactory
    {
        public static RuntimeSnapshot CreateRuntimeSnapshot(
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
            return new RuntimeSnapshot(
                startedAt,
                paths,
                discoveredMods,
                loadedMods,
                registry,
                errors,
                warnings,
                hookStatuses,
                featureStatuses,
                configPages,
                latestLogPath,
                latestReportPath,
                lastExportPath);
        }

        public static IDtmDiagnosticsSnapshot CreateDiagnosticsSnapshot(
            DateTimeOffset startedAt,
            IReadOnlyList<DiscoveredMod> discoveredMods,
            IReadOnlyList<DiscoveredMod> loadedMods,
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyList<IDtmWarningInfo> warnings,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IDtmFeatureStatusInfo> featureStatuses,
            string latestLogPath,
            string latestReportPath)
        {
            return new DtmDiagnosticsSnapshot(
                startedAt,
                loadedMods.Select(m => new DtmLoadedModInfo(m.Manifest)).Cast<IDtmLoadedModInfo>().ToArray(),
                CreateModStatusSnapshot(discoveredMods, loadedMods, errors),
                errors,
                warnings,
                hookStatuses,
                featureStatuses,
                latestLogPath,
                latestReportPath);
        }

        public static IReadOnlyList<IDtmModStatusInfo> CreateModStatusSnapshot(
            IReadOnlyList<DiscoveredMod> discoveredMods,
            IReadOnlyList<DiscoveredMod> loadedMods,
            IReadOnlyList<IDtmErrorInfo> errors)
        {
            HashSet<string> loadedIds = new HashSet<string>(loadedMods.Select(m => m.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<IDtmErrorInfo>> errorsByOwner = errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Owner))
                .GroupBy(e => e.Owner, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            List<IDtmModStatusInfo> rows = new List<IDtmModStatusInfo>();

            foreach (DiscoveredMod mod in discoveredMods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                bool loaded = loadedIds.Contains(mod.Manifest.UniqueID);
                string status;
                string statusCode;
                string reason;

                if (!mod.OfficialEnabled)
                {
                    status = "disabled";
                    statusCode = "disabled";
                    reason = string.IsNullOrWhiteSpace(mod.EnablementReason)
                        ? "Disabled by the source enablement path."
                        : mod.EnablementReason;
                    if (loaded)
                        reason += " Already loaded in this process; restart is required for DLL unload.";
                }
                else if (errorsByOwner.TryGetValue(mod.Manifest.UniqueID, out List<IDtmErrorInfo>? modErrors) && modErrors.Count > 0)
                {
                    status = "error";
                    statusCode = GetModStatusCode(status, modErrors);
                    reason = string.Join(" | ", modErrors.Select(error => error.Message + (string.IsNullOrWhiteSpace(error.Details) ? string.Empty : " " + error.Details)).ToArray());
                }
                else if (loaded)
                {
                    status = "loaded";
                    statusCode = "loaded";
                    reason = "Loaded by DTMAPI runtime.";
                }
                else
                {
                    status = "discovered";
                    statusCode = "discovered";
                    reason = "Discovered by DTMAPI but not loaded yet.";
                }

                rows.Add(new DtmModStatusInfo(
                    mod.Manifest.UniqueID,
                    mod.Manifest.Name,
                    mod.Manifest.Version,
                    mod.Manifest.Type,
                    mod.Source,
                    mod.OfficialId,
                    mod.OfficialEnabled,
                    mod.OfficialEnablementManaged,
                    mod.EnablementReason,
                    mod.Manifest.EntryDll,
                    mod.Manifest.EntryType,
                    loaded,
                    status,
                    statusCode,
                    reason,
                    mod.ManifestPath,
                    mod.RootPath));
            }

            return rows;
        }

        private static string GetModStatusCode(string status, IReadOnlyList<IDtmErrorInfo> errors)
        {
            if (!status.Equals("error", StringComparison.OrdinalIgnoreCase))
                return status ?? string.Empty;
            foreach (IDtmErrorInfo error in errors)
            {
                string code = GetModErrorStatusCode(error);
                if (!code.Equals("unknown-error", StringComparison.OrdinalIgnoreCase))
                    return code;
            }
            return "unknown-error";
        }

        private static string GetModErrorStatusCode(IDtmErrorInfo error)
        {
            string message = error?.Message ?? string.Empty;
            string details = error?.Details ?? string.Empty;
            string combined = message + " " + details;
            if (combined.IndexOf("依赖循环", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("dependency cycle", StringComparison.OrdinalIgnoreCase) >= 0)
                return "dependency-cycle";
            if (combined.IndexOf("缺少必需依赖", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("依赖声明缺少", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("依赖版本", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("dependency", StringComparison.OrdinalIgnoreCase) >= 0 && combined.IndexOf("requires", StringComparison.OrdinalIgnoreCase) >= 0)
                return "missing-dependency";
            if (combined.IndexOf("DTMAPI API 版本", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("MinimumDTMApiVersion", StringComparison.OrdinalIgnoreCase) >= 0)
                return "api-too-new";
            if (combined.IndexOf("EntryDll", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("EntryType", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("DtmMod 入口", StringComparison.OrdinalIgnoreCase) >= 0)
                return "entry-dll-error";
            if (combined.IndexOf("Failed to load code mod", StringComparison.OrdinalIgnoreCase) >= 0)
                return "code-load-error";
            return "unknown-error";
        }
    }
}
