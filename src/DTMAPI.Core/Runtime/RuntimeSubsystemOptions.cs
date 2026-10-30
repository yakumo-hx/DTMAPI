using System;
using System.IO;
using System.Runtime.Serialization;
using DTMAPI.Core.Json;

namespace DTMAPI.Core.Runtime
{
    // Retain the config path and names of active Runtime subsystems. Retired
    // diagnostic scaffolding is ignored when reading an older configuration.
    [DataContract(Name = "RefactorScaffoldOptions")]
    internal sealed class RuntimeSubsystemOptions
    {
        public const string FeatureStatusId = "Refactor.ScaffoldFlags";
        private const string ConfigFileName = "refactor-scaffold.json";

        [DataMember(Name = "LifecycleObservation")]
        public bool? LifecycleObservationValue { get; set; }

        [DataMember(Name = "ResourceLifecycleLedger")]
        public bool? ResourceLifecycleLedgerValue { get; set; }

        [DataMember(Name = "ResourceLifecycleCleanup")]
        public bool? ResourceLifecycleCleanupValue { get; set; }

        [DataMember(Name = "HookInstallScheduler")]
        public bool? HookInstallSchedulerValue { get; set; }

        [DataMember(Name = "HookStatusQueue")]
        public bool? HookStatusQueueValue { get; set; }

        [DataMember(Name = "EventMainThreadBoundary")]
        public bool? EventMainThreadBoundaryValue { get; set; }

        [DataMember(Name = "EventHandlerTimingDiagnostics")]
        public bool? EventHandlerTimingDiagnosticsValue { get; set; }

        [DataMember(Name = "HookReadinessLayers")]
        public bool? HookReadinessLayersValue { get; set; }

        [DataMember(Name = "SaveLoadRequestCoordinator")]
        public bool? SaveLoadRequestCoordinatorValue { get; set; }

        [DataMember(Name = "ModOwnerLedger")]
        public bool? ModOwnerLedgerValue { get; set; }

        [DataMember(Name = "ModLoadTransaction")]
        public bool? ModLoadTransactionValue { get; set; }

        [DataMember(Name = "OwnerBoundInput")]
        public bool? OwnerBoundInputValue { get; set; }

        [DataMember(Name = "EventHandlerQuarantine")]
        public bool? EventHandlerQuarantineValue { get; set; }

        [DataMember(Name = "ConfigPreviewAudit")]
        public bool? ConfigPreviewAuditValue { get; set; }

        [DataMember(Name = "GameBridgeFeatureContracts")]
        public bool? GameBridgeFeatureContractsValue { get; set; }

        [DataMember(Name = "GameBridgeFinalHealthSnapshot")]
        public bool? GameBridgeFinalHealthSnapshotValue { get; set; }

        [DataMember(Name = "ContentManifestRegistry")]
        public bool? ContentManifestRegistryValue { get; set; }

        public bool LifecycleObservation => LifecycleObservationValue ?? true;




        public bool ResourceLifecycleLedger => ResourceLifecycleLedgerValue ?? true;

        public bool ResourceLifecycleCleanup => ResourceLifecycleCleanupValue ?? true;


        public bool HookInstallScheduler => HookInstallSchedulerValue ?? true;

        public bool HookStatusQueue => HookStatusQueueValue ?? true;

        public bool EventMainThreadBoundary => EventMainThreadBoundaryValue ?? true;

        public bool EventHandlerTimingDiagnostics => EventHandlerTimingDiagnosticsValue ?? false;

        public bool HookReadinessLayers => HookReadinessLayersValue ?? true;

        public bool SaveLoadRequestCoordinator => SaveLoadRequestCoordinatorValue ?? true;

        public bool ModOwnerLedger => ModOwnerLedgerValue ?? true;

        public bool ModLoadTransaction => ModLoadTransactionValue ?? true;

        public bool OwnerBoundInput => OwnerBoundInputValue ?? true;

        public bool EventHandlerQuarantine => EventHandlerQuarantineValue ?? true;

        public bool ConfigPreviewAudit => ConfigPreviewAuditValue ?? true;

        public bool GameBridgeFeatureContracts => GameBridgeFeatureContractsValue ?? true;

        public bool GameBridgeFinalHealthSnapshot => GameBridgeFinalHealthSnapshotValue ?? true;

        public bool ContentManifestRegistry => ContentManifestRegistryValue ?? true;

