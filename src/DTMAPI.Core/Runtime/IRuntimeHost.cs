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

    // Existing Unit fixtures predate the official two-source player boundary.
    // Only those fixtures implement this internal marker; the production
    // Bootstrap host deliberately does not, so <game>/Mods stays undiscoverable.
    internal interface ILegacyDevelopmentModSourceTestHost
    {
        bool IncludeLegacyDevelopmentModSourceForTests { get; }
    }
}
