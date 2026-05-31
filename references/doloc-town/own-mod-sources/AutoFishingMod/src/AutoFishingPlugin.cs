using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
using DolocTown.SMAPI;
using DolocTown.SMAPI.Experimental;
#else
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
#endif
using HarmonyLib;
using UnityEngine;

#if SMAPI_AUTO_FISHING_BUILD
[assembly: AssemblyVersion("1.4.2.0")]
[assembly: AssemblyFileVersion("1.4.2.0")]
#elif SMAPI_FISHING_TEST_BUILD
[assembly: AssemblyVersion("0.2.1.0")]
[assembly: AssemblyFileVersion("0.2.1.0")]
#endif

namespace Dlk.DolocAutoFishing
{
#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
#if SMAPI_AUTO_FISHING_BUILD
    public sealed class AutoFishingMod : IDolocMod, IDolocModLifecycle
#else
    public sealed class FishingTestMod : IDolocMod, IDolocModLifecycle
#endif
    {
        private IModHelper helper;
        private Harmony harmony;
        private bool active;

        public void Entry(IModHelper helper)
        {
            this.helper = helper;
            AutoFishingPlugin.Log = new ManualLogSource(helper.Monitor);
            Paths.ConfigPath = helper.Config.GetConfigPath();
            AutoFishingController.Bind(ConfigFile.Create(helper.Config));
#if SMAPI_AUTO_FISHING_BUILD
            AutoFishingController.RegisterConfigMenu(helper);
#endif
            helper.Events.UpdateTicked += OnUpdateTicked;
            helper.Ui.GuiRendering += OnGuiRendering;
            StartRuntime("Entry");
            helper.Monitor.Info(AutoFishingPlugin.PluginName + " loaded as a native DolocTown SMAPI mod. Press " + AutoFishingController.ToggleKey.Value + " to toggle.");
        }

        public void OnEnabled()
        {
#if SMAPI_AUTO_FISHING_BUILD
            AutoFishingController.RegisterConfigMenu(helper);
#endif
            StartRuntime("Workshop enabled");
        }

        public void OnDisabled()
        {
#if SMAPI_AUTO_FISHING_BUILD
            AutoFishingController.UnregisterConfigMenu(helper);
#endif
            AutoFishingController.SetEnabled(false, "workshop disabled");
            AutoFishingController.UnbindSmapi();
            AutoFishingController.StopNativeHotkeyThread();
            AutoFishingController.RestoreFastAnimationRuntime();
            if (helper != null && harmony != null)
            {
                helper.Patching.UnpatchSelf(harmony);
                harmony = null;
            }

            active = false;
            if (helper != null)
            {
                helper.Monitor.Warn(AutoFishingPlugin.PluginName + " was disabled. Runtime events are stopped, but the DLL cannot be unloaded completely until the game restarts.");
            }
        }

        private void StartRuntime(string reason)
        {
            if (helper == null || active)
            {
                return;
            }

            harmony = helper.Patching.CreateHarmony(AutoFishingPlugin.PluginGuid);
            helper.Patching.PatchAll(harmony, typeof(AutoFishingPlugin).Assembly);
            LogPatchedMethods(reason);
            AutoFishingController.BindSmapi(helper);
            active = true;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (active)
            {
                AutoFishingController.UpdateFromGameLoop("SMAPI.UpdateTicked");
            }
        }

        private void OnGuiRendering(object sender, GuiRenderingEventArgs e)
        {
            if (active)
            {
                AutoFishingController.OnGui();
            }
        }

        private void LogPatchedMethods(string reason)
        {
            try
            {
                int count = 0;
                foreach (MethodBase method in harmony.GetPatchedMethods())
                {
                    count++;
                    if (count <= 24)
                    {
                        helper.Monitor.Info("Patched: " + method.DeclaringType.FullName + "." + method.Name);
                    }
                }

                helper.Monitor.Info("Harmony patched method count: " + count + " (" + reason + ").");
            }
            catch (Exception e)
            {
                helper.Monitor.Info("Harmony patch diagnostics failed: " + e.GetType().Name + " " + e.Message);
            }
        }
    }

    internal static class AutoFishingPlugin
    {
#if SMAPI_AUTO_FISHING_BUILD
        public const string PluginGuid = "Yuuka.AutoFishing";
        public const string PluginName = "DolocTownAutoFishing";
        public const string PluginVersion = "1.4.2";
#else
        public const string PluginGuid = "com.yuuka.dlk.fishingtest";
        public const string PluginName = "DLKFishingTestMod";
        public const string PluginVersion = "0.2.1";
#endif
        internal static ManualLogSource Log;
    }
#else
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AutoFishingPlugin : BaseUnityPlugin
    {
#if FISHING_ASSIST_BUILD
        public const string PluginGuid = "com.yuuka.dlk.fishingassist";
#else
        public const string PluginGuid = "com.dlk.doloctown.autofishing";
#endif
#if FISHING_ASSIST_BUILD
        public const string PluginName = "DLKFishingAssist";
        public const string PluginVersion = "0.1.0";
#elif FISHING_TEST_BUILD
        public const string PluginName = "DLKFishingTestMod";
        public const string PluginVersion = "0.1.1";
#else
        public const string PluginName = "DolocTownAutoFishing";
        public const string PluginVersion = "1.3.3";
#endif

        private Harmony harmony;
        private GameObject runnerObject;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            AutoFishingController.Bind(Config);
            AutoFishingController.StartNativeHotkeyThread();

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(AutoFishingPlugin).Assembly);
            LogPatchedMethods();

            runnerObject = new GameObject(PluginName + ".Runner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<AutoFishingRunner>();

            Logger.LogInfo(PluginName + " loaded. Press " + AutoFishingController.ToggleKey.Value + " to toggle.");
        }

