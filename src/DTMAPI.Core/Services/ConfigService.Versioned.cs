using System;
using System.IO;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class ConfigService
    {
        internal IVersionedConfigHelper CreateVersioned(string owner, Action ensureActive) =>
            new VersionedConfig(new GlobalDataService(owner, Path.Combine(paths.ConfigPath, "versioned"), ensureActive));

        // Reuse the bounded owner envelope and atomic commit implementation in a distinct
        // Config root. Legacy config files and their Action migration contract stay intact.
        private sealed class VersionedConfig : IVersionedConfigHelper, IDisposable
        {
            private readonly GlobalDataService store;
            internal VersionedConfig(GlobalDataService store) { this.store = store; }
            public void RegisterMigration<T>(int fromSchemaVersion, Func<T, T> migrate) => store.RegisterMigration("settings", fromSchemaVersion, migrate);
            public OwnerDataResult<T> Read<T>(int schemaVersion, Func<T, string?>? validate = null) => store.ReadValidated("settings", schemaVersion, validate);
            public OwnerDataResult<bool> Write<T>(T value, int schemaVersion, Func<T, string?>? validate = null) => store.WriteValidated("settings", value, schemaVersion, validate);
            public void Dispose() => store.Dispose();
        }
    }
}
