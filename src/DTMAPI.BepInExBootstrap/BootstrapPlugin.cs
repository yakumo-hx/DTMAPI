using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using BepInEx;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
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
        private ReflectedDebugConsoleUi? debugConsoleUi;
        private BepInExRuntimeHost? host;
        private bool initialized;
        private bool frameSourceLogged;
        private bool updateCallbackSeen;
        private bool fallbackPumpLogged;
        private bool bridgeUpdateErrorLogged;
        private bool titleUiUpdateErrorLogged;
        private bool debugUiUpdateErrorLogged;
        private int fallbackTickQueued;
        private SynchronizationContext? unityContext;
        private Timer? fallbackPump;
        private DateTimeOffset lastFallbackTick = DateTimeOffset.MinValue;
        private DateTimeOffset lastUnityUpdateTick = DateTimeOffset.MinValue;
        private string diagnosticsHotkey = "None";

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
                debugConsoleUi = new ReflectedDebugConsoleUi(runtime);
                runtime.SaveSessionLoaded += (slot, isNewGame) => debugConsoleUi?.ResetForSaveBoundary(slot, isNewGame);
                runtime.ReturnedToTitleBoundary += () => debugConsoleUi?.ResetForTitleBoundary();
                runtime.RegisterRuntimeApi<IDebugConsoleApi>(new ManifestModel
                {
                    Name = "DTMAPI Debug Console Host",
                    Author = "DTMAPI",
                    Version = DtmApiRuntime.ApiVersion,
                    UniqueID = "DTMAPI.DebugConsoleHost",
                    Type = "RuntimeApi"
                }, debugConsoleUi);
                bridge = new DolocTownGameBridge(runtime, () => titleSettingsUi.ClickTitleButtonForSmoke(), debugConsoleUi);
                unityContext = SynchronizationContext.Current;
                diagnosticsHotkey = ResolveDiagnosticsHotkey();
                runtime.Start();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.RuntimeStart elapsedMs=" + startup.ElapsedMilliseconds + ".");
                runtime.RuntimeMonitor.Log(
                    IsDiagnosticsHotkeyEnabled()
                        ? "DTMAPI diagnostics hotkey is dev-enabled on " + diagnosticsHotkey + "."
                        : "DTMAPI diagnostics hotkey is disabled by default; use the title-page DTMAPI Settings entry.");
                bridge.Initialize();
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.HarmonyInitialize elapsedMs=" + startup.ElapsedMilliseconds + ".");
                initialized = true;
                TryStartCoroutineLoop();
                StartFallbackPump();
                Logger.LogInfo("DTMAPI Bootstrap loaded.");
                runtime.RuntimeMonitor.Log("Startup segment Bootstrap.Awake totalMs=" + startup.ElapsedMilliseconds + ".");
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

        private void StartFallbackPump()
        {
            try
            {
                fallbackPump = new Timer(_ => QueueFallbackTick(), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
                runtime?.RuntimeMonitor.Log("DTMAPI fallback tick pump armed.");
            }
            catch (Exception ex)
            {
                runtime?.Diagnostics.RecordError("DTMAPI.BepInExBootstrap", "Failed to start fallback tick pump.", ex.ToString());
            }
        }

        private void QueueFallbackTick()
        {
            if (!initialized || runtime == null)
                return;
            if (updateCallbackSeen && (DateTimeOffset.Now - lastUnityUpdateTick).TotalMilliseconds < 500)
                return;
            if (Interlocked.Exchange(ref fallbackTickQueued, 1) == 1)
                return;

            SynchronizationContext? context = unityContext;
            if (context != null)
            {
                context.Post(_ =>
                {
                    try
                    {
                        TickFromUnity("SynchronizationContext");
                    }
                    finally
                    {
                        Interlocked.Exchange(ref fallbackTickQueued, 0);
                    }
                }, null);
                return;
            }

            try
            {
                TickFromUnity("TimerFallback");
            }
            finally
            {
                Interlocked.Exchange(ref fallbackTickQueued, 0);
            }
        }

        private void TryStartCoroutineLoop()
        {
            try
            {
                MethodInfo? startCoroutine = GetType().BaseType?.GetMethod("StartCoroutine", new[] { typeof(IEnumerator) });
                if (startCoroutine == null)
                    startCoroutine = GetType().GetMethod("StartCoroutine", new[] { typeof(IEnumerator) });
                startCoroutine?.Invoke(this, new object[] { RuntimeCoroutine() });
                runtime?.RuntimeMonitor.Log("DTMAPI coroutine loop requested.");
            }
            catch (Exception ex)
            {
                runtime?.Diagnostics.RecordError("DTMAPI.BepInExBootstrap", "Failed to start coroutine loop.", ex.ToString());
            }
        }

        private IEnumerator RuntimeCoroutine()
        {
            while (true)
            {
                TickFromUnity("Coroutine");
                yield return null;
            }
        }

        public void Start()
        {
            runtime?.RuntimeMonitor.Log("Unity Start observed by DTMAPI bootstrap.");
        }

        public void Update()
        {
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
            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                if (source == "Update")
                {
                    updateCallbackSeen = true;
                    lastUnityUpdateTick = now;
                }
                else if (updateCallbackSeen && (now - lastUnityUpdateTick).TotalMilliseconds < 500)
                    return;
                else if ((now - lastFallbackTick).TotalMilliseconds < 50)
                    return;
                else
                    lastFallbackTick = now;

                if (!frameSourceLogged)
                {
                    frameSourceLogged = true;
                    runtime.RuntimeMonitor.Log("Unity frame callback observed by DTMAPI bootstrap: " + source);
                }
                if (source == "SynchronizationContext" && !fallbackPumpLogged)
                {
                    fallbackPumpLogged = true;
                    runtime.RuntimeMonitor.Log("DTMAPI fallback tick pump is dispatching through Unity synchronization context.");
                }

                bool allowUnityApi = source != "TimerFallback";
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
                        debugConsoleUi?.Update();
                    }
                    catch (Exception ex)
                    {
                        RecordUpdateComponentError("DTMAPI.DebugConsole", "Debug console UI update failed.", ex, ref debugUiUpdateErrorLogged);
                    }
                }

                bool uiCapturingKey = titleSettingsUi != null && titleSettingsUi.IsCapturingKey;
                if (allowUnityApi && !uiCapturingKey && IsDiagnosticsHotkeyEnabled() && ReflectedUnityInput.GetKeyDown(diagnosticsHotkey))
                {
                    bool wasOpen = runtime.UI.IsOpen;
                    runtime.UI.Toggle();
                    runtime.RuntimeMonitor.Log("DTMAPI diagnostics hotkey " + diagnosticsHotkey + " observed. open=" + wasOpen + " -> " + runtime.UI.IsOpen + " context=" + runtime.UI.InputContext + ".");
                }

                bool debugConsoleConsumedInput = debugConsoleUi != null && debugConsoleUi.ConsumedInputThisFrame;
                if (allowUnityApi && !uiCapturingKey && !debugConsoleConsumedInput)
                    PollRegisteredInputButtons();

                runtime.Update();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.BepInExBootstrap", "Update failed.", ex.ToString());
            }
        }

        private void RecordUpdateComponentError(string owner, string message, Exception ex, ref bool alreadyLogged)
        {
            if (runtime == null || alreadyLogged)
                return;
            alreadyLogged = true;
            runtime.Diagnostics.RecordError(owner, message, ex.ToString());
            runtime.RuntimeMonitor.Log(message + " " + ex.GetType().Name + ": " + ex.Message, DTMAPI.Abstractions.LogLevel.Warn);
        }

        private void PollRegisteredInputButtons()
        {
            if (runtime == null)
                return;

            foreach (string button in runtime.GetRegisteredInputButtons())
            {
                if (string.IsNullOrWhiteSpace(button))
                    continue;
                if (ReflectedUnityInput.GetKeyDown(button))
                    runtime.RecordInputPressed(button);
                else if (!ReflectedUnityInput.GetKey(button) && runtime.IsInputDown(button))
                    runtime.RecordInputReleased(button);
            }
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
            fallbackPump?.Dispose();
            fallbackPump = null;
            runtime?.RuntimeMonitor.Log("Unity OnApplicationQuit observed by DTMAPI bootstrap.");
        }
    }
}
