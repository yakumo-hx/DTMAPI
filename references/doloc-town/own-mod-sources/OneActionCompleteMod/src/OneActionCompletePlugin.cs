using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
#if SMAPI_ONE_ACTION_BUILD
using DolocTown.SMAPI;
#else
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
#endif
using HarmonyLib;
using UnityEngine;

#if SMAPI_ONE_ACTION_BUILD
[assembly: AssemblyVersion("1.1.2.0")]
[assembly: AssemblyFileVersion("1.1.2.0")]
#endif

namespace Dlk.DolocOneActionComplete
{
#if SMAPI_ONE_ACTION_BUILD
    public sealed class OneActionCompleteMod : IDolocMod, IDolocModLifecycle
    {
        private IModHelper helper;
        private Harmony harmony;
        private bool active;

        public void Entry(IModHelper helper)
        {
            this.helper = helper;
            OneActionCompletePlugin.Log = new ManualLogSource(helper.Monitor);
            OneActionCompleteController.Bind(ConfigFile.Create(helper.Config));
            StartRuntime("Entry");
            helper.Monitor.Info(OneActionCompletePlugin.PluginName + " loaded as a native DolocTown SMAPI mod. F11 menu.");
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

            OneActionCompleteController.UnbindSmapi();
            if (helper != null && harmony != null)
            {
                helper.Patching.UnpatchSelf(harmony);
                harmony = null;
            }

            active = false;
            if (helper != null)
            {
                helper.Monitor.Warn(OneActionCompletePlugin.PluginName + " was disabled. Runtime events and Harmony patches were stopped; restart the game to unload the DLL completely.");
            }
        }

        private void StartRuntime(string reason)
        {
            if (helper == null || active)
            {
                return;
            }

            harmony = helper.Patching.CreateHarmony(OneActionCompletePlugin.PluginGuid);
            helper.Patching.PatchAll(harmony, typeof(OneActionCompleteMod).Assembly);
            helper.Events.UpdateTicked += OnUpdateTicked;
            helper.Ui.GuiRendering += OnGuiRendering;
            OneActionCompleteController.BindSmapi(helper);
            ReportDiagnostics();
            LogPatchedMethods(reason);
            active = true;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (active)
            {
                OneActionCompleteController.Update();
            }
        }

        private void OnGuiRendering(object sender, GuiRenderingEventArgs e)
        {
            if (active)
            {
                OneActionCompleteController.OnGui();
            }
        }

        private void ReportDiagnostics()
        {
            helper.Diagnostics.ReportFeature("native-smapi", "SMAPI native entry", DiagnosticFeatureStatus.Available, "Loaded through IDolocMod; no BepInEx.dll reference is required.");
            helper.Diagnostics.ReportFeature("one-action-resources", "One-action resource completion", DiagnosticFeatureStatus.Risky, "Finishes trees, ores, weeds, and pickaxe garbage after one original tool hit while preserving resource drops and full tool energy cost.");
            helper.Diagnostics.ReportFeature("fuel-callback-prefix", "Fuel machine callback replacement", DiagnosticFeatureStatus.Experimental, "Keeps a mod-owned Harmony prefix for the fuel callback because SMAPI 0.8.1 machine helpers cannot cancel the original delayed single-item callback.");
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

    internal static class OneActionCompletePlugin
    {
        public const string PluginGuid = "Yuuka.OneActionComplete";
        public const string LegacyPluginGuid = "com.dlk.doloctown.oneactioncomplete";
        public const string PluginName = "DolocTownOneActionComplete";
        public const string PluginVersion = "1.1.2";

        internal static ManualLogSource Log;
    }
#else
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class OneActionCompletePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dlk.doloctown.oneactioncomplete";
        public const string PluginName = "DolocTownOneActionComplete";
        public const string PluginVersion = "1.0.3";

        private Harmony harmony;
        private GameObject runnerObject;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            OneActionCompleteController.Bind(Config);

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(OneActionCompletePlugin).Assembly);
            LogPatchedMethods();

            runnerObject = new GameObject(PluginName + ".Runner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<OneActionCompleteRunner>();

            Logger.LogInfo(PluginName + " loaded. F11 menu.");
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
    }
#endif

#if !SMAPI_ONE_ACTION_BUILD
    public sealed class OneActionCompleteRunner : MonoBehaviour
    {
        private void Update()
        {
            OneActionCompleteController.Update();
        }

        private void OnGUI()
        {
            OneActionCompleteController.OnGui();
        }
    }
#endif

