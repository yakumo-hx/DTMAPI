using System;

namespace DTMAPI.ModConfigMenu
{
    internal static class ConfigMenuCallbackRunner
    {
        public static void Run(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(operation + " callback failed: " + Describe(ex), ex);
            }
        }

        public static string Describe(Exception ex)
        {
            string message = ex.Message ?? string.Empty;
            return string.IsNullOrWhiteSpace(message) ? ex.GetType().Name : ex.GetType().Name + ": " + message;
        }
    }
}
