namespace DTMAPI.LegacyNativeHelperFixture
{
    public static class LegacyNativeHelper
    {
        public static string Describe(string gameRoot)
        {
            return "legacy-helper-ready:" + (gameRoot ?? string.Empty);
        }
    }
}
