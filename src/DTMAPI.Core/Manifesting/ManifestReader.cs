using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Diagnostics;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Manifesting
{
    internal sealed class ManifestReader
    {
        public ManifestModel Read(string path)
        {
            IReadOnlyList<string> properties = JsonTopLevelPropertyReader.Read(path);
            RejectMisCasedOrDuplicateWireField(properties, "Type");
            RejectMisCasedOrDuplicateWireField(properties, "CodeModKind");

            ManifestModel model = JsonFile.Read<ManifestModel>(path);
            model.Normalize();
            if (string.IsNullOrWhiteSpace(model.UniqueID))
                throw new InvalidDataException("manifest 缺少 UniqueID。");
            if (string.IsNullOrWhiteSpace(model.Name))
                model.Name = model.UniqueID;
            if (string.IsNullOrWhiteSpace(model.Author))
                model.Author = "Unknown";
            return model;
        }

        private static void RejectMisCasedOrDuplicateWireField(IReadOnlyList<string> properties, string canonicalName)
        {
            string[] matches = properties
                .Where(name => string.Equals(name, canonicalName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
                return;
            if (matches.Length != 1 || !string.Equals(matches[0], canonicalName, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "manifest field " + canonicalName + " is case-sensitive and may appear exactly once; received " +
                    string.Join(", ", matches) + ".");
            }
        }
    }

    internal sealed class ModScanner
    {
        internal const int MaxDiagnosticSamplesPerSeverity = 128;
        internal const int MaxDuplicateCandidateSamples = 16;

        private readonly RuntimePaths paths;
        private readonly ManifestReader reader = new ManifestReader();
        private readonly ManagedModClassifier classifier;
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<AuthorSourceSelectionDecision> sourceSelectionDecisions = new List<AuthorSourceSelectionDecision>();
        private readonly NativeWorkshopSubscriptionSnapshot workshopSubscriptions;
        private readonly AuthorSourceSelectionState authorSourceState;
        private readonly bool authorSessionActive;
        private int errorCount;
        private int warningCount;
        private int duplicateUniqueIdWarningCount;
        private long diagnosticTrimmedBytes;

        public ModScanner(
            RuntimePaths paths,
            NativeWorkshopSubscriptionSnapshot? workshopSubscriptions = null,
            AuthorSourceSelectionState? authorSourceState = null,
            bool authorSessionActive = false)
        {
            this.paths = paths;
            classifier = new ManagedModClassifier(paths.GamePath);
            this.workshopSubscriptions = workshopSubscriptions ?? NativeWorkshopSubscriptionSnapshot.Unavailable("snapshot not supplied");
            this.authorSourceState = authorSourceState ?? new AuthorSourceSelectionState(paths.GamePath, false, string.Empty, Array.Empty<AuthorSourceSelection>(), string.Empty, string.Empty);
            this.authorSessionActive = authorSessionActive;
        }

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public ModScannerDiagnosticTotals DiagnosticTotals => new ModScannerDiagnosticTotals(
            errorCount,
            warningCount,
            duplicateUniqueIdWarningCount,
            errors.Count,
            warnings.Count,
            diagnosticTrimmedBytes);
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
            errorCount = 0;
            warningCount = 0;
            duplicateUniqueIdWarningCount = 0;
            diagnosticTrimmedBytes = 0;
            sourceSelectionDecisions.Clear();
            OfficialModEnablementIndex official = OfficialModEnablementIndex.Load();
            OfficialLocalModsRoot = official.LocalModsRoot;
            OfficialEnablementFilePath = official.EnablementFilePath;
            OfficialEnablementFileExists = official.FileExists;
            OfficialEnablementEntryCount = official.EntryCount;
            AddFromRoot(paths.ModsPath, "Local", canDtmApiToggle: true, official, useOfficialEnablement: false, mods);
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
                AddFromRoot(official.LocalModsRoot, "OfficialLocal", canDtmApiToggle: false, official, useOfficialEnablement: true, mods);
                OfficialModsScanElapsedMilliseconds = officialWatch.ElapsedMilliseconds;
            }
            Stopwatch workshopWatch = Stopwatch.StartNew();
            string workshopRoot = Path.Combine(paths.GamePath, "steamapps", "workshop", "content", "2285550");
            if (Directory.Exists(workshopRoot))
                AddWorkshopRoot(workshopRoot, official, mods);
            string siblingWorkshopRoot = Path.GetFullPath(Path.Combine(paths.GamePath, "..", "..", "workshop", "content", "2285550"));
            if (Directory.Exists(siblingWorkshopRoot) && !StringComparer.OrdinalIgnoreCase.Equals(workshopRoot, siblingWorkshopRoot))
                AddWorkshopRoot(siblingWorkshopRoot, official, mods);
            WorkshopScanElapsedMilliseconds = workshopWatch.ElapsedMilliseconds;
            IReadOnlyList<DiscoveredMod> result = PreferSourceManagedDuplicates(mods);
            SourceSelectionSummary = "nativeSubscriptions=" + workshopSubscriptions.Available +
                "/" + workshopSubscriptions.Count +
                "; playerReproduction=" + authorSourceState.PlayerReproductionActive +
                "; authorSession=" + authorSessionActive +
                "; overrides=" + authorSourceState.Selections.Count +
                "; selected=" + result.Count;
            ContentQueryElapsedMilliseconds = total.ElapsedMilliseconds;
            return result;
        }

        private void AddWorkshopRoot(string root, OfficialModEnablementIndex official, List<DiscoveredMod> mods)
        {
            foreach (string itemDir in Directory.GetDirectories(root))
            {
                ulong workshopId;
                ulong? parsedId = ulong.TryParse(Path.GetFileName(itemDir), out workshopId) ? workshopId : (ulong?)null;
                AddSingleDirectory(itemDir, "Workshop", canDtmApiToggle: false, workshopId: parsedId, official: official, useOfficialEnablement: true, mods: mods);
            }
        }

        private void AddFromRoot(string root, string source, bool canDtmApiToggle, OfficialModEnablementIndex official, bool useOfficialEnablement, List<DiscoveredMod> mods)
        {
            if (!Directory.Exists(root))
                return;
            foreach (string dir in Directory.GetDirectories(root))
                AddSingleDirectory(dir, source, canDtmApiToggle, workshopId: null, official: official, useOfficialEnablement: useOfficialEnablement, mods: mods);
        }

        private void AddSingleDirectory(string dir, string source, bool canDtmApiToggle, ulong? workshopId, OfficialModEnablementIndex official, bool useOfficialEnablement, List<DiscoveredMod> mods)
        {
            string manifestPath = FindManifest(dir);
            if (manifestPath.Length == 0)
                return;

            try
            {
                ManifestModel manifest = reader.Read(manifestPath);
                if (manifest.UpdateKeyModels.Any(key => !string.IsNullOrWhiteSpace(key)))
                {
                    RecordWarning(
                        "Manifest " + manifest.UniqueID +
                        " declares UpdateKeys, but DTMAPI currently treats them as inactive schema-only metadata; " +
                        "no update service, network request, or compatibility decision is performed from these values.");
                }
                bool localMarkerEnabled = !File.Exists(Path.Combine(dir, "dtmapi.disabled")) && !Directory.Exists(Path.Combine(dir, ".disabled"));
                bool enabled = localMarkerEnabled;
                string reason = enabled ? string.Empty : "此 Mod 已被本地 DTMAPI 禁用标记停用。";
                string officialId = string.Empty;
                if (useOfficialEnablement)
                {
                    officialId = workshopId.HasValue ? "Workshop." + workshopId.Value.ToString() : "Local." + Path.GetFileName(dir);
                    if (official.TryGetEnabled(officialId, out bool officialEnabled))
                    {
                        enabled = enabled && officialEnabled;
                        if (!officialEnabled)
                            reason = "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。";
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
                }
                bool subscriptionVerified = false;
                if (source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) && workshopId.HasValue && workshopSubscriptions.Available)
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
                        enabled = localMarkerEnabled && subscription.NativeEnabled.Value;
                        officialId = string.IsNullOrWhiteSpace(subscription.NativeOfficialId)
                            ? officialId
                            : subscription.NativeOfficialId;
                        if (!subscription.NativeEnabled.Value)
                            reason = "此 Workshop Mod 已在 ModManager 的当前原生状态中禁用。";
                        else if (localMarkerEnabled)
                            reason = string.Empty;
                    }
                }
                ManagedModClassification classification = classifier.Classify(
                    manifest,
                    dir,
                    manifestPath,
                    source,
                    subscriptionVerified,
                    workshopId);
                mods.Add(new DiscoveredMod(manifest, dir, manifestPath, source, workshopId, enabled, canDtmApiToggle, officialId, useOfficialEnablement, reason, subscriptionVerified, classification: classification));
            }
            catch (Exception ex)
            {
                RecordError($"{manifestPath}: {ex.Message}");
            }
        }

        private static string FindManifest(string dir)
        {
            string dtm = Path.Combine(dir, "dtmapi.manifest.json");
            if (File.Exists(dtm))
                return dtm;
            string standard = Path.Combine(dir, "manifest.json");
            if (File.Exists(standard))
                return standard;
            string contentManifest = Path.Combine(dir, "Content", "DTMAPI", "manifest.json");
            return File.Exists(contentManifest) ? contentManifest : string.Empty;
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
                byId[id] = selected;
                sourceSelectionDecisions.Add(new AuthorSourceSelectionDecision(id, mode, outcome, selected, candidates));

                if (ordered.Length <= 1)
                    continue;

                string ignored = string.Join("; ", ordered
                    .Skip(1)
                    .Take(MaxDuplicateCandidateSamples)
                    .Select(DescribeDuplicateCandidate)
                    .ToArray());
                int omittedCandidates = Math.Max(0, ordered.Length - 1 - MaxDuplicateCandidateSamples);
                RecordWarning(
                    "Duplicate UniqueID " + id +
                    " discovered; using " + DescribeDuplicateCandidate(selected) +
                    "; mode=" + mode +
                    (omittedCandidates > 0 ? "; omittedCandidates=" + omittedCandidates : string.Empty) +
                    "; ignored " + ignored + ".",
                    duplicateUniqueId: true);
            }

            unnamed.AddRange(byId.Values.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase));
            return unnamed;
        }

        private static string DescribeDuplicateCandidate(DiscoveredMod mod)
        {
            return mod.Source + " enabled=" + (mod.OfficialEnabled ? "true" : "false") + " root=" + mod.RootPath;
        }

        private IEnumerable<DiscoveredMod> OrderCandidatesForMode(string uniqueId, AuthorSourceMode mode, DiscoveredMod[] candidates)
        {
            if (mode == AuthorSourceMode.LocalDevelopment)
            {
                if (!authorSourceState.TryGetSelection(uniqueId, out AuthorSourceSelection selection) || string.IsNullOrWhiteSpace(selection.SourcePath))
                {
                    RecordWarning("Local Development selection is incomplete for " + uniqueId + "; using safe Player/Workshop fallback.");
                    return OrderPlayerWorkshopCandidates(uniqueId, candidates);
                }

                DiscoveredMod[] matches = candidates
                    .Where(candidate => !candidate.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) && PathsEqual(candidate.RootPath, selection.SourcePath))
                    .OrderBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (matches.Length == 0)
                {
                    RecordWarning("Local Development source is unavailable for " + uniqueId + ": " + selection.SourcePath + "; using safe Player/Workshop fallback.");
                    return OrderPlayerWorkshopCandidates(uniqueId, candidates);
                }

                if (!TryValidateExpectedSourceTree(uniqueId, selection, matches[0], out string localTreeFailure))
                {
                    RecordWarning("Local Development source validation failed for " + uniqueId + ": " + localTreeFailure + "; using safe Player/Workshop fallback.");
                    return OrderPlayerWorkshopCandidates(uniqueId, candidates);
                }

                return matches.Concat(candidates.Where(candidate => !matches.Contains(candidate)).OrderBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase));
            }

            if (mode == AuthorSourceMode.WorkshopValidation)
            {
                if (!authorSessionActive)
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": no active startup-authorized author session.");
                    return Array.Empty<DiscoveredMod>();
                }
                if (!workshopSubscriptions.Available)
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": native subscription snapshot unavailable: " + workshopSubscriptions.Failure + ".");
                    return Array.Empty<DiscoveredMod>();
                }

                DiscoveredMod[] matches = candidates
                    .Where(candidate => candidate.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) && candidate.NativeSubscriptionVerified)
                    .OrderBy(candidate => candidate.OfficialEnabled ? 0 : 1)
                    .ThenBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (matches.Length == 0)
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": no native-subscribed installed source.");
                    return Array.Empty<DiscoveredMod>();
                }

                if (!authorSourceState.TryGetSelection(uniqueId, out AuthorSourceSelection validationSelection))
                {
                    RecordWarning("Workshop Validation blocked for " + uniqueId + ": no exact SDK source selection was recorded.");
                    return Array.Empty<DiscoveredMod>();
                }
                return matches.Concat(candidates.Where(candidate => !matches.Contains(candidate)).OrderBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase));
            }

            return OrderPlayerWorkshopCandidates(uniqueId, candidates);
        }

        private IEnumerable<DiscoveredMod> OrderPlayerWorkshopCandidates(string uniqueId, DiscoveredMod[] candidates)
        {
            bool useNativeWorkshopAuthority = workshopSubscriptions.Available;
            if (!useNativeWorkshopAuthority && candidates.Length > 1)
            {
                RecordWarning(
                    "Duplicate UniqueID " + uniqueId +
                    " blocked because the native Workshop subscription snapshot is unavailable; raw directory priority cannot prove player source authority. failure=" +
                    workshopSubscriptions.Failure + ".",
                    duplicateUniqueId: true);
                return Array.Empty<DiscoveredMod>();
            }
            if (!useNativeWorkshopAuthority && candidates.Length == 1)
            {
                RecordWarning(
                    "Sole-source compatibility fallback for " + uniqueId +
                    " because the native Workshop subscription snapshot is unavailable; source is not native-subscription-verified. source=" +
                    candidates[0].Source + "; root=" + candidates[0].RootPath + "; failure=" + workshopSubscriptions.Failure + ".");
            }
            return candidates
                .OrderBy(candidate => CandidatePriority(candidate, useNativeWorkshopAuthority))
                .ThenBy(candidate => candidate.RootPath, StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryValidateExpectedSourceTree(
            string uniqueId,
            AuthorSourceSelection selection,
            DiscoveredMod candidate,
            out string failure)
        {
            failure = string.Empty;
            if (selection == null || !AuthorSessionProtocol.IsSha256(selection.ExpectedTreeSha256))
            {
                failure = "missing " + AuthorFileTreeDigest.Algorithm + " expectedTreeSha256";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(selection.SourcePath) && !PathsEqual(selection.SourcePath, candidate.RootPath))
            {
                failure = "selected source path no longer matches the recorded exact root";
                return false;
            }

            try
            {
                string actual = AuthorFileTreeDigest.Compute(candidate.RootPath);
                if (!string.Equals(actual, selection.ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase))
                {
                    failure = AuthorFileTreeDigest.Algorithm + " mismatch expected=" + selection.ExpectedTreeSha256 + "; actual=" + actual;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                failure = "failed to hash source tree for " + uniqueId + ": " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static int CandidatePriority(DiscoveredMod mod, bool useNativeWorkshopAuthority)
        {
            if (useNativeWorkshopAuthority && mod.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase) && mod.NativeSubscriptionVerified)
                return mod.OfficialEnabled ? 0 : 1;
            if (mod.OfficialEnabled)
                return 10 + SourcePriority(mod.Source, useNativeWorkshopAuthority);
            return 30 + SourcePriority(mod.Source, useNativeWorkshopAuthority);
        }

        private static int SourcePriority(string source, bool nativeWorkshopAuthority)
        {
            if (nativeWorkshopAuthority && source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (source.Equals("OfficialLocal", StringComparison.OrdinalIgnoreCase))
                return nativeWorkshopAuthority ? 1 : 0;
            if (source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                return 1;
            return 2;
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

        private void RecordError(string value)
        {
            errorCount++;
            if (errors.Count < MaxDiagnosticSamplesPerSeverity)
                errors.Add(BoundedDiagnosticScalar.Sanitize(value, BoundedDiagnosticScalar.DetailsChars, ref diagnosticTrimmedBytes));
        }

        private void RecordWarning(string value, bool duplicateUniqueId = false)
        {
            warningCount++;
            if (duplicateUniqueId)
                duplicateUniqueIdWarningCount++;
            if (warnings.Count < MaxDiagnosticSamplesPerSeverity)
                warnings.Add(BoundedDiagnosticScalar.Sanitize(value, BoundedDiagnosticScalar.DetailsChars, ref diagnosticTrimmedBytes));
        }
    }

    internal readonly struct ModScannerDiagnosticTotals
    {
        public ModScannerDiagnosticTotals(
            int errorCount,
            int warningCount,
            int duplicateUniqueIdWarningCount,
            int errorSampleCount,
            int warningSampleCount,
            long trimmedBytes = 0)
        {
            ErrorCount = Math.Max(0, errorCount);
            WarningCount = Math.Max(0, warningCount);
            DuplicateUniqueIdWarningCount = Math.Max(0, Math.Min(WarningCount, duplicateUniqueIdWarningCount));
            ErrorSampleCount = Math.Max(0, Math.Min(ErrorCount, errorSampleCount));
            WarningSampleCount = Math.Max(0, Math.Min(WarningCount, warningSampleCount));
            TrimmedBytes = Math.Max(0, trimmedBytes);
        }

        public int ErrorCount { get; }
        public int WarningCount { get; }
        public int DuplicateUniqueIdWarningCount { get; }
        public int ErrorSampleCount { get; }
        public int WarningSampleCount { get; }
        public long TrimmedBytes { get; }
        public int ErrorTrimmedCount => ErrorCount - ErrorSampleCount;
        public int WarningTrimmedCount => WarningCount - WarningSampleCount;

        public string FormatSummary()
        {
            return "errors=" + ErrorCount +
                "; errorSamples=" + ErrorSampleCount +
                "; errorsTrimmed=" + ErrorTrimmedCount +
                "; warnings=" + WarningCount +
                "; warningSamples=" + WarningSampleCount +
                "; warningsTrimmed=" + WarningTrimmedCount +
                "; duplicateUniqueIds=" + DuplicateUniqueIdWarningCount +
                "; trimmedBytes=" + TrimmedBytes +
                "; maxSamplesPerSeverity=" + ModScanner.MaxDiagnosticSamplesPerSeverity;
        }
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

        public bool TryGetEnabled(string officialId, out bool enabled)
        {
            if (stateByOfficialId.TryGetValue(officialId, out OfficialModInfoState state))
            {
                enabled = state.Enabled;
                return true;
            }

            enabled = false;
            return false;
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
        public int Priority { get; set; } = -1;

        [DataMember(Name = "source")]
        public string Source { get; set; } = string.Empty;

        [DataMember(Name = "title")]
        public string Title { get; set; } = string.Empty;
    }
}
