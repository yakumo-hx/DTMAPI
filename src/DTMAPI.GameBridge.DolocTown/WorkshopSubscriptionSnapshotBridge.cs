using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        internal bool CaptureNativeWorkshopSubscriptions(string reason, object? knownModManager = null, bool requireSteamInitialized = true)
        {
            if (!TryCaptureNativeWorkshopSubscriptionSnapshot(
                    reason,
                    knownModManager,
                    requireSteamInitialized,
                    publishUnavailableOnFailure: true,
                    out NativeWorkshopSubscriptionSnapshot? snapshot))
                return false;

            PublishNativeWorkshopSubscriptionSnapshot(snapshot!, reason);
            return true;
        }

        private bool TryCaptureNativeWorkshopSubscriptionSnapshot(
            string reason,
            object? knownModManager,
            bool requireSteamInitialized,
            bool publishUnavailableOnFailure,
            out NativeWorkshopSubscriptionSnapshot? snapshot)
        {
            string source = "DolocTown.Config.ModManager.GetSubscribedMods";
            snapshot = null;
            try
            {
                object? modManager = knownModManager ?? ResolveNativeModManager();
                if (modManager == null)
                    throw new InvalidOperationException("DolocAPI.modManager is unavailable.");
                if (requireSteamInitialized && !IsSteamInitialized())
                    throw new InvalidOperationException("SteamManager.Initialized is false.");

                MethodInfo? getSubscribedMods = modManager.GetType().GetMethod(
                    "GetSubscribedMods",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (getSubscribedMods == null)
                    throw new MissingMethodException(modManager.GetType().FullName, "GetSubscribedMods");

                object? nativeRows;
                try
                {
                    nativeRows = getSubscribedMods.Invoke(modManager, null);
                }
                catch (Exception ex)
                {
                    Exception root = ex is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : ex;
                    throw new InvalidOperationException("GetSubscribedMods required subscription-owner capture failed: " + root.GetType().Name + ": " + root.Message, root);
                }
                if (!(nativeRows is IEnumerable enumerable))
                    throw new InvalidOperationException("GetSubscribedMods did not return an enumerable result.");

                MethodInfo? getDirectory = modManager.GetType().GetMethod(
                    "GetSubscribedModDirectory",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Dictionary<ulong, NativeModInfoRow> nativeInfoByWorkshopId = CaptureNativeModInfos(modManager, out string nativeInfoFailure);
                var subscriptions = new List<NativeWorkshopSubscription>();
                foreach (object nativeId in enumerable)
                {
                    if (!TryReadPublishedFileId(nativeId, out ulong workshopId) || workshopId == 0)
                        continue;

                    string installPath = string.Empty;
                    if (getDirectory != null)
                    {
                        try
                        {
                            object?[] args = { nativeId, string.Empty };
                            object? directoryResult = getDirectory.Invoke(modManager, args);
                            if (directoryResult is bool found && found)
                                installPath = args[1] as string ?? string.Empty;
                        }
                        catch
                        {
                            installPath = string.Empty;
                        }
                    }

                    if (nativeInfoByWorkshopId.TryGetValue(workshopId, out NativeModInfoRow nativeInfo))
                    {
                        if (string.IsNullOrWhiteSpace(installPath))
                            installPath = nativeInfo.RootPath;
                        subscriptions.Add(new NativeWorkshopSubscription(workshopId, installPath, nativeInfo.Enabled, nativeInfo.Priority, nativeInfo.OfficialId));
                    }
                    else
                    {
                        subscriptions.Add(new NativeWorkshopSubscription(workshopId, installPath));
                    }
                }

                NativeWorkshopSubscription[] rows = subscriptions
                    .GroupBy(row => row.WorkshopId)
                    .Select(group => group.First())
                    .OrderBy(row => row.WorkshopId)
                    .ToArray();
                snapshot = new NativeWorkshopSubscriptionSnapshot(
                    source,
                    nativeInfoFailure,
                    rows);
                runtime.RuntimeMonitor.Log(
                    "Captured native Workshop subscription candidate reason=" + (reason ?? string.Empty) +
                    "; count=" + rows.Length.ToString(CultureInfo.InvariantCulture) +
                    "; nativeInfoCount=" + nativeInfoByWorkshopId.Count.ToString(CultureInfo.InvariantCulture) +
                    "; nativeInfoFailure=" + (string.IsNullOrWhiteSpace(nativeInfoFailure) ? "none" : nativeInfoFailure) + ".",
                    string.IsNullOrWhiteSpace(nativeInfoFailure) ? DTMAPI.Abstractions.LogLevel.Info : DTMAPI.Abstractions.LogLevel.Warn);
                return true;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : ex;
                if (publishUnavailableOnFailure)
                {
                    runtime.UpdateNativeWorkshopSubscriptions(
                        available: false,
                        source: source,
                        failure: root.GetType().Name + ": " + root.Message,
                        subscriptions: Array.Empty<NativeWorkshopSubscription>());
                }
                runtime.RuntimeMonitor.Log(
                    "Native Workshop subscription candidate unavailable reason=" + (reason ?? string.Empty) +
                    "; error=" + root.GetType().Name + ": " + root.Message + ".",
                    DTMAPI.Abstractions.LogLevel.Warn);
                return false;
            }
        }

        private void PublishNativeWorkshopSubscriptionSnapshot(
            NativeWorkshopSubscriptionSnapshot snapshot,
            string reason)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            runtime.UpdateNativeWorkshopSubscriptions(
                available: true,
                source: snapshot.Source,
                failure: snapshot.Failure,
                subscriptions: snapshot.Subscriptions);
            runtime.RuntimeMonitor.Log(
                "Published native Workshop subscription authority reason=" +
                (reason ?? string.Empty) + "; count=" +
                snapshot.Subscriptions.Length.ToString(CultureInfo.InvariantCulture) + ".",
                string.IsNullOrWhiteSpace(snapshot.Failure)
                    ? DTMAPI.Abstractions.LogLevel.Info
                    : DTMAPI.Abstractions.LogLevel.Warn);
        }

        private static object? ResolveNativeModManager()
        {
            Type? dolocApi = Type.GetType("DolocAPI, Assembly-CSharp", throwOnError: false);
            if (dolocApi == null)
                return null;

            FieldInfo? persistenceField = dolocApi.GetField("dataPersistenceManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (persistenceField != null && persistenceField.GetValue(null) == null)
                throw new InvalidOperationException("DolocAPI.dataPersistenceManager is not initialized; native ModManager is unavailable before GameManager.Awake completes.");

            PropertyInfo? property = dolocApi.GetProperty("modManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                dolocApi.GetProperty("ModManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(null, null);

            FieldInfo? field = dolocApi.GetField("modManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                dolocApi.GetField("_modManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(null);
        }

        private static bool IsSteamInitialized()
        {
            Type? steamManager = Type.GetType("SteamManager, Assembly-CSharp", throwOnError: false);
            PropertyInfo? initialized = steamManager?.GetProperty("Initialized", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return initialized?.GetValue(null, null) is bool value && value;
        }

        private static Dictionary<ulong, NativeModInfoRow> CaptureNativeModInfos(object modManager, out string failure)
        {
            var rows = new Dictionary<ulong, NativeModInfoRow>();
            failure = string.Empty;
            try
            {
                MethodInfo? getAllValid = modManager.GetType().GetMethod(
                    "GetAllValidModInfos",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (getAllValid == null)
                {
                    failure = "GetAllValidModInfos enrichment unavailable: method not found.";
                    return rows;
                }
                if (!(getAllValid.Invoke(modManager, null) is IEnumerable nativeRows))
                {
                    failure = "GetAllValidModInfos enrichment unavailable: result was not enumerable.";
                    return rows;
                }

                foreach (object? row in nativeRows)
                {
                    if (row == null || !TryReadUInt64Member(row, "workshopId", out ulong workshopId) || workshopId == 0)
                        continue;
                    string officialId = ReadStringMember(row, "id");
                    string source = ReadStringMember(row, "source");
                    if (!string.IsNullOrWhiteSpace(source) && !source.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                        continue;
                    rows[workshopId] = new NativeModInfoRow(
                        officialId,
                        ReadStringMember(row, "rootPath"),
                        ReadBooleanMember(row, "enabled"),
                        ReadInt32Member(row, "priority", -1));
                }
                return rows;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : ex;
                string detail = root.GetType().Name + ": " + root.Message;
                if (detail.Length > 512)
                    detail = detail.Substring(0, 512);
                failure = "GetAllValidModInfos enrichment failed: " + detail;
                rows.Clear();
                return rows;
            }
        }

        private static string ReadStringMember(object value, string name)
        {
            object? raw = ReadNativeSnapshotMember(value, name);
            return raw?.ToString() ?? string.Empty;
        }

        private static bool ReadBooleanMember(object value, string name)
        {
            object? raw = ReadNativeSnapshotMember(value, name);
            try { return raw != null && Convert.ToBoolean(raw, CultureInfo.InvariantCulture); }
            catch { return false; }
        }

        private static int ReadInt32Member(object value, string name, int fallback)
        {
            object? raw = ReadNativeSnapshotMember(value, name);
            try { return raw == null ? fallback : Convert.ToInt32(raw, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        private static bool TryReadUInt64Member(object value, string name, out ulong result)
        {
            object? raw = ReadNativeSnapshotMember(value, name);
            try
            {
                result = raw == null ? 0UL : Convert.ToUInt64(raw, CultureInfo.InvariantCulture);
                return result != 0;
            }
            catch
            {
                result = 0;
                return false;
            }
        }

        private static object? ReadNativeSnapshotMember(object value, string name)
        {
            Type type = value.GetType();
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(value);
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(value, null);
        }

        private sealed class NativeModInfoRow
        {
            public NativeModInfoRow(string officialId, string rootPath, bool enabled, int priority)
            {
                OfficialId = officialId ?? string.Empty;
                RootPath = rootPath ?? string.Empty;
                Enabled = enabled;
                Priority = priority;
            }

            public string OfficialId { get; }
            public string RootPath { get; }
            public bool Enabled { get; }
            public int Priority { get; }
        }

        private sealed class NativeWorkshopSubscriptionSnapshot
        {
            public NativeWorkshopSubscriptionSnapshot(
                string source,
                string failure,
                NativeWorkshopSubscription[] subscriptions)
            {
                Source = source ?? string.Empty;
                Failure = failure ?? string.Empty;
                Subscriptions = subscriptions ?? Array.Empty<NativeWorkshopSubscription>();
            }

            public string Source { get; }
            public string Failure { get; }
            public NativeWorkshopSubscription[] Subscriptions { get; }
        }

        private static bool TryReadPublishedFileId(object value, out ulong workshopId)
        {
            workshopId = 0;
            if (value == null)
                return false;
            if (value is ulong direct)
            {
                workshopId = direct;
                return true;
            }

            Type type = value.GetType();
            FieldInfo? field = type.GetField("m_PublishedFileId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                type.GetField("PublishedFileId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object? raw = field?.GetValue(value);
            if (raw == null)
            {
                PropertyInfo? property = type.GetProperty("m_PublishedFileId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    type.GetProperty("PublishedFileId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                raw = property?.GetValue(value, null);
            }

            try
            {
                workshopId = Convert.ToUInt64(raw, CultureInfo.InvariantCulture);
                return workshopId != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
