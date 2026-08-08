using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
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
        private readonly Dictionary<MigrationKey, Delegate> migrations = new Dictionary<MigrationKey, Delegate>();

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

            MigrationKey key = GetMigrationKey(manifest, typeof(TConfig));
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
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            if (migrate == null)
                throw new ArgumentNullException(nameof(migrate));
            MigrationKey key = GetMigrationKey(manifest, typeof(TConfig));
            if (migrations.ContainsKey(key))
                throw new InvalidOperationException("Owner '" + manifest.UniqueID + "' already registered a config migration for '" + (typeof(TConfig).FullName ?? typeof(TConfig).Name) + "'.");
            migrations.Add(key, migrate);
        }

        internal IConfigHelper CreateOwnerBound(IManifest owner, Action ensureOwnerActive)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ensureOwnerActive == null)
                throw new ArgumentNullException(nameof(ensureOwnerActive));
            return new OwnerBoundConfigHelper(this, owner, ensureOwnerActive);
        }

        internal int RemoveOwner(string uniqueId)
        {
            int removed = 0;
            foreach (MigrationKey key in migrations.Keys.Where(k => k.OwnerId.Equals(uniqueId ?? string.Empty, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                if (migrations.Remove(key))
                    removed++;
            }
            return removed;
        }

        internal int CountOwner(string uniqueId)
        {
            return migrations.Keys.Count(k => k.OwnerId.Equals(uniqueId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        internal int TotalMigrationCount => migrations.Count;

        private static MigrationKey GetMigrationKey(IManifest manifest, Type type) => new MigrationKey(manifest.UniqueID, type);

        private readonly struct MigrationKey : IEquatable<MigrationKey>
        {
            public MigrationKey(string ownerId, Type configType)
            {
                OwnerId = ownerId ?? string.Empty;
                ConfigType = configType ?? throw new ArgumentNullException(nameof(configType));
            }

            public string OwnerId { get; }

            public Type ConfigType { get; }

            public bool Equals(MigrationKey other) =>
                StringComparer.OrdinalIgnoreCase.Equals(OwnerId, other.OwnerId) && ConfigType == other.ConfigType;

            public override bool Equals(object? obj) => obj is MigrationKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerId) * 397) ^ ConfigType.GetHashCode();
                }
            }
        }

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

            // Never trade atomic replacement for a delete-then-move fallback. If the
            // platform can't replace the destination atomically, leave the last-good
            // config in place and let the caller observe the write failure. The
            // temporary candidate is removed by WriteConfig's finally block.
            File.Replace(tempPath, path, null);
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "Unknown.Mod" : value;
        }

        private sealed class OwnerBoundConfigHelper : IConfigHelper
        {
            private readonly ConfigService inner;
            private readonly IManifest owner;
            private readonly Action ensureOwnerActive;

            public OwnerBoundConfigHelper(ConfigService inner, IManifest owner, Action ensureOwnerActive)
            {
                this.inner = inner;
                this.owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public TConfig ReadConfig<TConfig>(IManifest manifest) where TConfig : new()
            {
                EnsureOwner(manifest);
                ensureOwnerActive();
                return inner.ReadConfig<TConfig>(owner);
            }

            public void WriteConfig<TConfig>(IManifest manifest, TConfig config)
            {
                EnsureOwner(manifest);
                ensureOwnerActive();
                inner.WriteConfig(owner, config);
            }

            public string GetConfigPath(IManifest manifest)
            {
                EnsureOwner(manifest);
                ensureOwnerActive();
                return inner.GetConfigPath(owner);
            }

            public void RegisterMigration<TConfig>(IManifest manifest, Action<TConfig> migrate) where TConfig : new()
            {
                EnsureOwner(manifest);
                ensureOwnerActive();
                inner.RegisterMigration(owner, migrate);
            }

            private void EnsureOwner(IManifest manifest)
            {
                if (manifest == null || !string.Equals(manifest.UniqueID, owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Config helper for owner '" + owner.UniqueID + "' can't access another owner manifest.");
            }
        }
    }
}
