using System;
using System.IO;
using System.Reflection;

namespace DTMAPI.MoreEquipmentSlots
{
    internal interface INativeItemPlacementGateway
    {
        int CountBackpack(string itemId);

        int CountUnacceptedMail(string itemId);

        bool TryPlaceBackpackOnly(string itemId);

        bool TrySendMail(string itemId);

        bool TryCostBackpack(string itemId);
    }

    internal sealed class EquipmentSlotNativePlacement
    {
        private readonly INativeItemPlacementGateway gateway;

        internal EquipmentSlotNativePlacement(
            INativeItemPlacementGateway gateway) =>
            this.gateway = gateway ??
                throw new ArgumentNullException(nameof(gateway));

        internal NativeRecoveryObservation Observe(
            string itemId,
            string saveFingerprint) =>
            new NativeRecoveryObservation(
                saveFingerprint,
                gateway.CountBackpack(itemId),
                gateway.CountUnacceptedMail(itemId));

        internal NativePlacementResult PlaceOne(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return Failure(
                    "Native placement requires an item identity.");
            }

            int beforeBackpack =
                gateway.CountBackpack(itemId);
            int beforeMail =
                gateway.CountUnacceptedMail(itemId);
            bool backpackReported;
            try
            {
                backpackReported =
                    gateway.TryPlaceBackpackOnly(itemId);
            }
            catch (Exception ex)
            {
                throw Unknown(
                    itemId,
                    beforeBackpack,
                    beforeMail,
                    "Backpack invocation failed after native mutation may have begun.",
                    ex);
            }

            EquipmentSlotNativeMutationResolution
                backpackResolution =
                    ObserveAfterMutation(
                        itemId,
                        beforeBackpack,
                        beforeMail,
                        "backpack",
                        out int afterBackpack,
                        out int afterBackpackMail);
            if (backpackResolution ==
                EquipmentSlotNativeMutationResolution.Backpack)
            {
                return new NativePlacementResult(
                    NativePlacementKind.Backpack,
                    1,
                    0,
                    "Exact native evidence records one backpack item.",
                    afterBackpack,
                    afterBackpackMail);
            }
            if (backpackResolution !=
                    EquipmentSlotNativeMutationResolution.None ||
                backpackReported)
            {
                throw Unknown(
                    itemId,
                    beforeBackpack,
                    beforeMail,
                    "Backpack invocation returned " +
                    backpackReported +
                    " with " +
                    backpackResolution +
                    " count evidence.");
            }

            bool mailReported;
            try
            {
                mailReported = gateway.TrySendMail(itemId);
            }
            catch (Exception ex)
            {
                throw Unknown(
                    itemId,
                    beforeBackpack,
                    beforeMail,
                    "Mail invocation failed after native mutation may have begun.",
                    ex);
            }

            EquipmentSlotNativeMutationResolution mailResolution =
                ObserveAfterMutation(
                    itemId,
                    beforeBackpack,
                    beforeMail,
                    "mail",
                    out int finalBackpack,
                    out int finalMail);
            if (mailResolution ==
                EquipmentSlotNativeMutationResolution.Mail)
            {
                return new NativePlacementResult(
                    NativePlacementKind.Mail,
                    0,
                    1,
                    "Exact native evidence records one unaccepted mail item.",
                    finalBackpack,
                    finalMail);
            }
            if (mailResolution ==
                    EquipmentSlotNativeMutationResolution.None &&
                !mailReported)
            {
                return Failure(
                    "Native backpack and mail both reported failure and exact evidence remained unchanged.",
                    finalBackpack,
                    finalMail);
            }

            throw Unknown(
                itemId,
                beforeBackpack,
                beforeMail,
                "Mail invocation returned " +
                mailReported +
                " with " +
                mailResolution +
                " count evidence.");
        }