    internal static class OneActionCompleteController
    {
        private enum PendingMachineKind
        {
            None,
            Fuel,
            Feeder
        }

        internal sealed class ToolHitSnapshot
        {
            public DolocTown.DungeonResource Resource;
            public DolocTown.ResourceFellData FellData;
            public int PreHealth;
            public int PreChopCounter;
            public int EffectiveDamage;
            public int ToolEnergyCost;
            public bool ShouldCostChopCounter;
            public bool ShouldPayEnergy;
            public bool HadEnoughEnergy;
        }

        private const string TextWindowTitle = "\u4e00\u952e\u5b8c\u6210\u8bbe\u7f6e (F11)";
        private const string TextVersion = "\u7248\u672c ";
        private const string TextMasterToggle = "\u603b\u5f00\u5173";
        private const string TextMasterHint = "\u53ef\u5728\u672c\u83dc\u5355\u4e2d\u5f00\u542f/\u5173\u95ed\u672c\u6a21\u7ec4\u3002";
        private const string TextCurrentOn = "\u5f53\u524d\u72b6\u6001\uff1a\u5df2\u5f00\u542f";
        private const string TextCurrentOff = "\u5f53\u524d\u72b6\u6001\uff1a\u5df2\u5173\u95ed";
        private const string TextTrees = "\u4e00\u6b21\u780d\u6389\u6811";
        private const string TextOres = "\u4e00\u6b21\u6316\u6389\u77ff\u77f3";
        private const string TextGarbage = "\u4e00\u6b21\u6316\u6389\u5783\u573e";
        private const string TextWeeds = "\u4e00\u6b21\u5272\u6389\u8349";
        private const string TextFuel = "\u4e00\u6b21\u52a0\u6ee1\u71c3\u6599\u673a";
        private const string TextFeeder = "\u4e00\u6b21\u52a0\u6ee1\u9972\u6599\u69fd";
        private const string TextClose = "\u5173\u95ed";
        private const string TextModOn = "\u4e00\u952e\u5b8c\u6210\uff1a\u5f00\u542f";
        private const string TextModOff = "\u4e00\u952e\u5b8c\u6210\uff1a\u5173\u95ed";
        private const float MenuDefaultX = 1340f;
        private const float MenuDefaultY = 90f;
        private const float MenuDefaultWidth = 370f;
        private const float MenuDefaultHeight = 285f;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<KeyCode> menuToggleKey;
        private static ConfigEntry<bool> completeTrees;
        private static ConfigEntry<bool> completeOres;
        private static ConfigEntry<bool> completeGarbage;
        private static ConfigEntry<bool> completeWeeds;
        private static ConfigEntry<bool> completeMachineFuel;
        private static ConfigEntry<bool> completeFeeder;
        private static ConfigEntry<float> menuWindowX;
        private static ConfigEntry<float> menuWindowY;
        private static ConfigEntry<bool> verboseLogging;
        private static ConfigEntry<bool> uploadDefaultsOffMigrationApplied;

        private static ConfigFile boundConfig;
        private static bool menuOpen;
        private static Rect menuRect = new Rect(MenuDefaultX, MenuDefaultY, MenuDefaultWidth, MenuDefaultHeight);
        private static bool menuPositionDirty;
        private static float nextMenuPositionSaveAt;
        private static object pendingMachineTarget;
        private static PendingMachineKind pendingMachineKind;
        private static float pendingMachineUntil;
        private static bool fillingFuelMachine;

        private static readonly Dictionary<int, bool> nativeKeyWasDown = new Dictionary<int, bool>();
#if SMAPI_ONE_ACTION_BUILD
        private static IModHelper smapiHelper;
#endif

