using System.Text.Json;

namespace DTMAPI.AuthorSdk;

internal static class StandardSdkIntegrity
{
    internal static void VerifyBundledSdk(string sdkRoot)
    {
        string toolchain = Path.Combine(sdkRoot, "toolchain", "dotnet");
        if (!Directory.Exists(toolchain)) return; // Source development uses the tracked host.
        string inventory = Path.Combine(sdkRoot, "author-sdk-release.json");
        if (!File.Exists(inventory)) throw new InvalidDataException("Complete SDK release inventory is missing. Extract the full SDK ZIP.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(inventory));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool compilerFound = false;
        foreach (var item in document.RootElement.GetProperty("files").EnumerateArray())
        {
            string relative = item.GetProperty("path").GetString()!;
            if (!seen.Add(relative)) throw new InvalidDataException("Duplicate SDK inventory path: " + relative);
            // Guides do not participate in execution. Everything executable or
            // selected by restore/build is checked against the delivered inventory.
            if (!(relative.StartsWith("toolchain/", StringComparison.Ordinal) || relative.StartsWith("offline-packages/", StringComparison.Ordinal)
                || relative.StartsWith("build/", StringComparison.Ordinal) || relative.StartsWith("analyzers/", StringComparison.Ordinal)
                || !relative.Contains('/') && Path.GetExtension(relative) is ".dll" or ".exe" or ".json")) continue;
            string path = PathSafety.ResolveUnderRoot(sdkRoot, relative, "SDK inventory");
            if (!File.Exists(path) || new FileInfo(path).Length != item.GetProperty("length").GetInt64()
                || !PathSafety.Sha256File(path).Equals(item.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SDK toolchain or build asset changed: " + relative + ". Extract the full SDK ZIP again.");
            compilerFound |= relative == "toolchain/dotnet/sdk/" + StandardBuildIntegration.SdkVersion + "/Roslyn/bincore/csc.dll";
        }
        if (!compilerFound) throw new InvalidDataException("SDK inventory does not contain the supported full compiler toolchain.");
        foreach (string path in Directory.EnumerateFiles(toolchain, "*", SearchOption.AllDirectories))
            if (!seen.Contains(Path.GetRelativePath(sdkRoot, path).Replace('\\', '/')))
                throw new InvalidDataException("Unlisted SDK toolchain input: " + path);
    }
}
