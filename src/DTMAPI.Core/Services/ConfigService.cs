using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    internal sealed class ConfigService : IConfigHelper
    {
        private readonly RuntimePaths paths;
        private readonly Dictionary<string, Delegate> migrations = new Dictionary<string, Delegate>(StringComparer.OrdinalIgnoreCase);

        public ConfigService(RuntimePaths paths)
        {
            this.paths = paths;
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

            TConfig config = JsonFile.Read<TConfig>(path);
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
            JsonFile.Write(path, config);
            string raw = File.ReadAllText(path);
            File.WriteAllText(path, JsonFile.Prettyish(raw));
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

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "Unknown.Mod" : value;
        }
    }
}
