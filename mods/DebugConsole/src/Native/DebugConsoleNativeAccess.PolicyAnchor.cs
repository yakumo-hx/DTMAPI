namespace DTMAPI.DebugConsole
{
    internal static partial class DebugConsoleNativeAccess
    {
        // Keeps the SDK policy receipt truthful while reviewed game calls remain
        // late-bound. Compatibility links only the reflection implementation and
        // therefore does not acquire a compile-time Assembly-CSharp dependency.
        private static readonly System.Type AssemblyCSharpAnchor =
            typeof(global::DolocAPI);
    }
}
