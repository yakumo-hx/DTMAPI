using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using DTMAPI.Abstractions;
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
        private ReflectedTitleMenuSettingsUi? titleSettingsUi;
        private BepInExRuntimeHost? host;
        private bool initialized;
        private bool playerLoopFrameDriverInstalled;
        private bool bridgeUpdateErrorLogged;
        private bool titleUiUpdateErrorLogged;
        private int updateFailureCount;
        private bool shutdownStarted;
        private DateTimeOffset lastUpdateFailurePublishedAt = DateTimeOffset.MinValue;
        private EventInfo? applicationQuittingEvent;
        private Delegate? applicationQuittingHandler;
        private int? lastProcessedUnityFrame;
        private int? lastNativeUnityFrame;
        private string diagnosticsHotkey = "None";
        private readonly List<InputButtonSample> inputFrameSamples = new List<InputButtonSample>();
        private bool startupAttempted;
        private bool startupPrepared;

        public BootstrapPlugin() { }

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
                runtime = new DtmApiRuntime(host, configMenu, new HarmonyModPatchCleanup());
                runtime.RegisterRuntimeModule("DTMAPI.Monsters");
                runtime.RegisterRuntimeModule("DTMAPI.Audio");
                runtime.RegisterRuntimeModule("DTMAPI.Animals");
                titleSettingsUi = new ReflectedTitleMenuSettingsUi(runtime, configMenu);
                runtime.AppendBgmGroups = titleSettingsUi.ReadStartupBgmAppend();
                runtime.SaveSessionLoaded += (slot, isNewGame) => ReflectedUnityInput.ClearTransientState();
                runtime.SaveSessionLoaded += (slot, isNewGame) => ReinstallPlayerLoopFrameDriver("SaveLoaded");
                runtime.SaveSessionLoaded += (slot, isNewGame) => titleSettingsUi?.ResetForSaveBoundary(slot, isNewGame);
                runtime.ReturnedToTitleBoundary += () => ReflectedUnityInput.ClearTransientState();
                runtime.ReturnedToTitleBoundary += () => ReinstallPlayerLoopFrameDriver("ReturnedToTitle");
                runtime.ReturnedToTitleBoundary += () => titleSettingsUi?.ResetForTitleBoundary();
                bridge = new DolocTownGameBridge(runtime);
                runtime.NativeLoadGameReturned += (slot, result) => ReinstallPlayerLoopFrameDriver("LoadGameReturned:" + slot.ToString(CultureInfo.InvariantCulture) + ":" + result.ToString(CultureInfo.InvariantCulture));
                runtime.NativeGameFrame += OnNativeGameFrame;
                diagnosticsHotkey = ResolveDiagnosticsHotkey();
                startupPrepared = true;
                SubscribeApplicationQuitting();
                TryInstallPlayerLoopFrameDriver();
                Logger.LogInfo("DTMAPI Bootstrap prepared; native source capture and Runtime start are deferred until the first PlayerLoop frame after native Awake initialization.");
                Logger.LogInfo("DTMAPI startup segment Bootstrap.Awake prepared elapsedMs=" + startup.ElapsedMilliseconds + ".");
            }
            catch (Exception ex)
            {
                Logger.LogError("DTMAPI bootstrap failed: " + ex);
            }
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

        private void TryInstallPlayerLoopFrameDriver()
        {
            if (shutdownStarted || playerLoopFrameDriverInstalled)
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
            if (shutdownStarted)
                return;

            playerLoopFrameDriverInstalled = false;
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
            if (!initialized)
                return;

            int unityFrame = ReflectedUnityInput.GetUnityFrameCount();
            if (lastNativeUnityFrame == unchecked(unityFrame - 1))
                return;
            TickFromUnity(unityFrame);
        }

        private void OnNativeGameFrame()
        {
            if (!initialized)
                return;

            int unityFrame = ReflectedUnityInput.GetUnityFrameCount();
            lastNativeUnityFrame = unityFrame;
            TickFromUnity(unityFrame);
        }

        private void RemovePlayerLoopFrameDriver()
        {
            if (!playerLoopFrameDriverInstalled)
                return;
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
            playerLoopFrameDriverInstalled = false;
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
            try
            {
                Logger.LogInfo("DTMAPI startup segment Bootstrap.StartRuntime begin source=" + source + ".");
                if (ReflectedUnityInput.GetUnityFrameCount() == -1)
                    Logger.LogError("DTMAPI 无法读取 UnityEngine.Time.frameCount；每帧更新无法继续。");
                bridge.CaptureNativeWorkshopSubscriptions("Bootstrap." + source + ".PreRuntime");
                runtime.Start();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.RuntimeStart elapsedMs=" + startup.ElapsedMilliseconds + ".");
                runtime.RuntimeMonitor.Log(
                    IsDiagnosticsHotkeyEnabled()
                        ? "DTMAPI diagnostics hotkey is dev-enabled on " + diagnosticsHotkey + "."
                        : "DTMAPI diagnostics hotkey is disabled by default; use the title-page DTMAPI Settings entry.");
                bridge.Initialize();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.HarmonyInitialize elapsedMs=" + startup.ElapsedMilliseconds + ".");
                TryInstallPlayerLoopFrameDriver();
                initialized = true;
                Logger.LogInfo("DTMAPI Bootstrap loaded.");
                runtime.RuntimeMonitor.Log("Native-ready Unity lifecycle observed by DTMAPI bootstrap source=" + source + ". Startup segment Bootstrap.StartRuntime totalMs=" + startup.ElapsedMilliseconds + ".");
            }
            catch (Exception ex)
            {
                Logger.LogError("DTMAPI bootstrap Start failed: " + ex);
            }
        }

        private void TickFromUnity(int unityFrame)
        {
            if (lastProcessedUnityFrame == unityFrame)
                return;
            lastProcessedUnityFrame = unityFrame;

            try
            {
                DtmApiRuntime activeRuntime = runtime!;
                ReflectedUnityInput.LatchInputFrame(activeRuntime.GetInputButtonsToSample());
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

                bool uiCapturingKey = titleSettingsUi != null && titleSettingsUi.IsCapturingKey;
                bool diagnosticsHandledInput = false;
                if (!uiCapturingKey && IsDiagnosticsHotkeyEnabled() && ReflectedUnityInput.GetKeyDown(diagnosticsHotkey))
                {
                    diagnosticsHandledInput = true;
                    bool wasOpen = activeRuntime.UI.IsOpen;
                    activeRuntime.UI.Toggle();
                    activeRuntime.RuntimeMonitor.Log("DTMAPI diagnostics hotkey " + diagnosticsHotkey + " observed. open=" + wasOpen + " -> " + activeRuntime.UI.IsOpen + " context=" + activeRuntime.UI.InputContext + ".");
                }

                if (!uiCapturingKey)
                    SampleDtmInputFrame();
                else
                    ReflectedUnityInput.DiscardLatchedEdges();

                if (!diagnosticsHandledInput)
                    titleSettingsUi?.ProcessEntryAction();
                activeRuntime.Update();
            }
            catch (Exception ex)
            {
                RecordBootstrapUpdateError(ex);
            }
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
            DtmApiRuntime activeRuntime = runtime!;
            IReadOnlyList<string> buttons = activeRuntime.GetInputButtonsToSample();

            inputFrameSamples.Clear();
            foreach (string button in buttons)
            {
                InputButtonSample sample = ReflectedUnityInput.SampleButtonCached(button);
                inputFrameSamples.Add(sample);
            }

            activeRuntime.RecordInputFrame(inputFrameSamples);
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
            Shutdown("Unity OnApplicationQuit");
        }

        private void SubscribeApplicationQuitting()
        {
            try
            {
                Type? application = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule")
                    ?? Type.GetType("UnityEngine.Application, UnityEngine");
                EventInfo? quitting = application?.GetEvent("quitting", BindingFlags.Public | BindingFlags.Static);
                if (quitting?.EventHandlerType == null)
                    throw new MissingMemberException("UnityEngine.Application.quitting");
                MethodInfo callback = GetType().GetMethod(nameof(OnApplicationQuitting), BindingFlags.Instance | BindingFlags.NonPublic)!;
                Delegate handler = Delegate.CreateDelegate(quitting.EventHandlerType, this, callback);
                quitting.AddEventHandler(null, handler);
                applicationQuittingEvent = quitting;
                applicationQuittingHandler = handler;
                runtime?.RuntimeMonitor.Log("DTMAPI shutdown subscribed to Unity Application.quitting.");
            }
            catch (Exception ex)
            {
                runtime?.RuntimeMonitor.Log("Unity Application.quitting subscription failed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private void OnApplicationQuitting()
        {
            Shutdown("Unity Application.quitting");
        }

        private void Shutdown(string reason)
        {
            if (shutdownStarted)
                return;
            shutdownStarted = true;

            initialized = false;
            runtime?.RuntimeMonitor.Log("DTMAPI bootstrap shutdown begin source=" + reason + ".");
            RunShutdownStage("Application.quitting subscription", () =>
            {
                if (applicationQuittingEvent != null && applicationQuittingHandler != null)
                    applicationQuittingEvent.RemoveEventHandler(null, applicationQuittingHandler);
            });
            applicationQuittingEvent = null;
            applicationQuittingHandler = null;
            RunShutdownStage("PlayerLoop driver", RemovePlayerLoopFrameDriver);
            RunShutdownStage("input transient state", ReflectedUnityInput.ClearTransientState);
            RunShutdownStage("title UI", () => titleSettingsUi?.Shutdown(reason));
            RunShutdownStage("GameBridge", () => bridge?.Shutdown(reason));
            RunShutdownStage("Core", () => runtime?.NotifyRuntimeShutdown(reason));
            runtime?.RuntimeMonitor.Log(reason + " observed by DTMAPI bootstrap.");
        }

        private void RunShutdownStage(string stage, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                try { runtime?.RuntimeMonitor.Log("Bootstrap shutdown stage failed stage=" + stage + ": " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn); }
                catch { }
            }
        }


    }
}