        private void LogPatchedMethods()
        {
            try
            {
                int count = 0;
                foreach (MethodBase method in harmony.GetPatchedMethods())
                {
                    count++;
                    if (count <= 24)
                    {
                        Logger.LogInfo("Patched: " + method.DeclaringType.FullName + "." + method.Name);
                    }
                }

                Logger.LogInfo("Harmony patched method count: " + count);
            }
            catch (Exception e)
            {
                Logger.LogInfo("Harmony patch diagnostics failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        private void OnDestroy()
        {
            Logger.LogInfo(PluginName + " plugin component OnDestroy observed. Leaving Harmony patches and native hotkey thread alive for scene transitions.");
        }
    }
#endif

#if !(SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD)
    public sealed class AutoFishingRunner : MonoBehaviour
    {
        private void Awake()
        {
            AutoFishingController.LogRuntime("Runner awake.");
        }

        private void Update()
        {
            AutoFishingController.UpdateFromGameLoop("Runner.Update");
        }

        private void OnGUI()
        {
            AutoFishingController.OnGui();
        }

    }
#endif

#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
    internal sealed class ConfigFile
    {
        private readonly IConfigHelper helper;
        private readonly Dictionary<string, string> values;

        private ConfigFile(IConfigHelper helper, Dictionary<string, string> values)
        {
            this.helper = helper;
            this.values = values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static ConfigFile Create(IConfigHelper helper)
        {
            Dictionary<string, string> values = helper.ReadJsonFile<Dictionary<string, string>>("config.json");
            return new ConfigFile(helper, values);
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            string id = section + "." + key;
            if (!values.ContainsKey(id))
            {
                values[id] = ToStoredValue(defaultValue);
                Save();
            }

            return new ConfigEntry<T>(this, id, defaultValue);
        }

        public void Save()
        {
            helper.WriteJsonFile("config.json", values);
        }

        internal T Get<T>(string id, T defaultValue)
        {
            string raw;
            if (!values.TryGetValue(id, out raw))
            {
                return defaultValue;
            }

            try
            {
                Type type = typeof(T);
                if (type.IsEnum)
                {
                    return (T)Enum.Parse(type, raw, true);
                }

                if (type == typeof(bool))
                {
                    return (T)(object)bool.Parse(raw);
                }

                if (type == typeof(float))
                {
                    return (T)(object)float.Parse(raw, CultureInfo.InvariantCulture);
                }

                if (type == typeof(double))
                {
                    return (T)(object)double.Parse(raw, CultureInfo.InvariantCulture);
                }

                if (type == typeof(int))
                {
                    return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);
                }

                if (type == typeof(string))
                {
                    return (T)(object)raw;
                }
            }
            catch
            {
                values[id] = ToStoredValue(defaultValue);
                Save();
            }

            return defaultValue;
        }

        internal void Set<T>(string id, T value)
        {
            values[id] = ToStoredValue(value);
            Save();
        }

        private static string ToStoredValue<T>(T value)
        {
            if (value == null)
            {
                return "";
            }

            IFormattable formattable = value as IFormattable;
            return formattable != null ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        }
    }

    internal sealed class ConfigEntry<T>
    {
        private readonly ConfigFile config;
        private readonly string id;
        private readonly T defaultValue;

        public ConfigEntry(ConfigFile config, string id, T defaultValue)
        {
            this.config = config;
            this.id = id;
            this.defaultValue = defaultValue;
        }

        public T Value
        {
            get { return config.Get(id, defaultValue); }
            set { config.Set(id, value); }
        }
    }

    internal sealed class ManualLogSource
    {
        private readonly IMonitor monitor;

        public ManualLogSource(IMonitor monitor)
        {
            this.monitor = monitor;
        }

        public void LogInfo(object data)
        {
            monitor.Info(data == null ? "" : data.ToString());
        }

        public void LogWarning(object data)
        {
            monitor.Warn(data == null ? "" : data.ToString());
        }

        public void LogError(object data)
        {
            monitor.Error(data == null ? "" : data.ToString());
        }
    }

    internal static class Paths
    {
        public static string ConfigPath = "";
    }
#endif

    internal enum AutoFishingPhase
    {
        Idle,
        Starting,
        HoldingCast,
        Casting,
        Waiting,
        MiniGame,
        Pulling,
        Cooldown
    }

    internal static class AutoFishingController
    {
        internal static ConfigEntry<KeyCode> ToggleKey;

        private static ConfigEntry<bool> autoRecast;
        private static ConfigEntry<bool> stopOnManualMove;
        private static ConfigEntry<bool> requireSelectedFishingRod;
        private static ConfigEntry<bool> fastMode;
        private static ConfigEntry<bool> skipMiniGame;
        private static ConfigEntry<bool> instantBite;
        private static ConfigEntry<bool> fastAnimations;
        private static ConfigEntry<bool> verboseLogging;
        private static ConfigEntry<KeyCode> alternateToggleKey;
        private static ConfigEntry<KeyCode> emergencyToggleKey;
        private static ConfigEntry<KeyCode> mouseToggleKey;
        private static ConfigEntry<KeyCode> menuToggleKey;
        private static ConfigEntry<float> nativeStartHoldSeconds;
        private static ConfigEntry<float> autoEnableAfterSeconds;
        private static ConfigEntry<float> castReleaseProgress;
        private static ConfigEntry<float> recastDelaySeconds;
        private static ConfigEntry<float> miniGameReactionDelaySeconds;
        private static ConfigEntry<float> fastBiteIntervalMultiplier;
        private static ConfigEntry<float> fastAnimationMultiplier;
        private static ConfigEntry<float> startTimeoutSeconds;
        private static ConfigEntry<float> manualInputCheckIntervalSeconds;

        private static bool enabled;
        private static AutoFishingPhase phase = AutoFishingPhase.Idle;
        private static float phaseStartedAt;
        private static float nextRecastAt;
        private static float useToolPressUntil;
        private static int lastBonusNoteIndex = -1;
        private static int bonusTapFrame = -1;
        private static float lastStartRetryLogAt = -999f;
        private static bool runtimeUpdateLogged;
        private static bool runnerTickLogged;
        private static bool tickSourceLogged;
        private static bool nativeHotkeyUnavailableLogged;
        private static bool legacyInputUnavailableLogged;
        private static bool inputSystemUnavailableLogged;
        private static bool nativeToggleWasDown;
        private static bool alternateNativeToggleWasDown;
        private static bool emergencyNativeToggleWasDown;
        private static bool mouseNativeToggleWasDown;
        private static bool menuNativeToggleWasDown;
        private static bool autoEnableConsumed;
        private static bool tickRunning;
        private static int lastTickFrame = -1;
        private static Thread nativeHotkeyThread;
        private static volatile bool stopNativeHotkeyThread;
        private static readonly DateTime startedAtUtc = DateTime.UtcNow;
        private static float nextNativeHeartbeatAt;
        private static readonly object hotkeySync = new object();
        private static float lastHotkeyToggleAt = -999f;
        private static string lastHotkeyToggleSource = string.Empty;
        private static float lastMenuToggleAt = -999f;
        private static int pendingNativeAction;
        private static string pendingNativeReason = string.Empty;
        private static ConfigFile boundConfig;
#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
        private static IModHelper smapiHelper;
        private static bool smapiInputBound;
        private static bool smapiFishingEventLogged;
#endif
        private static bool menuOpen;
        private static Rect menuRect = new Rect(80f, 80f, 660f, 580f);
#if FISHING_TEST_BUILD
        private static int menuTab;
#endif
        private static int fishInfoMode;
        private static string selectedFishName = "鲤鱼";
        private static Vector2 fishListScroll;
        private static Vector2 fishDetailScroll;
        private static FishMenuContext cachedFishMenuContext;
        private static bool fishMenuContextDirty = true;
        private const int NativeActionNone = 0;
        private const int NativeActionToggle = 1;
        private const int NativeActionEnable = 2;
        private const int NativeActionDisable = 3;
        private const int NativeActionToggleMenu = 4;
        private const float HotkeyDebounceSeconds = 3f;
        private static readonly float[] FastAnimationMultiplierSteps = { 2f, 3f, 4f, 5f };
        private static readonly string[] FastAnimationMultiplierChoices = { "2x", "3x", "4x", "5x" };
#if FISHING_TEST_BUILD
        private static readonly string[] MenuTabs = { "\u81ea\u52a8\u9493\u9c7c\u8bbe\u7f6e" };
#endif
        private static readonly string[] FishInfoModes = { "本月鱼类", "全部图鉴", "详情" };
        private static readonly FishInfo[] FishInfoByPrice = BuildFishInfoByPrice();
        private static MethodInfo checkFishUnlockedMethod;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static float NowSeconds()
        {
            return (float)(DateTime.UtcNow - startedAtUtc).TotalSeconds;
        }

        private static FishInfo[] BuildFishInfoByPrice()
        {
            FishInfo[] sorted = new FishInfo[FishInfoDatabase.All.Length];
            Array.Copy(FishInfoDatabase.All, sorted, sorted.Length);
            Array.Sort(sorted, delegate(FishInfo left, FishInfo right)
            {
                int price = right.Price.CompareTo(left.Price);
                if (price != 0)
                {
                    return price;
                }

                return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            });

            return sorted;
        }

        private sealed class FishMenuContext
        {
            internal bool Available;
            internal int Month;
            internal int Day;
            internal int Hour;
            internal int Minute;
            internal int RodLevel;
            internal string WeatherKey = string.Empty;
            internal string WeatherText = string.Empty;
            internal string RodText = string.Empty;
            internal string StatusText = string.Empty;
            internal readonly Dictionary<string, bool> FishUnlockStatus = new Dictionary<string, bool>();
        }

        private static float NativeHoldSeconds()
        {
            return Math.Max(1f, nativeStartHoldSeconds.Value);
        }

        private static DolocTown.AgentStateFishingWait currentWaitState;
        private static DolocTown.FishingGameScrollBar currentMiniGame;
        private static DolocTown.FishingNoteData cachedCurrentNote;
        private static float cachedCurrentTime;
        private static int cachedMiniGameFrame = -1;
        private static bool cachedWaitForFishBite = true;
        private static float cachedFishOnHookDuration;
        private static int cachedWaitFrame = -1;
        private static int cachedHookFrame = -1;
        private static bool cachedHookResult;
        private static int cachedMiniGamePressFrame = -1;
        private static bool cachedMiniGamePressResult;
        private static readonly Dictionary<Type, MethodInfo> useAsToolMethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<string, MethodInfo> boolPropertyGetterCache = new Dictionary<string, MethodInfo>();
        private static readonly Dictionary<Animator, float> originalAnimatorSpeeds = new Dictionary<Animator, float>();
        private static readonly Dictionary<Rigidbody2D, float> originalHookGravityScales = new Dictionary<Rigidbody2D, float>();

        private static readonly FieldInfo readyCastTimerField = AccessTools.Field(typeof(DolocTown.AgentStateFishingReady), "_castTimer");
        private static readonly FieldInfo waitForFishBiteField = AccessTools.Field(typeof(DolocTown.AgentStateFishingWait), "_waitForFishBite");
        private static readonly FieldInfo fishOnHookDurationField = AccessTools.Field(typeof(DolocTown.AgentStateFishingWait), "_fishOnHookDuration");
        private static readonly FieldInfo waitRollTimerField = AccessTools.Field(typeof(DolocTown.AgentStateFishingWait), "_tuCounter");
        private static readonly FieldInfo waitHasRolledField = AccessTools.Field(typeof(DolocTown.AgentStateFishingWait), "_hasRolled");
        private static readonly FieldInfo waitHookProbabilityField = AccessTools.Field(typeof(DolocTown.AgentStateFishingWait), "_hookProbability");
        private static readonly FieldInfo currentNoteField = AccessTools.Field(typeof(DolocTown.FishingGameScrollBar), "currentNote");
        private static readonly FieldInfo currentTimeField = AccessTools.Field(typeof(DolocTown.FishingGameScrollBar), "currentTime");
        private static readonly FieldInfo currentGameStatusField = AccessTools.Field(typeof(DolocTown.FishingGameScrollBar), "currentGameStatus");
        private static readonly FieldInfo fishStaminaField = AccessTools.Field(typeof(DolocTown.FishingGameScrollBar), "fishStamina");
        private static readonly FieldInfo currentScoreField = AccessTools.Field(typeof(DolocTown.FishingGameScrollBar), "currentScore");
        private static readonly FieldInfo bodyAnimatorField = AccessTools.Field(typeof(DolocTown.BodyController), "animator");
        private static readonly FieldInfo bodyFishRodRendererField = AccessTools.Field(typeof(DolocTown.BodyController), "fishRodRenderer");
        private static readonly FieldInfo fishRodAnimatorField = AccessTools.Field(typeof(DolocTown.FishRodRenderer), "_animator");
        private static readonly FieldInfo fishRodHookField = AccessTools.Field(typeof(DolocTown.FishRodRenderer), "_hook");
        private static readonly FieldInfo pullDurationField = AccessTools.Field(typeof(DolocTown.AgentStateFishingPull), "_pullDuration");
        private static readonly FieldInfo agentBodyField = FindFieldByTypeName("AgentStateBase", "body");
        private static readonly MethodInfo rollFishMethod = AccessTools.Method(typeof(DolocTown.AgentStateFishing), "RollFish");
        private static readonly MethodInfo refreshProgressMethod = AccessTools.Method(typeof(DolocTown.FishingGameScrollBar), "RefreshProgress");

        internal static bool Enabled
        {
            get { return enabled; }
        }

        internal static void Bind(ConfigFile config)
        {
            boundConfig = config;

            ToggleKey = config.Bind("General", "ToggleKey", KeyCode.F8, "Toggles full auto fishing.");
#if FISHING_TEST_BUILD
            autoRecast = config.Bind("FishingTest", "AutoRecast", true, "Cast again after a fishing attempt finishes.");
            stopOnManualMove = config.Bind("FishingTest", "StopOnManualMove", true, "Stop automation if movement, jump, dash, or menu input is detected.");
#else
            autoRecast = config.Bind("General", "AutoRecast", true, "Cast again after a fishing attempt finishes.");
            stopOnManualMove = config.Bind("General", "StopOnManualMove", true, "Stop automation if movement, jump, dash, or menu input is detected.");
#endif
            requireSelectedFishingRod = config.Bind("General", "RequireSelectedFishingRod", true, "Only start automation when the selected quick-slot item is an ItemFishingRod.");
            verboseLogging = config.Bind("General", "VerboseLogging", false, "Write detailed state transitions to BepInEx logs.");
            alternateToggleKey = config.Bind("General", "AlternateToggleKey", KeyCode.None, "Second native-thread toggle key. Set None to disable.");
            emergencyToggleKey = config.Bind("General", "EmergencyToggleKey", KeyCode.None, "Third native-thread toggle key. Set None to disable.");
            mouseToggleKey = config.Bind("General", "MouseToggleKey", KeyCode.None, "Mouse toggle key for native-thread fallback. Set None to disable.");
#if FISHING_TEST_BUILD
            menuToggleKey = config.Bind("Menu", "MenuToggleKey", KeyCode.F9, "Toggles the fishing test status window.");
#else
            menuToggleKey = config.Bind("Menu", "MenuToggleKey", KeyCode.F9, "Toggles the in-game fish information window.");
#endif

            castReleaseProgress = config.Bind("Timing", "CastReleaseProgress", 0f, "Release the cast when the original cast meter reaches this progress. Use 0 to skip charging.");
            recastDelaySeconds = config.Bind("Timing", "RecastDelaySeconds", 0.25f, "Delay after pull/result before the next cast.");
            startTimeoutSeconds = config.Bind("Timing", "StartTimeoutSeconds", 8.0f, "Retry if the game does not enter fishing-ready state after starting.");
            miniGameReactionDelaySeconds = config.Bind("Timing", "MiniGameReactionDelaySeconds", 0.03f, "Delay after note start before the mod presses in the fishing minigame.");
            nativeStartHoldSeconds = config.Bind("Timing", "NativeStartHoldSeconds", 8f, "How long native/file-trigger enable keeps the cast input open while waiting for the game to poll input.");
            manualInputCheckIntervalSeconds = config.Bind("Timing", "ManualInputCheckIntervalSeconds", 0.25f, "How often to poll manual movement/menu input while auto fishing is active.");
            autoEnableAfterSeconds = config.Bind("Timing", "AutoEnableAfterSeconds", 0f, "Diagnostic fallback. If greater than 0, enable automatically this many seconds after plugin load.");

            fastMode = config.Bind("FastMode", "FastMode", false, "Optional mild speed-up. Original costs, fish pools, and minigame still apply.");
            fastBiteIntervalMultiplier = config.Bind("FastMode", "FastBiteIntervalMultiplier", 0.5f, "Multiplier for the original bite-roll interval when FastMode is enabled. Clamp range: 0.1 to 1.0.");

#if FISHING_TEST_BUILD
            skipMiniGame = config.Bind("FishingTest", "AutoMiniGame", true, "Automatically play the fishing minigame after a fish bites.");
            instantBite = config.Bind("Features", "InstantBite", false, "Make fish bite immediately after the hook lands. Energy cost and fish pool remain unchanged.");
            fastAnimations = config.Bind("FishingTest", "FastAnimations", true, "Speed up cast and pull animations.");
            fastAnimationMultiplier = config.Bind("FishingTest", "FastAnimationMultiplier", 3f, "Animation speed multiplier for FastAnimations. Available in-game steps: 2, 3, 4, 5. Default 3.0.");
#else
            skipMiniGame = config.Bind("Features", "SkipMiniGame", TestBuildDefault(true, false), "Skip the fishing minigame after a fish bites. Energy cost and fish pool remain unchanged.");
            instantBite = config.Bind("Features", "InstantBite", false, "Make fish bite immediately after the hook lands. Energy cost and fish pool remain unchanged.");
            fastAnimations = config.Bind("Features", "FastAnimations", TestBuildDefault(true, false), "Speed up cast and pull animations while SkipMiniGame or InstantBite is enabled.");
            fastAnimationMultiplier = config.Bind("Features", "FastAnimationMultiplier", 3f, "Animation speed multiplier for FastAnimations. Available in-game steps: 2, 3, 4, 5. Default 3.0.");
#endif
        }

#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
#if SMAPI_AUTO_FISHING_BUILD
        internal static void RegisterConfigMenu(IModHelper helper)
        {
            if (helper == null)
            {
                return;
            }

            try
            {
                if (helper.ConfigMenu.IsRegistered(helper.Manifest))
                {
                    helper.ConfigMenu.Unregister(helper.Manifest);
                }

                helper.ConfigMenu.Register(
                    helper.Manifest,
                    delegate { ResetConfigMenuDefaults(); },
                    delegate { SaveConfig(); });

                helper.ConfigMenu.AddSectionTitle(helper.Manifest, () => "自动钓鱼");
                helper.ConfigMenu.AddParagraph(
                    helper.Manifest,
                    () => "当前状态：" + (enabled ? "已开启" : "已关闭") + "。启停自动钓鱼请使用 F8；F9 只打开鱼类信息窗口。");

                helper.ConfigMenu.AddKeybindOption(
                    helper.Manifest,
                    () => "自动钓鱼开关",
                    () => ToggleKey.Value,
                    value => SetKeyConfig(ToggleKey, value),
                    () => "默认 F8。手持鱼竿并站在可钓鱼位置后按下。");

                helper.ConfigMenu.AddKeybindOption(
                    helper.Manifest,
                    () => "鱼类信息窗口",
                    () => menuToggleKey.Value,
                    value => SetKeyConfig(menuToggleKey, value),
                    () => "默认 F9。只显示鱼类信息，不再显示设置页。");

                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "自动再次抛竿",
                    () => AutoRecastEnabled(),
                    value => SetBoolConfig(autoRecast, value),
                    () => "一次钓鱼结束后自动开始下一轮。");

                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "移动/跳跃/冲刺/菜单/取消时关闭",
                    () => StopOnManualMoveEnabled(),
                    value => SetBoolConfig(stopOnManualMove, value),
                    () => "检测到手动输入后立即关闭自动钓鱼。");

                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "要求手持鱼竿",
                    () => requireSelectedFishingRod != null && requireSelectedFishingRod.Value,
                    value => SetBoolConfig(requireSelectedFishingRod, value),
                    () => "只在当前快捷栏选中鱼竿时启动自动钓鱼。");

                helper.ConfigMenu.AddSectionTitle(helper.Manifest, () => "加速功能");
                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "跳过小游戏",
                    () => SkipMiniGameEnabled(),
                    value => SetBoolConfig(skipMiniGame, value),
                    () => "咬钩后跳过钓鱼小游戏，保留原体力消耗和鱼池。");

                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "秒咬钩",
                    () => InstantBiteEnabled(),
                    value => SetBoolConfig(instantBite, value),
                    () => "抛竿落水后快速咬钩，保留原体力消耗和鱼池。");

                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "快速动画",
                    () => FastAnimationsEnabled(),
                    value => SetBoolConfig(fastAnimations, value),
                    () => "仅在跳过小游戏或秒咬钩开启时加速抛竿/收杆动画。");

                helper.ConfigMenu.AddChoiceOption(
                    helper.Manifest,
                    () => "动画倍率",
                    () => FastAnimationChoiceIndex(),
                    value => SetFastAnimationChoiceIndex(value),
                    () => FastAnimationMultiplierChoices,
                    () => "可选 2/3/4/5 倍，默认 3 倍。",
                    () => FastAnimationsEnabled());

                helper.ConfigMenu.AddSectionTitle(helper.Manifest, () => "高级");
                helper.ConfigMenu.AddBoolOption(
                    helper.Manifest,
                    () => "详细日志",
                    () => verboseLogging != null && verboseLogging.Value,
                    value => SetBoolConfig(verboseLogging, value),
                    () => "仅用于排查问题，会增加日志输出。");

                LogInfo("Registered Auto Fishing options in DolocTown SMAPI config menu.");
            }
            catch (Exception e)
            {
                LogInfo("Config menu registration failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        internal static void UnregisterConfigMenu(IModHelper helper)
        {
            if (helper == null)
            {
                return;
            }

            try
            {
                if (helper.ConfigMenu.IsRegistered(helper.Manifest))
                {
                    helper.ConfigMenu.Unregister(helper.Manifest);
                }
            }
            catch (Exception e)
            {
                LogDebug("Config menu unregister failed: " + e.Message);
            }
        }

        private static void ResetConfigMenuDefaults()
        {
            SetKeyConfig(ToggleKey, KeyCode.F8);
            SetKeyConfig(menuToggleKey, KeyCode.F9);
            SetBoolConfig(autoRecast, true);
            SetBoolConfig(stopOnManualMove, true);
            SetBoolConfig(requireSelectedFishingRod, true);
            SetBoolConfig(skipMiniGame, false);
            SetBoolConfig(instantBite, false);
            SetBoolConfig(fastAnimations, false);
            SetFastAnimationMultiplier(3f);
            SetBoolConfig(verboseLogging, false);
        }
#endif

        internal static void BindSmapi(IModHelper helper)
        {
            UnbindSmapi();

            smapiHelper = helper;
            RegisterSmapiButton(ToggleKey);
            RegisterSmapiButton(alternateToggleKey);
            RegisterSmapiButton(emergencyToggleKey);
            RegisterSmapiButton(mouseToggleKey);
            RegisterSmapiButton(menuToggleKey);
            helper.Input.ButtonPressed += OnSmapiButtonPressed;
            helper.Experimental.Fishing.PhaseChanged += OnSmapiFishingPhaseChanged;
            smapiInputBound = true;

            ReportSmapiCapabilities(helper);
            LogInfo("Hotkey backend: DolocTown SMAPI Input helper (" + ToggleKey.Value + ", menu " + menuToggleKey.Value + ").");
        }

        internal static void UnbindSmapi()
        {
            if (smapiHelper != null)
            {
                if (smapiInputBound)
                {
                    smapiHelper.Input.ButtonPressed -= OnSmapiButtonPressed;
                    UnregisterSmapiButton(ToggleKey);
                    UnregisterSmapiButton(alternateToggleKey);
                    UnregisterSmapiButton(emergencyToggleKey);
                    UnregisterSmapiButton(mouseToggleKey);
                    UnregisterSmapiButton(menuToggleKey);
                }

                smapiHelper.Experimental.Fishing.PhaseChanged -= OnSmapiFishingPhaseChanged;
            }

            smapiHelper = null;
            smapiInputBound = false;
            smapiFishingEventLogged = false;
        }

        private static void RegisterSmapiButton(ConfigEntry<KeyCode> entry)
        {
            if (smapiHelper != null && entry != null && entry.Value != KeyCode.None)
            {
                smapiHelper.Input.RegisterButton(entry.Value);
            }
        }

        private static void UnregisterSmapiButton(ConfigEntry<KeyCode> entry)
        {
            if (smapiHelper != null && entry != null && entry.Value != KeyCode.None)
            {
                smapiHelper.Input.UnregisterButton(entry.Value);
            }
        }

        private static void OnSmapiButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (MatchesKey(menuToggleKey, e.Button))
            {
                ToggleMenu("SMAPI input " + e.Button);
                return;
            }

            if (MatchesKey(ToggleKey, e.Button) ||
                MatchesKey(alternateToggleKey, e.Button) ||
                MatchesKey(emergencyToggleKey, e.Button) ||
                MatchesKey(mouseToggleKey, e.Button))
            {
                LogInfo("Hotkey backend: SMAPI input (" + e.Button + ")");
                ToggleFromHotkey("SMAPI input " + e.Button, false);
            }
        }

        private static bool MatchesKey(ConfigEntry<KeyCode> entry, KeyCode button)
        {
            return entry != null && entry.Value != KeyCode.None && entry.Value == button;
        }

        private static void OnSmapiFishingPhaseChanged(object sender, FishingPhaseChangedEventArgs e)
        {
            if (!smapiFishingEventLogged)
            {
                smapiFishingEventLogged = true;
                LogInfo("Fishing phase events are provided by DolocTown SMAPI helper. First observed phase: " + e.CurrentPhase + ".");
            }

            LogDebug("SMAPI fishing phase: " + e.PreviousPhase + " -> " + e.CurrentPhase + ".");
        }

        private static void ReportSmapiCapabilities(IModHelper helper)
        {
            helper.Diagnostics.ReportFeature("input.hotkeys", "Runtime input helper", DiagnosticFeatureStatus.Available, "F8/F9 and optional extra hotkeys are handled by DolocTown SMAPI 0.8.");
            helper.Diagnostics.ReportFeature("fishing.events", "Fishing phase events", DiagnosticFeatureStatus.Experimental, "Runtime publishes fishing phase events; this mod still owns gameplay Harmony patches.");
            helper.Diagnostics.ReportFeature("auto-recast", "Continuous fishing", DiagnosticFeatureStatus.Available, "The mod can start a new fishing cycle after the previous one completes.");
            helper.Diagnostics.ReportFeature("manual-stop", "Movement closes automation", DiagnosticFeatureStatus.Available, "Manual movement/menu input can stop automation before it keeps acting.");
            helper.Diagnostics.ReportFeature("auto-minigame", "Automatic minigame input", DiagnosticFeatureStatus.Experimental, "The mod presses fishing minigame inputs through its own Harmony hooks.");
            helper.Diagnostics.ReportFeature("fast-animations", "Fast cast/pull animations", DiagnosticFeatureStatus.Experimental, "Animation speed changes are runtime tweaks and may need a restart after disable.");
#if FISHING_TEST_BUILD
            helper.Diagnostics.ReportFeature("skip-minigame", "Skip minigame", DiagnosticFeatureStatus.Unavailable, "Fishing Test keeps the public boundary to automatic minigame play and does not skip results.");
            helper.Diagnostics.ReportFeature("instant-bite", "Instant bite", DiagnosticFeatureStatus.Unavailable, "Fishing Test does not expose instant bite.");
            helper.Diagnostics.ReportFeature("fish-info", "Fish information page", DiagnosticFeatureStatus.Unavailable, "Fishing Test keeps the public boundary to fishing automation only.");
#else
            helper.Diagnostics.ReportFeature("skip-minigame", "Skip minigame", DiagnosticFeatureStatus.Risky, "Optional high-impact patch owned by the mod; disable it if you want the original minigame.");
            helper.Diagnostics.ReportFeature("instant-bite", "Instant bite", DiagnosticFeatureStatus.Risky, "Optional high-impact patch owned by the mod; it changes bite timing.");
            helper.Diagnostics.ReportFeature("fish-info", "Fish information page", DiagnosticFeatureStatus.Available, "Reads save/runtime state and built-in fish table for the in-game information page.");
#endif
        }
#endif

        private static bool TestBuildDefault(bool testValue, bool fullValue)
        {
#if FISHING_TEST_BUILD
            return testValue;
#else
            return fullValue;
#endif
        }

        private static bool AutoRecastEnabled()
        {
            return autoRecast != null && autoRecast.Value;
        }

        private static bool StopOnManualMoveEnabled()
        {
            return stopOnManualMove != null && stopOnManualMove.Value;
        }

        private static bool FastModeEnabled()
        {
#if FISHING_TEST_BUILD
            return false;
#else
            return fastMode != null && fastMode.Value;
#endif
        }

        private static bool SkipMiniGameEnabled()
        {
#if FISHING_TEST_BUILD
            return false;
#else
            return skipMiniGame != null && skipMiniGame.Value;
#endif
        }

        private static bool InstantBiteEnabled()
        {
#if FISHING_TEST_BUILD
            return false;
#else
            return instantBite != null && instantBite.Value;
#endif
        }

        private static bool FastAnimationsEnabled()
        {
            return fastAnimations != null && fastAnimations.Value;
        }

        private static bool AutoMiniGameEnabled()
        {
#if FISHING_TEST_BUILD
            return skipMiniGame != null && skipMiniGame.Value;
#else
            return true;
#endif
        }

        internal static void OnGui()
        {
            if (!menuOpen)
            {
                return;
            }

#if FISHING_TEST_BUILD
            menuRect = GUILayout.Window(830811, menuRect, DrawMenuWindow, "\u9493\u9c7c\u6d4b\u8bd5");
#else
            menuRect = GUILayout.Window(830811, menuRect, DrawMenuWindow, "自动钓鱼 - 鱼类信息");
#endif
        }

        private static void DrawMenuWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("版本 " + AutoFishingPlugin.PluginVersion);
#if FISHING_TEST_BUILD
            menuTab = GUILayout.Toolbar(menuTab, MenuTabs);
            GUILayout.Space(4f);

            DrawSettingsPage();
#else
            DrawFishInfoPage();
#endif

            GUILayout.Space(8f);
            if (GUILayout.Button("关闭"))
            {
                menuOpen = false;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static void DrawSettingsPage()
        {
            GUILayout.Label("\u81ea\u52a8\u9493\u9c7c: " + (enabled ? "\u5df2\u5f00\u542f" : "\u5df2\u5173\u95ed") + " (F8)");
#if FISHING_TEST_BUILD
            GUILayout.Label("\u53ea\u663e\u793a\u672c\u6d4b\u8bd5\u7248\u652f\u6301\u7684\u529f\u80fd\uff0c\u5f00\u5173\u4f1a\u81ea\u52a8\u4fdd\u5b58\u3002");
            DrawConfigToggle("\u8fde\u7eed\u9493\u9c7c", autoRecast);
            DrawConfigToggle("\u81ea\u52a8\u5b8c\u6210\u5c0f\u6e38\u620f", skipMiniGame);
            DrawConfigToggle("\u52a0\u901f\u629b\u7aff/\u6536\u6746", fastAnimations);
            if (FastAnimationsEnabled())
            {
                DrawFastAnimationMultiplier();
            }
            DrawConfigToggle("\u79fb\u52a8/\u8df3\u8dc3/\u51b2\u523a/\u83dc\u5355/\u53d6\u6d88\u65f6\u5173\u95ed", stopOnManualMove);
#else
            DrawConfigToggle("跳过小游戏", skipMiniGame);
            DrawConfigToggle("秒咬钩", instantBite);
            DrawConfigToggle("快速动画", fastAnimations);
            DrawFastAnimationMultiplier();
            DrawConfigToggle("自动再次抛竿", autoRecast);
            DrawConfigToggle("移动/跳跃/冲刺/菜单/取消时关闭", stopOnManualMove);
#endif
        }

        private static void DrawConfigToggle(string label, ConfigEntry<bool> entry)
        {
            if (entry == null)
            {
                return;
            }

            bool value = GUILayout.Toggle(entry.Value, label);
            if (value != entry.Value)
            {
                entry.Value = value;
                SaveConfig();
                if (!FastAnimationsActive())
                {
                    RestoreFastAnimationRuntime();
                }
            }
        }

        private static void SetBoolConfig(ConfigEntry<bool> entry, bool value)
        {
            if (entry == null || entry.Value == value)
            {
                return;
            }

            entry.Value = value;
            SaveConfig();
            if (!FastAnimationsActive())
            {
                RestoreFastAnimationRuntime();
            }
        }

        private static void SetKeyConfig(ConfigEntry<KeyCode> entry, KeyCode value)
        {
            if (entry == null || entry.Value == value)
            {
                return;
            }

#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
            bool shouldRefreshSmapi = smapiHelper != null && smapiInputBound;
            if (shouldRefreshSmapi)
            {
                UnregisterSmapiButton(entry);
            }
#endif

            entry.Value = value;
            SaveConfig();

#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
            if (shouldRefreshSmapi)
            {
                RegisterSmapiButton(entry);
            }
#endif
        }

        private static void DrawFastAnimationMultiplier()
        {
            if (fastAnimationMultiplier == null)
            {
                return;
            }

            GUILayout.Label("动画倍率: " + FastAnimationMultiplier().ToString("0") + "x");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < FastAnimationMultiplierSteps.Length; i++)
            {
                float step = FastAnimationMultiplierSteps[i];
                string label = Mathf.Approximately(FastAnimationMultiplier(), step) ? "[" + step.ToString("0") + "x]" : step.ToString("0") + "x";
                if (GUILayout.Button(label))
                {
                    SetFastAnimationMultiplier(step);
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawFishInfoPage()
        {
            FishMenuContext context = GetCachedFishMenuContext();
            GUILayout.BeginHorizontal();
            GUILayout.Label(context.StatusText);
            if (GUILayout.Button("刷新", GUILayout.Width(90f)))
            {
                context = RefreshFishMenuContext();
                fishListScroll = Vector2.zero;
            }
            GUILayout.EndHorizontal();

            fishInfoMode = GUILayout.Toolbar(fishInfoMode, FishInfoModes);

            if (fishInfoMode == 2)
            {
                DrawSelectedFishDetails(425f);
            }
            else
            {
                DrawFishList(context, fishInfoMode == 0);
            }
        }

        private static FishMenuContext GetCachedFishMenuContext()
        {
            if (cachedFishMenuContext == null || fishMenuContextDirty)
            {
                return RefreshFishMenuContext();
            }

            return cachedFishMenuContext;
        }

        private static FishMenuContext RefreshFishMenuContext()
        {
            cachedFishMenuContext = GetFishMenuContext();
            fishMenuContextDirty = false;
            return cachedFishMenuContext;
        }

        private static void DrawFishList(FishMenuContext context, bool currentMonthOnly)
        {
            FishInfo[] source = currentMonthOnly ? FishInfoByPrice : FishInfoDatabase.All;
            int shown = 0;

            if (currentMonthOnly)
            {
                if (context.Available)
                {
                    GUILayout.Label(context.Month + " 月可钓鱼类，按售价排序。天气、时间和竹鱼竿以上要求会单独标注。");
                }
                else
                {
                    GUILayout.Label("当前月份不可用，暂时显示全部图鉴。");
                    source = FishInfoDatabase.All;
                    currentMonthOnly = false;
                }
            }

            fishListScroll = GUILayout.BeginScrollView(fishListScroll, GUILayout.Height(420f));
            for (int i = 0; i < source.Length; i++)
            {
                FishInfo fish = source[i];
                if (currentMonthOnly && !fish.MatchesMonth(context.Month))
                {
                    continue;
                }

                shown++;
                string prefix = string.Equals(selectedFishName, fish.Name, StringComparison.Ordinal) ? "> " : string.Empty;
                if (GUILayout.Button(prefix + BuildFishListLine(fish, !currentMonthOnly, context), GUILayout.ExpandWidth(true)))
                {
                    selectedFishName = fish.Name;
                }
            }

            if (shown == 0)
            {
                GUILayout.Label("当前月份没有匹配的鱼。");
            }

            GUILayout.EndScrollView();
        }

        private static void DrawSelectedFishDetails(float height)
        {
            FishInfo fish = FindSelectedFish();
            if (fish == null)
            {
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label(fish.Name + " | " + fish.PriceText + " | " + fish.Source);
            fishDetailScroll = GUILayout.BeginScrollView(fishDetailScroll, GUILayout.Height(height));
            GUILayout.Label("地点: " + fish.LocationText);
            GUILayout.Label("月份: " + fish.MonthsText);
            GUILayout.Label("天气: " + fish.WeatherText);
            GUILayout.Label("时间: " + fish.TimeText);
            GUILayout.Label("流程: " + fish.UnlockText);
            GUILayout.Label("鱼竿: " + fish.RodText);
            for (int i = 0; i + 1 < fish.Details.Length; i += 2)
            {
                GUILayout.Label(fish.Details[i] + ": " + fish.Details[i + 1]);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static string BuildFishListLine(FishInfo fish, bool includeMonths, FishMenuContext context)
        {
            string line = fish.Name + " | " + fish.LocationText + " | " + fish.PriceText;
            if (includeMonths)
            {
                line += " | " + fish.MonthsText;
            }

            if (fish.RequiredRodLevel >= 3)
            {
                line += " [竹竿+]";
            }

            if (fish.WeatherKeys.Length > 0)
            {
                line += " [天气: " + fish.WeatherText + "]";
            }

            if (fish.StartMinute >= 0 && fish.EndMinute >= 0)
            {
                line += " [时间: " + fish.TimeText + "]";
            }

            if (!FishInfoDatabase.IsDefaultUnlock(fish))
            {
                bool unlocked;
                if (context != null && context.FishUnlockStatus.TryGetValue(fish.Name, out unlocked) && !unlocked)
                {
                    line += " [未解锁: " + fish.UnlockText + "]";
                }
                else
                {
                    line += " [阶段: " + fish.UnlockText + "]";
                }
            }

            return line;
        }

        private static FishInfo FindSelectedFish()
        {
            for (int i = 0; i < FishInfoDatabase.All.Length; i++)
            {
                if (string.Equals(FishInfoDatabase.All[i].Name, selectedFishName, StringComparison.Ordinal))
                {
                    return FishInfoDatabase.All[i];
                }
            }

            return FishInfoDatabase.All.Length == 0 ? null : FishInfoDatabase.All[0];
        }

        private static FishMenuContext GetFishMenuContext()
        {
            FishMenuContext context = new FishMenuContext();
            context.RodLevel = GetSelectedFishingRodLevel(out context.RodText);

            try
            {
                object archive = GetDolocStaticProperty("archiveHandle", "get_archiveHandle");
                if (archive == null)
                {
                    context.StatusText = "当前状态不可用：未进入存档。";
                    return context;
                }

                object date = ReadProperty(archive, "DateNow");
                if (date == null)
                {
                    context.StatusText = "当前状态不可用：无法读取日期。";
                    return context;
                }

                int month = ReadIntMember(date, "MonthShown", 0);
                if (month <= 0)
                {
                    month = ReadIntMember(date, "Month", 0);
                }

                context.Month = month;
                context.Day = ReadIntMember(date, "DayShown", ReadIntMember(date, "Day", 0));
                context.Hour = ReadIntMember(date, "Hour", -1);
                context.Minute = ReadIntMember(date, "Minute", 0);

                object weather = ReadProperty(archive, "CurrentWeatherType");
                context.WeatherKey = weather == null ? string.Empty : weather.ToString();
                context.WeatherText = FormatWeather(context.WeatherKey);

                if (context.Month <= 0 || context.Hour < 0)
                {
                    context.StatusText = "当前状态不可用：日期或时间未就绪。";
                    return context;
                }

                PopulateFishUnlockStatus(context);
                context.Available = true;
                context.StatusText = string.Format(
                    "快照: {0}月{1}日 {2:00}:{3:00} | 天气: {4} | 鱼竿: {5}",
                    context.Month,
                    context.Day,
                    context.Hour,
                    context.Minute,
                    context.WeatherText,
                    context.RodText);
                return context;
            }
            catch (Exception e)
            {
                context.StatusText = "当前状态不可用：" + e.GetType().Name + "。";
                return context;
            }
        }

        private static void PopulateFishUnlockStatus(FishMenuContext context)
        {
            context.FishUnlockStatus.Clear();
            for (int i = 0; i < FishInfoDatabase.All.Length; i++)
            {
                FishInfo fish = FishInfoDatabase.All[i];
                if (FishInfoDatabase.IsDefaultUnlock(fish))
                {
                    continue;
                }

                bool unlocked;
                if (TryCheckFishUnlocked(fish, out unlocked))
                {
                    context.FishUnlockStatus[fish.Name] = unlocked;
                }
            }
        }

        private static bool TryCheckFishUnlocked(FishInfo fish, out bool unlocked)
        {
            unlocked = true;
            try
            {
                MethodInfo method = GetCheckFishUnlockedMethod();
                if (method == null)
                {
                    return false;
                }

                object value = method.Invoke(null, new object[] { fish.Name });
                if (value is bool)
                {
                    unlocked = (bool)value;
                    return true;
                }
            }
            catch (Exception e)
            {
                LogDebug("CheckFishUnlocked failed for " + fish.Name + ": " + e.Message);
            }

            return false;
        }

        private static MethodInfo GetCheckFishUnlockedMethod()
        {
            if (checkFishUnlockedMethod != null)
            {
                return checkFishUnlockedMethod;
            }

            Type dolocApi = AccessTools.TypeByName("DolocAPI");
            checkFishUnlockedMethod = FindMethod(dolocApi, "CheckFishUnlocked", new Type[] { typeof(string) });
            return checkFishUnlockedMethod;
        }

        private static int GetSelectedFishingRodLevel(out string rodText)
        {
            rodText = "未手持鱼竿";
            object selected = GetSelectedItem();
            if (!IsTypeOrBaseType(selected, "DolocTown.ItemFishingRod"))
            {
                return 0;
            }

            string displayText = GetSelectedItemDisplayText(selected);
            int inferredLevel = InferFishingRodLevel(displayText);
            int reflectedLevel = 0;

            try
            {
                FieldInfo functionField = FindFieldInHierarchy(selected.GetType(), "_function");
                object function = functionField == null ? null : functionField.GetValue(selected);
                if (function == null)
                {
                    object proto = ReadProperty(selected, "proto");
                    function = ReadProperty(proto, "Function");
                }

                reflectedLevel = ReadIntMember(function, "Level", 0);
            }
            catch (Exception e)
            {
                LogDebug("Fishing rod level read failed: " + e.Message);
            }

            int level = Math.Max(reflectedLevel, inferredLevel);
            if (level <= 0)
            {
                level = 1;
            }

            rodText = FormatFishingRodLevel(level);
            if (!string.IsNullOrEmpty(displayText))
            {
                rodText += " (" + displayText + ")";
            }

            return level;
        }

        private static string GetSelectedItemDisplayText(object selected)
        {
            string text = ReadStringMember(selected, "title", string.Empty);
            if (string.IsNullOrEmpty(text))
            {
                text = ReadStringMember(selected, "name", string.Empty);
            }

            object proto = ReadProperty(selected, "proto");
            if (string.IsNullOrEmpty(text))
            {
                text = ReadStringMember(proto, "Title", string.Empty);
            }

            if (string.IsNullOrEmpty(text))
            {
                text = ReadStringMember(proto, "Name", string.Empty);
            }

            return text;
        }

        private static int InferFishingRodLevel(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            string lower = text.ToLowerInvariant();
            if (text.IndexOf("碳纤维", StringComparison.Ordinal) >= 0 || lower.IndexOf("carbon", StringComparison.Ordinal) >= 0)
            {
                return 4;
            }

            if (text.IndexOf("竹", StringComparison.Ordinal) >= 0 || lower.IndexOf("bamboo", StringComparison.Ordinal) >= 0)
            {
                return 3;
            }

            if (text.IndexOf("老旧", StringComparison.Ordinal) >= 0 || lower.IndexOf("old", StringComparison.Ordinal) >= 0)
            {
                return 2;
            }

            if (text.IndexOf("简易", StringComparison.Ordinal) >= 0 || lower.IndexOf("simple", StringComparison.Ordinal) >= 0)
            {
                return 1;
            }

            return 0;
        }

        private static string FormatFishingRodLevel(int level)
        {
            if (level <= 1)
            {
                return "简易鱼竿+";
            }

            if (level == 2)
            {
                return "老旧鱼竿+";
            }

            if (level == 3)
            {
                return "竹鱼竿+";
            }

            return level + "级鱼竿";
        }

        private static string FormatWeather(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "未知";
            }

            if (key == "SUNNY")
            {
                return "晴天";
            }
            if (key == "CLOUDY")
            {
                return "多云";
            }
            if (key == "RAIN")
            {
                return "雨天";
            }
            if (key == "THUNDERSTORM")
            {
                return "雷雨";
            }
            if (key == "WINDY")
            {
                return "大风";
            }
            if (key == "ACID_RAIN")
            {
                return "酸雨";
            }
            if (key == "SCORCH_SUN")
            {
                return "烈日";
            }
            if (key == "NONE")
            {
                return "无";
            }

            return key;
        }

        private static void SetFastAnimationMultiplier(float value)
        {
            float snapped = SnapFastAnimationMultiplier(value);
            if (Mathf.Approximately(FastAnimationMultiplier(), snapped))
            {
                return;
            }

            fastAnimationMultiplier.Value = snapped;
            SaveConfig();
            RestoreFastAnimationRuntime();
        }

        private static int FastAnimationChoiceIndex()
        {
            float multiplier = FastAnimationMultiplier();
            int bestIndex = 1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < FastAnimationMultiplierSteps.Length; i++)
            {
                float distance = Mathf.Abs(FastAnimationMultiplierSteps[i] - multiplier);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static void SetFastAnimationChoiceIndex(int index)
        {
            if (index < 0)
            {
                index = 0;
            }
            if (index >= FastAnimationMultiplierSteps.Length)
            {
                index = FastAnimationMultiplierSteps.Length - 1;
            }

            SetFastAnimationMultiplier(FastAnimationMultiplierSteps[index]);
        }

        private static void SaveConfig()
        {
            try
            {
                if (boundConfig != null)
                {
                    boundConfig.Save();
                }
            }
            catch (Exception e)
            {
                LogDebug("Config save failed: " + e.Message);
            }
        }

        internal static void StartNativeHotkeyThread()
        {
            if (nativeHotkeyThread != null && nativeHotkeyThread.IsAlive)
            {
                return;
            }

            stopNativeHotkeyThread = false;
            nativeHotkeyThread = new Thread(NativeHotkeyLoop);
            nativeHotkeyThread.IsBackground = true;
            nativeHotkeyThread.Name = "DolocTownAutoFishing.NativeHotkey";
            nativeHotkeyThread.Start();
            LogInfo("Native hotkey thread started. Keys: " + ToggleKey.Value + ", " + alternateToggleKey.Value + ", " + emergencyToggleKey.Value + ", " + mouseToggleKey.Value + ".");
        }

        internal static void StopNativeHotkeyThread()
        {
            stopNativeHotkeyThread = true;
        }

        private static void NativeHotkeyLoop()
        {
            while (!stopNativeHotkeyThread)
            {
                try
                {
                    float now = NowSeconds();
                    if (autoEnableAfterSeconds.Value > 0f && !autoEnableConsumed && now >= autoEnableAfterSeconds.Value)
                    {
                        autoEnableConsumed = true;
                        LogInfo("AutoEnableAfterSeconds fired after " + autoEnableAfterSeconds.Value + " seconds.");
                        QueueNativeAction(NativeActionEnable, "auto-enable timer");
                    }

                    if (ConsumeTriggerFile("toggle"))
                    {
                        QueueNativeAction(NativeActionToggle, "trigger file toggle");
                    }
                    else if (ConsumeTriggerFile("enable"))
                    {
                        QueueNativeAction(NativeActionEnable, "trigger file enable");
                    }
                    else if (ConsumeTriggerFile("disable"))
                    {
                        QueueNativeAction(NativeActionDisable, "trigger file disable");
                    }

                    string keyName;
                    if (IsAnyNativeTogglePressed(out keyName))
                    {
                        LogInfo("Hotkey backend: native-thread (" + keyName + ")");
                        QueueNativeAction(NativeActionToggle, "native-thread " + keyName);
                    }
                    else if (menuToggleKey != null && CheckNativeKeyPressed(menuToggleKey.Value, ref menuNativeToggleWasDown))
                    {
                        QueueNativeAction(NativeActionToggleMenu, "native-thread " + menuToggleKey.Value);
                    }

                    if (verboseLogging != null && verboseLogging.Value && now >= nextNativeHeartbeatAt)
                    {
                        nextNativeHeartbeatAt = now + 5f;
                        LogInfo("Native hotkey heartbeat. Enabled=" + enabled + ", phase=" + phase + ", trigger files: " + TriggerFilePath("toggle") + " / enable / disable");
                    }
                }
                catch (Exception e)
                {
                    LogInfo("Native hotkey loop failed: " + e.GetType().Name + " " + e.Message);
                }

                Thread.Sleep(50);
            }
        }

        private static void QueueNativeAction(int action, string reason)
        {
            lock (hotkeySync)
            {
                pendingNativeAction = action;
                pendingNativeReason = reason;
            }
        }

        private static bool IsAnyNativeTogglePressed(out string keyName)
        {
            if (CheckNativeKeyPressed(ToggleKey.Value, ref nativeToggleWasDown))
            {
                keyName = ToggleKey.Value.ToString();
                return true;
            }

            if (CheckNativeKeyPressed(alternateToggleKey.Value, ref alternateNativeToggleWasDown))
            {
                keyName = alternateToggleKey.Value.ToString();
                return true;
            }

            if (CheckNativeKeyPressed(emergencyToggleKey.Value, ref emergencyNativeToggleWasDown))
            {
                keyName = emergencyToggleKey.Value.ToString();
                return true;
            }

            if (CheckNativeKeyPressed(mouseToggleKey.Value, ref mouseNativeToggleWasDown))
            {
                keyName = mouseToggleKey.Value.ToString();
                return true;
            }

            keyName = string.Empty;
            return false;
        }

        private static bool ConsumeTriggerFile(string action)
        {
            string path = TriggerFilePath(action);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                File.Delete(path);
            }
            catch
            {
            }

            LogInfo("Trigger file consumed: " + path);
            return true;
        }

        private static string TriggerFilePath(string action)
        {
            return Path.Combine(Paths.ConfigPath, "com.dlk.doloctown.autofishing." + action);
        }

        internal static void Update()
        {
            UpdateFromGameLoop(null);
        }

        internal static void UpdateFromGameLoop(string source)
        {
            if (tickRunning)
            {
                return;
            }

            if (!tickSourceLogged && !string.IsNullOrEmpty(source))
            {
                tickSourceLogged = true;
                LogInfo("Tick source: " + source);
            }

            int frame = Time.frameCount;
            if (lastTickFrame == frame)
            {
                return;
            }

            lastTickFrame = frame;
            tickRunning = true;
            try
            {
                UpdateCore();
            }
            catch (Exception e)
            {
                LogInfo("Auto fishing update failed: " + e);
            }
            finally
            {
                tickRunning = false;
            }
        }

        private static void UpdateCore()
        {
            if (!runtimeUpdateLogged)
            {
                runtimeUpdateLogged = true;
#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
                LogInfo("Runtime update active. Hotkeys use DolocTown SMAPI Input helper; gameplay patches remain owned by this mod.");
#else
                LogInfo("Runtime update active. Toggle checks use native queue, Unity Input System, and Unity legacy input fallback.");
#endif
            }

#if SMAPI_AUTO_FISHING_BUILD || SMAPI_FISHING_TEST_BUILD
            if (autoEnableAfterSeconds.Value > 0f && !autoEnableConsumed && NowSeconds() >= autoEnableAfterSeconds.Value)
            {
                autoEnableConsumed = true;
                LogInfo("AutoEnableAfterSeconds fired after " + autoEnableAfterSeconds.Value + " seconds.");
                SetEnabled(true, "auto-enable timer");
            }

            if (ConsumeTriggerFile("toggle"))
            {
                ToggleFromHotkey("trigger file toggle", false);
            }
            else if (ConsumeTriggerFile("enable"))
            {
                SetEnabled(true, "trigger file enable");
            }
            else if (ConsumeTriggerFile("disable"))
            {
                SetEnabled(false, "trigger file disable");
            }
#else
            ConsumeNativeAction();

            if (menuToggleKey != null && IsConfiguredKeyPressed(menuToggleKey.Value))
            {
                ToggleMenu("menu hotkey");
            }

            if (IsTogglePressed())
            {
                ToggleFromHotkey("toggle", false);
            }
#endif

            if (!enabled)
            {
                return;
            }

            if (ShouldPauseForManualInput())
            {
                DisableForManualInput();
                return;
            }

            float now = NowSeconds();
            if ((phase == AutoFishingPhase.Idle || phase == AutoFishingPhase.Cooldown) && AutoRecastEnabled() && now >= nextRecastAt)
            {
                StartCycle();
                return;
            }

            if (phase == AutoFishingPhase.Starting && now - phaseStartedAt > startTimeoutSeconds.Value)
            {
                ScheduleStartRetry("start timed out");
            }
        }

        private static bool ShouldPauseForManualInput()
        {
            if (!StopOnManualMoveEnabled())
            {
                return false;
            }

            return HasManualCancelInput();
        }

        private static void ConsumeNativeAction()
        {
            int action;
            string reason;
            lock (hotkeySync)
            {
                action = pendingNativeAction;
                reason = pendingNativeReason;
                pendingNativeAction = NativeActionNone;
                pendingNativeReason = string.Empty;
            }

            if (action == NativeActionNone)
            {
                return;
            }

            if (action == NativeActionToggle)
            {
                ToggleFromHotkey(reason, true);
            }
            else if (action == NativeActionEnable)
            {
                SetEnabledFromNativeThread(true, reason);
            }
            else if (action == NativeActionDisable)
            {
                SetEnabledFromNativeThread(false, reason);
            }
            else if (action == NativeActionToggleMenu)
            {
                ToggleMenu(reason);
            }
        }

        internal static void LogRuntime(string message)
        {
            if (!runnerTickLogged)
            {
                runnerTickLogged = true;
                LogInfo(message);
            }
        }

        private static void ToggleMenu(string reason)
        {
            float now = NowSeconds();
            if (now - lastMenuToggleAt < 0.25f)
            {
                return;
            }

            lastMenuToggleAt = now;
            menuOpen = !menuOpen;
            if (menuOpen && (menuRect.width < 620f || menuRect.height < 520f))
            {
                menuRect.width = 660f;
                menuRect.height = 580f;
            }

            if (menuOpen)
            {
                fishMenuContextDirty = true;
                fishListScroll = Vector2.zero;
            }

            LogDebug("Fish info menu " + (menuOpen ? "opened" : "closed") + " by " + reason + ".");
        }

        private static bool IsConfiguredKeyPressed(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return false;
            }

            if (IsInputSystemKeyPressed(key))
            {
                return true;
            }

            if (!legacyInputUnavailableLogged)
            {
                try
                {
                    return Input.GetKeyDown(key);
                }
                catch (Exception e)
                {
                    legacyInputUnavailableLogged = true;
                    LogInfo("Unity legacy input is unavailable for hotkeys: " + e.GetType().Name + " " + e.Message);
                }
            }

            return false;
        }

        private static bool IsTogglePressed()
        {
            if (IsInputSystemKeyPressed(ToggleKey.Value))
            {
                LogDebug("Hotkey backend: input-system");
                return true;
            }

            if (!legacyInputUnavailableLogged)
            {
                try
                {
                    if (Input.GetKeyDown(ToggleKey.Value))
                    {
                        LogDebug("Hotkey backend: legacy");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    legacyInputUnavailableLogged = true;
                    LogInfo("Unity legacy input is unavailable for hotkeys: " + e.GetType().Name + " " + e.Message);
                }
            }

            return false;
        }

        private static void ToggleFromHotkey(string source, bool nativeThread)
        {
            lock (hotkeySync)
            {
                float now = NowSeconds();
                if (now - lastHotkeyToggleAt < HotkeyDebounceSeconds)
                {
                    LogDebug("Ignored duplicate hotkey from " + source + "; previous was " + lastHotkeyToggleSource + ".");
                    return;
                }

                lastHotkeyToggleAt = now;
                lastHotkeyToggleSource = source;

                if (nativeThread)
                {
                    SetEnabledFromNativeThread(!enabled, source);
                }
                else
                {
                    SetEnabled(!enabled, source);
                }
            }
        }

        private static bool IsNativeKeyPressed(KeyCode keyCode)
        {
            return CheckNativeKeyPressed(keyCode, ref nativeToggleWasDown);
        }

        private static bool CheckNativeKeyPressed(KeyCode keyCode, ref bool wasDownState)
        {
            int virtualKey;
            if (!TryMapToVirtualKey(keyCode, out virtualKey))
            {
                wasDownState = false;
                return false;
            }

            try
            {
                short state = GetAsyncKeyState(virtualKey);
                bool isDown = (state & unchecked((short)0x8000)) != 0;
                bool edgeSinceLastCall = (state & 0x0001) != 0;
                bool wasPressed = edgeSinceLastCall || (isDown && !wasDownState);
                wasDownState = isDown;
                return wasPressed;
            }
            catch (Exception e)
            {
                if (!nativeHotkeyUnavailableLogged)
                {
                    nativeHotkeyUnavailableLogged = true;
                    LogInfo("Native hotkey backend is unavailable: " + e.GetType().Name + " " + e.Message);
                }

                wasDownState = false;
                return false;
            }
        }

        private static bool TryMapToVirtualKey(KeyCode keyCode, out int virtualKey)
        {
            switch (keyCode)
            {
                case KeyCode.F1: virtualKey = 0x70; return true;
                case KeyCode.F2: virtualKey = 0x71; return true;
                case KeyCode.F3: virtualKey = 0x72; return true;
                case KeyCode.F4: virtualKey = 0x73; return true;
                case KeyCode.F5: virtualKey = 0x74; return true;
                case KeyCode.F6: virtualKey = 0x75; return true;
                case KeyCode.F7: virtualKey = 0x76; return true;
                case KeyCode.F8: virtualKey = 0x77; return true;
                case KeyCode.F9: virtualKey = 0x78; return true;
                case KeyCode.F10: virtualKey = 0x79; return true;
                case KeyCode.F11: virtualKey = 0x7A; return true;
                case KeyCode.F12: virtualKey = 0x7B; return true;
                case KeyCode.A: virtualKey = 0x41; return true;
                case KeyCode.B: virtualKey = 0x42; return true;
                case KeyCode.C: virtualKey = 0x43; return true;
                case KeyCode.D: virtualKey = 0x44; return true;
                case KeyCode.E: virtualKey = 0x45; return true;
                case KeyCode.F: virtualKey = 0x46; return true;
                case KeyCode.G: virtualKey = 0x47; return true;
                case KeyCode.H: virtualKey = 0x48; return true;
                case KeyCode.I: virtualKey = 0x49; return true;
                case KeyCode.J: virtualKey = 0x4A; return true;
                case KeyCode.K: virtualKey = 0x4B; return true;
                case KeyCode.L: virtualKey = 0x4C; return true;
                case KeyCode.M: virtualKey = 0x4D; return true;
                case KeyCode.N: virtualKey = 0x4E; return true;
                case KeyCode.O: virtualKey = 0x4F; return true;
                case KeyCode.P: virtualKey = 0x50; return true;
                case KeyCode.Q: virtualKey = 0x51; return true;
                case KeyCode.R: virtualKey = 0x52; return true;
                case KeyCode.S: virtualKey = 0x53; return true;
                case KeyCode.T: virtualKey = 0x54; return true;
                case KeyCode.U: virtualKey = 0x55; return true;
                case KeyCode.V: virtualKey = 0x56; return true;
                case KeyCode.W: virtualKey = 0x57; return true;
                case KeyCode.X: virtualKey = 0x58; return true;
                case KeyCode.Y: virtualKey = 0x59; return true;
                case KeyCode.Z: virtualKey = 0x5A; return true;
                case KeyCode.Alpha0: virtualKey = 0x30; return true;
                case KeyCode.Alpha1: virtualKey = 0x31; return true;
                case KeyCode.Alpha2: virtualKey = 0x32; return true;
                case KeyCode.Alpha3: virtualKey = 0x33; return true;
                case KeyCode.Alpha4: virtualKey = 0x34; return true;
                case KeyCode.Alpha5: virtualKey = 0x35; return true;
                case KeyCode.Alpha6: virtualKey = 0x36; return true;
                case KeyCode.Alpha7: virtualKey = 0x37; return true;
                case KeyCode.Alpha8: virtualKey = 0x38; return true;
                case KeyCode.Alpha9: virtualKey = 0x39; return true;
                case KeyCode.Space: virtualKey = 0x20; return true;
                case KeyCode.Return: virtualKey = 0x0D; return true;
                case KeyCode.KeypadEnter: virtualKey = 0x0D; return true;
                case KeyCode.Escape: virtualKey = 0x1B; return true;
                case KeyCode.Tab: virtualKey = 0x09; return true;
                case KeyCode.Backspace: virtualKey = 0x08; return true;
                case KeyCode.UpArrow: virtualKey = 0x26; return true;
                case KeyCode.DownArrow: virtualKey = 0x28; return true;
                case KeyCode.LeftArrow: virtualKey = 0x25; return true;
                case KeyCode.RightArrow: virtualKey = 0x27; return true;
                case KeyCode.Insert: virtualKey = 0x2D; return true;
                case KeyCode.Delete: virtualKey = 0x2E; return true;
                case KeyCode.Home: virtualKey = 0x24; return true;
                case KeyCode.End: virtualKey = 0x23; return true;
                case KeyCode.PageUp: virtualKey = 0x21; return true;
                case KeyCode.PageDown: virtualKey = 0x22; return true;
                case KeyCode.Mouse0: virtualKey = 0x01; return true;
                case KeyCode.Mouse1: virtualKey = 0x02; return true;
                case KeyCode.Mouse2: virtualKey = 0x04; return true;
                case KeyCode.Mouse3: virtualKey = 0x05; return true;
                case KeyCode.Mouse4: virtualKey = 0x06; return true;
                default:
                    virtualKey = 0;
                    return false;
            }
        }

        private static bool IsInputSystemKeyPressed(KeyCode keyCode)
        {
            if (inputSystemUnavailableLogged)
            {
                return false;
            }

            try
            {
                UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard == null)
                {
                    return false;
                }

                UnityEngine.InputSystem.Key key;
                if (!TryMapToInputSystemKey(keyCode, out key))
                {
                    return false;
                }

                var control = keyboard[key];
                return control != null && control.wasPressedThisFrame;
            }
            catch (Exception e)
            {
                if (!inputSystemUnavailableLogged)
                {
                    inputSystemUnavailableLogged = true;
                    LogInfo("Unity Input System is unavailable for hotkeys: " + e.GetType().Name + " " + e.Message);
                }

                return false;
            }
        }

        private static bool TryMapToInputSystemKey(KeyCode keyCode, out UnityEngine.InputSystem.Key key)
        {
            switch (keyCode)
            {
                case KeyCode.F1: key = UnityEngine.InputSystem.Key.F1; return true;
                case KeyCode.F2: key = UnityEngine.InputSystem.Key.F2; return true;
                case KeyCode.F3: key = UnityEngine.InputSystem.Key.F3; return true;
                case KeyCode.F4: key = UnityEngine.InputSystem.Key.F4; return true;
                case KeyCode.F5: key = UnityEngine.InputSystem.Key.F5; return true;
                case KeyCode.F6: key = UnityEngine.InputSystem.Key.F6; return true;
                case KeyCode.F7: key = UnityEngine.InputSystem.Key.F7; return true;
                case KeyCode.F8: key = UnityEngine.InputSystem.Key.F8; return true;
                case KeyCode.F9: key = UnityEngine.InputSystem.Key.F9; return true;
                case KeyCode.F10: key = UnityEngine.InputSystem.Key.F10; return true;
                case KeyCode.F11: key = UnityEngine.InputSystem.Key.F11; return true;
                case KeyCode.F12: key = UnityEngine.InputSystem.Key.F12; return true;
                case KeyCode.A: key = UnityEngine.InputSystem.Key.A; return true;
                case KeyCode.B: key = UnityEngine.InputSystem.Key.B; return true;
                case KeyCode.C: key = UnityEngine.InputSystem.Key.C; return true;
                case KeyCode.D: key = UnityEngine.InputSystem.Key.D; return true;
                case KeyCode.E: key = UnityEngine.InputSystem.Key.E; return true;
                case KeyCode.F: key = UnityEngine.InputSystem.Key.F; return true;
                case KeyCode.G: key = UnityEngine.InputSystem.Key.G; return true;
                case KeyCode.H: key = UnityEngine.InputSystem.Key.H; return true;
                case KeyCode.I: key = UnityEngine.InputSystem.Key.I; return true;
                case KeyCode.J: key = UnityEngine.InputSystem.Key.J; return true;
                case KeyCode.K: key = UnityEngine.InputSystem.Key.K; return true;
                case KeyCode.L: key = UnityEngine.InputSystem.Key.L; return true;
                case KeyCode.M: key = UnityEngine.InputSystem.Key.M; return true;
                case KeyCode.N: key = UnityEngine.InputSystem.Key.N; return true;
                case KeyCode.O: key = UnityEngine.InputSystem.Key.O; return true;
                case KeyCode.P: key = UnityEngine.InputSystem.Key.P; return true;
                case KeyCode.Q: key = UnityEngine.InputSystem.Key.Q; return true;
                case KeyCode.R: key = UnityEngine.InputSystem.Key.R; return true;
                case KeyCode.S: key = UnityEngine.InputSystem.Key.S; return true;
                case KeyCode.T: key = UnityEngine.InputSystem.Key.T; return true;
                case KeyCode.U: key = UnityEngine.InputSystem.Key.U; return true;
                case KeyCode.V: key = UnityEngine.InputSystem.Key.V; return true;
                case KeyCode.W: key = UnityEngine.InputSystem.Key.W; return true;
                case KeyCode.X: key = UnityEngine.InputSystem.Key.X; return true;
                case KeyCode.Y: key = UnityEngine.InputSystem.Key.Y; return true;
                case KeyCode.Z: key = UnityEngine.InputSystem.Key.Z; return true;
                case KeyCode.Alpha0: key = UnityEngine.InputSystem.Key.Digit0; return true;
                case KeyCode.Alpha1: key = UnityEngine.InputSystem.Key.Digit1; return true;
                case KeyCode.Alpha2: key = UnityEngine.InputSystem.Key.Digit2; return true;
                case KeyCode.Alpha3: key = UnityEngine.InputSystem.Key.Digit3; return true;
                case KeyCode.Alpha4: key = UnityEngine.InputSystem.Key.Digit4; return true;
                case KeyCode.Alpha5: key = UnityEngine.InputSystem.Key.Digit5; return true;
                case KeyCode.Alpha6: key = UnityEngine.InputSystem.Key.Digit6; return true;
                case KeyCode.Alpha7: key = UnityEngine.InputSystem.Key.Digit7; return true;
                case KeyCode.Alpha8: key = UnityEngine.InputSystem.Key.Digit8; return true;
                case KeyCode.Alpha9: key = UnityEngine.InputSystem.Key.Digit9; return true;
                case KeyCode.Space: key = UnityEngine.InputSystem.Key.Space; return true;
                case KeyCode.Return: key = UnityEngine.InputSystem.Key.Enter; return true;
                case KeyCode.KeypadEnter: key = UnityEngine.InputSystem.Key.NumpadEnter; return true;
                case KeyCode.Escape: key = UnityEngine.InputSystem.Key.Escape; return true;
                case KeyCode.Tab: key = UnityEngine.InputSystem.Key.Tab; return true;
                case KeyCode.Backspace: key = UnityEngine.InputSystem.Key.Backspace; return true;
                case KeyCode.UpArrow: key = UnityEngine.InputSystem.Key.UpArrow; return true;
                case KeyCode.DownArrow: key = UnityEngine.InputSystem.Key.DownArrow; return true;
                case KeyCode.LeftArrow: key = UnityEngine.InputSystem.Key.LeftArrow; return true;
                case KeyCode.RightArrow: key = UnityEngine.InputSystem.Key.RightArrow; return true;
                case KeyCode.Insert: key = UnityEngine.InputSystem.Key.Insert; return true;
                case KeyCode.Delete: key = UnityEngine.InputSystem.Key.Delete; return true;
                case KeyCode.Home: key = UnityEngine.InputSystem.Key.Home; return true;
                case KeyCode.End: key = UnityEngine.InputSystem.Key.End; return true;
                case KeyCode.PageUp: key = UnityEngine.InputSystem.Key.PageUp; return true;
                case KeyCode.PageDown: key = UnityEngine.InputSystem.Key.PageDown; return true;
                default:
                    key = UnityEngine.InputSystem.Key.None;
                    return false;
            }
        }

        internal static void SetEnabled(bool value, string reason)
        {
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            if (enabled)
            {
                phase = AutoFishingPhase.Idle;
                phaseStartedAt = NowSeconds();
                nextRecastAt = NowSeconds() + 0.15f;
                useToolPressUntil = 0f;
                lastBonusNoteIndex = -1;
                lastStartRetryLogAt = -999f;
                currentWaitState = null;
                currentMiniGame = null;
                ResetMiniGameCache();
                cachedWaitFrame = -1;
                ResetInputCaches();
                LogInfo("Auto fishing enabled.");
                ShowSmallMessage("Auto fishing enabled");
            }
            else
            {
                ResetRuntimeState();
                LogInfo("Auto fishing disabled: " + reason + ".");
                ShowSmallMessage("Auto fishing disabled");
            }
        }

        private static void SetEnabledFromNativeThread(bool value, string reason)
        {
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            if (enabled)
            {
                phase = AutoFishingPhase.Idle;
                phaseStartedAt = NowSeconds();
                nextRecastAt = NowSeconds() + 0.15f;
                useToolPressUntil = NowSeconds() + NativeHoldSeconds();
                lastBonusNoteIndex = -1;
                lastStartRetryLogAt = -999f;
                currentWaitState = null;
                currentMiniGame = null;
                ResetMiniGameCache();
                cachedWaitFrame = -1;
                ResetInputCaches();
                LogInfo("Auto fishing enabled by " + reason + ". Will retry direct rod use until fishing starts.");
            }
            else
            {
                ResetRuntimeState();
                LogInfo("Auto fishing disabled by " + reason + ".");
            }
        }

        internal static bool ShouldUseToolPressed()
        {
            return enabled && phase == AutoFishingPhase.Starting && NowSeconds() <= useToolPressUntil;
        }

        internal static bool ShouldUseToolHeld()
        {
            return enabled && ((phase == AutoFishingPhase.Starting && NowSeconds() <= useToolPressUntil) || phase == AutoFishingPhase.HoldingCast);
        }

        internal static bool ShouldHookFish()
        {
            int frame = Time.frameCount;
            if (cachedHookFrame == frame)
            {
                return cachedHookResult;
            }

            cachedHookFrame = frame;
            cachedHookResult = false;

            if (!enabled || phase != AutoFishingPhase.Waiting || currentWaitState == null)
            {
                return false;
            }

            RefreshWaitCache();
            cachedHookResult = !cachedWaitForFishBite && cachedFishOnHookDuration > 0f;
            return cachedHookResult;
        }

        internal static bool ShouldMiniGamePress()
        {
            int frame = Time.frameCount;
            if (cachedMiniGamePressFrame == frame)
            {
                return cachedMiniGamePressResult;
            }

            cachedMiniGamePressFrame = frame;
            cachedMiniGamePressResult = false;

            if (!enabled || phase != AutoFishingPhase.MiniGame || currentMiniGame == null)
            {
                return false;
            }

            if (!AutoMiniGameEnabled())
            {
                return false;
            }

            cachedMiniGamePressResult = ShouldTapBonusNote() || ShouldStableNoteHeld();
            return cachedMiniGamePressResult;
        }

        internal static bool ShouldFishingPressed()
        {
            if (ShouldHookFish())
            {
                return true;
            }

            return enabled && phase == AutoFishingPhase.MiniGame && currentMiniGame != null && AutoMiniGameEnabled() && ShouldTapBonusNote();
        }

        private static bool ShouldStableNoteHeld()
        {
            DolocTown.FishingNoteData note = GetCurrentNote();
            if (note == null || note.noteType != DolocTown.FishingNoteType.Stable)
            {
                return false;
            }

            float now = GetCurrentMiniGameTime();
            float reactionDelay = Mathf.Max(0f, miniGameReactionDelaySeconds.Value);
            return now >= note.startTime + reactionDelay && now <= note.endTime;
        }

        private static bool FastAnimationsActive()
        {
#if FISHING_TEST_BUILD
            return enabled && FastAnimationsEnabled();
#else
            return enabled &&
                   FastAnimationsEnabled() &&
                   (SkipMiniGameEnabled() || InstantBiteEnabled());
#endif
        }

        private static float FastAnimationMultiplier()
        {
            return SnapFastAnimationMultiplier(fastAnimationMultiplier == null ? 3f : fastAnimationMultiplier.Value);
        }

        private static float SnapFastAnimationMultiplier(float value)
        {
            return Mathf.Clamp(Mathf.Round(value), 2f, 5f);
        }

        private static void ApplyFastAnimationToState(object state)
        {
            if (!FastAnimationsActive())
            {
                RestoreFastAnimationRuntime();
                return;
            }

            object body = GetAgentBody(state);
            ApplyAnimatorSpeed(GetBodyAnimator(body), FastAnimationMultiplier());
            ApplyAnimatorSpeed(GetFishRodAnimator(GetFishRodRenderer(body)), FastAnimationMultiplier());
        }

        internal static void OnCastHook(DolocTown.FishRodRenderer renderer)
        {
            if (!FastAnimationsActive() || renderer == null)
            {
                return;
            }

            try
            {
                float multiplier = FastAnimationMultiplier();
                DolocTown.FishRodHook hook = GetFishRodHook(renderer);
                if (hook == null)
                {
                    return;
                }

                hook.Velocity = hook.Velocity * multiplier;
                Rigidbody2D rb = hook.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    if (!originalHookGravityScales.ContainsKey(rb))
                    {
                        originalHookGravityScales[rb] = rb.gravityScale;
                    }

                    rb.gravityScale = originalHookGravityScales[rb] * multiplier * multiplier;
                }
            }
            catch (Exception e)
            {
                LogDebug("Fast cast hook failed: " + e.Message);
            }
        }

        private static void ApplyFastPullDuration(DolocTown.AgentStateFishingPull instance)
        {
            if (!FastAnimationsActive() || instance == null || pullDurationField == null)
            {
                return;
            }

            try
            {
                float duration = ReadFloat(pullDurationField, instance, 0f);
                if (duration > 0f)
                {
                    pullDurationField.SetValue(instance, duration / FastAnimationMultiplier());
                }
            }
            catch (Exception e)
            {
                LogDebug("Fast pull duration failed: " + e.Message);
            }
        }

        internal static void RestoreFastAnimationRuntime()
        {
            foreach (KeyValuePair<Animator, float> entry in originalAnimatorSpeeds)
            {
                try
                {
                    if (entry.Key != null)
                    {
                        entry.Key.speed = entry.Value;
                    }
                }
                catch
                {
                }
            }

            originalAnimatorSpeeds.Clear();

            foreach (KeyValuePair<Rigidbody2D, float> entry in originalHookGravityScales)
            {
                try
                {
                    if (entry.Key != null)
                    {
                        entry.Key.gravityScale = entry.Value;
                    }
                }
                catch
                {
                }
            }

            originalHookGravityScales.Clear();
        }

        private static void ApplyAnimatorSpeed(Animator animator, float speed)
        {
            if (animator == null)
            {
                return;
            }

            if (!originalAnimatorSpeeds.ContainsKey(animator))
            {
                originalAnimatorSpeeds[animator] = animator.speed;
            }

            animator.speed = speed;
        }

        private static object GetAgentBody(object state)
        {
            if (state == null || agentBodyField == null)
            {
                return null;
            }

            try
            {
                return agentBodyField.GetValue(state);
            }
            catch
            {
                return null;
            }
        }

        private static Animator GetBodyAnimator(object body)
        {
            return GetFieldValue(bodyAnimatorField, body) as Animator;
        }

        private static DolocTown.FishRodRenderer GetFishRodRenderer(object body)
        {
            return GetFieldValue(bodyFishRodRendererField, body) as DolocTown.FishRodRenderer;
        }

        private static Animator GetFishRodAnimator(DolocTown.FishRodRenderer renderer)
        {
            return GetFieldValue(fishRodAnimatorField, renderer) as Animator;
        }

        private static DolocTown.FishRodHook GetFishRodHook(DolocTown.FishRodRenderer renderer)
        {
            return GetFieldValue(fishRodHookField, renderer) as DolocTown.FishRodHook;
        }

        internal static void OnReadyEnter(DolocTown.AgentStateFishingReady instance)
        {
            if (!enabled)
            {
                return;
            }

            ApplyFastAnimationToState(instance);

            if (Mathf.Clamp01(castReleaseProgress.Value) <= 0.001f)
            {
                SetPhase(AutoFishingPhase.Casting);
                return;
            }

            SetPhase(AutoFishingPhase.HoldingCast);
        }

        internal static void OnReadyPlay(DolocTown.AgentStateFishingReady instance)
        {
            if (!enabled || phase != AutoFishingPhase.HoldingCast)
            {
                return;
            }

            float progress = ReadCastProgress(instance);
            float target = Mathf.Clamp01(castReleaseProgress.Value);
            if (progress >= target || NowSeconds() - phaseStartedAt > 4f)
            {
                SetPhase(AutoFishingPhase.Casting);
            }
        }

        internal static void OnCastEnter(DolocTown.AgentStateFishingCast instance)
        {
            if (enabled)
            {
                ApplyFastAnimationToState(instance);
                SetPhase(AutoFishingPhase.Casting);
            }
        }

        internal static void OnWaitEnter(DolocTown.AgentStateFishingWait instance)
        {
            if (!enabled)
            {
                return;
            }

            currentWaitState = instance;
            currentMiniGame = null;
            ResetMiniGameCache();
            cachedWaitFrame = -1;
            ResetInputCaches();
            SetPhase(AutoFishingPhase.Waiting);
            ApplyFastWaitInterval(instance);
            ApplyInstantBite(instance);
        }

        internal static void OnMiniGameStart(DolocTown.FishingGameScrollBar instance)
        {
            if (!enabled)
            {
                return;
            }

            currentWaitState = null;
            currentMiniGame = instance;
            lastBonusNoteIndex = -1;
            bonusTapFrame = -1;
            cachedMiniGameFrame = -1;
            ResetInputCaches();
            SetPhase(AutoFishingPhase.MiniGame);
#if !FISHING_TEST_BUILD
            CompleteMiniGameIfConfigured(instance);
#endif
        }

        internal static void OnMiniGameStop(DolocTown.FishingGameScrollBar instance)
        {
            if (currentMiniGame == instance)
            {
                currentMiniGame = null;
                ResetMiniGameCache();
                ResetInputCaches();
            }
        }

        internal static void OnPullEnter(DolocTown.AgentStateFishingPull instance)
        {
            if (!enabled)
            {
                return;
            }

            ApplyFastAnimationToState(instance);
            ApplyFastPullDuration(instance);
            currentWaitState = null;
            currentMiniGame = null;
            ResetMiniGameCache();
            cachedWaitFrame = -1;
            ResetInputCaches();
            SetPhase(AutoFishingPhase.Pulling);
        }

        internal static void OnPullExit()
        {
            RestoreFastAnimationRuntime();
            if (!enabled)
            {
                return;
            }

            ScheduleRecast();
        }

        private static void StartCycle()
        {
            object selected = GetSelectedItem();
            if (requireSelectedFishingRod.Value && !IsTypeOrBaseType(selected, "DolocTown.ItemFishingRod"))
            {
                ScheduleStartRetry("selected item is " + (selected == null ? "null" : selected.GetType().FullName));
                return;
            }

            SetPhase(AutoFishingPhase.Starting);
            useToolPressUntil = NowSeconds() + NativeHoldSeconds();
            if (!TryUseSelectedItemAsTool(selected))
            {
                ScheduleStartRetry("UseAsTool did not run");
            }
        }

        private static bool TryUseSelectedItemAsTool(object selected)
        {
            if (selected == null)
            {
                return false;
            }

            try
            {
                Type selectedType = selected.GetType();
                MethodInfo useAsTool = GetUseAsToolMethod(selectedType);
                if (useAsTool == null)
                {
                    LogDebug("Selected item has no UseAsTool method: " + selectedType.FullName);
                    return false;
                }

                LogDebug("Calling UseAsTool on " + selectedType.FullName + ".");
                useAsTool.Invoke(selected, null);
                return true;
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException ?? e;
                LogInfo("UseAsTool threw: " + inner.GetType().Name + " " + inner.Message);
                return false;
            }
            catch (Exception e)
            {
                LogInfo("UseAsTool failed: " + e.GetType().Name + " " + e.Message);
                return false;
            }
        }

        private static void ScheduleStartRetry(string reason)
        {
            float now = NowSeconds();
            if (now - lastStartRetryLogAt >= 5f)
            {
                lastStartRetryLogAt = now;
                LogInfo("Fishing start retry scheduled: " + reason + ".");
            }

            SetPhase(AutoFishingPhase.Cooldown);
            nextRecastAt = now + 1f;
            useToolPressUntil = 0f;
            RestoreFastAnimationRuntime();
        }

        private static void DisableForManualInput()
        {
            LogInfo("Manual input detected. Disabling auto fishing.");
            SetEnabled(false, "manual input");
        }

        private static void ScheduleRecast()
        {
            if (!AutoRecastEnabled())
            {
                SetPhase(AutoFishingPhase.Idle);
                return;
            }

            SetPhase(AutoFishingPhase.Cooldown);
            nextRecastAt = NowSeconds() + Mathf.Max(0.1f, recastDelaySeconds.Value);
        }

        private static void ResetRuntimeState()
        {
            phase = AutoFishingPhase.Idle;
            phaseStartedAt = NowSeconds();
            nextRecastAt = 0f;
            useToolPressUntil = 0f;
            RestoreFastAnimationRuntime();
            lastBonusNoteIndex = -1;
            bonusTapFrame = -1;
            lastStartRetryLogAt = -999f;
            currentWaitState = null;
            currentMiniGame = null;
            ResetMiniGameCache();
            cachedWaitFrame = -1;
            ResetInputCaches();
        }

        private static void ResetInputCaches()
        {
            cachedHookFrame = -1;
            cachedHookResult = false;
            cachedMiniGamePressFrame = -1;
            cachedMiniGamePressResult = false;
        }

        private static void SetPhase(AutoFishingPhase next)
        {
            if (phase == next)
            {
                return;
            }

            phase = next;
            phaseStartedAt = NowSeconds();
            if (verboseLogging.Value)
            {
                LogInfo("Phase: " + next);
            }
        }

        private static bool IsInActiveFishingFlow()
        {
            return phase == AutoFishingPhase.Starting ||
                   phase == AutoFishingPhase.HoldingCast ||
                   phase == AutoFishingPhase.Casting ||
                   phase == AutoFishingPhase.Waiting ||
                   phase == AutoFishingPhase.MiniGame ||
                   phase == AutoFishingPhase.Pulling;
        }

        private static bool ShouldTapBonusNote()
        {
            DolocTown.FishingNoteData note = GetCurrentNote();
            if (note == null || note.noteType != DolocTown.FishingNoteType.Bonus)
            {
                return bonusTapFrame == Time.frameCount;
            }

            float now = GetCurrentMiniGameTime();
            float reactionDelay = Mathf.Max(0f, miniGameReactionDelaySeconds.Value);
            if (now < note.startTime + reactionDelay || now > note.endTime)
            {
                return bonusTapFrame == Time.frameCount;
            }

            if (lastBonusNoteIndex != note.index)
            {
                lastBonusNoteIndex = note.index;
                bonusTapFrame = Time.frameCount;
            }

            return bonusTapFrame == Time.frameCount;
        }

        private static DolocTown.FishingNoteData GetCurrentNote()
        {
            RefreshMiniGameCache();
            return cachedCurrentNote;
        }

        private static float GetCurrentMiniGameTime()
        {
            RefreshMiniGameCache();
            return cachedCurrentTime;
        }

        private static void RefreshMiniGameCache()
        {
            if (cachedMiniGameFrame == Time.frameCount)
            {
                return;
            }

            cachedMiniGameFrame = Time.frameCount;
            cachedCurrentNote = null;
            cachedCurrentTime = 0f;

            if (currentMiniGame == null)
            {
                return;
            }

            try
            {
                if (currentNoteField != null)
                {
                    cachedCurrentNote = currentNoteField.GetValue(currentMiniGame) as DolocTown.FishingNoteData;
                }

                if (currentTimeField != null)
                {
                    cachedCurrentTime = ReadFloat(currentTimeField, currentMiniGame, 0f);
                }
            }
            catch (Exception e)
            {
                LogDebug("Failed to refresh fishing minigame cache: " + e.Message);
            }
        }

        private static void ResetMiniGameCache()
        {
            cachedCurrentNote = null;
            cachedCurrentTime = 0f;
            cachedMiniGameFrame = -1;
        }

        private static void RefreshWaitCache()
        {
            if (cachedWaitFrame == Time.frameCount)
            {
                return;
            }

            cachedWaitFrame = Time.frameCount;
            cachedWaitForFishBite = true;
            cachedFishOnHookDuration = 0f;

            if (currentWaitState == null)
            {
                return;
            }

            try
            {
                cachedWaitForFishBite = ReadBool(waitForFishBiteField, currentWaitState, true);
                cachedFishOnHookDuration = ReadFloat(fishOnHookDurationField, currentWaitState, 0f);
            }
            catch (Exception e)
            {
                LogDebug("Failed to refresh fishing wait cache: " + e.Message);
            }
        }

        private static MethodInfo GetUseAsToolMethod(Type type)
        {
            MethodInfo method;
            if (useAsToolMethodCache.TryGetValue(type, out method))
            {
                return method;
            }

            method = FindMethodInHierarchy(type, "UseAsTool", Type.EmptyTypes);
            useAsToolMethodCache[type] = method;
            return method;
        }

        private static FieldInfo FindFieldByTypeName(string typeName, string fieldName)
        {
            Type type = AccessTools.TypeByName(typeName);
            return type == null ? null : AccessTools.Field(type, fieldName);
        }

        private static float ReadCastProgress(DolocTown.AgentStateFishingReady instance)
        {
            if (readyCastTimerField == null)
            {
                return 1f;
            }

            try
            {
                object timer = readyCastTimerField.GetValue(instance);
                return ReadPropertyFloat(timer, "Progress", 0f);
            }
            catch (Exception e)
            {
                LogDebug("Failed to read cast progress: " + e.Message);
                return 1f;
            }
        }

        private static void ApplyFastWaitInterval(DolocTown.AgentStateFishingWait instance)
        {
            if (!FastModeEnabled() || waitRollTimerField == null)
            {
                return;
            }

            object timer = null;
            try
            {
                timer = waitRollTimerField.GetValue(instance);
            }
            catch
            {
                timer = null;
            }

            if (timer == null)
            {
                return;
            }

            float original = ReadDolocGlobalFloat("FishingRollInterval", 1f);
            float multiplier = Mathf.Clamp(fastBiteIntervalMultiplier.Value, 0.1f, 1f);
            MethodInfo setInterval = FindMethod(timer.GetType(), "SetInterval", new Type[] { typeof(float) });
            if (setInterval != null)
            {
                setInterval.Invoke(timer, new object[] { original * multiplier });
            }
        }

        private static void ApplyInstantBite(DolocTown.AgentStateFishingWait instance)
        {
            if (!InstantBiteEnabled() || instance == null)
            {
                return;
            }

            try
            {
                bool rolled = false;
                if (rollFishMethod != null)
                {
                    object result = rollFishMethod.Invoke(instance, null);
                    rolled = result is bool && (bool)result;
                }

                SetFieldValue(waitHasRolledField, instance, true);
                SetFieldValue(waitHookProbabilityField, instance, 1f);
                SetFieldValue(waitForFishBiteField, instance, false);
                SetFieldValue(fishOnHookDurationField, instance, Mathf.Max(0.1f, ReadDolocGlobalFloat("PullTiming", 1f)));
                cachedWaitFrame = -1;
                ResetInputCaches();
                LogDebug("Instant bite applied. RollFish=" + rolled + ".");
            }
            catch (Exception e)
            {
                LogDebug("Instant bite failed: " + e.Message);
            }
        }

        private static void CompleteMiniGameIfConfigured(DolocTown.FishingGameScrollBar instance)
        {
            if (!SkipMiniGameEnabled() || instance == null)
            {
                return;
            }

            try
            {
                int stamina = ReadInt(fishStaminaField, instance, 1);
                SetFieldValue(currentScoreField, instance, Mathf.Max(1f, stamina));
                if (currentGameStatusField != null)
                {
                    currentGameStatusField.SetValue(instance, Enum.ToObject(currentGameStatusField.FieldType, 1));
                }

                if (refreshProgressMethod != null)
                {
                    refreshProgressMethod.Invoke(instance, null);
                }

                ResetMiniGameCache();
                ResetInputCaches();
                LogDebug("Fishing minigame completed by SkipMiniGame.");
            }
            catch (Exception e)
            {
                LogDebug("Skip minigame failed: " + e.Message);
            }
        }

        private static bool HasManualCancelInput()
        {
            object userInput = GetDolocStaticProperty("UserInput", "get_UserInput");
            if (userInput == null)
            {
                return false;
            }

            return ReadCachedBoolProperty(userInput, "NormalIsMovePressed") ||
                   ReadCachedBoolProperty(userInput, "NormalJump") ||
                   ReadCachedBoolProperty(userInput, "NormalDash") ||
                   ReadCachedBoolProperty(userInput, "GlobalToggleMenu") ||
                   ReadCachedBoolProperty(userInput, "GlobalIsCancelPressed");
        }

        private static bool IsSelectedItemFishingRod()
        {
            try
            {
                object selected = GetSelectedItem();
                if (IsTypeOrBaseType(selected, "DolocTown.ItemFishingRod"))
                {
                    return true;
                }

                LogDebug("Selected item is " + (selected == null ? "null" : selected.GetType().FullName) + ".");
                return false;
            }
            catch (Exception e)
            {
                LogDebug("Selected item check failed: " + e.Message);
                return false;
            }
        }

        private static object GetSelectedItem()
        {
            Type dolocApi = AccessTools.TypeByName("DolocAPI");
            if (dolocApi == null)
            {
                return null;
            }

            MethodInfo selectedGetter = FindMethod(dolocApi, "get_SelectedItem", Type.EmptyTypes);
            return selectedGetter == null ? null : selectedGetter.Invoke(null, null);
        }

        private static bool IsTypeOrBaseType(object value, string fullName)
        {
            Type type = value == null ? null : value.GetType();
            while (type != null)
            {
                if (type.FullName == fullName)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static MethodInfo FindMethodInHierarchy(Type type, string name, Type[] parameters)
        {
            while (type != null)
            {
                MethodInfo method = FindMethod(type, name, parameters);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetDolocStaticProperty(string propertyName, string getterName)
        {
            Type dolocApi = AccessTools.TypeByName("DolocAPI");
            if (dolocApi == null)
            {
                return null;
            }

            MethodInfo getter = FindMethod(dolocApi, getterName, Type.EmptyTypes);
            if (getter == null)
            {
                getter = AccessTools.PropertyGetter(dolocApi, propertyName);
            }

            return getter == null ? null : getter.Invoke(null, null);
        }

        private static float ReadDolocGlobalFloat(string propertyName, float fallback)
        {
            try
            {
                object parameter = GetDolocStaticProperty("GlobalParameter", "get_GlobalParameter");
                return ReadPropertyFloat(parameter, propertyName, fallback);
            }
            catch
            {
                return fallback;
            }
        }

        private static object ReadProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            MethodInfo getter = FindMethod(target.GetType(), "get_" + propertyName, Type.EmptyTypes);
            if (getter == null)
            {
                getter = AccessTools.PropertyGetter(target.GetType(), propertyName);
            }

            return getter == null ? null : getter.Invoke(target, null);
        }

        private static int ReadIntMember(object target, string memberName, int fallback)
        {
            object value = ReadMember(target, memberName);
            if (value is int)
            {
                return (int)value;
            }

            if (value is float)
            {
                return Mathf.RoundToInt((float)value);
            }

            if (value is Enum)
            {
                return Convert.ToInt32(value);
            }

            return fallback;
        }

        private static string ReadStringMember(object target, string memberName, string fallback)
        {
            object value = ReadMember(target, memberName);
            return value == null ? fallback : value.ToString();
        }

        private static object ReadMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            try
            {
                object value = ReadProperty(target, memberName);
                if (value != null)
                {
                    return value;
                }
            }
            catch
            {
            }

            try
            {
                FieldInfo field = FindFieldInHierarchy(target.GetType(), memberName);
                return field == null ? null : field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool ReadBoolProperty(object target, string propertyName)
        {
            try
            {
                object value = ReadProperty(target, propertyName);
                return value is bool && (bool)value;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadCachedBoolProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return false;
            }

            try
            {
                Type type = target.GetType();
                string key = type.FullName + "." + propertyName;
                MethodInfo getter;
                if (!boolPropertyGetterCache.TryGetValue(key, out getter))
                {
                    getter = FindMethod(type, "get_" + propertyName, Type.EmptyTypes);
                    if (getter == null)
                    {
                        getter = AccessTools.PropertyGetter(type, propertyName);
                    }

                    boolPropertyGetterCache[key] = getter;
                }

                if (getter == null)
                {
                    return false;
                }

                object value = getter.Invoke(target, null);
                return value is bool && (bool)value;
            }
            catch
            {
                return false;
            }
        }

        private static float ReadPropertyFloat(object target, string propertyName, float fallback)
        {
            try
            {
                object value = ReadProperty(target, propertyName);
                if (value is float)
                {
                    return (float)value;
                }
                if (value is int)
                {
                    return (int)value;
                }
            }
            catch
            {
                return fallback;
            }

            return fallback;
        }

        private static void SetFieldValue(FieldInfo field, object target, object value)
        {
            if (field == null || target == null)
            {
                return;
            }

            field.SetValue(target, value);
        }

        private static object GetFieldValue(FieldInfo field, object target)
        {
            if (field == null || target == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static bool ReadBool(FieldInfo field, object target, bool fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }

            try
            {
                return (bool)field.GetValue(target);
            }
            catch
            {
                return fallback;
            }
        }

        private static float ReadFloat(FieldInfo field, object target, float fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }

            try
            {
                object value = field.GetValue(target);
                if (value is float)
                {
                    return (float)value;
                }
                if (value is int)
                {
                    return (int)value;
                }
            }
            catch
            {
                return fallback;
            }

            return fallback;
        }

        private static int ReadInt(FieldInfo field, object target, int fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }

            try
            {
                object value = field.GetValue(target);
                if (value is int)
                {
                    return (int)value;
                }
                if (value is float)
                {
                    return Mathf.RoundToInt((float)value);
                }
            }
            catch
            {
                return fallback;
            }

            return fallback;
        }

        private static MethodInfo FindMethod(Type type, string name, Type[] parameters)
        {
            if (type == null)
            {
                return null;
            }

            return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
        }

        private static void ShowSmallMessage(string text)
        {
            try
            {
                Type dolocApi = AccessTools.TypeByName("DolocAPI");
                MethodInfo method = FindMethod(dolocApi, "ShowMessageBoxSmall", new Type[] { typeof(string), typeof(float) });
                if (method != null)
                {
                    method.Invoke(null, new object[] { text, 2f });
                }
            }
            catch
            {
            }
        }

        private static void LogInfo(string message)
        {
            if (AutoFishingPlugin.Log != null)
            {
                AutoFishingPlugin.Log.LogInfo(message);
            }
        }

        private static void LogDebug(string message)
        {
            if (verboseLogging != null && verboseLogging.Value && AutoFishingPlugin.Log != null)
            {
                AutoFishingPlugin.Log.LogInfo(message);
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.DolocUserInput), "get_NormalUseTool")]
    internal static class NormalUseToolPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && AutoFishingController.ShouldUseToolPressed())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.DolocUserInput), "get_NormalUseToolInProgress")]
    internal static class NormalUseToolInProgressPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && AutoFishingController.ShouldUseToolHeld())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.DolocUserInput), "get_NormalFishing")]
    internal static class NormalFishingPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && AutoFishingController.ShouldFishingPressed())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.DolocUserInput), "get_NormalFishingInProgress")]
    internal static class NormalFishingInProgressPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && AutoFishingController.ShouldHookFish())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.FishingGameScrollBar), "get_IsFishingKeyPushed")]
    internal static class FishingKeyPushedPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && AutoFishingController.ShouldMiniGamePress())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingReady), "OnEnter")]
    internal static class FishingReadyEnterPatch
    {
        private static void Postfix(DolocTown.AgentStateFishingReady __instance)
        {
            AutoFishingController.OnReadyEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingReady), "OnPlay")]
    internal static class FishingReadyPlayPatch
    {
        private static void Postfix(DolocTown.AgentStateFishingReady __instance)
        {
            AutoFishingController.OnReadyPlay(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingCast), "OnEnter")]
    internal static class FishingCastEnterPatch
    {
        private static void Postfix(DolocTown.AgentStateFishingCast __instance)
        {
            AutoFishingController.OnCastEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.FishRodRenderer), "CastHook")]
    internal static class FishRodRendererCastHookPatch
    {
        private static void Postfix(DolocTown.FishRodRenderer __instance)
        {
            AutoFishingController.OnCastHook(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingWait), "OnEnter")]
    internal static class FishingWaitEnterPatch
    {
        private static void Prefix()
        {
            AutoFishingController.RestoreFastAnimationRuntime();
        }

        private static void Postfix(DolocTown.AgentStateFishingWait __instance)
        {
            AutoFishingController.OnWaitEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.FishingGameScrollBar), "StartGame")]
    internal static class FishingGameStartPatch
    {
        private static void Postfix(DolocTown.FishingGameScrollBar __instance)
        {
            AutoFishingController.OnMiniGameStart(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.FishingGameScrollBar), "StopGame")]
    internal static class FishingGameStopPatch
    {
        private static void Postfix(DolocTown.FishingGameScrollBar __instance)
        {
            AutoFishingController.OnMiniGameStop(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingPull), "OnEnter")]
    internal static class FishingPullEnterPatch
    {
        private static void Prefix()
        {
            AutoFishingController.RestoreFastAnimationRuntime();
        }

        private static void Postfix(DolocTown.AgentStateFishingPull __instance)
        {
            AutoFishingController.OnPullEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateFishingPull), "OnExit")]
    internal static class FishingPullExitPatch
    {
        private static void Postfix()
        {
            AutoFishingController.OnPullExit();
        }
    }
}
