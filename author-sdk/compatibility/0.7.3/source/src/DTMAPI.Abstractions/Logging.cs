using System;

namespace DTMAPI.Abstractions
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Alert
    }

    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IMonitor
    {
        void Log(string message, LogLevel level = LogLevel.Info);
        void LogOnce(string key, string message, LogLevel level = LogLevel.Info);
        void LogException(Exception exception, string message);
    }

    public sealed class NullMonitor : IMonitor
    {
        public static readonly NullMonitor Instance = new NullMonitor();
        public void Log(string message, LogLevel level = LogLevel.Info) { }
        public void LogOnce(string key, string message, LogLevel level = LogLevel.Info) { }
        public void LogException(Exception exception, string message) { }
    }
}
