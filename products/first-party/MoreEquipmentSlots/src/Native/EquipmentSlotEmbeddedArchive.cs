using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.MoreEquipmentSlots
{
    // Shared only with the existing MES Compatibility reader. No platform API added.
    internal static class EquipmentSlotEmbeddedArchive
    {
        internal const string Key = "$DTMAPI__MoreEquipmentSlots_v1";
        private static Type Api => EquipmentSlotNativeItem.ResolveApiType();
        private static object ReadRequired(object? value, string name) =>
            EquipmentSlotNativeItem.Read(value, name) ?? throw new InvalidDataException("Missing native archive member: " + name);
        private static object Handle => MoreEquipmentSlotsReflectionAccess.ReadStatic(Api, "archiveHandle") ??
            throw new InvalidOperationException("No active archive.");
        internal static object CurrentArchive => ReadRequired(Handle, "cityData");
        private static object Storage => ReadRequired(ReadRequired(CurrentArchive, "dialogueManager"), "variableStorage");
        private static object Handler => ReadRequired(MoreEquipmentSlotsReflectionAccess.ReadStatic(Api, "dataPersistenceManager"), "fileDataHandler");
        internal static string TargetPath(int slot) => Path.GetFullPath((string)(EquipmentSlotNativeItem.Call(Handler, "GetDataFullPath", slot) ??
            throw new InvalidDataException("Native save target unavailable.")));

        internal static bool ReadCurrent(out object? value)
        {
            var variables = ReadRequired(Storage, "variables") as IDictionary ?? throw new InvalidDataException("Native variables unreadable.");
            bool exists = variables.Contains(Key); value = exists ? variables[Key] : null; return exists;
        }
        internal static bool HasCurrentKey() => ReadCurrent(out _);
        internal static void Stage(string payload) => EquipmentSlotNativeItem.Call(Storage, "SetValue", Key, payload);

        internal static bool ReadDisk(string path, out object? value)
        {
            value = null;
            if (!File.Exists(path)) return false;
            string json = EquipmentSlotNativeItem.Call(Handler, "TryDecryptData", File.ReadAllText(path)) as string ??
                throw new InvalidDataException("Native archive cannot be decoded.");
            ArchiveEnvelope? archive;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                archive = new DataContractJsonSerializer(typeof(ArchiveEnvelope),
                    new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).ReadObject(stream) as ArchiveEnvelope;
            var variables = archive?.City?.Dialogue?.Storage?.Variables ?? throw new InvalidDataException("Native archive variable container missing.");
            return variables.TryGetValue(Key, out value);
        }
        [DataContract] private sealed class ArchiveEnvelope
        { [DataMember(Name = "cityData")] public CityEnvelope? City; }
        [DataContract] private sealed class CityEnvelope
        { [DataMember(Name = "dialogueManager")] public DialogueEnvelope? Dialogue; }
        [DataContract] private sealed class DialogueEnvelope
        { [DataMember(Name = "variableStorage")] public StorageEnvelope? Storage; }
        [DataContract] private sealed class StorageEnvelope
        { [DataMember(Name = "variables")] public Dictionary<string, object>? Variables; }
    }
}