        internal NativePlacementResult
            PlaceOneWithImmediateEvidence(string itemId)
        {
            try
            {
                return PlaceOne(itemId);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    pending)
            {
                // A single synchronous re-observation is the last authority
                // for this invocation. Callers must retain their Working or
                // escrow state and block SaveGame if this remains unknown.
                return ResolveUnknown(pending);
            }
        }

        internal NativePlacementResult ResolveUnknown(
            EquipmentSlotNativeMutationOutcomeUnknownException
                pending)
        {
            if (pending == null)
                throw new ArgumentNullException(nameof(pending));
            EquipmentSlotNativeMutationResolution resolution =
                ObserveAfterMutation(
                    pending.ItemId,
                    pending.BeforeBackpackCount,
                    pending.BeforeMailCount,
                    "reconciliation",
                    out int afterBackpack,
                    out int afterMail,
                    pending.ExpectedCount);
            if (resolution ==
                EquipmentSlotNativeMutationResolution.Backpack)
            {
                return new NativePlacementResult(
                    NativePlacementKind.Backpack,
                    pending.ExpectedCount,
                    0,
                    "Outcome-unknown placement reconciled to one exact backpack destination.",
                    afterBackpack,
                    afterMail);
            }
            if (resolution ==
                EquipmentSlotNativeMutationResolution.Mail)
            {
                return new NativePlacementResult(
                    NativePlacementKind.Mail,
                    0,
                    pending.ExpectedCount,
                    "Outcome-unknown placement reconciled to one exact mail destination.",
                    afterBackpack,
                    afterMail);
            }
            if (resolution ==
                EquipmentSlotNativeMutationResolution.None)
            {
                return Failure(
                    "Outcome-unknown placement reconciled to an unchanged native preimage.",
                    afterBackpack,
                    afterMail);
            }
            throw Unknown(
                pending.ItemId,
                pending.BeforeBackpackCount,
                pending.BeforeMailCount,
                "Reconciliation still has ambiguous native count evidence.",
                pending,
                pending.ExpectedCount);
        }

        internal bool TryWithdrawOne(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            int before = gateway.CountBackpack(itemId);
            bool reported;
            try
            {
                reported = gateway.TryCostBackpack(itemId);
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence.Unknown(
                    itemId,
                    1,
                    before,
                    0,
                    "Native backpack withdrawal threw after mutation may have begun.",
                    ex);
            }

            int after;
            try
            {
                after = gateway.CountBackpack(itemId);
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence.Unknown(
                    itemId,
                    1,
                    before,
                    0,
                    "Native backpack withdrawal post-state is unreadable.",
                    ex);
            }
            int delta = after - before;
            if (delta == -1)
                return true;
            if (delta == 0 && !reported)
                return false;
            throw EquipmentSlotNativeMutationEvidence.Unknown(
                itemId,
                1,
                before,
                0,
                "Native backpack withdrawal returned " +
                reported +
                " with contradictory count evidence: before=" +
                before +
                ", after=" +
                after +
                ".");
        }

        private EquipmentSlotNativeMutationResolution
            ObserveAfterMutation(
                string itemId,
                int beforeBackpack,
                int beforeMail,
                string phase,
                out int afterBackpack,
                out int afterMail,
                int expectedCount = 1)
        {
            afterBackpack = -1;
            afterMail = -1;
            try
            {
                afterBackpack =
                    gateway.CountBackpack(itemId);
                afterMail =
                    gateway.CountUnacceptedMail(itemId);
                return EquipmentSlotNativeMutationEvidence
                    .Resolve(
                        beforeBackpack,
                        beforeMail,
                        afterBackpack,
                        afterMail,
                        expectedCount);
            }
            catch (Exception ex)
            {
                throw Unknown(
                    itemId,
                    beforeBackpack,
                    beforeMail,
                    "Native evidence became unreadable after " +
                    phase +
                    " invocation.",
                    ex,
                    expectedCount);
            }
        }

