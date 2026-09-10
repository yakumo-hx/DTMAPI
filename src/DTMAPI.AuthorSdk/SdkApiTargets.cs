using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.AuthorSdk;

internal static class SdkApiTargets
{
    public static AuthorApiTarget ForCommand(string apiTarget)
    {
        AuthorApiTarget target;
        try { target = AuthorApiTargetCatalog.Current.GetAvailable(apiTarget); }
        catch (InvalidDataException ex) { throw new CommandLineException(ex.Message); }
        if (!target.SdkVersions.Contains(AuthorSdkContract.SdkVersion, StringComparer.Ordinal))
            throw new CommandLineException("This SDK cannot use API target " + target.ApiTarget + ".");
        return target;
    }
}
