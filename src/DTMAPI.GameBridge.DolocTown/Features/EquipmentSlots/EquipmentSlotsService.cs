#pragma warning disable CS0618 // Frozen IEquipmentSlotsApi proxy.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Mandatory thin proxy for the frozen IEquipmentSlotsApi ABI.
    /// Product policy, native hooks, UI, and sidecar execution live in the dormant Compatibility Host.
    /// </summary>
    internal sealed class EquipmentSlotsService : IEquipmentSlotsApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly DtmApiRuntime runtime;
        private readonly string configPath;
        private string coldStorageSource = string.Empty;

        internal EquipmentSlotsService(DtmApiRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            this.runtime = runtime;
            broker = CompatibilityHostBroker.For(runtime);
            configPath = runtime.Paths.ConfigPath;
        }

        private object Backend => broker.GetService("EquipmentSlots", Array.Empty<object>());

        public EquipmentSlotsRegisterResult RegisterSlots(IManifest owner, EquipmentSlotsOptions options) =>
            CompatibilityHostBroker.Invoke<EquipmentSlotsRegisterResult>(
                Backend,
                "RegisterSlots",
                owner,
                NormalizeEquipmentSlotsOptions(options));

        public System.Collections.Generic.IReadOnlyList<EquipmentSlotInfo> GetSlots(string uniqueId) =>
            CompatibilityHostBroker.Invoke<System.Collections.Generic.IReadOnlyList<EquipmentSlotInfo>>(
                Backend,
                "GetSlots",
                uniqueId);

        public EquipmentSlotEquipResult EquipExtraSlot(IManifest owner, string slotId, string itemId) =>
            CompatibilityHostBroker.Invoke<EquipmentSlotEquipResult>(
                Backend,
                "EquipExtraSlot",
                owner,
                slotId,
                itemId);

        public EquipmentSlotEquipResult UnequipExtraSlot(IManifest owner, string slotId, string reason) =>
            CompatibilityHostBroker.Invoke<EquipmentSlotEquipResult>(
                Backend,
                "UnequipExtraSlot",
                owner,
                slotId,
                reason);

        public EquipmentSlotsState GetState(string uniqueId) =>
            CompatibilityHostBroker.Invoke<EquipmentSlotsState>(Backend, "GetState", uniqueId);

        public EquipmentSlotsRecoveryResult RecoverExtraSlotItems(IManifest owner, string reason) =>
            CompatibilityHostBroker.Invoke<EquipmentSlotsRecoveryResult>(
                Backend,
                "RecoverExtraSlotItems",
                owner,
                reason);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);

        internal void SaveLoaded(bool isNewGame)
        {
            if (broker.TryGetService("EquipmentSlots", out object? backend) && backend != null)
            {
                CompatibilityHostBroker.Invoke(backend, "NotifyEquipmentSlotsSaveLoaded", isNewGame);
                CompatibilityHostBroker.Invoke(
                    backend,
                    "RecoverOrphanEquipmentSlotsIfNeeded");
                return;
            }

            if (!HasColdCompatibilityStorage())
                return;

            runtime.RuntimeMonitor.Log(
                "EquipmentSlots cold compatibility demand detected source=" +
                (string.IsNullOrWhiteSpace(coldStorageSource) ? "Sidecar" : coldStorageSource) +
                ".");
            CompatibilityHostBroker.Invoke(Backend, "NotifyEquipmentSlotsSaveLoaded", isNewGame);
            CompatibilityHostBroker.Invoke(Backend, "RecoverOrphanEquipmentSlotsIfNeeded");
        }

        internal void SaveSaved(int slot) =>
            TryInvokeVoid("NotifyEquipmentSlotsSaveSaved", (int?)slot);

        internal void SaveSaving(int slot) =>
            TryInvokeVoid("NotifyEquipmentSlotsSaveSaving", (int?)slot);

        internal void ReturnedToTitle() =>
            TryInvokeVoid("NotifyEquipmentSlotsReturnedToTitle");

        internal void EnvironmentReset(string reason) =>
            TryInvokeVoid("NotifyEquipmentSlotsEnvironmentReset", reason ?? string.Empty);

        internal void UpdateCompatibilityUi() =>
            TryInvokeVoid("UpdateDemandedEquipmentSlotsUi");

        internal bool UpdateColdRecovery() =>
            TryInvoke("RecoverOrphanEquipmentSlotsIfNeeded", false);

        internal int RemoveOwner(string ownerId, string reason) =>
            TryInvoke("RemoveOwner", 0, ownerId, reason);

        internal int CountOwnerResources(string ownerId) =>
            TryInvoke("CountOwnerResources", 0, ownerId);

        internal string GetLifecycleSummary() =>
            TryInvoke(
                "GetEquipmentSlotsLifecycleSummary",
                "equipmentClones=0, equipmentBinders=0, equipmentRendered=false, equipmentStorageOwners=0, equipmentDirtyOwners=0, equipmentEntries=0");

        internal string GetUiOwnerObjectGraphSummary() =>
            TryInvoke(
                "GetEquipmentSlotsUiOwnerObjectGraphSummary",
                "DTMAPI.EquipmentSlots={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}");

        internal static EquipmentSlotsOptions NormalizeEquipmentSlotsOptions(EquipmentSlotsOptions? options)
        {
            options ??= new EquipmentSlotsOptions();
            return new EquipmentSlotsOptions
            {
                Enabled = options.Enabled,
                ExtraAttributeSlots = Math.Max(0, Math.Min(24, options.ExtraAttributeSlots)),
                SlotIdPrefix = string.IsNullOrWhiteSpace(options.SlotIdPrefix) ? "dtmapi.extra" : options.SlotIdPrefix,
                PreserveVanillaVisualSlots = options.PreserveVanillaVisualSlots,
                ExtraSlotsAffectVisuals = options.ExtraSlotsAffectVisuals,
                SafeUnequipOnDisable = options.SafeUnequipOnDisable,
                AutoRecoverOnMissingMod = options.AutoRecoverOnMissingMod,
                VerboseLogging = options.VerboseLogging
            };
        }

        internal bool HasColdCompatibilityStorage()
        {
            try
            {
                string[] candidates =
                    FindColdCompatibilityStorageCandidates(
                        configPath,
                        ownerId =>
                            runtime.ModRegistry.IsLoaded(
                                ownerId));
                bool coldStoragePresent =
                    candidates.Length != 0;
                coldStorageSource =
                    coldStoragePresent
                        ? DetectColdStorageSource(
                            candidates[0])
                        : string.Empty;
                if (coldStoragePresent)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                coldStorageSource = string.Empty;
                runtime.RuntimeMonitor.Log(
                    "EquipmentSlots cold compatibility probe failed and will be retried at the next lifecycle boundary: " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message,
                    LogLevel.Warn);
                return false;
            }
        }

        internal static string[]
            FindColdCompatibilityStorageCandidates(
                string configRoot,
                Func<string, bool> isOwnerLoaded)
        {
            if (string.IsNullOrWhiteSpace(configRoot))
                return Array.Empty<string>();
            if (isOwnerLoaded == null)
                throw new ArgumentNullException(nameof(isOwnerLoaded));

            var candidates = new List<string>();
            if (Directory.Exists(configRoot))
            {
                candidates.AddRange(
                    Directory.GetFiles(
                        configRoot,
                        "equipment-slots-*.json",
                        SearchOption.TopDirectoryOnly));
                candidates.AddRange(
                    Directory.GetFiles(
                        configRoot,
                        "equipment-slots-*.json.previous",
                        SearchOption.TopDirectoryOnly));
                candidates.AddRange(
                    Directory.GetFiles(
                        configRoot,
                        "equipment-slots-*.json.migration-capture-*",
                        SearchOption.TopDirectoryOnly));
                candidates.AddRange(
                    Directory.GetFiles(
                        configRoot,
                        "equipment-slots-*.json.migrated-product-v3-*",
                        SearchOption.TopDirectoryOnly));
            }

            string protectedRoot =
                Path.Combine(
                    configRoot,
                    "protected-items",
                    "equipment-slots");
            if (Directory.Exists(protectedRoot))
            {
                candidates.AddRange(
                    Directory.GetFiles(
                        protectedRoot,
                        "equipment-slots-*.json",
                        SearchOption.AllDirectories));
                candidates.AddRange(
                    Directory.GetFiles(
                        protectedRoot,
                        "equipment-slots-*.json.previous",
                        SearchOption.AllDirectories));
                candidates.AddRange(
                    Directory.GetFiles(
                        protectedRoot,
                        "*.flat.json",
                        SearchOption.AllDirectories)
                    .Where(IsCanonicalLegacyMigrationBackup));
            }

            string migrationClaimRoot =
                Path.Combine(
                    configRoot,
                    ".equipment-slot-migration-claims");
            if (Directory.Exists(migrationClaimRoot))
            {
                candidates.AddRange(
                    Directory.GetFiles(
                        migrationClaimRoot,
                        "*.json",
                        SearchOption.AllDirectories));
                candidates.AddRange(
                    Directory.GetFiles(
                        migrationClaimRoot,
                        "*.transition-*",
                        SearchOption.AllDirectories));
            }

            return candidates
                .Where(path =>
                    !TryGetColdStorageOwner(
                        path,
                        out string ownerId) ||
                    !isOwnerLoaded(ownerId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool TryGetColdStorageOwner(
            string path,
            out string ownerId)
        {
            if (TryGetStorageOwnerFromPath(
                path,
                out ownerId))
            {
                return true;
            }
            ownerId = string.Empty;
            try
            {
                using (var stream =
                    File.OpenRead(path))
                {
                    var serializer =
                        new DataContractJsonSerializer(
                            typeof(ColdStorageOwnerProbe));
                    var probe =
                        serializer.ReadObject(stream) as
                            ColdStorageOwnerProbe;
                    ownerId =
                        probe?.OwnerId?.Trim()
                        ?? string.Empty;
                    return ownerId.Length != 0;
                }
            }
            catch
            {
                ownerId = string.Empty;
                return false;
            }
        }

        private static bool IsCanonicalLegacyMigrationBackup(
            string path)
        {
            string? directory =
                Path.GetDirectoryName(path);
            if (!string.Equals(
                    Path.GetFileName(directory),
                    ".legacy-migrations",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string name =
                Path.GetFileName(path);
            const string suffix = ".flat.json";
            if (!name.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string hash =
                name.Substring(
                    0,
                    name.Length - suffix.Length);
            return hash.Length == 64 &&
                hash.All(
                    value =>
                        (value >= '0' && value <= '9') ||
                        (value >= 'A' && value <= 'F') ||
                        (value >= 'a' && value <= 'f'));
        }

        private static bool TryGetStorageOwnerFromPath(
            string path,
            out string ownerId)
        {
            ownerId = string.Empty;
            const string prefix = "equipment-slots-";
            const string suffix = ".json";
            string? directory =
                Path.GetDirectoryName(path);
            string directoryName =
                Path.GetFileName(directory)
                ?? string.Empty;
            string? claimParent =
                string.IsNullOrWhiteSpace(directory)
                    ? null
                    : Path.GetDirectoryName(directory);
            if (string.Equals(
                    Path.GetFileName(claimParent),
                    ".equipment-slot-migration-claims",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(directoryName))
            {
                ownerId = directoryName;
                return true;
            }

            string name =
                Path.GetFileName(path) ?? string.Empty;
            if (name.EndsWith(
                ".previous",
                StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(
                    0,
                    name.Length - ".previous".Length);
            }
            foreach (string marker in
                new[]
                {
                    ".migration-capture-",
                    ".migrated-product-v3-"
                })
            {
                int markerIndex =
                    name.IndexOf(
                        marker,
                        StringComparison.OrdinalIgnoreCase);
                if (markerIndex > 0)
                {
                    name =
                        name.Substring(
                            0,
                            markerIndex);
                    break;
                }
            }
            if (!name.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase) ||
                name.Length <=
                    prefix.Length + suffix.Length)
            {
                return false;
            }
            ownerId = name.Substring(
                prefix.Length,
                name.Length -
                    prefix.Length -
                    suffix.Length);
            return !string.IsNullOrWhiteSpace(ownerId);
        }

        [DataContract]
        private sealed class ColdStorageOwnerProbe
        {
            [DataMember(
                Name = "ownerId",
                EmitDefaultValue = false)]
            public string OwnerId { get; set; } =
                string.Empty;
        }

        private static string DetectColdStorageSource(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                return json.IndexOf("\"journal\"", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "Journal"
                    : "Sidecar";
            }
            catch
            {
                return "Sidecar";
            }
        }

        private T TryInvoke<T>(string method, T fallback, params object?[] arguments) =>
            broker.TryGetService("EquipmentSlots", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, arguments)
                : fallback;

        private void TryInvokeVoid(string method, params object?[] arguments)
        {
            if (broker.TryGetService("EquipmentSlots", out object? backend) && backend != null)
                CompatibilityHostBroker.Invoke(backend, method, arguments);
        }
    }
}
