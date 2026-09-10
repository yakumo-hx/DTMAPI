using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

namespace DTMAPI.AbiCompatibilityHarness
{
    internal static partial class Program
    {
        private const string AbstractionsAssemblyName = "DTMAPI.Abstractions";
        private const string FishingOptionsTypeName = "DTMAPI.Abstractions.FishingAutomationOptions";
        private const string GameBridgeProviderUniqueId = "DTMAPI.GameBridge.DolocTown";
        private const string MandatoryGameBridgeAssemblyName = "DTMAPI.GameBridge.DolocTown";
        private const string CompatibilityHostAssemblyName = "DTMAPI.GameBridge.DolocTown.Compatibility";
        private const string CompatibilityHostFactoryTypeName = "DTMAPI.GameBridge.DolocTown.CompatibilityHostFactory";
        private const string CompatibilityHostFactoryMethodName = "Create";
        private const string CurrentRuntimeAssemblyVersion = "0.5.3.0";
        private const string NetStandard20TargetFramework = ".NETStandard,Version=v2.0";
        private const string StopOnManualMovePropertyName = "StopOnManualMove";
        private const string StopOnManualMoveSetterName = "set_StopOnManualMove";
        private static readonly string[] RetainedLampTypeNames =
        {
            "DTMAPI.Abstractions.ILampControlApi",
            "DTMAPI.Abstractions.LampManualToggleOptions",
            "DTMAPI.Abstractions.LampManualToggleRegisterResult",
            "DTMAPI.Abstractions.LampManualToggleState"
        };
        private static readonly string[] FrozenCompatibilityTypeNames =
        {
            "DTMAPI.Abstractions.IActionCompletionApi",
            "DTMAPI.Abstractions.ActionCompletionOptions",
            "DTMAPI.Abstractions.IFishingAutomationApi",
            "DTMAPI.Abstractions.FishingBiteWaitMode",
            "DTMAPI.Abstractions.FishingResultMode",
            "DTMAPI.Abstractions.FishingAnimationMode",
            "DTMAPI.Abstractions.FishingAutomationOptions",
            "DTMAPI.Abstractions.FishingAutomationState",
            "DTMAPI.Abstractions.IActionSpeedApi",
            "DTMAPI.Abstractions.ActionSpeedOptions",
            "DTMAPI.Abstractions.IItemTooltipApi",
            "DTMAPI.Abstractions.FishRoeTooltipOptions",
            "DTMAPI.Abstractions.FishRoeDisplayInfo",
            "DTMAPI.Abstractions.IAnimalViewerApi",
            "DTMAPI.Abstractions.AnimalHusbandryProgressOptions",
            "DTMAPI.Abstractions.IMachineProductionApi",
            "DTMAPI.Abstractions.MachineDefinition",
            "DTMAPI.Abstractions.MachineRecipeInput",
            "DTMAPI.Abstractions.MachineOutputRule",
            "DTMAPI.Abstractions.MachineRegisterResult",
            "DTMAPI.Abstractions.MachineProductionState",
            "DTMAPI.Abstractions.IEquipmentSlotsApi",
            "DTMAPI.Abstractions.EquipmentSlotsOptions",
            "DTMAPI.Abstractions.EquipmentSlotsRegisterResult",
            "DTMAPI.Abstractions.EquipmentSlotsState",
            "DTMAPI.Abstractions.EquipmentSlotInfo",
            "DTMAPI.Abstractions.EquipmentSlotEquipResult",
            "DTMAPI.Abstractions.EquipmentSlotsRecoveryResult",
            "DTMAPI.Abstractions.ISaveSlotsApi",
            "DTMAPI.Abstractions.SaveSlotsOptions",
            "DTMAPI.Abstractions.SaveSlotsRegisterResult",
            "DTMAPI.Abstractions.SaveSlotsState",
            "DTMAPI.Abstractions.ICameraViewApi",
            "DTMAPI.Abstractions.ICameraViewLease",
            "DTMAPI.Abstractions.CameraViewRequest",
            "DTMAPI.Abstractions.CameraViewResult",
            "DTMAPI.Abstractions.CameraViewState",
            "DTMAPI.Abstractions.ICameraZoomApi",
            "DTMAPI.Abstractions.CameraZoomOptions",
            "DTMAPI.Abstractions.CameraZoomRegisterResult",
            "DTMAPI.Abstractions.CameraZoomResult",
            "DTMAPI.Abstractions.CameraZoomState",
            "DTMAPI.Abstractions.IChestLocatorEnhancerApi",
            "DTMAPI.Abstractions.ChestLocatorEnhancerOptions",
            "DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult",
            "DTMAPI.Abstractions.ChestLocatorEnhancerState",
            "DTMAPI.Abstractions.IStrongPlantingGunApi",
            "DTMAPI.Abstractions.StrongPlantingGunOptions",
            "DTMAPI.Abstractions.StrongPlantingGunRegisterResult",
            "DTMAPI.Abstractions.StrongPlantingGunState",
            "DTMAPI.Abstractions.ICustomAnimalApi",
            "DTMAPI.Abstractions.ICustomMonsterApi",
            "DTMAPI.Abstractions.ICustomAttackApi",
            "DTMAPI.Abstractions.ICustomDroneApi"
        };
        private static readonly string[] RetainedDebugCompatibilityTypeNames =
        {
            "DTMAPI.Abstractions.IDebugConsoleApi",
            "DTMAPI.Abstractions.IInventoryDebugApi",
            "DTMAPI.Abstractions.IWeatherDebugApi",
            "DTMAPI.Abstractions.ITeleportDebugApi",
            "DTMAPI.Abstractions.IInstantSaveDebugApi",
            "DTMAPI.Abstractions.ITimeDebugApi",
            "DTMAPI.Abstractions.IMovementDebugApi",
            "DTMAPI.Abstractions.IAdvancedDebugApi",
            "DTMAPI.Abstractions.AdvancedTimeAdvanceKind",
            "DTMAPI.Abstractions.InventoryDebugQuery",
            "DTMAPI.Abstractions.InventoryDebugPage",
            "DTMAPI.Abstractions.InventoryDebugSourceGroup",
            "DTMAPI.Abstractions.InventoryDebugItem",
            "DTMAPI.Abstractions.InventoryGiveResult",
            "DTMAPI.Abstractions.WeatherDebugState",
            "DTMAPI.Abstractions.WeatherDebugOption",
            "DTMAPI.Abstractions.WeatherSetResult",
            "DTMAPI.Abstractions.TeleportDestination",
            "DTMAPI.Abstractions.TeleportSnapshot",
            "DTMAPI.Abstractions.TeleportResult",
            "DTMAPI.Abstractions.InstantSaveDebugState",
            "DTMAPI.Abstractions.InstantSaveDebugResult",
            "DTMAPI.Abstractions.TimeDebugState",
            "DTMAPI.Abstractions.TimeSkipResult",
            "DTMAPI.Abstractions.TimeScaleDebugResult",
            "DTMAPI.Abstractions.DebugValueResult",
            "DTMAPI.Abstractions.DebugCommandResult",
            "DTMAPI.Abstractions.CropMaturityResult",
            "DTMAPI.Abstractions.CreativeModeState",
            "DTMAPI.Abstractions.CreativeModeResult",
            "DTMAPI.Abstractions.TechPointDebugOption",
            "DTMAPI.Abstractions.SpawnDebugOption",
            "DTMAPI.Abstractions.SpawnDebugResult",
            "DTMAPI.Abstractions.MovementDebugState",
            "DTMAPI.Abstractions.MovementSpeedResult"
        };
        private static readonly HashSet<string> AuthorizedRetiredPublicApiEntries =
            new HashSet<string>(StringComparer.Ordinal)
            {
                // The exact old Y-console has no remaining users. Update
                // 20260831-0001 authorizes only this unused CSV member/DTO
                // surface; every other public deletion must still fail.
                "M|public|instance|DTMAPI.Abstractions.ITeleportDebugApi|ExportDestinationsCsv|(DTMAPI.Abstractions.IManifest)|DTMAPI.Abstractions.TeleportCsvExportResult",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|.ctor|()|System.Void",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|get_FailureReason|()|System.String",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|get_Message|()|System.String",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|get_Path|()|System.String",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|get_RowCount|()|System.Int32",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|get_Success|()|System.Boolean",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|set_FailureReason|(System.String)|System.Void",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|set_Message|(System.String)|System.Void",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|set_Path|(System.String)|System.Void",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|set_RowCount|(System.Int32)|System.Void",
                "M|public|instance|DTMAPI.Abstractions.TeleportCsvExportResult|set_Success|(System.Boolean)|System.Void",
                "P|DTMAPI.Abstractions.TeleportCsvExportResult|FailureReason|()|System.String",
                "P|DTMAPI.Abstractions.TeleportCsvExportResult|Message|()|System.String",
                "P|DTMAPI.Abstractions.TeleportCsvExportResult|Path|()|System.String",
                "P|DTMAPI.Abstractions.TeleportCsvExportResult|RowCount|()|System.Int32",
                "P|DTMAPI.Abstractions.TeleportCsvExportResult|Success|()|System.Boolean",
                "TB|DTMAPI.Abstractions.TeleportCsvExportResult|System.Object",
                "TK|DTMAPI.Abstractions.TeleportCsvExportResult|sealed-class",
                "T|DTMAPI.Abstractions.TeleportCsvExportResult"
            };
        private static readonly KnownCompatibilityConsumerContract[] KnownCompatibilityConsumers =
        {
            new(
                "action-speed",
                "3169DB47BA7686B124F2BC907FED89605DF5B1BD17703C33566A147DD8D7B13A",
                "DTMAPI.Abstractions.IActionSpeedApi",
                new[]
                {
                    "DTMAPI.Abstractions.IActionSpeedApi::Configure",
                    "DTMAPI.Abstractions.IActionSpeedApi::GetStatus",
                    "DTMAPI.Abstractions.ActionSpeedOptions::set_Enabled"
                }),
            new(
                "one-action-complete",
                "612B173653EB22E3EB46C7882D97BB1117D308A70EF00773C8031E79DF5626E6",
                "DTMAPI.Abstractions.IActionCompletionApi",
                new[]
                {
                    "DTMAPI.Abstractions.IActionCompletionApi::Configure",
                    "DTMAPI.Abstractions.IActionCompletionApi::GetStatus",
                    "DTMAPI.Abstractions.ActionCompletionOptions::set_Enabled"
                }),
            new(
                "fish-roe-info",
                "128EE6AD1893A8C98C0A6BC879E398371350B555E7BA47CE5D54AEE248031BEA",
                "DTMAPI.Abstractions.IItemTooltipApi",
                new[]
                {
                    "DTMAPI.Abstractions.IItemTooltipApi::ConfigureFishRoeProvider",
                    "DTMAPI.Abstractions.IItemTooltipApi::GetStatus",
                    "DTMAPI.Abstractions.FishRoeTooltipOptions::set_Enabled"
                }),
            new(
                "animal-husbandry-progress",
                "E5DAA3B385AD30E9671FD9BC0407534A66390F28B327358183C65D9F68DA7530",
                "DTMAPI.Abstractions.IAnimalViewerApi",
                new[]
                {
                    "DTMAPI.Abstractions.IAnimalViewerApi::ConfigureSpecialProduceProgress",
                    "DTMAPI.Abstractions.IAnimalViewerApi::GetStatus",
                    "DTMAPI.Abstractions.AnimalHusbandryProgressOptions::set_Enabled"
                }),
            new(
                "auto-fishing",
                "E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA",
                "DTMAPI.Abstractions.IFishingAutomationApi",
                new[]
                {
                    "DTMAPI.Abstractions.IFishingAutomationApi::Configure",
                    "DTMAPI.Abstractions.IFishingAutomationApi::SetEnabled",
                    "DTMAPI.Abstractions.IFishingAutomationApi::GetState",
                    "DTMAPI.Abstractions.IFishingAutomationApi::GetStatus",
                    "DTMAPI.Abstractions.FishingAutomationOptions::set_StopOnManualMove"
                }),
            new(
                "more-saves",
                "F9CB4CE42BBECF7541C963B600440C65007E0C9F088C4414046658539862226E",
                "DTMAPI.Abstractions.ISaveSlotsApi",
                new[]
                {
                    "DTMAPI.Abstractions.ISaveSlotsApi::RegisterSlots",
                    "DTMAPI.Abstractions.ISaveSlotsApi::GetState",
                    "DTMAPI.Abstractions.SaveSlotsOptions::set_Enabled",
                    "DTMAPI.Abstractions.SaveSlotsOptions::set_SlotCount",
                    "DTMAPI.Abstractions.SaveSlotsOptions::set_VerboseLogging"
                }),
            new(
                "more-equipment-slots",
                "092807CC5C5DB359B40D5325EDD6EBC6860238E51F7F65014F5B39FBC0CF71ED",
                "DTMAPI.Abstractions.IEquipmentSlotsApi",
                new[]
                {
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::.ctor",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_AutoRecoverOnMissingMod",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_Enabled",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_ExtraAttributeSlots",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_ExtraSlotsAffectVisuals",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_PreserveVanillaVisualSlots",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_SafeUnequipOnDisable",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_SlotIdPrefix",
                    "DTMAPI.Abstractions.EquipmentSlotsOptions::set_VerboseLogging",
                    "DTMAPI.Abstractions.EquipmentSlotsRegisterResult::get_Message",
                    "DTMAPI.Abstractions.EquipmentSlotsRegisterResult::get_Success",
                    "DTMAPI.Abstractions.IEquipmentSlotsApi::RegisterSlots",
                }),
            new(
                "chest-locator-enhancer",
                "125704135FFF47778993B268F89A90E911B7757D14938B731AB91CCC7BCDE2CE",
                "DTMAPI.Abstractions.IChestLocatorEnhancerApi",
                new[]
                {
                    "DTMAPI.Abstractions.IChestLocatorEnhancerApi::Register",
                    "DTMAPI.Abstractions.IChestLocatorEnhancerApi::GetState",
                    "DTMAPI.Abstractions.ChestLocatorEnhancerOptions::set_Enabled",
                    "DTMAPI.Abstractions.ChestLocatorEnhancerOptions::set_IncludeSharedCases",
                    "DTMAPI.Abstractions.ChestLocatorEnhancerOptions::set_IncludeSharedStorageShelfBoxes",
                    "DTMAPI.Abstractions.ChestLocatorEnhancerOptions::set_RespectNativeAutoUseBoxSetting"
                }),
            new(
                "zoom",
                "DFA74BDD3561A9E9647FEC01AB9B48F095826D2A752AE920566BE2C5063971B3",
                "DTMAPI.Abstractions.ICameraViewApi",
                new[]
                {
                    "DTMAPI.Abstractions.CameraViewRequest::.ctor",
                    "DTMAPI.Abstractions.CameraViewRequest::set_Enabled",
                    "DTMAPI.Abstractions.CameraViewRequest::set_LeaseName",
                    "DTMAPI.Abstractions.CameraViewRequest::set_MaxViewScale",
                    "DTMAPI.Abstractions.CameraViewRequest::set_MinViewScale",
                    "DTMAPI.Abstractions.CameraViewRequest::set_Priority",
                    "DTMAPI.Abstractions.CameraViewRequest::set_Step",
                    "DTMAPI.Abstractions.CameraViewRequest::set_VerboseLogging",
                    "DTMAPI.Abstractions.CameraViewRequest::set_ViewScale",
                    "DTMAPI.Abstractions.CameraViewResult::get_AfterViewScale",
                    "DTMAPI.Abstractions.CameraViewResult::get_AppliedViewScale",
                    "DTMAPI.Abstractions.CameraViewResult::get_BeforeViewScale",
                    "DTMAPI.Abstractions.CameraViewResult::get_Message",
                    "DTMAPI.Abstractions.CameraViewResult::get_Success",
                    "DTMAPI.Abstractions.CameraViewState::get_ActiveOwnerId",
                    "DTMAPI.Abstractions.CameraViewState::get_AppliedOrthographicSize",
                    "DTMAPI.Abstractions.CameraViewState::get_AppliedViewScale",
                    "DTMAPI.Abstractions.CameraViewState::get_ArbitrationStatus",
                    "DTMAPI.Abstractions.CameraViewState::get_CameraAvailable",
                    "DTMAPI.Abstractions.CameraViewState::get_CameraOwnerStatus",
                    "DTMAPI.Abstractions.CameraViewState::get_CurrentViewScale",
                    "DTMAPI.Abstractions.CameraViewState::get_LastMessage",
                    "DTMAPI.Abstractions.CameraViewState::get_LifecycleStatus",
                    "DTMAPI.Abstractions.CameraViewState::get_MaxViewScale",
                    "DTMAPI.Abstractions.CameraViewState::get_MinViewScale",
                    "DTMAPI.Abstractions.CameraViewState::get_NativeRefreshStatus",
                    "DTMAPI.Abstractions.CameraViewState::get_Status",
                    "DTMAPI.Abstractions.CameraViewState::get_VanillaOrthographicSize",
                    "DTMAPI.Abstractions.ICameraViewApi::AcquireLease",
                    "DTMAPI.Abstractions.ICameraViewApi::GetState",
                    "DTMAPI.Abstractions.ICameraViewLease::GetState",
                    "DTMAPI.Abstractions.ICameraViewLease::SetViewScale",
                    "DTMAPI.Abstractions.ICameraViewLease::Update",
                    "DTMAPI.Abstractions.ICameraViewLease::get_IsReleased",
                    "DTMAPI.Abstractions.ICameraViewLease::get_LastResult"
                }),
            new(
                "y-console",
                "E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E",
                "DTMAPI.Abstractions.IDebugConsoleApi",
                new[]
                {
                    "DTMAPI.Abstractions.IDebugConsoleApi::Bind",
                    "DTMAPI.Abstractions.IDebugConsoleApi::BindAdvanced",
                    "DTMAPI.Abstractions.IDebugConsoleApi::Close",
                    "DTMAPI.Abstractions.IDebugConsoleApi::SetLanguage",
                    "DTMAPI.Abstractions.IDebugConsoleApi::Toggle",
                    "DTMAPI.Abstractions.IDebugConsoleApi::get_IsOpen",
                    "DTMAPI.Abstractions.IMovementDebugApi::ResetSpeed"
                })
        };
        private static readonly KnownExternalConsumerContract[] KnownExternalConsumers =
        {
            new(
                "3743621104",
                "DolocStorageExpansionMod.dll",
                "45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366",
                "0.5.1.0"),
            new(
                "3743644065",
                "DolocStoreCapacityMod.dll",
                "BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF",
                "0.5.1.0"),
            new(
                "3754869009",
                "DolocTownQoL.dll",
                "FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10",
                "0.5.2.0"),
            new(
                "3759797170",
                "Mxx_DolocTownMod_Installer.dll",
                "F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D",
                "0.5.2.0")
        };
        private static readonly string[] HeavyExecutorMarkerTypeNames =
        {
            "DTMAPI.GameBridge.DolocTown.AnimalViewerService+AnimalProgressInfo",
            "DTMAPI.GameBridge.DolocTown.AnimalViewerService+AnimalProgressRenderRow",
            "DTMAPI.GameBridge.DolocTown.FishingCompatibilityInputOverride",
            "DTMAPI.GameBridge.DolocTown.FishingCompatibilityLifecyclePublicationGate",
            "DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService+FishingMiniGameInputStats",
            "DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService+FishingAutomationFailureState",
            "DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService+FishingAutomationDiagnosticState",
            "DTMAPI.GameBridge.DolocTown.ChestLocatorEnhancerCompatibilityService+EffectiveChestLocatorPolicy",
            "DTMAPI.DebugConsole.CompatibilityDebugActionService",
            "DTMAPI.DebugConsole.DebugConsoleNativeActions"
        };
        private static readonly string[] CompatibilityServiceTypeNames =
        {
            "DTMAPI.GameBridge.DolocTown.ActionCompletionService",
            "DTMAPI.GameBridge.DolocTown.ActionSpeedService",
            "DTMAPI.GameBridge.DolocTown.FishRoeTooltipService",
            "DTMAPI.GameBridge.DolocTown.AnimalViewerService",
            "DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService"
        };
        private const string ChestLocatorMandatoryProxyType =
            "DTMAPI.GameBridge.DolocTown.ChestLocatorEnhancerService";
        private const string ChestLocatorHostExecutorType =
            "DTMAPI.GameBridge.DolocTown.ChestLocatorEnhancerCompatibilityService";
        private const string DebugActionMandatoryProxyType =
            "DTMAPI.GameBridge.DolocTown.DebugActionCompatibilityProxy";
        private const string DebugActionHostExecutorType =
            "DTMAPI.DebugConsole.CompatibilityDebugActionService";

