using System;

namespace DTMAPI.MoreEquipmentSlots
{
    internal readonly struct MoreEquipmentSlotsUiLayout
    {
        internal MoreEquipmentSlotsUiLayout(
            bool valid,
            float contentWidth,
            float contentHeight,
            float firstSlotX,
            float slotY,
            string reason)
        {
            Valid = valid;
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
            FirstSlotX = firstSlotX;
            SlotY = slotY;
            Reason = reason ?? string.Empty;
        }

        internal bool Valid { get; }

        internal float ContentWidth { get; }

        internal float ContentHeight { get; }

        internal float FirstSlotX { get; }

        internal float SlotY { get; }

        internal string Reason { get; }
    }

    internal static class MoreEquipmentSlotsUiLayoutPolicy
    {
        internal const float SlotSize = 112f;
        internal const float SlotSpacing = 12f;
        internal const int MaximumOfficialPassiveSlots = 5;
        internal const float ProductContentWidth =
            SlotSize * 3f + SlotSpacing * 2f;
        private const float GeometryTolerance = 1f;

        internal static MoreEquipmentSlotsUiLayout Calculate(
            float officialSlotWidth,
            float officialSlotHeight,
            float observedSpacing,
            int officialPassiveCount)
        {
            if (officialPassiveCount < 1 ||
                officialPassiveCount >
                    MaximumOfficialPassiveSlots)
            {
                return Invalid(
                    "The native accessories bar must expose between one and five reviewed passive slots.");
            }
            if (Math.Abs(officialSlotWidth - SlotSize) >
                    GeometryTolerance ||
                Math.Abs(officialSlotHeight - SlotSize) >
                    GeometryTolerance)
            {
                return Invalid(
                    "The last native equipment slot is not the reviewed 112x112 geometry.");
            }
            if (Math.Abs(observedSpacing - SlotSpacing) >
                GeometryTolerance)
            {
                return Invalid(
                    "The native equipment row no longer exposes the reviewed 12-pixel spacing.");
            }

            return new MoreEquipmentSlotsUiLayout(
                valid: true,
                contentWidth: ProductContentWidth,
                contentHeight: SlotSize,
                firstSlotX: SlotSize / 2f,
                slotY: SlotSize / 2f,
                reason:
                    "Three full-size Product slots follow the last active native equipment slot.");
        }

        internal static float ProductSlotX(
            MoreEquipmentSlotsUiLayout layout,
            int index)
        {
            if (index < 0 || index >= 3)
                throw new ArgumentOutOfRangeException(nameof(index));
            return layout.FirstSlotX +
                index * (SlotSize + SlotSpacing);
        }

        private static MoreEquipmentSlotsUiLayout Invalid(
            string reason) =>
            new MoreEquipmentSlotsUiLayout(
                valid: false,
                contentWidth: 0f,
                contentHeight: 0f,
                firstSlotX: 0f,
                slotY: 0f,
                reason: reason);
    }
}