        private static
            EquipmentSlotNativeMutationOutcomeUnknownException
            Unknown(
                string itemId,
                int beforeBackpack,
                int beforeMail,
                string reason,
                Exception? innerException = null,
                int expectedCount = 1) =>
            EquipmentSlotNativeMutationEvidence.Unknown(
                itemId,
                expectedCount,
                beforeBackpack,
                beforeMail,
                reason,
                innerException);

        private static NativePlacementResult Failure(
            string message,
            int afterBackpackCount = -1,
            int afterMailCount = -1) =>
            new NativePlacementResult(
                NativePlacementKind.Failure,
                0,
                0,
                message,
                afterBackpackCount,
                afterMailCount);
    }

    internal sealed class DolocTownItemPlacementGateway :
        INativeItemPlacementGateway
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static;
        private static readonly Type DolocApiType = typeof(DolocAPI);
        private static readonly MethodInfo CountItemMethod =
            DolocApiType.GetMethod(
                "CountItem",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(bool) },
                modifiers: null) ??
            throw new MissingMethodException(
                DolocApiType.FullName,
                "CountItem(string,bool)");
        private static readonly MethodInfo TryPlaceMethod =
            DolocApiType.GetMethod(
                "TryPlaceInBackpack",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(bool)
                },
                modifiers: null) ??
            throw new MissingMethodException(
                DolocApiType.FullName,
                "TryPlaceInBackpack(string,int,bool)");
        private static readonly MethodInfo SendMailMethod =
            ResolveSendMailMethod();
        public int CountBackpack(string itemId)
        {
            object? result = CountItemMethod.Invoke(
                null,
                new object[] { itemId, false });
            if (result is int count && count >= 0)
                return count;
            throw new InvalidDataException(
                "DolocAPI.CountItem returned an unreadable or negative backpack count for " +
                itemId +
                ".");
        }

        public int CountUnacceptedMail(string itemId)
            => EquipmentSlotNativeMailEvidence
                .CountUnacceptedDtmapiItemMail(
                    DolocApiType,
                    itemId);

        public bool TryPlaceBackpackOnly(string itemId)
        {
            object? result = TryPlaceMethod.Invoke(
                null,
                new object[] { itemId, 1, false });
            return result is bool placed && placed;
        }

        public bool TrySendMail(string itemId)
        {
            object?[] arguments =
            {
                itemId,
                1,
                null,
                null,
                null,
                "send_item_template"
            };
            object? result = SendMailMethod.Invoke(null, arguments);
            return result is bool sent && sent;
        }

        public bool TryCostBackpack(string itemId)
        {
            MethodInfo? method = DolocApiType.GetMethod(
                "CostItem",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(bool)
                },
                modifiers: null);
            object? result = method?.Invoke(
                null,
                new object[] { itemId, 1, false });
            return result is bool cost && cost;
        }

        private static MethodInfo ResolveSendMailMethod()
        {
            foreach (MethodInfo method in DolocApiType.GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "SendItemAsEmail")
                    continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 6 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(int))
                {
                    return method;
                }
            }

            throw new MissingMethodException(
                DolocApiType.FullName,
                "SendItemAsEmail(string,int,string,string,string,string)");
        }

        internal static EquipmentSlotSaveScope ReadCurrentScope(
            int archiveIndex)
        {
            object? archive =
                ReadStaticMember(DolocApiType, "archiveHandle");
            object? farmData = ReadMember(archive, "farmData");
            object? agentData = ReadMember(farmData, "agentData");
            object? baseData = ReadMember(
                archive,
                "baseDataOnLoad");
            long totalGameSeconds =
                ReadLong(
                    baseData,
                    "totalGameSeconds",
                    -1);
            string customName = FirstText(
                ReadString(agentData, "customPlayerName"),
                ReadString(baseData, "customPlayerName"));
            string playerName = FirstText(
                ReadString(agentData, "playerName"),
                customName);
            return new EquipmentSlotSaveScope
            {
                ArchiveIndex = archiveIndex,
                PlayerName = playerName,
                CustomPlayerName = customName,
                TotalGameSeconds = totalGameSeconds >= 0
                    ? totalGameSeconds
                    : (long?)null
            };
        }

        internal static int ReadCurrentArchiveIndex()
        {
            object? archive =
                ReadStaticMember(DolocApiType, "archiveHandle");
            object? value = ReadMember(
                archive,
                "archiveIndex");
            if (value is int archiveIndex && archiveIndex >= 0)
                return archiveIndex;
            throw new InvalidDataException(
                "MoreEquipmentSlots could not resolve a valid initialized native archive index for NewGame.");
        }

        internal static bool NativeCurrentSaveExists(
            int archiveIndex)
        {
            object handler = GetNativeSaveHandler();
            return File.Exists(
                GetNativeSavePath(
                    handler,
                    archiveIndex));
        }

        internal static void RequireNativeCurrentSaveMissing(
            int archiveIndex)
        {
            object handler = GetNativeSaveHandler();
            string path = GetNativeSavePath(
                handler,
                archiveIndex);
            if (File.Exists(path))
            {
                throw new InvalidDataException(
                    "MoreEquipmentSlots refused NewGame sidecar reset because the native current archive already exists: " +
                    path);
            }
        }

        internal static string GetNativeSaveFingerprint(
            int archiveIndex) =>
            GetNativeSaveFingerprint(
                archiveIndex,
                allowMissingCurrent: false);

        internal static string GetNativeSaveFingerprint(
            int archiveIndex,
            bool allowMissingCurrent)
        {
            object handler = GetNativeSaveHandler();
            string path = GetNativeSavePath(
                handler,
                archiveIndex);
            int backupCount = GetNativeBackupCount(handler);
            return allowMissingCurrent
                ? EquipmentSlotNativeCommitFingerprint
                    .ComputeAllowingMissingCurrent(
                        path,
                        backupCount)
                : EquipmentSlotNativeCommitFingerprint
                    .Compute(path, backupCount);
        }

        private static object GetNativeSaveHandler()
        {
            object? persistence = ReadStaticMember(
                DolocApiType,
                "dataPersistenceManager");
            object? handler =
                ReadMember(persistence, "fileDataHandler");
            if (handler == null)
            {
                throw new InvalidDataException(
                    "Native save fingerprint could not resolve LocalSave.");
            }
            return handler;
        }

        private static string GetNativeSavePath(
            object handler,
            int archiveIndex)
        {
            MethodInfo? method = handler.GetType().GetMethod(
                "GetDataFullPath",
                AllMembers,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);
            string? path = method?.Invoke(
                handler,
                new object[] { archiveIndex }) as string;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidDataException(
                    "Native save fingerprint could not resolve the current archive path.");
            }
            return path!;
        }

        private static int GetNativeBackupCount(object handler)
        {
            object? value = ReadMember(handler, "backupCount");
            if (value is int count && count > 0)
                return count;
            throw new InvalidDataException(
                "Native save fingerprint could not resolve LocalSave.backupCount.");
        }

        internal static object? ReadMember(
            object? instance,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.Read(
                instance,
                name);

        private static object? ReadStaticMember(
            Type type,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.ReadStatic(
                type,
                name);

        private static bool ReadBool(
            object? instance,
            string name,
            bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        private static int ReadInt(
            object? instance,
            string name)
        {
            object? value = ReadMember(instance, name);
            return value == null
                ? 0
                : Convert.ToInt32(value);
        }

        private static long ReadLong(
            object? instance,
            string name,
            long fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null
                ? fallback
                : Convert.ToInt64(value);
        }

        private static string ReadString(
            object? instance,
            string name) =>
            ReadMember(instance, name) as string ??
            string.Empty;

        private static string FirstText(
            string first,
            string second) =>
            !string.IsNullOrWhiteSpace(first)
                ? first
                : second ?? string.Empty;

    }
}
