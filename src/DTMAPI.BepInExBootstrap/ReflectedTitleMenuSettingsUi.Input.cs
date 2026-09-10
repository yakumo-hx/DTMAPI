using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
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
        private readonly ManifestModel inputManifest = new ManifestModel {
            UniqueID = "DTMAPI.TitleSettings", Name = "DTMAPI Input", Author = "DTMAPI", Version = DTMAPI.Core.Runtime.DtmApiRuntime.ApiVersion, Type = "RuntimeApi"
        };

        private void InitializeInputSettings()
        {
            if (inputSettingsRegistered || inputOwnerClosed || !(configMenu is IDtmConfigMenuKeybindDefaultsApi defaults) || !(configMenu is IDtmConfigMenuApi api)) return;
            inputSettingsRegistered = true;
            runtime.RegisterRuntimeConfigOwner(inputManifest.UniqueID);
            inputSettings = runtime.Config.ReadConfig<InputSettings>(inputManifest);
            nativeTitleEntryEnabledAtStartup = inputSettings.ShowNativeTitleEntry;
            entryInput = runtime.Input.CreateOwnerBound(inputManifest.UniqueID, () => {
                if (inputOwnerClosed) throw new ObjectDisposedException(inputManifest.UniqueID);
            });
            entryRegistration = entryInput.RegisterKeybind("open-config", inputSettings.OpenConfig, DtmInputScope.Title);
            api.Register(inputManifest, () => inputSettings = new InputSettings(), () => {
                runtime.Config.WriteConfig(inputManifest, inputSettings);
                entryRegistration?.Update(DtmKeybindList.Parse(inputSettings.OpenConfig), DtmInputScope.Title);
            }, titleScreenOnly: true);
            api.SetDisplayName(inputManifest, () => T("controller.title", "Input settings"));
            defaults.AddKeybindOption(inputManifest, () => T("controller.entry", "Open Mod config"),
                () => T("controller.help", "Title only"), () => inputSettings.OpenConfig, v => inputSettings.OpenConfig = v, () => "F8");
            api.AddBoolOption(inputManifest, () => T("controller.nativeEntry", "Show Mod config in the main menu"),
                () => T("controller.nativeEntryHelp", "Off by default. Save and restart the game to apply."),
                () => inputSettings.ShowNativeTitleEntry, value => inputSettings.ShowNativeTitleEntry = value);
            api.AddParagraph(inputManifest, () => T("controller.nativeEntryHelp", "Off by default. Save and restart the game to apply."));
            api.AddParagraph(inputManifest, () => {
                ControllerButtonAdapter.Refresh(ReflectedUnityInput.GetUnityFrameCount());
                return ControllerButtonAdapter.Available ? T("controller.connected", "Gamepad detected") : T("controller.absent", "No Gamepad detected");
            });
            api.AddParagraph(inputManifest, () => T("controller.help", "Title only"));
            api.AddParagraph(inputManifest, () => T("controller.limits", "Axis and trigger bindings unsupported."));
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
            public bool ShowNativeTitleEntry { get; set; }
        }
    }
}
