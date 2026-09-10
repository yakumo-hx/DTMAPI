using System.Diagnostics;
using System.Text.Json;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

internal static class OfficialLocalPaths
{
    public static string PersistentRoot => Path.GetFullPath(Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") is string configured && configured.Length > 0
        ? configured : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RedSawGames", "DolocTown"));
    public static string ModsRoot => Path.Combine(PersistentRoot, "MODS");
    public static string Destination(string uniqueId)
    {
        SourceStateService.ValidateLocalInstallUniqueId(uniqueId);
        return Path.Combine(ModsRoot, uniqueId);
    }
    public static void AddStatus(CommandReport report, string uniqueId)
    {
        report.Values["officialId"] = "Local." + uniqueId;
        report.Values["officialEnabled"] = "unavailable";
        report.Values["selectedSource"] = "unavailable: query session snapshot while Runtime is running";
        report.Values["residentAssembly"] = "unavailable: disk state does not prove loaded bytes";
        report.Values["restartRequired"] = "code changes take effect on next process";
        string path = Path.Combine(PersistentRoot, "SAVE", "mod_infos.json");
        if (!File.Exists(path)) return;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllBytes(path));
            if (json.RootElement.GetProperty("modInfos").TryGetProperty("Local." + uniqueId, out var state))
                report.Values["officialEnabled"] = state.GetProperty("enabled").GetBoolean().ToString().ToLowerInvariant();
            else report.Values["officialEnabled"] = "not-registered";
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        { report.Values["officialEnablementError"] = ex.Message; }
    }
}

internal sealed class ColdGameMutationLease : IDisposable
{
    private readonly FileStream executable;
    private ColdGameMutationLease(FileStream executable) => this.executable = executable;
    public static ColdGameMutationLease Acquire(string gameRoot)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidDataException("Official Local cold mutation is currently supported on Windows only.");
        if (Process.GetProcessesByName("DolocTown").Length != 0) throw new InvalidDataException("restart-required: exit Doloc Town before changing code packages.");
        // Windows denies a concurrent executable image open while this share-none
        // handle is held, including a launch after the initial process check.
        return new ColdGameMutationLease(new FileStream(Path.Combine(gameRoot, "DolocTown.exe"), FileMode.Open, FileAccess.Read, FileShare.None));
    }
    public void Dispose() => executable.Dispose();
}
