using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace DebugConsoleMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private DebugConsoleConfig config = new DebugConsoleConfig();
        private IDebugConsoleApi? consoleApi;
        private IMovementDebugApi? movementApi;
        private bool inSave;
        private bool ignoredTitleLogged;
        private bool updateLogged;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<DebugConsoleConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            helper.Input.RegisterButton("Y");
            helper.Input.RegisterButton("Escape");
            BindApis();

            helper.Events.Save.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            helper.Monitor.Log(T("mod.loaded", "Y-Key Console loaded; press Y after loading a save."));
        }

        private void BindApis()
        {
            consoleApi = helper.ModRegistry.GetApi<IDebugConsoleApi>("DTMAPI.DebugConsoleHost");
            IInventoryDebugApi? inventoryApi = helper.ModRegistry.GetApi<IInventoryDebugApi>("DTMAPI.GameBridge.DolocTown");
            IWeatherDebugApi? weatherApi = helper.ModRegistry.GetApi<IWeatherDebugApi>("DTMAPI.GameBridge.DolocTown");
            ITeleportDebugApi? teleportApi = helper.ModRegistry.GetApi<ITeleportDebugApi>("DTMAPI.GameBridge.DolocTown");
            ITimeDebugApi? timeApi = helper.ModRegistry.GetApi<ITimeDebugApi>("DTMAPI.GameBridge.DolocTown");
            movementApi = helper.ModRegistry.GetApi<IMovementDebugApi>("DTMAPI.GameBridge.DolocTown");
            IInstantSaveDebugApi? instantSaveApi = helper.ModRegistry.GetApi<IInstantSaveDebugApi>("DTMAPI.GameBridge.DolocTown");

            if (consoleApi != null)
            {
                consoleApi.Bind(helper.ModManifest, inventoryApi, weatherApi, teleportApi, timeApi, movementApi, instantSaveApi);
                consoleApi.SetLanguage(helper.ModManifest, config.Language);
            }

            bool bound = consoleApi != null && inventoryApi != null && weatherApi != null && teleportApi != null && timeApi != null && movementApi != null && instantSaveApi != null;
            helper.Monitor.Log(bound ? T("mod.bound", "Debug console API binding completed.") : T("mod.boundMissing", "Debug console API binding is incomplete."), bound ? LogLevel.Info : LogLevel.Warn);
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                NormalizeConfig();
                helper.WriteConfig(config);
                consoleApi?.SetLanguage(helper.ModManifest, config.Language);
                helper.Monitor.Log(T("mod.configSaved", "Y-Key Console config saved."));
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "Y-Key Console"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Console"));
            menu.AddChoiceOption(
                helper.ModManifest,
                () => T("config.language.name", "Display language"),
                () => T("config.language.tooltip", "Choose console UI language. Auto follows DTMAPI language detection."),
                () => config.Language,
                value => config.Language = value,
                new[] { "Auto", "schinese", "english" });
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            inSave = true;
            ignoredTitleLogged = false;
            BindApis();
            helper.Monitor.Log(T("mod.saveLoaded", "Debug console SaveLoaded boundary OK.") + " slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            inSave = false;
            consoleApi?.Close(helper.ModManifest, "ReturnedToTitle");
            movementApi?.ResetSpeed(helper.ModManifest, "ReturnedToTitle");
            helper.Monitor.Log(T("mod.returnedTitle", "Returned to title; debug console closed."));
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                if (!inSave)
                {
                    if (!ignoredTitleLogged)
                    {
                        ignoredTitleLogged = true;
                        helper.Monitor.Log(T("mod.ignoredTitle", "Not in a save yet; ignored Y debug console hotkey."));
                    }
                    return;
                }

                consoleApi?.Toggle(helper.ModManifest, "hotkey Y");
                helper.Monitor.Log(T("mod.toggle", "Y debug console toggle requested.") + " open=" + (consoleApi?.IsOpen == true));
            }
            else if (e.Button.Equals("Escape", StringComparison.OrdinalIgnoreCase) && consoleApi?.IsOpen == true)
            {
                consoleApi.Close(helper.ModManifest, "Escape");
                helper.Monitor.Log("DebugConsoleMod Escape close requested.");
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (updateLogged)
                return;
            updateLogged = true;
            helper.Monitor.Log("DebugConsoleMod UpdateTicked OK tick=" + e.Tick);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ResetConfig()
        {
            config = new DebugConsoleConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            if (!config.Language.Equals("Auto", StringComparison.OrdinalIgnoreCase) &&
                !config.Language.Equals("schinese", StringComparison.OrdinalIgnoreCase) &&
                !config.Language.Equals("english", StringComparison.OrdinalIgnoreCase))
                config.Language = "Auto";
        }

        [DataContract]
        public sealed class DebugConsoleConfig
        {
            [DataMember] public string Language { get; set; } = "Auto";
        }
    }
}
