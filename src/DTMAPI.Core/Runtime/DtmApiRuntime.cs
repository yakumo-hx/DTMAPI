using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;

namespace DTMAPI.Core.Runtime
{
    public sealed class DtmApiRuntime
    {
        public const string ApiVersion = "0.2.5";

        private readonly IRuntimeHost host;
        private readonly IDtmConfigMenuApi? configMenuApi;
        private readonly List<DiscoveredMod> discoveredMods = new List<DiscoveredMod>();
        private readonly List<DiscoveredMod> loadedMods = new List<DiscoveredMod>();
        private readonly Dictionary<string, DiscoveredMod> discoveredById = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmMod> modInstances = new Dictionary<string, DtmMod>(StringComparer.OrdinalIgnoreCase);
        private readonly DateTimeOffset startedAt = DateTimeOffset.Now;
        private DateTimeOffset lastSecondTick = DateTimeOffset.Now;
        private ulong updateTick;
        private uint secondTick;
        private int? currentLoadingSlot;
        private bool started;

        public DtmApiRuntime(IRuntimeHost host, IDtmConfigMenuApi? configMenuApi = null)
        {
            this.host = host;
            this.configMenuApi = configMenuApi;
            Paths = new RuntimePaths(host.GamePath, host.PluginPath);
            Diagnostics = new DiagnosticsService(Paths);
            Events = new EventManager(Diagnostics);
            Config = new ConfigService(Paths);
            ModRegistry = new ModRegistryService();
            Workshop = new WorkshopService();
            Content = new ContentQueryService(Paths);
            Input = new InputService();
            UI = new UiRuntimeService(ExportLogs, Events.DispatchMenuOpened, Events.DispatchMenuClosed);
        }

        public RuntimePaths Paths { get; }
        public DiagnosticsService Diagnostics { get; }
        public UiRuntimeService UI { get; }
        internal EventManager Events { get; }
        internal ConfigService Config { get; }
        internal ModRegistryService ModRegistry { get; }
        internal WorkshopService Workshop { get; }
        internal ContentQueryService Content { get; }
        internal InputService Input { get; }
        public IMonitor RuntimeMonitor { get; private set; } = NullMonitor.Instance;
        public IReadOnlyList<DiscoveredMod> DiscoveredMods => discoveredMods.ToArray();
        public IReadOnlyList<DiscoveredMod> LoadedMods => loadedMods.ToArray();
        public DateTimeOffset StartedAt => startedAt;
        public event Action<int?, bool>? SaveSessionLoaded;
        public event Action? ReturnedToTitleBoundary;

        public void Start()
        {
            if (started)
                return;
            started = true;
            Stopwatch startup = Stopwatch.StartNew();
            Paths.Ensure();
            string latestLog = Path.Combine(Paths.LogsPath, "latest.log");
            Diagnostics.LatestLogPath = latestLog;
            RuntimeMonitor = new FileMonitor(host, "DTMAPI", latestLog);
            RuntimeMonitor.Log("DTMAPI runtime starting.");
            RuntimeMonitor.Log("Startup segment Core.PathsAndLog elapsedMs=" + startup.ElapsedMilliseconds + ".");
            RuntimeMonitor.Log("Host = " + host.HostName);
            RuntimeMonitor.Log("GamePath = " + Paths.GamePath);
            RuntimeMonitor.Log("ModsPath = " + Paths.ModsPath);

            ModRegistry.AddLoaded(CreateRuntimeManifest());
            if (configMenuApi != null)
            {
                IManifest configMenuManifest = CreateConfigMenuManifest();
                ModRegistry.AddLoaded(configMenuManifest);
                ModRegistry.RegisterApi<IDtmConfigMenuApi>(configMenuManifest, configMenuApi);
            }

            DiscoverMods();
            long afterDiscovery = startup.ElapsedMilliseconds;
            LoadMods(initialLoad: true);
            RuntimeMonitor.Log("Startup segment ModLoad elapsedMs=" + (startup.ElapsedMilliseconds - afterDiscovery) + " totalMs=" + startup.ElapsedMilliseconds + ".");
            RefreshConfigPageLocks();
            Diagnostics.SetHookStatus("GameLoop.GameLaunched", "verified", "DTMAPI.Core", "Dispatched after DTMAPI mod Entry completed.");
            Events.DispatchGameLaunched();
            RuntimeMonitor.Log("GameLaunched dispatched.");
            RuntimeMonitor.Log("Startup segment Core.Start totalMs=" + startup.ElapsedMilliseconds + ".");
        }

