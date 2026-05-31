using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
#if SMAPI_ACTION_SPEED_BUILD
using DolocTown.SMAPI;
#else
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
#endif
using HarmonyLib;
using UnityEngine;

#if SMAPI_ACTION_SPEED_BUILD
[assembly: AssemblyVersion("1.3.2.0")]
[assembly: AssemblyFileVersion("1.3.2.0")]
#endif

namespace Dlk.DolocActionSpeed
{
#if SMAPI_ACTION_SPEED_BUILD
    public sealed class ActionSpeedMod : IDolocMod, IDolocModLifecycle
    {
        private IModHelper helper;
        private Harmony harmony;
        private bool active;

        public void Entry(IModHelper helper)
        {
            this.helper = helper;
            ActionSpeedPlugin.Log = new ManualLogSource(helper.Monitor);
            ActionSpeedController.Bind(ConfigFile.Create(helper.Config));
            StartRuntime("Entry");
            helper.Monitor.Info(ActionSpeedPlugin.PluginName + " loaded as a native DolocTown SMAPI mod. F10 menu.");
        }

        public void OnEnabled()
        {
            StartRuntime("Workshop enabled");
        }

        public void OnDisabled()
        {
            if (helper != null)
            {
                helper.Events.UpdateTicked -= OnUpdateTicked;
                helper.Ui.GuiRendering -= OnGuiRendering;
            }

            ActionSpeedController.UnbindSmapi();
            ActionSpeedController.RestoreRuntime();
            if (helper != null && harmony != null)
            {
                helper.Patching.UnpatchSelf(harmony);
                harmony = null;
            }

            active = false;
            if (helper != null)
            {
                helper.Monitor.Warn(ActionSpeedPlugin.PluginName + " was disabled. Runtime events and Harmony patches were stopped; restart the game to unload the DLL completely.");
            }
        }

        private void StartRuntime(string reason)
        {
            if (helper == null || active)
            {
                return;
            }

            harmony = helper.Patching.CreateHarmony(ActionSpeedPlugin.PluginGuid);
            helper.Patching.PatchAll(harmony, typeof(ActionSpeedMod).Assembly);
            helper.Events.UpdateTicked += OnUpdateTicked;
            helper.Ui.GuiRendering += OnGuiRendering;
            ActionSpeedController.BindSmapi(helper);
            ReportDiagnostics();
            LogPatchedMethods(reason);
            active = true;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (active)
            {
                ActionSpeedController.Update();
            }
        }

        private void OnGuiRendering(object sender, GuiRenderingEventArgs e)
        {
            if (active)
            {
                ActionSpeedController.OnGui();
            }
        }

        private void ReportDiagnostics()
        {
            helper.Diagnostics.ReportFeature("native-smapi", "SMAPI native entry", DiagnosticFeatureStatus.Available, "Loaded through IDolocMod; no BepInEx.dll reference is required.");
            helper.Diagnostics.ReportFeature("action-speed-harmony", "Timing-sensitive Harmony patches", DiagnosticFeatureStatus.Experimental, "Uses mod-owned Harmony prefixes for harvest, bottle fill, and machine-add animation timing because SMAPI 0.8.1 object interaction events are postfix-only.");
            helper.Diagnostics.ReportFeature("right-click-drink", "Safe continuous bottled-water drinking", DiagnosticFeatureStatus.Available, "Requires selected item to implement IEatable and match GlobalParameter.WaterItems.");
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

    internal static class ActionSpeedPlugin
    {
        public const string PluginGuid = "Yuuka.ActionSpeed";
        public const string LegacyPluginGuid = "com.dlk.doloctown.actionspeed";
        public const string PluginName = "DolocTownActionSpeed";
        public const string PluginVersion = "1.3.2";

        internal static ManualLogSource Log;
    }
#else
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ActionSpeedPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dlk.doloctown.actionspeed";
        public const string PluginName = "DolocTownActionSpeed";
        public const string PluginVersion = "1.2.3";

        private Harmony harmony;
        private GameObject runnerObject;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ActionSpeedController.Bind(Config);

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(ActionSpeedPlugin).Assembly);
            LogPatchedMethods();

            runnerObject = new GameObject(PluginName + ".Runner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<ActionSpeedRunner>();

            Logger.LogInfo(PluginName + " loaded. F10 menu.");
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
            Logger.LogInfo(PluginName + " plugin component OnDestroy observed. Runtime animation speed cache will be restored if possible.");
            ActionSpeedController.RestoreRuntime();
        }
    }
#endif

#if !SMAPI_ACTION_SPEED_BUILD
    public sealed class ActionSpeedRunner : MonoBehaviour
    {
        private void Update()
        {
            ActionSpeedController.Update();
        }

        private void OnGUI()
        {
            ActionSpeedController.OnGui();
        }
    }
#endif

    internal static class ActionSpeedController
    {
        private enum FastInteractKind
        {
            None,
            BottleFill,
            MachineAdd,
            Harvest
        }

