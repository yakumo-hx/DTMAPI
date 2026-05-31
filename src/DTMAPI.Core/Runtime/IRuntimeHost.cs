using System;

namespace DTMAPI.Core.Runtime
{
    public interface IRuntimeHost
    {
        string GamePath { get; }
        string PluginPath { get; }
        string HostName { get; }
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
    }
}
