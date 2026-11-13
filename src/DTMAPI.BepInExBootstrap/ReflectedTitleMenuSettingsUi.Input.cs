using System;
using System.IO;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Json;
using DTMAPI.Core.Manifesting;
using DTMAPI.GameBridge.DolocTown.Native;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed partial class ReflectedTitleMenuSettingsUi
    {
        private IInputHelper? entryInput;
        private IInputRegistration? entryRegistration;
        private InputSettings inputSettings = new InputSettings();
        private bool inputSettingsRegistered;
        private bool inputOwnerClosed;
        private bool nativeTitleEntryEnabledAtStartup;
        private bool nativeTitleEntryDefault;
        private readonly ManifestModel inputManifest = new ManifestModel {
            UniqueID = "DTMAPI.TitleSettings", Name = "DTMAPI Input", Author = "DTMAPI", Version = DTMAPI.Core.Runtime.DtmApiRuntime.ApiVersion, Type = "RuntimeApi"
        };

        private void InitializeInputSettings()
        {
            if (inputSettingsRegistered || inputOwnerClosed || !(configMenu is IDtmConfigMenuKeybindDefaultsApi defaults) || !(configMenu is IDtmConfigMenuApi api)) return;
            inputSettingsRegistered = true;
            runtime.RegisterRuntimeConfigOwner(inputManifest.UniqueID);
            nativeTitleEntryDefault = ReadNativeTitleEntryDefault();
            inputSettings = runtime.Config.ReadConfig<InputSettings>(inputManifest);
            inputSettings.ShowNativeTitleEntry ??= nativeTitleEntryDefault;
            nativeTitleEntryEnabledAtStartup = inputSettings.ShowNativeTitleEntry.Value;
            entryInput = runtime.Input.CreateOwnerBound(inputManifest.UniqueID, () => {
                if (inputOwnerClosed) throw new ObjectDisposedException(inputManifest.UniqueID);
            });
            entryRegistration = entryInput.RegisterKeybind("open-config", inputSettings.OpenConfig, DtmInputScope.Title);
            api.Register(inputManifest, () => inputSettings = new InputSettings { ShowNativeTitleEntry = nativeTitleEntryDefault }, () => {
                runtime.Config.WriteConfig(inputManifest, inputSettings);
                entryRegistration?.Update(DtmKeybindList.Parse(inputSettings.OpenConfig), DtmInputScope.Title);
            }, titleScreenOnly: true);
            api.SetDisplayName(inputManifest, () => T("controller.title", "DTMAPI Settings"));
            defaults.AddKeybindOption(inputManifest, () => T("controller.entry", "Open Mod config"),
                () => T("controller.help", "Title only"), () => inputSettings.OpenConfig, v => inputSettings.OpenConfig = v, () => "F8");
            api.AddBoolOption(inputManifest, () => T("controller.nativeEntry", "Show Mod config in the main menu"),
                () => T("controller.nativeEntryHelp", "Save and restart the game to apply."),
                () => inputSettings.ShowNativeTitleEntry.Value, value => inputSettings.ShowNativeTitleEntry = value);
            ((IDtmConfigMenuRestartApi)api).RequireRestartForLastOption(inputManifest);
            api.AddBoolOption(inputManifest, () => T("controller.bgmAppend", "Append BGM tracks"),
                () => T("controller.bgmAppendHelp", "Append and deduplicate tracks in official order. When off, replace each group. Restart to apply."),
                () => inputSettings.AppendBgmGroups, value => inputSettings.AppendBgmGroups = value);
            ((IDtmConfigMenuRestartApi)api).RequireRestartForLastOption(inputManifest);
        }

        private bool ReadNativeTitleEntryDefault()
        {
            string path = Path.Combine(runtime.Paths.DtmApiPath, "install-state.json");
            try
            {
                return File.Exists(path) && JsonFile.Read<InputSettings>(path).ShowNativeTitleEntry == true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(inputManifest.UniqueID, "无法读取 Mod 配置入口的安装默认值。", "Path=" + path + "; Error=" + ex);
                return false;
            }
        }
        internal bool ReadStartupBgmAppend() => runtime.Config.ReadConfig<InputSettings>(inputManifest).AppendBgmGroups;

        private void SaveConfigPage(IConfigMenuPage page)
        {
            TryPageAction(page, T("config.saved", "Saved"), () => configMenu.Save(page.Manifest.UniqueID));
        }

        private bool TryShowRestartNoticeOnClose()
        {
            if (!configMenu.HasUnnotifiedRestartChanges()) return false;
            var dialog = new DtmConfigDialog(T("config.restartTitle", "Restart required"),
                T("config.restartBody", "Restart the game to apply:") + "\n\n" + string.Join("\n\n", configMenu.GetPendingRestartChanges()),
                new[] { new DtmConfigDialogButton("ok", T("dialog.ok", "Got it")) }, "ok", size: new DtmConfigDialogSize(480, 300));
            return ((IDtmConfigMenuDialogsApi)configMenu).TryShowDialog(inputManifest, dialog, _ => {
                configMenu.AcknowledgeRestartChanges();
                CloseOwnedRuntimeMenu();
            }, out _);
        }

        // RecordInputFrame has settled the snapshot; consume it before Update clears frame edges.
        public void ProcessEntryAction()
        {
            if (entryInput == null || inputOwnerClosed || runtime.UI.IsOpen || IsCapturingKey ||
                !runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase)) return;
            if (entryInput.WasKeybindPressed("open-config")) OpenFromTitleButton();
        }

        private void CloseInputOwner()
        {
            entryRegistration?.Dispose();
            entryRegistration = null;
            runtime.Input.RemoveOwner(inputManifest.UniqueID);
            runtime.RemoveRuntimeConfigOwner(inputManifest.UniqueID);
            configMenu.RemoveOwner(inputManifest.UniqueID);
            inputOwnerClosed = true;
            entryInput = null;
        }

        [DataContract]
        public sealed class InputSettings
        {
            [DataMember]
            public string OpenConfig { get; set; } = "F8";

            [DataMember]
            // An omitted choice follows the installed package's default.
            public bool? ShowNativeTitleEntry { get; set; }
            [DataMember]
            public bool AppendBgmGroups { get; set; } = true;

            // DataContractJsonSerializer skips initializers; present members override these defaults.
            [OnDeserializing]
            private void OnDeserializing(StreamingContext context)
            {
                OpenConfig = "F8";
                AppendBgmGroups = true;
            }
        }
    }
}
