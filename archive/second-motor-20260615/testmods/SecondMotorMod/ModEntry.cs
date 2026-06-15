using System;
using System.Linq;
using DTMAPI.Abstractions;

namespace SecondMotorMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string VehicleId = "dtmapi.second_motor";
        private const string KeyItemId = "dtmapi_second_motor_key";
        private const string LocalSourceId = "Local.DTMAPI_SecondMotor";
        private const string AppearanceAssetRelativePath = "Content/DTMAPI/assets/second-motor/dtmapi_second_motor.png";

        private IDtmHelper helper = null!;
        private IMotorVehicleApi? vehicleApi;
        private MotorVehicleRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            if (!CanRunOfficialFeatures("Entry"))
            {
                helper.Monitor.Log(T("mod.disabled", "SecondMotor official content source is disabled or missing; ordinary vehicle behavior is skipped."));
                return;
            }

            RegisterConfigMenu();
            BindVehicleApi("Entry");
            helper.Events.Save.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Monitor.Log(T("mod.loaded", "Alternate flying motor loaded; buy its key from the official phone booth or spawn the key for smoke testing."));
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!CanRunOfficialFeatures("SaveLoaded"))
                return;

            BindVehicleApi("SaveLoaded");
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            helper.Monitor.Log(T("mod.returnedTitle", "Alternate flying motor returned to title boundary observed."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log(T("config.apiMissing", "DTMAPI config menu API is not available yet."), LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "Alternate Flying Motor"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.vehicle", "Vehicle status"));
            menu.AddParagraph(helper.ModManifest, BuildVehicleBridgeStatusText);
            menu.AddParagraph(helper.ModManifest, BuildVehicleFlagStatusText);
            menu.AddParagraph(helper.ModManifest, BuildVehicleLocationStatusText);
            menu.AddParagraph(helper.ModManifest, BuildVehicleKeyStatusText);
            menu.AddParagraph(helper.ModManifest, BuildVehicleAppearanceStatusText);
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.acquisition", "Key acquisition"));
            menu.AddParagraph(helper.ModManifest, () => T("config.acquisitionStatus", "The key is added to the official phone booth store by mod_tbmodstoreextension.json. DTMAPI does not mail this key."));
            helper.Monitor.Log(T("config.registered", "SecondMotor config/status page registered."));
        }

        private void BindVehicleApi(string reason)
        {
            if (!CanRunOfficialFeatures("BindVehicleApi " + reason))
                return;

            vehicleApi = helper.ModRegistry.GetApi<IMotorVehicleApi>("DTMAPI.GameBridge.DolocTown");
            if (vehicleApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Motor vehicle API is not available yet."), LogLevel.Warn);
                return;
            }

            vehicleApi.VehicleChanged -= OnVehicleChanged;
            vehicleApi.VehicleChanged += OnVehicleChanged;
            lastRegisterResult = vehicleApi.RegisterCustomMotor(helper.ModManifest, new CustomMotorDefinition
            {
                VehicleId = VehicleId,
                DisplayName = T("vehicle.name", "Alternate Flying Motor"),
                PrimaryKeyItemId = KeyItemId,
                KeyItemIds = new[] { KeyItemId },
                SpeedMultiplier = 1,
                MovementMode = "native-flying-motor",
                CollisionProfile = "native-motor",
                AppearanceMode = "scoped-sprite",
                AppearanceAssetRelativePath = AppearanceAssetRelativePath,
                TextureSourceNote = "Official Workshop vehicle example texture is copied into private DTMAPI-named assets and applied only to the DTMAPI clone.",
                VerboseLogging = true
            });
            helper.Monitor.Log((lastRegisterResult.Success ? T("mod.registered", "Alternate flying motor registered.") : T("mod.registerFailed", "Alternate flying motor registration failed.")) + " reason=" + reason + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void OnVehicleChanged(object? sender, MotorVehicleEventArgs e)
        {
            if (!e.VehicleId.Equals(VehicleId, StringComparison.OrdinalIgnoreCase))
                return;
            helper.Monitor.Log("SecondMotor event=" + e.EventType + " room=" + e.State.RoomId + " riding=" + e.State.IsRiding + " speed=" + e.State.SpeedMultiplier.ToString("0.###") + " message=" + e.Message);
        }

        private string BuildVehicleBridgeStatusText()
        {
            if (vehicleApi == null)
                return T("config.vehicleMissing", "Vehicle bridge is not available.");

            BridgeFeatureStatus bridgeStatus = vehicleApi.GetStatus(helper.ModManifest.UniqueID);
            return string.Format(T("config.vehicleBridgeStatus", "Bridge: {0}."), bridgeStatus.Status);
        }

        private string BuildVehicleFlagStatusText()
        {
            if (vehicleApi == null)
                return T("config.vehicleMissing", "Vehicle bridge is not available.");

            MotorVehicleState state = vehicleApi.GetVehicleState(VehicleId);
            return string.Format(T("config.vehicleFlagStatus", "Registered={0}, visible={1}, riding={2}."), state.IsRegistered, state.IsVisible, state.IsRiding);
        }

        private string BuildVehicleLocationStatusText()
        {
            if (vehicleApi == null)
                return T("config.vehicleMissing", "Vehicle bridge is not available.");

            MotorVehicleState state = vehicleApi.GetVehicleState(VehicleId);
            string room = string.IsNullOrWhiteSpace(state.RoomTitle) ? state.RoomId : state.RoomTitle;
            if (string.IsNullOrWhiteSpace(room))
                room = T("config.none", "none");
            return string.Format(T("config.vehicleLocationStatus", "Room={0}, speed={1}."), room, state.SpeedMultiplier.ToString("0.###"));
        }

        private string BuildVehicleKeyStatusText()
        {
            return string.Format(T("config.vehicleKeyStatus", "Key={0}; sold at the official phone booth."), KeyItemId);
        }

        private string BuildVehicleAppearanceStatusText()
        {
            if (vehicleApi == null)
                return T("config.vehicleMissing", "Vehicle bridge is not available.");

            MotorVehicleState state = vehicleApi.GetVehicleState(VehicleId);
            string registerStatus = lastRegisterResult == null ? "pending" : (lastRegisterResult.Success ? "registered" : "failed:" + lastRegisterResult.FailureReason);
            return string.Format(T("config.vehicleAppearanceStatus", "Appearance={0}; {1}; registration={2}."), state.AppearanceMode, state.AppearanceSummary, registerStatus);
        }

        private bool CanRunOfficialFeatures(string reason)
        {
            IWorkshopModInfo? self = helper.Workshop.GetDtmApiMods()
                .FirstOrDefault(mod => mod.UniqueID.Equals(helper.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase));
            if (self != null && !self.IsEnabledByOfficialPath)
                return OfficialFeatureBlocked("source-disabled", "SecondMotor is disabled by Doloc Town official Mod UI or Steam Workshop enablement. reason=" + reason);

            IContentItemInfo? keyItem = helper.Content.GetIndexedItem(KeyItemId);
            if (keyItem == null)
                return OfficialFeatureBlocked("missing-key-content", "SecondMotor key item is not indexed from official content. reason=" + reason);
            if (!keyItem.Enabled)
                return OfficialFeatureBlocked("key-source-disabled", "SecondMotor key item source is disabled. source=" + keyItem.SourceId + " reason=" + reason);
            if (!IsExpectedKeySource(keyItem.SourceId))
                return OfficialFeatureBlocked("unexpected-key-source", "SecondMotor key item source is " + keyItem.SourceId + ", expected the local DTMAPI_SecondMotor package or a Workshop source. reason=" + reason);

            return true;
        }

        private static bool IsExpectedKeySource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return false;
            if (sourceId.Equals(LocalSourceId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (sourceId.StartsWith("Workshop.", StringComparison.OrdinalIgnoreCase) ||
                sourceId.StartsWith("SteamWorkshop.", StringComparison.OrdinalIgnoreCase))
                return true;
            return sourceId.IndexOf("DTMAPI_SecondMotor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool OfficialFeatureBlocked(string failureReason, string message)
        {
            helper.Monitor.LogOnce("secondmotor-official-block-" + failureReason, message, LogLevel.Warn);
            lastRegisterResult = new MotorVehicleRegisterResult
            {
                Success = false,
                VehicleId = VehicleId,
                KeyItemId = KeyItemId,
                KeyItemIds = new[] { KeyItemId },
                FailureReason = failureReason,
                Message = message
            };
            return false;
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
    }
}
