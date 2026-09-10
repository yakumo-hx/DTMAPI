using System;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public enum OwnerDataStatus { Found, Missing, Corrupt, UnsupportedSchema, MigrationRequired, MigrationFailed, AccessDenied, IoError, ValidationFailed }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public sealed class OwnerDataResult<T>
    {
        public OwnerDataResult(OwnerDataStatus status, T? value, int schemaVersion = 0, string message = "")
        { Status = status; Value = value; SchemaVersion = schemaVersion; Message = message; }
        public OwnerDataStatus Status { get; }
        public T? Value { get; }
        public int SchemaVersion { get; }
        public string Message { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Read-only files from this Mod's selected package; runtime-thread and active-owner only. Relative keys, strict UTF-8 and bounded file size.")]
    public interface IOwnerFileHelper
    {
        OwnerDataResult<string> ReadText(string key);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Local global data, independent of gameplay saves and package lifetime. Found indicates a successful read/write/delete; no native save or Cloud guarantee.")]
    public interface IGlobalDataHelper
    {
        OwnerDataResult<T> ReadGlobal<T>(string key, int schemaVersion);
        OwnerDataResult<bool> WriteGlobal<T>(string key, T value, int schemaVersion);
        OwnerDataResult<bool> DeleteGlobal(string key, int schemaVersion);
        void RegisterMigration<T>(string key, int fromSchemaVersion, Func<T, T> migrate);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public static class OwnerFileServiceExtensions
    {
        public static IOwnerFileHelper GetOwnerFiles(this IDtmHelper helper) => helper.GetRequiredService<IOwnerFileHelper>("0.6.2", "0.6.2");
        public static IGlobalDataHelper GetGlobalData(this IDtmHelper helper) => helper.GetRequiredService<IGlobalDataHelper>("0.6.2", "0.6.2");
    }
}
