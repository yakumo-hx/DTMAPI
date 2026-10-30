using System;
using System.IO;

namespace DTMAPI.Core.Runtime
{
    public sealed class RuntimePaths
    {
        public RuntimePaths(string gamePath, string pluginPath)
        {
            GamePath = Path.GetFullPath(gamePath);
            PluginPath = Path.GetFullPath(pluginPath);
            var runtimeDirectoryOverride =
                Environment.GetEnvironmentVariable("DTMAPI_RUNTIME_DIR")
                ?? Environment.GetEnvironmentVariable("DTMAPI_STATE_DIR");
            DtmApiPath = string.IsNullOrWhiteSpace(runtimeDirectoryOverride)
                ? Path.Combine(GamePath, "DTMAPI")
                : Path.GetFullPath(runtimeDirectoryOverride);
            LegacyDevelopmentModsPath = Path.Combine(GamePath, "Mods");
            LogsPath = Path.Combine(DtmApiPath, "logs");
            ReportsPath = Path.Combine(DtmApiPath, "reports");
            ConfigPath = Path.Combine(DtmApiPath, "config");
            GlobalDataPath = Path.Combine(DtmApiPath, "global-data");
            EvidencePath = Path.Combine(DtmApiPath, "evidence");
            PlayerDoctorPath = Path.Combine(DtmApiPath, "tools", "player-doctor", "dtmapi-player-doctor.exe");
            PlayerDoctorJsonReportPath = Path.Combine(ReportsPath, "player-doctor-latest.json");
            PlayerDoctorTextReportPath = Path.Combine(ReportsPath, "player-doctor-latest.txt");
            PlayerDoctorSummaryPath = Path.Combine(ReportsPath, "player-doctor-latest.summary.txt");
        }

        public string GamePath { get; }
        public string PluginPath { get; }
        public string DtmApiPath { get; }
        // Test fixtures and old-deployment diagnostics may name this path, but
        // ordinary Runtime startup neither creates nor discovers it.
        public string LegacyDevelopmentModsPath { get; }
        public string LogsPath { get; }
        public string ReportsPath { get; }
        public string ConfigPath { get; }
        public string GlobalDataPath { get; }
        public string EvidencePath { get; }
        public string PlayerDoctorPath { get; }
        public string PlayerDoctorJsonReportPath { get; }
        public string PlayerDoctorTextReportPath { get; }
        public string PlayerDoctorSummaryPath { get; }

        public void Ensure()
        {
            Directory.CreateDirectory(DtmApiPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(ReportsPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(EvidencePath);
        }
    }
}
