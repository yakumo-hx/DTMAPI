using DTMAPI.Authoring.Contracts;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DTMAPI.AuthorSdk;

internal static class AuthorStatePaths
{
    public const int SchemaVersion = 1;

    public static string CanonicalGameRoot(string gameRoot) => Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string GameRootKey(string gameRoot)
    {
        string canonical = CanonicalGameRoot(gameRoot).ToUpperInvariant();
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    public static string InstallationStateRoot(string gameRoot)
    {
        string configured = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT") ?? string.Empty;
        string baseRoot = configured.Length > 0
            ? Path.GetFullPath(configured)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DTMAPI", "AuthorSdk", "state");
        return Path.Combine(baseRoot, "installations", GameRootKey(gameRoot));
    }

    public static string SourceStatePath(string gameRoot) => Path.Combine(InstallationStateRoot(gameRoot), "source-state.json");
    public static string WorkshopSnapshotPath(string gameRoot) => Path.Combine(InstallationStateRoot(gameRoot), "workshop-subscriptions.json");
    public static string DeploymentJournalPath(string gameRoot, string uniqueId) => Path.Combine(InstallationStateRoot(gameRoot), "deployments", uniqueId + ".journal.json");
    public static string SourceSnapshotPath(string gameRoot, string snapshotId) => Path.Combine(InstallationStateRoot(gameRoot), "source-snapshots", snapshotId + ".json");
    public static string AuthorSessionDescriptorPath(string gameRoot) => Path.Combine(InstallationStateRoot(gameRoot), "author-session.json");
    public static string AuthorSessionClientPath(string gameRoot) => Path.Combine(InstallationStateRoot(gameRoot), "author-session-client.json");
    public static string LockPath(string gameRoot) => Path.Combine(InstallationStateRoot(gameRoot), "operation.lock");
}

internal sealed class GameOperationLock : IDisposable
{
    private readonly FileStream stream;

    private GameOperationLock(FileStream stream) => this.stream = stream;

    public static GameOperationLock Acquire(string gameRoot)
    {
        string path = AuthorStatePaths.LockPath(gameRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            return new GameOperationLock(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException ex)
        {
            throw new InvalidDataException("Another Author SDK operation owns the exclusive lock for this game root: " + AuthorStatePaths.CanonicalGameRoot(gameRoot), ex);
        }
    }

    public void Dispose() => stream.Dispose();
}

internal static class AtomicStateFile
{
    public static T ReadStrict<T>(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(text, JsonSupport.Tool)
            ?? throw new InvalidDataException(Path.GetFileName(path) + " must contain one JSON object.");
    }

    public static void Write<T>(string path, T value)
    {
        WriteBytes(path, new UTF8Encoding(false).GetBytes(JsonSupport.SerializeTool(value)));
    }

    public static void WriteBytes(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        string backup = path + ".replace-backup";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                File.Replace(temporary, path, backup, true);
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}

internal sealed class SimulatedAuthorCrashException : Exception
{
    public SimulatedAuthorCrashException(string point) : base("Simulated author process crash at " + point + ".") => Point = point;
    public string Point { get; }
}

internal static class AuthorFaultInjector
{
    internal static Action<string>? BeforeThrowForTests { get; set; }

    public static void Hit(string point)
    {
        string configured = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_FAULT") ?? string.Empty;
        if (configured.Length == 0)
            return;
        string action = "fail";
        string expected = configured;
        int separator = configured.IndexOf(':');
        if (separator > 0)
        {
            action = configured[..separator];
            expected = configured[(separator + 1)..];
        }
        if (!expected.Equals(point, StringComparison.OrdinalIgnoreCase))
            return;
        BeforeThrowForTests?.Invoke(point);
        if (action.Equals("crash", StringComparison.OrdinalIgnoreCase))
            throw new SimulatedAuthorCrashException(point);
        throw new IOException("Injected Author SDK transaction failure at " + point + ".");
    }
}

internal static class DeploymentTree
{
    public const string ReceiptFileName = ".dtmapi-author-receipt.json";

    public static DeploymentTreeInventory Create(string root, bool excludeReceipt = false)
    {
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            throw new DirectoryNotFoundException("Deployment tree was not found: " + fullRoot);
        string[] entries = Directory.EnumerateFileSystemEntries(fullRoot, "*", SearchOption.AllDirectories).ToArray();
        PathSafety.RejectReparsePoints(fullRoot, entries);
        List<string> directories = Directory.EnumerateDirectories(fullRoot, "*", SearchOption.AllDirectories)
            .Select(path => PathSafety.RelativePath(fullRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        List<DeploymentInventoryFile> files = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
            .Where(path => !excludeReceipt || !PathSafety.RelativePath(fullRoot, path).Equals(ReceiptFileName, StringComparison.Ordinal))
            .Select(path => new DeploymentInventoryFile
            {
                Path = PathSafety.RelativePath(fullRoot, path),
                Length = new FileInfo(path).Length,
                Sha256 = PathSafety.Sha256File(path)
            })
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string directory in directories)
            aggregate.AppendData(Encoding.UTF8.GetBytes("D\0" + directory + "\0"));
        foreach (DeploymentInventoryFile file in files)
            aggregate.AppendData(Encoding.UTF8.GetBytes("F\0" + file.Path + "\0" + file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + file.Sha256 + "\0"));
        return new DeploymentTreeInventory
        {
            TreeSha256 = Convert.ToHexString(aggregate.GetHashAndReset()).ToLowerInvariant(),
            Directories = directories,
            Files = files
        };
    }

    public static bool EqualsExact(DeploymentTreeInventory left, DeploymentTreeInventory right)
    {
        if (!string.Equals(left.TreeSha256, right.TreeSha256, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!left.Directories.SequenceEqual(right.Directories, StringComparer.Ordinal))
            return false;
        if (left.Files.Count != right.Files.Count)
            return false;
        for (int i = 0; i < left.Files.Count; i++)
        {
            DeploymentInventoryFile a = left.Files[i];
            DeploymentInventoryFile b = right.Files[i];
            if (!a.Path.Equals(b.Path, StringComparison.Ordinal) || a.Length != b.Length || !a.Sha256.Equals(b.Sha256, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