        private const string TextWindowTitle = "\u52a8\u4f5c\u52a0\u901f\u8bbe\u7f6e (F10)";
        private const string TextVersion = "\u7248\u672c ";
        private const string TextMasterToggle = "\u603b\u5f00\u5173";
        private const string TextMasterHint = "\u53ef\u5728\u672c\u83dc\u5355\u4e2d\u5173\u95ed\u603b\u5f00\u5173\uff1b\u5173\u95ed\u540e\u6682\u505c\u6240\u6709\u52a0\u901f\u548c\u81ea\u52a8\u52a8\u4f5c\u3002";
        private const string TextToolToggle = "\u5de5\u5177\u52a8\u4f5c\u52a0\u901f (\u65a7\u5934/\u9550\u5b50/\u9570\u5200)";
        private const string TextToolMultiplier = "\u5de5\u5177\u500d\u7387 (\u6700\u9ad8 4x)";
        private const string TextBottleToggle = "\u88c5\u6c34\u52a8\u4f5c\u52a0\u901f";
        private const string TextBottleMultiplier = "\u88c5\u6c34\u500d\u7387";
        private const string TextEatToggle = "\u5403\u559d\u52a8\u4f5c\u52a0\u901f";
        private const string TextEatMultiplier = "\u5403\u559d\u500d\u7387";
        private const string TextMachineToggle = "\u673a\u5668\u52a0\u6599\u52a8\u4f5c\u52a0\u901f";
        private const string TextMachineMultiplier = "\u673a\u5668\u52a0\u6599\u500d\u7387";
        private const string TextHarvestToggle = "\u91c7\u6458/\u6536\u83b7/\u6811\u8102\u52a8\u4f5c\u52a0\u901f";
        private const string TextHarvestMultiplier = "\u91c7\u6458/\u6536\u83b7/\u6811\u8102\u500d\u7387";
        private const string TextAutoFillToggle = "\u81ea\u52a8\u88c5\u6c34";
        private const string TextRightClickDrinkToggle = "\u957f\u6309\u53f3\u952e\u8fde\u7eed\u559d\u74f6\u88c5\u6c34";
        private const string TextDrinkHint = "\u53f3\u952e\u957f\u6309\u8fde\u559d\u74f6\u88c5\u6c34\uff1b\u53ea\u8bc6\u522b\u6c34\u7c7b\u996e\u54c1\u3002";
        private const string TextClose = "\u5173\u95ed";
        private const string TextModOn = "\u52a8\u4f5c\u52a0\u901f\uff1a\u5f00\u542f";
        private const string TextModOff = "\u52a8\u4f5c\u52a0\u901f\uff1a\u5173\u95ed";
        private const string TextAutoFillOn = "\u81ea\u52a8\u88c5\u6c34\uff1a\u5f00\u542f";
        private const string TextAutoFillOff = "\u81ea\u52a8\u88c5\u6c34\uff1a\u5173\u95ed";
        private const string TextCurrentOn = "\u5f53\u524d\u72b6\u6001\uff1a\u5df2\u5f00\u542f";
        private const string TextCurrentOff = "\u5f53\u524d\u72b6\u6001\uff1a\u5df2\u5173\u95ed";

        private const float MaxAnimationMultiplier = 4f;
        private const float MenuDefaultX = 760f;
        private const float MenuDefaultY = 90f;
        private const float MenuDefaultWidth = 430f;
        private const float MenuDefaultHeight = 390f;
        private const int VirtualKeyRightMouse = 0x02;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> toolSpeedEnabled;
        private static ConfigEntry<float> toolAnimationMultiplier;
        private static ConfigEntry<bool> bottleFillSpeedEnabled;
        private static ConfigEntry<float> bottleFillAnimationMultiplier;
        private static ConfigEntry<bool> eatDrinkSpeedEnabled;
        private static ConfigEntry<float> eatDrinkAnimationMultiplier;
        private static ConfigEntry<bool> machineAddSpeedEnabled;
        private static ConfigEntry<float> machineAddAnimationMultiplier;
        private static ConfigEntry<bool> harvestSpeedEnabled;
        private static ConfigEntry<float> harvestAnimationMultiplier;
        private static ConfigEntry<bool> autoFillBottle;
        private static ConfigEntry<KeyCode> continuousDrinkHoldKey;
        private static ConfigEntry<bool> continuousDrinkWithRightClick;
        private static ConfigEntry<KeyCode> menuToggleKey;
        private static ConfigEntry<float> menuWindowX;
        private static ConfigEntry<float> menuWindowY;
        private static ConfigEntry<float> autoActionCooldownSeconds;
        private static ConfigEntry<bool> verboseLogging;
        private static ConfigEntry<bool> uploadDefaultsOffMigrationApplied;

        private static ConfigFile boundConfig;
        private static bool menuOpen;
        private static Rect menuRect = new Rect(MenuDefaultX, MenuDefaultY, MenuDefaultWidth, MenuDefaultHeight);
        private static bool menuPositionDirty;
        private static float nextMenuPositionSaveAt;
        private static float nextAutoFillAt;
        private static float nextDrinkAt;
        private static FastInteractKind pendingInteractKind;
        private static float pendingInteractUntil;
        private static object activeInteractState;
        private static bool waterItemsUnavailableLogged;

        private static readonly Dictionary<Animator, float> originalAnimatorSpeeds = new Dictionary<Animator, float>();
        private static readonly Dictionary<Type, MethodInfo> useAsToolMethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> useAsItemMethodCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<int, bool> nativeKeyWasDown = new Dictionary<int, bool>();
#if SMAPI_ACTION_SPEED_BUILD
        private static IModHelper smapiHelper;
#endif

