using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi : IInventoryDebugApi, IMailDeliveryApi, IWeatherDebugApi, ITeleportDebugApi, IInstantSaveDebugApi, ITimeDebugApi, IMovementDebugApi, IMachineProductionApi, IEquipmentSlotsApi, IAdvancedDebugApi
    {
        private const int VanillaArchiveSlotCount = 6;

        private readonly DTMAPI.Core.Runtime.DtmApiRuntime runtime;
        private readonly Dictionary<string, List<MachineDefinition>> machineDefinitions = new Dictionary<string, List<MachineDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MachineProductionState> machineStates = new Dictionary<string, MachineProductionState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MachineRuntimeEntry> machineRuntimeEntries = new Dictionary<string, MachineRuntimeEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Random machineRandom = new Random();
        private readonly Dictionary<string, EquipmentSlotsOptions> equipmentSlotOptions = new Dictionary<string, EquipmentSlotsOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EquipmentSlotsState> equipmentSlotStates = new Dictionary<string, EquipmentSlotsState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<EquipmentSlotRuntimeEntry>> equipmentSlotEntries = new Dictionary<string, List<EquipmentSlotRuntimeEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadedEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> dirtyEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> migratedLegacyEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<object> activeEquipmentSlotUiObjects = new List<object>();
        private readonly List<object> equipmentSlotUiEventBinders = new List<object>();
        private ActionSpeedService? actionSpeedService;
        private DateTimeOffset lastMachineProductionPollAt = DateTimeOffset.MinValue;
        private int lastMachineProductionTotalTus = -1;
        private bool machineRuntimeLoopInstalled;
        private bool equipmentSlotsRuntimeHooksInstalled;
        private bool equipmentSlotsUiHooksInstalled;
        private bool equipmentSlotsUiRendered;
        private bool equipmentSlotsUiEvidenceRecorded;
        private bool equipmentSlotsUiBindDiagnosticLogged;
        private bool equipmentSlotsUiCloneDiagnosticLogged;
        private bool equipmentSlotsUiInteractionFailureLogged;
        private bool equipmentSlotsApplyingFunctions;
        private bool equipmentSlotsOrphanRecoveryChecked;
        private DateTimeOffset lastEquipmentSlotsUiRefreshAt = DateTimeOffset.MinValue;
        private string equipmentSlotsUiLastSummary = string.Empty;

        public DolocTownExperimentalBridgeApi(DTMAPI.Core.Runtime.DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal void AttachActionSpeedService(ActionSpeedService service)
        {
            actionSpeedService = service;
        }


        internal int ActionSpeedApplicationCount => actionSpeedService?.ActionSpeedApplicationCount ?? 0;

        internal string LastActionSpeedApplicationSummary => actionSpeedService?.LastActionSpeedApplicationSummary ?? string.Empty;

        internal int ActionSpeedContinuousUseApplicationCount => actionSpeedService?.ActionSpeedContinuousUseApplicationCount ?? 0;

        internal string LastActionSpeedContinuousUseSummary => actionSpeedService?.LastActionSpeedContinuousUseSummary ?? string.Empty;

        internal string LastActionSpeedAutoFillSummary => actionSpeedService?.LastActionSpeedAutoFillSummary ?? string.Empty;

        internal int ActionSpeedAutoFillApplicationCount => actionSpeedService?.ActionSpeedAutoFillApplicationCount ?? 0;

        internal bool SuppressActionSpeedAutoFillForSmoke
        {
            get => actionSpeedService?.SuppressActionSpeedAutoFillForSmoke ?? false;
            set
            {
                if (actionSpeedService != null)
                    actionSpeedService.SuppressActionSpeedAutoFillForSmoke = value;
            }
        }

        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId)
        {
            if (actionSpeedService != null)
                return actionSpeedService.TryGetConfiguredActionSpeedOwner(out ownerId);

            ownerId = string.Empty;
            return false;
        }

        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId)
        {
            if (actionSpeedService != null)
                return actionSpeedService.TryGetConfiguredActionSpeedInteractionOwner(out ownerId);

            ownerId = string.Empty;
            return false;
        }

        internal void RestoreActionSpeed(string reason)
        {
            actionSpeedService?.RestoreActionSpeed(reason);
        }

        internal bool ForceMachineProductionDueForSmoke { get; set; }

        internal void UpdateRuntimeAutomation(bool forceMachineProductionPoll = false)
        {
            RecoverOrphanEquipmentSlotsIfNeeded();
            UpdateMachineProduction(forceMachineProductionPoll);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("runtime", force: false);
            UpdateMovementDebugLease(forceMachineProductionPoll ? "runtime-forced" : "runtime");
        }


        private static bool TryPlaceNativeItemInBackpack(Type dolocApi, string itemId, int count, out string message)
        {
            message = string.Empty;
            if (dolocApi == null)
            {
                message = "DolocAPI is not available.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                message = "Invalid item id or count.";
                return false;
            }

            MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
            object?[] queryArgs = new object?[] { itemId, null };
            if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
            {
                message = "Item " + itemId + " is not present in DolocConfig.Tables.TbItem.";
                return false;
            }

            MethodInfo? canPlaceItem = dolocApi.GetMethod("CanPlaceItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
            MethodInfo? tryPlaceInBackpack = dolocApi.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            if (canPlaceItem == null || tryPlaceInBackpack == null)
            {
                message = "Native backpack placement methods are not available.";
                return false;
            }

            object? canPlace = canPlaceItem.Invoke(null, new object?[] { itemId, count });
            if (!(canPlace is bool okToPlace) || !okToPlace)
            {
                message = "Backpack cannot place " + itemId + " x" + count + ".";
                return false;
            }

            object? placed = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, count, false });
            if (!(placed is bool ok) || !ok)
            {
                message = "DolocAPI.TryPlaceInBackpack returned false for " + itemId + " x" + count + ".";
                return false;
            }

            message = "Placed " + itemId + " x" + count + " through native backpack placement.";
            return true;
        }


        private static IEnumerable<object> EnumerateMachineCandidateEquipments(Type dolocApi, object archive, object currentRoom)
        {
            var visitedEquipment = new HashSet<int>();
            foreach (object room in EnumerateMachineCandidateRooms(dolocApi, archive, currentRoom))
            {
                foreach (object equipment in EnumerateEquipments(room))
                {
                    int key = RuntimeHelpers.GetHashCode(equipment);
                    if (visitedEquipment.Add(key))
                        yield return equipment;
                }
            }
        }

        private static IEnumerable<object> EnumerateMachineCandidateRooms(Type dolocApi, object archive, object currentRoom)
        {
            var visitedRooms = new HashSet<int>();
            void AddRoom(object? room, List<object> rooms)
            {
                if (room == null)
                    return;
                int key = RuntimeHelpers.GetHashCode(room);
                if (visitedRooms.Add(key))
                    rooms.Add(room);
            }

            var result = new List<object>();
            AddRoom(currentRoom, result);
            AddRoom(ReadMember(currentRoom, "RootRoom"), result);
            AddRoom(ReadStaticMember(dolocApi, "CurrentRootRoom"), result);
            AddRoom(ReadMember(archive, "currentRoom"), result);
            AddRoom(ReadMember(archive, "MainFarm"), result);
            object? farmData = ReadMember(archive, "farmData");
            AddRoom(farmData == null ? null : ReadMember(farmData, "currentRoom"), result);
            AddRoom(farmData == null ? null : ReadMember(farmData, "MainFarm"), result);
            return result;
        }

        private static IEnumerable<object> EnumerateEquipments(object room)
        {
            var visitedRooms = new HashSet<object>();
            foreach (object equipment in EnumerateEquipments(room, visitedRooms))
                yield return equipment;
        }

        private static IEnumerable<object> EnumerateEquipments(object room, HashSet<object> visitedRooms)
        {
            if (room == null || !visitedRooms.Add(room))
                yield break;

            object? equipmentManager = ReadMember(room, "DM_equipment");
            object? allEquipments = equipmentManager == null ? null : ReadMember(equipmentManager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object? equipment in enumerable)
                {
                    if (equipment != null)
                        yield return equipment;
                }
            }

            object? buildingManager = ReadMember(room, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            if (!(buildings is IEnumerable buildingEnumerable))
                yield break;

            foreach (object? building in buildingEnumerable)
            {
                if (building == null)
                    continue;
                object? childRoom = ReadMember(building, "room");
                if (childRoom == null)
                    continue;
                foreach (object childEquipment in EnumerateEquipments(childRoom, visitedRooms))
                    yield return childEquipment;
            }
        }


        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static double ClampDouble(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }


        private static bool IsAcceleratedTool(object? tool)
        {
            if (tool == null)
                return false;

            object? toolType = ReadMember(tool, "ToolType");
            string name = toolType?.ToString() ?? string.Empty;
            return name.Equals("AXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("SICKLE", StringComparison.OrdinalIgnoreCase);
        }

        internal static void ShowNativeSmallMessage(string message, bool error)
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                string methodName = error ? "ShowMessageBoxSmallErr" : "ShowMessageBoxSmall";
                MethodInfo? method = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(string));
                if (method == null)
                    return;
                ParameterInfo[] parameters = method.GetParameters();
                object?[] args = parameters.Length == 1
                    ? new object?[] { message }
                    : parameters.Length == 2
                        ? new object?[] { message, 1.2f }
                        : new object?[] { message, 1.2f, false };
                method.Invoke(null, args);
            }
            catch
            {
            }
        }

        internal static double ReadAnimatorSpeed(object animator, double fallback)
        {
            object? value = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animator);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        internal static bool TryWriteAnimatorSpeed(object animator, double value)
        {
            try
            {
                PropertyInfo? speed = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance);
                if (speed == null || !speed.CanWrite)
                    return false;
                speed.SetValue(animator, Convert.ChangeType(value, speed.PropertyType));
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(1, value));
        }

        internal static double ClampSeconds(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return min;
            return Math.Min(max, Math.Max(min, value));
        }

        internal static object? TryGetDungeonResourceFromCollider(object collider)
        {
            return GameBridgeNativeHelpers.TryGetDungeonResourceFromCollider(collider);
        }

        internal static bool IsResourceRemoved(object resource)
        {
            return GameBridgeNativeHelpers.IsResourceRemoved(resource);
        }

        internal static string GetResourceName(object resource)
        {
            return GameBridgeNativeHelpers.GetResourceName(resource);
        }

        internal static string GetResourceClass(object resource)
        {
            return GameBridgeNativeHelpers.GetResourceClass(resource);
        }

        internal static string FormatRatio(double value)
        {
            return GameBridgeNativeHelpers.FormatRatio(value);
        }

        internal static int ReadIntMember(object instance, string name, int fallback)
        {
            return GameBridgeNativeHelpers.ReadIntMember(instance, name, fallback);
        }

        internal static double ReadDoubleMember(object instance, string name, double fallback)
        {
            return GameBridgeNativeHelpers.ReadDoubleMember(instance, name, fallback);
        }

        private static bool TryGenerateNativeItem(string itemId, int count, out object? item, out string reason, out string message)
        {
            item = null;
            reason = string.Empty;
            message = string.Empty;
            Type? itemFactory = ResolveType("DolocTown.ItemFactory, Assembly-CSharp");
            MethodInfo? generateItem = itemFactory?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.Equals("GenerateItem", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 3 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(int) &&
                        parameters[2].ParameterType.IsByRef;
                });
            if (generateItem == null)
            {
                reason = "missing-item-factory";
                message = "DolocTown.ItemFactory.GenerateItem(string,int,out Item) was not found.";
                return false;
            }

            object?[] args = { itemId, Math.Max(1, count), null };
            object? ok = generateItem.Invoke(null, args);
            item = args[2];
            if (!(ok is bool success) || !success || item == null)
            {
                reason = "item-generation-failed";
                message = "ItemFactory.GenerateItem failed for " + itemId + ".";
                return false;
            }

            return true;
        }

        private static bool InvokeBoolNoArgs(object instance, string methodName, bool fallback)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(instance.GetType(), methodName, 0);
                object? value = method?.Invoke(instance, null);
                return value is bool result ? result : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        internal static Type? ResolveType(string assemblyQualifiedName)
        {
            return GameBridgeNativeHelpers.ResolveType(assemblyQualifiedName);
        }

        internal static bool IsTypeOrBase(Type type, string fullName)
        {
            return GameBridgeNativeHelpers.IsTypeOrBase(type, fullName);
        }

        private static bool ImplementsInterface(Type? type, string fullName)
        {
            if (type == null)
                return false;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (string.Equals(interfaceType.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        internal static object? ReadStaticMember(Type? type, string name)
        {
            return GameBridgeNativeHelpers.ReadStaticMember(type, name);
        }

        internal static bool ReadStaticBoolMember(Type? type, string name, bool fallback)
        {
            object? value = ReadStaticMember(type, name);
            return value is bool result ? result : fallback;
        }

        internal static string FirstText(params string[] values)
        {
            return GameBridgeNativeHelpers.FirstText(values);
        }


        internal static string ReadStringMember(object instance, string name)
        {
            return GameBridgeNativeHelpers.ReadStringMember(instance, name);
        }

        internal static string ReadStringMember(object instance, string name, string fallback)
        {
            return GameBridgeNativeHelpers.ReadStringMember(instance, name, fallback);
        }

        internal static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            return GameBridgeNativeHelpers.ReadBoolMember(instance, name, fallback);
        }

        internal static bool WriteBoolMember(object instance, string name, bool value)
        {
            return WriteMember(instance, name, typeof(bool), value);
        }

        internal static bool WriteFloatMember(object instance, string name, float value)
        {
            return WriteMember(instance, name, typeof(float), value);
        }

        private static bool WriteMember(object instance, string name, Type expectedType, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == expectedType)
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == expectedType)
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        internal static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool TrySetMemberValue(object? instance, string name, object value)
        {
            if (instance == null)
                return false;

            try
            {
                return SetMemberValue(instance, name, value);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeNoArg(object? instance, string methodName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(methodName))
                return false;

            try
            {
                MethodInfo? method = FindMethodInHierarchy(instance.GetType(), methodName, 0);
                method?.Invoke(instance, null);
                return method != null;
            }
            catch
            {
                return false;
            }
        }

        internal static object? ReadMember(object instance, string name)
        {
            return GameBridgeNativeHelpers.ReadMember(instance, name);
        }

        internal static object? CloneUnityObject(object original)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? instantiate = objectType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Instantiate" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == objectType);
            return instantiate?.Invoke(null, new[] { original });
        }


        internal static void DestroyUnityObject(object instance)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? destroy = objectType?.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
            destroy?.Invoke(null, new[] { instance });
        }

        internal static object? GetComponent(object gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null)
                return null;
            if (componentType.IsInstanceOfType(gameObject))
                return gameObject;

            foreach (MethodInfo method in gameObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "GetComponent" && parameters.Length == 1 && parameters[0].ParameterType == typeof(Type))
                {
                    object? result = method.Invoke(gameObject, new object[] { componentType });
                    if (result != null)
                        return result;
                }
            }

            foreach (MethodInfo method in gameObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "GetComponents" && parameters.Length == 1 && parameters[0].ParameterType == typeof(Type))
                {
                    object? result = method.Invoke(gameObject, new object[] { componentType });
                    foreach (object component in EnumerateObjects(result))
                    {
                        if (componentType.IsInstanceOfType(component))
                            return component;
                    }
                }
            }

            return null;
        }

        private static object? GetComponentInChildren(object gameObject, Type componentType, bool includeInactive)
        {
            MethodInfo? getComponent = gameObject.GetType().GetMethod("GetComponentInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            return getComponent?.Invoke(gameObject, new object[] { componentType, includeInactive });
        }

        private static List<object> GetSpriteRenderers(object gameObject, bool includeInactive)
        {
            var renderers = new List<object>();
            Type? spriteRendererType = ResolveType("UnityEngine.SpriteRenderer, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.SpriteRenderer, UnityEngine");
            if (spriteRendererType == null)
                return renderers;

            object? rootRenderer = GetComponent(gameObject, spriteRendererType);
            if (rootRenderer != null)
                renderers.Add(rootRenderer);

            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            object? result = getComponents?.Invoke(gameObject, new object[] { spriteRendererType, includeInactive });
            if (result is IEnumerable components)
            {
                foreach (object component in components)
                {
                    if (component == null || renderers.Any(existing => ReferenceEquals(existing, component)))
                        continue;
                    renderers.Add(component);
                }
            }

            return renderers;
        }

        private static bool IsComponentUnder(object component, object? parentComponent)
        {
            if (parentComponent == null)
                return false;

            object? current = ReadMember(component, "transform");
            object? parentTransform = ReadMember(parentComponent, "transform");
            int guard = 0;
            while (current != null && parentTransform != null && guard++ < 64)
            {
                if (ReferenceEquals(current, parentTransform))
                    return true;
                current = ReadMember(current, "parent");
            }

            return false;
        }

        internal static void SetParent(object transform, object parent, bool worldPositionStays)
        {
            MethodInfo? setParent = transform.GetType().GetMethod("SetParent", BindingFlags.Public | BindingFlags.Instance, null, new[] { parent.GetType(), typeof(bool) }, null)
                ?? transform.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "SetParent" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(bool));
            setParent?.Invoke(transform, new object[] { parent, worldPositionStays });
        }

        internal static void SetActive(object gameObject, bool active)
        {
            MethodInfo? setActive = gameObject.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
            setActive?.Invoke(gameObject, new object[] { active });
        }


        internal static object? CreateUnityVector3(double x, double y, double z)
        {
            Type? vector3 = ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector3, UnityEngine");
            return vector3 == null ? null : Activator.CreateInstance(vector3, (float)x, (float)y, (float)z);
        }

        private static object? CreateUnityVector2(double x, double y)
        {
            Type? vector2 = ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2, UnityEngine");
            return vector2 == null ? null : Activator.CreateInstance(vector2, (float)x, (float)y);
        }

        private static object? CreateUnityVector2Int(int x, int y)
        {
            Type? vector2Int = ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null ? null : Activator.CreateInstance(vector2Int, x, y);
        }

        private static object? CreateUnityColor(DtmColor color)
        {
            Type? unityColor = ResolveType("UnityEngine.Color, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Color, UnityEngine");
            if (unityColor == null)
                return null;
            float r = (float)Math.Max(0, Math.Min(1, color.R));
            float g = (float)Math.Max(0, Math.Min(1, color.G));
            float b = (float)Math.Max(0, Math.Min(1, color.B));
            float a = (float)Math.Max(0, Math.Min(1, color.A));
            return Activator.CreateInstance(unityColor, r, g, b, a);
        }

        internal static MethodInfo? FindMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            return GameBridgeNativeHelpers.FindMethodInHierarchy(type, name, parameterCount);
        }

        internal static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = ResolveType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.ScreenCapture, UnityEngine");
                MethodInfo? captureTexture = screenCapture?.GetMethod("CaptureScreenshotAsTexture", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                object? texture = captureTexture?.Invoke(null, null);
                if (texture != null)
                {
                    try
                    {
                        MethodInfo? encodeToPng = texture.GetType().GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        if (encodeToPng?.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                            File.WriteAllBytes(path, bytes);
                            return true;
                        }
                    }
                    finally
                    {
                        DestroyUnityObject(texture);
                    }
                }

                if (TryCaptureScreenshotWithReadPixels(path))
                    return true;

                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCaptureScreenshotWithReadPixels(string path)
        {
            object? texture = null;
            try
            {
                Type? screenType = ResolveType("UnityEngine.Screen, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Screen, UnityEngine");
                Type? texture2DType = ResolveType("UnityEngine.Texture2D, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Texture2D, UnityEngine");
                Type? textureFormatType = ResolveType("UnityEngine.TextureFormat, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.TextureFormat, UnityEngine");
                Type? rectType = ResolveType("UnityEngine.Rect, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Rect, UnityEngine");
                if (screenType == null || texture2DType == null || textureFormatType == null || rectType == null)
                    return false;

                int width = Math.Max(1, Convert.ToInt32(screenType.GetProperty("width", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), CultureInfo.InvariantCulture));
                int height = Math.Max(1, Convert.ToInt32(screenType.GetProperty("height", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), CultureInfo.InvariantCulture));
                object format = Enum.Parse(textureFormatType, "RGB24");
                texture = Activator.CreateInstance(texture2DType, width, height, format, false);
                if (texture == null)
                    return false;

                object rect = Activator.CreateInstance(rectType, 0f, 0f, (float)width, (float)height)!;
                MethodInfo? readPixels = texture2DType.GetMethod("ReadPixels", BindingFlags.Public | BindingFlags.Instance, null, new[] { rectType, typeof(int), typeof(int) }, null);
                MethodInfo? apply = texture2DType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                MethodInfo? encodeToPng = texture2DType.GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readPixels == null || apply == null || encodeToPng == null)
                    return false;

                readPixels.Invoke(texture, new object[] { rect, 0, 0 });
                apply.Invoke(texture, null);
                if (encodeToPng.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                    File.WriteAllBytes(path, bytes);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (texture != null)
                    DestroyUnityObject(texture);
            }
        }


        private static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name.Equals(name, StringComparison.Ordinal) && method.GetParameters().Length == parameterCount)
                    return method;
            }
            return null;
        }

        internal static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value == null)
                yield break;
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private static int InvokeInt(MethodInfo? method, object? target, object?[] args, int fallback)
        {
            try
            {
                object? value = method?.Invoke(target, args);
                return value == null ? fallback : Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }


        internal static double ReadVectorComponent(object? vector, string name)
        {
            if (vector == null)
                return double.NaN;
            object? value = ReadMember(vector, name);
            return value == null ? double.NaN : Convert.ToDouble(value);
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (!keys.Add(key))
                return;
            runtime.RuntimeMonitor.Log(message);
        }

    }
}