        private static readonly FieldInfo currentToolField = AccessTools.Field(typeof(DolocTown.ToolCollider), "currentTool");
        private static readonly FieldInfo hitboxField = AccessTools.Field(typeof(DolocTown.ToolCollider), "_hitbox");
        private static readonly FieldInfo chopCounterField = AccessTools.Field(typeof(DolocTown.ToolCollider), "chopCounter");
        private static readonly FieldInfo shouldCostEnergyField = AccessTools.Field(typeof(DolocTown.ToolCollider), "shouldCostEnergy");
        private static readonly FieldInfo generatorFuelField = AccessTools.Field(typeof(DolocTown.PowerGeneratorFuel), "generatorFuel");
        private static readonly FieldInfo generatorHasMoreFuelField = AccessTools.Field(typeof(DolocTown.PowerGeneratorFuel), "hasMoreFuel");
        private static readonly FieldInfo feederCountField = AccessTools.Field(typeof(DolocTown.Feeder), "feederCount");
        private static readonly MethodInfo feederCheckItemMethod = AccessTools.Method(typeof(DolocTown.Feeder), "CheckCurrentItemCanInteract");
        private static readonly MethodInfo feederUpdateBarMethod = AccessTools.Method(typeof(DolocTown.Feeder), "UpdateBarRenderer", Type.EmptyTypes);
        private static readonly MethodInfo feederUpdateSpriteMethod = AccessTools.Method(typeof(DolocTown.Feeder), "UpdateSprite", Type.EmptyTypes);
        private static readonly Type equipmentRendererPatchType = AccessTools.TypeByName("DolocTown.EquipmentRendererBasePatch");
        private static readonly MethodInfo updateProgressBarRendererMethod = AccessTools.Method(
            equipmentRendererPatchType,
            "UpdateProgressBarRenderer",
            new Type[] { typeof(DolocTown.EquipmentRenderer), typeof(float), typeof(Vector2) });
        private static readonly MethodInfo setStateRendererStatusMethod = AccessTools.Method(
            equipmentRendererPatchType,
            "SetStateRendererStatus",
            new Type[] { typeof(DolocTown.EquipmentRenderer), typeof(bool), typeof(Vector2) });

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        internal static void Bind(ConfigFile config)
        {
            boundConfig = config;

            modEnabled = config.Bind("General", "ModEnabled", false, "Master switch for this mod.");
            menuToggleKey = config.Bind("Menu", "MenuToggleKey", KeyCode.F11, "Toggle this mod's in-game settings window.");

            completeTrees = config.Bind("Resources", "CompleteTrees", false, "Finish tree resources after one normal tool hit.");
            completeOres = config.Bind("Resources", "CompleteOres", false, "Finish ore resources after one normal tool hit.");
            completeGarbage = config.Bind("Resources", "CompleteGarbage", false, "Finish pickaxe garbage resources after one normal tool hit.");
            completeWeeds = config.Bind("Resources", "CompleteWeeds", false, "Finish grass and weed resources after one normal tool hit.");

            completeMachineFuel = config.Bind("Machines", "CompleteMachineFuel", false, "Fill fuel machines after one direct right-click add action.");
            completeFeeder = config.Bind("Machines", "CompleteFeeder", false, "Fill feeders after one direct right-click add action.");

            menuWindowX = config.Bind("Menu", "WindowX", MenuDefaultX, "Saved in-game settings window X position.");
            menuWindowY = config.Bind("Menu", "WindowY", MenuDefaultY, "Saved in-game settings window Y position.");
            verboseLogging = config.Bind("Debug", "VerboseLogging", false, "Write detailed state transitions to BepInEx logs.");
            uploadDefaultsOffMigrationApplied = config.Bind("General", "UploadDefaultsOffMigrationApplied", false, "Internal flag for upload-safe default-off migration.");
            LoadMenuPosition();
            ApplyUploadDefaultsOffMigration();
        }

        private static void ApplyUploadDefaultsOffMigration()
        {
            if (uploadDefaultsOffMigrationApplied == null || uploadDefaultsOffMigrationApplied.Value)
            {
                return;
            }

            SetConfigValue(modEnabled, false);
            SetConfigValue(completeTrees, false);
            SetConfigValue(completeOres, false);
            SetConfigValue(completeGarbage, false);
            SetConfigValue(completeWeeds, false);
            SetConfigValue(completeMachineFuel, false);
            SetConfigValue(completeFeeder, false);
            uploadDefaultsOffMigrationApplied.Value = true;
            SaveConfig();
        }

#if SMAPI_ONE_ACTION_BUILD
        internal static void BindSmapi(IModHelper helper)
        {
            smapiHelper = helper;
            RegisterSmapiButton(menuToggleKey);
        }

