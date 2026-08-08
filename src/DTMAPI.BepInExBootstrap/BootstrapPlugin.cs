using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using BepInEx;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.BepInExBootstrap
{
    [BepInPlugin("dev.dtmapi.bootstrap", "DTMAPI Bootstrap", DtmApiRuntime.BinaryVersion)]
    public sealed class BootstrapPlugin : BaseUnityPlugin
    {
        private DtmApiRuntime? runtime;
        private DolocTownGameBridge? bridge;
        private DebugConsoleCompatibilityProxy? debugConsoleCompatibility;
        private ReflectedTitleMenuSettingsUi? titleSettingsUi;
        private BepInExRuntimeHost? host;
        private bool initialized;
        private bool frameSourceLogged;
        private bool updateCallbackSeen;
        private bool inputSystemFrameCallbackSeen;
        private bool inputSystemFrameDriverSubscribed;
        private bool inputSystemFrameDriverErrorLogged;
        private bool inputSystemRecoveryAwaitingCallback;
        private bool inputSystemRecoveryFailureLogged;
        private bool playerLoopFrameDriverInstalled;
        private bool playerLoopFrameCallbackSeen;
        private bool nativeGameFrameCallbackSeen;
        private bool bridgeUpdateErrorLogged;
        private bool titleUiUpdateErrorLogged;
        private bool debugCompatibilityUpdateErrorLogged;
        private int updateFailureCount;
        private int fallbackTickQueued;
        private int shutdownStarted;
        private SynchronizationContext? unityContext;
        private Timer? fallbackPump;
        private DateTimeOffset lastUpdateFailurePublishedAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastFallbackTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastUnityUpdateTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastInputSystemFrameTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastPlayerLoopFrameTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastNativeGameFrameTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastInputSystemFrameDriverAttempt = DateTimeOffset.MinValue;
        private EventInfo? inputSystemAfterUpdateEvent;
        private Delegate? inputSystemAfterUpdateHandler;
        private int lastProcessedUnityFrame = -1;
        private long inputSystemFrameCallbackCount;
        private long playerLoopFrameCallbackCount;
        private long nativeGameFrameCallbackCount;
        private long unityUpdateCallbackCount;
        private FrameDriverHealthSnapshot queuedFrameDriverHealthSnapshot;
        private FrameDriverHealthProbeKind queuedFrameDriverHealthProbeKind;
        private string diagnosticsHotkey = "None";
        private readonly List<InputButtonSample> inputFrameSamples = new List<InputButtonSample>();
        private bool startupAttempted;
        private bool startupPrepared;

        public void OnEnable()
        {
            runtime?.RuntimeMonitor.Log("Unity OnEnable observed by DTMAPI bootstrap.");
        }

        public void Awake()
        {
            Stopwatch startup = Stopwatch.StartNew();
            try
            {
                Logger.LogInfo("DTMAPI startup segment Bootstrap.Awake begin.");
                host = new BepInExRuntimeHost(Logger);
                ConfigurePersistentRoot();
                Logger.LogInfo("DTMAPI startup segment BepInEx init elapsedMs=" + startup.ElapsedMilliseconds + ".");
                var configMenu = new ConfigMenuRegistry();
                runtime = new DtmApiRuntime(host, configMenu);
                titleSettingsUi = new ReflectedTitleMenuSettingsUi(runtime, configMenu);
                runtime.AddRuntimeReportContextProvider(BuildBootstrapRuntimeReportContext);
                runtime.AddTitleReturnObjectGraphProvider(BuildBootstrapTitleReturnObjectGraphSection);
                runtime.AddTitleReturnObjectGraphProvider(BuildBootstrapUiOwnerObjectGraphSection);
                runtime.SaveSessionLoaded += (slot, isNewGame) => ReflectedUnityInput.ClearTransientState();
                runtime.SaveSessionLoaded += (slot, isNewGame) => ReinstallPlayerLoopFrameDriver("SaveLoaded");
                runtime.SaveSessionLoaded += (slot, isNewGame) => titleSettingsUi?.ResetForSaveBoundary(slot, isNewGame);
                runtime.ReturnedToTitleBoundary += () => ReflectedUnityInput.ClearTransientState();
                runtime.ReturnedToTitleBoundary += () => ReinstallPlayerLoopFrameDriver("ReturnedToTitle");
                runtime.ReturnedToTitleBoundary += () => titleSettingsUi?.ResetForTitleBoundary();
                debugConsoleCompatibility =
                    new DebugConsoleCompatibilityProxy(runtime);
                runtime.SaveSessionLoaded +=
                    (slot, isNewGame) =>
                        debugConsoleCompatibility?
                            .ResetForSaveBoundaryIfLoaded(
                                slot,
                                isNewGame);
                runtime.ReturnedToTitleBoundary +=
                    () =>
                        debugConsoleCompatibility?
                            .ResetForTitleBoundaryIfLoaded();
                runtime.RegisterRuntimeApi<IDebugConsoleApi>(
                    new ManifestModel
                    {
                        Name = "DTMAPI Debug Console Compatibility",
                        Author = "DTMAPI",
                        Version = DtmApiRuntime.ApiVersion,
                        UniqueID = "DTMAPI.DebugConsoleHost",
                        Type = "RuntimeApi"
                    },
                    debugConsoleCompatibility,
                    OwnerBoundGameBridgeApis.ForDebugConsole(
                        debugConsoleCompatibility));
                PreparedQaHost? preparedQaHost = QaHostActivationLoader.LoadIfRequested(runtime);
                bridge = new DolocTownGameBridge(
                    runtime,
                    debugConsoleCompatibility,
                    preparedQaHost);
                runtime.NativeLoadGameReturned += (slot, result) => ReinstallPlayerLoopFrameDriver("LoadGameReturned:" + slot.ToString(CultureInfo.InvariantCulture) + ":" + result.ToString(CultureInfo.InvariantCulture));
                runtime.NativeGameFrame += OnNativeGameFrame;
                unityContext = SynchronizationContext.Current;
                diagnosticsHotkey = ResolveDiagnosticsHotkey();
                startupPrepared = true;
                TryInstallPlayerLoopFrameDriver();
                Logger.LogInfo("DTMAPI Bootstrap prepared; native source capture and Runtime start are deferred until the first PlayerLoop frame after native Awake initialization.");
                Logger.LogInfo("DTMAPI startup segment Bootstrap.Awake prepared elapsedMs=" + startup.ElapsedMilliseconds + ".");
            }
            catch (Exception ex)
            {
                Logger.LogError("DTMAPI bootstrap failed: " + ex);
            }
        }

        private TitleReturnObjectGraphSection BuildBootstrapTitleReturnObjectGraphSection()
        {
            return new TitleReturnObjectGraphSection(
                "BootstrapUi",
                "bootstrap={fallbackPumpAlive=" + (fallbackPump == null ? "false" : "true") +
                "; fallbackTickQueued=" + fallbackTickQueued +
                "; updateFailures=" + updateFailureCount +
                "; initialized=" + initialized +
                "; updateCallbackSeen=" + updateCallbackSeen +
                "; inputSystemFrameDriverSubscribed=" + inputSystemFrameDriverSubscribed +
                "; inputSystemFrameCallbackSeen=" + inputSystemFrameCallbackSeen +
                "; playerLoopFrameDriverInstalled=" + playerLoopFrameDriverInstalled +
                "; playerLoopFrameCallbackSeen=" + playerLoopFrameCallbackSeen +
                "; nativeGameFrameCallbackSeen=" + nativeGameFrameCallbackSeen +
                "; unityUpdateCallbacks=" + unityUpdateCallbackCount +
                "; unityContextAlive=" + (unityContext == null ? "false" : "true") + "}" +
                "; titleSettings={" + (titleSettingsUi?.GetLifecycleSummary() ?? "not-created") + "}; debugConsole={" + (debugConsoleCompatibility?.GetLifecycleSummary() ?? "resident-dormant") + "}" +
                "; uiByOwner={" + (titleSettingsUi?.GetOwnerObjectGraphSummary() ?? "DTMAPI.TitleSettings={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "; " + (debugConsoleCompatibility?.GetOwnerObjectGraphSummary() ?? "DTMAPI.DebugConsole={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") + "}");
        }

        private TitleReturnObjectGraphSection BuildBootstrapUiOwnerObjectGraphSection()
        {
            return new TitleReturnObjectGraphSection(
                "UI",
                "byOwner={" +
                (titleSettingsUi?.GetOwnerObjectGraphSummary() ?? "DTMAPI.TitleSettings={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "; " +
                (debugConsoleCompatibility?.GetOwnerObjectGraphSummary() ?? "DTMAPI.DebugConsole={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "}");
        }

        private string BuildBootstrapRuntimeReportContext()
        {
            var builder = new StringBuilder();
            builder.AppendLine("BootstrapLifecycle: fallbackPumpAlive=" + (fallbackPump == null ? "false" : "true") +
                "; fallbackTickQueued=" + fallbackTickQueued +
                "; updateFailures=" + updateFailureCount +
                "; initialized=" + initialized +
                "; updateCallbackSeen=" + updateCallbackSeen +
                "; inputSystemFrameDriverSubscribed=" + inputSystemFrameDriverSubscribed +
                "; inputSystemFrameCallbackSeen=" + inputSystemFrameCallbackSeen +
                "; inputSystemFrameCallbacks=" + inputSystemFrameCallbackCount +
                "; playerLoopFrameDriverInstalled=" + playerLoopFrameDriverInstalled +
                "; playerLoopFrameCallbackSeen=" + playerLoopFrameCallbackSeen +
                "; playerLoopFrameCallbacks=" + playerLoopFrameCallbackCount +
                "; nativeGameFrameCallbackSeen=" + nativeGameFrameCallbackSeen +
                "; nativeGameFrameCallbacks=" + nativeGameFrameCallbackCount +
                "; unityUpdateCallbacks=" + unityUpdateCallbackCount +
                "; lastProcessedUnityFrame=" + lastProcessedUnityFrame +
                "; unityContextAlive=" + (unityContext == null ? "false" : "true"));
            builder.AppendLine("BootstrapUiLifecycle: titleSettings={" + (titleSettingsUi?.GetLifecycleSummary() ?? "not-created") + "}; debugConsole={" + (debugConsoleCompatibility?.GetLifecycleSummary() ?? "resident-dormant") + "}");
            return builder.ToString();
        }

        private void ConfigurePersistentRoot()
        {
            string existing = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(existing))
                return;

            string persistentDataPath = ResolveUnityPersistentDataPath();
            if (string.IsNullOrWhiteSpace(persistentDataPath))
                return;

            string fullPath = Path.GetFullPath(persistentDataPath);
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", fullPath);
            Logger.LogInfo("DTMAPI persistent root resolved from Unity: " + fullPath);
        }

        private static string ResolveUnityPersistentDataPath()
        {
            try
            {
                Type? application = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule")
                    ?? Type.GetType("UnityEngine.Application, UnityEngine");
                PropertyInfo? property = application?.GetProperty("persistentDataPath", BindingFlags.Public | BindingFlags.Static);
                return property?.GetValue(null) as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void StartFallbackPump()
        {
            try
            {
                fallbackPump = new Timer(_ => QueueFallbackTick(), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
                runtime?.RuntimeMonitor.Log("DTMAPI fallback tick pump armed.");
            }
            catch (Exception ex)
            {
                runtime?.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Fallback health pump is unavailable.", ex.ToString());
                runtime?.RuntimeMonitor.Log("DTMAPI fallback health pump could not start; frame drivers remain active. " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private void QueueFallbackTick()
        {
            if (Volatile.Read(ref shutdownStarted) != 0 || !initialized || runtime == null)
                return;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FrameDriverHealthSnapshot current = CaptureFrameDriverHealthSnapshot();
            FrameDriverHealthProbeKind probeKind = FrameDriverHealthPolicy.GetProbeKind(current, now);
            if (probeKind == FrameDriverHealthProbeKind.None)
                return;
            if (Interlocked.Exchange(ref fallbackTickQueued, 1) == 1)
                return;
            queuedFrameDriverHealthSnapshot = current;
            queuedFrameDriverHealthProbeKind = probeKind;

            SynchronizationContext? context = unityContext;
            if (context != null)
            {
                context.Post(_ =>
                {
                    try
                    {
                        if (Volatile.Read(ref shutdownStarted) == 0 && initialized)
                            RunFallbackHealthCheck();
                    }
                    finally
                    {
                        Interlocked.Exchange(ref fallbackTickQueued, 0);
                    }
                }, null);
                return;
            }

            Interlocked.Exchange(ref fallbackTickQueued, 0);
        }

        private void RunFallbackHealthCheck()
        {
            if (Volatile.Read(ref shutdownStarted) != 0 || !initialized)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            FrameDriverHealthSnapshot current = CaptureFrameDriverHealthSnapshot();
            FrameDriverHealthDecision decision = FrameDriverHealthPolicy.Evaluate(
                queuedFrameDriverHealthProbeKind,
                queuedFrameDriverHealthSnapshot,
                current,
                now);
            if (decision == FrameDriverHealthDecision.RecoverInputSystem)
            {
                bool previousRecoveryDidNotResume = inputSystemRecoveryAwaitingCallback;
                if (previousRecoveryDidNotResume && !inputSystemRecoveryFailureLogged)
                {
                    inputSystemRecoveryFailureLogged = true;
                    string persistentDetails = "inputCallbacks=" + current.InputSystemCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; playerLoopCallbacks=" + current.PlayerLoopCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; nativeCallbacks=" + current.NativeCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; unityUpdateCallbacks=" + current.UnityUpdateCallbackCount.ToString(CultureInfo.InvariantCulture) + ".";
                    runtime?.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Input System frame driver did not resume after a verified recovery attempt.", persistentDetails);
                    runtime?.RuntimeMonitor.Log("DTMAPI Input System frame driver did not resume after a verified recovery attempt. " + persistentDetails, LogLevel.Warn);
                }
                UnsubscribeInputSystemFrameDriver();
                lastInputSystemFrameDriverAttempt = DateTimeOffset.MinValue;
                bool recovered = TrySubscribeInputSystemFrameDriver();
                if (recovered)
                {
                    inputSystemRecoveryAwaitingCallback = true;
                }
                else
                {
                    string details = "inputCallbacks=" + current.InputSystemCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; playerLoopCallbacks=" + current.PlayerLoopCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; nativeCallbacks=" + current.NativeCallbackCount.ToString(CultureInfo.InvariantCulture) +
                        "; unityUpdateCallbacks=" + current.UnityUpdateCallbackCount.ToString(CultureInfo.InvariantCulture) + ".";
                    runtime?.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Input System frame driver recovery failed while sibling Unity drivers were progressing.", details);
                    runtime?.RuntimeMonitor.Log("DTMAPI Input System frame driver recovery failed while sibling Unity drivers were progressing. " + details, LogLevel.Warn);
                }
                return;
            }

            // A global startup/load/pause gap is not evidence that one driver
            // failed. Keep every subscription intact and wait for relative
            // progress. A missing subscription may still retry on its normal
            // bounded cadence.
            if (!current.InputSystemSubscribed)
                TrySubscribeInputSystemFrameDriver();
        }

        private bool TrySubscribeInputSystemFrameDriver()
        {
            if (Volatile.Read(ref shutdownStarted) != 0)
                return false;

            if (inputSystemFrameDriverSubscribed)
                return true;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (lastInputSystemFrameDriverAttempt != DateTimeOffset.MinValue && now - lastInputSystemFrameDriverAttempt < TimeSpan.FromSeconds(10))
                return false;
            lastInputSystemFrameDriverAttempt = now;
            try
            {
                Type? inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
                EventInfo? afterUpdate = inputSystemType?.GetEvent("onAfterUpdate", BindingFlags.Public | BindingFlags.Static);
                Type? handlerType = afterUpdate?.EventHandlerType;
                MethodInfo? callback = GetType().GetMethod(nameof(OnInputSystemAfterUpdate), BindingFlags.Instance | BindingFlags.NonPublic);
                if (afterUpdate == null || handlerType == null || callback == null)
                    return false;

                Delegate handler = Delegate.CreateDelegate(handlerType, this, callback);
                afterUpdate.AddEventHandler(null, handler);
                inputSystemAfterUpdateEvent = afterUpdate;
                inputSystemAfterUpdateHandler = handler;
                inputSystemFrameDriverSubscribed = true;
                runtime?.RuntimeMonitor.Log("DTMAPI per-frame driver subscribed to InputSystem.onAfterUpdate.");
                return true;
            }
            catch (Exception ex)
            {
                if (!inputSystemFrameDriverErrorLogged)
                {
                    inputSystemFrameDriverErrorLogged = true;
                    runtime?.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Input System per-frame driver subscription failed.", ex.GetType().Name + ": " + ex.Message);
                }
                return false;
            }
        }

        private void OnInputSystemAfterUpdate()
        {
            if (!initialized || runtime == null)
                return;

            inputSystemFrameCallbackSeen = true;
            Interlocked.Increment(ref inputSystemFrameCallbackCount);
            lastInputSystemFrameTick = DateTimeOffset.Now;
            inputSystemRecoveryFailureLogged = false;
            if (inputSystemRecoveryAwaitingCallback)
            {
                inputSystemRecoveryAwaitingCallback = false;
                runtime.RuntimeMonitor.Log("DTMAPI Input System frame driver recovered after an isolated callback stall; PlayerLoop/native progress remained healthy.");
            }
            int frame = ReflectedUnityInput.GetUnityFrameCount();
            ReflectedUnityInput.LatchInputFrame(runtime.GetInputButtonsToSample());
            if (playerLoopFrameDriverInstalled)
                return;
            if (frame >= 0 && frame == lastProcessedUnityFrame)
                return;

            TickFromUnity("InputSystemAfterUpdate");
        }

        private void TryInstallPlayerLoopFrameDriver()
        {
            if (Volatile.Read(ref shutdownStarted) != 0)
                return;

            if (playerLoopFrameDriverInstalled)
                return;
            try
            {
                Type? playerLoopType = Type.GetType("UnityEngine.LowLevel.PlayerLoop, UnityEngine.CoreModule");
                Type? playerLoopSystemType = Type.GetType("UnityEngine.LowLevel.PlayerLoopSystem, UnityEngine.CoreModule");
                Type? updateGroupType = Type.GetType("UnityEngine.PlayerLoop.Update, UnityEngine.CoreModule");
                MethodInfo? getCurrent = playerLoopType?.GetMethod("GetCurrentPlayerLoop", BindingFlags.Public | BindingFlags.Static);
                MethodInfo? setCurrent = playerLoopType?.GetMethod("SetPlayerLoop", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? typeField = playerLoopSystemType?.GetField("type", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo? systemsField = playerLoopSystemType?.GetField("subSystemList", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo? updateDelegateField = playerLoopSystemType?.GetField("updateDelegate", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo? callback = GetType().GetMethod(nameof(OnPlayerLoopUpdate), BindingFlags.Instance | BindingFlags.NonPublic);
                if (playerLoopSystemType == null || getCurrent == null || setCurrent == null || typeField == null || systemsField == null || updateDelegateField == null || callback == null)
                    return;

                object root = getCurrent.Invoke(null, null)!;
                RemovePlayerLoopDriverNoLock(ref root, playerLoopSystemType, typeField, systemsField);
                Delegate handler = Delegate.CreateDelegate(updateDelegateField.FieldType, this, callback);
                object driver = Activator.CreateInstance(playerLoopSystemType)!;
                typeField.SetValue(driver, typeof(BootstrapPlugin));
                updateDelegateField.SetValue(driver, handler);
                if (!AppendPlayerLoopDriverNoLock(ref root, updateGroupType, driver, typeField, systemsField))
                    AppendPlayerLoopChildNoLock(ref root, driver, playerLoopSystemType, systemsField);
                setCurrent.Invoke(null, new[] { root });
                playerLoopFrameDriverInstalled = true;
                runtime?.RuntimeMonitor.Log("DTMAPI scene-stable frame driver installed in Unity PlayerLoop.Update.");
            }
            catch (Exception ex)
            {
                runtime?.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Unity PlayerLoop frame driver installation failed.", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void ReinstallPlayerLoopFrameDriver(string reason)
        {
            if (Volatile.Read(ref shutdownStarted) != 0)
                return;

            playerLoopFrameDriverInstalled = false;
            playerLoopFrameCallbackSeen = false;
            TryInstallPlayerLoopFrameDriver();
            runtime?.RuntimeMonitor.Log("DTMAPI PlayerLoop frame driver refreshed at lifecycle boundary " + reason + ". installed=" + playerLoopFrameDriverInstalled + ".");
        }

        private static bool AppendPlayerLoopDriverNoLock(ref object node, Type? targetGroupType, object driver, FieldInfo typeField, FieldInfo systemsField)
        {
            if (targetGroupType != null && typeField.GetValue(node) is Type nodeType && nodeType == targetGroupType)
            {
                AppendPlayerLoopChildNoLock(ref node, driver, node.GetType(), systemsField);
                return true;
            }

            if (!(systemsField.GetValue(node) is Array children))
                return false;
            for (int i = 0; i < children.Length; i++)
            {
                object? child = children.GetValue(i);
                if (child == null || !AppendPlayerLoopDriverNoLock(ref child, targetGroupType, driver, typeField, systemsField))
                    continue;
                children.SetValue(child, i);
                systemsField.SetValue(node, children);
                return true;
            }
            return false;
        }

        private static void AppendPlayerLoopChildNoLock(ref object node, object driver, Type playerLoopSystemType, FieldInfo systemsField)
        {
            Array? existing = systemsField.GetValue(node) as Array;
            int count = existing?.Length ?? 0;
            Array updated = Array.CreateInstance(playerLoopSystemType, count + 1);
            if (existing != null)
                Array.Copy(existing, updated, count);
            updated.SetValue(driver, count);
            systemsField.SetValue(node, updated);
        }

        private static bool RemovePlayerLoopDriverNoLock(ref object node, Type playerLoopSystemType, FieldInfo typeField, FieldInfo systemsField)
        {
            if (!(systemsField.GetValue(node) is Array children) || children.Length == 0)
                return false;

            var retained = new List<object>(children.Length);
            bool changed = false;
            for (int i = 0; i < children.Length; i++)
            {
                object? child = children.GetValue(i);
                if (child == null)
                    continue;
                if (typeField.GetValue(child) is Type childType && childType == typeof(BootstrapPlugin))
                {
                    changed = true;
                    continue;
                }
                if (RemovePlayerLoopDriverNoLock(ref child, playerLoopSystemType, typeField, systemsField))
                    changed = true;
                retained.Add(child);
            }
            if (!changed)
                return false;

            Array updated = Array.CreateInstance(playerLoopSystemType, retained.Count);
            for (int i = 0; i < retained.Count; i++)
                updated.SetValue(retained[i], i);
            systemsField.SetValue(node, updated);
            return true;
        }

        private void OnPlayerLoopUpdate()
        {
            if (!startupAttempted)
                TryStartRuntimeOnce("PlayerLoop.FirstFrame");
            if (!initialized || runtime == null)
                return;
            playerLoopFrameCallbackSeen = true;
            Interlocked.Increment(ref playerLoopFrameCallbackCount);
            lastPlayerLoopFrameTick = DateTimeOffset.Now;
            if (nativeGameFrameCallbackSeen && (lastPlayerLoopFrameTick - lastNativeGameFrameTick).TotalMilliseconds < 250)
                return;
            ReflectedUnityInput.LatchInputFrame(runtime.GetInputButtonsToSample());
            TickFromUnity("PlayerLoop");
        }

        private void OnNativeGameFrame()
        {
            if (!initialized || runtime == null)
                return;
            nativeGameFrameCallbackSeen = true;
            Interlocked.Increment(ref nativeGameFrameCallbackCount);
            lastNativeGameFrameTick = DateTimeOffset.Now;
            ReflectedUnityInput.LatchInputFrame(runtime.GetInputButtonsToSample());
            TickFromUnity("NativeGameUpdate");
        }

        private void RemovePlayerLoopFrameDriver()
        {
            if (!playerLoopFrameDriverInstalled)
                return;
            try
            {
                Type? playerLoopType = Type.GetType("UnityEngine.LowLevel.PlayerLoop, UnityEngine.CoreModule");
                Type? playerLoopSystemType = Type.GetType("UnityEngine.LowLevel.PlayerLoopSystem, UnityEngine.CoreModule");
                MethodInfo? getCurrent = playerLoopType?.GetMethod("GetCurrentPlayerLoop", BindingFlags.Public | BindingFlags.Static);
                MethodInfo? setCurrent = playerLoopType?.GetMethod("SetPlayerLoop", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? typeField = playerLoopSystemType?.GetField("type", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo? systemsField = playerLoopSystemType?.GetField("subSystemList", BindingFlags.Public | BindingFlags.Instance);
                if (playerLoopSystemType != null && getCurrent != null && setCurrent != null && typeField != null && systemsField != null)
                {
                    object root = getCurrent.Invoke(null, null)!;
                    if (RemovePlayerLoopDriverNoLock(ref root, playerLoopSystemType, typeField, systemsField))
                        setCurrent.Invoke(null, new[] { root });
                }
            }
            catch
            {
            }
            playerLoopFrameDriverInstalled = false;
            playerLoopFrameCallbackSeen = false;
        }

        private void UnsubscribeInputSystemFrameDriver()
        {
            if (inputSystemAfterUpdateEvent != null && inputSystemAfterUpdateHandler != null)
            {
                try
                {
                    inputSystemAfterUpdateEvent.RemoveEventHandler(null, inputSystemAfterUpdateHandler);
                }
                catch
                {
                }
            }
            inputSystemAfterUpdateEvent = null;
            inputSystemAfterUpdateHandler = null;
            inputSystemFrameDriverSubscribed = false;
            inputSystemFrameCallbackSeen = false;
            inputSystemRecoveryAwaitingCallback = false;
            lastProcessedUnityFrame = -1;
        }

        public void Start()
        {
            TryStartRuntimeOnce("Unity.Start");
        }

        private void TryStartRuntimeOnce(string source)
        {
            if (initialized)
                return;
            if (startupAttempted)
            {
                Logger.LogError("DTMAPI Bootstrap Start was invoked again after an incomplete startup attempt; refusing a partial retry.");
                return;
            }
            startupAttempted = true;
            if (!startupPrepared || runtime == null || bridge == null)
            {
                Logger.LogError("DTMAPI Bootstrap Start could not continue because Awake preparation did not complete.");
                return;
            }

            Stopwatch startup = Stopwatch.StartNew();
            bool runtimeStartEntered = false;
            try
            {
                Logger.LogInfo("DTMAPI startup segment Bootstrap.StartRuntime begin source=" + source + ".");
                bridge.CaptureNativeWorkshopSubscriptions("Bootstrap." + source + ".PreRuntime");
                bridge.PrepareQaHostBeforeRuntimeStart();
                runtimeStartEntered = true;
                runtime.Start();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.RuntimeStart elapsedMs=" + startup.ElapsedMilliseconds + ".");
                runtime.RuntimeMonitor.Log(
                    IsDiagnosticsHotkeyEnabled()
                        ? "DTMAPI diagnostics hotkey is dev-enabled on " + diagnosticsHotkey + "."
                        : "DTMAPI diagnostics hotkey is disabled by default; use the title-page DTMAPI Settings entry.");
                bridge.Initialize();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.HarmonyInitialize elapsedMs=" + startup.ElapsedMilliseconds + ".");
                TryInstallPlayerLoopFrameDriver();
                TrySubscribeInputSystemFrameDriver();
                StartFallbackPump();
                initialized = true;
                Logger.LogInfo("DTMAPI Bootstrap loaded.");
                runtime.RuntimeMonitor.Log("Native-ready Unity lifecycle observed by DTMAPI bootstrap source=" + source + ". Startup segment Bootstrap.StartRuntime totalMs=" + startup.ElapsedMilliseconds + ".");
            }
            catch (Exception ex)
            {
                Logger.LogError("DTMAPI bootstrap Start failed: " + ex);
                try
                {
                    if (!runtimeStartEntered)
                        bridge?.AbortQaHostBeforeRuntimeStart(ex.GetType().Name);
                    else
                        bridge?.RetainQaHostAndRequestApplicationQuitAfterRuntimeStartFailure(ex.GetType().Name);
                }
                catch (Exception cleanupFailure)
                {
                    Logger.LogError(
                        runtimeStartEntered
                            ? "DTMAPI bootstrap could not request bounded exit while retaining the optional pre-Runtime QA owner after Runtime Start entered: " + cleanupFailure
                            : "DTMAPI bootstrap could not release the optional pre-Runtime QA owner before Runtime Start: " + cleanupFailure);
                }
            }
        }

        public void Update()
        {
            if (!startupAttempted)
                TryStartRuntimeOnce("Unity.Update.FirstFrame");
            if (initialized)
            {
                updateCallbackSeen = true;
                lastUnityUpdateTick = DateTimeOffset.Now;
                Interlocked.Increment(ref unityUpdateCallbackCount);
            }
            TickFromUnity("Update");
        }

        public void FixedUpdate()
        {
            TickFromUnity("FixedUpdate");
        }

        public void LateUpdate()
        {
            TickFromUnity("LateUpdate");
        }

        private void TickFromUnity(string source)
        {
            if (!initialized || runtime == null)
                return;
            if (source != "NativeGameUpdate" && nativeGameFrameCallbackSeen && (DateTimeOffset.Now - lastNativeGameFrameTick).TotalMilliseconds < 250)
                return;
            if (playerLoopFrameDriverInstalled && source != "PlayerLoop" && source != "NativeGameUpdate")
                return;
            if (!playerLoopFrameDriverInstalled && inputSystemFrameDriverSubscribed && source != "InputSystemAfterUpdate" && source != "NativeGameUpdate")
                return;
            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                bool primaryFrameSource = source == "NativeGameUpdate" || source == "PlayerLoop" || source == "InputSystemAfterUpdate" || source == "Update";
                if (source != "Update" && source != "NativeGameUpdate" && source != "PlayerLoop" && source != "InputSystemAfterUpdate" &&
                    ((nativeGameFrameCallbackSeen && (now - lastNativeGameFrameTick).TotalMilliseconds < 500) ||
                     (playerLoopFrameCallbackSeen && (now - lastPlayerLoopFrameTick).TotalMilliseconds < 500) ||
                     (inputSystemFrameCallbackSeen && (now - lastInputSystemFrameTick).TotalMilliseconds < 500) ||
                     (updateCallbackSeen && (now - lastUnityUpdateTick).TotalMilliseconds < 500)))
                    return;

                int unityFrame = ReflectedUnityInput.GetUnityFrameCount();
                if (unityFrame >= 0)
                {
                    if (unityFrame == lastProcessedUnityFrame)
                        return;
                    lastProcessedUnityFrame = unityFrame;
                }
                else if (!primaryFrameSource && (now - lastFallbackTick).TotalMilliseconds < 50)
                    return;
                else if (!primaryFrameSource)
                    lastFallbackTick = now;

                if (!frameSourceLogged)
                {
                    frameSourceLogged = true;
                    runtime.RuntimeMonitor.Log("Unity frame callback observed by DTMAPI bootstrap: " + source);
                }
                bool allowUnityApi = true;
                if (allowUnityApi)
                {
                    try
                    {
                        bridge?.Update();
                    }
                    catch (Exception ex)
                    {
                        RecordUpdateComponentError("DTMAPI.GameBridge", "GameBridge update failed.", ex, ref bridgeUpdateErrorLogged);
                    }

                    try
                    {
                        titleSettingsUi?.Update();
                    }
                    catch (Exception ex)
                    {
                        RecordUpdateComponentError("DTMAPI.TitleSettings", "Title settings UI update failed.", ex, ref titleUiUpdateErrorLogged);
                    }

                    try
                    {
                        debugConsoleCompatibility?.UpdateIfLoaded();
                    }
                    catch (Exception ex)
                    {
                        RecordUpdateComponentError(
                            "DTMAPI.DebugConsoleCompatibility",
                            "Frozen DebugConsole compatibility update failed.",
                            ex,
                            ref debugCompatibilityUpdateErrorLogged);
                    }
                }

                bool uiCapturingKey = titleSettingsUi != null && titleSettingsUi.IsCapturingKey;
                if (allowUnityApi && !uiCapturingKey && IsDiagnosticsHotkeyEnabled() && ReflectedUnityInput.GetKeyDown(diagnosticsHotkey))
                {
                    bool wasOpen = runtime.UI.IsOpen;
                    runtime.UI.Toggle();
                    runtime.RuntimeMonitor.Log("DTMAPI diagnostics hotkey " + diagnosticsHotkey + " observed. open=" + wasOpen + " -> " + runtime.UI.IsOpen + " context=" + runtime.UI.InputContext + ".");
                }

                bool compatibilityConsumedInput =
                    debugConsoleCompatibility?.ConsumedInputThisFrame == true;
                if (allowUnityApi &&
                    !uiCapturingKey &&
                    !compatibilityConsumedInput)
                    SampleDtmInputFrame();
                else
                    ReflectedUnityInput.DiscardLatchedEdges();

                runtime.Update();
            }
            catch (Exception ex)
            {
                RecordBootstrapUpdateError(ex);
            }
        }

        private FrameDriverHealthSnapshot CaptureFrameDriverHealthSnapshot()
        {
            return new FrameDriverHealthSnapshot(
                inputSystemFrameDriverSubscribed,
                inputSystemFrameCallbackSeen,
                playerLoopFrameCallbackSeen,
                nativeGameFrameCallbackSeen,
                updateCallbackSeen,
                Interlocked.Read(ref inputSystemFrameCallbackCount),
                Interlocked.Read(ref playerLoopFrameCallbackCount),
                Interlocked.Read(ref nativeGameFrameCallbackCount),
                Interlocked.Read(ref unityUpdateCallbackCount),
                lastInputSystemFrameTick,
                lastInputSystemFrameDriverAttempt,
                lastPlayerLoopFrameTick,
                lastNativeGameFrameTick,
                lastUnityUpdateTick);
        }

        private void RecordBootstrapUpdateError(Exception ex)
        {
            if (runtime == null)
                return;

            updateFailureCount++;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string summary = ex.GetType().Name + ": " + ex.Message;
            if (updateFailureCount <= 3)
            {
                lastUpdateFailurePublishedAt = now;
                runtime.Diagnostics.RecordError("DTMAPI.BepInExBootstrap", "Update failed.", ex.ToString());
                runtime.RuntimeMonitor.Log("Bootstrap Update failed count=" + updateFailureCount + " " + summary, LogLevel.Warn);
                return;
            }

            if ((now - lastUpdateFailurePublishedAt).TotalSeconds < 30)
                return;

            lastUpdateFailurePublishedAt = now;
            string details = "count=" + updateFailureCount + ", lastError=" + summary;
            runtime.Diagnostics.RecordWarning("DTMAPI.BepInExBootstrap", "Repeated Update failures throttled.", details);
            runtime.RuntimeMonitor.Log("Throttled bootstrap Update failures " + details, LogLevel.Warn);
        }

        private void RecordUpdateComponentError(string owner, string message, Exception ex, ref bool alreadyLogged)
        {
            if (runtime == null || alreadyLogged)
                return;
            alreadyLogged = true;
            runtime.Diagnostics.RecordError(owner, message, ex.ToString());
            runtime.RuntimeMonitor.Log(message + " " + ex.GetType().Name + ": " + ex.Message, DTMAPI.Abstractions.LogLevel.Warn);
        }

        private void SampleDtmInputFrame()
        {
            if (runtime == null)
                return;

            IReadOnlyList<string> buttons = runtime.GetInputButtonsToSample();

            inputFrameSamples.Clear();
            foreach (string button in buttons)
            {
                if (string.IsNullOrWhiteSpace(button))
                    continue;

                InputButtonSample sample = ReflectedUnityInput.SampleButtonCached(button);
                inputFrameSamples.Add(sample);
            }

            runtime.RecordInputFrame(inputFrameSamples);
        }

        private bool IsDiagnosticsHotkeyEnabled()
        {
            return !string.IsNullOrWhiteSpace(diagnosticsHotkey) && !diagnosticsHotkey.Equals("None", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveDiagnosticsHotkey()
        {
            string enabled = Environment.GetEnvironmentVariable("DTMAPI_ENABLE_DIAGNOSTICS_HOTKEY") ?? string.Empty;
            if (enabled.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                enabled.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                enabled.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return "F8";

            string configured = Environment.GetEnvironmentVariable("DTMAPI_DIAGNOSTICS_HOTKEY") ?? string.Empty;
            configured = configured.Trim();
            return string.IsNullOrWhiteSpace(configured) ? "None" : configured;
        }

        public void OnApplicationQuit()
        {
            if (Interlocked.Exchange(ref shutdownStarted, 1) != 0)
                return;

            initialized = false;
            fallbackPump?.Dispose();
            fallbackPump = null;
            RemovePlayerLoopFrameDriver();
            UnsubscribeInputSystemFrameDriver();
            ReflectedUnityInput.ClearTransientState();
            titleSettingsUi?.Shutdown("Unity OnApplicationQuit");
            debugConsoleCompatibility?.ShutdownIfLoaded(
                "Unity OnApplicationQuit");
            bridge?.Shutdown("Unity OnApplicationQuit");
            runtime?.NotifyRuntimeShutdown("Unity OnApplicationQuit");
            runtime?.RuntimeMonitor.Log("Unity OnApplicationQuit observed by DTMAPI bootstrap.");
        }


    }
}
