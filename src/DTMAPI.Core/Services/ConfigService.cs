using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    internal sealed class ConfigService : IConfigHelper
    {
        private readonly RuntimePaths paths;
        private readonly DiagnosticsService diagnostics;
        private readonly Dictionary<string, Delegate> migrations = new Dictionary<string, Delegate>(StringComparer.OrdinalIgnoreCase);

        public ConfigService(RuntimePaths paths, DiagnosticsService diagnostics)
        {
            this.paths = paths;
            this.diagnostics = diagnostics;
        }

        public TConfig ReadConfig<TConfig>(IManifest manifest) where TConfig : new()
        {
            string path = GetConfigPath(manifest);
            if (!File.Exists(path))
            {
                var defaultConfig = new TConfig();
                WriteConfig(manifest, defaultConfig);
                return defaultConfig;
            }

            TConfig config;
            try
            {
                config = JsonFile.Read<TConfig>(path);
            }
            catch (Exception ex)
            {
                string backupPath = BackupInvalidConfig(path, manifest, ex);
                var defaultConfig = new TConfig();
                WriteConfig(manifest, defaultConfig);
                diagnostics.RecordError(
                    manifest.UniqueID,
                    "配置 JSON 损坏，已备份并恢复默认配置。",
                    "Path=" + path + "; Backup=" + backupPath + "; Error=" + ex);
                return defaultConfig;
            }

            string key = GetMigrationKey(manifest, typeof(TConfig));
            if (migrations.TryGetValue(key, out Delegate migration) && migration is Action<TConfig> action)
            {
                action(config);
                WriteConfig(manifest, config);
            }
            return config;
        }

        public void WriteConfig<TConfig>(IManifest manifest, TConfig config)
        {
            string path = GetConfigPath(manifest);
            string dir = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(dir);
            string tempPath = Path.Combine(dir, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                JsonFile.Write(tempPath, config);
                string raw = File.ReadAllText(tempPath, new UTF8Encoding(false, true));
                File.WriteAllText(tempPath, JsonFile.Prettyish(raw), new UTF8Encoding(false));
                ReplaceWithTempFile(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        public string GetConfigPath(IManifest manifest)
        {
            string file = MakeSafeFileName(manifest.UniqueID) + ".json";
            return Path.Combine(paths.ConfigPath, file);
        }

        public void RegisterMigration<TConfig>(IManifest manifest, Action<TConfig> migrate) where TConfig : new()
        {
            migrations[GetMigrationKey(manifest, typeof(TConfig))] = migrate;
        }

        private static string GetMigrationKey(IManifest manifest, Type type) => manifest.UniqueID + "|" + type.FullName;

        private string BackupInvalidConfig(string path, IManifest manifest, Exception ex)
        {
            string backupPath = path + ".invalid-" + DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
            try
            {
                File.Copy(path, backupPath, overwrite: false);
                return backupPath;
            }
            catch (Exception backupEx)
            {
                diagnostics.RecordError(
                    manifest.UniqueID,
                    "配置 JSON 损坏，但备份失败。",
                    "Path=" + path + "; Backup=" + backupPath + "; ReadError=" + ex + "; BackupError=" + backupEx);
                return backupPath;
            }
        }

        private static void ReplaceWithTempFile(string tempPath, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, null);
            }
            catch
            {
                File.Delete(path);
                File.Move(tempPath, path);
            }
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "Unknown.Mod" : value;
        }
    }
}
