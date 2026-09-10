using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed class GlobalDataService : IGlobalDataHelper, IDisposable
    {
        private const int FormatVersion = 1;
        private const int MaxEnvelopeBytes = 4 * 1024 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly string owner;
        private readonly string root;
        private readonly Action ensureActive;
        private readonly GlobalDataFileOperations files;
        private readonly Dictionary<string, Dictionary<int, Migration>> migrations = new Dictionary<string, Dictionary<int, Migration>>(StringComparer.Ordinal);
        private bool busy;
        private bool disposed;

        internal GlobalDataService(string owner, string dataRoot, Action ensureActive, GlobalDataFileOperations? files = null)
        {
            this.owner = owner.ToLowerInvariant();
            root = Path.Combine(dataRoot, Digest(this.owner));
            this.ensureActive = ensureActive;
            this.files = files ?? new GlobalDataFileOperations();
        }

        public void RegisterMigration<T>(string key, int fromSchemaVersion, Func<T, T> migrate)
        {
            CheckActive();
            key = Validate(key, fromSchemaVersion);
            if (migrate == null) throw new ArgumentNullException(nameof(migrate));
            if (!migrations.TryGetValue(key, out var chain)) migrations.Add(key, chain = new Dictionary<int, Migration>());
            if (chain.ContainsKey(fromSchemaVersion)) throw new InvalidOperationException("A migration is already registered for this key and schema.");
            chain.Add(fromSchemaVersion, new Migration(typeof(T), json => Serialize(migrate(Deserialize<T>(json)))));
        }

        public OwnerDataResult<T> ReadGlobal<T>(string key, int schemaVersion) => ReadValidated<T>(key, schemaVersion, null);

        internal OwnerDataResult<T> ReadValidated<T>(string key, int schemaVersion, Func<T, string?>? validate)
        {
            CheckActive();
            key = Validate(key, schemaVersion);
            busy = true;
            try
            {
                Envelope? envelope = ReadEnvelope(key, schemaVersion, out string? original, out var status);
                if (status != OwnerDataStatus.Found) return Result<T>(status);
                string payload = envelope!.Payload;
                int version = envelope.SchemaVersion;
                while (version < schemaVersion)
                {
                    if (!migrations.TryGetValue(key, out var chain) || !chain.TryGetValue(version, out var migration) || migration.Type != typeof(T))
                        return Result<T>(OwnerDataStatus.MigrationRequired, version, "Register every successive migration for this key and data model.");
                    try { payload = migration.Apply(payload); }
                    catch (Exception ex) when (!(ex is ObjectDisposedException))
                    { return Result<T>(OwnerDataStatus.MigrationFailed, version, "Migration failed; original data was retained."); }
                    EnsureStillActive();
                    version++;
                }
                T value;
                try { value = Deserialize<T>(payload); }
                catch (Exception ex) when (IsDataError(ex)) { return Result<T>(OwnerDataStatus.Corrupt, version, "Payload does not match the requested data model."); }
                EnsureStillActive();
                string? validationError = validate == null ? null : ValidateCandidate(Deserialize<T>(payload), validate);
                EnsureStillActive();
                if (validationError != null) return Result<T>(OwnerDataStatus.ValidationFailed, version, validationError);
                if (version != envelope.SchemaVersion) Commit(key, CreateEnvelope(key, version, payload), original);
                return new OwnerDataResult<T>(OwnerDataStatus.Found, value, version);
            }
            catch (UnauthorizedAccessException) { return Result<T>(OwnerDataStatus.AccessDenied); }
            catch (IOException) { return Result<T>(OwnerDataStatus.IoError, message: "Global data IO failed; retry after resolving the filesystem error."); }
            finally { busy = false; }
        }

        public OwnerDataResult<bool> WriteGlobal<T>(string key, T value, int schemaVersion) => WriteValidated(key, value, schemaVersion, null);

        internal OwnerDataResult<bool> WriteValidated<T>(string key, T value, int schemaVersion, Func<T, string?>? validate)
        {
            CheckActive();
            key = Validate(key, schemaVersion);
            busy = true;
            try
            {
                var existing = ReadEnvelope(key, schemaVersion, out string? original, out var status);
                if (status != OwnerDataStatus.Found && status != OwnerDataStatus.Missing) return Result<bool>(status);
                if (existing != null && existing.SchemaVersion < schemaVersion) return Result<bool>(OwnerDataStatus.MigrationRequired, existing.SchemaVersion);
                if (existing != null)
                {
                    try { Deserialize<T>(existing.Payload); }
                    catch (Exception ex) when (IsDataError(ex)) { return Result<bool>(OwnerDataStatus.Corrupt, existing.SchemaVersion); }
                    EnsureStillActive();
                }
                string payload = Serialize(value);
                EnsureStillActive();
                string? validationError = validate == null ? null : ValidateCandidate(Deserialize<T>(payload), validate);
                EnsureStillActive();
                if (validationError != null) return Result<bool>(OwnerDataStatus.ValidationFailed, schemaVersion, validationError);
                Commit(key, CreateEnvelope(key, schemaVersion, payload), original);
                return new OwnerDataResult<bool>(OwnerDataStatus.Found, true, schemaVersion);
            }
            catch (UnauthorizedAccessException) { return Result<bool>(OwnerDataStatus.AccessDenied); }
            catch (IOException) { return Result<bool>(OwnerDataStatus.IoError); }
            finally { busy = false; }
        }

        public OwnerDataResult<bool> DeleteGlobal(string key, int schemaVersion)
        {
            CheckActive();
            key = Validate(key, schemaVersion);
            busy = true;
            try
            {
                ReadEnvelope(key, schemaVersion, out string? original, out var status);
                if (status != OwnerDataStatus.Found) return Result<bool>(status);
                EnsureStillActive();
                string path = OwnerFilePath.Resolve(root, key + ".json");
                if (files.Read(path) != original) return Result<bool>(OwnerDataStatus.IoError, message: "Data changed before deletion; retry.");
                File.Delete(path);
                return new OwnerDataResult<bool>(OwnerDataStatus.Found, true, schemaVersion);
            }
            catch (UnauthorizedAccessException) { return Result<bool>(OwnerDataStatus.AccessDenied); }
            catch (IOException) { return Result<bool>(OwnerDataStatus.IoError); }
            finally { busy = false; }
        }

        private Envelope? ReadEnvelope(string key, int expectedSchema, out string? original, out OwnerDataStatus status)
        {
            original = null;
            status = OwnerDataStatus.Missing;
            string path = OwnerFilePath.Resolve(root, key + ".json");
            try { original = files.Read(path); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
            catch (DecoderFallbackException) { status = OwnerDataStatus.Corrupt; return null; }
            catch (InvalidDataException) { status = OwnerDataStatus.Corrupt; return null; }
            Envelope envelope;
            try { envelope = Deserialize<Envelope>(original); }
            catch (Exception ex) when (IsDataError(ex)) { status = OwnerDataStatus.Corrupt; return null; }
            if (envelope == null || envelope.Format <= 0 || envelope.SchemaVersion <= 0 ||
                envelope.Owner != owner || envelope.Key != key || envelope.Payload == null || envelope.Sha256 == null)
            { status = OwnerDataStatus.Corrupt; return null; }
            if (envelope.Format != FormatVersion || envelope.SchemaVersion > expectedSchema)
            { status = OwnerDataStatus.UnsupportedSchema; return null; }
            if (Utf8.GetByteCount(envelope.Payload) > OwnerFileService.MaxBytes || envelope.Sha256 != Digest(envelope.Payload))
            { status = OwnerDataStatus.Corrupt; return null; }
            status = OwnerDataStatus.Found;
            return envelope;
        }

        private void Commit(string key, Envelope envelope, string? original)
        {
            EnsureStillActive();
            string json = Serialize(envelope, MaxEnvelopeBytes);
            string path = OwnerFilePath.Resolve(root, key + ".json", createParents: true);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = Utf8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                EnsureStillActive();
                OwnerFilePath.Resolve(root, key + ".json");
                if (original != null && files.Read(path) != original) throw new IOException("Global data changed before commit.");
                // File.Move refuses an unexpected existing destination; File.Replace preserves
                // the previous destination if replacement itself fails. No delete/write fallback.
                files.Commit(temporary, path, original != null);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public void Dispose() { disposed = true; migrations.Clear(); }
        private static string? ValidateCandidate<T>(T value, Func<T, string?>? validate)
        {
            if (validate == null) return null;
            string? error;
            try { error = validate(value); }
            catch (Exception ex) when (!(ex is ObjectDisposedException)) { error = "Validation callback failed."; }
            if (string.IsNullOrEmpty(error)) return null;
            return error!.Length > 512 ? error.Substring(0, 512) : error;
        }
        private void CheckActive()
        {
            EnsureStillActive();
            if (busy) throw new InvalidOperationException("Reentrant global data operations are not supported.");
        }
        private void EnsureStillActive()
        {
            ensureActive();
            if (disposed) throw new ObjectDisposedException(nameof(GlobalDataService));
        }
        private static string Validate(string key, int schema)
        {
            if (schema < 1) throw new ArgumentOutOfRangeException(nameof(schema), "Schema versions start at 1.");
            return OwnerFilePath.NormalizeKey(key);
        }
        private Envelope CreateEnvelope(string key, int schema, string payload) => new Envelope
        { Format = FormatVersion, Owner = owner, Key = key, SchemaVersion = schema, Payload = payload, Sha256 = Digest(payload) };
        private static OwnerDataResult<T> Result<T>(OwnerDataStatus status, int schema = 0, string message = "") => new OwnerDataResult<T>(status, default, schema, message);
        private static bool IsDataError(Exception ex) => ex is SerializationException || ex is System.Xml.XmlException || ex is FormatException || ex is ArgumentException;
        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
        private static string Serialize<T>(T value, int maximumBytes = OwnerFileService.MaxBytes)
        {
            using (var stream = new BoundedJsonStream(maximumBytes))
            {
                Serializer<T>().WriteObject(stream, value);
                if (stream.Length > maximumBytes) throw new ArgumentException("Global JSON data exceeds the size limit.");
                return Utf8.GetString(stream.ToArray());
            }
        }
        private static T Deserialize<T>(string json)
        {
            using (var stream = new MemoryStream(Utf8.GetBytes(json))) return (T)Serializer<T>().ReadObject(stream);
        }
        private static DataContractJsonSerializer Serializer<T>() => new DataContractJsonSerializer(typeof(T),
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true, EmitTypeInformation = EmitTypeInformation.Never });

        [DataContract]
        private sealed class Envelope
        {
            [DataMember(Name = "format", IsRequired = true)] public int Format;
            [DataMember(Name = "owner", IsRequired = true)] public string Owner = "";
            [DataMember(Name = "key", IsRequired = true)] public string Key = "";
            [DataMember(Name = "schema", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "payload", IsRequired = true)] public string Payload = "";
            [DataMember(Name = "sha256", IsRequired = true)] public string Sha256 = "";
        }
        private sealed class Migration
        {
            internal Migration(Type type, Func<string, string> apply) { Type = type; Apply = apply; }
            internal Type Type { get; }
            internal Func<string, string> Apply { get; }
        }

        private sealed class BoundedJsonStream : MemoryStream
        {
            private readonly int maximum;
            internal BoundedJsonStream(int maximum) { this.maximum = maximum; }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Position + count > maximum) throw new ArgumentException("Global JSON data exceeds the size limit.");
                base.Write(buffer, offset, count);
            }
            public override void WriteByte(byte value)
            {
                if (Position >= maximum) throw new ArgumentException("Global JSON data exceeds the size limit.");
                base.WriteByte(value);
            }
        }

        internal static string ReadBounded(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > MaxEnvelopeBytes) throw new InvalidDataException("Global envelope is too large.");
                using (var reader = new StreamReader(stream, Utf8, false)) return reader.ReadToEnd();
            }
        }
    }

    internal sealed class GlobalDataFileOperations
    {
        internal Func<string, string> Read = GlobalDataService.ReadBounded;
        internal Action<string, string, bool> Commit = (temporary, path, exists) =>
        { if (exists) File.Replace(temporary, path, null); else File.Move(temporary, path); };
    }
}
