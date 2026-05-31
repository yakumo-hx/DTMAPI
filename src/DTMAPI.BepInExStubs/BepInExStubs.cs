using System;

namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BepInPlugin : Attribute
    {
        public BepInPlugin(string guid, string name, string version)
        {
            GUID = guid;
            Name = name;
            Version = version;
        }

        public string GUID { get; }
        public string Name { get; }
        public string Version { get; }
    }

    public class BaseUnityPlugin
    {
        public Logging.ManualLogSource Logger { get; } = new Logging.ManualLogSource("DTMAPI.Stub");
    }

    public static class Paths
    {
        public static string GameRootPath { get; set; } = AppContext.BaseDirectory;
        public static string PluginPath { get; set; } = AppContext.BaseDirectory;
        public static string BepInExRootPath { get; set; } = AppContext.BaseDirectory;
    }
}

namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        public ManualLogSource(string sourceName)
        {
            SourceName = sourceName;
        }

        public string SourceName { get; }

        public void LogDebug(object data) => Console.WriteLine("[Debug] " + data);
        public void LogInfo(object data) => Console.WriteLine("[Info] " + data);
        public void LogWarning(object data) => Console.WriteLine("[Warning] " + data);
        public void LogError(object data) => Console.WriteLine("[Error] " + data);
        public void LogFatal(object data) => Console.WriteLine("[Fatal] " + data);
    }
}
