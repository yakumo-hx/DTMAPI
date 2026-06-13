using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;

namespace DTMAPI.Core.Runtime
{
    public sealed class DtmApiRuntime : IDtmDiagnosticsApi
    {
        public const string ApiVersion = "0.5.1-alpha";
        public const string BinaryVersion = "0.5.1.0";

        private readonly IRuntimeHost host;
        private readonly IDtmConfigMenuApi? configMenuApi;
        private readonly IConfigMenuRuntime? configMenuRuntime;
        private readonly List<DiscoveredMod> discoveredMods = new List<DiscoveredMod>();
        private readonly List<DiscoveredMod> loadedMods = new List<DiscoveredMod>();
        private readonly Dictionary<string, DiscoveredMod> discoveredById = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmMod> modInstances = new Dictionary<string, DtmMod>(StringComparer.OrdinalIgnoreCase);
        private readonly DateTimeOffset startedAt = DateTimeOffset.Now;
        private DateTimeOffset lastSecondTick = DateTimeOffset.Now;
        private ulong updateTick;
        private uint secondTick;
        private int? currentLoadingSlot;
        private int runtimeThreadId;
        private bool started;

        public DtmApiRuntime(IRuntimeHost host, IDtmConfigMenuApi? configMenuApi = null)
        {
            this.host = host;
            this.configMenuApi = configMenuApi;
            configMenuRuntime = configMenuApi as IConfigMenuRuntime;
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            Paths = new RuntimePaths(host.GamePath, host.PluginPath);
            Diagnostics = new DiagnosticsService(Paths);
            Events = new EventManager(Diagnostics);
            Config = new ConfigService(Paths, Diagnostics);
            ModRegistry = new ModRegistryService();
            Workshop = new WorkshopService();
            Content = new ContentQueryService(Paths);
            Input = new InputService();
            CustomEntities = new CustomEntityRegistryService(Diagnostics);
            UI = new UiRuntimeService(
                ExportLogs,
                Events.DispatchMenuOpened,
                Events.DispatchMenuClosed,
                Diagnostics.RecordError,
                (message, level) => RuntimeMonitor.Log(message, level));
            UI.ManagerModelProvider = new DtmManagerRuntimeModelProvider(this, ExportLogs, () => ManagerInstallStateSummary.FromRuntimePaths(Paths));
        }

        public RuntimePaths Paths { get; }
        public DiagnosticsService Diagnostics { get; }
        public UiRuntimeService UI { get; }
        internal IConfigMenuRuntime? ConfigMenuRuntime => configMenuRuntime;
        internal EventManager Events { get; }
        internal ConfigService Config { get; }
        internal ModRegistryService ModRegistry { get; }
        internal WorkshopService Workshop { get; }
        internal ContentQueryService Content { get; }
        internal InputService Input { get; }
        public CustomEntityRegistryService CustomEntities { get; }
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
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
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

            IManifest runtimeManifest = CreateRuntimeManifest();
            RegisterRuntimeApi<IDtmDiagnosticsApi>(runtimeManifest, this);
            RegisterRuntimeApi<ICustomAnimalApi>(runtimeManifest, CustomEntities);
            RegisterRuntimeApi<ICustomMonsterApi>(runtimeManifest, CustomEntities);
            RegisterRuntimeApi<ICustomAttackApi>(runtimeManifest, CustomEntities);
            RegisterRuntimeApi<ICustomDroneApi>(runtimeManifest, CustomEntities);
            if (configMenuApi != null)
            {
                IManifest configMenuManifest = CreateConfigMenuManifest();
                ModRegistry.AddLoaded(configMenuManifest);
                ModRegistry.RegisterApiForOwner<IDtmConfigMenuApi>(configMenuManifest, configMenuApi);
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
            if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
            {
                RuntimeMonitor.LogOnce(
                    "timer-fallback-runtime-update-skipped",
                    "Skipped ordinary runtime.Update dispatch from a non-runtime thread; TimerFallback must not deliver mod update callbacks.",
                    LogLevel.Warn);
                return;
            }

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
            CustomEntities.BeginSaveSession(currentLoadingSlot, isNewGame);
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
            CustomEntities.ClearRuntimeInstances("returned-to-title");
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
            ModRegistry.RegisterApiForOwner(owner, api);
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
                Diagnostics.GetWarnings(),
                Diagnostics.GetHookStatuses(),
                Diagnostics.GetFeatureStatuses(),
                configMenuRuntime?.GetPages() ?? new IConfigMenuPage[0],
                Diagnostics.GetLatestLogPath(),
                Diagnostics.GetLatestReportPath(),
                UI.LastExportPath);
        }