        internal static void UnbindSmapi()
        {
            if (smapiHelper == null)
            {
                return;
            }

            UnregisterSmapiButton(menuToggleKey);
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

            if (pendingMachineKind != PendingMachineKind.None && Time.realtimeSinceStartup > pendingMachineUntil)
            {
                ClearPendingMachine();
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
            GUI.backgroundColor = new Color(1f, 0.93f, 0.78f, 1f);
            menuRect = GUILayout.Window(914509, menuRect, DrawMenuWindow, TextWindowTitle);
            GUI.backgroundColor = previousColor;
            PersistMenuPosition(false);
        }

        internal static void CaptureToolHit(DolocTown.ToolCollider collider, Collider2D other, ref ToolHitSnapshot state)
        {
            state = null;
            if (!IsModEnabled() || collider == null || other == null)
            {
                return;
            }

            DolocTown.ItemTool tool = ReadField<DolocTown.ItemTool>(currentToolField, collider);
            if (tool == null)
            {
                return;
            }

            DolocTown.IFellable fellable = other.GetComponent<DolocTown.IFellable>();
            if (fellable == null)
            {
                return;
            }

            bool shouldCostChopCounter = SafeShouldCostChopCounter(fellable);
            int chopCounter = ReadFieldValue(chopCounterField, collider, 0);
            if (shouldCostChopCounter && chopCounter == 0)
            {
                return;
            }

            DolocTown.DungeonResourceRenderer renderer = other.GetComponent<DolocTown.DungeonResourceRenderer>();
            DolocTown.DungeonResource resource = renderer == null ? null : renderer.DungeonResource;
            if (resource == null || !IsEnabledForResource(resource))
            {
                return;
            }

            Vector2 hitPoint = GetHitPoint(collider, other, resource);
            DolocTown.ResourceFellData fellData = new DolocTown.ResourceFellData(resource, tool, hitPoint);
            if (!fellData.Valid)
            {
                return;
            }

            if (!fellData.levelMatch)
            {
                return;
            }

            int effectiveDamage = GetEffectiveDamage(resource, fellData);
            if (effectiveDamage <= 0)
            {
                return;
            }

            state = new ToolHitSnapshot
            {
                Resource = resource,
                FellData = fellData,
                PreHealth = resource.currentHealth,
                PreChopCounter = chopCounter,
                EffectiveDamage = effectiveDamage,
                ToolEnergyCost = GetToolEnergyCost(),
                ShouldCostChopCounter = shouldCostChopCounter,
                ShouldPayEnergy = SafeShouldCostEnergy(fellable) && ReadFieldValue(shouldCostEnergyField, collider, false),
                HadEnoughEnergy = HasEnoughEnergyForUsingTool()
            };
        }

        internal static void CompleteToolHit(DolocTown.ToolCollider collider, ToolHitSnapshot state)
        {
            if (!IsModEnabled() || state == null || state.Resource == null)
            {
                return;
            }

            bool originalSucceeded = state.Resource.currentHealth < state.PreHealth;
            if (state.ShouldCostChopCounter)
            {
                originalSucceeded = originalSucceeded || ReadFieldValue(chopCounterField, collider, state.PreChopCounter) < state.PreChopCounter;
            }

            if (!originalSucceeded)
            {
                return;
            }

            ApplyFullToolEnergyCost(state);

            int remainingHealth = state.Resource.currentHealth;
            if (remainingHealth > 0 && remainingHealth < state.PreHealth)
            {
                DolocTown.ResourceFellData finishData = new DolocTown.ResourceFellData(
                    true,
                    state.FellData.toolLevel,
                    remainingHealth,
                    state.FellData.hitPoint,
                    false,
                    false,
                    state.FellData.overrideSpawnLut);
                state.Resource._Fell(finishData);
                LogDebug("Completed resource " + state.Resource.GetType().Name + " after one action.");
            }
        }

        internal static void OnFuelInteract(DolocTown.PowerGeneratorFuel instance)
        {
            if (instance == null || !IsModEnabled() || !completeMachineFuel.Value)
            {
                return;
            }

            object fuelComponent = ReadField<object>(generatorFuelField, instance);
            if (fuelComponent == null || ReadMemberBool(fuelComponent, "IsFull", true))
            {
                return;
            }

            DolocTown.Item selected = SafeSelectedItem();
            if (selected == null || !instance.IsSuitableFuel(selected))
            {
                return;
            }

            MarkPendingMachine(PendingMachineKind.Fuel, instance);
        }

        internal static void OnFeederInteract(DolocTown.Feeder instance)
        {
            if (instance == null || !IsModEnabled() || !completeFeeder.Value)
            {
                return;
            }

            if (!ReadMemberBool(instance, "CanInteractContinues", false))
            {
                return;
            }

            MarkPendingMachine(PendingMachineKind.Feeder, instance);
        }

        internal static void OnInteractExit()
        {
            if (!IsModEnabled() || pendingMachineKind == PendingMachineKind.None || pendingMachineTarget == null)
            {
                return;
            }

            object target = pendingMachineTarget;
            PendingMachineKind kind = pendingMachineKind;
            ClearPendingMachine();

            if (kind == PendingMachineKind.Fuel)
            {
                FillFuelMachine(target as DolocTown.PowerGeneratorFuel);
            }
            else if (kind == PendingMachineKind.Feeder)
            {
                FillFeeder(target as DolocTown.Feeder);
            }
        }

        internal static float CaptureFuelCallbackBefore(object callbackState)
        {
            return ReadFuelAmount(GetFuelGeneratorFromCallback(callbackState));
        }

        internal static bool TryCompleteFuelCallback(object callbackState)
        {
            if (fillingFuelMachine || !IsModEnabled() || !completeMachineFuel.Value)
            {
                return false;
            }

            DolocTown.PowerGeneratorFuel generator = GetFuelGeneratorFromCallback(callbackState);
            if (generator == null ||
                pendingMachineKind != PendingMachineKind.Fuel ||
                pendingMachineTarget == null ||
                !ReferenceEquals(pendingMachineTarget, generator))
            {
                return false;
            }

            string itemName = GetFuelItemNameFromCallback(callbackState);
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            ClearPendingMachine();
            int added = FillFuelMachine(generator, itemName);
            return added > 0;
        }

        internal static void OnFuelInteractCallbackCompleted(object callbackState, float beforeFuel)
        {
            if (beforeFuel < -998f)
            {
                return;
            }

            if (fillingFuelMachine || !IsModEnabled())
            {
                return;
            }

            DolocTown.PowerGeneratorFuel generator = GetFuelGeneratorFromCallback(callbackState);
            if (generator == null ||
                pendingMachineKind != PendingMachineKind.Fuel ||
                pendingMachineTarget == null ||
                !ReferenceEquals(pendingMachineTarget, generator))
            {
                return;
            }

            float afterFuel = ReadFuelAmount(generator);
            ClearPendingMachine();
            if (beforeFuel >= 0f && afterFuel <= beforeFuel + 0.001f)
            {
                return;
            }

            FillFuelMachine(generator);
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
            GUILayout.Label(TextVersion + OneActionCompletePlugin.PluginVersion, GUILayout.Height(18f));

            bool enabled = GUILayout.Toggle(IsModEnabled(), TextMasterToggle, GUILayout.Height(18f));
            if (enabled != IsModEnabled())
            {
                SetModEnabled(enabled, false);
            }

            GUILayout.Label(IsModEnabled() ? TextCurrentOn : TextCurrentOff, GUILayout.Height(18f));
            GUILayout.Label(TextMasterHint, GUILayout.Height(18f));

            DrawConfigToggle(TextTrees, completeTrees);
            DrawConfigToggle(TextOres, completeOres);
            DrawConfigToggle(TextGarbage, completeGarbage);
            DrawConfigToggle(TextWeeds, completeWeeds);
            DrawConfigToggle(TextFuel, completeMachineFuel);
            DrawConfigToggle(TextFeeder, completeFeeder);

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

        private static void ClampMenuRect()
        {
            const float margin = 16f;
            float maxWidth = Mathf.Max(320f, Screen.width - margin * 2f);
            float maxHeight = Mathf.Max(220f, Screen.height - margin * 2f);
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

        private static void DrawConfigToggle(string label, ConfigEntry<bool> entry)
        {
            bool value = GUILayout.Toggle(entry.Value, label, GUILayout.Height(18f));
            if (value != entry.Value)
            {
                entry.Value = value;
                SaveConfig();
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
            if (modEnabled == null || modEnabled.Value == value)
            {
                return;
            }

            modEnabled.Value = value;
            SaveConfig();
            ClearPendingMachine();

            if (showMessage)
            {
                ShowSmallMessage(value ? TextModOn : TextModOff);
            }

            LogDebug("Master toggle " + (value ? "ON" : "OFF") + ".");
        }

        private static void MarkPendingMachine(PendingMachineKind kind, object target)
        {
            pendingMachineKind = kind;
            pendingMachineTarget = target;
            pendingMachineUntil = Time.realtimeSinceStartup + 2f;
        }

        private static void ClearPendingMachine()
        {
            pendingMachineKind = PendingMachineKind.None;
            pendingMachineTarget = null;
            pendingMachineUntil = 0f;
        }

        private static bool IsEnabledForResource(DolocTown.DungeonResource resource)
        {
            if (resource is DolocTown.DungeonResourceOre)
            {
                return completeOres.Value;
            }

            if (resource is DolocTown.DungeonResourceBuildingsGarbage ||
                resource is DolocTown.DungeonResourceModelMechanicalGarbage)
            {
                return completeGarbage.Value;
            }

            if (resource is DolocTown.DungeonResourceTree || resource is DolocTown.DungeonResourceTreeTrunk)
            {
                return completeTrees.Value;
            }

            if (resource is DolocTown.DungeonResourceWeeds)
            {
                return completeWeeds.Value;
            }

            return false;
        }

        private static int GetEffectiveDamage(DolocTown.DungeonResource resource, DolocTown.ResourceFellData fellData)
        {
            int damage = fellData.Damage;
            if (resource is DolocTown.DungeonResourceOre)
            {
                try
                {
                    damage += global::DolocAPI.AgentEquipmentParams.fellCoundAdditionOre;
                }
                catch
                {
                }
            }

            return damage;
        }

        private static Vector2 GetHitPoint(DolocTown.ToolCollider collider, Collider2D other, DolocTown.DungeonResource resource)
        {
            try
            {
                Collider2D hitbox = ReadField<Collider2D>(hitboxField, collider);
                if (hitbox != null)
                {
                    return other.ClosestPoint((Vector2)hitbox.bounds.center);
                }
            }
            catch
            {
            }

            try
            {
                return resource.PositionCenter;
            }
            catch
            {
                return Vector2.zero;
            }
        }

        private static void ApplyFullToolEnergyCost(ToolHitSnapshot state)
        {
            if (state.ToolEnergyCost <= 0 || state.EffectiveDamage <= 0 || !state.ShouldPayEnergy)
            {
                return;
            }

            int requiredHits = Mathf.Max(1, Mathf.CeilToInt(state.PreHealth / (float)state.EffectiveDamage));
            int totalCost = requiredHits * state.ToolEnergyCost;
            int vanillaPaid = state.HadEnoughEnergy ? state.ToolEnergyCost : 0;
            int remainingCost = Mathf.Max(0, totalCost - vanillaPaid);
            if (remainingCost <= 0)
            {
                return;
            }

            global::DolocAPI.ChangeEnergy(-remainingCost);
            LogDebug("Charged remaining tool energy: " + remainingCost + ".");
        }

        private static int GetToolEnergyCost()
        {
            try
            {
                return global::DolocAPI.GlobalParameter.ToolEnergyCost;
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasEnoughEnergyForUsingTool()
        {
            try
            {
                return global::DolocAPI.HasEnoughEnergyForUsingTool();
            }
            catch
            {
                return false;
            }
        }

        private static bool SafeShouldCostEnergy(DolocTown.IFellable fellable)
        {
            try
            {
                return fellable.ShouldCostEnergy;
            }
            catch
            {
                return false;
            }
        }

        private static bool SafeShouldCostChopCounter(DolocTown.IFellable fellable)
        {
            try
            {
                return fellable.ShouldCostChopCounter;
            }
            catch
            {
                return false;
            }
        }

        private static int FillFuelMachine(DolocTown.PowerGeneratorFuel generator)
        {
            return FillFuelMachine(generator, null);
        }

        private static int FillFuelMachine(DolocTown.PowerGeneratorFuel generator, string preferredItemName)
        {
            if (generator == null)
            {
                return 0;
            }

            object fuelComponent = ReadField<object>(generatorFuelField, generator);
            if (fuelComponent == null || ReadMemberBool(fuelComponent, "IsFull", true))
            {
                return 0;
            }

            DolocTown.Item selected = SafeSelectedItem();
            string itemName = string.IsNullOrEmpty(preferredItemName) ? SafeItemName(selected) : preferredItemName;
            if (string.IsNullOrEmpty(itemName))
            {
                return 0;
            }

            DolocTown.Config.Item.ItemInfo fuelItem;
            if (string.IsNullOrEmpty(itemName) || !global::DolocAPI.QueryItemProto(itemName, out fuelItem))
            {
                return 0;
            }

            if (selected == null || !generator.IsSuitableFuel(selected))
            {
                return 0;
            }

            int added = 0;
            fillingFuelMachine = true;
            try
            {
                for (int guard = 0; guard < 999 && !ReadMemberBool(fuelComponent, "IsFull", true); guard++)
                {
                    selected = SafeSelectedItem();
                    if (!IsSameItem(selected, itemName))
                    {
                        break;
                    }

                    if (!generator.IsSuitableFuel(selected))
                    {
                        break;
                    }

                    DolocTown.Item next;
                    if (!selected.CostSelf(out next, false))
                    {
                        break;
                    }

                    generator.AddFuel(fuelItem, true);
                    added++;
                }
            }
            finally
            {
                fillingFuelMachine = false;
            }

            if (added > 0)
            {
                TrySetField(generatorHasMoreFuelField, generator, true);
                RefreshFuelMachineRenderer(generator);
                TrySendUseEquipmentMessage(generator);
                LogDebug("Filled fuel machine with extra count: " + added + ".");
            }

            return added;
        }

        private static DolocTown.PowerGeneratorFuel GetFuelGeneratorFromCallback(object callbackState)
        {
            if (callbackState == null)
            {
                return null;
            }

            FieldInfo field = FindFieldInHierarchy(callbackState.GetType(), "<>4__this");
            return ReadField<DolocTown.PowerGeneratorFuel>(field, callbackState);
        }

        private static string GetFuelItemNameFromCallback(object callbackState)
        {
            FieldInfo field = FindFieldInHierarchy(callbackState == null ? null : callbackState.GetType(), "item");
            DolocTown.Item item = ReadField<DolocTown.Item>(field, callbackState);
            return SafeItemName(item);
        }

        private static float ReadFuelAmount(DolocTown.PowerGeneratorFuel generator)
        {
            object fuelComponent = ReadField<object>(generatorFuelField, generator);
            return ReadMemberFloat(fuelComponent, "Fuel", -1f);
        }

        private static void RefreshFuelMachineRenderer(DolocTown.PowerGeneratorFuel generator)
        {
            object fuelComponent = ReadField<object>(generatorFuelField, generator);
            float percent = ReadMemberFloat(fuelComponent, "FuelPercent", -1f);
            object rendererObject = ReadMember(generator, "Renderer");
            DolocTown.EquipmentRenderer renderer = rendererObject as DolocTown.EquipmentRenderer;
            if (renderer == null || percent < 0f)
            {
                return;
            }

            Vector2 position = Vector2.zero;
            try
            {
                position = (Vector2)generator.PositionCenter;
            }
            catch
            {
            }

            InvokeStatic(updateProgressBarRendererMethod, renderer, percent, position);
            InvokeStatic(setStateRendererStatusMethod, renderer, true, position);
        }

        private static void FillFeeder(DolocTown.Feeder feeder)
        {
            if (feeder == null)
            {
                return;
            }

            DolocTown.Item selected = SafeSelectedItem();
            if (selected == null)
            {
                return;
            }

            string itemName = SafeItemName(selected);
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            int added = 0;
            for (int guard = 0; guard < 999 && ReadMemberFloat(feeder, "progress", 1f) < 1f; guard++)
            {
                selected = SafeSelectedItem();
                if (!IsSameItem(selected, itemName))
                {
                    break;
                }

                int energy;
                if (!CheckFeederItem(feeder, out energy) || energy <= 0)
                {
                    break;
                }

                DolocTown.Item next;
                if (!selected.CostSelf(out next, false))
                {
                    break;
                }

                int current = ReadFieldValue(feederCountField, feeder, 0);
                TrySetField(feederCountField, feeder, current + energy);
                InvokeNoArgs(feederUpdateBarMethod, feeder);
                InvokeNoArgs(feederUpdateSpriteMethod, feeder);
                added++;
            }

            if (added > 0)
            {
                LogDebug("Filled feeder with extra count: " + added + ".");
            }
        }

        private static bool CheckFeederItem(DolocTown.Feeder feeder, out int energy)
        {
            energy = 0;
            if (feederCheckItemMethod == null)
            {
                return false;
            }

            try
            {
                object[] args = new object[] { 0 };
                bool result = (bool)feederCheckItemMethod.Invoke(feeder, args);
                if (args.Length > 0 && args[0] is int)
                {
                    energy = (int)args[0];
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        private static DolocTown.Item SafeSelectedItem()
        {
            try
            {
                return global::DolocAPI.SelectedItem;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSameItem(DolocTown.Item item, string itemName)
        {
            return item != null && string.Equals(SafeItemName(item), itemName, StringComparison.Ordinal);
        }

        private static string SafeItemName(DolocTown.Item item)
        {
            try
            {
                return item == null ? string.Empty : item.name;
            }
            catch
            {
                return string.Empty;
            }
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

        private static T ReadFieldValue<T>(FieldInfo field, object target, T fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }

            try
            {
                object value = field.GetValue(target);
                return value is T ? (T)value : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static void TrySetField(FieldInfo field, object target, object value)
        {
            if (field == null || target == null)
            {
                return;
            }

            try
            {
                field.SetValue(target, value);
            }
            catch
            {
            }
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

        private static bool ReadMemberBool(object target, string memberName, bool fallback)
        {
            object value = ReadMember(target, memberName);
            return value is bool ? (bool)value : fallback;
        }

        private static float ReadMemberFloat(object target, string memberName, float fallback)
        {
            object value = ReadMember(target, memberName);
            if (value is float)
            {
                return (float)value;
            }

            return fallback;
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
                MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static void InvokeNoArgs(MethodInfo method, object target)
        {
            if (method == null || target == null)
            {
                return;
            }

            try
            {
                method.Invoke(target, null);
            }
            catch
            {
            }
        }

        private static void InvokeStatic(MethodInfo method, params object[] args)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(null, args);
            }
            catch
            {
            }
        }

        private static void TrySendUseEquipmentMessage(DolocTown.Equipment equipment)
        {
            try
            {
                MethodInfo method = FindMethodInHierarchy(equipment.GetType(), "SendUseEquipmentMessage", Type.EmptyTypes);
                if (method != null)
                {
                    method.Invoke(equipment, null);
                }
            }
            catch
            {
            }
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if SMAPI_ONE_ACTION_BUILD
            bool smapiPressed = WasSmapiKeyPressed(key);
#else
            bool smapiPressed = false;
#endif
            bool unityPressed = WasUnityKeyPressed(key);
            bool nativePressed = WasNativeKeyPressed(key);
            return smapiPressed || unityPressed || nativePressed;
        }

#if SMAPI_ONE_ACTION_BUILD
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
                global::DolocAPI.ShowMessageBoxSmall(text, 2f);
            }
            catch
            {
            }
        }

        private static void LogDebug(string message)
        {
            if (verboseLogging != null && verboseLogging.Value && OneActionCompletePlugin.Log != null)
            {
                OneActionCompletePlugin.Log.LogInfo(message);
            }
        }
    }

    [HarmonyPatch]
    internal static class ToolColliderHandleToolsPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(DolocTown.ToolCollider), "HandleTools", new Type[] { typeof(Collider2D) });
        }

        private static void Prefix(DolocTown.ToolCollider __instance, Collider2D other, ref OneActionCompleteController.ToolHitSnapshot __state)
        {
            OneActionCompleteController.CaptureToolHit(__instance, other, ref __state);
        }

        private static void Postfix(DolocTown.ToolCollider __instance, OneActionCompleteController.ToolHitSnapshot __state)
        {
            OneActionCompleteController.CompleteToolHit(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(DolocTown.PowerGeneratorFuel), "OnInteract")]
    internal static class PowerGeneratorFuelInteractPatch
    {
        private static void Prefix(DolocTown.PowerGeneratorFuel __instance)
        {
            OneActionCompleteController.OnFuelInteract(__instance);
        }
    }

    [HarmonyPatch]
    internal static class PowerGeneratorFuelCallbackPatch
    {
        private static MethodBase TargetMethod()
        {
            Type[] nestedTypes = typeof(DolocTown.PowerGeneratorFuel).GetNestedTypes(BindingFlags.NonPublic);
            for (int i = 0; i < nestedTypes.Length; i++)
            {
                MethodInfo method = AccessTools.Method(nestedTypes[i], "<OnInteract>b__0", Type.EmptyTypes);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool Prefix(object __instance, ref float __state)
        {
            if (OneActionCompleteController.TryCompleteFuelCallback(__instance))
            {
                __state = -999f;
                return false;
            }

            __state = OneActionCompleteController.CaptureFuelCallbackBefore(__instance);
            return true;
        }

        private static void Postfix(object __instance, float __state)
        {
            OneActionCompleteController.OnFuelInteractCallbackCompleted(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(DolocTown.Feeder), "OnInteract")]
    internal static class FeederInteractPatch
    {
        private static void Prefix(DolocTown.Feeder __instance)
        {
            OneActionCompleteController.OnFeederInteract(__instance);
        }
    }

    [HarmonyPatch(typeof(DolocTown.AgentStateInteract), "OnExit")]
    internal static class AgentStateInteractExitPatch
    {
        private static void Postfix()
        {
            OneActionCompleteController.OnInteractExit();
        }
    }

#if SMAPI_ONE_ACTION_BUILD
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