        public void Update()
        {
            updateTick++;
            if (!UI.BlocksModUpdates)
                Events.DispatchUpdateTicked(updateTick);
            DateTimeOffset now = DateTimeOffset.Now;
            if ((now - lastSecondTick).TotalSeconds >= 1)
            {
                secondTick++;
                lastSecondTick = now;
                Events.DispatchOneSecondUpdateTicked(secondTick);
            }
            Input.ClearFrame();
        }

        public void RecordInputPressed(string button)
        {
            if (UI.BlocksGameplayHotkeys)
            {
                RuntimeMonitor.LogOnce(
                    "input-blocked-" + button + "-" + UI.InputContext + "-" + UI.IsOpen,
                    "Input " + button + " blocked by UI boundary. context=" + UI.InputContext + " menuOpen=" + UI.IsOpen + " gameplayHotkeys=" + UI.GameplayHotkeysAllowed + ".");
                return;
            }
            Input.SetPressed(button);
            RuntimeMonitor.Log("Input " + button + " pressed dispatched to DTMAPI mods. context=" + UI.InputContext + " menuOpen=" + UI.IsOpen + ".");
            Events.DispatchButtonPressed(button);
        }

        public void RecordInputReleased(string button)
        {
            Input.SetReleased(button);
            if (UI.BlocksGameplayHotkeys)
                return;
            RuntimeMonitor.Log("Input " + button + " released dispatched to DTMAPI mods. context=" + UI.InputContext + " menuOpen=" + UI.IsOpen + ".");
            Events.DispatchButtonReleased(button);
        }

        public IReadOnlyList<string> GetRegisteredInputButtons() => Input.GetRegisteredButtons();

        public bool IsInputDown(string button) => Input.IsDown(button);

        public void NotifyLoadGameRequested(int slot)
        {
            currentLoadingSlot = slot;
            RuntimeMonitor.Log($"LoadGame requested for slot/index {slot}.");
        }

        public void NotifySaveLoaded(bool isNewGame)
        {
            RuntimeMonitor.Log($"SaveLoaded hook dispatched. slot/index={currentLoadingSlot?.ToString() ?? "unknown"} isNewGame={isNewGame}");
            try
            {
                SaveSessionLoaded?.Invoke(currentLoadingSlot, isNewGame);
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.Runtime", "Save session boundary listener failed.", ex.ToString());
            }
            Events.DispatchSaveLoaded(currentLoadingSlot, isNewGame);
        }