        public IDtmDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            IReadOnlyList<IDtmErrorInfo> errors = Diagnostics.GetErrors();
            return new DtmDiagnosticsSnapshot(
                startedAt,
                loadedMods.Select(m => new DtmLoadedModInfo(m.Manifest)).Cast<IDtmLoadedModInfo>().ToArray(),
                CreateModStatusSnapshot(errors),
                errors,
                Diagnostics.GetWarnings(),
                Diagnostics.GetHookStatuses(),
                Diagnostics.GetFeatureStatuses(),
                Diagnostics.GetLatestLogPath(),
                Diagnostics.GetLatestReportPath());
        }

        IDtmDiagnosticsSnapshot IDtmDiagnosticsApi.GetSnapshot() => CreateDiagnosticsSnapshot();

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
            HashSet<string> dependencyCycleBlockedIds;
            foreach (DiscoveredMod mod in OrderMods(discoveredMods, out dependencyCycleBlockedIds))
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

                if (!CanLoadApiVersion(mod))
                    continue;

                if (!CanLoadGameVersion(mod))
                    continue;

                if (dependencyCycleBlockedIds.Contains(mod.Manifest.UniqueID))
                {
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "依赖循环阻止加载。", "This mod is part of a dependency cycle and is blocked until the cycle is resolved.");
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：此 Mod 处于依赖循环中，已标记为 blocked。", LogLevel.Warn);
                    continue;
                }

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

        private IReadOnlyList<IDtmModStatusInfo> CreateModStatusSnapshot(IReadOnlyList<IDtmErrorInfo> errors)
        {
            HashSet<string> loadedIds = new HashSet<string>(loadedMods.Select(m => m.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<IDtmErrorInfo>> errorsByOwner = errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Owner))
                .GroupBy(e => e.Owner, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            List<IDtmModStatusInfo> rows = new List<IDtmModStatusInfo>();

            foreach (DiscoveredMod mod in discoveredMods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                bool loaded = loadedIds.Contains(mod.Manifest.UniqueID);
                string status;
                string statusCode;
                string reason;

                if (!mod.OfficialEnabled)
                {
                    status = "disabled";
                    statusCode = "disabled";
                    reason = string.IsNullOrWhiteSpace(mod.EnablementReason)
                        ? "Disabled by the source enablement path."
                        : mod.EnablementReason;
                    if (loaded)
                        reason += " Already loaded in this process; restart is required for DLL unload.";
                }
                else if (errorsByOwner.TryGetValue(mod.Manifest.UniqueID, out List<IDtmErrorInfo>? modErrors) && modErrors.Count > 0)
                {
                    status = "error";
                    statusCode = GetModStatusCode(status, modErrors);
                    reason = string.Join(" | ", modErrors.Select(error => error.Message + (string.IsNullOrWhiteSpace(error.Details) ? string.Empty : " " + error.Details)).ToArray());
                }
                else if (loaded)
                {
                    status = "loaded";
                    statusCode = "loaded";
                    reason = "Loaded by DTMAPI runtime.";
                }
                else
                {
                    status = "discovered";
                    statusCode = "discovered";
                    reason = "Discovered by DTMAPI but not loaded yet.";
                }

                rows.Add(new DtmModStatusInfo(
                    mod.Manifest.UniqueID,
                    mod.Manifest.Name,
                    mod.Manifest.Version,
                    mod.Manifest.Type,
                    mod.Source,
                    mod.OfficialId,
                    mod.OfficialEnabled,
                    mod.OfficialEnablementManaged,
                    mod.EnablementReason,
                    mod.Manifest.EntryDll,
                    mod.Manifest.EntryType,
                    loaded,
                    status,
                    statusCode,
                    reason,
                    mod.ManifestPath,
                    mod.RootPath));
            }

            return rows;
        }

        private static string GetModStatusCode(string status, IReadOnlyList<IDtmErrorInfo> errors)
        {
            if (!status.Equals("error", StringComparison.OrdinalIgnoreCase))
                return status ?? string.Empty;
            foreach (IDtmErrorInfo error in errors)
            {
                string code = GetModErrorStatusCode(error);
                if (!code.Equals("unknown-error", StringComparison.OrdinalIgnoreCase))
                    return code;
            }
            return "unknown-error";
        }

        private static string GetModErrorStatusCode(IDtmErrorInfo error)
        {
            string message = error?.Message ?? string.Empty;
            string details = error?.Details ?? string.Empty;
            string combined = message + " " + details;
            if (combined.IndexOf("依赖循环", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("dependency cycle", StringComparison.OrdinalIgnoreCase) >= 0)
                return "dependency-cycle";
            if (combined.IndexOf("缺少必需依赖", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("依赖声明缺少", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("依赖版本", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("dependency", StringComparison.OrdinalIgnoreCase) >= 0 && combined.IndexOf("requires", StringComparison.OrdinalIgnoreCase) >= 0)
                return "missing-dependency";
            if (combined.IndexOf("DTMAPI API 版本", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("MinimumDTMApiVersion", StringComparison.OrdinalIgnoreCase) >= 0)
                return "api-too-new";
            if (combined.IndexOf("EntryDll", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("EntryType", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("DtmMod 入口", StringComparison.OrdinalIgnoreCase) >= 0)
                return "entry-dll-error";
            if (combined.IndexOf("Failed to load code mod", StringComparison.OrdinalIgnoreCase) >= 0)
                return "code-load-error";
            return "unknown-error";
        }

        private void RefreshConfigPageLocks()
        {
            if (configMenuRuntime == null)
                return;

            foreach (IConfigMenuPage page in configMenuRuntime.GetPages())
            {
                if (!discoveredById.TryGetValue(page.Manifest.UniqueID, out DiscoveredMod discovered))
                {
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, true, "DTMAPI 当前不再发现此 Mod。");
                    continue;
                }

                bool loaded = IsLoadedMod(page.Manifest.UniqueID);
                if (!discovered.OfficialEnabled && loaded)
                    configMenuRuntime.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        "此 Mod 已经加载；官方禁用会在重启游戏后完全停用。本次运行锁定配置编辑。");
                else if (!discovered.OfficialEnabled)
                    configMenuRuntime.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        string.IsNullOrWhiteSpace(discovered.EnablementReason) ? "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。" : discovered.EnablementReason);
                else if (!loaded)
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, true, "此 Mod 尚未加载；请重启游戏或先检查依赖错误，再编辑配置。");
                else
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, false, string.Empty);
            }
        }

        private bool LoadCodeMod(DiscoveredMod mod)
        {
            if (!TryResolveEntryDllPath(mod, out string dllPath))
                return false;

            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);
                if (!TryResolveEntryType(mod, assembly, out Type? entryType))
                    return false;

                var modInstance = (DtmMod)Activator.CreateInstance(entryType);
                IMonitor monitor = new FileMonitor(host, mod.Manifest.UniqueID, Diagnostics.LatestLogPath);
                modInstance.AttachContext(mod.Manifest, monitor);
                var helper = new DtmHelper(
                    mod.Manifest,
                    monitor,
                    Events.CreateProxy(mod.Manifest.UniqueID),
                    Config,
                    ModRegistry.CreateOwnerBoundRegistry(mod.Manifest),
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
                if (string.IsNullOrWhiteSpace(dependency.UniqueID))
                {
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "依赖声明缺少 UniqueID。", "Dependency UniqueID is empty.");
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：依赖声明缺少 UniqueID。", LogLevel.Warn);
                    return false;
                }

                IManifest? loadedDependency = ModRegistry.Get(dependency.UniqueID);
                if (loadedDependency == null)
                {
                    if (!dependency.Required)
                        continue;
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "缺少必需依赖。", dependency.UniqueID);
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：缺少依赖 " + dependency.UniqueID + "。", LogLevel.Warn);
                    return false;
                }

                if (IsVersionRequirementSatisfied(dependency.MinimumVersion, loadedDependency.Version, out string dependencyVersionReason))
                    continue;

                string message = dependency.Required ? "必需依赖版本过低。" : "可选依赖版本过低。";
                string details = dependency.UniqueID + " requires >= " + dependency.MinimumVersion + ", loaded " + loadedDependency.Version + ". " + dependencyVersionReason;
                if (dependency.Required)
                    Diagnostics.RecordError(mod.Manifest.UniqueID, message, details);
                else
                    Diagnostics.RecordWarning(mod.Manifest.UniqueID, message, details);
                RuntimeMonitor.Log(
                    (dependency.Required ? "跳过 " + mod.Manifest.UniqueID + "：" : "诊断 " + mod.Manifest.UniqueID + "：") +
                    dependency.UniqueID + " 版本不满足要求 " + dependency.MinimumVersion + "，当前 " + loadedDependency.Version + "。",
                    LogLevel.Warn);
                if (dependency.Required)
                    return false;
            }
            return true;
        }

        private bool CanLoadApiVersion(DiscoveredMod mod)
        {
            string minimum = mod.Manifest.MinimumDTMApiVersion;
            if (string.IsNullOrWhiteSpace(minimum))
                return true;

            if (IsVersionRequirementSatisfied(minimum, ApiVersion, out string reason))
                return true;

            Diagnostics.RecordError(mod.Manifest.UniqueID, "DTMAPI API 版本不满足要求。", "MinimumDTMApiVersion=" + minimum + ", runtime=" + ApiVersion + ". " + reason);
            RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：需要 DTMAPI API >= " + minimum + "，当前 " + ApiVersion + "。", LogLevel.Warn);
            return false;
        }

        private bool CanLoadGameVersion(DiscoveredMod mod)
        {
            string minimum = mod.Manifest.MinimumGameVersion;
            if (string.IsNullOrWhiteSpace(minimum))
                return true;

            Diagnostics.RecordWarning(
                mod.Manifest.UniqueID,
                "MinimumGameVersion cannot be verified.",
                "MinimumGameVersion=" + minimum + "; the current runtime host cannot detect the Doloc Town game version, so DTMAPI continues loading and records this as a structured warning.");
            RuntimeMonitor.Log(
                "MinimumGameVersion warning for " + mod.Manifest.UniqueID + ": manifest requires Doloc Town >= " + minimum + ", but the current runtime host cannot detect the game version; DTMAPI will continue loading and treat this as an explicit warning instead of silently ignoring it.",
                LogLevel.Warn);
            return true;
        }

        private bool TryResolveEntryDllPath(DiscoveredMod mod, out string dllPath)
        {
            dllPath = string.Empty;
            string entryDll = (mod.Manifest.EntryDll ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(entryDll))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", mod.RootPath);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失。", LogLevel.Warn);
                return false;
            }

            if (Path.IsPathRooted(entryDll))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 必须是相对路径。", entryDll);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 必须是相对路径，不能使用 " + entryDll + "。", LogLevel.Warn);
                return false;
            }

            string root = Path.GetFullPath(mod.RootPath);
            string resolved = Path.GetFullPath(Path.Combine(root, entryDll));
            string rootWithSeparator = EnsureTrailingDirectorySeparator(root);
            if (!resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 不能逃出 Mod 根目录。", "EntryDll=" + entryDll + ", resolved=" + resolved + ", root=" + root);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 不能逃出 Mod 根目录。", LogLevel.Warn);
                return false;
            }

            if (!Path.GetExtension(resolved).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 必须指向 .dll 文件。", "EntryDll=" + entryDll + ", resolved=" + resolved);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 必须指向 .dll 文件。", LogLevel.Warn);
                return false;
            }

            if (!File.Exists(resolved))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", resolved);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失，路径 " + resolved + "。", LogLevel.Warn);
                return false;
            }

            dllPath = resolved;
            return true;
        }

        private bool TryResolveEntryType(DiscoveredMod mod, Assembly assembly, out Type? entryType)
        {
            entryType = null;
            Type[] entryTypes = assembly.GetTypes()
                .Where(t => typeof(DtmMod).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToArray();

            string requestedEntryType = (mod.Manifest.EntryType ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(requestedEntryType))
            {
                Type[] matches = entryTypes
                    .Where(t =>
                        string.Equals(t.FullName, requestedEntryType, StringComparison.Ordinal) ||
                        string.Equals(t.Name, requestedEntryType, StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length == 1)
                {
                    entryType = matches[0];
                    return true;
                }

                string details = "EntryType=" + requestedEntryType + "; candidates=" + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                Diagnostics.RecordError(
                    mod.Manifest.UniqueID,
                    matches.Length == 0 ? "EntryType 未找到。" : "EntryType 匹配多个 DtmMod 类型。",
                    details);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType " + requestedEntryType + (matches.Length == 0 ? " 未在 DLL 中找到。" : " 匹配多个 DtmMod 类型。"), LogLevel.Warn);
                return false;
            }

            if (entryTypes.Length == 0)
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "未找到 DtmMod 入口类型。", "No concrete DtmMod entry type with a parameterless constructor was found.");
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：DLL 内没有可构造的 DtmMod 入口类型。", LogLevel.Warn);
                return false;
            }

            if (entryTypes.Length > 1)
            {
                string details = "EntryType is required when a DLL contains multiple DtmMod subclasses: " + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryType 缺失且 DLL 内包含多个 DtmMod 子类。", details);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType 缺失且 DLL 内包含多个 DtmMod 子类，请在 manifest 中填写 EntryType。", LogLevel.Warn);
                return false;
            }

            entryType = entryTypes[0];
            return true;
        }

        private IReadOnlyList<DiscoveredMod> OrderMods(IReadOnlyList<DiscoveredMod> mods, out HashSet<string> dependencyCycleBlockedIds)
        {
            dependencyCycleBlockedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var byId = mods.Where(m => !string.IsNullOrWhiteSpace(m.Manifest.UniqueID)).ToDictionary(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<DiscoveredMod>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reportedCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DiscoveredMod mod in mods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
                Visit(mod, byId, ordered, visiting, visited, new List<string>(), reportedCycles, dependencyCycleBlockedIds);

            return ordered;
        }

        private void Visit(
            DiscoveredMod mod,
            Dictionary<string, DiscoveredMod> byId,
            List<DiscoveredMod> ordered,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> stack,
            HashSet<string> reportedCycles,
            HashSet<string> dependencyCycleBlockedIds)
        {
            string id = mod.Manifest.UniqueID;
            if (visited.Contains(id))
                return;
            if (!visiting.Add(id))
            {
                int index = stack.FindIndex(value => value.Equals(id, StringComparison.OrdinalIgnoreCase));
                IEnumerable<string> cycle = index >= 0 ? stack.Skip(index).Concat(new[] { id }) : stack.Concat(new[] { id });
                foreach (string cycleId in cycle.Distinct(StringComparer.OrdinalIgnoreCase))
                    dependencyCycleBlockedIds.Add(cycleId);
                string cycleText = string.Join(" -> ", cycle.ToArray());
                if (reportedCycles.Add(cycleText))
                {
                    Diagnostics.RecordError("DTMAPI.ModLoader", "检测到 Mod 依赖循环。", cycleText);
                    RuntimeMonitor.Log("检测到 Mod 依赖循环：" + cycleText + "。", LogLevel.Warn);
                }
                return;
            }
            stack.Add(id);
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                if (byId.TryGetValue(dependency.UniqueID, out DiscoveredMod dependencyMod))
                    Visit(dependencyMod, byId, ordered, visiting, visited, stack, reportedCycles, dependencyCycleBlockedIds);
            }
            stack.RemoveAt(stack.Count - 1);
            visiting.Remove(id);
            visited.Add(id);
            ordered.Add(mod);
        }

        private static bool IsVersionRequirementSatisfied(string minimumVersion, string actualVersion, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(minimumVersion))
                return true;

            if (!TryParseVersion(minimumVersion, out Version minimum))
            {
                reason = "Cannot parse required version.";
                return false;
            }

            if (!TryParseVersion(actualVersion, out Version actual))
            {
                reason = "Cannot parse actual version.";
                return false;
            }

            bool satisfied = CompareVersions(actual, minimum) >= 0;
            if (!satisfied)
                reason = "Actual version is older than the required minimum.";
            return satisfied;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = new Version(0, 0, 0, 0);
            string text = (value ?? string.Empty).Trim();
            int suffixIndex = text.IndexOfAny(new[] { '-', '+' });
            if (suffixIndex >= 0)
                text = text.Substring(0, suffixIndex);
            return Version.TryParse(text, out version);
        }

        private static int CompareVersions(Version actual, Version minimum)
        {
            int[] left = { actual.Major, actual.Minor, Math.Max(actual.Build, 0), Math.Max(actual.Revision, 0) };
            int[] right = { minimum.Major, minimum.Minor, Math.Max(minimum.Build, 0), Math.Max(minimum.Revision, 0) };
            for (int i = 0; i < left.Length; i++)
            {
                int comparison = left[i].CompareTo(right[i]);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;
            return path + Path.DirectorySeparatorChar;
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
            IReadOnlyList<IDtmWarningInfo> warnings,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IDtmFeatureStatusInfo> featureStatuses,
            IReadOnlyList<IConfigMenuPage> configPages,
            string latestLogPath,
            string latestReportPath,
            string lastExportPath)
        {
            StartedAt = startedAt;
            Paths = paths;
            DiscoveredMods = discoveredMods;
            LoadedMods = loadedMods;
            Registry = registry;
            Errors = errors;
            Warnings = warnings;
            HookStatuses = hookStatuses;
            FeatureStatuses = featureStatuses;
            ConfigPages = configPages;
            LatestLogPath = latestLogPath;
            LatestReportPath = latestReportPath;
            LastExportPath = lastExportPath;
        }

        public DateTimeOffset StartedAt { get; }
        public RuntimePaths Paths { get; }
        public IReadOnlyList<DiscoveredMod> DiscoveredMods { get; }
        public IReadOnlyList<DiscoveredMod> LoadedMods { get; }
        public IReadOnlyList<IManifest> Registry { get; }
        public IReadOnlyList<IDtmErrorInfo> Errors { get; }
        public IReadOnlyList<IDtmWarningInfo> Warnings { get; }
        public IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        public IReadOnlyList<IDtmFeatureStatusInfo> FeatureStatuses { get; }
        public IReadOnlyList<IConfigMenuPage> ConfigPages { get; }
        public string LatestLogPath { get; }
        public string LatestReportPath { get; }
        public string LastExportPath { get; }
    }
}
