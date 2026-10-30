using System;

namespace DTMAPI.Modules.Monsters
{
    // Explicit Advanced QA only. Installed before module demand; no late Harmony
    // patch, reflection scan, file I/O or per-call allocation when unarmed.
    internal static class OptimizationTrace
    {
        private static Action<int, bool>? Observer = AppDomain.CurrentDomain.GetData("DTMAPI.Monsters.OptimizationTrace") as Action<int, bool>;
        internal static void Stop() => Observer = null;
        internal readonly struct Scope : IDisposable
        {
            private readonly int category;
            internal Scope(int category) { this.category = category; Observer?.Invoke(category, true); }
            public void Dispose() => Observer?.Invoke(category, false);
        }
        internal static Scope Begin(int category) => new Scope(category);
    }
}
