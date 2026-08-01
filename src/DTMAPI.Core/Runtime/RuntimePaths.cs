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
            ModsPath = Path.Combine(GamePath, "Mods");
            LogsPath = Path.Combine(DtmApiPath, "logs");
            ReportsPath = Path.Combine(DtmApiPath, "reports");
            ConfigPath = Path.Combine(DtmApiPath, "config");
            EvidencePath = Path.Combine(DtmApiPath, "evidence");
            PlayerDoctorPath = Path.Combine(DtmApiPath, "tools", "player-doctor", "dtmapi-player-doctor.exe");
            PlayerDoctorJsonReportPath = Path.Combine(ReportsPath, "player-doctor-latest.json");
            PlayerDoctorTextReportPath = Path.Combine(ReportsPath, "player-doctor-latest.txt");
            PlayerDoctorSummaryPath = Path.Combine(ReportsPath, "player-doctor-latest.summary.txt");
        }

        public string GamePath { get; }
        public string PluginPath { get; }
        public string DtmApiPath { get; }
        public string ModsPath { get; }
        public string LogsPath { get; }
        public string ReportsPath { get; }
        public string ConfigPath { get; }
        public string EvidencePath { get; }
        public string PlayerDoctorPath { get; }
        public string PlayerDoctorJsonReportPath { get; }
        public string PlayerDoctorTextReportPath { get; }
        public string PlayerDoctorSummaryPath { get; }

        public void Ensure()
        {
            Directory.CreateDirectory(DtmApiPath);
            Directory.CreateDirectory(ModsPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(ReportsPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(EvidencePath);
        }
    }
}