        public static RuntimeSubsystemOptions Default()
        {
            return new RuntimeSubsystemOptions
            {
                LifecycleObservationValue = true,
                ResourceLifecycleLedgerValue = true,
                ResourceLifecycleCleanupValue = true,
                HookInstallSchedulerValue = true,
                HookStatusQueueValue = true,
                EventMainThreadBoundaryValue = true,
                EventHandlerTimingDiagnosticsValue = false,
                HookReadinessLayersValue = true,
                SaveLoadRequestCoordinatorValue = true,
                ModOwnerLedgerValue = true,
                ModLoadTransactionValue = true,
                OwnerBoundInputValue = true,
                EventHandlerQuarantineValue = true,
                ConfigPreviewAuditValue = true,
                GameBridgeFeatureContractsValue = true,
                GameBridgeFinalHealthSnapshotValue = true,
                ContentManifestRegistryValue = true
            };
        }

        public static string GetConfigPath(RuntimePaths paths) => Path.Combine(paths.ConfigPath, ConfigFileName);

        public static RuntimeSubsystemOptions Load(RuntimePaths paths, out string summary, out string warning)
        {
            string path = GetConfigPath(paths);
            RuntimeSubsystemOptions options = Default();
            warning = string.Empty;
            bool createdDefault = false;
            bool legacyFeatureBucketOptionDetected = false;
            bool removedLegacyFeatureBucketOption = false;

            try
            {
                if (File.Exists(path))
                {
                    string serialized = File.ReadAllText(path);
                    legacyFeatureBucketOptionDetected =
                        serialized.IndexOf("\"GameBridgeFeatureUpdateBuckets\"", StringComparison.OrdinalIgnoreCase) >= 0;
                    options = JsonFile.Read<RuntimeSubsystemOptions>(path) ?? Default();
                    options.NormalizeMissingValues();
                    if (legacyFeatureBucketOptionDetected)
                    {
                        try
                        {
                            JsonFile.Write(path, options);
                            removedLegacyFeatureBucketOption = true;
                        }
                        catch (Exception ex)
                        {
                            warning = "Loaded " + path + " but failed to remove the retired GameBridge feature bucket option. " +
                                ex.GetType().Name + ": " + ex.Message;
                        }
                    }
                }
                else
                {
                    JsonFile.Write(path, options);
                    createdDefault = true;
                }
            }
            catch (Exception ex)
            {
                warning = "Failed to read " + path + "; using safe defaults. " + ex.GetType().Name + ": " + ex.Message;
                options = Default();
            }

            options.ApplyEnvironmentOverrides();
            summary = options.ToSummary() +
                "; configPath=" + path +
                "; createdDefault=" + (createdDefault ? "true" : "false") +
                "; legacyFeatureBucketOptionDetected=" + (legacyFeatureBucketOptionDetected ? "true" : "false") +
                "; removedLegacyFeatureBucketOption=" + (removedLegacyFeatureBucketOption ? "true" : "false");
            return options;
        }

        public string ToSummary()
        {
            return "LifecycleObservation=" + (LifecycleObservation ? "true" : "false") +
                "; ResourceLifecycleLedger=" + (ResourceLifecycleLedger ? "true" : "false") +
                "; ResourceLifecycleCleanup=" + (ResourceLifecycleCleanup ? "true" : "false") +
                "; HookInstallScheduler=" + (HookInstallScheduler ? "true" : "false") +
                "; HookStatusQueue=" + (HookStatusQueue ? "true" : "false") +
                "; EventMainThreadBoundary=" + (EventMainThreadBoundary ? "true" : "false") +
                "; EventHandlerTimingDiagnostics=" + (EventHandlerTimingDiagnostics ? "true" : "false") +
                "; HookReadinessLayers=" + (HookReadinessLayers ? "true" : "false") +
                "; SaveLoadRequestCoordinator=" + (SaveLoadRequestCoordinator ? "true" : "false") +
                "; ModOwnerLedger=" + (ModOwnerLedger ? "true" : "false") +
                "; ModLoadTransaction=" + (ModLoadTransaction ? "true" : "false") +
                "; OwnerBoundInput=" + (OwnerBoundInput ? "true" : "false") +
                "; EventHandlerQuarantine=" + (EventHandlerQuarantine ? "true" : "false") +
                "; ConfigPreviewAudit=" + (ConfigPreviewAudit ? "true" : "false") +
                "; GameBridgeFeatureContracts=" + (GameBridgeFeatureContracts ? "true" : "false") +
                "; GameBridgeFinalHealthSnapshot=" + (GameBridgeFinalHealthSnapshot ? "true" : "false") +
                "; ContentManifestRegistry=" + (ContentManifestRegistry ? "true" : "false");
        }

