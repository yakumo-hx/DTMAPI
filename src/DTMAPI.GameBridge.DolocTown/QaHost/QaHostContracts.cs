using System;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class QaHostProtocol
    {
        internal const int SchemaVersion = 1;
        internal const int ProtocolVersion = 7;
        internal const string AssemblySimpleName = "DTMAPI.GameBridge.DolocTown.QA";
        internal const string FactoryTypeName = "DTMAPI.GameBridge.DolocTown.QA.QaHostFactory";
        internal const string ParticipantOnlyMode = "participant-only";
    }

    internal sealed class QaHostPreparationContext
    {
        internal QaHostPreparationContext(
            DtmApiRuntime runtime,
            string runId,
            string evidenceRoot,
            byte[] settingsBytes)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            RunId = runId ?? throw new ArgumentNullException(nameof(runId));
            EvidenceRoot = evidenceRoot ?? throw new ArgumentNullException(nameof(evidenceRoot));
            SettingsBytes = settingsBytes == null ? throw new ArgumentNullException(nameof(settingsBytes)) : (byte[])settingsBytes.Clone();
        }

        internal DtmApiRuntime Runtime { get; }

        internal string RunId { get; }

        internal string EvidenceRoot { get; }

        internal byte[] SettingsBytes { get; }
    }

    internal sealed class GameBridgeFixtureStartupOptions
    {
        internal static readonly GameBridgeFixtureStartupOptions None = new GameBridgeFixtureStartupOptions(string.Empty);

        internal GameBridgeFixtureStartupOptions(
            string runId,
            int saveSlot = 0,
            bool disableEquipmentSlotsRuntime = false,
            string[]? disabledFeatureIds = null)
        {
            RunId = runId ?? string.Empty;
            SaveSlot = saveSlot;
            DisableEquipmentSlotsRuntime = disableEquipmentSlotsRuntime;
            DisabledFeatureIds = disabledFeatureIds == null ? Array.Empty<string>() : (string[])disabledFeatureIds.Clone();
        }

        internal string RunId { get; }

        internal int SaveSlot { get; }

        internal bool DisableEquipmentSlotsRuntime { get; }

        internal string[] DisabledFeatureIds { get; }
    }

    internal sealed class PreparedQaHost
    {
        internal PreparedQaHost(
            IQaHostFactory factory,
            QaHostPreparationContext preparationContext,
            GameBridgeFixtureStartupOptions startupOptions)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            PreparationContext = preparationContext ?? throw new ArgumentNullException(nameof(preparationContext));
            StartupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
        }

        internal IQaHostFactory Factory { get; }

        internal QaHostPreparationContext PreparationContext { get; }

        internal GameBridgeFixtureStartupOptions StartupOptions { get; }
    }

    internal interface IQaHostFactory
    {
        int ProtocolVersion { get; }

        string SupportedRuntimeReleaseVersion { get; }

        GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context);

        IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access);
    }

    internal interface IQaHostParticipant
    {
        string Id { get; }

        void Start();

        void Update();

        QaHostRunDisposition GetRunDisposition();

        void OnSaveLoaded(int? slot, bool isNewGame);

        void OnSaveSaved(int? slot);

        void OnWorkshopReloadCompleted();

        QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded);

        void Close(string reason);
    }

    internal interface IQaHostNativeDriver
    {
        bool TryRequestInitialSaveLoad(int saveSlot);

        void ContinueInitialSaveLoad();

        void OnUiObservation(string source);
    }

    internal enum QaHostBoundaryDisposition
    {
        Continue = 0,
        Close = 1
    }

    internal enum QaHostRunDisposition
    {
        Continue = 0,
        RequestInitialSaveLoad = 1,
        RequestReturnHome = 2,
        RequestQuit = 3
    }
}