        private static int Main(string[] args)
        {
            try
            {
                IReadOnlyDictionary<string, string> options = ParseArguments(args);
                if (options.ContainsKey("helper-implementer"))
                    return RunHelperAbi(options);
                if (options.ContainsKey("synthetic-contract"))
                    return RunSyntheticContract(options);
                string baselinePath = RequireFile(options, "baseline-abstractions");
                string candidatePath = RequireFile(options, "candidate-abstractions");
                string consumerPath = RequireFile(options, "consumer");
                string expectedConsumerSha256 = RequireOption(options, "expected-consumer-sha256").ToUpperInvariant();
                string expectedBaselineSha256 = RequireOption(options, "expected-baseline-sha256").ToUpperInvariant();
                string expectedBaselineVersion = RequireOption(options, "expected-baseline-version");
                string expectedCandidateVersion = RequireOption(options, "expected-candidate-version");
                string expectedConsumerReferenceVersion = RequireOption(options, "expected-consumer-reference-version");
                string? reportPath = GetOptionalPath(options, "report");

                string baselineSha256 = ComputeSha256(baselinePath);
                string candidateSha256 = ComputeSha256(candidatePath);
                string consumerSha256 = ComputeSha256(consumerPath);
                RequireEqual(expectedBaselineSha256, baselineSha256, "retained baseline SHA-256");
                RequireEqual(expectedConsumerSha256, consumerSha256, "retained consumer SHA-256");

                AssemblySnapshot baseline = ReadPublicSurface(baselinePath);
                AssemblySnapshot candidate = ReadPublicSurface(candidatePath);
                RequireEqual(AbstractionsAssemblyName, baseline.Name, "baseline assembly name");
                RequireEqual(AbstractionsAssemblyName, candidate.Name, "candidate assembly name");
                RequireEqual(expectedBaselineVersion, baseline.AssemblyVersion, "baseline assembly version");
                RequireEqual(expectedCandidateVersion, candidate.AssemblyVersion, "candidate assembly version");

                string[] removed = baseline.PublicSurface.Except(candidate.PublicSurface, StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                string[] authorizedRemoved = removed
                    .Where(AuthorizedRetiredPublicApiEntries.Contains)
                    .ToArray();
                string[] unexpectedRemoved = removed
                    .Where(value => !AuthorizedRetiredPublicApiEntries.Contains(value))
                    .ToArray();
                MemberReferenceInfo[] setterReferences = FindStopOnManualMoveSetterReferences(consumerPath);
                if (setterReferences.Length == 0)
                {
                    throw new InvalidOperationException(
                        "The retained consumer metadata does not reference FishingAutomationOptions.set_StopOnManualMove.");
                }

                ConsumerBindingResult binding = LoadConsumerAgainstCandidate(
                    consumerPath,
                    candidatePath,
                    expectedConsumerReferenceVersion,
                    expectedCandidateVersion,
                    setterReferences);

                PublicProductScanResult[] publicProducts = ScanRetainedPublicProducts(
                    options,
                    candidatePath,
                    expectedCandidateVersion);
                int publicProductLampMemberReferenceCount = publicProducts.Sum(product => product.LampMemberReferences.Length);
                ExternalConsumerScanResult[] externalConsumers =
                    ScanRetainedExternalConsumers(options, candidatePath, expectedCandidateVersion);
                CompatibilityHostMetadataReport? compatibilityHost = ValidateCompatibilityHostArtifacts(options);

                var report = new AbiGateReport(
                    SchemaVersion: 4,
                    OverallPass: unexpectedRemoved.Length == 0,
                    RuntimeValidation: "NotRunDotNet8HostOnly",
                    Baseline: new ArtifactReport(
                        Path.GetFileName(baselinePath), baselineSha256, baseline.AssemblyVersion, baseline.FileVersion, baseline.PublicSurface.Count),
                    Candidate: new ArtifactReport(
                        Path.GetFileName(candidatePath), candidateSha256, candidate.AssemblyVersion, candidate.FileVersion, candidate.PublicSurface.Count),
                    Consumer: new ConsumerReport(
                        Path.GetFileName(consumerPath), consumerSha256, binding.ConsumerReferencedAbstractionsVersion,
                        setterReferences.Length, binding.ResolvedSetterReferenceCount, binding.ConsumerTypeCount),
                    RemovedPublicApiCount: removed.Length,
                    RemovedPublicApi: removed,
                    RetainedLampTypeNames: RetainedLampTypeNames,
                    RetainedPublicProductCount: publicProducts.Length,
                    RetainedPublicProductLampMemberReferenceCount: publicProductLampMemberReferenceCount,
                    RetainedPublicProducts: publicProducts,
                    RetainedExternalConsumerCount: externalConsumers.Length,
                    RetainedExternalConsumers: externalConsumers,
                    StopOnManualMove: new StopOnManualMoveReport(
                        Getter: true, Setter: true, DefaultValue: true, RoundTrip: true, ObsoleteIsError: false),
                    LampCompatibility: binding.LampCompatibility,
                    CompatibilityHost: compatibilityHost);
                if (reportPath != null)
                    WriteReport(reportPath, report);

                Console.WriteLine("BaselineAbstractionsPath=" + baselinePath);
                Console.WriteLine("BaselineAbstractionsSha256=" + baselineSha256);
                Console.WriteLine("BaselineAbstractionsAssemblyVersion=" + baseline.AssemblyVersion);
                Console.WriteLine("BaselineAbstractionsFileVersion=" + baseline.FileVersion);
                Console.WriteLine("BaselinePublicApiCount=" + baseline.PublicSurface.Count);
                Console.WriteLine("CandidateAbstractionsPath=" + candidatePath);
                Console.WriteLine("CandidateAbstractionsSha256=" + candidateSha256);
                Console.WriteLine("CandidateAbstractionsAssemblyVersion=" + candidate.AssemblyVersion);
                Console.WriteLine("CandidateAbstractionsFileVersion=" + candidate.FileVersion);
                Console.WriteLine("CandidatePublicApiCount=" + candidate.PublicSurface.Count);
                Console.WriteLine("RemovedPublicApiCount=" + removed.Length);
                Console.WriteLine("AuthorizedRetiredPublicApiCount=" + authorizedRemoved.Length);
                Console.WriteLine("UnexpectedRemovedPublicApiCount=" + unexpectedRemoved.Length);
                Console.WriteLine("ConsumerPath=" + consumerPath);
                Console.WriteLine("ConsumerSha256=" + consumerSha256);
                Console.WriteLine("ConsumerReferencedAbstractionsVersion=" + binding.ConsumerReferencedAbstractionsVersion);
                Console.WriteLine("ConsumerMetadataStopOnManualMoveSetterReferences=" + setterReferences.Length);
                Console.WriteLine("ConsumerResolvedStopOnManualMoveSetterReferences=" + binding.ResolvedSetterReferenceCount);
                Console.WriteLine("ConsumerTypeCount=" + binding.ConsumerTypeCount);
                Console.WriteLine("ConsumerLoadedWithCandidateAbstractions=True");
                Console.WriteLine("RetainedPublicProductCount=" + publicProducts.Length);
                Console.WriteLine("RetainedPublicProductLampMemberReferenceCount=" + publicProductLampMemberReferenceCount);
                Console.WriteLine("RetainedPublicProductMetadataAbstractionsMemberReferenceCount=" +
                    publicProducts.Sum(value => value.MetadataAbstractionsMemberReferenceCount));
                Console.WriteLine("RetainedPublicProductResolvedAbstractionsMemberReferenceCount=" +
                    publicProducts.Sum(value => value.ResolvedAbstractionsMemberReferenceCount));
                foreach (PublicProductScanResult product in publicProducts)
                {
                    Console.WriteLine(
                        "RetainedPublicProductBinding=" + product.ProductId +
                        "|assembly=" + product.AssemblyName + "@" + product.AssemblyVersion +
                        "|abstractions=" + product.ReferencedAbstractionsVersion +
                        "|memberRefs=" + product.MetadataAbstractionsMemberReferenceCount +
                        "|resolved=" + product.ResolvedAbstractionsMemberReferenceCount);
                }
                Console.WriteLine("RetainedExternalConsumerCount=" + externalConsumers.Length);
                Console.WriteLine("RetainedExternalConsumerResolvedMemberReferenceCount=" +
                    externalConsumers.Sum(value => value.ResolvedAbstractionsMemberReferenceCount));
                Console.WriteLine("KnownFrozenCompatibilityConsumerCount=" +
                    publicProducts.Count(product => KnownCompatibilityConsumers.Any(
                        contract => string.Equals(contract.ProductId, product.ProductId, StringComparison.Ordinal))));
                Console.WriteLine("KnownFrozenCompatibilityProviderIdentityCount=" +
                    publicProducts.Count(product => KnownCompatibilityConsumers.Any(
                        contract => string.Equals(contract.ProductId, product.ProductId, StringComparison.Ordinal)) &&
                        product.GameBridgeProviderUniqueIdPresent));
                if (compatibilityHost != null)
                {
                    Console.WriteLine("MandatoryGameBridgeHostAssemblyReference=False");
                    Console.WriteLine("MandatoryGameBridgeHeavyExecutorMarkerCount=" + compatibilityHost.MandatoryHeavyExecutorMarkerCount);
                    Console.WriteLine("MandatoryCompatibilityServiceIlBytes=" + compatibilityHost.MandatoryCompatibilityServiceIlBytes);
                    Console.WriteLine("CompatibilityHostAssemblyName=" + compatibilityHost.HostAssemblyName);
                    Console.WriteLine("CompatibilityHostAssemblyVersion=" + compatibilityHost.HostAssemblyVersion);
                    Console.WriteLine("CompatibilityHostTargetFramework=" + compatibilityHost.TargetFramework);
                    Console.WriteLine("CompatibilityHostServiceIlBytes=" + compatibilityHost.HostCompatibilityServiceIlBytes);
                    Console.WriteLine("CompatibilityHostFactory=" + compatibilityHost.FactoryTypeName + "::" + compatibilityHost.FactoryMethodName);
                }
                Console.WriteLine("CandidateStopOnManualMoveGetter=True");
                Console.WriteLine("CandidateStopOnManualMoveSetter=True");
                Console.WriteLine("CandidateStopOnManualMoveDefault=True");
                Console.WriteLine("CandidateStopOnManualMoveRoundTrip=True");
                Console.WriteLine("CandidateStopOnManualMoveObsoleteIsError=False");
                Console.WriteLine("CandidateLampCompatibilityTypeCount=" + binding.LampCompatibility.TypeCount);
                Console.WriteLine("CandidateLampCompatibilityObsoleteTypeCount=" + binding.LampCompatibility.ObsoleteTypeCount);
                Console.WriteLine("CandidateLampCompatibilityObsoleteIsError=False");
                Console.WriteLine("CandidateLampCompatibilityDisabledStatusAttributes=" + binding.LampCompatibility.DisabledStatusAttributes);
                Console.WriteLine("CandidateLampCompatibilityHistoricalDefaults=" + binding.LampCompatibility.HistoricalDefaults);
                Console.WriteLine("UnityMonoRuntimeValidation=False");
                if (reportPath != null)
                    Console.WriteLine("MachineReadableReportPath=" + reportPath);

                if (unexpectedRemoved.Length > 0)
                {
                    Console.Error.WriteLine("ABI_GATE=FAIL");
                    Console.Error.WriteLine(
                        "Candidate deleted " + unexpectedRemoved.Length +
                        " public API entries outside the exact authorized DebugConsole CSV retirement; assembly versions were ignored. See the machine-readable report for the full removal list.");
                    return 1;
                }

                Console.WriteLine("ABI_GATE=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("ABI_GATE=FAIL");
                Console.Error.WriteLine(exception.ToString());
                Console.Error.WriteLine("UnityMonoRuntimeValidation=False");
                return 1;
            }
        }

        private static IReadOnlyDictionary<string, string> ParseArguments(string[] args)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Arguments must use --name value pairs.");

                string key = args[index].Substring(2);
                if (!result.TryAdd(key, args[index + 1]))
                    throw new ArgumentException("Duplicate argument: --" + key);
            }

            return result;
        }

