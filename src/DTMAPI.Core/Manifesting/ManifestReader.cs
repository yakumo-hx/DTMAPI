using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Text;
using DTMAPI.Internal.Authoring;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Manifesting
{
    internal sealed class ManifestReader
    {
        public ManifestModel Read(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string json;
            using (var stream = new MemoryStream(bytes))
            using (var text = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true))
                json = text.ReadToEnd();
            IReadOnlyList<string> properties = PackageDependencyContract.Object(PackageDependencyContract.ReadJson(bytes)).Keys.ToArray();
            ManifestModel model = JsonFile.Deserialize<ManifestModel>(json);
            model.Normalize();
            // Legacy format selector is only an ownership convention, never a proof requirement.
            model.NativeContractSelected = properties.Contains("NativeContractVersion");
            if (model.ManifestVersion != 0 && model.ManifestVersion != 2)
                throw new InvalidDataException("manifest-format-unsupported: " + model.ManifestVersion + "; update DTMAPI.");
            if (Version.TryParse(model.MinimumDTMApiVersion, out var minimum) && minimum > Version.Parse(DtmApiBuildVersion.ReleaseVersion))
                return model; // Keep a visible api-too-new row; do not parse a newer runtime contract.
            byte[]? infoBytes = null;
            if (model.ManifestVersion == 2)
            {
                string root = RuntimePackage.FindRoot(path);
                string info = Path.Combine(root, "info.json");
                if (File.Exists(info))
                {
                    infoBytes = File.ReadAllBytes(info);
                    var fields = PackageDocumentFacts.ReadInfo(infoBytes, out string warning);
                    model.InfoReadWarning = warning;
                    string Field(string key) => fields.TryGetValue(key, out var value) ? value : "";
                    model.Name = Field("name"); model.Author = Field("author");
                    model.Description = Field("description"); model.Version = Field("version");
                }
                else { model.Name = ""; model.Author = ""; model.Description = ""; model.Version = ""; }
            }
            model.DependencyContract = PackageDependencyContract.ReadManifest(bytes);
            if (properties.Any(property => property.Equals("ContentPackFor", StringComparison.OrdinalIgnoreCase)))
                model.ContentPackBinding = ContentPackBinding.Read(bytes);
            model.Normalize();
            if (string.IsNullOrWhiteSpace(model.UniqueID))
                throw new InvalidDataException("manifest 缺少 UniqueID。");
            if (string.IsNullOrWhiteSpace(model.Name))
                model.Name = model.UniqueID;
            if (string.IsNullOrWhiteSpace(model.Author))
                model.Author = "Unknown";
            return model;
        }

    }

    internal sealed class ModScanner
    {
        private readonly RuntimePaths paths;
        private readonly ManifestReader reader = new ManifestReader();
        private readonly ManagedModClassifier classifier;
        private readonly StartupPackageReadTransaction? packageReads;
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<AuthorSourceSelectionDecision> sourceSelectionDecisions = new List<AuthorSourceSelectionDecision>();
        private readonly NativeWorkshopSubscriptionSnapshot workshopSubscriptions;
        private readonly AuthorSourceSelectionState authorSourceState;

        public ModScanner(
            RuntimePaths paths,
            NativeWorkshopSubscriptionSnapshot? workshopSubscriptions = null,
            AuthorSourceSelectionState? authorSourceState = null,
            StartupPackageReadTransaction? packageReads = null)
        {
            this.paths = paths;
            this.packageReads = packageReads;
            classifier = new ManagedModClassifier();
            this.workshopSubscriptions = workshopSubscriptions ?? NativeWorkshopSubscriptionSnapshot.Unavailable("snapshot not supplied");
            this.authorSourceState = authorSourceState ?? new AuthorSourceSelectionState(paths.GamePath, false, string.Empty, Array.Empty<AuthorSourceSelection>(), string.Empty, string.Empty);
        }

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public string OfficialLocalModsRoot { get; private set; } = string.Empty;
        public bool OfficialLocalModsRootExists { get; private set; }
        public int OfficialLocalDirectoryCount { get; private set; }
        public string OfficialEnablementFilePath { get; private set; } = string.Empty;
        public bool OfficialEnablementFileExists { get; private set; }
        public int OfficialEnablementEntryCount { get; private set; }
        public long ManifestScanElapsedMilliseconds { get; private set; }
        public long OfficialModsScanElapsedMilliseconds { get; private set; }
        public long WorkshopScanElapsedMilliseconds { get; private set; }
        public long ContentQueryElapsedMilliseconds { get; private set; }
        public string SourceSelectionSummary { get; private set; } = string.Empty;
        public IReadOnlyList<AuthorSourceSelectionDecision> SourceSelectionDecisions => sourceSelectionDecisions.ToArray();

        public IReadOnlyList<DiscoveredMod> Discover()
        {
            Stopwatch total = Stopwatch.StartNew();
            var mods = new List<DiscoveredMod>();
            errors.Clear();
            warnings.Clear();
            sourceSelectionDecisions.Clear();
            OfficialModEnablementIndex official = OfficialModEnablementIndex.Load();
            OfficialLocalModsRoot = official.LocalModsRoot;
            OfficialEnablementFilePath = official.EnablementFilePath;
            OfficialEnablementFileExists = official.FileExists;
            OfficialEnablementEntryCount = official.EntryCount;
            ManifestScanElapsedMilliseconds = total.ElapsedMilliseconds;
            if (Directory.Exists(official.LocalModsRoot))
            {
                Stopwatch officialWatch = Stopwatch.StartNew();
                OfficialLocalModsRootExists = true;
                try
                {
                    OfficialLocalDirectoryCount = Directory.GetDirectories(official.LocalModsRoot).Length;
                }
                catch (Exception ex)
                {
                    RecordError(official.LocalModsRoot + ": failed to enumerate official local mods root: " + ex.Message);
                }
                AddFromRoot(official.LocalModsRoot, "Local", official, mods);
                OfficialModsScanElapsedMilliseconds = officialWatch.ElapsedMilliseconds;
            }
            Stopwatch workshopWatch = Stopwatch.StartNew();
            if (workshopSubscriptions.Available)
            {
                foreach (NativeWorkshopSubscription subscription in workshopSubscriptions.Subscriptions)
                {
                    if (string.IsNullOrWhiteSpace(subscription.InstallPath) || !Directory.Exists(subscription.InstallPath))
                    {
                        RecordWarning("Workshop " + subscription.WorkshopId + " 的原生订阅目录不可用：" + subscription.InstallPath);
                        continue;
                    }
                    AddSingleDirectory(subscription.InstallPath, "Workshop", subscription.WorkshopId, official, mods);
                }
            }
            else
                RecordWarning("无法取得原生订阅快照，本次不加载创意工坊 Mod：" + workshopSubscriptions.Failure);
            WorkshopScanElapsedMilliseconds = workshopWatch.ElapsedMilliseconds;
            IReadOnlyList<DiscoveredMod> result = PreferSourceManagedDuplicates(mods);
            SourceSelectionSummary = "nativeSubscriptions=" + workshopSubscriptions.Available +
                "/" + workshopSubscriptions.Count +
                "; playerReproduction=" + authorSourceState.PlayerReproductionActive +
                "; overrides=" + authorSourceState.Selections.Count +
                "; selected=" + result.Count;
            ContentQueryElapsedMilliseconds = total.ElapsedMilliseconds;
            return result;
        }

        private void AddFromRoot(string root, string source, OfficialModEnablementIndex official, List<DiscoveredMod> mods)
        {
            if (!Directory.Exists(root))
                return;
            foreach (string dir in Directory.GetDirectories(root))
                AddSingleDirectory(dir, source, null, official, mods);
        }

        private void AddSingleDirectory(string dir, string source, ulong? workshopId, OfficialModEnablementIndex official, List<DiscoveredMod> mods)
        {
            string manifestPath = FindManifest(dir);
            if (manifestPath.Length == 0)
                return;

            try
            {
                ManifestModel manifest = reader.Read(manifestPath);
                if (!string.IsNullOrEmpty(manifest.InfoReadWarning)) RecordWarning(manifest.UniqueID + ": " + manifest.InfoReadWarning);
                if (manifest.UpdateKeyModels.Any(key => !string.IsNullOrWhiteSpace(key)))
                {
                    RecordWarning(
                        "Manifest " + manifest.UniqueID +
                        " declares UpdateKeys, but DTMAPI currently treats them as inactive schema-only metadata; " +
                        "no update service, network request, or compatibility decision is performed from these values.");
                }
                bool enabled;
                string reason;
                bool isWorkshop = source.Equals("Workshop", StringComparison.OrdinalIgnoreCase);
                string officialId = isWorkshop
                    ? (workshopId.HasValue ? "Workshop." + workshopId.Value.ToString() : string.Empty)
                    : "Local." + Path.GetFileName(dir);
                int officialPriority = -1;
                if (!string.IsNullOrWhiteSpace(officialId) && official.TryGetState(officialId, out OfficialModInfoState officialState))
                {
                    enabled = officialState.Enabled;
                    officialPriority = officialState.Priority ?? -1;
                    reason = enabled ? string.Empty : "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。";
                }
                else
                {
                    enabled = false;
                    reason = official.FileExists
                        ? "未找到 " + officialId + " 的官方启用状态。请在 Doloc Town 官方 Mod 界面中启用这个本地 Mod。"
                        : "未找到官方启用状态文件：" + official.EnablementFilePath + "。请先打开一次 Doloc Town 官方 Mod 界面，再由 DTMAPI 加载这个官方路径管理的 Mod。";
                    if (!string.IsNullOrWhiteSpace(official.LoadError))
                        reason = "无法读取官方启用状态：" + official.LoadError;
                }
                bool subscriptionVerified = false;
                if (source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                {
                    if (!workshopId.HasValue)
                    {
                        enabled = false;
                        reason = "此 Workshop 目录名不是有效的当前订阅 ID；数值目录之外的路径不构成来源授权。";
                    }
                    else if (!workshopSubscriptions.Available)
                    {
                        enabled = false;
                        reason = "无法取得 ModManager.GetSubscribedMods 的当前订阅快照；原始 Workshop 目录不构成来源授权。";
                    }
                    else
                    {
                        bool subscriptionKnown = workshopSubscriptions.TryGet(workshopId.Value, out NativeWorkshopSubscription subscription);
                        subscriptionVerified = subscriptionKnown &&
                            !string.IsNullOrWhiteSpace(subscription.InstallPath) &&
                            PathsEqual(subscription.InstallPath, dir);
                        if (!subscriptionKnown)
                        {
                            enabled = false;
                            reason = "此 Workshop 目录不在 ModManager.GetSubscribedMods 的当前订阅快照中。";
                        }
                        else if (string.IsNullOrWhiteSpace(subscription.InstallPath))
                        {
                            enabled = false;
                            reason = "此 Workshop 订阅项没有可由 ModManager 证明的已安装根目录；数值目录本身不构成来源授权。";
                            RecordWarning("Workshop " + workshopId.Value + " is subscribed but ModManager did not provide an installed root; enumerated raw directory is not verified: " + dir + ".");
                        }
                        else if (!subscriptionVerified)
                        {
                            enabled = false;
                            reason = "此 Workshop 目录与 ModManager 的当前已安装根目录不一致。";
                            RecordWarning("Workshop " + workshopId.Value + " native install path differs from enumerated directory; native=" + subscription.InstallPath + "; enumerated=" + dir + ".");
                        }
                        else if (!subscription.NativeEnabled.HasValue)
                        {
                            if (string.IsNullOrWhiteSpace(workshopSubscriptions.Failure))
                            {
                                enabled = false;
                                reason = "此 Workshop 订阅项未出现在 ModManager.GetAllValidModInfos 的当前原生快照中。";
                            }
                            else
                            {
                                RecordWarning("Workshop " + workshopId.Value + " subscription and installed root were verified, but optional native enablement enrichment failed; official enablement state remains authoritative for this scan. failure=" + workshopSubscriptions.Failure);
                            }
                        }
                        else
                        {
                            enabled = subscription.NativeEnabled.Value;
                            if (subscription.NativePriority >= 0)
                                officialPriority = subscription.NativePriority;
                            officialId = string.IsNullOrWhiteSpace(subscription.NativeOfficialId)
                                ? officialId
                                : subscription.NativeOfficialId;
                            if (!subscription.NativeEnabled.Value)
                                reason = "此 Workshop Mod 已在 ModManager 的当前原生状态中禁用。";
                            else
                                reason = string.Empty;
                        }
                    }
                }
                // Source selection and enablement precede payload/native inspection.
                ManagedModClassification classification = ManagedModClassifier.ClassifyDeclaredIdentity(manifest);
                mods.Add(new DiscoveredMod(manifest, dir, manifestPath, source, workshopId, enabled, false, officialId, true, reason, subscriptionVerified, classification: classification, officialPriority: officialPriority));
            }
            catch (Exception ex)
            {
                RecordError($"{manifestPath}: {ex.Message}");
            }
        }

        private static string FindManifest(string dir)
        {
            return PackageDocumentFacts.FindManifest(dir) ?? string.Empty;
        }

        private IReadOnlyList<DiscoveredMod> PreferSourceManagedDuplicates(IEnumerable<DiscoveredMod> mods)
        {
            var byId = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
            var unnamed = new List<DiscoveredMod>();
            foreach (IGrouping<string, DiscoveredMod> group in mods.GroupBy(m => m.Manifest.UniqueID ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                string id = group.Key;
                if (string.IsNullOrWhiteSpace(id))
                {
                    unnamed.AddRange(group);
                    continue;
                }

                AuthorSourceMode mode = authorSourceState.GetMode(id);
                DiscoveredMod[] candidates = group.ToArray();
                DiscoveredMod[] ordered = OrderCandidatesForMode(id, mode, candidates).ToArray();
                if (ordered.Length == 0)
                {
                    sourceSelectionDecisions.Add(new AuthorSourceSelectionDecision(id, mode, "blocked/no-authorized-source", null, candidates));
                    continue;
                }
                string outcome = "mode=" + mode +
                    "; selected=" + DescribeDuplicateCandidate(ordered[0]) +
                    "; nativeSnapshot=" + workshopSubscriptions.Available;
                DiscoveredMod selected = ordered[0].WithSelectionReason(outcome);
                if (selected.OfficialEnabled)
                {
                    try
                    {
                        selected = selected.WithSelectionReason(outcome, classifier.Classify(
                            selected.Manifest, selected.Source, selected.NativeSubscriptionVerified));
                    }
                    catch (Exception ex)
                    {
                        RecordError($"{selected.ManifestPath}: {ex.Message}");
                        // Invalid selected sources never silently fall back to an ignored candidate.
                        sourceSelectionDecisions.Add(new AuthorSourceSelectionDecision(id, mode, "blocked/package-invalid", null, candidates));
                        continue;
                    }
                }
                byId[id] = selected;
                sourceSelectionDecisions.Add(new AuthorSourceSelectionDecision(id, mode, outcome, selected, candidates));

                if (candidates.Length <= 1)
                    continue;

                string ignored = string.Join("; ", candidates
                    .Where(candidate => !PathsEqual(candidate.RootPath, ordered[0].RootPath))
                    .Select(DescribeDuplicateCandidate)
                    .ToArray());
                RecordWarning(
                    "Duplicate UniqueID " + id +
                    " discovered; using " + DescribeDuplicateCandidate(selected) +
                    "; mode=" + mode +
                    "; ignored " + ignored + ".");
            }

            unnamed.AddRange(byId.Values.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase));
            return unnamed;
        }

        private static string DescribeDuplicateCandidate(DiscoveredMod mod)
        {
            return mod.Source + " enabled=" + (mod.OfficialEnabled ? "true" : "false") +
                " priority=" + mod.OfficialPriority + " root=" + mod.RootPath;
        }

        private IEnumerable<DiscoveredMod> OrderCandidatesForMode(string uniqueId, AuthorSourceMode mode, DiscoveredMod[] candidates)
        {
            if (mode == AuthorSourceMode.LocalDevelopment)
            {
                RecordWarning(
                    "Legacy Local Development selection is recovery-only for " + uniqueId +
                    "; player Runtime uses official Local/Workshop enablement and load order.");
                return OrderPlayerWorkshopCandidates(uniqueId, candidates);
            }

            if (mode == AuthorSourceMode.WorkshopValidation)
            {
                if (!workshopSubscriptions.Available)
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": native subscription snapshot unavailable: " + workshopSubscriptions.Failure + ".");
                    return Array.Empty<DiscoveredMod>();
                }

                DiscoveredMod[] matches = candidates
                    .Where(candidate => candidate.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) &&
                        candidate.NativeSubscriptionVerified &&
                        candidate.OfficialEnabled)
                    .OrderByDescending(candidate => candidate.OfficialPriority)
                    .ThenBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (matches.Length == 0)
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": no native-subscribed installed source.");
                    return Array.Empty<DiscoveredMod>();
                }

                return matches.Concat(candidates.Where(candidate => !matches.Contains(candidate)).OrderBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase));
            }

            return OrderPlayerWorkshopCandidates(uniqueId, candidates);
        }

        private IEnumerable<DiscoveredMod> OrderPlayerWorkshopCandidates(string uniqueId, DiscoveredMod[] candidates)
        {
            DiscoveredMod[] authorized = candidates
                .Where(IsOfficialPlayerCandidate)
                .ToArray();
            DiscoveredMod[] enabled = authorized
                .Where(candidate => candidate.OfficialEnabled)
                .ToArray();
            if (enabled.Length == 0)
                return authorized.Length == 1 ? authorized : Array.Empty<DiscoveredMod>();
            if (enabled.Length == 1)
                return enabled;

            if (enabled.Any(candidate => candidate.OfficialPriority < 0))
            {
                RecordWarning(
                    "Duplicate UniqueID " + uniqueId +
                    " has multiple enabled official candidates but their complete ModManager priority/load order is unavailable; " +
                    "blocked. Please enable only one copy in the official Mod page.");
                return Array.Empty<DiscoveredMod>();
            }

            int highestPriority = enabled.Max(candidate => candidate.OfficialPriority);
            DiscoveredMod[] winners = enabled
                .Where(candidate => candidate.OfficialPriority == highestPriority)
                .ToArray();
            if (winners.Length != 1)
            {
                RecordWarning(
                    "Duplicate UniqueID " + uniqueId +
                    " has multiple enabled official candidates at priority " + highestPriority +
                    "; blocked. Please enable only one copy in the official Mod page.");
                return Array.Empty<DiscoveredMod>();
            }
            return winners;
        }

        private bool IsOfficialPlayerCandidate(DiscoveredMod candidate)
        {
            if (candidate.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                return candidate.NativeSubscriptionVerified;
            return candidate.Source.Equals("Local", StringComparison.OrdinalIgnoreCase) &&
                candidate.OfficialEnablementManaged;
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                string leftFull = Path.GetFullPath(left ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string rightFull = Path.GetFullPath(right ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(leftFull, rightFull, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void RecordError(string value) => errors.Add(value);

        private void RecordWarning(string value) => warnings.Add(value);
    }

    internal sealed class OfficialModEnablementIndex
    {
        private readonly Dictionary<string, OfficialModInfoState> stateByOfficialId = new Dictionary<string, OfficialModInfoState>(StringComparer.OrdinalIgnoreCase);

        private OfficialModEnablementIndex(string dolocPersistentRoot)
        {
            DolocPersistentRoot = dolocPersistentRoot;
            LocalModsRoot = Path.Combine(dolocPersistentRoot, "MODS");
        }

        public string DolocPersistentRoot { get; }
        public string LocalModsRoot { get; }
        public string EnablementFilePath { get; private set; } = string.Empty;
        public bool FileExists { get; private set; }
        public string LoadError { get; private set; } = string.Empty;
        public bool HasData => stateByOfficialId.Count > 0;
        public int EntryCount => stateByOfficialId.Count;

        public static OfficialModEnablementIndex Load()
        {
            string root = GetDolocPersistentRoot();
            var index = new OfficialModEnablementIndex(root);
            string path = Path.Combine(root, "SAVE", "mod_infos.json");
            index.EnablementFilePath = path;
            if (!File.Exists(path))
                return index;

            index.FileExists = true;
            try
            {
                OfficialModInfoFile? file = JsonFile.Read<OfficialModInfoFile>(path);
                if (file?.ModInfos == null)
                    return index;
                foreach (KeyValuePair<string, OfficialModInfoState> pair in file.ModInfos)
                {
                    string id = string.IsNullOrWhiteSpace(pair.Value.Id) ? pair.Key : pair.Value.Id;
                    if (!string.IsNullOrWhiteSpace(id))
                        index.stateByOfficialId[id] = pair.Value;
                }
            }
            catch (Exception ex)
            {
                index.LoadError = ex.Message;
            }
            return index;
        }

        public bool TryGetState(string officialId, out OfficialModInfoState state) => stateByOfficialId.TryGetValue(officialId, out state);

        private static string GetDolocPersistentRoot()
        {
            string configured = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(configured))
                return Path.GetFullPath(configured);

            string userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userProfile))
                userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
                return Path.Combine(userProfile, "AppData", "LocalLow", "RedSawGames", "DolocTown");

            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(localAppData))
                localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                DirectoryInfo? appDataRoot = Directory.GetParent(localAppData);
                if (appDataRoot != null)
                    return Path.Combine(appDataRoot.FullName, "LocalLow", "RedSawGames", "DolocTown");
            }

            return Path.Combine("AppData", "LocalLow", "RedSawGames", "DolocTown");
        }
    }

    [DataContract]
    internal sealed class OfficialModInfoFile
    {
        [DataMember(Name = "modInfos")]
        public Dictionary<string, OfficialModInfoState> ModInfos { get; set; } = new Dictionary<string, OfficialModInfoState>(StringComparer.OrdinalIgnoreCase);
    }

    [DataContract]
    internal sealed class OfficialModInfoState
    {
        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "priority")]
        public int? Priority { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; } = string.Empty;

        [DataMember(Name = "title")]
        public string Title { get; set; } = string.Empty;
    }
}