        private static readonly FieldInfo agentBodyField = FindFieldByTypeName("AgentStateBase", "body");
        private static readonly FieldInfo bodyAnimatorField = AccessTools.Field(typeof(DolocTown.BodyController), "animator");
        private static readonly FieldInfo toolRendererAnimatorField = AccessTools.Field(typeof(DolocTown.ToolRenderer), "animator");
        private static readonly FieldInfo toolRendererColliderField = AccessTools.Field(typeof(DolocTown.ToolRenderer), "_collider");
        private static readonly FieldInfo toolColliderAnimatorField = AccessTools.Field(typeof(DolocTown.ToolCollider), "_animator");
        private static readonly FieldInfo interactCurrentNameField = AccessTools.Field(typeof(DolocTown.AgentStateInteract), "currentName");
        private static readonly FieldInfo resinCollectorCurrentValueField = AccessTools.Field(typeof(DolocTown.ResinCollector), "currentValue");

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        internal static void Bind(ConfigFile config)
        {
            boundConfig = config;

            modEnabled = config.Bind("General", "ModEnabled", false, "Master switch for all action speed and automatic actions in this mod.");

            toolSpeedEnabled = config.Bind("Tool Speed", "ToolSpeedEnabled", false, "Speed up axe, pickaxe, and sickle action animations only.");
            toolAnimationMultiplier = config.Bind("Tool Speed", "ToolAnimationMultiplier", 3f, "Animation multiplier for axe, pickaxe, and sickle actions.");

            bottleFillSpeedEnabled = config.Bind("Bottle Fill", "BottleFillSpeedEnabled", false, "Speed up plastic bottle water-fill interaction animation.");
            bottleFillAnimationMultiplier = config.Bind("Bottle Fill", "BottleFillAnimationMultiplier", 3f, "Animation multiplier for bottle water-fill actions.");
            autoFillBottle = config.Bind("Bottle Fill", "AutoFillBottle", false, "Automatically fill held plastic bottles while the player is in water.");

            eatDrinkSpeedEnabled = config.Bind("Eat Drink Speed", "EatDrinkSpeedEnabled", false, "Speed up eat and drink animations only.");
            eatDrinkAnimationMultiplier = config.Bind("Eat Drink Speed", "EatDrinkAnimationMultiplier", 3f, "Animation multiplier for eat and drink actions.");
            continuousDrinkHoldKey = config.Bind("Eat Drink Speed", "ContinuousDrinkHoldKey", KeyCode.None, "Optional fallback key to repeatedly drink selected bottled water. None means disabled.");
            continuousDrinkWithRightClick = config.Bind("Eat Drink Speed", "ContinuousDrinkWithRightClick", false, "Hold the global right-click action to repeatedly drink selected bottled water.");

            machineAddSpeedEnabled = config.Bind("Machine Add", "MachineAddSpeedEnabled", false, "Speed up direct right-click fuel/feed interaction animations.");
            machineAddAnimationMultiplier = config.Bind("Machine Add", "MachineAddAnimationMultiplier", 3f, "Animation multiplier for direct fuel/feed interactions.");

            harvestSpeedEnabled = config.Bind("Harvest Speed", "HarvestSpeedEnabled", false, "Speed up harvest, wild gathering, and resin collector interaction animations.");
            harvestAnimationMultiplier = config.Bind("Harvest Speed", "HarvestAnimationMultiplier", 3f, "Animation multiplier for crop basin, forage grass, wild vegetation, and resin collector harvests.");

            menuToggleKey = config.Bind("Menu", "MenuToggleKey", KeyCode.F10, "Toggle this mod's in-game settings window.");
            menuWindowX = config.Bind("Menu", "WindowX", MenuDefaultX, "Saved in-game settings window X position.");
            menuWindowY = config.Bind("Menu", "WindowY", MenuDefaultY, "Saved in-game settings window Y position.");
            autoActionCooldownSeconds = config.Bind("Timing", "AutoActionCooldownSeconds", 0.25f, "Minimum interval between automatic fill/drink attempts.");
            verboseLogging = config.Bind("Debug", "VerboseLogging", false, "Write detailed state transitions to BepInEx logs.");
            uploadDefaultsOffMigrationApplied = config.Bind("General", "UploadDefaultsOffMigrationApplied", false, "Internal flag for upload-safe default-off migration.");

            LoadMenuPosition();
            ApplyUploadDefaultsOffMigration();
            MigrateLegacyHotkeys();
            NormalizeMultipliers();
        }

#if SMAPI_ACTION_SPEED_BUILD
        internal static void BindSmapi(IModHelper helper)
        {
            smapiHelper = helper;
            RegisterSmapiButton(menuToggleKey);
            RegisterSmapiButton(continuousDrinkHoldKey);
        }

        internal static void UnbindSmapi()
        {
            if (smapiHelper == null)
            {
                return;
            }

            UnregisterSmapiButton(menuToggleKey);
            UnregisterSmapiButton(continuousDrinkHoldKey);
            smapiHelper = null;
        }

        private static void RegisterSmapiButton(ConfigEntry<KeyCode> entry)
        {
            if (entry != null)
            {
                RegisterSmapiButton(entry.Value);
            }
        }

        private static void RegisterSmapiButton(KeyCode key)
        {
            if (smapiHelper != null && key != KeyCode.None)
            {
                smapiHelper.Input.RegisterButton(key);
            }
        }

        private static void UnregisterSmapiButton(ConfigEntry<KeyCode> entry)
        {
            if (entry != null)
            {
                UnregisterSmapiButton(entry.Value);
            }
        }

        private static void UnregisterSmapiButton(KeyCode key)
        {
            if (smapiHelper != null && key != KeyCode.None)
            {
                smapiHelper.Input.UnregisterButton(key);
            }
        }
#endif

        private static void ApplyUploadDefaultsOffMigration()
        {
            if (uploadDefaultsOffMigrationApplied == null || uploadDefaultsOffMigrationApplied.Value)
            {
                return;
            }

            SetConfigValue(modEnabled, false);
            SetConfigValue(toolSpeedEnabled, false);
            SetConfigValue(bottleFillSpeedEnabled, false);
            SetConfigValue(autoFillBottle, false);
            SetConfigValue(eatDrinkSpeedEnabled, false);
            SetConfigValue(continuousDrinkWithRightClick, false);
            SetConfigValue(machineAddSpeedEnabled, false);
            SetConfigValue(harvestSpeedEnabled, false);
            uploadDefaultsOffMigrationApplied.Value = true;
            SaveConfig();
        }

        private static void MigrateLegacyHotkeys()
        {
            bool changed = false;

            if (continuousDrinkHoldKey != null && continuousDrinkHoldKey.Value == KeyCode.F6)
            {
                continuousDrinkHoldKey.Value = KeyCode.None;
                changed = true;
            }

            if (changed)
            {
                SaveConfig();
                LogInfo("Migrated legacy ActionSpeed continuous-drink hotkey to right-click continuous drinking.");
            }
        }

        private static void NormalizeMultipliers()
        {
            bool changed = false;
            changed |= CapMultiplier(toolAnimationMultiplier);
            changed |= CapMultiplier(bottleFillAnimationMultiplier);
            changed |= CapMultiplier(eatDrinkAnimationMultiplier);
            changed |= CapMultiplier(machineAddAnimationMultiplier);
            changed |= CapMultiplier(harvestAnimationMultiplier);

            if (changed)
            {
                SaveConfig();
                LogInfo("Animation multipliers capped at 4x.");
            }
        }

