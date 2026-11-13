namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void PreviewVersionMetadataIsConsistent()
        {
            PlatformVersionProjectionTests.RunAll(FindRepositoryRoot());
        }
    }
}
