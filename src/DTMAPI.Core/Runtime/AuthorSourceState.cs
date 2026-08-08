using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{
    internal enum AuthorSourceMode
    {
        PlayerWorkshop,
        LocalDevelopment,
        WorkshopValidation,
        PlayerReproduction
    }

    internal sealed class NativeWorkshopSubscription
    {
        public NativeWorkshopSubscription(
            ulong workshopId,
            string installPath,
            bool? nativeEnabled = null,
            int nativePriority = -1,
            string nativeOfficialId = "")
        {
            WorkshopId = workshopId;
            InstallPath = NormalizePath(installPath);
            NativeEnabled = nativeEnabled;
            NativePriority = nativePriority;
            NativeOfficialId = nativeOfficialId ?? string.Empty;
        }

        public ulong WorkshopId { get; }
        public string InstallPath { get; }
        public bool? NativeEnabled { get; }
        public int NativePriority { get; }
        public string NativeOfficialId { get; }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal sealed class NativeWorkshopSubscriptionSnapshot
    {
        private readonly Dictionary<ulong, NativeWorkshopSubscription> byId;

        private NativeWorkshopSubscriptionSnapshot(bool available, string source, string failure, DateTimeOffset capturedAtUtc, IEnumerable<NativeWorkshopSubscription> subscriptions)
        {
            Available = available;
            Source = source ?? string.Empty;
            Failure = failure ?? string.Empty;
            CapturedAtUtc = capturedAtUtc;
            byId = (subscriptions ?? Array.Empty<NativeWorkshopSubscription>())
                .Where(row => row.WorkshopId != 0)
                .GroupBy(row => row.WorkshopId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        public bool Available { get; }
        public string Source { get; }
        public string Failure { get; }
        public DateTimeOffset CapturedAtUtc { get; }
        public IReadOnlyList<NativeWorkshopSubscription> Subscriptions => byId.Values.OrderBy(row => row.WorkshopId).ToArray();
        public int Count => byId.Count;

        public bool TryGet(ulong workshopId, out NativeWorkshopSubscription subscription) => byId.TryGetValue(workshopId, out subscription);

        public static NativeWorkshopSubscriptionSnapshot Unavailable(string failure) =>
            new NativeWorkshopSubscriptionSnapshot(false, "DolocTown.Config.ModManager.GetSubscribedMods", failure, DateTimeOffset.UtcNow, Array.Empty<NativeWorkshopSubscription>());

        public static NativeWorkshopSubscriptionSnapshot Captured(string source, string failure, IEnumerable<NativeWorkshopSubscription> subscriptions) =>
            new NativeWorkshopSubscriptionSnapshot(true, source, failure, DateTimeOffset.UtcNow, subscriptions);

        public string FormatSummary()
        {
            return "available=" + Available.ToString(CultureInfo.InvariantCulture) +
                "; count=" + Count.ToString(CultureInfo.InvariantCulture) +
                "; source=" + Source +
                "; capturedAtUtc=" + CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture) +
                "; failure=" + (string.IsNullOrWhiteSpace(Failure) ? "none" : Failure);
        }
    }

    internal sealed class AuthorSourceSelectionState
    {
        private readonly Dictionary<string, AuthorSourceSelection> selections;

        public AuthorSourceSelectionState(
            string gameRoot,
            bool playerReproductionActive,
            string reproductionSnapshotId,
            IEnumerable<AuthorSourceSelection> selections,
            string sourcePath,
            string loadFailure)
        {
            GameRoot = NormalizePath(gameRoot);
            PlayerReproductionActive = playerReproductionActive;
            ReproductionSnapshotId = reproductionSnapshotId ?? string.Empty;
            SourcePath = sourcePath ?? string.Empty;
            LoadFailure = loadFailure ?? string.Empty;
            this.selections = (selections ?? Array.Empty<AuthorSourceSelection>())
                .Where(row => !string.IsNullOrWhiteSpace(row.UniqueId))
                .GroupBy(row => row.UniqueId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        }

        public string GameRoot { get; }
        public bool PlayerReproductionActive { get; }
        public string ReproductionSnapshotId { get; }
        public string SourcePath { get; }
        public string LoadFailure { get; }
        public IReadOnlyList<AuthorSourceSelection> Selections => selections.Values.OrderBy(row => row.UniqueId, StringComparer.OrdinalIgnoreCase).ToArray();

        public AuthorSourceMode GetMode(string uniqueId)
        {
            if (PlayerReproductionActive)
                return AuthorSourceMode.PlayerReproduction;
            return selections.TryGetValue(uniqueId ?? string.Empty, out AuthorSourceSelection selection)
                ? selection.Mode
                : AuthorSourceMode.PlayerWorkshop;
        }

        public bool TryGetSelection(string uniqueId, out AuthorSourceSelection selection) => selections.TryGetValue(uniqueId ?? string.Empty, out selection);

        public string FormatSummary()
        {
            return "statePath=" + SourcePath +
                "; playerReproduction=" + PlayerReproductionActive.ToString(CultureInfo.InvariantCulture) +
                "; snapshotId=" + (string.IsNullOrWhiteSpace(ReproductionSnapshotId) ? "none" : ReproductionSnapshotId) +
                "; selections=" + selections.Count.ToString(CultureInfo.InvariantCulture) +
                "; loadFailure=" + (string.IsNullOrWhiteSpace(LoadFailure) ? "none" : LoadFailure);
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal sealed class AuthorSourceSelection
    {
        public AuthorSourceSelection(string uniqueId, AuthorSourceMode mode, string sourcePath, string expectedTreeSha256)
        {
            UniqueId = uniqueId ?? string.Empty;
            Mode = mode;
            SourcePath = NormalizePath(sourcePath);
            ExpectedTreeSha256 = (expectedTreeSha256 ?? string.Empty).Trim().ToUpperInvariant();
        }

        public string UniqueId { get; }
        public AuthorSourceMode Mode { get; }
        public string SourcePath { get; }
        public string ExpectedTreeSha256 { get; }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal sealed class AuthorSourceSelectionDecision
    {
        public AuthorSourceSelectionDecision(
            string uniqueId,
            AuthorSourceMode mode,
            string outcome,
            DiscoveredMod? selected,
            IEnumerable<DiscoveredMod> candidates)
        {
            UniqueId = uniqueId ?? string.Empty;
            Mode = mode;
            Outcome = outcome ?? string.Empty;
            Selected = selected;
            Candidates = (candidates ?? Array.Empty<DiscoveredMod>())
                .OrderBy(candidate => candidate.Source, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public string UniqueId { get; }
        public AuthorSourceMode Mode { get; }
        public string Outcome { get; }
        public DiscoveredMod? Selected { get; }
        public IReadOnlyList<DiscoveredMod> Candidates { get; }
    }

    internal static class AuthorSourceStateStore
    {
        internal const int SchemaVersion = 1;

        public static string GetInstallationStateRoot(string gameRoot)
        {
            string configured = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT") ?? string.Empty;
            string baseRoot = !string.IsNullOrWhiteSpace(configured)
                ? Path.GetFullPath(configured)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DTMAPI", "AuthorSdk", "state");
            return Path.Combine(baseRoot, "installations", ComputeGameRootKey(gameRoot));
        }

        public static string GetSourceStatePath(string gameRoot) => Path.Combine(GetInstallationStateRoot(gameRoot), "source-state.json");

        public static string GetWorkshopSnapshotPath(string gameRoot) => Path.Combine(GetInstallationStateRoot(gameRoot), "workshop-subscriptions.json");

        public static string GetAuthorSessionPath(string gameRoot) => Path.Combine(GetInstallationStateRoot(gameRoot), "author-session.json");

        public static AuthorSourceSelectionState Load(string gameRoot)
        {
            string canonicalRoot = NormalizePath(gameRoot);
            string path = GetSourceStatePath(canonicalRoot);
            if (!File.Exists(path))
                return new AuthorSourceSelectionState(canonicalRoot, false, string.Empty, Array.Empty<AuthorSourceSelection>(), path, string.Empty);

            try
            {
                SourceStateFile model;
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SourceStateFile));
                    model = (SourceStateFile)(serializer.ReadObject(stream) ?? new SourceStateFile());
                }

                if (model.SchemaVersion != SchemaVersion)
                    throw new InvalidDataException("unsupported schemaVersion=" + model.SchemaVersion.ToString(CultureInfo.InvariantCulture));
                if (!PathsEqual(canonicalRoot, model.GameRoot))
                    throw new InvalidDataException("gameRoot does not match this installation");

                var selections = new List<AuthorSourceSelection>();
                foreach (SourceSelectionFile row in model.Selections ?? new List<SourceSelectionFile>())
                {
                    if (!TryParseMode(row.Mode, out AuthorSourceMode mode) || mode == AuthorSourceMode.PlayerReproduction)
                        continue;
                    selections.Add(new AuthorSourceSelection(row.UniqueId, mode, row.SourcePath, row.ExpectedTreeSha256));
                }

                return new AuthorSourceSelectionState(canonicalRoot, model.PlayerReproductionActive, model.ReproductionSnapshotId, selections, path, string.Empty);
            }
            catch (Exception ex)
            {
                return new AuthorSourceSelectionState(canonicalRoot, false, string.Empty, Array.Empty<AuthorSourceSelection>(), path, ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void WriteWorkshopSnapshot(string gameRoot, NativeWorkshopSubscriptionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            string canonicalRoot = NormalizePath(gameRoot);
            string path = GetWorkshopSnapshotPath(canonicalRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var model = new WorkshopSnapshotFile
            {
                SchemaVersion = SchemaVersion,
                GameRoot = canonicalRoot,
                Available = snapshot.Available,
                NativeOwner = snapshot.Source,
                Failure = snapshot.Failure,
                CapturedAtUtc = snapshot.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                Subscriptions = snapshot.Subscriptions.Select(row => new WorkshopSubscriptionFile
                {
                    WorkshopId = row.WorkshopId.ToString(CultureInfo.InvariantCulture),
                    InstallPath = row.InstallPath,
                    NativeInfoAvailable = row.NativeEnabled.HasValue,
                    NativeEnabled = row.NativeEnabled ?? false,
                    NativePriority = row.NativePriority,
                    NativeOfficialId = row.NativeOfficialId
                }).ToList()
            };
            WriteAtomic(path, model, typeof(WorkshopSnapshotFile));
        }

        public static string ComputeGameRootKey(string gameRoot)
        {
            string canonical = NormalizePath(gameRoot).ToUpperInvariant();
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(32);
                for (int i = 0; i < 16; i++)
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void WriteAtomic(string path, object value, Type type)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var serializer = new DataContractJsonSerializer(type);
                    serializer.WriteObject(stream, value);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    string backup = path + ".replace-backup";
                    try
                    {
                        File.Replace(temporary, path, backup, true);
                        if (File.Exists(backup))
                            File.Delete(backup);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(path);
                        File.Move(temporary, path);
                    }
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

        private static bool TryParseMode(string text, out AuthorSourceMode mode)
        {
            string value = (text ?? string.Empty).Replace("/", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            if (value.Equals("PlayerWorkshop", StringComparison.OrdinalIgnoreCase))
            {
                mode = AuthorSourceMode.PlayerWorkshop;
                return true;
            }
            if (value.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase))
            {
                mode = AuthorSourceMode.LocalDevelopment;
                return true;
            }
            if (value.Equals("WorkshopValidation", StringComparison.OrdinalIgnoreCase))
            {
                mode = AuthorSourceMode.WorkshopValidation;
                return true;
            }
            if (value.Equals("PlayerReproduction", StringComparison.OrdinalIgnoreCase))
            {
                mode = AuthorSourceMode.PlayerReproduction;
                return true;
            }

            mode = AuthorSourceMode.PlayerWorkshop;
            return false;
        }

        private static bool PathsEqual(string left, string right) => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        [DataContract]
        private sealed class SourceStateFile
        {
            [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
            [DataMember(Name = "gameRoot")] public string GameRoot { get; set; } = string.Empty;
            [DataMember(Name = "playerReproductionActive")] public bool PlayerReproductionActive { get; set; }
            [DataMember(Name = "reproductionSnapshotId")] public string ReproductionSnapshotId { get; set; } = string.Empty;
            [DataMember(Name = "selections")] public List<SourceSelectionFile> Selections { get; set; } = new List<SourceSelectionFile>();
        }

        [DataContract]
        private sealed class SourceSelectionFile
        {
            [DataMember(Name = "uniqueId")] public string UniqueId { get; set; } = string.Empty;
            [DataMember(Name = "mode")] public string Mode { get; set; } = string.Empty;
            [DataMember(Name = "sourcePath")] public string SourcePath { get; set; } = string.Empty;
            [DataMember(Name = "expectedTreeSha256")] public string ExpectedTreeSha256 { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class WorkshopSnapshotFile
        {
            [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
            [DataMember(Name = "gameRoot")] public string GameRoot { get; set; } = string.Empty;
            [DataMember(Name = "available")] public bool Available { get; set; }
            [DataMember(Name = "nativeOwner")] public string NativeOwner { get; set; } = string.Empty;
            [DataMember(Name = "failure")] public string Failure { get; set; } = string.Empty;
            [DataMember(Name = "capturedAtUtc")] public string CapturedAtUtc { get; set; } = string.Empty;
            [DataMember(Name = "subscriptions")] public List<WorkshopSubscriptionFile> Subscriptions { get; set; } = new List<WorkshopSubscriptionFile>();
        }

        [DataContract]
        private sealed class WorkshopSubscriptionFile
        {
            [DataMember(Name = "workshopId")] public string WorkshopId { get; set; } = string.Empty;
            [DataMember(Name = "installPath")] public string InstallPath { get; set; } = string.Empty;
            [DataMember(Name = "nativeInfoAvailable")] public bool NativeInfoAvailable { get; set; }
            [DataMember(Name = "nativeEnabled")] public bool NativeEnabled { get; set; }
            [DataMember(Name = "nativePriority")] public int NativePriority { get; set; }
            [DataMember(Name = "nativeOfficialId")] public string NativeOfficialId { get; set; } = string.Empty;
        }
    }
}