        private static bool CapMultiplier(ConfigEntry<float> entry)
        {
            if (entry == null || entry.Value <= MaxAnimationMultiplier)
            {
                return false;
            }

            entry.Value = MaxAnimationMultiplier;
            return true;
        }

        internal static void Update()
        {
            if (WasKeyPressed(menuToggleKey.Value))
            {
                if (menuOpen)
                {
                    PersistMenuPosition(true);
                    menuOpen = false;
                }
                else
                {
                    LoadMenuPosition();
                    menuOpen = true;
                }
            }

            if (!IsModEnabled())
            {
                ClearPendingInteract();
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (pendingInteractKind != FastInteractKind.None && now > pendingInteractUntil)
            {
                ClearPendingInteract();
            }

            if (autoFillBottle.Value)
            {
                TickAutoFill(now);
            }

            if (IsContinuousDrinkHeld())
            {
                TickContinuousDrink(now);
            }
        }

        internal static void OnGui()
        {
            if (!menuOpen)
            {
                return;
            }

            ClampMenuRect();
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.82f, 1f, 0.9f, 1f);
            menuRect = GUILayout.Window(914207, menuRect, DrawMenuWindow, TextWindowTitle);
            GUI.backgroundColor = previousColor;
            PersistMenuPosition(false);
        }

        private static void ClampMenuRect()
        {
            const float margin = 16f;
            float maxWidth = Mathf.Max(320f, Screen.width - margin * 2f);
            float maxHeight = Mathf.Max(240f, Screen.height - margin * 2f);
            menuRect.width = Mathf.Min(menuRect.width, maxWidth);
            menuRect.height = Mathf.Min(menuRect.height, maxHeight);
            menuRect.x = Mathf.Clamp(menuRect.x, margin, Mathf.Max(margin, Screen.width - menuRect.width - margin));
            menuRect.y = Mathf.Clamp(menuRect.y, margin, Mathf.Max(margin, Screen.height - menuRect.height - margin));
        }

        private static void LoadMenuPosition()
        {
            menuRect.width = MenuDefaultWidth;
            menuRect.height = MenuDefaultHeight;
            menuRect.x = menuWindowX == null ? MenuDefaultX : menuWindowX.Value;
            menuRect.y = menuWindowY == null ? MenuDefaultY : menuWindowY.Value;
            ClampMenuRect();
        }

        private static void PersistMenuPosition(bool force)
        {
            if (menuWindowX == null || menuWindowY == null)
            {
                return;
            }

            if (Mathf.Abs(menuWindowX.Value - menuRect.x) > 0.5f || Mathf.Abs(menuWindowY.Value - menuRect.y) > 0.5f)
            {
                menuWindowX.Value = menuRect.x;
                menuWindowY.Value = menuRect.y;
                menuPositionDirty = true;
            }

            if (!menuPositionDirty)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (force || now >= nextMenuPositionSaveAt)
            {
                SaveConfig();
                menuPositionDirty = false;
                nextMenuPositionSaveAt = now + 0.5f;
            }
        }

