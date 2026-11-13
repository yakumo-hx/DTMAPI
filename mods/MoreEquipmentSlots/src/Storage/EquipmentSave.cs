using System;
using System.Collections.Generic;

namespace DTMAPI.MoreEquipmentSlots
{
    // 额外槽的写入资格跟随这次读档。读不了的原生变量始终不写。
    internal sealed class EquipmentSave
    {
        internal EquipmentSave(bool exists, object? raw)
        {
            Kind = EquipmentPayload.Read(exists, raw, out var slots, out var error);
            Slots = slots;
            Error = error;
        }

        internal EquipmentPayloadReadKind Kind { get; private set; }
        internal IReadOnlyList<EquipmentSlotData>? Slots { get; }
        internal string Error { get; private set; }
        internal bool CanWrite => Kind == EquipmentPayloadReadKind.Ready;

        internal void Enable() => Kind = EquipmentPayloadReadKind.Ready;

        internal void Block(string error)
        {
            Kind = EquipmentPayloadReadKind.Unavailable;
            Error = error;
        }

        internal bool Write(IReadOnlyList<EquipmentSlotData> slots, Action<string> write)
        {
            if (!CanWrite) return false;
            write(EquipmentPayload.Encode(slots));
            return true;
        }
    }
}
