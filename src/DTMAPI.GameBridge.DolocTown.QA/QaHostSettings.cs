using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class QaHostSettings
    {
        [DataMember(Name = "schemaVersion")]
        internal int SchemaVersion { get; set; }

        [DataMember(Name = "protocolVersion")]
        internal int ProtocolVersion { get; set; }

        [DataMember(Name = "runId")]
        internal string RunId { get; set; } = string.Empty;

        [DataMember(Name = "mode")]
        internal string Mode { get; set; } = string.Empty;

        [DataMember(Name = "Batch6AutoFishingPilot")]
        internal Batch6AutoFishingPilotSettings Batch6AutoFishingPilot { get; set; } = new Batch6AutoFishingPilotSettings();

        [DataMember(Name = "Batch6AutoFishingManagerLifecycle")]
        internal Batch6AutoFishingManagerLifecycleSettings Batch6AutoFishingManagerLifecycle { get; set; } = new Batch6AutoFishingManagerLifecycleSettings();

        [DataMember(Name = "Batch5GcLadderEnabled")]
        internal bool Batch5GcLadderEnabled { get; set; }

        [DataMember(Name = "Batch5GcLadderDomain")]
        internal string Batch5GcLadderDomain { get; set; } = "None";

        [DataMember(Name = "Batch5GcLadderLevel")]
        internal string Batch5GcLadderLevel { get; set; } = string.Empty;

        [DataMember(Name = "Batch5GcLadderWorkload")]
        internal string Batch5GcLadderWorkload { get; set; } = string.Empty;

        [DataMember(Name = "Batch5GcLadderMultiplier")]
        internal double Batch5GcLadderMultiplier { get; set; } = 1d;

        [DataMember(Name = "Batch5GcLadderMeasureSeconds")]
        internal int Batch5GcLadderMeasureSeconds { get; set; } = 600;

        [DataMember(Name = "Batch5GcLadderSampleSeconds")]
        internal int Batch5GcLadderSampleSeconds { get; set; } = 30;

        [DataMember(Name = "Batch5GcLadderTargetUnits")]
        internal int Batch5GcLadderTargetUnits { get; set; }

        [DataMember(Name = "Batch5NoDemandEnabled")]
        internal bool Batch5NoDemandEnabled { get; set; }

        [DataMember(Name = "Batch5NoDemandWarmupFrames")]
        internal int Batch5NoDemandWarmupFrames { get; set; }

        [DataMember(Name = "Batch5NoDemandTargetFrames")]
        internal int Batch5NoDemandTargetFrames { get; set; }

        [DataMember(Name = "CustomEntityContractEnabled")]
        internal bool CustomEntityContractEnabled { get; set; }

        [DataMember(Name = "FishRoeTooltipObservationEnabled")]
        internal bool FishRoeTooltipObservationEnabled { get; set; }

        [DataMember(Name = "DiagnosticsSnapshotEnabled")]
        internal bool DiagnosticsSnapshotEnabled { get; set; }

        [DataMember(Name = "DiagnosticsExpectedFeatureIds")]
        internal string[] DiagnosticsExpectedFeatureIds { get; set; } = Array.Empty<string>();

        [DataMember(Name = "DiagnosticsScenario")]
        internal string DiagnosticsScenario { get; set; } = "G3";

        [DataMember(Name = "ObserveSaveLoaded")]
        internal bool ObserveSaveLoaded { get; set; }

        [DataMember(Name = "ObserveSaveSaved")]
        internal bool ObserveSaveSaved { get; set; }

        [DataMember(Name = "ObserveWorkshopReloadCompleted")]
        internal bool ObserveWorkshopReloadCompleted { get; set; }

        [DataMember(Name = "TitleSettingsUiEnabled")]
        internal bool TitleSettingsUiEnabled { get; set; }

        [DataMember(Name = "ManagerStatusUiEnabled")]
        internal bool ManagerStatusUiEnabled { get; set; }

        [DataMember(Name = "ManagerMvpUiEnabled")]
        internal bool ManagerMvpUiEnabled { get; set; }

        [DataMember(Name = "OfficialModUiEnabled")]
        internal bool OfficialModUiEnabled { get; set; }

        [DataMember(Name = "PauseMenuLayoutEnabled")]
        internal bool PauseMenuLayoutEnabled { get; set; }

        [DataMember(Name = "DebugConsoleEnabled")]
        internal bool DebugConsoleEnabled { get; set; }

        [DataMember(Name = "SaveSlotsPagingEnabled")]
        internal bool SaveSlotsPagingEnabled { get; set; }

        [DataMember(Name = "SaveSlot")]
        internal int SaveSlot { get; set; }

        [DataMember(Name = "SaveTestMode")]
        internal string SaveTestMode { get; set; } =
            "NoNativeSave";

        [DataMember(Name = "DisposableSaveFixtureSaveRoot")]
        internal string DisposableSaveFixtureSaveRoot { get; set; } =
            string.Empty;

        [DataMember(Name = "RequireDisposableSaveRedirect")]
        internal bool RequireDisposableSaveRedirect { get; set; }

        [DataMember(Name = "AnimalObservationEnabled")]
        internal bool AnimalObservationEnabled { get; set; }

        [DataMember(Name = "EquipmentSlotsObservationEnabled")]
        internal bool EquipmentSlotsObservationEnabled { get; set; }

        [DataMember(Name = "AudioObservationEnabled")]
        internal bool AudioObservationEnabled { get; set; }

        [DataMember(Name = "HatchVoiceEnabled")]
        internal bool HatchVoiceEnabled { get; set; }

        [DataMember(Name = "CameraPlayableEnabled")]
        internal bool CameraPlayableEnabled { get; set; }

        [DataMember(Name = "ContentMetadataObservationEnabled")]
        internal bool ContentMetadataObservationEnabled { get; set; }

        [DataMember(Name = "ContentMetadataExpectOilAbsent")]
        internal bool ContentMetadataExpectOilAbsent { get; set; }

        [DataMember(Name = "ContinuousHomePageTerminalEnabled")]
        internal bool ContinuousHomePageTerminalEnabled { get; set; }

        [DataMember(Name = "ContinuousHomePageRequiresSaveLoaded")]
        internal bool ContinuousHomePageRequiresSaveLoaded { get; set; }

        [DataMember(Name = "ContinuousHomePageSeconds")]
        internal double ContinuousHomePageSeconds { get; set; } = 1.5d;

        [DataMember(Name = "TitleLifecycleEnabled")]
        internal bool TitleLifecycleEnabled { get; set; }

        [DataMember(Name = "ProductOwnerRefreshEnabled")]
        internal bool ProductOwnerRefreshEnabled { get; set; }

        [DataMember(Name = "AdvancedProductOwnerDeactivationEnabled")]
        internal bool AdvancedProductOwnerDeactivationEnabled { get; set; }

        [DataMember(Name = "AdvancedProductOwnerDeactivationOwnerIds")]
        internal string[] AdvancedProductOwnerDeactivationOwnerIds { get; set; } = Array.Empty<string>();

        [DataMember(Name = "ExternalPlayerInputObservationEnabled")]
        internal bool ExternalPlayerInputObservationEnabled { get; set; }

        [DataMember(Name = "ExternalPlayerInputToken")]
        internal string ExternalPlayerInputToken { get; set; } = string.Empty;

        [DataMember(Name = "ExternalPlayerInputMarkerPath")]
        internal string ExternalPlayerInputMarkerPath { get; set; } = string.Empty;

        [DataMember(Name = "ExternalPlayerInputEvidenceRoot")]
        internal string ExternalPlayerInputEvidenceRoot { get; set; } = string.Empty;

        [DataMember(Name = "G5WorldMutationCases")]
        internal string[] G5WorldMutationCases { get; set; } = Array.Empty<string>();

        [DataMember(Name = "DebugConsoleSaveAcceptancePhase")]
        internal string DebugConsoleSaveAcceptancePhase { get; set; } =
            "None";

        [DataMember(Name = "ExpectedDebugConsoleMoney")]
        internal int ExpectedDebugConsoleMoney { get; set; } = -1;

        [DataMember(Name = "ExpectedMoreEquipmentSlotsBackpackBaseline")]
        internal int ExpectedMoreEquipmentSlotsBackpackBaseline { get; set; } = -1;

        [DataMember(Name = "ExpectedMoreEquipmentSlotsCommittedGeneration")]
        internal long ExpectedMoreEquipmentSlotsCommittedGeneration { get; set; } = -1L;

        [DataMember(Name = "ExpectedMoreEquipmentSlotsCommittedOccupied")]
        internal int ExpectedMoreEquipmentSlotsCommittedOccupied { get; set; } = -1;

        [DataMember(Name = "ExpectedMoreEquipmentSlotsCommittedSlots")]
        internal string ExpectedMoreEquipmentSlotsCommittedSlots { get; set; } =
            string.Empty;

        [DataMember(Name = "ExpectedMoreEquipmentSlotsCandidatePreGeneration")]
        internal long ExpectedMoreEquipmentSlotsCandidatePreGeneration
        {
            get;
            set;
        } = -1L;

        [DataMember(Name = "MoreEquipmentSlotsInterruptedCandidateObservationEnabled")]
        internal bool
            MoreEquipmentSlotsInterruptedCandidateObservationEnabled
        {
            get;
            set;
        }

        [DataMember(Name = "MoreEquipmentSlotsTransitionPhase")]
        internal string MoreEquipmentSlotsTransitionPhase { get; set; } =
            "None";

        [DataMember(Name = "G6LifecycleCases")]
        internal string[] G6LifecycleCases { get; set; } = Array.Empty<string>();

        [DataMember(Name = "G6InitialDelaySeconds")]
        internal int G6InitialDelaySeconds { get; set; } = 3;

        [DataMember(Name = "G6SaveLoadCycleCount")]
        internal int G6SaveLoadCycleCount { get; set; }

        [DataMember(Name = "G6SaveLoadCycleInitialTitleIdleSeconds")]
        internal int G6SaveLoadCycleInitialTitleIdleSeconds { get; set; }

        [DataMember(Name = "G6SaveLoadCycleIntervalSeconds")]
        internal int G6SaveLoadCycleIntervalSeconds { get; set; } = 5;

        [DataMember(Name = "G6SaveLoadCycleInSaveSeconds")]
        internal int G6SaveLoadCycleInSaveSeconds { get; set; } = 5;

        [DataMember(Name = "G6PreLoadGcProbe")]
        internal bool G6PreLoadGcProbe { get; set; }

        [DataMember(Name = "G6SaveLoadObjectSnapshotMode")]
        internal string G6SaveLoadObjectSnapshotMode { get; set; } = "Full";

        [DataMember(Name = "G6RootIsolationProfile")]
        internal string G6RootIsolationProfile { get; set; } = "None";

        [DataMember(Name = "G6OwnerRootIsolationProfile")]
        internal string G6OwnerRootIsolationProfile { get; set; } = "None";

        [DataMember(Name = "G6NativeLoadContinuationProbe")]
        internal string G6NativeLoadContinuationProbe { get; set; } = "None";

        [DataMember(Name = "G6PendingPressureSeconds")]
        internal int G6PendingPressureSeconds { get; set; }

        [DataMember(Name = "G6PendingPressureIntervalSeconds")]
        internal double G6PendingPressureIntervalSeconds { get; set; } = 2d;

        [DataMember(Name = "G6LongTitleIdleSeconds")]
        internal int G6LongTitleIdleSeconds { get; set; }

        internal static QaHostSettings Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new InvalidDataException("QA host settings are empty.");

            using (var stream = new MemoryStream(bytes, writable: false))
            {
                var serializer = new DataContractJsonSerializer(typeof(QaHostSettings));
                var settings = serializer.ReadObject(stream) as QaHostSettings;
                return settings ?? throw new InvalidDataException("QA host settings did not contain an object.");
            }
        }

        internal void Validate(string expectedRunId)
        {
            Batch6AutoFishingPilot ??=
                new Batch6AutoFishingPilotSettings();
            Batch6AutoFishingManagerLifecycle ??=
                new Batch6AutoFishingManagerLifecycleSettings();
            if (SchemaVersion != QaHostProtocol.SchemaVersion)
                throw new InvalidDataException("Unsupported QA settings schemaVersion " + SchemaVersion + ".");
            if (ProtocolVersion != QaHostProtocol.ProtocolVersion)
                throw new InvalidDataException("Unsupported QA settings protocolVersion " + ProtocolVersion + ".");
            if (!string.Equals(RunId, expectedRunId, StringComparison.Ordinal))
                throw new InvalidDataException("QA settings runId does not match the activation receipt.");
            if (!string.Equals(Mode, QaHostProtocol.ParticipantOnlyMode, StringComparison.Ordinal))
                throw new InvalidDataException("QA settings require mode=participant-only.");
            DiagnosticsExpectedFeatureIds = (DiagnosticsExpectedFeatureIds ?? Array.Empty<string>())
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            DiagnosticsScenario = string.IsNullOrWhiteSpace(DiagnosticsScenario) ? "G3" : DiagnosticsScenario.Trim();
            AdvancedProductOwnerDeactivationOwnerIds = (AdvancedProductOwnerDeactivationOwnerIds ?? Array.Empty<string>())
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] supportedAdvancedOwnerIds =
            {
                "Yuuka.DTMAPI.ActionSpeed",
                "Yuuka.DTMAPI.OneActionComplete",
                "Yuuka.DTMAPI.FishBreedingAssistant",
                "Yuuka.DTMAPI.AnimalHusbandryProgress",
                "DTMAPI.MoreSavesMod",
                "DTMAPI.ChestLocatorEnhancerMod",
                "DTMAPI.MoreEquipmentSlotsMod",
                "DTMAPI.StrongPlantingGunMod",
                "DTMAPI.ZoomMod",
                "DTMAPI.MineMod",
                "DTMAPI.DebugConsoleMod"
            };
            if (AdvancedProductOwnerDeactivationOwnerIds.Any(ownerId =>
                !supportedAdvancedOwnerIds.Contains(ownerId, StringComparer.OrdinalIgnoreCase)))
                throw new InvalidDataException("QA Advanced owner deactivation contains an unsupported owner ID.");
            if (AdvancedProductOwnerDeactivationEnabled != (AdvancedProductOwnerDeactivationOwnerIds.Length > 0))
                throw new InvalidDataException("QA Advanced owner deactivation requires an explicit non-empty owner ID set exactly when enabled.");
            if (DiagnosticsSnapshotEnabled && DiagnosticsExpectedFeatureIds.Length == 0)
                throw new InvalidDataException("QA diagnostics snapshot requires at least one expected feature id.");
            int titleRouteCount = (TitleSettingsUiEnabled ? 1 : 0) +
                (ManagerStatusUiEnabled ? 1 : 0) +
                (ManagerMvpUiEnabled ? 1 : 0);
            if (titleRouteCount > 1)
                throw new InvalidDataException("QA title UI routes are mutually exclusive: choose basic settings, Manager Status, or Manager MVP.");
            if (ContinuousHomePageSeconds == 0d)
                ContinuousHomePageSeconds = 1.5d;
            if ((ContinuousHomePageTerminalEnabled || ExternalPlayerInputObservationEnabled) &&
                (ContinuousHomePageSeconds < 0.25d || ContinuousHomePageSeconds > 30d))
                throw new InvalidDataException("QA continuous HomePage terminal duration must be between 0.25 and 30 seconds.");
            if ((TitleLifecycleEnabled || ProductOwnerRefreshEnabled || AdvancedProductOwnerDeactivationEnabled) &&
                (!ContinuousHomePageTerminalEnabled || !ContinuousHomePageRequiresSaveLoaded || SaveSlot <= 0))
            {
                throw new InvalidDataException("QA title lifecycle, product-owner refresh, and final Advanced owner deactivation require a positive SaveSlot plus the post-save continuous HomePage terminal.");
            }
            if (SaveSlotsPagingEnabled && SaveSlot <= 0)
                throw new InvalidDataException("QA save-slot coordinator requires a positive SaveSlot.");
            if (EquipmentSlotsObservationEnabled && SaveSlot <= 0)
                throw new InvalidDataException("QA equipment-slot UI observation requires a positive SaveSlot.");
            SaveTestMode = string.IsNullOrWhiteSpace(SaveTestMode)
                ? "NoNativeSave"
                : SaveTestMode.Trim();
            DebugConsoleSaveAcceptancePhase =
                string.IsNullOrWhiteSpace(
                    DebugConsoleSaveAcceptancePhase)
                    ? "None"
                    : DebugConsoleSaveAcceptancePhase.Trim();
            MoreEquipmentSlotsTransitionPhase =
                string.IsNullOrWhiteSpace(
                    MoreEquipmentSlotsTransitionPhase)
                    ? "None"
                    : MoreEquipmentSlotsTransitionPhase.Trim();
            string[] supportedSaveTestModes =
            {
                "NoNativeSave",
                "NativeSaveExpected",
                "ArchiveMutation"
            };
            if (!supportedSaveTestModes.Contains(
                    SaveTestMode,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    "QA save-test mode is unsupported.");
            }
            bool saveRedirectMode =
                SaveTestMode.Equals(
                    "NativeSaveExpected",
                    StringComparison.Ordinal) ||
                SaveTestMode.Equals(
                    "ArchiveMutation",
                    StringComparison.Ordinal);
            if (saveRedirectMode &&
                !RequireDisposableSaveRedirect)
            {
                throw new InvalidDataException(
                    "QA native-save/archive tests require exactly one disposable save redirect.");
            }
            if (RequireDisposableSaveRedirect)
            {
                if (string.IsNullOrWhiteSpace(
                        DisposableSaveFixtureSaveRoot) ||
                    !Path.IsPathRooted(
                        DisposableSaveFixtureSaveRoot))
                {
                    throw new InvalidDataException(
                        "QA disposable save redirect requires an absolute fixture SAVE root.");
                }
                DisposableSaveFixtureSaveRoot =
                    Path.GetFullPath(
                        DisposableSaveFixtureSaveRoot)
                        .TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);
                if (!Directory.Exists(
                        DisposableSaveFixtureSaveRoot) ||
                    !Path.GetFileName(
                        DisposableSaveFixtureSaveRoot)
                        .Equals(
                            "SAVE",
                            StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "QA disposable save redirect must reference an existing directory named SAVE.");
                }
                string livePersistentRoot =
                    Path.GetFullPath(
                        Path.Combine(
                            Environment.GetFolderPath(
                                Environment.SpecialFolder
                                    .UserProfile),
                            "AppData",
                            "LocalLow",
                            "RedSawGames",
                            "DolocTown"))
                        .TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);
                string fixturePrefix =
                    DisposableSaveFixtureSaveRoot +
                    Path.DirectorySeparatorChar;
                string livePrefix =
                    livePersistentRoot +
                    Path.DirectorySeparatorChar;
                if (DisposableSaveFixtureSaveRoot.Equals(
                        livePersistentRoot,
                        StringComparison.OrdinalIgnoreCase) ||
                    fixturePrefix.StartsWith(
                        livePrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    livePrefix.StartsWith(
                        fixturePrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "QA disposable save redirect must remain outside the live Steam AutoCloud persistent-data tree.");
                }
            }
            if (Batch5NoDemandEnabled &&
                (SaveSlot <= 0 || Batch5NoDemandWarmupFrames <= 0 || Batch5NoDemandTargetFrames <= 0))
            {
                throw new InvalidDataException("QA Batch 5 no-demand measurement requires a positive SaveSlot, warm-up frame count, and target frame count.");
            }
            if (ExternalPlayerInputObservationEnabled &&
                (string.IsNullOrWhiteSpace(ExternalPlayerInputToken) ||
                 string.IsNullOrWhiteSpace(ExternalPlayerInputMarkerPath) ||
                 !Path.IsPathRooted(ExternalPlayerInputMarkerPath) ||
                 string.IsNullOrWhiteSpace(ExternalPlayerInputEvidenceRoot) ||
                 !Path.IsPathRooted(ExternalPlayerInputEvidenceRoot)))
            {
                throw new InvalidDataException("QA external player-input observation requires a token, absolute evidence root, and absolute marker path.");
            }
            string[] allowedG5Cases =
            {
                "ActionSpeedTool",
                "ActionSpeedConfigApply",
                "ActionSpeedInteraction",
                "ActionSpeedGcLadder",
                "OneActionResourceHit",
                "OneActionWrongTool",
                "OneActionFuelFeed",
                "OneActionVegetation",
                "NewContent",
                "OilOnly",
                "MineContent",
                "ChestLocatorEnhancer",
                "ZoomProductNative",
                "MoreEquipmentSlots",
                "MoreEquipmentSlotsCommittedShieldSetup",
                "MoreEquipmentSlotsCommittedShieldDamageNoNativeSave",
                "MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave",
                "MoreEquipmentSlotsNoNativeSave",
                "MoreEquipmentSlotsNoNativeSaveColdObserver",
                "MoreEquipmentSlotsTransition",
                "StrongPlantingGun",
                "CropHarvestingApi",
                "InstantSave",
                "DebugConsoleSaveAcceptance",
                "DebugInventory",
                "DebugWeather",
                "DebugTeleport",
                "DebugTime",
                "DebugMovement",
                "AdvancedDebug"
            };
            G5WorldMutationCases = (G5WorldMutationCases ?? Array.Empty<string>())
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] unknownG5Cases = G5WorldMutationCases
                .Where(item => !allowedG5Cases.Contains(item, StringComparer.Ordinal))
                .ToArray();
            if (unknownG5Cases.Length > 0)
                throw new InvalidDataException("Unsupported G5 world-mutation case(s): " + string.Join(",", unknownG5Cases) + ".");
            if (G5WorldMutationCases.Length > 0 && SaveSlot <= 0)
                throw new InvalidDataException("QA G5 world-mutation cases require a positive SaveSlot.");
            bool debugConsoleSaveAcceptance =
                G5WorldMutationCases.Contains(
                    "DebugConsoleSaveAcceptance",
                    StringComparer.Ordinal);
            string[] allowedDebugConsoleSavePhases =
            {
                "FailedMutation",
                "SuccessfulMutation",
                "ColdObserve"
            };
            if (debugConsoleSaveAcceptance &&
                !allowedDebugConsoleSavePhases.Contains(
                    DebugConsoleSaveAcceptancePhase,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    "QA DebugConsole save acceptance requires an explicit FailedMutation, SuccessfulMutation, or ColdObserve phase.");
            }
            if (!debugConsoleSaveAcceptance &&
                !DebugConsoleSaveAcceptancePhase.Equals(
                    "None",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "QA DebugConsole save acceptance phase requires its exact G5 case.");
            }
            if (debugConsoleSaveAcceptance &&
                (!RequireDisposableSaveRedirect ||
                 SaveSlot != 3 ||
                 (DebugConsoleSaveAcceptancePhase.Equals(
                      "ColdObserve",
                      StringComparison.Ordinal)
                      ? !SaveTestMode.Equals(
                          "NoNativeSave",
                          StringComparison.Ordinal)
                      : !SaveTestMode.Equals(
                          "NativeSaveExpected",
                          StringComparison.Ordinal)) ||
                 (DebugConsoleSaveAcceptancePhase.Equals(
                      "ColdObserve",
                      StringComparison.Ordinal) &&
                  ExpectedDebugConsoleMoney < 0)))
            {
                throw new InvalidDataException(
                    "QA DebugConsole save acceptance requires the disposable redirect and SaveSlot=3; mutation phases require NativeSaveExpected, while ColdObserve requires NoNativeSave and exact expected money.");
            }
            if (G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsCommittedShieldSetup",
                    StringComparer.Ordinal) &&
                (!SaveTestMode.Equals(
                     "NativeSaveExpected",
                     StringComparison.Ordinal) ||
                 !RequireDisposableSaveRedirect ||
                 SaveSlot != 3))
            {
                throw new InvalidDataException(
                    "QA MoreEquipmentSlotsCommittedShieldSetup requires SaveTestMode=NativeSaveExpected, the disposable redirect, and SaveSlot=3.");
            }
            bool committedShieldRollback =
                G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsCommittedShieldDamageNoNativeSave",
                    StringComparer.Ordinal) ||
                G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave",
                    StringComparer.Ordinal);
            if (committedShieldRollback &&
                (!SaveTestMode.Equals(
                    "NoNativeSave",
                    StringComparison.Ordinal) ||
                 !RequireDisposableSaveRedirect ||
                 SaveSlot != 3 ||
                 !TitleLifecycleEnabled ||
                 ExpectedMoreEquipmentSlotsBackpackBaseline < 0 ||
                 ExpectedMoreEquipmentSlotsCommittedGeneration < 0 ||
                 ExpectedMoreEquipmentSlotsCommittedOccupied < 0 ||
                 string.IsNullOrWhiteSpace(
                     ExpectedMoreEquipmentSlotsCommittedSlots)))
            {
                throw new InvalidDataException(
                    "QA committed-shield rollback requires NoNativeSave, the same disposable redirect, SaveSlot=3, title lifecycle, and the exact setup baseline.");
            }
            if (G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsNoNativeSave",
                    StringComparer.Ordinal) &&
                (!SaveTestMode.Equals(
                     "NoNativeSave",
                     StringComparison.Ordinal) ||
                 SaveSlot != 3 ||
                 !TitleLifecycleEnabled))
            {
                throw new InvalidDataException(
                    "QA MoreEquipmentSlotsNoNativeSave requires SaveTestMode=NoNativeSave, SaveSlot=3, and TitleLifecycleEnabled.");
            }
            if (G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsNoNativeSaveColdObserver",
                    StringComparer.Ordinal))
            {
                if (!SaveTestMode.Equals(
                        "NoNativeSave",
                        StringComparison.Ordinal) ||
                    SaveSlot != 3 ||
                    ExpectedMoreEquipmentSlotsBackpackBaseline < 0 ||
                    ExpectedMoreEquipmentSlotsCommittedGeneration < 0 ||
                    ExpectedMoreEquipmentSlotsCommittedOccupied < 0 ||
                    string.IsNullOrWhiteSpace(
                        ExpectedMoreEquipmentSlotsCommittedSlots))
                {
                    throw new InvalidDataException(
                    "QA MoreEquipmentSlotsNoNativeSaveColdObserver requires SaveTestMode=NoNativeSave, SaveSlot=3, and the complete first-process expected state.");
                }
            }
            if (MoreEquipmentSlotsInterruptedCandidateObservationEnabled)
            {
                if (ExpectedMoreEquipmentSlotsCandidatePreGeneration ==
                        long.MaxValue ||
                    !SaveTestMode.Equals(
                        "NativeSaveExpected",
                        StringComparison.Ordinal) ||
                    !RequireDisposableSaveRedirect ||
                    SaveSlot != 3 ||
                    !G5WorldMutationCases.Contains(
                        "MoreEquipmentSlots",
                        StringComparer.Ordinal) ||
                    ExpectedMoreEquipmentSlotsCommittedOccupied < 0 ||
                    string.IsNullOrWhiteSpace(
                        ExpectedMoreEquipmentSlotsCommittedSlots))
                {
                    throw new InvalidDataException(
                        "QA MoreEquipmentSlots interrupted-candidate observation requires NativeSaveExpected, the disposable redirect, SaveSlot=3, the existing MoreEquipmentSlots case, and exact pre-generation/Committed expectations.");
                }
            }
            bool moreEquipmentSlotsTransition =
                G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsTransition",
                    StringComparer.Ordinal);
            string[] allowedMoreEquipmentSlotsTransitionPhases =
            {
                "U1",
                "Prepare",
                "U3Backpack",
                "U3Mail",
                "U4",
                "MigratedSave"
            };
            if (moreEquipmentSlotsTransition &&
                (!allowedMoreEquipmentSlotsTransitionPhases.Contains(
                     MoreEquipmentSlotsTransitionPhase,
                     StringComparer.Ordinal) ||
                 !RequireDisposableSaveRedirect ||
                 SaveSlot != 3 ||
                 !TitleLifecycleEnabled ||
                 (MoreEquipmentSlotsTransitionPhase.Equals(
                     "U4",
                      StringComparison.Ordinal)
                      ? !SaveTestMode.Equals(
                          "NoNativeSave",
                          StringComparison.Ordinal)
                      : !SaveTestMode.Equals(
                          "NativeSaveExpected",
                          StringComparison.Ordinal))))
            {
                throw new InvalidDataException(
                    "QA MoreEquipmentSlots transition acceptance requires an explicit U1/Prepare/U3Backpack/U3Mail/U4/MigratedSave phase, the disposable redirect, SaveSlot=3, title lifecycle, NativeSaveExpected for mutation phases, and NoNativeSave for U4.");
            }
            if (moreEquipmentSlotsTransition &&
                MoreEquipmentSlotsTransitionPhase.Equals(
                    "MigratedSave",
                    StringComparison.Ordinal) &&
                (ExpectedMoreEquipmentSlotsBackpackBaseline < 0 ||
                 ExpectedMoreEquipmentSlotsCommittedGeneration < 0 ||
                 ExpectedMoreEquipmentSlotsCommittedOccupied < 0 ||
                 string.IsNullOrWhiteSpace(
                     ExpectedMoreEquipmentSlotsCommittedSlots)))
            {
                throw new InvalidDataException(
                    "QA MoreEquipmentSlots MigratedSave requires the exact migrated committed baseline.");
            }
            if (!moreEquipmentSlotsTransition &&
                !MoreEquipmentSlotsTransitionPhase.Equals(
                    "None",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "QA MoreEquipmentSlots transition phase requires its exact G5 case.");
            }
            if (EquipmentSlotsObservationEnabled &&
                (!moreEquipmentSlotsTransition ||
                 !MoreEquipmentSlotsTransitionPhase.Equals(
                     "U1",
                     StringComparison.Ordinal)) &&
                G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsTransition",
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    "QA transition equipment UI observation is permitted only for U1.");
            }
            if (G5WorldMutationCases.Contains("NewContent", StringComparer.Ordinal) &&
                (G5WorldMutationCases.Contains("OilOnly", StringComparer.Ordinal) || G5WorldMutationCases.Contains("MineContent", StringComparer.Ordinal)))
            {
                throw new InvalidDataException("QA G5 NewContent is mutually exclusive with OilOnly and MineContent in one participant transaction.");
            }
            if (EquipmentSlotsObservationEnabled && G5WorldMutationCases.Contains("NewContent", StringComparer.Ordinal))
                throw new InvalidDataException("QA G4 equipment-slot UI observation and G5 NewContent must run as separate participant transactions.");
            string[] allowedG6Cases =
            {
                "ModOwnerLifetime",
                "LegacyFishingCompatibility",
                Batch6AutoFishingPilotSettings.CaseId,
                Batch6AutoFishingManagerLifecycleSettings.CaseId,
                "SaveLoadCycle",
                "SaveLoadPendingPressure",
                "LongTitleLoad"
            };
            G6LifecycleCases = (G6LifecycleCases ?? Array.Empty<string>())
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] unknownG6Cases = G6LifecycleCases
                .Where(item => !allowedG6Cases.Contains(item, StringComparer.Ordinal))
                .ToArray();
            if (unknownG6Cases.Length > 0)
                throw new InvalidDataException("Unsupported G6 lifecycle case(s): " + string.Join(",", unknownG6Cases) + ".");
            if (G6LifecycleCases.Length > 0 && SaveSlot <= 0)
                throw new InvalidDataException("QA G6 lifecycle cases require a positive SaveSlot.");
            bool batch6AutoFishingSelected = G6LifecycleCases.Contains(Batch6AutoFishingPilotSettings.CaseId, StringComparer.Ordinal);
            if (batch6AutoFishingSelected && G6LifecycleCases.Length != 1)
                throw new InvalidDataException("Batch6AutoFishingPilot must be the single G6 lifecycle case in its QA transaction.");
            Batch6AutoFishingPilot = Batch6AutoFishingPilot ?? new Batch6AutoFishingPilotSettings();
            Batch6AutoFishingPilot.NormalizeAndValidate(batch6AutoFishingSelected, SaveSlot);
            bool batch6AutoFishingManagerSelected = G6LifecycleCases.Contains(Batch6AutoFishingManagerLifecycleSettings.CaseId, StringComparer.Ordinal);
            if (batch6AutoFishingManagerSelected && G6LifecycleCases.Length != 1)
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle must be the single G6 lifecycle case in its QA transaction.");
            Batch6AutoFishingManagerLifecycle = Batch6AutoFishingManagerLifecycle ?? new Batch6AutoFishingManagerLifecycleSettings();
            Batch6AutoFishingManagerLifecycle.NormalizeAndValidate(batch6AutoFishingManagerSelected, SaveSlot);
            int exclusiveSaveLoadCases = (G6LifecycleCases.Contains("SaveLoadCycle", StringComparer.Ordinal) ? 1 : 0) +
                (G6LifecycleCases.Contains("SaveLoadPendingPressure", StringComparer.Ordinal) ? 1 : 0) +
                (G6LifecycleCases.Contains("LongTitleLoad", StringComparer.Ordinal) ? 1 : 0);
            if (exclusiveSaveLoadCases > 1)
                throw new InvalidDataException("QA G6 save/load, pending-pressure, and long-title routes are mutually exclusive.");
            if (G6SaveLoadCycleIntervalSeconds == 0)
                G6SaveLoadCycleIntervalSeconds = 5;
            if (G6SaveLoadCycleInSaveSeconds == 0)
                G6SaveLoadCycleInSaveSeconds = 5;
            if (G6PendingPressureIntervalSeconds == 0d)
                G6PendingPressureIntervalSeconds = 2d;
            if (G6InitialDelaySeconds < 0 || G6SaveLoadCycleCount < 0 || G6SaveLoadCycleInitialTitleIdleSeconds < 0 ||
                G6SaveLoadCycleIntervalSeconds <= 0 || G6SaveLoadCycleInSaveSeconds <= 0 || G6PendingPressureSeconds < 0 ||
                G6PendingPressureIntervalSeconds <= 0d || G6LongTitleIdleSeconds < 0)
                throw new InvalidDataException("QA G6 timing values are invalid.");
            if (G6PreLoadGcProbe && !G6LifecycleCases.Contains("SaveLoadCycle", StringComparer.Ordinal))
                throw new InvalidDataException("QA G6 pre-load forced-GC probe requires SaveLoadCycle.");
            G6RootIsolationProfile = string.IsNullOrWhiteSpace(G6RootIsolationProfile) ? "None" : G6RootIsolationProfile.Trim();
            G6NativeLoadContinuationProbe = string.IsNullOrWhiteSpace(G6NativeLoadContinuationProbe) ? "None" : G6NativeLoadContinuationProbe.Trim();
            G6OwnerRootIsolationProfile = string.IsNullOrWhiteSpace(G6OwnerRootIsolationProfile) ? "None" : G6OwnerRootIsolationProfile.Trim();
            if (!new[] { "None", "UiRuntime" }.Contains(G6RootIsolationProfile ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported QA G6 root isolation profile " + G6RootIsolationProfile + ".");
            if (!new[] { "None", "VersionPatcher" }.Contains(G6NativeLoadContinuationProbe ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported QA G6 native-load continuation probe " + G6NativeLoadContinuationProbe + ".");
            G6RootIsolationProfile = string.Equals(G6RootIsolationProfile, "UiRuntime", StringComparison.OrdinalIgnoreCase) ? "UiRuntime" : "None";
            G6NativeLoadContinuationProbe = string.Equals(G6NativeLoadContinuationProbe, "VersionPatcher", StringComparison.OrdinalIgnoreCase) ? "VersionPatcher" : "None";
            if (EquipmentSlotsObservationEnabled && string.Equals(G6RootIsolationProfile, "UiRuntime", StringComparison.Ordinal))
                throw new InvalidDataException("QA G4 equipment-slot UI observation requires the production equipment UI Runtime and cannot use G6 UiRuntime isolation.");

            Batch5GcLadderDomain = string.IsNullOrWhiteSpace(Batch5GcLadderDomain) ? "None" : Batch5GcLadderDomain.Trim();
            Batch5GcLadderLevel = (Batch5GcLadderLevel ?? string.Empty).Trim().ToUpperInvariant();
            Batch5GcLadderWorkload = (Batch5GcLadderWorkload ?? string.Empty).Trim();
            if (Batch5GcLadderEnabled)
            {
                if (batch6AutoFishingSelected || batch6AutoFishingManagerSelected)
                    throw new InvalidDataException("Batch6 AutoFishing product QA owns its transaction and cannot share the generic Batch 5 GC ladder transaction.");
                bool actionSpeed = Batch5GcLadderDomain.Equals("ActionSpeed", StringComparison.OrdinalIgnoreCase);
                if (!actionSpeed)
                    throw new InvalidDataException("Generic GameBridge QA supports only the ActionSpeed GC ladder domain.");
                if (!new[] { "L0", "L1", "L2", "L3", "L4", "L5" }.Contains(Batch5GcLadderLevel, StringComparer.Ordinal))
                    throw new InvalidDataException("Batch 5 GC ladder level must be L0 through L5.");
                if (SaveSlot != 3)
                    throw new InvalidDataException("ActionSpeed GC ladder requires save slot 3.");
                if (string.IsNullOrWhiteSpace(Batch5GcLadderWorkload))
                    throw new InvalidDataException("Batch 5 GC ladder workload is required.");
                string[] actionSpeedWorkloads = { "Tool", "Interact", "Eat", "ContinuousUse" };
                if (actionSpeed && !actionSpeedWorkloads.Contains(Batch5GcLadderWorkload, StringComparer.Ordinal))
                    throw new InvalidDataException("ActionSpeed GC ladder workload must be Tool, Interact, Eat, or ContinuousUse.");
                if (double.IsNaN(Batch5GcLadderMultiplier) || double.IsInfinity(Batch5GcLadderMultiplier) || Batch5GcLadderMultiplier < 1d || Batch5GcLadderMultiplier > 4d)
                    throw new InvalidDataException("Batch 5 GC ladder multiplier must be between 1 and 4.");
                if (Batch5GcLadderMeasureSeconds <= 0 || Batch5GcLadderSampleSeconds <= 0 || Batch5GcLadderSampleSeconds > Batch5GcLadderMeasureSeconds)
                    throw new InvalidDataException("Batch 5 GC ladder sampling durations are invalid.");
                if (Batch5GcLadderTargetUnits <= 0)
                    throw new InvalidDataException("Batch 5 GC ladder target units must be positive.");
                if (actionSpeed && !G5WorldMutationCases.Contains("ActionSpeedGcLadder", StringComparer.Ordinal))
                    throw new InvalidDataException("ActionSpeed GC ladder requires the ActionSpeedGcLadder G5 case.");
                Batch5GcLadderDomain = "ActionSpeed";
            }
            else
            {
                Batch5GcLadderDomain = "None";
                Batch5GcLadderLevel = string.Empty;
                Batch5GcLadderWorkload = string.Empty;
            }
        }
    }
}