        private static void DrawMenuWindow(int windowId)
        {
            int oldLabelFont = GUI.skin.label.fontSize;
            int oldButtonFont = GUI.skin.button.fontSize;
            int oldToggleFont = GUI.skin.toggle.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.skin.button.fontSize = 12;
            GUI.skin.toggle.fontSize = 12;

            GUILayout.BeginVertical(GUILayout.Width(MenuDefaultWidth - 18f));
            GUILayout.Label(TextVersion + ActionSpeedPlugin.PluginVersion, GUILayout.Height(18f));

            DrawMasterToggle();
            GUILayout.Label(TextMasterHint, GUILayout.Height(18f));

            DrawConfigToggle(TextToolToggle, toolSpeedEnabled);
            DrawMultiplier(TextToolMultiplier, toolAnimationMultiplier);
            DrawConfigToggle(TextBottleToggle, bottleFillSpeedEnabled);
            DrawMultiplier(TextBottleMultiplier, bottleFillAnimationMultiplier);
            DrawConfigToggle(TextEatToggle, eatDrinkSpeedEnabled);
            DrawMultiplier(TextEatMultiplier, eatDrinkAnimationMultiplier);
            DrawConfigToggle(TextMachineToggle, machineAddSpeedEnabled);
            DrawMultiplier(TextMachineMultiplier, machineAddAnimationMultiplier);
            DrawConfigToggle(TextHarvestToggle, harvestSpeedEnabled);
            DrawMultiplier(TextHarvestMultiplier, harvestAnimationMultiplier);
            DrawConfigToggle(TextAutoFillToggle, autoFillBottle);
            DrawConfigToggle(TextRightClickDrinkToggle, continuousDrinkWithRightClick);
            GUILayout.Label(TextDrinkHint, GUILayout.Height(18f));

            if (GUILayout.Button(TextClose, GUILayout.Height(20f)))
            {
                PersistMenuPosition(true);
                menuOpen = false;
            }

            GUILayout.EndVertical();
            GUI.skin.label.fontSize = oldLabelFont;
            GUI.skin.button.fontSize = oldButtonFont;
            GUI.skin.toggle.fontSize = oldToggleFont;
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static void DrawMasterToggle()
        {
            bool value = GUILayout.Toggle(IsModEnabled(), TextMasterToggle, GUILayout.Height(18f));
            if (value != IsModEnabled())
            {
                SetModEnabled(value, false);
            }

            GUILayout.Label(IsModEnabled() ? TextCurrentOn : TextCurrentOff, GUILayout.Height(18f));
        }

        private static void DrawConfigToggle(string label, ConfigEntry<bool> entry)
        {
            bool value = GUILayout.Toggle(entry.Value, label, GUILayout.Height(18f));
            if (value != entry.Value)
            {
                entry.Value = value;
                SaveConfig();
                if (!value)
                {
                    RestoreRuntime();
                }
            }
        }

        private static bool IsModEnabled()
        {
            return modEnabled == null || modEnabled.Value;
        }

        private static void SetConfigValue(ConfigEntry<bool> entry, bool value)
        {
            if (entry != null && entry.Value != value)
            {
                entry.Value = value;
            }
        }

        private static void SetModEnabled(bool value, bool showMessage)
        {
            if (modEnabled == null)
            {
                return;
            }

            if (modEnabled.Value == value)
            {
                return;
            }

            modEnabled.Value = value;
            SaveConfig();
            if (!value)
            {
                ClearPendingInteract();
                activeInteractState = null;
                RestoreRuntime();
            }

            if (showMessage)
            {
                ShowSmallMessage(value ? TextModOn : TextModOff);
            }

            LogInfo("Master toggle " + (value ? "ON" : "OFF") + ".");
        }

        private static void DrawMultiplier(string label, ConfigEntry<float> entry)
        {
            float value = ClampMultiplier(entry.Value);
            GUILayout.BeginHorizontal(GUILayout.Height(20f));
            GUILayout.Label(label + ": " + value.ToString("0") + "x", GUILayout.Width(200f), GUILayout.Height(18f));
            DrawMultiplierButton(entry, 2f);
            DrawMultiplierButton(entry, 3f);
            DrawMultiplierButton(entry, 4f);
            GUILayout.EndHorizontal();
        }

        private static void DrawMultiplierButton(ConfigEntry<float> entry, float value)
        {
            string label = Mathf.Approximately(ClampMultiplier(entry.Value), value) ? "[" + value.ToString("0") + "x]" : value.ToString("0") + "x";
            if (GUILayout.Button(label, GUILayout.Width(58f), GUILayout.Height(18f)))
            {
                entry.Value = value;
                SaveConfig();
            }
        }

        private static void TickAutoFill(float now)
        {
            if (!IsModEnabled())
            {
                return;
            }

            if (now < nextAutoFillAt)
            {
                return;
            }

            if (!IsAgentInWater() || !IsSelectedItemPlasticBottle())
            {
                return;
            }

            if (CallSelectedItemUse("UseAsTool"))
            {
                nextAutoFillAt = now + CooldownSeconds();
                LogDebug("Auto fill attempted.");
            }
        }

        private static void TickContinuousDrink(float now)
        {
            if (!IsModEnabled())
            {
                return;
            }

            if (now < nextDrinkAt)
            {
                return;
            }

            if (!IsSelectedItemBottledWater())
            {
                return;
            }

            if (CallSelectedItemUse("UseAsItem"))
            {
                nextDrinkAt = now + CooldownSeconds();
                LogDebug("Continuous bottled-water drink attempted.");
            }
        }

        private static bool IsContinuousDrinkHeld()
        {
            bool rightClickHeld = continuousDrinkWithRightClick != null &&
                                  continuousDrinkWithRightClick.Value &&
                                  IsRawRightMouseHeld();
            if (rightClickHeld)
            {
                return true;
            }

            return continuousDrinkHoldKey != null &&
                   continuousDrinkHoldKey.Value != KeyCode.None &&
                   IsKeyHeld(continuousDrinkHoldKey.Value);
        }

        private static bool IsRawRightMouseHeld()
        {
            try
            {
                if (Input.GetMouseButton(1))
                {
                    return true;
                }
            }
            catch
            {
            }

            return IsVirtualKeyDown(VirtualKeyRightMouse);
        }

        private static void MarkPendingInteract(FastInteractKind kind)
        {
            pendingInteractKind = kind;
            pendingInteractUntil = Time.realtimeSinceStartup + 0.75f;
        }

        private static void ClearPendingInteract()
        {
            pendingInteractKind = FastInteractKind.None;
            pendingInteractUntil = 0f;
        }

        private static float InteractMultiplier(FastInteractKind kind)
        {
            if (kind == FastInteractKind.BottleFill && bottleFillSpeedEnabled.Value)
            {
                return BottleFillMultiplier();
            }

            if (kind == FastInteractKind.MachineAdd && machineAddSpeedEnabled.Value)
            {
                return MachineAddMultiplier();
            }

            if (kind == FastInteractKind.Harvest && harvestSpeedEnabled.Value)
            {
                return HarvestMultiplier();
            }

            return 1f;
        }

        internal static void OnToolEnter(DolocTown.AgentStateTool instance)
        {
            if (instance == null || !IsModEnabled() || !toolSpeedEnabled.Value)
            {
                return;
            }

            DolocTown.ItemTool tool = null;
            try
            {
                tool = instance.tool;
            }
            catch
            {
                tool = null;
            }

            if (!IsAcceleratedTool(tool))
            {
                return;
            }

            object body = GetBody(instance);
            ApplyAnimatorSpeed(GetBodyAnimator(body), ToolMultiplier());
            ApplyToolRendererSpeed(body, ToolMultiplier());
            LogDebug("Tool animation speed applied to " + SafeItemName(tool) + ".");
        }

        internal static void OnToolExit()
        {
            RestoreRuntime();
        }

        internal static void OnItemBottleUse()
        {
            if (!IsModEnabled() || !bottleFillSpeedEnabled.Value)
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.BottleFill);
        }

        internal static void OnMachineAddInteract(object instance)
        {
            if (instance == null || !IsModEnabled() || !machineAddSpeedEnabled.Value)
            {
                return;
            }

            if (!ReadMemberBool(instance, "CanInteractContinues", false))
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.MachineAdd);
        }