        private static int RunSyntheticContract(IReadOnlyDictionary<string, string> options)
        {
            string contractPath = RequireFile(options, "synthetic-contract");
            string candidatePath = RequireFile(options, "candidate-abstractions");
            string? reportPath = GetOptionalPath(options, "report");
            SyntheticAbiContract contract = JsonSerializer.Deserialize<SyntheticAbiContract>(
                File.ReadAllText(contractPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Synthetic ABI contract could not be parsed.");
            if (contract.SchemaVersion != 1 || string.IsNullOrWhiteSpace(contract.ContractId) ||
                contract.TypeSurfaceSha256 == null || contract.TypeSurfaceSha256.Count == 0)
                throw new InvalidOperationException("Synthetic ABI contract schema is incomplete.");

            string candidateSha256 = ComputeSha256(candidatePath);
            AssemblySnapshot snapshot = ReadPublicSurface(candidatePath);
            RequireEqual(contract.ExpectedAssemblyName, snapshot.Name, "synthetic candidate assembly name");
            RequireEqual(contract.ExpectedAssemblyVersion, snapshot.AssemblyVersion, "synthetic candidate assembly version");

            var actualHashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> expected in contract.TypeSurfaceSha256.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                string[] surface = snapshot.PublicSurface
                    .Where(entry => IsDeclaredTypeSurface(entry, expected.Key))
                    .OrderBy(entry => entry, StringComparer.Ordinal)
                    .ToArray();
                if (surface.Length == 0)
                    throw new MissingMemberException("Synthetic ABI type surface is missing: " + expected.Key + ".");
                string actualHash = ComputeTextSha256(string.Join("\n", surface));
                actualHashes[expected.Key] = actualHash;
                RequireEqual(expected.Value.ToUpperInvariant(), actualHash, "synthetic type surface " + expected.Key);
            }

            var context = new ArtifactLoadContext();
            LampCompatibilityReport lamp;
            try
            {
                Assembly candidate = context.LoadFromAssemblyPath(candidatePath);
                ValidateRestoredProperty(candidate);
                lamp = ValidateLampCompatibilityShell(candidate);
            }
            finally
            {
                context.Unload();
            }

            var report = new SyntheticAbiReport(
                SchemaVersion: 1,
                OverallPass: true,
                ContractId: contract.ContractId,
                CandidateFileName: Path.GetFileName(candidatePath),
                CandidateSha256: candidateSha256,
                CandidateAssemblyVersion: snapshot.AssemblyVersion,
                TypeSurfaceSha256: actualHashes,
                StopOnManualMoveDefault: true,
                StopOnManualMoveObsoleteIsError: false,
                LampCompatibility: lamp,
                PrivateRetainedArtifactsUsed: false);
            if (reportPath != null)
                WriteSyntheticReport(reportPath, report);

            Console.WriteLine("SyntheticContractId=" + contract.ContractId);
            Console.WriteLine("CandidateAbstractionsPath=" + candidatePath);
            Console.WriteLine("CandidateAbstractionsSha256=" + candidateSha256);
            Console.WriteLine("CandidateAbstractionsAssemblyVersion=" + snapshot.AssemblyVersion);
            Console.WriteLine("SyntheticTypeSurfaceCount=" + actualHashes.Count);
            Console.WriteLine("CandidateStopOnManualMoveDefault=True");
            Console.WriteLine("CandidateStopOnManualMoveObsoleteIsError=False");
            Console.WriteLine("CandidateLampCompatibilityTypeCount=" + lamp.TypeCount);
            Console.WriteLine("PrivateRetainedArtifactsUsed=False");
            if (reportPath != null)
                Console.WriteLine("MachineReadableReportPath=" + reportPath);
            Console.WriteLine("ABI_GATE=PASS");
            return 0;
        }

        private static bool IsDeclaredTypeSurface(string entry, string typeName)
        {
            return entry.Equals("T|" + typeName, StringComparison.Ordinal) ||
                entry.StartsWith("TK|" + typeName + "|", StringComparison.Ordinal) ||
                entry.StartsWith("TB|" + typeName + "|", StringComparison.Ordinal) ||
                entry.StartsWith("TI|" + typeName + "|", StringComparison.Ordinal) ||
                entry.StartsWith("TG|" + typeName + "|", StringComparison.Ordinal) ||
                entry.Contains("|" + typeName + "|", StringComparison.Ordinal) &&
                    (entry.StartsWith("M|", StringComparison.Ordinal) ||
                     entry.StartsWith("F|", StringComparison.Ordinal)) ||
                entry.StartsWith("P|" + typeName + "|", StringComparison.Ordinal) ||
                entry.StartsWith("E|" + typeName + "|", StringComparison.Ordinal);
        }

        private static string ComputeTextSha256(string value)
        {
            using SHA256 sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static void WriteSyntheticReport(string path, SyntheticAbiReport report)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + Environment.NewLine);
        }

