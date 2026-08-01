using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingNativeFishingContextReceipt
    {
        [DataMember(Name = "observedAtUtc", Order = 1)] internal string ObservedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "saveLoadOrdinal", Order = 2)] internal int SaveLoadOrdinal { get; set; }
        [DataMember(Name = "phase", Order = 3)] internal string Phase { get; set; } = string.Empty;
        [DataMember(Name = "selectedRodObserved", Order = 4)] internal bool SelectedRodObserved { get; set; }
        [DataMember(Name = "selectedRodType", Order = 5)] internal string SelectedRodType { get; set; } = string.Empty;
        [DataMember(Name = "fishingPoolObserved", Order = 6)] internal bool FishingPoolObserved { get; set; }
        [DataMember(Name = "fishingPoolCount", Order = 7)] internal int FishingPoolCount { get; set; } = -1;
        [DataMember(Name = "verified", Order = 8)] internal bool Verified { get; set; }
        [DataMember(Name = "error", Order = 9)] internal string Error { get; set; } = string.Empty;
        [DataMember(Name = "selectedRodSource", Order = 10)] internal string SelectedRodSource { get; set; } = "DolocAPI.SelectedItem";
        [DataMember(Name = "fishingPoolSource", Order = 11)] internal string FishingPoolSource { get; set; } = "UnityEngine.Object.FindObjectsOfType(DolocTown.FishingPool)";
        [DataMember(Name = "currentRoomType", Order = 12)] internal string CurrentRoomType { get; set; } = string.Empty;
        [DataMember(Name = "selectedRodIdentity", Order = 13)] internal string SelectedRodIdentity { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class Batch6AutoFishingNativeVitalsReceipt
    {
        [DataMember(Name = "kind", Order = 1)] internal string Kind { get; set; } = string.Empty;
        [DataMember(Name = "context", Order = 2)] internal string Context { get; set; } = string.Empty;
        [DataMember(Name = "saveLoadOrdinal", Order = 3)] internal int SaveLoadOrdinal { get; set; }
        [DataMember(Name = "workloadPhase", Order = 4)] internal string WorkloadPhase { get; set; } = string.Empty;
        [DataMember(Name = "energyCommandInvoked", Order = 5)] internal bool EnergyCommandInvoked { get; set; }
        [DataMember(Name = "spiritCommandInvoked", Order = 6)] internal bool SpiritCommandInvoked { get; set; }
        [DataMember(Name = "energyPercentBefore", Order = 7)] internal double EnergyPercentBefore { get; set; }
        [DataMember(Name = "energyPercentAfter", Order = 8)] internal double EnergyPercentAfter { get; set; }
        [DataMember(Name = "spiritPercentBefore", Order = 9)] internal double SpiritPercentBefore { get; set; }
        [DataMember(Name = "spiritPercentAfter", Order = 10)] internal double SpiritPercentAfter { get; set; }
        [DataMember(Name = "nativeEnergySufficientAfter", Order = 11)] internal bool NativeEnergySufficientAfter { get; set; }
        [DataMember(Name = "nativeEnergyReserveSufficientAfter", Order = 12)] internal bool NativeEnergyReserveSufficientAfter { get; set; }
        [DataMember(Name = "fishingEnergyCost", Order = 13)] internal int FishingEnergyCost { get; set; }
        [DataMember(Name = "composeEnergyDelegateIdentity", Order = 14)] internal string ComposeEnergyDelegateIdentity { get; set; } = string.Empty;
        [DataMember(Name = "composeSpiritDelegateIdentity", Order = 15)] internal string ComposeSpiritDelegateIdentity { get; set; } = string.Empty;
        [DataMember(Name = "getEnergyPercentDelegateIdentity", Order = 16)] internal string GetEnergyPercentDelegateIdentity { get; set; } = string.Empty;
        [DataMember(Name = "getSpiritPercentDelegateIdentity", Order = 17)] internal string GetSpiritPercentDelegateIdentity { get; set; } = string.Empty;
        [DataMember(Name = "delegateIdentitiesVerified", Order = 18)] internal bool DelegateIdentitiesVerified { get; set; }
        [DataMember(Name = "readbackVerified", Order = 19)] internal bool ReadbackVerified { get; set; }
        [DataMember(Name = "source", Order = 20)] internal string Source { get; set; } = string.Empty;
        [DataMember(Name = "nativeEnergyInsufficientObserved", Order = 21)] internal bool NativeEnergyInsufficientObserved { get; set; }
        [DataMember(Name = "nativeEnergyReserveLowObserved", Order = 22)] internal bool NativeEnergyReserveLowObserved { get; set; }
        [DataMember(Name = "nativeSpiritLowObserved", Order = 23)] internal bool NativeSpiritLowObserved { get; set; }
    }

    [DataContract]
    internal sealed class Batch6AutoFishingNativeVitalsEvidence
    {
        [DataMember(Name = "verified", Order = 1)] internal bool Verified { get; set; }
        [DataMember(Name = "readbackVerified", Order = 2)] internal bool ReadbackVerified { get; set; }
        [DataMember(Name = "workloadStartCount", Order = 3)] internal int WorkloadStartCount { get; set; }
        [DataMember(Name = "maintenanceCount", Order = 4)] internal int MaintenanceCount { get; set; }
        [DataMember(Name = "l4RecoveryCheckpointCount", Order = 5)] internal int L4RecoveryCheckpointCount { get; set; }
        [DataMember(Name = "finalReadbackCount", Order = 6)] internal int FinalReadbackCount { get; set; }
        [DataMember(Name = "receipts", Order = 7)] internal List<Batch6AutoFishingNativeVitalsReceipt> Receipts { get; set; } = new List<Batch6AutoFishingNativeVitalsReceipt>();
        [DataMember(Name = "energyInsufficientObservationCount", Order = 8)] internal int EnergyInsufficientObservationCount { get; set; }
        [DataMember(Name = "energyReserveLowObservationCount", Order = 9)] internal int EnergyReserveLowObservationCount { get; set; }
        [DataMember(Name = "spiritLowObservationCount", Order = 10)] internal int SpiritLowObservationCount { get; set; }
    }

    internal static class Batch6AutoFishingNativeFishingContextObserver
    {
        internal static Type RequireDolocApiType() =>
            ResolveType("DolocAPI", "Assembly-CSharp")
            ?? throw new InvalidOperationException("Batch6AutoFishingPilot could not resolve DolocAPI from Assembly-CSharp.");

        internal static bool IsNormalGameState()
        {
            object? value = ReadStaticMember(RequireDolocApiType(), "IsNormalState");
            return value is bool normal && normal;
        }

        internal static Batch6AutoFishingNativeFishingContextReceipt Capture(int saveLoadOrdinal, string phase, DateTimeOffset now)
        {
            var receipt = new Batch6AutoFishingNativeFishingContextReceipt
            {
                ObservedAtUtc = now.ToUniversalTime().ToString("O"),
                SaveLoadOrdinal = saveLoadOrdinal,
                Phase = phase ?? string.Empty
            };
            try
            {
                Type dolocApi = RequireDolocApiType();
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                receipt.CurrentRoomType = currentRoom?.GetType().FullName ?? string.Empty;
                if (currentRoom == null)
                    throw new InvalidOperationException("DolocAPI.CurrentRoom is null at the fifth-save fishing preflight.");
                object? selected = ReadStaticMember(dolocApi, "SelectedItem");
                receipt.SelectedRodType = selected?.GetType().FullName ?? string.Empty;
                receipt.SelectedRodIdentity = ReadItemIdentity(selected);
                receipt.SelectedRodObserved = selected != null && IsTypeOrBase(selected.GetType(), "DolocTown.ItemFishingRod");

                Type poolType = ResolveType("DolocTown.FishingPool", "Assembly-CSharp")
                    ?? throw new MissingMemberException("Assembly-CSharp", "DolocTown.FishingPool");
                Type unityObject = ResolveType("UnityEngine.Object", "UnityEngine.CoreModule") ??
                    ResolveType("UnityEngine.Object", "UnityEngine") ??
                    throw new MissingMemberException("UnityEngine", "UnityEngine.Object");
                MethodInfo findObjects = unityObject.GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(Type) },
                    modifiers: null)
                    ?? throw new MissingMethodException(unityObject.FullName, "FindObjectsOfType(Type)");
                object? found = findObjects.Invoke(null, new object[] { poolType });
                receipt.FishingPoolCount = Count(found);
                receipt.FishingPoolObserved = receipt.FishingPoolCount > 0;
                receipt.Verified = receipt.SelectedRodObserved && receipt.FishingPoolObserved;
                if (!receipt.Verified)
                    receipt.Error = "The fifth-save native fishing context requires both a selected ItemFishingRod and at least one FishingPool.";
            }
            catch (Exception ex)
            {
                receipt.Verified = false;
                receipt.Error = ex.GetType().Name + ": " + ex.Message;
            }
            return receipt;
        }

        private static Type? ResolveType(string fullName, string assemblyName) =>
            Type.GetType(fullName + ", " + assemblyName, throwOnError: false) ??
            AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                ?.GetType(fullName, throwOnError: false);

        private static object? ReadStaticMember(Type type, string name)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            PropertyInfo? property = type.GetProperty(name, Flags);
            if (property != null)
                return property.GetValue(null, null);
            FieldInfo? field = type.GetField(name, Flags);
            if (field != null)
                return field.GetValue(null);
            throw new MissingMemberException(type.FullName, name);
        }

        private static bool IsTypeOrBase(Type? type, string fullName)
        {
            while (type != null)
            {
                if (string.Equals(type.FullName, fullName, StringComparison.Ordinal))
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private static string ReadItemIdentity(object? item)
        {
            if (item == null)
                return string.Empty;
            foreach (string name in new[] { "ID", "Id", "ItemID", "UniqueID" })
            {
                object? value = TryReadInstanceMember(item, name);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                    return value.ToString() ?? string.Empty;
            }
            object? proto = TryReadInstanceMember(item, "Proto") ?? TryReadInstanceMember(item, "proto");
            if (proto != null)
            {
                foreach (string name in new[] { "ID", "Id", "ItemID", "UniqueID" })
                {
                    object? value = TryReadInstanceMember(proto, name);
                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        return value.ToString() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private static object? TryReadInstanceMember(object target, string name)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                PropertyInfo? property = target.GetType().GetProperty(name, Flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target, null);
                return target.GetType().GetField(name, Flags)?.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static int Count(object? value)
        {
            if (value is Array array)
                return array.Length;
            if (!(value is IEnumerable enumerable))
                return 0;
            int count = 0;
            foreach (object? ignored in enumerable)
                count++;
            return count;
        }
    }

    internal static class Batch6AutoFishingNativeVitalsEvidenceMapper
    {
        internal static Batch6AutoFishingNativeVitalsReceipt From(global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            string kind = source.WorkloadStart ? "WorkloadStart" :
                source.MaintenanceRefill ? "MaintenanceRefill" :
                source.L4RecoveryCheckpoint ? "L4RecoveryCheckpoint" :
                source.FinalObservation ? "FinalReadback" : "Unknown";
            bool identities =
                string.Equals(source.Source, global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialCommandSource, StringComparison.Ordinal) &&
                string.Equals(source.ComposeEnergyDelegateIdentity, global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity, StringComparison.Ordinal) &&
                string.Equals(source.ComposeSpiritDelegateIdentity, global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity, StringComparison.Ordinal) &&
                string.Equals(source.GetEnergyPercentDelegateIdentity, global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity, StringComparison.Ordinal) &&
                string.Equals(source.GetSpiritPercentDelegateIdentity, global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity, StringComparison.Ordinal);
            bool validReadback =
                global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsValidPercent(source.EnergyPercentBefore) &&
                global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsValidPercent(source.EnergyPercentAfter) &&
                global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsValidPercent(source.SpiritPercentBefore) &&
                global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsValidPercent(source.SpiritPercentAfter) &&
                source.NativeEnergySufficientAfter && source.NativeEnergyReserveSufficientAfter && source.FishingEnergyCost > 0;
            bool kindVerified = source.WorkloadStart || source.L4RecoveryCheckpoint
                ? source.EnergyCommandInvoked && source.SpiritCommandInvoked &&
                    global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsFull(source.EnergyPercentAfter) &&
                    global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.IsFull(source.SpiritPercentAfter)
                : source.MaintenanceRefill
                    ? source.EnergyCommandInvoked || source.SpiritCommandInvoked
                    : source.FinalObservation && !source.EnergyCommandInvoked && !source.SpiritCommandInvoked;
            return new Batch6AutoFishingNativeVitalsReceipt
            {
                Kind = kind,
                Context = source.Context,
                SaveLoadOrdinal = source.SaveLoadOrdinal,
                WorkloadPhase = source.WorkloadPhase,
                EnergyCommandInvoked = source.EnergyCommandInvoked,
                SpiritCommandInvoked = source.SpiritCommandInvoked,
                EnergyPercentBefore = source.EnergyPercentBefore,
                EnergyPercentAfter = source.EnergyPercentAfter,
                SpiritPercentBefore = source.SpiritPercentBefore,
                SpiritPercentAfter = source.SpiritPercentAfter,
                NativeEnergySufficientAfter = source.NativeEnergySufficientAfter,
                NativeEnergyReserveSufficientAfter = source.NativeEnergyReserveSufficientAfter,
                FishingEnergyCost = source.FishingEnergyCost,
                ComposeEnergyDelegateIdentity = source.ComposeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = source.ComposeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = source.GetEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = source.GetSpiritPercentDelegateIdentity,
                DelegateIdentitiesVerified = identities,
                ReadbackVerified = identities && validReadback && kindVerified &&
                    !source.NativeEnergyInsufficientObserved && !string.Equals(kind, "Unknown", StringComparison.Ordinal),
                Source = source.Source,
                NativeEnergyInsufficientObserved = source.NativeEnergyInsufficientObserved,
                NativeEnergyReserveLowObserved = source.NativeEnergyReserveLowObserved,
                NativeSpiritLowObserved = source.NativeSpiritLowObserved
            };
        }
    }
}
