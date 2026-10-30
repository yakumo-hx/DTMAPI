using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;

namespace DTMAPI.MoreEquipmentSlots
{
    // Check presence before the legacy DTO serializer can supply zero/false for
    // missing fields. This reads the captured bytes only; it never normalizes them.
    internal static class EquipmentSlotSnapshotShape
    {
        internal static void Validate(byte[] bytes, bool embedded)
        {
            XElement root;
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
                root = XElement.Load(reader);
            Fields(root, embedded ? "schema,slots" : "schemaVersion,scope,generation,slots",
                embedded ? "" : "journal,gameplayCandidate,legacyMigration");
            if (!embedded)
                Fields(root.Element("scope")!, "archiveIndex,playerName,customPlayerName,totalGameSeconds", "");
            var slots = root.Element("slots")!;
            if ((string?)slots.Attribute("type") != "array") throw new InvalidDataException("slots 必须为完整三槽数组。");
            foreach (var slot in slots.Elements())
            {
                Fields(slot, "index,itemId,displayName,skillId,defenseBonus,isShield,shieldValue,shieldMaxValue,shieldDefend", "itemState");
                var state = slot.Element("itemState");
                if (state == null || (string?)state.Attribute("type") == "null") continue;
                Fields(state, "instanceId,nativeType", "filled,cooldown");
            }
        }

        private static void Fields(XElement value, string required, string optional)
        {
            if ((string?)value.Attribute("type") != "object") throw new InvalidDataException(value.Name + " 必须为对象。");
            var missing = new HashSet<string>(required.Split(','), StringComparer.Ordinal);
            var allowed = new HashSet<string>(missing, StringComparer.Ordinal);
            foreach (string name in optional.Split(',')) if (name.Length > 0) allowed.Add(name);
            foreach (var field in value.Elements())
            {
                string name = field.Name.LocalName;
                if (!allowed.Remove(name)) throw new InvalidDataException("未知或重复字段：" + value.Name + "." + name);
                missing.Remove(name);
                if (name == "itemState" || name == "journal" || name == "gameplayCandidate" || name == "legacyMigration" || name == "scope" || name == "slots") continue;
                string type = name == "isShield" || name == "isShieldHat" || name == "filled" ? "boolean" :
                    name == "itemId" || name == "displayName" || name == "skillId" || name == "instanceId" || name == "nativeType" || name == "playerName" || name == "customPlayerName" || name == "ownerId" || name == "storageScope" || name == "savedAt" || name == "slotId" || name == "lastMessage" ? "string" : "number";
                if ((string?)field.Attribute("type") != type) throw new InvalidDataException("字段类型无效：" + value.Name + "." + name);
            }
            if (missing.Count > 0) throw new InvalidDataException("缺少完整快照字段：" + value.Name + "." + string.Join(",", missing));
        }

        internal static void ValidateLegacy(byte[] bytes, bool preSchema)
        {
            XElement root;
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
                root = XElement.Load(reader);
            Fields(root, preSchema ? "ownerId,savedAt,slots" : "schemaVersion,ownerId,slots",
                preSchema ? "" : "storageScope,archiveIndex,playerName,customPlayerName,generation,savedTotalGameSeconds,journal,gameplayCandidate");
            var slots = root.Element("slots")!;
            if ((string?)slots.Attribute("type") != "array") throw new InvalidDataException("旧 slots 必须为数组。");
            foreach (var slot in slots.Elements())
                Fields(slot, preSchema ? "index,slotId,itemId,displayName,skillId,lastMessage" : "index,itemId",
                    preSchema ? "" : "displayName,skillId,defenseBonus,isShieldHat,shieldValue,shieldMaxValue,shieldDefend");
        }
    }
}
