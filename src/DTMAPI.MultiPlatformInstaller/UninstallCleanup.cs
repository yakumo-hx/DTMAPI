using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.MultiPlatformInstaller;

// Cleanup owns paths, not installation phases. It never replays an old install.
internal sealed class UninstallCleanup : IDisposable
{
    private readonly string _game;
    private readonly JsonDocument _policy;
    private readonly InstallerLog _log;
    private readonly List<(string Root, string Path)> _targets = new();
    private readonly List<(string Root, string Path, List<(string Source, string Target)> Originals)> _transactions = new();
    private readonly HashSet<string> _restored = new(FileSystemSafety.PathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly List<string> _runtimeRoots = new();
    public List<string> Problems { get; } = new();
    public List<string> Equipment { get; } = new();
    public int Removed { get; private set; }
    private static readonly Regex Stamp = new("^[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-fA-F]{8}$");

    public UninstallCleanup(string game, InstallerLog log)
    {
        _game = FileSystemSafety.NormalizeRoot(game);
        _log = log;
        using Stream resource = typeof(UninstallCleanup).Assembly.GetManifestResourceStream("DTMAPI.UninstallTargets.json")!;
        _policy = JsonDocument.Parse(resource);
    }

    private string[] Rows(string key) => _policy.RootElement.GetProperty(key).EnumerateArray().Select(x => x.GetString()!).ToArray();
    private static bool Under(string path, string root) => path.StartsWith(root + Path.DirectorySeparatorChar, FileSystemSafety.PathComparison);
    private void Problem(string path, Exception error) => Problem(path, error.Message + " (0x" + error.HResult.ToString("X8") + ")");
    private void Problem(string path, string message)
    {
        string detail = path + " | " + message;
        Problems.Add(detail);
        _log.Write("REMAINING " + detail);
    }
    private static string Child(string root, string relative) => FileSystemSafety.CombineUnder(root, FileSystemSafety.NormalizeRelativePath(relative));
    private static bool Exists(string path)
    {
        try { _ = File.GetAttributes(path); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }
    private void Add(string root, string relative)
    {
        string path = Child(root, relative);
        try { if (Exists(path)) _targets.Add((root, path)); }
        catch (Exception ex) { Problem(path, ex); }
    }

    public void Discover()
    {
        _runtimeRoots.Add(Child(_game, "DTMAPI"));
        string? custom = Environment.GetEnvironmentVariable("DTMAPI_RUNTIME_DIR") ?? Environment.GetEnvironmentVariable("DTMAPI_STATE_DIR");
        if (!string.IsNullOrWhiteSpace(custom))
        {
            try
            {
                string root = FileSystemSafety.NormalizeRoot(custom);
                if (!string.Equals(root, _runtimeRoots[0], FileSystemSafety.PathComparison))
                {
                    if (string.Equals(root, _game, FileSystemSafety.PathComparison) || Under(_game, root))
                        throw new InvalidDataException("Custom Runtime directory contains the game directory.");
                    SafePath(root, root);
                    using JsonDocument state = JsonDocument.Parse(File.ReadAllText(Child(root, "install-state.json")));
                    if (!string.Equals(FileSystemSafety.NormalizeRoot(state.RootElement.GetProperty("GameDir").GetString()!), _game, FileSystemSafety.PathComparison))
                        throw new InvalidDataException("Custom Runtime directory has no ownership record for the selected game.");
                    _runtimeRoots.Add(root);
                }
            }
            catch (Exception ex) { Problem(custom, ex); }
        }
        foreach (string root in _runtimeRoots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                SafePath(root, root);
                string config = Child(root, "config");
                if (Directory.Exists(config)) FindEquipment(root, config);
                foreach (string relative in Rows("RuntimeDirectories").Concat(Rows("RuntimeFiles"))) Add(root, relative);
                foreach (string pattern in Rows("RuntimeFilePatterns"))
                    foreach (string path in Directory.EnumerateFiles(root, pattern)) _targets.Add((root, path));
                foreach (string scratch in Directory.EnumerateFileSystemEntries(root))
                    if (Regex.IsMatch(Path.GetFileName(scratch), _policy.RootElement.GetProperty("RuntimeScratchPattern").GetString()!)) _targets.Add((root, scratch));
                string backups = Child(root, "backups");
                if (Directory.Exists(backups))
                {
                    SafePath(root, backups);
                    foreach (string backup in Directory.EnumerateFileSystemEntries(backups)) DiscoverBackup(root, backup);
                }
                foreach (string txn in Directory.EnumerateFileSystemEntries(root, ".runtime-install-transaction-*")) DiscoverPsState(root, txn);
            }
            catch (Exception ex) { Problem(root, ex); }
        }
        foreach (string relative in Rows("GameDirectories").Concat(Rows("GameFiles")).Concat(Rows("CoreFiles").Select(x => "BepInEx/core/" + x))) Add(_game, relative);
        foreach (JsonProperty shared in _policy.RootElement.GetProperty("SharedFiles").EnumerateObject())
        {
            string path = Child(_game, shared.Name);
            try
            {
                if (!Exists(path)) continue;
                if (IsSharedOwned(path, shared.Name)) _targets.Add((_game, path));
                else Problem(path, "Unrecognized shared file preserved; export logs before replacing it.");
            }
            catch (Exception ex) { Problem(path, ex); }
        }
        foreach (string path in Directory.EnumerateFileSystemEntries(_game))
        {
            string name = Path.GetFileName(path);
            if (Regex.IsMatch(name, _policy.RootElement.GetProperty("GameScratchPattern").GetString()!)) _targets.Add((_game, path));
            foreach (string prefix in new[] { ".dtmapi-runtime-install-", ".dtmapi-multiplatform-install-", ".dtmapi-multiplatform-genesis-" })
                if (name.StartsWith(prefix, StringComparison.Ordinal)) { DiscoverTransaction(_game, path); break; }
        }
    }

    // Resolve every ancestor from the volume root; never follow an external link.
    private static void SafePath(string root, string path)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(fullRoot, fullPath, FileSystemSafety.PathComparison) && !Under(fullPath, fullRoot))
            throw new InvalidDataException("Cleanup path escaped its root: " + path);
        // Attribute reads are constant-time; don't enumerate each parent once per
        // cached media file just to find that file again.
        for (string? current = fullPath; current is not null; current = Path.GetDirectoryName(current))
        {
            if (Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked path preserved: " + current);
        }
    }