        public void NotifySaveSaving(int? slot)
        {
            RuntimeMonitor.Log($"SaveSaving hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            Events.DispatchSaveSaving(slot ?? currentLoadingSlot);
        }

        public void NotifySaveSaved(int? slot)
        {
            RuntimeMonitor.Log($"SaveSaved hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            Events.DispatchSaveSaved(slot ?? currentLoadingSlot);
        }

        public void NotifyReturnedToTitle()
        {
            RuntimeMonitor.Log("ReturnedToTitle hook dispatched.");
            currentLoadingSlot = null;
            try
            {
                ReturnedToTitleBoundary?.Invoke();
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.Runtime", "Returned-to-title boundary listener failed.", ex.ToString());
            }
            Events.DispatchReturnedToTitle();
        }

        public void NotifyWorkshopModListChanged()
        {
            DiscoverMods();
            int hotLoaded = LoadMods(initialLoad: false);
            RuntimeMonitor.Log($"Workshop ModListChanged hook dispatched. discoveredMods={discoveredMods.Count} hotLoaded={hotLoaded}");
            Events.DispatchWorkshopModListChanged(discoveredMods.Count);
        }

        public void SetHookStatus(string hookId, string status, string source, string details)
        {
            if (Diagnostics.SetHookStatus(hookId, status, source, details))
            {
                Events.DispatchHookStatusChanged(hookId, status);
                RuntimeMonitor.Log($"Hook status: {hookId} = {status}. {details}");
            }
        }

        public void RegisterRuntimeApi<TApi>(IManifest owner, TApi api) where TApi : class
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (api == null)
                throw new ArgumentNullException(nameof(api));
            ModRegistry.AddLoaded(owner);
            ModRegistry.RegisterApi(owner, api);
            RuntimeMonitor.Log("Registered runtime API " + typeof(TApi).FullName + " from " + owner.UniqueID + ".");
        }

        public string ExportLogs()
        {
            string report = Diagnostics.ExportLogs();
            Events.DispatchLogExported(report);
            RuntimeMonitor.Log("Exported DTMAPI logs to " + report);
            return report;
        }

        public RuntimeSnapshot CreateSnapshot()
        {
            return new RuntimeSnapshot(
                startedAt,
                Paths,
                discoveredMods.ToArray(),
                loadedMods.ToArray(),
                ModRegistry.GetAll(),
                Diagnostics.GetErrors(),
                Diagnostics.GetHookStatuses(),
                configMenuApi?.GetPages() ?? new IConfigMenuPage[0],
                UI.LastExportPath);
        }

        public IReadOnlyList<IContentItemInfo> GetIndexedContentItems() => Content.GetIndexedItems();

        public IContentItemInfo? GetIndexedContentItem(string itemId) => Content.GetIndexedItem(itemId);

        private void DiscoverMods()
        {
            Stopwatch scan = Stopwatch.StartNew();
            discoveredMods.Clear();
            discoveredById.Clear();
            var scanner = new ModScanner(Paths);
            foreach (DiscoveredMod mod in scanner.Discover())
            {
                discoveredMods.Add(mod);
                if (!string.IsNullOrWhiteSpace(mod.Manifest.UniqueID) && !discoveredById.ContainsKey(mod.Manifest.UniqueID))
                    discoveredById.Add(mod.Manifest.UniqueID, mod);
            }
            foreach (string error in scanner.Errors)
            {
                Diagnostics.RecordError("DTMAPI.ModScanner", "Manifest discovery failed.", error);
                RuntimeMonitor.Log("Manifest discovery failed: " + error, LogLevel.Warn);
            }
            Workshop.SetMods(discoveredMods);
            Stopwatch content = Stopwatch.StartNew();
            Content.Rebuild(discoveredMods);
            RuntimeMonitor.Log($"Discovered {discoveredMods.Count} DTMAPI-capable mod folder(s).");
            RuntimeMonitor.Log("Official content item source index = " + Content.IndexedItemCount + " item row(s) from " + Content.IndexedItemSourceCount + " source mod(s).");
            RuntimeMonitor.Log(
                "Official local MODS root = " + scanner.OfficialLocalModsRoot +
                "; exists=" + scanner.OfficialLocalModsRootExists +
                "; folders=" + scanner.OfficialLocalDirectoryCount +
                "; enablementFile=" + scanner.OfficialEnablementFilePath +
                "; enablementFileExists=" + scanner.OfficialEnablementFileExists +
                "; enablementEntries=" + scanner.OfficialEnablementEntryCount + ".");
            RuntimeMonitor.Log("Startup segment ManifestScan elapsedMs=" + scanner.ManifestScanElapsedMilliseconds + "; OfficialModsScan elapsedMs=" + scanner.OfficialModsScanElapsedMilliseconds + "; WorkshopScan elapsedMs=" + scanner.WorkshopScanElapsedMilliseconds + "; ContentQueryIndex elapsedMs=" + content.ElapsedMilliseconds + "; DiscoverMods totalMs=" + scan.ElapsedMilliseconds + ".");
        }

        private int LoadMods(bool initialLoad)
        {
            if (initialLoad)
            {
                loadedMods.Clear();
                modInstances.Clear();
            }

            int loadedNow = 0;
            foreach (DiscoveredMod mod in OrderMods(discoveredMods))
            {
                bool alreadyLoaded = IsLoadedMod(mod.Manifest.UniqueID);
                if (!mod.OfficialEnabled)
                {
                    string reason = string.IsNullOrWhiteSpace(mod.EnablementReason) ? "已由来源管理路径禁用。" : mod.EnablementReason;
                    if (alreadyLoaded)
                        RuntimeMonitor.LogOnce(
                            "loaded-mod-disabled-" + mod.Manifest.UniqueID,
                            mod.Manifest.UniqueID + " 已经加载；官方禁用会在重启游戏后完全停用，本次运行不尝试卸载 DLL。",
                            LogLevel.Warn);
                    else
                        RuntimeMonitor.Log($"Skipping {mod.Manifest.UniqueID}: {reason}");
                    continue;
                }

                if (alreadyLoaded)
                    continue;

                if (!CanLoadDependencies(mod))
                    continue;

                if (!mod.Manifest.Type.Equals("CodeMod", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(mod.Manifest.Type))
                {
                    RuntimeMonitor.Log($"Indexed non-code mod {mod.Manifest.UniqueID} ({mod.Manifest.Type}).");
                    loadedMods.Add(mod);
                    ModRegistry.AddLoaded(mod.Manifest);
                    loadedNow++;
                    continue;
                }

                if (LoadCodeMod(mod))
                    loadedNow++;
            }
            RefreshConfigPageLocks();
            return loadedNow;
        }

        private void RefreshConfigPageLocks()
        {
            if (configMenuApi == null)
                return;

            foreach (IConfigMenuPage page in configMenuApi.GetPages())
            {
                if (!discoveredById.TryGetValue(page.Manifest.UniqueID, out DiscoveredMod discovered))
                {
                    configMenuApi.SetPageLock(page.Manifest.UniqueID, true, "DTMAPI 当前不再发现此 Mod。");
                    continue;
                }

                bool loaded = IsLoadedMod(page.Manifest.UniqueID);
                if (!discovered.OfficialEnabled && loaded)
                    configMenuApi.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        "此 Mod 已经加载；官方禁用会在重启游戏后完全停用。本次运行锁定配置编辑。");
                else if (!discovered.OfficialEnabled)
                    configMenuApi.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        string.IsNullOrWhiteSpace(discovered.EnablementReason) ? "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。" : discovered.EnablementReason);
                else if (!loaded)
                    configMenuApi.SetPageLock(page.Manifest.UniqueID, true, "此 Mod 尚未加载；请重启游戏或先检查依赖错误，再编辑配置。");
                else
                    configMenuApi.SetPageLock(page.Manifest.UniqueID, false, string.Empty);
            }
        }

