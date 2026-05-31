using System.IO;

namespace DTMAPI.Core.Runtime
{
    public sealed class RuntimePaths
    {
        public RuntimePaths(string gamePath, string pluginPath)
        {
            GamePath = Path.GetFullPath(gamePath);
            PluginPath = Path.GetFullPath(pluginPath);
            DtmApiPath = Path.Combine(GamePath, "DTMAPI");
            ModsPath = Path.Combine(GamePath, "Mods");
            LogsPath = Path.Combine(DtmApiPath, "logs");
            ReportsPath = Path.Combine(DtmApiPath, "reports");
            ConfigPath = Path.Combine(DtmApiPath, "config");
            EvidencePath = Path.Combine(DtmApiPath, "evidence");
            SmokeSettingsPath = Path.Combine(DtmApiPath, "smoke-settings.json");
        }

        public string GamePath { get; }
        public string PluginPath { get; }
        public string DtmApiPath { get; }
        public string ModsPath { get; }
        public string LogsPath { get; }
        public string ReportsPath { get; }
        public string ConfigPath { get; }
        public string EvidencePath { get; }
        public string SmokeSettingsPath { get; }

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
