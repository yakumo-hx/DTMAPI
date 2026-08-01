using System;
using System.Collections;
using System.IO;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum EquipmentSlotNativeMutationResolution
    {
        None = 0,
        Backpack = 1,
        Mail = 2,
        Ambiguous = 3
    }

    internal sealed class
        EquipmentSlotNativeMutationOutcomeUnknownException :
        InvalidOperationException
    {
        internal EquipmentSlotNativeMutationOutcomeUnknownException(
            string itemId,
            int expectedCount,
            int beforeBackpackCount,
            int beforeMailCount,
            string message,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ItemId = itemId ?? string.Empty;
            ExpectedCount = Math.Max(1, expectedCount);
            BeforeBackpackCount =
                Math.Max(0, beforeBackpackCount);
            BeforeMailCount =
                Math.Max(0, beforeMailCount);
        }

        internal string ItemId { get; }

        internal int ExpectedCount { get; }

        internal int BeforeBackpackCount { get; }

        internal int BeforeMailCount { get; }
    }

    internal static class EquipmentSlotNativeMutationEvidence
    {
        internal static EquipmentSlotNativeMutationResolution
            Resolve(
                int beforeBackpackCount,
                int beforeMailCount,
                int afterBackpackCount,
                int afterMailCount,
                int expectedCount)
        {
            if (beforeBackpackCount < 0 ||
                beforeMailCount < 0 ||
                afterBackpackCount < 0 ||
                afterMailCount < 0 ||
                expectedCount <= 0)
            {
                return EquipmentSlotNativeMutationResolution
                    .Ambiguous;
            }

            int backpackDelta =
                afterBackpackCount - beforeBackpackCount;
            int mailDelta =
                afterMailCount - beforeMailCount;
            if (backpackDelta == 0 && mailDelta == 0)
            {
                return EquipmentSlotNativeMutationResolution
                    .None;
            }
            if (backpackDelta == expectedCount &&
                mailDelta == 0)
            {
                return EquipmentSlotNativeMutationResolution
                    .Backpack;
            }
            if (backpackDelta == 0 &&
                mailDelta == expectedCount)
            {
                return EquipmentSlotNativeMutationResolution
                    .Mail;
            }
            return EquipmentSlotNativeMutationResolution
                .Ambiguous;
        }

        internal static
            EquipmentSlotNativeMutationOutcomeUnknownException
            Unknown(
                string itemId,
                int expectedCount,
                int beforeBackpackCount,
                int beforeMailCount,
                string reason,
                Exception? innerException = null) =>
            new EquipmentSlotNativeMutationOutcomeUnknownException(
                itemId,
                expectedCount,
                beforeBackpackCount,
                beforeMailCount,
                "Native placement outcome is unknown for " +
                itemId +
                " x" +
                expectedCount +
                "; no retry or SaveGame is allowed until exact " +
                "backpack/mail evidence reconciles the attempt. " +
                (reason ?? string.Empty),
                innerException);
    }

    internal static class EquipmentSlotNativeMailEvidence
    {
        internal static int CountUnacceptedDtmapiItemMail(
            Type dolocApiType,
            string itemId)
        {
            if (dolocApiType == null)
                throw new ArgumentNullException(nameof(dolocApiType));
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException(
                    "A native item identity is required.",
                    nameof(itemId));

            object archive =
                Require(
                    ReadStaticMember(
                    dolocApiType,
                    "archiveHandle"),
                    "DolocAPI.archiveHandle");
            object farmData =
                Require(
                    ReadMember(archive, "farmData"),
                    "archiveHandle.farmData");
            object emailManager =
                Require(
                    ReadMember(farmData, "emailManager"),
                    "farmData.emailManager");
            IEnumerable emails =
                RequireEnumerable(
                    ReadMember(emailManager, "emails"),
                    "emailManager.emails");

            int count = 0;
            foreach (object? email in emails)
            {
                object readableEmail =
                    Require(email, "emailManager.emails entry");
                string emailId =
                    RequireString(
                        readableEmail,
                        "Id",
                        "email.Id");
                if (!string.Equals(
                    emailId,
                    "send_item_template",
                    StringComparison.Ordinal))
                {
                    continue;
                }
                IEnumerable attachments =
                    RequireEnumerable(
                        ReadMember(
                            readableEmail,
                            "emailAttaches"),
                        "send_item_template.emailAttaches");
                foreach (object? attachment in attachments)
                {
                    object readableAttachment =
                        Require(
                            attachment,
                            "send_item_template attachment");
                    if (!string.Equals(
                        readableAttachment.GetType().FullName,
                        "DolocTown.EmailAttachReward",
                        StringComparison.Ordinal))
                    {
                        continue;
                    }
                    bool isAccepted =
                        RequireBool(
                            readableAttachment,
                            "isAccept",
                            "EmailAttachReward.isAccept");
                    if (isAccepted)
                        continue;
                    object reward =
                        Require(
                            ReadMember(
                                readableAttachment,
                                "reward"),
                            "EmailAttachReward.reward");
                    if (!string.Equals(
                        reward.GetType().FullName,
                        "DolocTown.RewardItem",
                        StringComparison.Ordinal))
                    {
                        continue;
                    }
                    string rewardItemId =
                        RequireString(
                            reward,
                            "itemName",
                            "RewardItem.itemName");
                    int rewardCount =
                        RequireNonNegativeInt(
                            reward,
                            "itemCount",
                            "RewardItem.itemCount");
                    if (!string.Equals(
                        rewardItemId,
                        itemId,
                        StringComparison.Ordinal))
                    {
                        continue;
                    }
                    count = checked(count + rewardCount);
                }
            }

            return count;
        }

        private static object? ReadStaticMember(
            Type type,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.ReadStatic(
                type,
                name);

        private static object? ReadMember(
            object? target,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.Read(
                target,
                name);

        private static object Require(
            object? value,
            string authority)
        {
            if (value == null)
            {
                throw new InvalidDataException(
                    "Native mail authority is unreadable: " +
                    authority +
                    " is unavailable.");
            }
            return value;
        }

        private static IEnumerable RequireEnumerable(
            object? value,
            string authority)
        {
            if (value is IEnumerable enumerable)
                return enumerable;
            throw new InvalidDataException(
                "Native mail authority is unreadable: " +
                authority +
                " is not enumerable.");
        }

        private static string RequireString(
            object target,
            string name,
            string authority)
        {
            object? value = ReadMember(target, name);
            if (value is string text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
            throw new InvalidDataException(
                "Native mail authority is unreadable: " +
                authority +
                " is missing or is not text.");
        }

        private static int RequireNonNegativeInt(
            object target,
            string name,
            string authority)
        {
            object? value = ReadMember(target, name);
            if (value is int number && number >= 0)
                return number;
            throw new InvalidDataException(
                "Native mail authority is unreadable: " +
                authority +
                " is missing, negative, or is not Int32.");
        }

        private static bool RequireBool(
            object target,
            string name,
            string authority)
        {
            object? value = ReadMember(target, name);
            if (value is bool flag)
                return flag;
            throw new InvalidDataException(
                "Native mail authority is unreadable: " +
                authority +
                " is missing or is not Boolean.");
        }
    }
}