        private void NormalizeMissingValues()
        {
            LifecycleObservationValue = LifecycleObservation;
            ResourceLifecycleLedgerValue = ResourceLifecycleLedger;
            ResourceLifecycleCleanupValue = ResourceLifecycleCleanup;
            HookInstallSchedulerValue = HookInstallScheduler;
            HookStatusQueueValue = HookStatusQueue;
            EventMainThreadBoundaryValue = EventMainThreadBoundary;
            EventHandlerTimingDiagnosticsValue = EventHandlerTimingDiagnostics;
            HookReadinessLayersValue = HookReadinessLayers;
            SaveLoadRequestCoordinatorValue = SaveLoadRequestCoordinator;
            ModOwnerLedgerValue = ModOwnerLedger;
            ModLoadTransactionValue = ModLoadTransaction;
            OwnerBoundInputValue = OwnerBoundInput;
            EventHandlerQuarantineValue = EventHandlerQuarantine;
            ConfigPreviewAuditValue = ConfigPreviewAudit;
            GameBridgeFeatureContractsValue = GameBridgeFeatureContracts;
            GameBridgeFinalHealthSnapshotValue = GameBridgeFinalHealthSnapshot;
            ContentManifestRegistryValue = ContentManifestRegistry;
        }

        private void ApplyEnvironmentOverrides()
        {
            LifecycleObservationValue = ReadBoolOverride("DTMAPI_REFACTOR_LIFECYCLE_OBSERVATION", LifecycleObservationValue);
            ResourceLifecycleLedgerValue = ReadBoolOverride("DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_LEDGER", ResourceLifecycleLedgerValue);
            ResourceLifecycleCleanupValue = ReadBoolOverride("DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_CLEANUP", ResourceLifecycleCleanupValue);
            HookInstallSchedulerValue = ReadBoolOverride("DTMAPI_REFACTOR_HOOK_INSTALL_SCHEDULER", HookInstallSchedulerValue);
            HookStatusQueueValue = ReadBoolOverride("DTMAPI_REFACTOR_HOOK_STATUS_QUEUE", HookStatusQueueValue);
            EventMainThreadBoundaryValue = ReadBoolOverride("DTMAPI_REFACTOR_EVENT_MAIN_THREAD_BOUNDARY", EventMainThreadBoundaryValue);
            EventHandlerTimingDiagnosticsValue = ReadBoolOverride("DTMAPI_REFACTOR_EVENT_HANDLER_TIMING_DIAGNOSTICS", EventHandlerTimingDiagnosticsValue);
            HookReadinessLayersValue = ReadBoolOverride("DTMAPI_REFACTOR_HOOK_READINESS_LAYERS", HookReadinessLayersValue);
            SaveLoadRequestCoordinatorValue = ReadBoolOverride("DTMAPI_REFACTOR_SAVELOAD_REQUEST_COORDINATOR", SaveLoadRequestCoordinatorValue);
            ModOwnerLedgerValue = ReadBoolOverride("DTMAPI_REFACTOR_MOD_OWNER_LEDGER", ModOwnerLedgerValue);
            ModLoadTransactionValue = ReadBoolOverride("DTMAPI_REFACTOR_MOD_LOAD_TRANSACTION", ModLoadTransactionValue);
            OwnerBoundInputValue = ReadBoolOverride("DTMAPI_REFACTOR_OWNER_BOUND_INPUT", OwnerBoundInputValue);
            EventHandlerQuarantineValue = ReadBoolOverride("DTMAPI_REFACTOR_EVENT_HANDLER_QUARANTINE", EventHandlerQuarantineValue);
            ConfigPreviewAuditValue = ReadBoolOverride("DTMAPI_REFACTOR_CONFIG_PREVIEW_AUDIT", ConfigPreviewAuditValue);
            GameBridgeFeatureContractsValue = ReadBoolOverride("DTMAPI_REFACTOR_GAMEBRIDGE_FEATURE_CONTRACTS", GameBridgeFeatureContractsValue);
            GameBridgeFinalHealthSnapshotValue = ReadBoolOverride("DTMAPI_REFACTOR_GAMEBRIDGE_FINAL_HEALTH_SNAPSHOT", GameBridgeFinalHealthSnapshotValue);
            ContentManifestRegistryValue = ReadBoolOverride("DTMAPI_REFACTOR_CONTENT_MANIFEST_REGISTRY", ContentManifestRegistryValue);
            NormalizeMissingValues();
        }

        private static bool? ReadBoolOverride(string name, bool? current)
        {
            string value = Environment.GetEnvironmentVariable(name) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return current;

            value = value.Trim();
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase))
                return true;
            if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("off", StringComparison.OrdinalIgnoreCase))
                return false;

            return current;
        }
    }
}