    private bool IsSharedOwned(string path, string relative)
    {
        SafePath(Path.GetDirectoryName(path)!, path);
        FileInfo file = new(path);
        if (!file.Exists || file.Length > 4 * 1024 * 1024) return false;
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.UTF8.GetString(bytes);
        if (relative == "winhttp.dll") text += "\n" + Encoding.Unicode.GetString(bytes);
        if (!Regex.IsMatch(text, _policy.RootElement.GetProperty("SharedFiles").GetProperty(relative).GetString()!)) return false;
        // The version marker is otherwise indistinguishable from another Doorstop user's marker.
        if (relative is not (".doorstop_version" or "winhttp.dll")) return true;
        string config = Child(_game, "doorstop_config.ini");
        return File.Exists(config) ? IsSharedOwned(config, "doorstop_config.ini")
            : relative == "winhttp.dll" && (File.Exists(Child(_game, "BepInEx/core/BepInEx.dll")) || Directory.Exists(Child(_game, "BepInEx/plugins/DTMAPI")));
    }

    private bool OwnedRelative(string relative)
    {
        string r = FileSystemSafety.NormalizeRelativePath(relative);
        return Rows("GameFiles").Concat(Rows("CoreFiles").Select(x => "BepInEx/core/" + x)).Contains(r, StringComparer.OrdinalIgnoreCase)
            || Rows("GameDirectories").Any(d => r.Equals(d, StringComparison.OrdinalIgnoreCase) || r.StartsWith(d + "/", StringComparison.OrdinalIgnoreCase))
            || Rows("RuntimeFiles").Any(f => r.Equals("DTMAPI/" + f, StringComparison.OrdinalIgnoreCase))
            || Rows("RuntimeDirectories").Any(d => r.Equals("DTMAPI/" + d, StringComparison.OrdinalIgnoreCase) || r.StartsWith("DTMAPI/" + d + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEquipmentPath(string path) =>
        Path.GetFileName(path).StartsWith("equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json", StringComparison.OrdinalIgnoreCase)
        || (path.Replace('\\', '/').Contains("/equipment-slots/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".flat.json", StringComparison.OrdinalIgnoreCase));

    private void FindEquipment(string root, string directory)
    {
        SafePath(root, directory);
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            SafePath(root, entry);
            if (Directory.Exists(entry)) FindEquipment(root, entry);
            else if (IsEquipmentPath(entry)) Equipment.Add(entry);
        }
    }

    private void DiscoverPsState(string root, string path)
    {
        try
        {
            SafePath(root, path);
            string stamp = Path.GetFileName(path)[".runtime-install-transaction-".Length..];
            if (!Stamp.IsMatch(stamp)) throw new InvalidDataException("Unknown legacy transaction name.");
            foreach (string entry in Directory.EnumerateFileSystemEntries(path))
            {
                if (Path.GetFileName(entry) is not ("candidate" or "recovery")) throw new InvalidDataException("Unknown legacy transaction content: " + entry);
                foreach (string child in Directory.EnumerateFileSystemEntries(entry))
                    if (Path.GetFileName(child) is not ("tools" or "components" or "release-manifest.json" or "install-state.json")) throw new InvalidDataException("Unknown legacy state content: " + child);
            }
            FileSystemSafety.AssertTreeHasNoLinks(path);
            _transactions.Add((root, path, new()));
        }
        catch (Exception ex) { Problem(path, ex); }
    }

    private void DiscoverTransaction(string boundary, string path)
    {
        try
        {
            SafePath(boundary, path);
            FileSystemSafety.AssertTreeHasNoLinks(path);
            if (boundary == _game && !Regex.IsMatch(Path.GetFileName(path), "^\\.dtmapi-(?:(?:runtime|multiplatform)-install-[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-fA-F]{8}|multiplatform-genesis-[0-9a-fA-F]{32})$"))
                throw new InvalidDataException("Unknown transaction directory name.");
            string receipt = Child(path, "transaction.json");
            if (!File.Exists(receipt))
            {
                if (Directory.EnumerateFileSystemEntries(path).Any(x => !File.Exists(x) || !Regex.IsMatch(Path.GetFileName(x), "^transaction\\.json\\.(tmp|bak)-[0-9a-fA-F]{32}$"))) throw new InvalidDataException("Unmapped transaction content preserved (receipt missing).");
                _transactions.Add((boundary, path, new()));
                return;
            }
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(receipt));
            JsonElement row = doc.RootElement;
            if (row.GetProperty("SchemaVersion").GetInt32() != 1) throw new InvalidDataException("Unknown transaction schema.");
            List<(string Source, string Target)> originals = new();
            if (row.TryGetProperty("Engine", out JsonElement engine))
            {
                if (engine.GetString() is not ("DTMAPI.MultiPlatform/install" or "DTMAPI.MultiPlatform/bepinex" or "DTMAPI.MultiPlatform/uninstall")) throw new InvalidDataException("Unknown transaction engine.");
                HashSet<string> allowed = new(StringComparer.Ordinal) { "transaction.json" };
                if (engine.GetString() == "DTMAPI.MultiPlatform/uninstall") allowed.Add("uninstall-state.json");
                bool foreignDoorstop = row.GetProperty("Operations").EnumerateArray().Any(op =>
                    op.GetProperty("TargetRelativePath").GetString() == "doorstop_config.ini"
                    && File.Exists(Child(path, op.GetProperty("BackupRelativePath").GetString()!))
                    && !IsSharedOwned(Child(path, op.GetProperty("BackupRelativePath").GetString()!), "doorstop_config.ini"));
                foreach (JsonElement op in row.GetProperty("Operations").EnumerateArray())
                {
                    string relative = FileSystemSafety.NormalizeRelativePath(op.GetProperty("TargetRelativePath").GetString()!);
                    bool shared = _policy.RootElement.GetProperty("SharedFiles").TryGetProperty(relative, out _);
                    if (!OwnedRelative(relative) && !shared) throw new InvalidDataException("Unknown transaction target: " + relative);
                    string backup = FileSystemSafety.NormalizeRelativePath(op.GetProperty("BackupRelativePath").GetString()!);
                    string candidateValue = op.GetProperty("CandidateRelativePath").GetString() ?? "";
                    string candidate = candidateValue.Length == 0 ? "" : FileSystemSafety.NormalizeRelativePath(candidateValue);
                    if (!Regex.IsMatch(backup, "^backup/[0-9]{4,}\\.bin$") || (candidate.Length > 0 && !Regex.IsMatch(candidate, "^candidate/[0-9]{4,}\\.bin$"))) throw new InvalidDataException("Unknown transaction mapping.");
                    allowed.Add(backup);
                    if (candidate.Length > 0) allowed.Add(candidate);
                    string source = Child(path, backup);
                    if (IsEquipmentPath(relative))
                    {
                        if (File.Exists(source)) Equipment.Add(source + " -> " + relative);
                        if (candidate.Length > 0 && File.Exists(Child(path, candidate))) Equipment.Add(Child(path, candidate) + " -> " + relative);
                    }
                    if (shared && File.Exists(source) && ((foreignDoorstop && relative is "winhttp.dll" or ".doorstop_version") || !IsSharedOwned(source, relative))) originals.Add((source, Child(_game, relative)));
                }
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(path, file).Replace('\\', '/');
                    if (!allowed.Contains(relative) && !Regex.IsMatch(relative, "^transaction\\.json\\.(tmp|bak)-[0-9a-fA-F]{32}$")) throw new InvalidDataException("Unmapped transaction file: " + relative);
                }
            }
            else
            {
                if (!row.TryGetProperty("CandidatePlugin", out _) || !row.TryGetProperty("RecoveryPlugin", out _)) throw new InvalidDataException("Unknown legacy transaction receipt.");
                foreach (string entry in Directory.EnumerateFileSystemEntries(path))
                    if (Path.GetFileName(entry) is not ("candidate-plugin" or "recovery-plugin" or "transaction.json")) throw new InvalidDataException("Unknown legacy transaction content: " + entry);
            }
            _transactions.Add((boundary, path, originals));
        }
        catch (Exception ex) { Problem(path, ex); }
    }

    private void DiscoverBackup(string boundary, string path)
    {
        try
        {
            SafePath(boundary, path);
            string name = Path.GetFileName(path);
            if (File.Exists(Child(path, "transaction.json"))) { DiscoverTransaction(boundary, path); return; }
            if (!name.StartsWith("bepinex-install-", StringComparison.Ordinal) && !name.StartsWith("uninstall-", StringComparison.Ordinal)) throw new InvalidDataException("Backup is not an owned framework backup; preserved.");
            FileSystemSafety.AssertTreeHasNoLinks(path);
            List<(string Source, string Target)> originals = new();
            string backupConfig = Child(path, "doorstop_config.ini");
            bool foreignDoorstop = File.Exists(backupConfig) && !IsSharedOwned(backupConfig, "doorstop_config.ini");
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(path, file).Replace('\\', '/');
                if (relative == "install-summary.txt" && name.StartsWith("bepinex-install-", StringComparison.Ordinal)) continue;
                if (_policy.RootElement.GetProperty("SharedFiles").TryGetProperty(relative, out _))
                {
                    if ((foreignDoorstop && relative is "winhttp.dll" or ".doorstop_version") || !IsSharedOwned(file, relative)) originals.Add((file, Child(_game, relative)));
                }
                else if (!OwnedRelative(relative)) throw new InvalidDataException("Unmapped backup file preserved: " + relative);
            }
            FindEquipment(boundary, path);
            _transactions.Add((boundary, path, originals));
        }
        catch (Exception ex) { Problem(path, ex); }
    }

    public void Execute()
    {
        foreach (var transaction in _transactions)
        {
            try
            {
                foreach (var original in transaction.Originals.OrderBy(x => Path.GetFileName(x.Target) == "doorstop_config.ini" ? 1 : 0))
                {
                    SafePath(transaction.Root, original.Source);
                    SafePath(_game, original.Target);
                    if (File.Exists(original.Target) && !IsSharedOwned(original.Target, Path.GetFileName(original.Target))
                        && (new FileInfo(original.Target).Length > 4 * 1024 * 1024 || new FileInfo(original.Source).Length > 4 * 1024 * 1024 || !File.ReadAllBytes(original.Target).SequenceEqual(File.ReadAllBytes(original.Source))))
                        throw new IOException("Original conflicts with a current non-framework file; both copies preserved: " + original.Target);
                    File.Copy(original.Source, original.Target, true);
                    _restored.Add(original.Target);
                    Problems.RemoveAll(p => p.StartsWith(original.Target + " | Unrecognized shared file", FileSystemSafety.PathComparison));
                    _log.Write("RESTORED " + original.Target);
                }
                Delete(transaction.Root, transaction.Path);
            }
            catch (Exception ex) { Problem(transaction.Path, ex); }
        }
        foreach (var target in _targets)
        {
            if (_restored.Contains(target.Path)) continue;
            if (Path.GetFileName(target.Path) == "install-state.json" && Problems.Any(p => p.StartsWith(target.Root + Path.DirectorySeparatorChar, FileSystemSafety.PathComparison))) continue;
            try { Delete(target.Root, target.Path); }
            catch (Exception ex) { Problem(target.Path, ex); }
        }
        foreach (string relative in Rows("EmptyDirectories")) Prune(_game, Child(_game, relative));
        foreach (string root in _runtimeRoots)
        {
            foreach (string relative in new[] { "cache", "backups" }) Prune(root, Child(root, relative));
            Prune(root, root);
            if (Directory.Exists(root) && !Problems.Any(p => p.StartsWith(root, FileSystemSafety.PathComparison)))
                Problem(root, "Unrecognized remaining content preserved; export logs for review.");
        }
    }

    private void Delete(string root, string path)
    {
        if (!Exists(path)) return;
        SafePath(root, path);
        if (Directory.Exists(path))
        {
            foreach (string child in Directory.EnumerateFileSystemEntries(path).Where(x => Path.GetFileName(x) != "transaction.json").ToArray())
            {
                try { Delete(root, child); }
                catch (Exception ex) { Problem(child, ex); }
            }
            if (Directory.EnumerateFileSystemEntries(path).Any(x => Path.GetFileName(x) != "transaction.json")) return;
            string receipt = Path.Combine(path, "transaction.json");
            if (File.Exists(receipt)) Delete(root, receipt);
            Directory.Delete(path);
        }
        else
        {
            File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
            File.Delete(path);
        }
        Removed++;
        _log.Write("REMOVED " + path);
    }

    private void Prune(string root, string path)
    {
        try
        {
            if (!Directory.Exists(path)) return;
            SafePath(root, path);
            if (!Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path);
        }
        catch (Exception ex) { Problem(path, ex); }
    }

    public void Dispose() => _policy.Dispose();
}
