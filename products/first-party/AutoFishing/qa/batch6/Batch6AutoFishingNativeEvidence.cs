using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    internal sealed class Batch6AutoFishingConveyorPlatformReceipt
    {
        [DataMember(Name = "verified", Order = 1)] internal bool Verified { get; set; }
        [DataMember(Name = "error", Order = 2)] internal string Error { get; set; } = string.Empty;
        [DataMember(Name = "instanceId", Order = 3)] internal int InstanceId { get; set; }
        [DataMember(Name = "gameObjectName", Order = 4)] internal string GameObjectName { get; set; } = string.Empty;
        [DataMember(Name = "activeInHierarchy", Order = 5)] internal bool ActiveInHierarchy { get; set; }
        [DataMember(Name = "positionX", Order = 6)] internal double PositionX { get; set; }
        [DataMember(Name = "positionY", Order = 7)] internal double PositionY { get; set; }
        [DataMember(Name = "positionZ", Order = 8)] internal double PositionZ { get; set; }
        [DataMember(Name = "deltaFromAgentX", Order = 9)] internal double DeltaFromAgentX { get; set; }
        [DataMember(Name = "deltaFromAgentY", Order = 10)] internal double DeltaFromAgentY { get; set; }
        [DataMember(Name = "isTouched", Order = 11)] internal bool IsTouched { get; set; }
        [DataMember(Name = "isStay", Order = 12)] internal bool IsStay { get; set; }
        [DataMember(Name = "groupPresent", Order = 13)] internal bool GroupPresent { get; set; }
        [DataMember(Name = "groupSpeedX", Order = 14)] internal double GroupSpeedX { get; set; }
        [DataMember(Name = "groupSpeedY", Order = 15)] internal double GroupSpeedY { get; set; }
        [DataMember(Name = "groupMoveSpeed", Order = 16)] internal double GroupMoveSpeed { get; set; }
        [DataMember(Name = "groupMoveDirectionX", Order = 17)] internal double GroupMoveDirectionX { get; set; }
        [DataMember(Name = "groupMoveDirectionY", Order = 18)] internal double GroupMoveDirectionY { get; set; }
        [DataMember(Name = "currentOtherColliderPresent", Order = 19)] internal bool CurrentOtherColliderPresent { get; set; }
        [DataMember(Name = "currentOtherColliderType", Order = 20)] internal string CurrentOtherColliderType { get; set; } = string.Empty;
        [DataMember(Name = "currentOtherColliderGameObjectName", Order = 21)] internal string CurrentOtherColliderGameObjectName { get; set; } = string.Empty;
        [DataMember(Name = "currentOtherColliderInstanceId", Order = 22)] internal int CurrentOtherColliderInstanceId { get; set; }
        [DataMember(Name = "currentCollisionPresent", Order = 23)] internal bool CurrentCollisionPresent { get; set; }
        [DataMember(Name = "currentCollisionGameObjectName", Order = 24)] internal string CurrentCollisionGameObjectName { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class Batch6AutoFishingNativeSurfaceReceipt
    {
        [DataMember(Name = "observedAtUtc", Order = 1)] internal string ObservedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "saveLoadOrdinal", Order = 2)] internal int SaveLoadOrdinal { get; set; }
        [DataMember(Name = "phase", Order = 3)] internal string Phase { get; set; } = string.Empty;
        [DataMember(Name = "inputContext", Order = 4)] internal string InputContext { get; set; } = string.Empty;
        [DataMember(Name = "verified", Order = 5)] internal bool Verified { get; set; }
        [DataMember(Name = "error", Order = 6)] internal string Error { get; set; } = string.Empty;
        [DataMember(Name = "roomType", Order = 7)] internal string RoomType { get; set; } = string.Empty;
        [DataMember(Name = "roomId", Order = 8)] internal string RoomId { get; set; } = string.Empty;
        [DataMember(Name = "sceneRawName", Order = 9)] internal string SceneRawName { get; set; } = string.Empty;
        [DataMember(Name = "agentStateType", Order = 10)] internal string AgentStateType { get; set; } = string.Empty;
        [DataMember(Name = "agentFaceRight", Order = 11)] internal bool AgentFaceRight { get; set; }
        [DataMember(Name = "agentPositionX", Order = 12)] internal double AgentPositionX { get; set; }
        [DataMember(Name = "agentPositionY", Order = 13)] internal double AgentPositionY { get; set; }
        [DataMember(Name = "agentPositionZ", Order = 14)] internal double AgentPositionZ { get; set; }
        [DataMember(Name = "agentCellX", Order = 15)] internal int AgentCellX { get; set; }
        [DataMember(Name = "agentCellY", Order = 16)] internal int AgentCellY { get; set; }
        [DataMember(Name = "groundTouched", Order = 17)] internal bool GroundTouched { get; set; }
        [DataMember(Name = "groundTouchedExcludePlatforms", Order = 18)] internal bool GroundTouchedExcludePlatforms { get; set; }
        [DataMember(Name = "groundPlatformCount", Order = 19)] internal int GroundPlatformCount { get; set; }
        [DataMember(Name = "touchWall", Order = 20)] internal bool TouchWall { get; set; }
        [DataMember(Name = "nearWallTop", Order = 21)] internal bool NearWallTop { get; set; }
        [DataMember(Name = "rigidbodyVelocityX", Order = 22)] internal double RigidbodyVelocityX { get; set; }
        [DataMember(Name = "rigidbodyVelocityY", Order = 23)] internal double RigidbodyVelocityY { get; set; }
        [DataMember(Name = "inputMultiplier", Order = 24)] internal double InputMultiplier { get; set; }
        [DataMember(Name = "conveyorOffsetX", Order = 25)] internal double ConveyorOffsetX { get; set; }
        [DataMember(Name = "conveyorOffsetY", Order = 26)] internal double ConveyorOffsetY { get; set; }
        [DataMember(Name = "conveyorPlatformsEnumerated", Order = 27)] internal bool ConveyorPlatformsEnumerated { get; set; }
        [DataMember(Name = "conveyorPlatformInstanceCount", Order = 28)] internal int ConveyorPlatformInstanceCount { get; set; }
        [DataMember(Name = "conveyorPlatformTouchedCount", Order = 29)] internal int ConveyorPlatformTouchedCount { get; set; }
        [DataMember(Name = "conveyorPlatformStayCount", Order = 30)] internal int ConveyorPlatformStayCount { get; set; }
        [DataMember(Name = "conveyorPlatforms", Order = 31)] internal List<Batch6AutoFishingConveyorPlatformReceipt> ConveyorPlatforms { get; set; } = new List<Batch6AutoFishingConveyorPlatformReceipt>();

        internal string Summary
        {
            get
            {
                Batch6AutoFishingConveyorPlatformReceipt? touched = ConveyorPlatforms.FirstOrDefault(item => item.IsTouched || item.IsStay);
                string owner = touched == null
                    ? "none"
                    : touched.InstanceId.ToString() + ":" + touched.GameObjectName + ":" +
                        touched.GroupSpeedX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        touched.GroupSpeedY.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                return "phase=" + Phase +
                    "; verified=" + Verified.ToString().ToLowerInvariant() +
                    "; position=" + AgentPositionX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        AgentPositionY.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    "; cell=" + AgentCellX + "," + AgentCellY +
                    "; state=" + AgentStateType +
                    "; groundTouched=" + GroundTouched.ToString().ToLowerInvariant() +
                    "; platformCount=" + GroundPlatformCount +
                    "; touchWall=" + TouchWall.ToString().ToLowerInvariant() +
                    "; inputMultiplier=" + InputMultiplier.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    "; conveyorOffset=" + ConveyorOffsetX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        ConveyorOffsetY.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    "; rigidbodyVelocity=" + RigidbodyVelocityX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        RigidbodyVelocityY.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    "; activeConveyors=" + ConveyorPlatformInstanceCount +
                    "; touchedConveyors=" + ConveyorPlatformTouchedCount +
                    "; stayConveyors=" + ConveyorPlatformStayCount +
                    "; owner=" + owner +
                    (string.IsNullOrEmpty(Error) ? string.Empty : "; error=" + Error);
            }
        }
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

    internal static class Batch6AutoFishingNativeSurfaceObserver
    {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

        internal static Batch6AutoFishingNativeSurfaceReceipt Capture(
            int saveLoadOrdinal,
            string phase,
            string inputContext,
            DateTimeOffset now)
        {
            var receipt = CreateReceipt(saveLoadOrdinal, phase, inputContext, now);
            try
            {
                Type dolocApi = Batch6AutoFishingNativeFishingContextObserver.RequireDolocApiType();
                Type platformType = ResolveType("DolocTown.ConveyorPlatform", "Assembly-CSharp")
                    ?? throw new MissingMemberException("Assembly-CSharp", "DolocTown.ConveyorPlatform");
                object? found = FindObjectsOfType(platformType);
                IEnumerable<object> platforms = Enumerate(found);
                Populate(receipt, dolocApi, platforms);
            }
            catch (Exception ex)
            {
                receipt.Verified = false;
                receipt.Error = Describe(ex);
            }
            return receipt;
        }

        internal static Batch6AutoFishingNativeSurfaceReceipt CaptureForTesting(
            int saveLoadOrdinal,
            string phase,
            string inputContext,
            DateTimeOffset now,
            Type dolocApiType,
            IEnumerable<object> conveyorPlatforms)
        {
            var receipt = CreateReceipt(saveLoadOrdinal, phase, inputContext, now);
            try
            {
                Populate(
                    receipt,
                    dolocApiType ?? throw new ArgumentNullException(nameof(dolocApiType)),
                    conveyorPlatforms ?? throw new ArgumentNullException(nameof(conveyorPlatforms)));
            }
            catch (Exception ex)
            {
                receipt.Verified = false;
                receipt.Error = Describe(ex);
            }
            return receipt;
        }

        private static Batch6AutoFishingNativeSurfaceReceipt CreateReceipt(
            int saveLoadOrdinal,
            string phase,
            string inputContext,
            DateTimeOffset now) =>
            new Batch6AutoFishingNativeSurfaceReceipt
            {
                ObservedAtUtc = now.ToUniversalTime().ToString("O"),
                SaveLoadOrdinal = saveLoadOrdinal,
                Phase = phase ?? string.Empty,
                InputContext = inputContext ?? string.Empty
            };

        private static void Populate(
            Batch6AutoFishingNativeSurfaceReceipt receipt,
            Type dolocApiType,
            IEnumerable<object> conveyorPlatforms)
        {
            object currentRoom = RequireStaticMember(dolocApiType, "CurrentRoom");
            object agent = RequireStaticMember(dolocApiType, "agent");
            object agentPosition = RequireStaticMember(dolocApiType, "AgentPosition");
            object agentCell = RequireStaticMember(dolocApiType, "AgentRoomCellPosition");
            object status = RequireInstanceMember(agent, "Status");
            object stateManager = RequireInstanceMember(agent, "StateManager");
            object currentState = RequireInstanceMember(stateManager, "current");
            object moveModifier = RequireInstanceMember(status, "MoveModifier");
            object conveyorOffset = RequireInstanceMember(moveModifier, "conveyorOffset");
            object groundChecker = RequireInstanceMember(agent, "groundChecker");

            receipt.RoomType = currentRoom.GetType().FullName ?? currentRoom.GetType().Name;
            receipt.RoomId = Convert.ToString(RequireInstanceMember(currentRoom, "RoomId"), CultureInfo.InvariantCulture) ?? string.Empty;
            receipt.SceneRawName = Convert.ToString(RequireInstanceMember(currentRoom, "SceneRawName"), CultureInfo.InvariantCulture) ?? string.Empty;
            receipt.AgentStateType = currentState.GetType().FullName ?? currentState.GetType().Name;
            receipt.AgentFaceRight = ReadBoolean(RequireInstanceMember(agent, "IsFaceRight"), "agent.IsFaceRight");
            receipt.AgentPositionX = ReadDouble(RequireInstanceMember(agentPosition, "x"), "AgentPosition.x");
            receipt.AgentPositionY = ReadDouble(RequireInstanceMember(agentPosition, "y"), "AgentPosition.y");
            receipt.AgentPositionZ = ReadDouble(RequireInstanceMember(agentPosition, "z"), "AgentPosition.z");
            receipt.AgentCellX = ReadInt32(RequireInstanceMember(agentCell, "x"), "AgentRoomCellPosition.x");
            receipt.AgentCellY = ReadInt32(RequireInstanceMember(agentCell, "y"), "AgentRoomCellPosition.y");
            receipt.GroundTouched = ReadBoolean(RequireInstanceMember(groundChecker, "isTouched"), "groundChecker.isTouched");
            receipt.GroundTouchedExcludePlatforms = ReadBoolean(RequireInstanceMember(groundChecker, "isTouchedExcludePlatforms"), "groundChecker.isTouchedExcludePlatforms");
            receipt.GroundPlatformCount = ReadInt32(RequireInstanceMember(groundChecker, "PlatformCount"), "groundChecker.PlatformCount");
            receipt.TouchWall = ReadBoolean(RequireInstanceMember(status, "IsTouchWall"), "status.IsTouchWall");
            receipt.NearWallTop = ReadBoolean(RequireInstanceMember(status, "IsNearWallTop"), "status.IsNearWallTop");
            receipt.RigidbodyVelocityX = ReadDouble(RequireInstanceMember(status, "VelocityX"), "status.VelocityX");
            receipt.RigidbodyVelocityY = ReadDouble(RequireInstanceMember(status, "VelocityY"), "status.VelocityY");
            receipt.InputMultiplier = ReadDouble(RequireInstanceMember(moveModifier, "inputMultiplier"), "MoveModifier.inputMultiplier");
            receipt.ConveyorOffsetX = ReadDouble(RequireInstanceMember(conveyorOffset, "x"), "MoveModifier.conveyorOffset.x");
            receipt.ConveyorOffsetY = ReadDouble(RequireInstanceMember(conveyorOffset, "y"), "MoveModifier.conveyorOffset.y");

            foreach (object platform in conveyorPlatforms.Where(item => item != null))
                receipt.ConveyorPlatforms.Add(CapturePlatform(platform, receipt.AgentPositionX, receipt.AgentPositionY));
            receipt.ConveyorPlatforms = receipt.ConveyorPlatforms
                .OrderBy(item => item.InstanceId)
                .ThenBy(item => item.GameObjectName, StringComparer.Ordinal)
                .ToList();
            receipt.ConveyorPlatformsEnumerated = true;
            receipt.ConveyorPlatformInstanceCount = receipt.ConveyorPlatforms.Count;
            receipt.ConveyorPlatformTouchedCount = receipt.ConveyorPlatforms.Count(item => item.IsTouched);
            receipt.ConveyorPlatformStayCount = receipt.ConveyorPlatforms.Count(item => item.IsStay);
            receipt.Verified = receipt.ConveyorPlatforms.All(item => item.Verified);
            if (!receipt.Verified)
            {
                receipt.Error = string.Join(" | ", receipt.ConveyorPlatforms
                    .Where(item => !item.Verified)
                    .Select(item => "platform " + item.InstanceId + ": " + item.Error));
            }
        }

        private static Batch6AutoFishingConveyorPlatformReceipt CapturePlatform(
            object platform,
            double agentPositionX,
            double agentPositionY)
        {
            var receipt = new Batch6AutoFishingConveyorPlatformReceipt();
            try
            {
                receipt.InstanceId = ReadInstanceId(platform);
                object gameObject = RequireInstanceMember(platform, "gameObject");
                object transform = RequireInstanceMember(platform, "transform");
                object position = RequireInstanceMember(transform, "position");
                object? group = ReadInstanceMember(platform, "group");
                object? otherCollider = ReadInstanceMember(platform, "currentOtherCollider");
                object? collision = ReadInstanceMember(platform, "currentCollision");

                receipt.GameObjectName = Convert.ToString(RequireInstanceMember(gameObject, "name"), CultureInfo.InvariantCulture) ?? string.Empty;
                receipt.ActiveInHierarchy = ReadBoolean(RequireInstanceMember(gameObject, "activeInHierarchy"), "platform.gameObject.activeInHierarchy");
                receipt.PositionX = ReadDouble(RequireInstanceMember(position, "x"), "platform.position.x");
                receipt.PositionY = ReadDouble(RequireInstanceMember(position, "y"), "platform.position.y");
                receipt.PositionZ = ReadDouble(RequireInstanceMember(position, "z"), "platform.position.z");
                receipt.DeltaFromAgentX = receipt.PositionX - agentPositionX;
                receipt.DeltaFromAgentY = receipt.PositionY - agentPositionY;
                receipt.IsTouched = ReadBoolean(RequireInstanceMember(platform, "isTouched"), "platform.isTouched");
                receipt.IsStay = ReadBoolean(RequireInstanceMember(platform, "isStay"), "platform.isStay");
                receipt.GroupPresent = group != null;
                if (group == null)
                    throw new InvalidDataException("Active ConveyorPlatform has no owning group.");
                receipt.GroupSpeedX = ReadDouble(RequireInstanceMember(group, "SpeedX"), "group.SpeedX");
                receipt.GroupSpeedY = ReadDouble(RequireInstanceMember(group, "SpeedY"), "group.SpeedY");
                receipt.GroupMoveSpeed = ReadDouble(RequireInstanceMember(group, "moveSpeed"), "group.moveSpeed");
                object moveDirection = RequireInstanceMember(group, "moveDir");
                receipt.GroupMoveDirectionX = ReadDouble(RequireInstanceMember(moveDirection, "x"), "group.moveDir.x");
                receipt.GroupMoveDirectionY = ReadDouble(RequireInstanceMember(moveDirection, "y"), "group.moveDir.y");

                receipt.CurrentOtherColliderPresent = otherCollider != null;
                if (otherCollider != null)
                {
                    receipt.CurrentOtherColliderType = otherCollider.GetType().FullName ?? otherCollider.GetType().Name;
                    receipt.CurrentOtherColliderInstanceId = ReadInstanceId(otherCollider);
                    object otherGameObject = RequireInstanceMember(otherCollider, "gameObject");
                    receipt.CurrentOtherColliderGameObjectName = Convert.ToString(
                        RequireInstanceMember(otherGameObject, "name"),
                        CultureInfo.InvariantCulture) ?? string.Empty;
                }

                receipt.CurrentCollisionPresent = collision != null;
                if (collision != null)
                {
                    object collisionGameObject = RequireInstanceMember(collision, "gameObject");
                    receipt.CurrentCollisionGameObjectName = Convert.ToString(
                        RequireInstanceMember(collisionGameObject, "name"),
                        CultureInfo.InvariantCulture) ?? string.Empty;
                }
                receipt.Verified = true;
            }
            catch (Exception ex)
            {
                receipt.Verified = false;
                receipt.Error = Describe(ex);
            }
            return receipt;
        }

        private static object? FindObjectsOfType(Type targetType)
        {
            Type unityObject = ResolveType("UnityEngine.Object", "UnityEngine.CoreModule") ??
                ResolveType("UnityEngine.Object", "UnityEngine") ??
                throw new MissingMemberException("UnityEngine", "UnityEngine.Object");
            MethodInfo method = unityObject.GetMethod(
                "FindObjectsOfType",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Type) },
                modifiers: null)
                ?? throw new MissingMethodException(unityObject.FullName, "FindObjectsOfType(Type)");
            return method.Invoke(null, new object[] { targetType });
        }

        private static IEnumerable<object> Enumerate(object? value)
        {
            if (!(value is IEnumerable enumerable))
                throw new InvalidDataException("UnityEngine.Object.FindObjectsOfType did not return an enumerable ConveyorPlatform collection.");
            foreach (object? item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        private static object RequireStaticMember(Type type, string name)
        {
            object? value = ReadStaticMember(type, name);
            return value ?? throw new InvalidDataException(type.FullName + "." + name + " is null.");
        }

        private static object? ReadStaticMember(Type type, string name)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, StaticFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(null, null);
                FieldInfo? field = current.GetField(name, StaticFlags);
                if (field != null)
                    return field.GetValue(null);
            }
            throw new MissingMemberException(type.FullName, name);
        }

        private static object RequireInstanceMember(object target, string name)
        {
            object? value = ReadInstanceMember(target, name);
            return value ?? throw new InvalidDataException(target.GetType().FullName + "." + name + " is null.");
        }

        private static object? ReadInstanceMember(object target, string name)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            for (Type? current = target.GetType(); current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, InstanceFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target, null);
                FieldInfo? field = current.GetField(name, InstanceFlags);
                if (field != null)
                    return field.GetValue(target);
            }
            throw new MissingMemberException(target.GetType().FullName, name);
        }

        private static int ReadInstanceId(object target)
        {
            MethodInfo? method = null;
            for (Type? current = target.GetType(); current != null && method == null; current = current.BaseType)
                method = current.GetMethod("GetInstanceID", InstanceFlags, null, Type.EmptyTypes, null);
            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, "GetInstanceID()");
            return ReadInt32(method.Invoke(target, Array.Empty<object>())!, target.GetType().FullName + ".GetInstanceID()");
        }

        private static bool ReadBoolean(object value, string label)
        {
            if (value is bool boolean)
                return boolean;
            throw new InvalidDataException(label + " is not Boolean.");
        }

        private static int ReadInt32(object value, string label)
        {
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(label + " is not Int32.", ex);
            }
        }

        private static double ReadDouble(object value, string label)
        {
            try
            {
                double result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new InvalidDataException(label + " is non-finite.");
                return result;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(label + " is not numeric.", ex);
            }
        }

        private static Type? ResolveType(string fullName, string assemblyName) =>
            Type.GetType(fullName + ", " + assemblyName, throwOnError: false) ??
            AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                ?.GetType(fullName, throwOnError: false);

        private static string Describe(Exception exception)
        {
            Exception current = exception;
            while (current is TargetInvocationException && current.InnerException != null)
                current = current.InnerException;
            return current.GetType().Name + ": " + current.Message;
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
