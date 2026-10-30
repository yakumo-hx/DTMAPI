using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.InstallDoctor;
public enum InstalledRuntimeVersionProbeStatus { Consistent, Missing, Invalid, Inconsistent }
public sealed class InstalledRuntimeVersionProbeResult
{
    public InstalledRuntimeVersionProbeStatus Status { get; init; }
    public string InstalledDtmApiVersion { get; init; } = "";
    public IReadOnlyList<DoctorFinding> Findings { get; init; } = Array.Empty<DoctorFinding>();
}
public static class InstalledRuntimeVersionProbe
{
    public static InstalledRuntimeVersionProbeResult Inspect(string gameRoot)
    {
        string root = Path.GetFullPath(gameRoot), plugin = Path.Combine(root, "BepInEx/plugins/DTMAPI");
        var findings = new List<DoctorFinding>(); var versions = new Dictionary<string, string>();
        foreach (string name in new[] { "DTMAPI.Core", "DTMAPI.Abstractions", "DTMAPI.BepInExBootstrap", "DTMAPI.GameBridge.DolocTown", "DTMAPI.ModConfigMenu" })
        {
            string path = Path.Combine(plugin, name + ".dll");
            if (!File.Exists(path)) { Add("runtime-file-missing", path, "Runtime DLL is missing.", DoctorSeverity.Error); continue; }
            var pe = new PeMetadataInspector().Inspect(path, computeHash: false);
            if (!pe.IsManaged || pe.AssemblyName != name) { Add("runtime-identity-invalid", path, "Expected " + name + "; " + pe.Error, DoctorSeverity.Error); continue; }
            string value = FileVersionInfo.GetVersionInfo(path).ProductVersion?.Split('+')[0] ?? "";
            if (Version.TryParse(value.Split('-')[0], out _)) versions[name] = value;
            else Add("runtime-version-invalid", path, "No readable product version.", DoctorSeverity.Error);
        }
        string version = versions.TryGetValue("DTMAPI.Core", out var core) ? core : versions.Values.FirstOrDefault() ?? "";
        if (versions.Values.Distinct(StringComparer.Ordinal).Count() > 1) Add("runtime-version-inconsistent", plugin, string.Join(", ", versions.Select(p => p.Key + "=" + p.Value)), DoctorSeverity.Error);
        // Receipts describe installer state, but do not erase a readable Core version.
        foreach (string file in new[] { "install-state.json", "release-manifest.json" })
        {
            string path = Path.Combine(root, "DTMAPI", file);
            if (!File.Exists(path)) { Add("installer-state-unavailable", path, "Installer metadata is absent; DLL version remains available.", DoctorSeverity.Info); continue; }
            try
            {
                var data = JsonNode.Parse(File.ReadAllText(path));
                string claimed = data?["DTMAPIVersion"]?.GetValue<string>() ?? "";
                if (claimed.Length > 0 && claimed != version) Add("installer-state-version-differs", path, "Installer metadata=" + claimed + "; actual runtime=" + version, DoctorSeverity.Warning);
                if (data?["OptionalComponents"] is JsonArray components)
                    foreach (var component in components)
                    {
                        string relative = component?["Path"]?.GetValue<string>() ?? component?["path"]?.GetValue<string>() ?? "";
                        if (relative.Length == 0) continue;
                        string target = Path.GetFullPath(Path.Combine(root, relative));
                        if (!target.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!File.Exists(target) && !Directory.Exists(target)) Add("optional-component-missing", target, "Declared optional component is absent; runtime version is still " + version, DoctorSeverity.Warning);
                    }
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidOperationException)
            { Add("installer-state-unreadable", path, ex.Message, DoctorSeverity.Warning); }
        }
        return new InstalledRuntimeVersionProbeResult { InstalledDtmApiVersion = version, Findings = findings,
            Status = version.Length == 0 ? InstalledRuntimeVersionProbeStatus.Missing : findings.Any(f => f.Severity == DoctorSeverity.Error) ? InstalledRuntimeVersionProbeStatus.Inconsistent : InstalledRuntimeVersionProbeStatus.Consistent };
        void Add(string code, string path, string message, DoctorSeverity severity) => findings.Add(new DoctorFinding { Code = code, Path = path, Message = message, Severity = severity });
    }
}
