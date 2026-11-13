using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal sealed class UninstallCleanup
{
    private static readonly string[] ScratchPrefixes =
    {
        ".dtmapi-runtime-install-", ".dtmapi-multiplatform-install-",
        ".dtmapi-multiplatform-genesis-", ".dtmapi-multiplatform-bepinex-source-"
    };
    private readonly string _game;
    private readonly string _equipment;
    private readonly InstallerLog _log;
    public List<string> Problems { get; } = new();
    public List<string> OtherPlugins { get; } = new();
    public bool PreservedEquipment { get; private set; }
    public int Removed { get; private set; }

    public UninstallCleanup(string game, InstallerLog log)
    {
        _game = FileSystemSafety.NormalizeRoot(game);
        _equipment = Path.Combine(_game, "DTMAPI", "config", "protected-items", "equipment-slots");
        _log = log;
    }

    public void Execute()
    {
        FindOtherPlugins("BepInEx/plugins", excludeDtmApi: true);
        FindOtherPlugins("BepInEx/patchers", excludeDtmApi: false);
        bool removeBepInEx = OtherPlugins.Count == 0 && Problems.Count == 0;

        Delete(Path.Combine(_game, "DTMAPI"));
        Delete(Path.Combine(_game, "BepInEx", "plugins", "DTMAPI"));
        foreach (string path in ReadEntries(Path.Combine(_game, "BepInEx", "config")))
            if (File.Exists(path) && Path.GetFileName(path).StartsWith("dev.dtmapi.", StringComparison.OrdinalIgnoreCase))
                Delete(path);
        foreach (string path in ReadEntries(_game))
            if (Directory.Exists(path) && ScratchPrefixes.Any(prefix => Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                Delete(path);

        if (removeBepInEx)
        {
            Delete(Path.Combine(_game, "BepInEx"));
            foreach (string name in new[] { "winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt" })
                Delete(Path.Combine(_game, name));
        }
    }

    private void FindOtherPlugins(string relative, bool excludeDtmApi)
    {
        foreach (string path in ReadEntries(Path.Combine(_game, relative)))
            if (!excludeDtmApi || !string.Equals(Path.GetFileName(path), "DTMAPI", FileSystemSafety.PathComparison))
                OtherPlugins.Add(Path.GetRelativePath(_game, path));
    }

    private string[] ReadEntries(string directory)
    {
        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_game, directory);
            return Directory.Exists(directory) ? Directory.GetFileSystemEntries(directory) : Array.Empty<string>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Problem(directory, ex);
            return Array.Empty<string>();
        }
    }

    private void Delete(string path)
    {
        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_game, path);
            if (!FileSystemSafety.EntryExistsIncludingLink(path)) return;
            if (string.Equals(path, _equipment, FileSystemSafety.PathComparison))
            {
                PreservedEquipment = true;
                _log.Write("保留了旧装备文件：" + path);
                return;
            }
            if (Directory.Exists(path))
            {
                foreach (string child in Directory.GetFileSystemEntries(path)) Delete(child);
                if (Directory.EnumerateFileSystemEntries(path).Any()) return;
                Directory.Delete(path);
            }
            else
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                File.Delete(path);
            }
            Removed++;
            _log.Write("已删除：" + path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Problem(path, ex);
        }
    }

    private void Problem(string path, Exception error)
    {
        string detail = path + " | " + error.Message;
        Problems.Add(detail);
        _log.Write("未删除：" + detail);
    }
}
