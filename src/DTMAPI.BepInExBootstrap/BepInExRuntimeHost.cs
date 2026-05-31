using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using DTMAPI.Core.Runtime;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class BepInExRuntimeHost : IRuntimeHost
    {
        private readonly ManualLogSource logger;

        public BepInExRuntimeHost(ManualLogSource logger)
        {
            this.logger = logger;
            GamePath = ResolvePath(nameof(Paths.GameRootPath), Paths.GameRootPath);
            PluginPath = ResolvePath(nameof(Paths.PluginPath), Paths.PluginPath);
            if (string.IsNullOrWhiteSpace(PluginPath))
                PluginPath = Path.Combine(GamePath, "BepInEx", "plugins");
        }

        public string GamePath { get; }
        public string PluginPath { get; private set; }
        public string HostName => "BepInEx";
        public void Log(string message) => logger.LogInfo(message);
        public void LogWarning(string message) => logger.LogWarning(message);
        public void LogError(string message, Exception? exception = null) => logger.LogError(exception == null ? message : message + Environment.NewLine + exception);

        private static string ResolvePath(string name, string fallback)
        {
            try
            {
                string? value = typeof(Paths).GetProperty(name)?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(value))
                    return Path.GetFullPath(value);
            }
            catch
            {
            }
            return Path.GetFullPath(string.IsNullOrWhiteSpace(fallback) ? AppContext.BaseDirectory : fallback);
        }
    }
}
