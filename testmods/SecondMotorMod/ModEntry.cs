using System;
using System.Linq;
using DTMAPI.Abstractions;

namespace SecondMotorMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string VehicleId = "dtmapi.second_motor";
        private const string KeyItemId = "dtmapi_second_motor_key";
        private const string RequiredSourceId = "Local.DTMAPI_SecondMotor";

        private IDtmHelper helper = null!;
        private IMotorVehicleApi? vehicleApi;
        private IMailDeliveryApi? mailApi;
        private MailItemDeliveryResult? lastMailResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            if (!CanRunOfficialFeatures("Entry", updateMailResult: false))
            {
                helper.Monitor.Log(T("mod.disabled", "SecondMotor official content source is disabled or missing; ordinary vehicle/mail behavior is skipped."));
                return;
            }

            RegisterConfigMenu();
            BindVehicleApi("Entry");
            BindMailApi("Entry");
            helper.Events.Save.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Monitor.Log(T("mod.loaded", "Alternate flying motor example loaded; after a save loads, DTMAPI will try to deliver the key through native mail if needed."));
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!CanRunOfficialFeatures("SaveLoaded", updateMailResult: true))
                return;

            BindVehicleApi("SaveLoaded");
            BindMailApi("SaveLoaded");
            TryDeliverKeyMail("SaveLoaded");
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            helper.Monitor.Log(T("mod.returnedTitle", "Second Motor returned to title boundary observed."));
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
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.mail", "Key delivery"));
            menu.AddParagraph(helper.ModManifest, BuildMailStatusText);
            helper.Monitor.Log(T("config.registered", "SecondMotor config/status page registered."));
        }

        private void BindVehicleApi(string reason)
        {
            if (!CanRunOfficialFeatures("BindVehicleApi " + reason, updateMailResult: false))
                return;

            vehicleApi = helper.ModRegistry.GetApi<IMotorVehicleApi>("DTMAPI.GameBridge.DolocTown");
            if (vehicleApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Motor vehicle API is not available yet."), LogLevel.Warn);
                return;
            }

            vehicleApi.VehicleChanged -= OnVehicleChanged;
            vehicleApi.VehicleChanged += OnVehicleChanged;
            MotorVehicleRegisterResult result = vehicleApi.RegisterSecondMotor(helper.ModManifest, new SecondMotorOptions
            {
                VehicleId = VehicleId,
                DisplayName = T("vehicle.name", "Alternate Flying Motor"),
                KeyItemId = KeyItemId,
                SpeedMultiplier = 2,
                UseOriginalMotorVisuals = false,
                TextureSourceNote = "GameBridge applies an instance-scoped tint to the cloned motor; global sprite_vehicle_motor replacements are not installed.",
                VerboseLogging = true
            });
            helper.Monitor.Log((result.Success ? T("mod.registered", "Second Motor registered.") : T("mod.registerFailed", "Second Motor registration failed.")) + " reason=" + reason + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void BindMailApi(string reason)
        {
            if (!CanRunOfficialFeatures("BindMailApi " + reason, updateMailResult: true))
                return;

            mailApi = helper.ModRegistry.GetApi<IMailDeliveryApi>("DTMAPI.GameBridge.DolocTown");
            if (mailApi == null)
            {
                helper.Monitor.Log(T("mail.apiMissing", "Mail delivery API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }
            helper.Monitor.Log(T("mail.apiReady", "Mail delivery API is available.") + " reason=" + reason);
        }

        private void TryDeliverKeyMail(string reason)
        {
            if (!CanRunOfficialFeatures("TryDeliverKeyMail " + reason, updateMailResult: true))
                return;

            if (mailApi == null)
            {
                helper.Monitor.Log(T("mail.apiMissing", "Mail delivery API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastMailResult = mailApi.SendItemMail(helper.ModManifest, new MailItemDeliveryRequest
            {
                ItemId = KeyItemId,
                Count = 1,
                EmailName = "dtmapi_second_motor_key_delivery",
                Sender = T("mail.sender", "DTMAPI"),
                Content = T("mail.content", "Your alternate flying motor key is attached."),
                TemplateName = "send_item_template",
                SkipIfAlreadyOwned = true,
                PreventDuplicatePendingMail = true,
                RequireEnabledContentSource = true,
                RequiredSourceId = RequiredSourceId
            });
            string logPrefix = lastMailResult.Success
                ? T("mail.result", "Alternate flying motor key delivery checked.")
                : T("mail.failed", "Alternate flying motor key delivery failed.");
            helper.Monitor.Log(logPrefix + " reason=" + reason + " message=" + lastMailResult.Message, lastMailResult.Success ? LogLevel.Info : LogLevel.Warn);
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
            return string.Format(T("config.vehicleKeyStatus", "Key={0}."), KeyItemId);
        }

        private string BuildMailStatusText()
        {
            if (lastMailResult == null)
                return T("config.mailPending", "Key mail will be checked after a save loads.");

            return string.Format(
                T("config.mailStatus", "Mail: success={0}, sent={1}, skipped={2}, backpack={3}, pending={4}."),
                lastMailResult.Success,
                lastMailResult.Sent,
                lastMailResult.Skipped,
                lastMailResult.BackpackCount,
                lastMailResult.PendingMailCount);
        }

        private bool CanRunOfficialFeatures(string reason, bool updateMailResult)
        {
            IWorkshopModInfo? self = helper.Workshop.GetDtmApiMods()
                .FirstOrDefault(mod => mod.UniqueID.Equals(helper.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase));
            if (self != null && !self.IsEnabledByOfficialPath)
                return OfficialFeatureBlocked("source-disabled", "SecondMotor is disabled by Doloc Town official Mod UI or Steam Workshop enablement. reason=" + reason, updateMailResult);

            IContentItemInfo? keyItem = helper.Content.GetIndexedItem(KeyItemId);
            if (keyItem == null)
                return OfficialFeatureBlocked("missing-key-content", "SecondMotor key item is not indexed from official content. reason=" + reason, updateMailResult);
            if (!keyItem.Enabled)
                return OfficialFeatureBlocked("key-source-disabled", "SecondMotor key item source is disabled. source=" + keyItem.SourceId + " reason=" + reason, updateMailResult);
            if (!keyItem.SourceId.Equals(RequiredSourceId, StringComparison.OrdinalIgnoreCase))
                return OfficialFeatureBlocked("unexpected-key-source", "SecondMotor key item source is " + keyItem.SourceId + ", expected " + RequiredSourceId + ". reason=" + reason, updateMailResult);

            return true;
        }

        private bool OfficialFeatureBlocked(string failureReason, string message, bool updateMailResult)
        {
            helper.Monitor.LogOnce("secondmotor-official-block-" + failureReason, message, LogLevel.Warn);
            if (updateMailResult)
            {
                lastMailResult = new MailItemDeliveryResult
                {
                    Success = false,
                    Skipped = true,
                    ItemId = KeyItemId,
                    SourceId = RequiredSourceId,
                    FailureReason = failureReason,
                    Message = message
                };
            }
            return false;
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
    }
}
