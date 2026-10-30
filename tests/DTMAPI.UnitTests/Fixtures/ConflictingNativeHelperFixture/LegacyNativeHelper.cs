namespace DTMAPI.LegacyNativeHelperFixture
{
    public static class LegacyNativeHelper
    {
        public static string Describe(string gameRoot) => "replacement-helper:" + gameRoot;
        public static string ReplacementOnly() => "replacement-only";
    }
}
