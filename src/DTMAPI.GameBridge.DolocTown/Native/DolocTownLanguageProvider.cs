using System;
using DTMAPI.Core.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class DolocTownLanguageProvider
    {
        private static readonly object Gate = new object();
        private static Type? dolocApiType;

        internal static string? GetCurrentLanguage()
        {
            try
            {
                Type? type;
                lock (Gate)
                {
                    // Cache only a successfully loaded type. A late native assembly can be retried.
                    type = dolocApiType ?? (dolocApiType = Type.GetType("DolocAPI, Assembly-CSharp", throwOnError: false));
                }
                if (type == null) return null;
                using (var scope = new ReflectionScope())
                {
                    if (scope.TryStaticProperty<string>(type, "CurrentL10nId", out var property)) return property!.GetValue();
                    return scope.TryStaticField<string>(type, "CurrentL10nId", out var field) ? field!.GetValue() : null;
                }
            }
            catch
            {
                return null;
            }
        }

    }
}