        internal static void OnPlantBasinHarvestInteract(DolocTown.PlantBasin instance)
        {
            if (instance == null || !IsModEnabled() || !harvestSpeedEnabled.Value)
            {
                return;
            }

            if (!ReadMemberBool(instance, "CouldHarvest", false))
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.Harvest);
        }

        internal static void OnPlantBasinGrassHarvestInteract(DolocTown.PlantBasinGrass instance)
        {
            if (instance == null || !IsModEnabled() || !harvestSpeedEnabled.Value)
            {
                return;
            }

            if (!HasMatureForageGrass(instance))
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.Harvest);
        }

        internal static void OnVegetationHarvestInteract(object instance)
        {
            if (instance == null || !IsModEnabled() || !harvestSpeedEnabled.Value)
            {
                return;
            }

            if (!IsMatureVegetation(instance))
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.Harvest);
        }

        internal static void OnResinCollectorInteract(DolocTown.ResinCollector instance)
        {
            if (instance == null || !IsModEnabled() || !harvestSpeedEnabled.Value)
            {
                return;
            }

            if (ReadInt(resinCollectorCurrentValueField, instance, 0) <= 0)
            {
                return;
            }

            MarkPendingInteract(FastInteractKind.Harvest);
        }

        internal static void OnInteractEnter(DolocTown.AgentStateInteract instance)
        {
            if (instance == null || !IsModEnabled() || pendingInteractKind == FastInteractKind.None)
            {
                return;
            }

            if (!string.Equals(ReadString(interactCurrentNameField, instance, string.Empty), "interact", StringComparison.Ordinal))
            {
                return;
            }

            FastInteractKind kind = pendingInteractKind;
            ClearPendingInteract();

            float multiplier = InteractMultiplier(kind);
            if (multiplier <= 1f)
            {
                return;
            }

            activeInteractState = instance;
            ApplyAnimatorSpeed(GetBodyAnimator(GetBody(instance)), multiplier);
            LogDebug("Interact animation speed applied to " + kind + ".");
        }

        internal static void OnInteractExit(DolocTown.AgentStateInteract instance)
        {
            if (activeInteractState != null && ReferenceEquals(activeInteractState, instance))
            {
                activeInteractState = null;
                RestoreRuntime();
            }
        }

        internal static void OnEatEnter(DolocTown.AgentStateEat instance)
        {
            if (instance == null || !IsModEnabled() || !eatDrinkSpeedEnabled.Value)
            {
                return;
            }

            ApplyAnimatorSpeed(GetBodyAnimator(GetBody(instance)), EatDrinkMultiplier());
            LogDebug("Eat/drink animation speed applied.");
        }

        internal static void OnBaseStateExit(object instance)
        {
            if (instance != null && instance.GetType().FullName == "DolocTown.AgentStateEat")
            {
                RestoreRuntime();
            }
        }

        internal static void RestoreRuntime()
        {
            if (originalAnimatorSpeeds.Count == 0)
            {
                return;
            }

            List<Animator> animators = new List<Animator>(originalAnimatorSpeeds.Keys);
            for (int i = 0; i < animators.Count; i++)
            {
                Animator animator = animators[i];
                float speed;
                if (animator != null && originalAnimatorSpeeds.TryGetValue(animator, out speed))
                {
                    try
                    {
                        animator.speed = speed;
                    }
                    catch
                    {
                    }
                }
            }

            originalAnimatorSpeeds.Clear();
        }

        private static void ApplyToolRendererSpeed(object body, float multiplier)
        {
            DolocTown.BodyController controller = body as DolocTown.BodyController;
            if (controller == null || controller.ToolRenderer == null)
            {
                return;
            }

            ApplyAnimatorSpeed(ReadField<Animator>(toolRendererAnimatorField, controller.ToolRenderer), multiplier);
            object collider = ReadField<object>(toolRendererColliderField, controller.ToolRenderer);
            ApplyAnimatorSpeed(ReadField<Animator>(toolColliderAnimatorField, collider), multiplier);
        }

        private static void ApplyAnimatorSpeed(Animator animator, float multiplier)
        {
            if (animator == null)
            {
                return;
            }

            float safeMultiplier = ClampMultiplier(multiplier);
            try
            {
                float original;
                if (!originalAnimatorSpeeds.TryGetValue(animator, out original))
                {
                    original = animator.speed;
                    originalAnimatorSpeeds[animator] = original;
                }

                animator.speed = original * safeMultiplier;
            }
            catch
            {
            }
        }

        private static bool IsAcceleratedTool(DolocTown.ItemTool tool)
        {
            if (tool == null)
            {
                return false;
            }

            try
            {
                DolocTown.Config.Item.ToolType type = tool.ToolType;
                return type == DolocTown.Config.Item.ToolType.AXE ||
                       type == DolocTown.Config.Item.ToolType.PICKAXE ||
                       type == DolocTown.Config.Item.ToolType.SICKLE;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasMatureForageGrass(DolocTown.PlantBasinGrass basin)
        {
            object value = ReadMember(basin, "crops");
            Array crops = value as Array;
            if (crops == null)
            {
                return false;
            }

            for (int i = 0; i < crops.Length; i++)
            {
                object crop = crops.GetValue(i);
                if (ReadMemberBool(crop, "IsMature", false))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMatureVegetation(object vegetation)
        {
            int currentLevel = ReadMemberInt(vegetation, "currentLevel", -1);
            int maxLevel = ReadMemberInt(vegetation, "maxLevel", -1);
            return currentLevel >= 0 && maxLevel >= 0 && currentLevel >= maxLevel;
        }

        private static bool IsSelectedItemPlasticBottle()
        {
            return IsTypeOrBaseType(GetSelectedItem(), "DolocTown.ItemBottle");
        }

        private static bool IsSelectedItemBottledWater()
        {
            object selected = GetSelectedItem();
            if (selected == null || !ImplementsInterface(selected, "DolocTown.IEatable"))
            {
                return false;
            }

            string itemName = ReadMemberString(selected, "name", string.Empty);
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            string[] waterItems = ReadWaterItems();
            if (waterItems == null || waterItems.Length == 0)
            {
                if (!waterItemsUnavailableLogged)
                {
                    waterItemsUnavailableLogged = true;
                    LogInfo("GlobalParameter.WaterItems is unavailable; continuous bottled-water drinking is disabled until it can be read.");
                }

                return false;
            }

            for (int i = 0; i < waterItems.Length; i++)
            {
                if (string.Equals(itemName, waterItems[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] ReadWaterItems()
        {
            try
            {
                object parameter = GetDolocStaticProperty("GlobalParameter", "get_GlobalParameter");
                object value = ReadMember(parameter, "WaterItems");
                return value as string[];
            }
            catch
            {
                return null;
            }
        }

        private static bool IsAgentInWater()
        {
            try
            {
                object value = GetDolocStaticProperty("IsAgentInWater", "get_IsAgentInWater");
                return value is bool && (bool)value;
            }
            catch
            {
                return false;
            }
        }

        private static bool CallSelectedItemUse(string methodName)
        {
            object selected = GetSelectedItem();
            if (selected == null)
            {
                return false;
            }

            try
            {
                Type type = selected.GetType();
                MethodInfo method = methodName == "UseAsTool"
                    ? GetUseMethod(type, useAsToolMethodCache, "UseAsTool")
                    : GetUseMethod(type, useAsItemMethodCache, "UseAsItem");

                if (method == null)
                {
                    return false;
                }

                method.Invoke(selected, null);
                return true;
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException ?? e;
                LogDebug(methodName + " failed: " + inner.GetType().Name + " " + inner.Message);
                return false;
            }
            catch (Exception e)
            {
                LogDebug(methodName + " failed: " + e.GetType().Name + " " + e.Message);
                return false;
            }
        }

        private static MethodInfo GetUseMethod(Type type, Dictionary<Type, MethodInfo> cache, string methodName)
        {
            MethodInfo method;
            if (cache.TryGetValue(type, out method))
            {
                return method;
            }

            method = FindMethodInHierarchy(type, methodName, Type.EmptyTypes);
            cache[type] = method;
            return method;
        }

        private static object GetSelectedItem()
        {
            Type dolocApi = AccessTools.TypeByName("DolocAPI");
            if (dolocApi == null)
            {
                return null;
            }

            MethodInfo getter = FindMethod(dolocApi, "get_SelectedItem", Type.EmptyTypes);
            return getter == null ? null : getter.Invoke(null, null);
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

        private static object GetBody(object state)
        {
            return ReadField<object>(agentBodyField, state);
        }

        private static Animator GetBodyAnimator(object body)
        {
            return ReadField<Animator>(bodyAnimatorField, body);
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

        private static bool ImplementsInterface(object value, string fullName)
        {
            if (value == null)
            {
                return false;
            }

            Type[] interfaces = value.GetType().GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].FullName == fullName)
                {
                    return true;
                }
            }

            return false;
        }

        private static T ReadField<T>(FieldInfo field, object target) where T : class
        {
            if (field == null || target == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(target) as T;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadString(FieldInfo field, object target, string fallback)
        {
            object value = ReadField<object>(field, target);
            return value == null ? fallback : value.ToString();
        }

        private static int ReadInt(FieldInfo field, object target, int fallback)
        {
            object value = ReadField<object>(field, target);
            return value is int ? (int)value : fallback;
        }

        private static object ReadMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            try
            {
                MethodInfo getter = FindMethodInHierarchy(target.GetType(), "get_" + memberName, Type.EmptyTypes);
                if (getter != null)
                {
                    return getter.Invoke(target, null);
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

        private static string ReadMemberString(object target, string memberName, string fallback)
        {
            object value = ReadMember(target, memberName);
            return value == null ? fallback : value.ToString();
        }

        private static bool ReadMemberBool(object target, string memberName, bool fallback)
        {
            object value = ReadMember(target, memberName);
            return value is bool ? (bool)value : fallback;
        }

        private static int ReadMemberInt(object target, string memberName, int fallback)
        {
            object value = ReadMember(target, memberName);
            if (value is int)
            {
                return (int)value;
            }

            return fallback;
        }

        private static FieldInfo FindFieldByTypeName(string typeName, string fieldName)
        {
            Type type = AccessTools.TypeByName(typeName);
            return type == null ? null : AccessTools.Field(type, fieldName);
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

        private static MethodInfo FindMethod(Type type, string name, Type[] parameters)
        {
            if (type == null)
            {
                return null;
            }

            return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
        }

        private static float ToolMultiplier()
        {
            return ClampMultiplier(toolAnimationMultiplier.Value);
        }

        private static float BottleFillMultiplier()
        {
            return ClampMultiplier(bottleFillAnimationMultiplier.Value);
        }

        private static float EatDrinkMultiplier()
        {
            return ClampMultiplier(eatDrinkAnimationMultiplier.Value);
        }

        private static float MachineAddMultiplier()
        {
            return ClampMultiplier(machineAddAnimationMultiplier.Value);
        }

        private static float HarvestMultiplier()
        {
            return ClampMultiplier(harvestAnimationMultiplier.Value);
        }

        private static float ClampMultiplier(float value)
        {
            return Mathf.Clamp(value, 1f, MaxAnimationMultiplier);
        }

        private static float CooldownSeconds()
        {
            return Mathf.Clamp(autoActionCooldownSeconds.Value, 0.05f, 5f);
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if SMAPI_ACTION_SPEED_BUILD
            bool smapiPressed = WasSmapiKeyPressed(key);
#else
            bool smapiPressed = false;
#endif
            bool unityPressed = WasUnityKeyPressed(key);
            bool nativePressed = WasNativeKeyPressed(key);
            return smapiPressed || unityPressed || nativePressed;
        }

        private static bool IsKeyHeld(KeyCode key)
        {
#if SMAPI_ACTION_SPEED_BUILD
            return IsSmapiKeyHeld(key) || IsUnityKeyHeld(key) || IsNativeKeyHeld(key);
#else
            return IsUnityKeyHeld(key) || IsNativeKeyHeld(key);
#endif
        }

#if SMAPI_ACTION_SPEED_BUILD
        private static bool WasSmapiKeyPressed(KeyCode key)
        {
            try
            {
                return smapiHelper != null && key != KeyCode.None && smapiHelper.Input.WasPressed(key);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSmapiKeyHeld(KeyCode key)
        {
            try
            {
                return smapiHelper != null && key != KeyCode.None && smapiHelper.Input.IsDown(key);
            }
            catch
            {
                return false;
            }
        }
#endif

        private static bool WasUnityKeyPressed(KeyCode key)
        {
            try
            {
                return key != KeyCode.None && Input.GetKeyDown(key);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsUnityKeyHeld(KeyCode key)
        {
            try
            {
                return key != KeyCode.None && Input.GetKey(key);
            }
            catch
            {
                return false;
            }
        }

        private static bool WasNativeKeyPressed(KeyCode key)
        {
            int virtualKey = ToVirtualKey(key);
            if (virtualKey == 0)
            {
                return false;
            }

            bool isDown = IsVirtualKeyDown(virtualKey);
            bool wasDown;
            nativeKeyWasDown.TryGetValue(virtualKey, out wasDown);
            nativeKeyWasDown[virtualKey] = isDown;
            return isDown && !wasDown;
        }

        private static bool IsNativeKeyHeld(KeyCode key)
        {
            int virtualKey = ToVirtualKey(key);
            return virtualKey != 0 && IsVirtualKeyDown(virtualKey);
        }

        private static bool IsVirtualKeyDown(int virtualKey)
        {
            try
            {
                return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
            }
            catch
            {
                return false;
            }
        }

        private static int ToVirtualKey(KeyCode key)
        {
            if (key >= KeyCode.F1 && key <= KeyCode.F15)
            {
                return 0x70 + (key - KeyCode.F1);
            }

            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return 0x41 + (key - KeyCode.A);
            }

            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                return 0x30 + (key - KeyCode.Alpha0);
            }

            return 0;
        }

        private static string SafeItemName(DolocTown.Item item)
        {
            try
            {
                return item == null ? "null" : item.name;
            }
            catch
            {
                return "unknown";
            }
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
            catch
            {
            }
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
            if (ActionSpeedPlugin.Log != null)
            {
                ActionSpeedPlugin.Log.LogInfo(message);
            }
        }

        private static void LogDebug(string message)
        {
            if (verboseLogging != null && verboseLogging.Value && ActionSpeedPlugin.Log != null)
            {
                ActionSpeedPlugin.Log.LogInfo(message);
            }
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateTool), "OnEnter")]
    internal static class AgentStateToolEnterPatch
    {
        private static void Postfix(DolocTown.AgentStateTool __instance)
        {
            ActionSpeedController.OnToolEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateTool), "OnExit")]
    internal static class AgentStateToolExitPatch
    {
        private static void Postfix()
        {
            ActionSpeedController.OnToolExit();
        }
    }

    [HarmonyPatch(typeof(DolocTown.ItemBottle), "OnUseAsTool")]
    internal static class ItemBottleUseAsToolPatch
    {
        private static void Prefix()
        {
            ActionSpeedController.OnItemBottleUse();
        }
    }

    [HarmonyPatch(typeof(DolocTown.ItemBottle), "OnUseAsItem")]
    internal static class ItemBottleUseAsItemPatch
    {
        private static void Prefix()
        {
            ActionSpeedController.OnItemBottleUse();
        }
    }

    [HarmonyPatch(typeof(DolocTown.PowerGeneratorFuel), "OnInteract")]
    internal static class PowerGeneratorFuelInteractPatch
    {
        private static void Prefix(DolocTown.PowerGeneratorFuel __instance)
        {
            ActionSpeedController.OnMachineAddInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.Feeder), "OnInteract")]
    internal static class FeederInteractPatch
    {
        private static void Prefix(DolocTown.Feeder __instance)
        {
            ActionSpeedController.OnMachineAddInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.PlantBasin), "OnInteract")]
    internal static class PlantBasinHarvestInteractPatch
    {
        private static void Prefix(DolocTown.PlantBasin __instance)
        {
            ActionSpeedController.OnPlantBasinHarvestInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.PlantBasinGrass), "OnInteract")]
    internal static class PlantBasinGrassHarvestInteractPatch
    {
        private static void Prefix(DolocTown.PlantBasinGrass __instance)
        {
            ActionSpeedController.OnPlantBasinGrassHarvestInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.VegetationBerryThicket), "OnInteract")]
    internal static class VegetationBerryThicketHarvestInteractPatch
    {
        private static void Prefix(DolocTown.VegetationBerryThicket __instance)
        {
            ActionSpeedController.OnVegetationHarvestInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.VegetationGrowLuminous), "OnInteract")]
    internal static class VegetationGrowLuminousHarvestInteractPatch
    {
        private static void Prefix(DolocTown.VegetationGrowLuminous __instance)
        {
            ActionSpeedController.OnVegetationHarvestInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.VegetationCrop), "OnInteract")]
    internal static class VegetationCropHarvestInteractPatch
    {
        private static void Prefix(DolocTown.VegetationCrop __instance)
        {
            ActionSpeedController.OnVegetationHarvestInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.ResinCollector), "OnInteract")]
    internal static class ResinCollectorHarvestInteractPatch
    {
        private static void Prefix(DolocTown.ResinCollector __instance)
        {
            ActionSpeedController.OnResinCollectorInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateInteract), "OnEnter")]
    internal static class AgentStateInteractEnterPatch
    {
        private static void Postfix(DolocTown.AgentStateInteract __instance)
        {
            ActionSpeedController.OnInteractEnter(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateInteract), "OnExit")]
    internal static class AgentStateInteractExitPatch
    {
        private static void Postfix(DolocTown.AgentStateInteract __instance)
        {
            ActionSpeedController.OnInteractExit(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateEat), "OnEnter")]
    internal static class AgentStateEatEnterPatch
    {
        private static void Postfix(DolocTown.AgentStateEat __instance)
        {
            ActionSpeedController.OnEatEnter(__instance);
        }
    }

    [HarmonyPatch]
    internal static class AgentStateBaseExitPatch
    {
        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("AgentStateBase");
            return type == null ? null : AccessTools.Method(type, "OnExit");
        }

        private static void Postfix(object __instance)
        {
            ActionSpeedController.OnBaseStateExit(__instance);
        }
    }

#if SMAPI_ACTION_SPEED_BUILD
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
#endif
}