        private static string RequireFile(IReadOnlyDictionary<string, string> options, string key)
        {
            string path = Path.GetFullPath(RequireOption(options, key));
            if (!File.Exists(path))
                throw new FileNotFoundException("Required ABI artifact does not exist.", path);
            return path;
        }

        private static string RequireOption(IReadOnlyDictionary<string, string> options, string key)
        {
            if (!options.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Missing required argument: --" + key);
            return value;
        }

        private static string? GetOptionalPath(IReadOnlyDictionary<string, string> options, string key)
        {
            return options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? Path.GetFullPath(value)
                : null;
        }

        private static void WriteReport(string path, AbiGateReport report)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            string json = JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            File.WriteAllText(path, json + Environment.NewLine);
        }

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(stream));
        }

        private static AssemblySnapshot ReadPublicSurface(string path)
        {
            var context = new ArtifactLoadContext();
            try
            {
                Assembly assembly = context.LoadFromAssemblyPath(path);
                AssemblyName assemblyName = assembly.GetName();
                Type[] publicTypes = GetAllTypes(assembly)
                    .Where(IsExternallyVisible)
                    .OrderBy(type => FormatType(type), StringComparer.Ordinal)
                    .ToArray();
                var surface = new HashSet<string>(StringComparer.Ordinal);

                foreach (Type type in publicTypes)
                {
                    string typeName = FormatType(type);
                    surface.Add("T|" + typeName);
                    surface.Add("TK|" + typeName + "|" + GetTypeKind(type));
                    if (type.BaseType != null)
                        surface.Add("TB|" + typeName + "|" + FormatType(type.BaseType));
                    foreach (Type contract in type.GetInterfaces().OrderBy(FormatType, StringComparer.Ordinal))
                        surface.Add("TI|" + typeName + "|" + FormatType(contract));
                    AddGenericParameterSurface(surface, "TG|" + typeName, type.GetGenericArguments().Where(argument => argument.IsGenericParameter));

                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                               BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (ConstructorInfo constructor in type.GetConstructors(flags).Where(IsPublicApiMethod))
                        surface.Add("M|" + FormatMethod(constructor));
                    foreach (MethodInfo method in type.GetMethods(flags).Where(IsPublicApiMethod))
                    {
                        string methodKey = FormatMethod(method);
                        surface.Add("M|" + methodKey);
                        AddGenericParameterSurface(surface, "MG|" + methodKey, method.GetGenericArguments().Where(argument => argument.IsGenericParameter));
                    }
                    foreach (FieldInfo field in type.GetFields(flags).Where(IsPublicApiField))
                    {
                        surface.Add(
                            "F|" + Accessibility(field) + "|" + (field.IsStatic ? "static" : "instance") + "|" +
                            (field.IsLiteral ? "literal" : field.IsInitOnly ? "readonly" : "mutable") + "|" +
                            typeName + "|" + field.Name + "|" + FormatType(field.FieldType));
                    }
                    foreach (PropertyInfo property in type.GetProperties(flags).Where(IsPublicApiProperty))
                    {
                        string indexTypes = string.Join(",", property.GetIndexParameters().Select(parameter => FormatParameter(parameter)));
                        surface.Add("P|" + typeName + "|" + property.Name + "|(" + indexTypes + ")|" + FormatType(property.PropertyType));
                    }
                    foreach (EventInfo eventInfo in type.GetEvents(flags).Where(IsPublicApiEvent))
                        surface.Add("E|" + typeName + "|" + eventInfo.Name + "|" + FormatType(eventInfo.EventHandlerType!));
                }

                string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
                return new AssemblySnapshot(
                    assemblyName.Name ?? string.Empty,
                    assemblyName.Version?.ToString() ?? string.Empty,
                    fileVersion,
                    surface);
            }
            finally
            {
                context.Unload();
            }
        }

        private static void AddGenericParameterSurface(HashSet<string> surface, string owner, IEnumerable<Type> parameters)
        {
            foreach (Type parameter in parameters.OrderBy(value => value.GenericParameterPosition))
            {
                string prefix = owner + "|" + parameter.GenericParameterPosition + "|" + parameter.GenericParameterAttributes;
                surface.Add(prefix);
                foreach (Type constraint in parameter.GetGenericParameterConstraints().OrderBy(FormatType, StringComparer.Ordinal))
                    surface.Add(prefix + "|constraint=" + FormatType(constraint));
            }
        }

        private static MemberReferenceInfo[] FindStopOnManualMoveSetterReferences(string consumerPath)
        {
            using FileStream stream = File.OpenRead(consumerPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
                throw new BadImageFormatException("Retained consumer is not a managed assembly.", consumerPath);

            MetadataReader reader = peReader.GetMetadataReader();
            var results = new List<MemberReferenceInfo>();
            foreach (MemberReferenceHandle handle in reader.MemberReferences)
            {
                MemberReference reference = reader.GetMemberReference(handle);
                if (!string.Equals(reader.GetString(reference.Name), StopOnManualMoveSetterName, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(GetMemberParentTypeName(reader, reference.Parent), FishingOptionsTypeName, StringComparison.Ordinal))
                    continue;
                results.Add(new MemberReferenceInfo(MetadataTokens.GetToken(handle)));
            }
            return results.ToArray();
        }

        private static PublicProductScanResult[] ScanRetainedPublicProducts(
            IReadOnlyDictionary<string, string> options,
            string candidatePath,
            string expectedCandidateVersion)
        {
            bool hasPaths = options.TryGetValue("retained-product-dlls", out string? pathsValue);
            bool hasIds = options.TryGetValue("retained-product-ids", out string? idsValue);
            if (!hasPaths && !hasIds)
                return Array.Empty<PublicProductScanResult>();
            if (!hasPaths || !hasIds || string.IsNullOrWhiteSpace(pathsValue) || string.IsNullOrWhiteSpace(idsValue))
                throw new ArgumentException("--retained-product-dlls and --retained-product-ids must be supplied together.");

            string[] paths = pathsValue.Split('|').Select(Path.GetFullPath).ToArray();
            string[] ids = idsValue.Split('|');
            if (paths.Length != ids.Length)
                throw new ArgumentException("Retained product path/id counts do not match.");
            if (paths.Length != 11 || ids.Distinct(StringComparer.Ordinal).Count() != 11)
                throw new ArgumentException("Retained public product binding requires exactly 11 unique Catalog product ids.");

            var results = new List<PublicProductScanResult>(paths.Length);
            for (int index = 0; index < paths.Length; index++)
            {
                if (!File.Exists(paths[index]))
                    throw new FileNotFoundException("Retained public product DLL does not exist.", paths[index]);
                string sha256 = ComputeSha256(paths[index]);
                RetainedConsumerMetadata metadata = ReadRetainedConsumerMetadata(paths[index]);
                if (metadata.MemberReferenceTokens.Length == 0)
                    throw new InvalidOperationException(ids[index] + " has no DTMAPI.Abstractions MemberRef.");
                int resolvedMemberReferences = ResolveRetainedConsumerMemberReferences(
                    paths[index],
                    candidatePath,
                    expectedCandidateVersion,
                    metadata.MemberReferenceTokens,
                    ids[index]);
                if (resolvedMemberReferences != metadata.MemberReferenceTokens.Length)
                    throw new MissingMemberException(ids[index] + " did not resolve every DTMAPI.Abstractions MemberRef.");
                string[] references = FindMemberReferencesToTypes(paths[index], RetainedLampTypeNames);
                string[] frozenReferences = FindMemberReferencesToTypes(
                    paths[index],
                    FrozenCompatibilityTypeNames
                        .Concat(RetainedDebugCompatibilityTypeNames)
                        .ToArray());
                KnownCompatibilityConsumerContract? contract = KnownCompatibilityConsumers.SingleOrDefault(
                    value => string.Equals(value.ProductId, ids[index], StringComparison.Ordinal));
                bool providerUniqueIdPresent = ContainsUtf16MetadataString(paths[index], GameBridgeProviderUniqueId);
                if (contract != null)
                {
                    RequireEqual(contract.Sha256, sha256, contract.ProductId + " retained consumer SHA-256");
                    if (!providerUniqueIdPresent)
                        throw new InvalidOperationException(contract.ProductId + " does not retain the GameBridge provider UniqueID literal.");
                    if (!frozenReferences.Any(value => value.StartsWith(contract.FrozenInterfaceType + "::", StringComparison.Ordinal)))
                        throw new InvalidOperationException(contract.ProductId + " does not reference its frozen interface " + contract.FrozenInterfaceType + ".");
                    foreach (string requiredMember in contract.RequiredMemberReferences)
                    {
                        if (!frozenReferences.Contains(requiredMember, StringComparer.Ordinal))
                            throw new MissingMemberException(contract.ProductId + " retained consumer metadata is missing " + requiredMember + ".");
                    }
                    if (string.Equals(contract.ProductId, "more-equipment-slots", StringComparison.Ordinal) ||
                        string.Equals(contract.ProductId, "zoom", StringComparison.Ordinal) ||
                        string.Equals(contract.ProductId, "y-console", StringComparison.Ordinal))
                    {
                        RequireExactSet(
                            contract.RequiredMemberReferences,
                            frozenReferences,
                            contract.ProductId + " exact retained MemberRef set");
                    }
                }
                results.Add(new PublicProductScanResult(
                    ids[index], Path.GetFileName(paths[index]), sha256, references,
                    providerUniqueIdPresent, frozenReferences,
                    metadata.AssemblyName, metadata.AssemblyVersion,
                    metadata.AbstractionsReferenceVersion,
                    metadata.MemberReferenceNames.Length,
                    resolvedMemberReferences,
                    metadata.MemberReferenceNames));
            }
            PublicProductScanResult[] ordered = results.OrderBy(result => result.ProductId, StringComparer.Ordinal).ToArray();
            string[] missingKnownConsumers = KnownCompatibilityConsumers
                .Select(contract => contract.ProductId)
                .Except(ordered.Select(result => result.ProductId), StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (missingKnownConsumers.Length > 0)
            {
                throw new InvalidOperationException(
                    "Retained public product scan is missing the known-consumer compatibility authority: " +
                    string.Join(",", missingKnownConsumers) + ".");
            }
            return ordered;
        }

        private static ExternalConsumerScanResult[] ScanRetainedExternalConsumers(
            IReadOnlyDictionary<string, string> options,
            string candidatePath,
            string expectedCandidateVersion)
        {
            bool hasPaths = options.TryGetValue("retained-external-dlls", out string? pathsValue);
            bool hasIds = options.TryGetValue("retained-external-ids", out string? idsValue);
            if (!hasPaths && !hasIds)
                return Array.Empty<ExternalConsumerScanResult>();
            if (!hasPaths || !hasIds || string.IsNullOrWhiteSpace(pathsValue) || string.IsNullOrWhiteSpace(idsValue))
                throw new ArgumentException("--retained-external-dlls and --retained-external-ids must be supplied together.");

            string[] paths = pathsValue.Split('|').Select(Path.GetFullPath).ToArray();
            string[] ids = idsValue.Split('|');
            if (paths.Length != ids.Length)
                throw new ArgumentException("Retained external consumer path/id counts do not match.");
            RequireExactSet(
                KnownExternalConsumers.Select(value => value.WorkshopId),
                ids,
                "retained external consumer Workshop id set");

            var results = new List<ExternalConsumerScanResult>(paths.Length);
            for (int index = 0; index < paths.Length; index++)
            {
                if (!File.Exists(paths[index]))
                    throw new FileNotFoundException("Retained external consumer DLL does not exist.", paths[index]);
                KnownExternalConsumerContract contract = KnownExternalConsumers.Single(value =>
                    string.Equals(value.WorkshopId, ids[index], StringComparison.Ordinal));
                RequireEqual(contract.FileName, Path.GetFileName(paths[index]), contract.WorkshopId + " retained external file name");
                string sha256 = ComputeSha256(paths[index]);
                RequireEqual(contract.Sha256, sha256, contract.WorkshopId + " retained external SHA-256");

                RetainedConsumerMetadata metadata = ReadRetainedConsumerMetadata(paths[index]);
                if (!string.IsNullOrEmpty(contract.ExpectedAbstractionsReferenceVersion))
                {
                    RequireEqual(
                        contract.ExpectedAbstractionsReferenceVersion,
                        metadata.AbstractionsReferenceVersion,
                        contract.WorkshopId + " retained external DTMAPI.Abstractions reference version");
                }
                if (metadata.MemberReferenceTokens.Length == 0)
                    throw new InvalidOperationException(contract.WorkshopId + " has no DTMAPI.Abstractions MemberRef.");
                int resolvedMemberReferences = ResolveRetainedConsumerMemberReferences(
                    paths[index],
                    candidatePath,
                    expectedCandidateVersion,
                    metadata.MemberReferenceTokens,
                    contract.WorkshopId);
                if (resolvedMemberReferences != metadata.MemberReferenceTokens.Length)
                    throw new MissingMemberException(contract.WorkshopId + " did not resolve every DTMAPI.Abstractions MemberRef.");

                results.Add(new ExternalConsumerScanResult(
                    contract.WorkshopId,
                    Path.GetFileName(paths[index]),
                    sha256,
                    metadata.AssemblyName,
                    metadata.AssemblyVersion,
                    metadata.AbstractionsReferenceVersion,
                    metadata.MemberReferenceNames.Length,
                    resolvedMemberReferences,
                    metadata.MemberReferenceNames));
            }

            return results.OrderBy(value => value.WorkshopId, StringComparer.Ordinal).ToArray();
        }

        private static RetainedConsumerMetadata ReadRetainedConsumerMetadata(string assemblyPath)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
                throw new BadImageFormatException("Retained consumer is not a managed assembly.", assemblyPath);

            MetadataReader reader = peReader.GetMetadataReader();
            AssemblyDefinition assembly = reader.GetAssemblyDefinition();
            AssemblyReference[] abstractionsReferences = reader.AssemblyReferences
                .Select(reader.GetAssemblyReference)
                .Where(reference => string.Equals(
                    reader.GetString(reference.Name),
                    AbstractionsAssemblyName,
                    StringComparison.Ordinal))
                .ToArray();
            if (abstractionsReferences.Length != 1)
                throw new InvalidOperationException(
                    Path.GetFileName(assemblyPath) + " must have exactly one DTMAPI.Abstractions AssemblyRef.");

            var memberReferences = new List<(int Token, string Name)>();
            foreach (MemberReferenceHandle handle in reader.MemberReferences)
            {
                MemberReference reference = reader.GetMemberReference(handle);
                if (!IsTypeReferenceFromAbstractions(reader, reference.Parent))
                    continue;
                memberReferences.Add((
                    MetadataTokens.GetToken(handle),
                    GetMemberParentTypeName(reader, reference.Parent) + "::" + reader.GetString(reference.Name)));
            }

            return new RetainedConsumerMetadata(
                reader.GetString(assembly.Name),
                assembly.Version.ToString(),
                abstractionsReferences[0].Version.ToString(),
                memberReferences.Select(value => value.Token).ToArray(),
                memberReferences.Select(value => value.Name)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray());
        }

        private static bool IsTypeReferenceFromAbstractions(MetadataReader reader, EntityHandle parent)
        {
            if (parent.Kind != HandleKind.TypeReference)
                return false;
            TypeReference type = reader.GetTypeReference((TypeReferenceHandle)parent);
            EntityHandle scope = type.ResolutionScope;
            while (scope.Kind == HandleKind.TypeReference)
            {
                type = reader.GetTypeReference((TypeReferenceHandle)scope);
                scope = type.ResolutionScope;
            }
            if (scope.Kind != HandleKind.AssemblyReference)
                return false;
            AssemblyReference reference = reader.GetAssemblyReference((AssemblyReferenceHandle)scope);
            return string.Equals(reader.GetString(reference.Name), AbstractionsAssemblyName, StringComparison.Ordinal);
        }

        private static int ResolveRetainedConsumerMemberReferences(
            string consumerPath,
            string candidatePath,
            string expectedCandidateVersion,
            IReadOnlyCollection<int> memberReferenceTokens,
            string consumerId)
        {
            var context = new CandidateBindingLoadContext(candidatePath, Path.GetDirectoryName(consumerPath)!);
            try
            {
                Assembly candidate = context.LoadCandidate();
                RequireEqual(expectedCandidateVersion, candidate.GetName().Version?.ToString() ?? string.Empty, consumerId + " ABI candidate version");
                Assembly consumer = context.LoadFromAssemblyPath(consumerPath);
                int resolved = 0;
                foreach (int token in memberReferenceTokens)
                {
                    MemberInfo member = consumer.ManifestModule.ResolveMember(token)
                        ?? throw new MissingMemberException("Unable to resolve " + consumerId + " retained MemberRef 0x" + token.ToString("X8") + ".");
                    Assembly declaringAssembly = member.DeclaringType?.Assembly
                        ?? throw new MissingMemberException(consumerId + " retained MemberRef has no declaring assembly.");
                    if (!string.Equals(declaringAssembly.GetName().Name, AbstractionsAssemblyName, StringComparison.Ordinal) ||
                        !string.Equals(Path.GetFullPath(declaringAssembly.Location), Path.GetFullPath(candidatePath), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new MissingMemberException(
                            consumerId + " retained MemberRef resolved outside the exact candidate DTMAPI.Abstractions assembly.");
                    }
                    resolved++;
                }
                return resolved;
            }
            finally
            {
                context.Unload();
            }
        }

        private static bool ContainsUtf16MetadataString(string assemblyPath, string value)
        {
            byte[] assembly = File.ReadAllBytes(assemblyPath);
            byte[] encoded = System.Text.Encoding.Unicode.GetBytes(value);
            return assembly.AsSpan().IndexOf(encoded) >= 0;
        }

        private static CompatibilityHostMetadataReport? ValidateCompatibilityHostArtifacts(
            IReadOnlyDictionary<string, string> options)
        {
            bool hasMandatory = options.TryGetValue("mandatory-gamebridge", out string? mandatoryValue);
            bool hasHost = options.TryGetValue("compatibility-host", out string? hostValue);
            if (!hasMandatory && !hasHost)
                return null;
            if (!hasMandatory || !hasHost || string.IsNullOrWhiteSpace(mandatoryValue) || string.IsNullOrWhiteSpace(hostValue))
                throw new ArgumentException("--mandatory-gamebridge and --compatibility-host must be supplied together.");

            string mandatoryPath = Path.GetFullPath(mandatoryValue);
            string hostPath = Path.GetFullPath(hostValue);
            if (!File.Exists(mandatoryPath))
                throw new FileNotFoundException("Mandatory GameBridge artifact does not exist.", mandatoryPath);
            if (!File.Exists(hostPath))
                throw new FileNotFoundException("Compatibility Host artifact does not exist.", hostPath);

            ManagedAssemblyMetadata mandatory = ReadManagedAssemblyMetadata(mandatoryPath);
            ManagedAssemblyMetadata host = ReadManagedAssemblyMetadata(hostPath);
            RequireEqual(MandatoryGameBridgeAssemblyName, mandatory.Name, "mandatory GameBridge assembly name");
            RequireEqual(CurrentRuntimeAssemblyVersion, mandatory.Version, "mandatory GameBridge assembly version");
            RequireEqual(CompatibilityHostAssemblyName, host.Name, "Compatibility Host assembly name");
            RequireEqual(CurrentRuntimeAssemblyVersion, host.Version, "Compatibility Host assembly version");
            if (mandatory.AssemblyReferences.Contains(CompatibilityHostAssemblyName, StringComparer.Ordinal))
                throw new InvalidOperationException("Mandatory GameBridge has a forbidden static AssemblyRef to the optional Compatibility Host.");

            string[] expectedHostReferences =
            {
                "DTMAPI.Abstractions",
                "DTMAPI.Core",
                MandatoryGameBridgeAssemblyName
            };
            foreach (string expectedReference in expectedHostReferences)
            {
                if (!host.AssemblyReferences.Contains(expectedReference, StringComparer.Ordinal))
                    throw new InvalidOperationException("Compatibility Host is missing AssemblyRef " + expectedReference + ".");
            }

            foreach (string serviceType in CompatibilityServiceTypeNames)
            {
                if (!mandatory.TypeNames.Contains(serviceType))
                    throw new InvalidOperationException("Mandatory GameBridge is missing thin compatibility proxy " + serviceType + ".");
                if (!host.TypeNames.Contains(serviceType))
                    throw new InvalidOperationException("Compatibility Host is missing extracted executor " + serviceType + ".");
            }
            if (!mandatory.TypeNames.Contains(ChestLocatorMandatoryProxyType))
                throw new InvalidOperationException("Mandatory GameBridge is missing thin compatibility proxy " + ChestLocatorMandatoryProxyType + ".");
            if (!host.TypeNames.Contains(ChestLocatorHostExecutorType))
                throw new InvalidOperationException("Compatibility Host is missing extracted executor " + ChestLocatorHostExecutorType + ".");
            if (!mandatory.TypeNames.Contains(DebugActionMandatoryProxyType))
                throw new InvalidOperationException("Mandatory GameBridge is missing thin compatibility proxy " + DebugActionMandatoryProxyType + ".");
            if (!host.TypeNames.Contains(DebugActionHostExecutorType))
                throw new InvalidOperationException("Compatibility Host is missing extracted executor " + DebugActionHostExecutorType + ".");
            string[] mandatoryServiceTypes = CompatibilityServiceTypeNames
                .Append(ChestLocatorMandatoryProxyType)
                .Append(DebugActionMandatoryProxyType)
                .ToArray();
            string[] hostServiceTypes = CompatibilityServiceTypeNames
                .Append(ChestLocatorHostExecutorType)
                .Append(DebugActionHostExecutorType)
                .ToArray();
            int mandatoryServiceIlBytes = mandatory.Methods
                .Where(method => mandatoryServiceTypes.Contains(method.DeclaringType, StringComparer.Ordinal))
                .Sum(method => method.IlSize);
            int hostServiceIlBytes = host.Methods
                .Where(method => hostServiceTypes.Contains(method.DeclaringType, StringComparer.Ordinal))
                .Sum(method => method.IlSize);
            if (mandatoryServiceIlBytes > 12 * 1024)
                throw new InvalidOperationException("Mandatory GameBridge compatibility proxies retain too much executor IL: " + mandatoryServiceIlBytes + " bytes.");
            if (hostServiceIlBytes < 32 * 1024 || hostServiceIlBytes <= mandatoryServiceIlBytes * 4)
                throw new InvalidOperationException("Compatibility Host does not contain a materially extracted heavy executor body set.");

            string[] mandatoryHeavyMarkers = HeavyExecutorMarkerTypeNames
                .Where(mandatory.TypeNames.Contains)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (mandatoryHeavyMarkers.Length > 0)
                throw new InvalidOperationException("Mandatory GameBridge still contains heavy executor markers: " + string.Join(",", mandatoryHeavyMarkers) + ".");
            string[] missingHostMarkers = HeavyExecutorMarkerTypeNames
                .Where(value => !host.TypeNames.Contains(value))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (missingHostMarkers.Length > 0)
                throw new InvalidOperationException("Compatibility Host is missing extracted heavy executor markers: " + string.Join(",", missingHostMarkers) + ".");

            ManagedMethodMetadata[] factoryMethods = host.Methods
                .Where(method => string.Equals(method.DeclaringType, CompatibilityHostFactoryTypeName, StringComparison.Ordinal) &&
                    string.Equals(method.Name, CompatibilityHostFactoryMethodName, StringComparison.Ordinal))
                .ToArray();
            if (factoryMethods.Length != 1)
                throw new InvalidOperationException("Compatibility Host must expose exactly one well-known factory method.");
            ManagedMethodMetadata factory = factoryMethods[0];
            if (!factory.IsStatic || !factory.IsVisibleToMandatoryAssembly || factory.ParameterCount != 1 || factory.IlSize <= 0)
                throw new InvalidOperationException("Compatibility Host factory must be a callable static one-parameter method with a real body.");

            byte[] hostBytes = File.ReadAllBytes(hostPath);
            if (hostBytes.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes(NetStandard20TargetFramework)) < 0)
                throw new InvalidOperationException("Compatibility Host metadata does not declare netstandard2.0.");

            return new CompatibilityHostMetadataReport(
                MandatoryGameBridgeFileName: Path.GetFileName(mandatoryPath),
                MandatoryGameBridgeSha256: ComputeSha256(mandatoryPath),
                MandatoryHostAssemblyReference: false,
                MandatoryHeavyExecutorMarkerCount: mandatoryHeavyMarkers.Length,
                MandatoryCompatibilityServiceIlBytes: mandatoryServiceIlBytes,
                HostFileName: Path.GetFileName(hostPath),
                HostSha256: ComputeSha256(hostPath),
                HostAssemblyName: host.Name,
                HostAssemblyVersion: host.Version,
                TargetFramework: NetStandard20TargetFramework,
                HostHeavyExecutorMarkerCount: HeavyExecutorMarkerTypeNames.Length,
                HostCompatibilityServiceIlBytes: hostServiceIlBytes,
                FactoryTypeName: factory.DeclaringType,
                FactoryMethodName: factory.Name,
                FactoryParameterCount: factory.ParameterCount);
        }

        private static ManagedAssemblyMetadata ReadManagedAssemblyMetadata(string assemblyPath)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
                throw new BadImageFormatException("Compatibility artifact is not a managed assembly.", assemblyPath);
            MetadataReader reader = peReader.GetMetadataReader();
            AssemblyDefinition assembly = reader.GetAssemblyDefinition();
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            var methods = new List<ManagedMethodMetadata>();
            foreach (TypeDefinitionHandle typeHandle in reader.TypeDefinitions)
            {
                string typeName = GetTypeDefinitionFullName(reader, typeHandle);
                typeNames.Add(typeName);
                TypeDefinition type = reader.GetTypeDefinition(typeHandle);
                foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
                {
                    MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                    int ilSize = method.RelativeVirtualAddress == 0
                        ? 0
                        : peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()?.Length ?? 0;
                    int parameterCount = method.GetParameters()
                        .Select(reader.GetParameter)
                        .Count(parameter => parameter.SequenceNumber > 0);
                    MethodAttributes visibility = method.Attributes & MethodAttributes.MemberAccessMask;
                    methods.Add(new ManagedMethodMetadata(
                        typeName,
                        reader.GetString(method.Name),
                        ilSize,
                        parameterCount,
                        (method.Attributes & MethodAttributes.Static) != 0,
                        visibility == MethodAttributes.Public || visibility == MethodAttributes.Assembly));
                }
            }

            string[] references = reader.AssemblyReferences
                .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return new ManagedAssemblyMetadata(
                reader.GetString(assembly.Name),
                assembly.Version.ToString(),
                references,
                typeNames,
                methods.ToArray());
        }

        private static string GetTypeDefinitionFullName(MetadataReader reader, TypeDefinitionHandle handle)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string name = reader.GetString(type.Name);
            TypeDefinitionHandle parent = type.GetDeclaringType();
            if (!parent.IsNil)
                return GetTypeDefinitionFullName(reader, parent) + "+" + name;
            string ns = reader.GetString(type.Namespace);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private static string[] FindMemberReferencesToTypes(string assemblyPath, IReadOnlyCollection<string> targetTypeNames)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
                throw new BadImageFormatException("Retained public product is not a managed assembly.", assemblyPath);

            MetadataReader reader = peReader.GetMetadataReader();
            var results = new List<string>();
            foreach (MemberReferenceHandle handle in reader.MemberReferences)
            {
                MemberReference reference = reader.GetMemberReference(handle);
                string parentType = GetMemberParentTypeName(reader, reference.Parent);
                if (!targetTypeNames.Contains(parentType, StringComparer.Ordinal))
                    continue;
                results.Add(parentType + "::" + reader.GetString(reference.Name));
            }
            return results.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string GetMemberParentTypeName(MetadataReader reader, EntityHandle parent)
        {
            if (parent.Kind == HandleKind.TypeReference)
            {
                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)parent);
                string name = reader.GetString(type.Name);
                string ns = reader.GetString(type.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
            if (parent.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition type = reader.GetTypeDefinition((TypeDefinitionHandle)parent);
                string name = reader.GetString(type.Name);
                string ns = reader.GetString(type.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
            return string.Empty;
        }

        private static ConsumerBindingResult LoadConsumerAgainstCandidate(
            string consumerPath,
            string candidatePath,
            string expectedConsumerReferenceVersion,
            string expectedCandidateVersion,
            IReadOnlyCollection<MemberReferenceInfo> setterReferences)
        {
            var context = new CandidateBindingLoadContext(candidatePath, Path.GetDirectoryName(consumerPath)!);
            try
            {
                Assembly candidate = context.LoadCandidate();
                RequireEqual(expectedCandidateVersion, candidate.GetName().Version?.ToString() ?? string.Empty, "loaded candidate assembly version");

                Assembly consumer = context.LoadFromAssemblyPath(consumerPath);
                AssemblyName consumerReference = consumer.GetReferencedAssemblies()
                    .SingleOrDefault(reference => string.Equals(reference.Name, AbstractionsAssemblyName, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("Retained consumer does not reference DTMAPI.Abstractions.");
                RequireEqual(
                    expectedConsumerReferenceVersion,
                    consumerReference.Version?.ToString() ?? string.Empty,
                    "retained consumer DTMAPI.Abstractions reference version");

                Type[] consumerTypes = GetAllTypes(consumer);
                ForceSignatureResolution(consumerTypes);

                int resolvedSetterReferences = 0;
                foreach (MemberReferenceInfo reference in setterReferences)
                {
                    MemberInfo resolved = consumer.ManifestModule.ResolveMember(reference.MetadataToken)
                        ?? throw new MissingMethodException("Unable to resolve retained StopOnManualMove setter reference.");
                    if (resolved is not MethodInfo method ||
                        !string.Equals(method.Name, StopOnManualMoveSetterName, StringComparison.Ordinal) ||
                        !string.Equals(method.DeclaringType?.FullName, FishingOptionsTypeName, StringComparison.Ordinal) ||
                        !string.Equals(method.DeclaringType?.Assembly.GetName().Name, AbstractionsAssemblyName, StringComparison.Ordinal))
                    {
                        throw new MissingMethodException("Retained StopOnManualMove setter reference resolved to an unexpected member.");
                    }
                    Type declaringType = method.DeclaringType!;
                    RequireEqual(
                        Path.GetFullPath(candidatePath),
                        Path.GetFullPath(declaringType.Assembly.Location),
                        "resolved StopOnManualMove setter assembly path");
                    resolvedSetterReferences++;
                }

                ValidateRestoredProperty(candidate);
                ValidateFrozenCompatibilityMetadata(candidate);
                ValidateStrongPlantingGunFrozenShape(candidate);
                LampCompatibilityReport lampCompatibility = ValidateLampCompatibilityShell(candidate);
                Assembly[] boundAbstractions = context.Assemblies
                    .Where(assembly => string.Equals(assembly.GetName().Name, AbstractionsAssemblyName, StringComparison.Ordinal))
                    .ToArray();
                if (boundAbstractions.Length != 1 || !ReferenceEquals(boundAbstractions[0], candidate))
                    throw new InvalidOperationException("Consumer load context did not bind exactly one candidate DTMAPI.Abstractions assembly.");

                return new ConsumerBindingResult(
                    consumerReference.Version?.ToString() ?? string.Empty,
                    consumerTypes.Length,
                    resolvedSetterReferences,
                    lampCompatibility);
            }
            finally
            {
                context.Unload();
            }
        }

        private static void ValidateRestoredProperty(Assembly candidate)
        {
            Type optionsType = candidate.GetType(FishingOptionsTypeName, throwOnError: true, ignoreCase: false)!;
            PropertyInfo property = optionsType.GetProperty(
                StopOnManualMovePropertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?? throw new MissingMemberException(FishingOptionsTypeName, StopOnManualMovePropertyName);
            if (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true || property.PropertyType != typeof(bool))
                throw new MissingMethodException("StopOnManualMove must retain a public bool getter and setter.");

            object instance = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("FishingAutomationOptions default constructor returned null.");
            if (property.GetValue(instance) is not bool defaultValue || !defaultValue)
                throw new InvalidOperationException("StopOnManualMove must retain default=true.");
            property.SetValue(instance, false);
            if (property.GetValue(instance) is not bool roundTripValue || roundTripValue)
                throw new InvalidOperationException("StopOnManualMove getter/setter round trip failed.");

            CustomAttributeData obsolete = property.CustomAttributes.SingleOrDefault(
                attribute => string.Equals(attribute.AttributeType.FullName, typeof(ObsoleteAttribute).FullName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("StopOnManualMove must retain an Obsolete attribute.");
            if (obsolete.ConstructorArguments.Count < 2 || obsolete.ConstructorArguments[1].Value is not bool isError || isError)
                throw new InvalidOperationException("StopOnManualMove must use Obsolete(..., false).");
        }

        private static void ValidateFrozenCompatibilityMetadata(Assembly candidate)
        {
            Type stabilityEnum = candidate.GetType("DTMAPI.Abstractions.DtmApiStatus", throwOnError: true, ignoreCase: false)!;
            Type dispositionEnum = candidate.GetType("DTMAPI.Abstractions.DtmApiDisposition", throwOnError: true, ignoreCase: false)!;
            candidate.GetType("DTMAPI.Abstractions.DtmApiDispositionAttribute", throwOnError: true, ignoreCase: false);

            foreach (string typeName in FrozenCompatibilityTypeNames)
            {
                Type type = candidate.GetType(typeName, throwOnError: true, ignoreCase: false)!;
                CustomAttributeData obsolete = type.CustomAttributes.SingleOrDefault(attribute =>
                    string.Equals(attribute.AttributeType.FullName, typeof(ObsoleteAttribute).FullName, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(typeName + " must carry an Obsolete warning.");
                if (obsolete.ConstructorArguments.Count < 2 || obsolete.ConstructorArguments[1].Value is not bool isError || isError)
                    throw new InvalidOperationException(typeName + " must use Obsolete(..., false).");
                if (obsolete.ConstructorArguments[0].Value is not string message ||
                    !message.Contains("frozen", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(typeName + " must publish an explicit frozen warning.");

                CustomAttributeData stability = type.CustomAttributes.SingleOrDefault(attribute =>
                    string.Equals(attribute.AttributeType.FullName, "DTMAPI.Abstractions.DtmApiStatusAttribute", StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(typeName + " must publish DtmApiStatus metadata.");
                object stabilityValue = Enum.ToObject(stabilityEnum, stability.ConstructorArguments[0].Value!);
                if (!string.Equals(Enum.GetName(stabilityEnum, stabilityValue), "Experimental", StringComparison.Ordinal))
                    throw new InvalidOperationException(typeName + " must retain Experimental stability.");

                CustomAttributeData disposition = type.CustomAttributes.SingleOrDefault(attribute =>
                    string.Equals(attribute.AttributeType.FullName, "DTMAPI.Abstractions.DtmApiDispositionAttribute", StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(typeName + " must publish DtmApiDisposition metadata.");
                object dispositionValue = Enum.ToObject(dispositionEnum, disposition.ConstructorArguments[0].Value!);
                if (!string.Equals(Enum.GetName(dispositionEnum, dispositionValue), "Frozen", StringComparison.Ordinal))
                    throw new InvalidOperationException(typeName + " must publish the Frozen disposition.");
                CustomAttributeNamedArgument since = disposition.NamedArguments.SingleOrDefault(argument =>
                    string.Equals(argument.MemberName, "Since", StringComparison.Ordinal));
                if (!string.Equals(since.TypedValue.Value as string, "0.5.5", StringComparison.Ordinal))
                    throw new InvalidOperationException(typeName + " must publish Frozen since 0.5.5.");
            }
        }

        private static void ValidateStrongPlantingGunFrozenShape(Assembly candidate)
        {
            Type api = candidate.GetType(
                "DTMAPI.Abstractions.IStrongPlantingGunApi",
                throwOnError: true,
                ignoreCase: false)!;
            string[] actualMethods = api.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method =>
                    method.Name + "|" +
                    (method.ReturnType.FullName ?? method.ReturnType.Name) + "|" +
                    string.Join(",", method.GetParameters().Select(parameter =>
                        parameter.ParameterType.FullName ?? parameter.ParameterType.Name)))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            RequireExactSet(
                new[]
                {
                    "GetState|DTMAPI.Abstractions.StrongPlantingGunState|System.String",
                    "GetStatus|DTMAPI.Abstractions.BridgeFeatureStatus|System.String",
                    "Register|DTMAPI.Abstractions.StrongPlantingGunRegisterResult|DTMAPI.Abstractions.IManifest,DTMAPI.Abstractions.StrongPlantingGunOptions"
                },
                actualMethods,
                "IStrongPlantingGunApi exact frozen method shape");

            ValidateExactFrozenPropertyShape(
                candidate,
                "DTMAPI.Abstractions.StrongPlantingGunOptions",
                new[]
                {
                    "Enabled|System.Boolean",
                    "IncludeFertilizers|System.Boolean",
                    "IncludeFilms|System.Boolean",
                    "IncludeSeeds|System.Boolean",
                    "IncludeWater|System.Boolean",
                    "SlotCount|System.Int32",
                    "VerboseLogging|System.Boolean"
                });
            ValidateExactFrozenPropertyShape(
                candidate,
                "DTMAPI.Abstractions.StrongPlantingGunRegisterResult",
                new[]
                {
                    "Enabled|System.Boolean",
                    "FailureReason|System.String",
                    "Message|System.String",
                    "OwnerId|System.String",
                    "SlotCount|System.Int32",
                    "Success|System.Boolean",
                    "ToolHookInstalled|System.Boolean",
                    "UiHookInstalled|System.Boolean"
                });
            ValidateExactFrozenPropertyShape(
                candidate,
                "DTMAPI.Abstractions.StrongPlantingGunState",
                new[]
                {
                    "Enabled|System.Boolean",
                    "ExpandedGunCount|System.Int32",
                    "IsConfigured|System.Boolean",
                    "LastConsumedItemCount|System.Int32",
                    "LastFertilizerActions|System.Int32",
                    "LastFilmActions|System.Int32",
                    "LastMessage|System.String",
                    "LastSeedActions|System.Int32",
                    "LastVisitedEquipmentCount|System.Int32",
                    "LastWaterActions|System.Int32",
                    "OwnerId|System.String",
                    "SlotCount|System.Int32",
                    "Status|System.String",
                    "ToolHookInstalled|System.Boolean",
                    "UiHookInstalled|System.Boolean"
                });
        }

        private static void ValidateExactFrozenPropertyShape(
            Assembly candidate,
            string typeName,
            IReadOnlyCollection<string> expectedProperties)
        {
            Type type = candidate.GetType(typeName, throwOnError: true, ignoreCase: false)!;
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new MissingMethodException(typeName, ".ctor()");
            string[] actual = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(property =>
                {
                    if (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
                        throw new MissingMethodException(typeName, property.Name + " public getter/setter");
                    return property.Name + "|" + (property.PropertyType.FullName ?? property.PropertyType.Name);
                })
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            RequireExactSet(expectedProperties, actual, typeName + " exact frozen property shape");
        }

        private static LampCompatibilityReport ValidateLampCompatibilityShell(Assembly candidate)
        {
            Type[] lampTypes = RetainedLampTypeNames
                .Select(name => candidate.GetType(name, throwOnError: true, ignoreCase: false)!)
                .ToArray();
            int obsoleteTypeCount = 0;
            foreach (Type type in lampTypes)
            {
                CustomAttributeData obsolete = type.CustomAttributes.SingleOrDefault(attribute =>
                    string.Equals(attribute.AttributeType.FullName, typeof(ObsoleteAttribute).FullName, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(type.FullName + " must retain an Obsolete attribute.");
                if (obsolete.ConstructorArguments.Count < 2 || obsolete.ConstructorArguments[1].Value is not bool isError || isError)
                    throw new InvalidOperationException(type.FullName + " must use Obsolete(..., false).");
                obsoleteTypeCount++;

                CustomAttributeData status = type.CustomAttributes.SingleOrDefault(attribute =>
                    string.Equals(attribute.AttributeType.FullName, "DTMAPI.Abstractions.DtmApiStatusAttribute", StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(type.FullName + " must declare DtmApiStatus.Disabled.");
                if (status.ConstructorArguments.Count != 1)
                    throw new InvalidOperationException(type.FullName + " has an invalid DtmApiStatus attribute.");
                Type statusEnumType = candidate.GetType("DTMAPI.Abstractions.DtmApiStatus", throwOnError: true, ignoreCase: false)!;
                object enumValue = Enum.ToObject(statusEnumType, status.ConstructorArguments[0].Value!);
                if (!string.Equals(Enum.GetName(statusEnumType, enumValue), "Disabled", StringComparison.Ordinal))
                    throw new InvalidOperationException(type.FullName + " must be classified as DtmApiStatus.Disabled.");
            }

            Type optionsType = lampTypes.Single(type => string.Equals(type.FullName, RetainedLampTypeNames[1], StringComparison.Ordinal));
            object options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("LampManualToggleOptions default constructor returned null.");
            RequireBoolProperty(options, "Enabled", expected: true);
            RequireEmptyStringListProperty(options, "EquipmentIds");
            RequireBoolProperty(options, "VerboseLogging", expected: false);

            Type resultType = lampTypes.Single(type => string.Equals(type.FullName, RetainedLampTypeNames[2], StringComparison.Ordinal));
            object result = Activator.CreateInstance(resultType)
                ?? throw new InvalidOperationException("LampManualToggleRegisterResult default constructor returned null.");
            foreach (string property in new[] { "Success", "Enabled", "HookInstalled" })
                RequireBoolProperty(result, property, expected: false);
            foreach (string property in new[] { "OwnerId", "FailureReason", "Message", "LastTouchedEquipmentId", "LastToggledEquipmentId" })
                RequireEmptyStringProperty(result, property);
            RequireEmptyStringListProperty(result, "RegisteredEquipmentIds");
            RequireIntProperty(result, "SessionOverrideCount", expected: 0);

            Type stateType = lampTypes.Single(type => string.Equals(type.FullName, RetainedLampTypeNames[3], StringComparison.Ordinal));
            object state = Activator.CreateInstance(stateType)
                ?? throw new InvalidOperationException("LampManualToggleState default constructor returned null.");
            foreach (string property in new[] { "IsConfigured", "Enabled", "HookInstalled", "LastToggledValue" })
                RequireBoolProperty(state, property, expected: false);
            foreach (string property in new[] { "OwnerId", "Status", "FailureReason", "LastMessage", "LastTouchedEquipmentId", "LastToggledEquipmentId" })
                RequireEmptyStringProperty(state, property);
            RequireEmptyStringListProperty(state, "RegisteredEquipmentIds");
            RequireIntProperty(state, "SessionOverrideCount", expected: 0);

            return new LampCompatibilityReport(
                TypeCount: lampTypes.Length,
                ObsoleteTypeCount: obsoleteTypeCount,
                ObsoleteIsError: false,
                DisabledStatusAttributes: true,
                HistoricalDefaults: true);
        }

        private static object? GetRequiredPropertyValue(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?? throw new MissingMemberException(instance.GetType().FullName, propertyName);
            if (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
                throw new MissingMethodException(instance.GetType().FullName, propertyName + " public getter/setter");
            return property.GetValue(instance);
        }

        private static void RequireBoolProperty(object instance, string propertyName, bool expected)
        {
            if (GetRequiredPropertyValue(instance, propertyName) is not bool actual || actual != expected)
                throw new InvalidOperationException(instance.GetType().FullName + "." + propertyName + " historical default mismatch.");
        }

        private static void RequireIntProperty(object instance, string propertyName, int expected)
        {
            if (GetRequiredPropertyValue(instance, propertyName) is not int actual || actual != expected)
                throw new InvalidOperationException(instance.GetType().FullName + "." + propertyName + " historical default mismatch.");
        }

        private static void RequireEmptyStringProperty(object instance, string propertyName)
        {
            if (GetRequiredPropertyValue(instance, propertyName) is not string actual || actual.Length != 0)
                throw new InvalidOperationException(instance.GetType().FullName + "." + propertyName + " historical default must be an empty string.");
        }

        private static void RequireEmptyStringListProperty(object instance, string propertyName)
        {
            object? value = GetRequiredPropertyValue(instance, propertyName);
            if (value is not System.Collections.IEnumerable enumerable || enumerable.Cast<object?>().Any())
                throw new InvalidOperationException(instance.GetType().FullName + "." + propertyName + " historical default must be a non-null empty list.");
        }

        private static Type[] GetAllTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                string details = string.Join(Environment.NewLine, exception.LoaderExceptions.Where(value => value != null).Select(value => value!.ToString()));
                throw new InvalidOperationException("Unable to resolve every type in " + assembly.Location + Environment.NewLine + details, exception);
            }
        }

        private static void ForceSignatureResolution(IEnumerable<Type> types)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in types)
            {
                _ = type.BaseType;
                _ = type.GetInterfaces();
                foreach (FieldInfo field in type.GetFields(flags))
                    _ = field.FieldType;
                foreach (ConstructorInfo constructor in type.GetConstructors(flags))
                    _ = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    _ = method.ReturnType;
                    _ = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
                }
                foreach (PropertyInfo property in type.GetProperties(flags))
                {
                    _ = property.PropertyType;
                    _ = property.GetIndexParameters().Select(parameter => parameter.ParameterType).ToArray();
                }
                foreach (EventInfo eventInfo in type.GetEvents(flags))
                    _ = eventInfo.EventHandlerType;
            }
        }

        private static bool IsExternallyVisible(Type type)
        {
            if (!type.IsNested)
                return type.IsPublic;
            bool nestedVisible = type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem;
            return nestedVisible && type.DeclaringType != null && IsExternallyVisible(type.DeclaringType);
        }

        private static bool IsPublicApiMethod(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

        private static bool IsPublicApiField(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

        private static bool IsPublicApiProperty(PropertyInfo property) =>
            (property.GetMethod != null && IsPublicApiMethod(property.GetMethod)) ||
            (property.SetMethod != null && IsPublicApiMethod(property.SetMethod));

        private static bool IsPublicApiEvent(EventInfo eventInfo) =>
            (eventInfo.AddMethod != null && IsPublicApiMethod(eventInfo.AddMethod)) ||
            (eventInfo.RemoveMethod != null && IsPublicApiMethod(eventInfo.RemoveMethod));

        private static string FormatMethod(MethodBase method)
        {
            string declaringType = FormatType(method.DeclaringType!);
            string genericArity = method.IsGenericMethod ? "`" + method.GetGenericArguments().Length : string.Empty;
            string parameters = string.Join(",", method.GetParameters().Select(FormatParameter));
            string returnType = method is MethodInfo methodInfo ? FormatType(methodInfo.ReturnType) : "System.Void";
            return Accessibility(method) + "|" + (method.IsStatic ? "static" : "instance") + "|" +
                   declaringType + "|" + method.Name + genericArity + "|(" + parameters + ")|" + returnType;
        }

        private static string FormatParameter(ParameterInfo parameter)
        {
            string modifier = parameter.IsOut
                ? "out:"
                : parameter.ParameterType.IsByRef && parameter.IsIn
                    ? "in:"
                    : parameter.ParameterType.IsByRef
                        ? "ref:"
                        : string.Empty;
            return modifier + FormatType(parameter.ParameterType);
        }

        private static string FormatType(Type type)
        {
            if (type.IsByRef)
                return FormatType(type.GetElementType()!) + "&";
            if (type.IsPointer)
                return FormatType(type.GetElementType()!) + "*";
            if (type.IsArray)
                return FormatType(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            if (type.IsGenericParameter)
                return (type.DeclaringMethod != null ? "!!" : "!") + type.GenericParameterPosition;
            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                string definitionName = definition.FullName ?? definition.Name;
                return definitionName + "[" + string.Join(",", type.GetGenericArguments().Select(FormatType)) + "]";
            }
            return type.FullName ?? type.Name;
        }

        private static string GetTypeKind(Type type)
        {
            if (type.IsInterface)
                return "interface";
            if (type.IsEnum)
                return "enum:" + FormatType(Enum.GetUnderlyingType(type));
            if (type.IsValueType)
                return "struct";
            if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
                return "delegate";
            return type.IsAbstract && type.IsSealed ? "static-class" : type.IsAbstract ? "abstract-class" : type.IsSealed ? "sealed-class" : "class";
        }

        private static string Accessibility(MethodBase method) => method.IsPublic ? "public" : method.IsFamilyOrAssembly ? "protected-internal" : "protected";

        private static string Accessibility(FieldInfo field) => field.IsPublic ? "public" : field.IsFamilyOrAssembly ? "protected-internal" : "protected";

        private static void RequireEqual(string expected, string actual, string description)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(description + " mismatch: expected '" + expected + "', actual '" + actual + "'.");
        }

        private static void RequireExactSet(IEnumerable<string> expected, IEnumerable<string> actual, string description)
        {
            string[] expectedValues = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] actualValues = actual.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!expectedValues.SequenceEqual(actualValues, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    description + " mismatch: expected [" + string.Join(",", expectedValues) +
                    "], actual [" + string.Join(",", actualValues) + "].");
            }
        }

        private sealed record AssemblySnapshot(string Name, string AssemblyVersion, string FileVersion, HashSet<string> PublicSurface);

        private sealed record MemberReferenceInfo(int MetadataToken);

        private sealed record ConsumerBindingResult(
            string ConsumerReferencedAbstractionsVersion,
            int ConsumerTypeCount,
            int ResolvedSetterReferenceCount,
            LampCompatibilityReport LampCompatibility);

        private sealed record ArtifactReport(string FileName, string Sha256, string AssemblyVersion, string FileVersion, int PublicApiCount);

        private sealed record ConsumerReport(
            string FileName,
            string Sha256,
            string ReferencedAbstractionsVersion,
            int MetadataStopOnManualMoveSetterReferences,
            int ResolvedStopOnManualMoveSetterReferences,
            int TypeCount);

        private sealed record StopOnManualMoveReport(bool Getter, bool Setter, bool DefaultValue, bool RoundTrip, bool ObsoleteIsError);

        private sealed record LampCompatibilityReport(
            int TypeCount,
            int ObsoleteTypeCount,
            bool ObsoleteIsError,
            bool DisabledStatusAttributes,
            bool HistoricalDefaults);

        private sealed record KnownCompatibilityConsumerContract(
            string ProductId,
            string Sha256,
            string FrozenInterfaceType,
            string[] RequiredMemberReferences);

        private sealed record KnownExternalConsumerContract(
            string WorkshopId,
            string FileName,
            string Sha256,
            string ExpectedAbstractionsReferenceVersion);

        private sealed record PublicProductScanResult(
            string ProductId,
            string FileName,
            string Sha256,
            string[] LampMemberReferences,
            bool GameBridgeProviderUniqueIdPresent,
            string[] FrozenCompatibilityMemberReferences,
            string AssemblyName,
            string AssemblyVersion,
            string ReferencedAbstractionsVersion,
            int MetadataAbstractionsMemberReferenceCount,
            int ResolvedAbstractionsMemberReferenceCount,
            string[] AbstractionsMemberReferences);

        private sealed record RetainedConsumerMetadata(
            string AssemblyName,
            string AssemblyVersion,
            string AbstractionsReferenceVersion,
            int[] MemberReferenceTokens,
            string[] MemberReferenceNames);

        private sealed record ExternalConsumerScanResult(
            string WorkshopId,
            string FileName,
            string Sha256,
            string AssemblyName,
            string AssemblyVersion,
            string ReferencedAbstractionsVersion,
            int MetadataAbstractionsMemberReferenceCount,
            int ResolvedAbstractionsMemberReferenceCount,
            string[] AbstractionsMemberReferences);

        private sealed record ManagedAssemblyMetadata(
            string Name,
            string Version,
            string[] AssemblyReferences,
            HashSet<string> TypeNames,
            ManagedMethodMetadata[] Methods);

        private sealed record ManagedMethodMetadata(
            string DeclaringType,
            string Name,
            int IlSize,
            int ParameterCount,
            bool IsStatic,
            bool IsVisibleToMandatoryAssembly);

        private sealed record CompatibilityHostMetadataReport(
            string MandatoryGameBridgeFileName,
            string MandatoryGameBridgeSha256,
            bool MandatoryHostAssemblyReference,
            int MandatoryHeavyExecutorMarkerCount,
            int MandatoryCompatibilityServiceIlBytes,
            string HostFileName,
            string HostSha256,
            string HostAssemblyName,
            string HostAssemblyVersion,
            string TargetFramework,
            int HostHeavyExecutorMarkerCount,
            int HostCompatibilityServiceIlBytes,
            string FactoryTypeName,
            string FactoryMethodName,
            int FactoryParameterCount);

        private sealed record AbiGateReport(
            int SchemaVersion,
            bool OverallPass,
            string RuntimeValidation,
            ArtifactReport Baseline,
            ArtifactReport Candidate,
            ConsumerReport Consumer,
            int RemovedPublicApiCount,
            string[] RemovedPublicApi,
            string[] RetainedLampTypeNames,
            int RetainedPublicProductCount,
            int RetainedPublicProductLampMemberReferenceCount,
            PublicProductScanResult[] RetainedPublicProducts,
            int RetainedExternalConsumerCount,
            ExternalConsumerScanResult[] RetainedExternalConsumers,
            StopOnManualMoveReport StopOnManualMove,
            LampCompatibilityReport LampCompatibility,
            CompatibilityHostMetadataReport? CompatibilityHost);

        private sealed record SyntheticAbiContract(
            int SchemaVersion,
            string ContractId,
            string ExpectedAssemblyName,
            string ExpectedAssemblyVersion,
            Dictionary<string, string> TypeSurfaceSha256);

        private sealed record SyntheticAbiReport(
            int SchemaVersion,
            bool OverallPass,
            string ContractId,
            string CandidateFileName,
            string CandidateSha256,
            string CandidateAssemblyVersion,
            IReadOnlyDictionary<string, string> TypeSurfaceSha256,
            bool StopOnManualMoveDefault,
            bool StopOnManualMoveObsoleteIsError,
            LampCompatibilityReport LampCompatibility,
            bool PrivateRetainedArtifactsUsed);

        private sealed class ArtifactLoadContext : AssemblyLoadContext
        {
            public ArtifactLoadContext()
                : base(isCollectible: true)
            {
            }

            protected override Assembly? Load(AssemblyName assemblyName) => null;
        }

        private sealed class CandidateBindingLoadContext : AssemblyLoadContext
        {
            private readonly string _candidatePath;
            private readonly string _consumerDirectory;
            private Assembly? _candidate;

            public CandidateBindingLoadContext(string candidatePath, string consumerDirectory)
                : base(isCollectible: true)
            {
                _candidatePath = candidatePath;
                _consumerDirectory = consumerDirectory;
            }

            public Assembly LoadCandidate()
            {
                _candidate ??= LoadFromAssemblyPath(_candidatePath);
                return _candidate;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                if (string.Equals(assemblyName.Name, AbstractionsAssemblyName, StringComparison.Ordinal))
                    return LoadCandidate();

                string adjacentPath = Path.Combine(_consumerDirectory, (assemblyName.Name ?? string.Empty) + ".dll");
                if (File.Exists(adjacentPath))
                    return LoadFromAssemblyPath(adjacentPath);
                return null;
            }
        }
    }
}
