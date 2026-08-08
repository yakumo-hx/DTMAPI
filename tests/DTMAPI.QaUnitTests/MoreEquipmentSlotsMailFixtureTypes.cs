using System;
using System.Collections;

namespace DTMAPI.QaUnitTests
{
    internal sealed class MoreEquipmentSlotsMailArchiveFixture
    {
        internal MoreEquipmentSlotsMailArchiveFixture(object? farmData)
        {
            this.farmData = farmData;
        }

        internal readonly object? farmData;
    }

    internal sealed class MoreEquipmentSlotsMailFarmDataFixture
    {
        internal MoreEquipmentSlotsMailFarmDataFixture(
            object? emailManager)
        {
            this.emailManager = emailManager;
        }

        internal readonly object? emailManager;
    }

    internal sealed class MoreEquipmentSlotsMailManagerFixture
    {
        internal MoreEquipmentSlotsMailManagerFixture(object? emails)
        {
            this.emails = emails;
        }

        internal readonly object? emails;
    }

    internal sealed class MoreEquipmentSlotsMailEntryFixture
    {
        internal MoreEquipmentSlotsMailEntryFixture(
            object? id,
            object? attachments)
        {
            Id = id;
            emailAttaches = attachments;
        }

        internal readonly object? Id;

        internal readonly object? emailAttaches;
    }

    internal sealed class MoreEquipmentSlotsThrowingEnumerable :
        IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            throw new InvalidOperationException(
                "Synthetic mail enumeration failure.");
        }
    }
}

namespace DolocTown
{
    internal sealed class EmailAttachReward
    {
        internal EmailAttachReward(
            object? isAccept,
            object? reward)
        {
            this.isAccept = isAccept;
            this.reward = reward;
        }

        internal readonly object? isAccept;

        internal readonly object? reward;
    }

    internal sealed class RewardItem
    {
        internal RewardItem(
            object? itemName,
            object? itemCount)
        {
            this.itemName = itemName;
            this.itemCount = itemCount;
        }

        internal readonly object? itemName;

        internal readonly object? itemCount;
    }
}
