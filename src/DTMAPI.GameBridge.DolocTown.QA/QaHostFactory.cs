using System;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class QaHostFactory : IQaHostFactory
    {
        private QaHostSettings? settings;

        public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

        public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

        public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (settings != null)
                throw new InvalidOperationException("QA host startup options may be prepared only once.");

            settings = QaHostSettings.Read(context.SettingsBytes);
            settings.Validate(context.RunId);
            bool isolateUiRuntime = string.Equals(settings.G6RootIsolationProfile, "UiRuntime", StringComparison.OrdinalIgnoreCase);
            return new GameBridgeFixtureStartupOptions(
                runId: context.RunId,
                saveSlot: settings.SaveSlot,
                disableEquipmentSlotsRuntime: isolateUiRuntime,
                disabledFeatureIds: isolateUiRuntime ? new[] { "Camera", "SaveSlots" } : Array.Empty<string>());
        }

        public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access)
        {
            if (settings == null)
                throw new InvalidOperationException("PrepareStartupOptions must run before CreateParticipant.");
            return new QaHostParticipant(access, settings);
        }
    }
}
