using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Diagnostics;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Manifesting
{
    internal sealed class ManifestReader
    {
        public ManifestModel Read(string path)
        {
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
    }

    internal sealed class ModScanner
    {
        private readonly RuntimePaths paths;
        private readonly ManifestReader reader = new ManifestReader();
        private readonly List<string> errors = new List<string>();

        public ModScanner(RuntimePaths paths)
        {
            this.paths = paths;
        }

        public IReadOnlyList<string> Errors => errors;
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

        public IReadOnlyList<DiscoveredMod> Discover()
        {
            Stopwatch total = Stopwatch.StartNew();
            var mods = new List<DiscoveredMod>();
            errors.Clear();
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
                    errors.Add(official.LocalModsRoot + ": failed to enumerate official local mods root: " + ex.Message);
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
                bool enabled = !File.Exists(Path.Combine(dir, "dtmapi.disabled")) && !Directory.Exists(Path.Combine(dir, ".disabled"));
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
                mods.Add(new DiscoveredMod(manifest, dir, manifestPath, source, workshopId, enabled, canDtmApiToggle, officialId, useOfficialEnablement, reason));
            }
            catch (Exception ex)
            {
                errors.Add($"{manifestPath}: {ex.Message}");
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

        private static IReadOnlyList<DiscoveredMod> PreferSourceManagedDuplicates(IEnumerable<DiscoveredMod> mods)
        {
            var byId = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
            var unnamed = new List<DiscoveredMod>();
            foreach (DiscoveredMod mod in mods)
            {
                string id = mod.Manifest.UniqueID;
                if (string.IsNullOrWhiteSpace(id))
                {
                    unnamed.Add(mod);
                    continue;
                }

                if (!byId.TryGetValue(id, out DiscoveredMod existing) || SourcePriority(mod.Source) < SourcePriority(existing.Source))
                    byId[id] = mod;
            }

            unnamed.AddRange(byId.Values.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase));
            return unnamed;
        }

        private static int SourcePriority(string source)
        {
            if (source.Equals("OfficialLocal", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                return 1;
            return 2;
        }
    }

    internal sealed class OfficialModEnablementIndex
    {
        private readonly Dictionary<string, bool> enabledByOfficialId = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

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
        public bool HasData => enabledByOfficialId.Count > 0;
        public int EntryCount => enabledByOfficialId.Count;

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
                        index.enabledByOfficialId[id] = pair.Value.Enabled;
                }
            }
            catch (Exception ex)
            {
                index.LoadError = ex.Message;
            }
            return index;
        }

        public bool TryGetEnabled(string officialId, out bool enabled) => enabledByOfficialId.TryGetValue(officialId, out enabled);

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
    }
}