        private bool LoadCodeMod(DiscoveredMod mod)
        {
            string dllPath = Path.Combine(mod.RootPath, mod.Manifest.EntryDll ?? string.Empty);
            if (string.IsNullOrWhiteSpace(mod.Manifest.EntryDll) || !File.Exists(dllPath))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", dllPath);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失，路径 " + dllPath + "。", LogLevel.Warn);
                return false;
            }

            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);
                Type? entryType = assembly.GetTypes().FirstOrDefault(t => typeof(DtmMod).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
                if (entryType == null)
                    throw new InvalidOperationException("No concrete DtmMod entry type with a parameterless constructor was found.");

                var modInstance = (DtmMod)Activator.CreateInstance(entryType);
                IMonitor monitor = new FileMonitor(host, mod.Manifest.UniqueID, Diagnostics.LatestLogPath);
                modInstance.AttachContext(mod.Manifest, monitor);
                var helper = new DtmHelper(
                    mod.Manifest,
                    monitor,
                    Events.CreateProxy(mod.Manifest.UniqueID),
                    Config,
                    ModRegistry,
                    Workshop,
                    UI,
                    Diagnostics,
                    Content,
                    Input,
                    new TranslationService(mod.RootPath, TranslationService.DetectLanguage()));
                modInstance.Entry(helper);
                modInstances[mod.Manifest.UniqueID] = modInstance;
                loadedMods.Add(mod);
                ModRegistry.AddLoaded(mod.Manifest);
                monitor.Log("Mod Entry completed.");
                return true;
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "Failed to load code mod.", ex.ToString());
                RuntimeMonitor.LogException(ex, $"Failed to load {mod.Manifest.UniqueID}.");
                return false;
            }
        }

        private bool IsLoadedMod(string uniqueId)
        {
            return loadedMods.Any(m => m.Manifest.UniqueID.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanLoadDependencies(DiscoveredMod mod)
        {
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                if (!dependency.Required)
                    continue;
                if (ModRegistry.IsLoaded(dependency.UniqueID))
                    continue;
                Diagnostics.RecordError(mod.Manifest.UniqueID, "缺少必需依赖。", dependency.UniqueID);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：缺少依赖 " + dependency.UniqueID + "。", LogLevel.Warn);
                return false;
            }
            return true;
        }

        private static IReadOnlyList<DiscoveredMod> OrderMods(IReadOnlyList<DiscoveredMod> mods)
        {
            var byId = mods.Where(m => !string.IsNullOrWhiteSpace(m.Manifest.UniqueID)).ToDictionary(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<DiscoveredMod>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DiscoveredMod mod in mods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
                Visit(mod, byId, ordered, visiting, visited);

            return ordered;
        }

        private static void Visit(DiscoveredMod mod, Dictionary<string, DiscoveredMod> byId, List<DiscoveredMod> ordered, HashSet<string> visiting, HashSet<string> visited)
        {
            string id = mod.Manifest.UniqueID;
            if (visited.Contains(id))
                return;
            if (!visiting.Add(id))
                return;
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                if (byId.TryGetValue(dependency.UniqueID, out DiscoveredMod dependencyMod))
                    Visit(dependencyMod, byId, ordered, visiting, visited);
            }
            visiting.Remove(id);
            visited.Add(id);
            ordered.Add(mod);
        }

        private static IManifest CreateRuntimeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI",
                Author = "DTMAPI",
                Version = ApiVersion,
                UniqueID = "DTMAPI",
                Type = "Runtime"
            };
        }

        private static IManifest CreateConfigMenuManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Mod Config Menu",
                Author = "DTMAPI",
                Version = ApiVersion,
                UniqueID = "DTMAPI.ModConfigMenu",
                Type = "RuntimeApi"
            };
        }
    }

    public sealed class RuntimeSnapshot
    {
        public RuntimeSnapshot(
            DateTimeOffset startedAt,
            RuntimePaths paths,
            IReadOnlyList<DiscoveredMod> discoveredMods,
            IReadOnlyList<DiscoveredMod> loadedMods,
            IReadOnlyList<IManifest> registry,
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IConfigMenuPage> configPages,
            string lastExportPath)
        {
            StartedAt = startedAt;
            Paths = paths;
            DiscoveredMods = discoveredMods;
            LoadedMods = loadedMods;
            Registry = registry;
            Errors = errors;
            HookStatuses = hookStatuses;
            ConfigPages = configPages;
            LastExportPath = lastExportPath;
        }

        public DateTimeOffset StartedAt { get; }
        public RuntimePaths Paths { get; }
        public IReadOnlyList<DiscoveredMod> DiscoveredMods { get; }
        public IReadOnlyList<DiscoveredMod> LoadedMods { get; }
        public IReadOnlyList<IManifest> Registry { get; }
        public IReadOnlyList<IDtmErrorInfo> Errors { get; }
        public IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        public IReadOnlyList<IConfigMenuPage> ConfigPages { get; }
        public string LastExportPath { get; }
    }
}
