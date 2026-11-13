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
            string latestReportPath,
            IReadOnlyCollection<string>? restartRequiredOwners = null,
            Func<DiscoveredMod, string>? pendingRestartReason = null)
        {
            return new DtmDiagnosticsSnapshot(
                startedAt,
                loadedMods.Select(m => new DtmLoadedModInfo(m.Manifest)).Cast<IDtmLoadedModInfo>().ToArray(),
                CreateModStatusSnapshot(discoveredMods, loadedMods, errors, restartRequiredOwners, pendingRestartReason),
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
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyCollection<string>? restartRequiredOwners = null,
            Func<DiscoveredMod, string>? pendingRestartReason = null)
        {
            var loadedById = loadedMods.ToDictionary(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var discoveredIds = new HashSet<string>(discoveredMods.Select(mod => mod.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            var restartRequiredIds = new HashSet<string>(restartRequiredOwners ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<IDtmErrorInfo>> errorsByOwner = errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Owner))
                .GroupBy(error => error.Owner, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            List<IDtmModStatusInfo> rows = new List<IDtmModStatusInfo>();

            foreach (DiscoveredMod mod in discoveredMods.Concat(loadedMods.Where(mod => !discoveredIds.Contains(mod.Manifest.UniqueID)))
                .OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                bool loaded = loadedById.TryGetValue(mod.Manifest.UniqueID, out DiscoveredMod resident);
                bool selected = discoveredIds.Contains(mod.Manifest.UniqueID);
                string status;
                string statusCode;
                string reason;

                errorsByOwner.TryGetValue(mod.Manifest.UniqueID, out List<IDtmErrorInfo>? modErrors);
                string pending = pendingRestartReason?.Invoke(mod) ?? "";
                if (restartRequiredIds.Contains(mod.Manifest.UniqueID) && modErrors != null && modErrors.Count > 0)
                {
                    status = "error";
                    statusCode = GetModStatusCode(status, modErrors);
                    reason = "加载失败：" + string.Join(" | ", modErrors.Select(error => error.Message + " " + error.Details));
                }
                else if (restartRequiredIds.Contains(mod.Manifest.UniqueID))
                {
                    status = "inactive";
                    statusCode = "restart-required";
                    reason = "重启后生效：此 Mod 本次已结束初始化，不能再次加载。";
                }
                else if (loaded && (!selected || !mod.OfficialEnabled ||
                    !DtmApiRuntime.HasSameSourceIdentity(resident, mod) || resident.Manifest.Version != mod.Manifest.Version))
                {
                    status = "loaded";
                    statusCode = "restart-required";
                    reason = !selected || !mod.OfficialEnabled
                        ? "重启后停用：本次仍在运行。"
                        : "重启后生效：本次仍使用已加载的来源和版本。";
                    reason += " 本次版本：" + resident.Manifest.Version + "；本次来源：" + resident.Source + "/" + resident.OfficialId + "。";
                    if (modErrors != null && modErrors.Count > 0)
                        reason += " Prior diagnostics: " + string.Join(" | ", modErrors.Select(error => error.Message + " " + error.Details));
                }
                else if (loaded && (modErrors == null || modErrors.Count == 0))
                {
                    status = "loaded";
                    statusCode = "loaded";
                    reason = "运行中。";
                }
                else if (mod.OfficialEnabled && pending.Length != 0)
                {
                    status = "discovered";
                    statusCode = "restart-required";
                    reason = pending;
                }
                else if (modErrors != null && modErrors.Count > 0)
                {
                    status = "error";
                    statusCode = GetModStatusCode(status, modErrors);
                    reason = (loaded ? "运行中；错误：" : "加载失败：") + string.Join(" | ", modErrors.Select(error => error.Message + (string.IsNullOrWhiteSpace(error.Details) ? string.Empty : " " + error.Details)).ToArray());
                }
                else if (!mod.OfficialEnabled)
                {
                    status = "disabled";
                    statusCode = "disabled";
                    reason = string.IsNullOrWhiteSpace(mod.EnablementReason)
                        ? "Disabled by the source enablement path."
                        : mod.EnablementReason;
                }
                else
                {
                    status = "discovered";
                    statusCode = "discovered";
                    reason = "尚未加载。";
                }

                rows.Add(new DtmModStatusInfo(
                    mod.Manifest.UniqueID,
                    mod.Manifest.Name,
                    mod.Manifest.Version,
                    mod.Manifest.Type,
                    mod.Source,
                    mod.OfficialId,
                    selected && mod.OfficialEnabled,
                    mod.OfficialEnablementManaged,
                    mod.EnablementReason,
                    mod.Manifest.EntryDll,
                    mod.Manifest.EntryType,
                    loaded,
                    status,
                    statusCode,
                    reason,
                    mod.ManifestPath,
                    mod.RootPath,
                    mod.Classification));
            }

            return rows;
        }

        private static string GetModStatusCode(string status, IReadOnlyList<IDtmErrorInfo> errors)
        {
            if (!status.Equals("error", StringComparison.OrdinalIgnoreCase))
                return status;
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
            string message = error.Message;
            string details = error.Details;
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
            if (combined.IndexOf("代码 Mod 加载失败", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("Failed to load code mod", StringComparison.OrdinalIgnoreCase) >= 0)
                return "code-load-error";
            if (combined.IndexOf("Mod monitor reported an error", StringComparison.OrdinalIgnoreCase) >= 0)
                return "runtime-diagnostic";
            return "unknown-error";
        }
    }
}
