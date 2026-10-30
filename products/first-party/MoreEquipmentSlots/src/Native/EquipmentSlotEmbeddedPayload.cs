using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum EquipmentSlotEmbeddedReadKind { Missing, Supported, Unsupported, Invalid }
    internal enum EquipmentSlotEmbeddedState { Ready, NeedsChoice, Unavailable }

    [DataContract]
    internal sealed class EquipmentSlotEmbeddedPayload
    {
        [DataMember(Name = "schema", IsRequired = true, Order = 1)]
        public int Schema { get; set; } = 1;
        [DataMember(Name = "slots", IsRequired = true, Order = 2)]
        public List<EquipmentSlotStorageEntry> Slots { get; set; } = EquipmentSlotStorageDocument.CreateEmptySlots();

        internal static bool Validate(IReadOnlyList<EquipmentSlotStorageEntry>? slots, out string error)
        {
            error = "Expected three complete ordered slots.";
            if (slots == null || slots.Count != 3) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < 3; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Index != i ||
                    !EquipmentSlotDocumentStore.TryValidateRawSlot(slot, false, out error)) return false;
                if (slot.IsOccupied && (slot.ItemState?.IsValid != true || !ids.Add(slot.ItemState.InstanceId)))
                { error = "Occupied legacy slot has missing or duplicate instance state."; return false; }
            }
            error = string.Empty;
            return true;
        }

        internal static string Encode(IReadOnlyList<EquipmentSlotStorageEntry> slots)
        {
            if (!Validate(slots, out string error)) throw new InvalidDataException(error);
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(EquipmentSlotEmbeddedPayload)).WriteObject(stream,
                    new EquipmentSlotEmbeddedPayload { Slots = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(slots) });
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        internal static EquipmentSlotEmbeddedReadKind Read(bool exists, object? raw,
            out List<EquipmentSlotStorageEntry>? slots, out string error)
        {
            slots = null; error = string.Empty;
            if (!exists) return EquipmentSlotEmbeddedReadKind.Missing;
            if (!(raw is string text)) { error = "Embedded value is not a string."; return EquipmentSlotEmbeddedReadKind.Invalid; }
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                EquipmentSlotEmbeddedPayload? payload;
                using (var stream = new MemoryStream(bytes))
                    payload = new DataContractJsonSerializer(typeof(EquipmentSlotEmbeddedPayload)).ReadObject(stream) as EquipmentSlotEmbeddedPayload;
                if (payload == null) throw new InvalidDataException("Embedded payload is null.");
                if (payload.Schema != 1) { error = "Unsupported embedded schema."; return EquipmentSlotEmbeddedReadKind.Unsupported; }
                EquipmentSlotSnapshotShape.Validate(bytes, embedded: true);
                if (!Validate(payload.Slots, out error)) return EquipmentSlotEmbeddedReadKind.Invalid;
                slots = payload.Slots;
                return EquipmentSlotEmbeddedReadKind.Supported;
            }
            catch (Exception ex) { error = ex.Message; return EquipmentSlotEmbeddedReadKind.Invalid; }
        }
    }
}
