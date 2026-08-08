using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum EquipmentSlotStorageFormat
    {
        Missing = 0,
        ProductV3 = 1,
        LegacyFlat = 2,
        UnsupportedProduct = 3,
        UnsupportedLegacy = 4,
        Ambiguous = 5,
        Invalid = 6,
        PreSchemaGlobal = 7
    }

    internal readonly struct EquipmentSlotStorageFormatProbe
    {
        internal EquipmentSlotStorageFormatProbe(
            EquipmentSlotStorageFormat format,
            int schemaVersion,
            string failure)
        {
            Format = format;
            SchemaVersion = schemaVersion;
            Failure = failure ?? string.Empty;
        }

        internal EquipmentSlotStorageFormat Format { get; }

        internal int SchemaVersion { get; }

        internal string Failure { get; }
    }

    internal static class EquipmentSlotLegacyMigrationSourceKinds
    {
        internal const string ScopedFlat = "scoped-flat";
        internal const string GlobalFlat = "global-flat";
        internal const string PreSchemaGlobal =
            "global-pre-schema";
    }

    internal static class EquipmentSlotStorageFormatClassifier
    {
        private const int OldestSupportedSchema = 1;
        private const int NewestSupportedSchema = 3;

        internal static EquipmentSlotStorageFormatProbe Probe(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                return new EquipmentSlotStorageFormatProbe(
                    EquipmentSlotStorageFormat.Missing,
                    0,
                    "file-missing");
            }

            try
            {
                StorageShapeProbe? probe;
                using (FileStream stream = File.OpenRead(path))
                {
                    probe =
                        CreateSerializer(typeof(StorageShapeProbe))
                            .ReadObject(stream) as StorageShapeProbe;
                }
                if (probe == null)
                {
                    return new EquipmentSlotStorageFormatProbe(
                        EquipmentSlotStorageFormat.Invalid,
                        0,
                        "document-null");
                }

                int schema = probe.SchemaVersion ?? 0;
                bool hasProductShape = probe.Scope != null;
                bool hasLegacyShape =
                    probe.ArchiveIndex.HasValue ||
                    probe.OwnerId != null ||
                    probe.StorageScope != null;
                if (hasProductShape && hasLegacyShape)
                {
                    return new EquipmentSlotStorageFormatProbe(
                        EquipmentSlotStorageFormat.Ambiguous,
                        schema,
                        "product-and-legacy-shapes-coexist");
                }
                if (hasProductShape)
                {
                    return new EquipmentSlotStorageFormatProbe(
                        schema ==
                            MoreEquipmentSlotsProductContract
                                .StorageSchemaVersion
                            ? EquipmentSlotStorageFormat.ProductV3
                            : EquipmentSlotStorageFormat
                                .UnsupportedProduct,
                        schema,
                        schema ==
                            MoreEquipmentSlotsProductContract
                                .StorageSchemaVersion
                            ? string.Empty
                            : "unsupported-product-schema");
                }
                if (!probe.SchemaVersion.HasValue &&
                    IsExactPreSchemaGlobal(probe, path))
                {
                    return new EquipmentSlotStorageFormatProbe(
                        EquipmentSlotStorageFormat.PreSchemaGlobal,
                        0,
                        string.Empty);
                }
                if (hasLegacyShape)
                {
                    bool supported =
                        probe.SchemaVersion.HasValue &&
                        schema >= OldestSupportedSchema &&
                        schema <= NewestSupportedSchema;
                    return new EquipmentSlotStorageFormatProbe(
                        supported
                            ? EquipmentSlotStorageFormat.LegacyFlat
                            : EquipmentSlotStorageFormat
                                .UnsupportedLegacy,
                        schema,
                        supported
                            ? string.Empty
                            : "unsupported-legacy-schema");
                }

                return new EquipmentSlotStorageFormatProbe(
                    EquipmentSlotStorageFormat.Invalid,
                    schema,
                    "storage-shape-unrecognized");
            }
            catch (Exception ex)
            {
                return new EquipmentSlotStorageFormatProbe(
                    EquipmentSlotStorageFormat.Invalid,
                    0,
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsExactPreSchemaGlobal(
            StorageShapeProbe probe,
            string path)
        {
            if (!string.Equals(
                    probe.OwnerId?.Trim(),
                    MoreEquipmentSlotsProductContract.UniqueId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(probe.SavedAt) ||
                !DateTimeOffset.TryParse(
                    probe.SavedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _) ||
                probe.Slots == null ||
                probe.StorageScope != null ||
                probe.ArchiveIndex.HasValue ||
                probe.PlayerName != null ||
                probe.CustomPlayerName != null ||
                probe.CurrentScene != null ||
                probe.SavedTotalGameSeconds.HasValue ||
                probe.Generation.HasValue ||
                probe.Journal != null ||
                probe.GameplayCandidate != null ||
                probe.LegacyMigration != null ||
                !HasExactPreSchemaJsonShape(path))
            {
                return false;
            }

            var indexes = new HashSet<int>();
            foreach (PreSchemaSlotShape slot in probe.Slots)
            {
                if (slot == null ||
                    slot.Index < 0 ||
                    !indexes.Add(slot.Index) ||
                    slot.SlotId == null ||
                    slot.ItemId == null ||
                    slot.DisplayName == null ||
                    slot.SkillId == null ||
                    slot.LastMessage == null ||
                    slot.DefenseBonus.HasValue ||
                    slot.IsShieldHat.HasValue ||
                    slot.ShieldValue.HasValue ||
                    slot.ShieldMaxValue.HasValue ||
                    slot.ShieldDefend.HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasExactPreSchemaJsonShape(
            string path)
        {
            var top = new HashSet<string>(
                StringComparer.Ordinal);
            HashSet<string>? slot = null;
            using (XmlDictionaryReader reader =
                JsonReaderWriterFactory.CreateJsonReader(
                    File.ReadAllBytes(path),
                    XmlDictionaryReaderQuotas.Max))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        string type =
                            reader.GetAttribute("type") ??
                            string.Empty;
                        if (reader.Depth == 0)
                        {
                            if (reader.LocalName != "root" ||
                                type != "object")
                            {
                                return false;
                            }
                        }
                        else if (reader.Depth == 1)
                        {
                            string name = reader.LocalName;
                            if ((name != "ownerId" &&
                                 name != "savedAt" &&
                                 name != "slots") ||
                                !top.Add(name) ||
                                (name == "slots"
                                    ? type != "array"
                                    : type != "string"))
                            {
                                return false;
                            }
                        }
                        else if (reader.Depth == 2)
                        {
                            if (reader.LocalName != "item" ||
                                type != "object" ||
                                slot != null)
                            {
                                return false;
                            }
                            slot =
                                new HashSet<string>(
                                    StringComparer.Ordinal);
                        }
                        else if (reader.Depth == 3)
                        {
                            if (slot == null ||
                                !IsExactPreSchemaSlotProperty(
                                    reader.LocalName,
                                    type) ||
                                !slot.Add(reader.LocalName))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (reader.NodeType ==
                                 XmlNodeType.EndElement &&
                             reader.Depth == 2)
                    {
                        if (slot == null ||
                            slot.Count != 6)
                        {
                            return false;
                        }
                        slot = null;
                    }
                }
            }
            return slot == null &&
                top.SetEquals(
                    new[]
                    {
                        "ownerId",
                        "savedAt",
                        "slots"
                    });
        }

        private static bool IsExactPreSchemaSlotProperty(
            string name,
            string type) =>
            (name == "index"
                ? type == "number"
                : type == "string") &&
            (name == "index" ||
             name == "slotId" ||
             name == "itemId" ||
             name == "displayName" ||
             name == "skillId" ||
             name == "lastMessage");

        private static DataContractJsonSerializer CreateSerializer(
            Type type) =>
            new DataContractJsonSerializer(type);

        [DataContract]
        private sealed class StorageShapeProbe
        {
            [DataMember(
                Name = "schemaVersion",
                EmitDefaultValue = false)]
            public int? SchemaVersion { get; set; }

            [DataMember(Name = "scope", EmitDefaultValue = false)]
            public ProductScopeShape? Scope { get; set; }

            [DataMember(
                Name = "ownerId",
                EmitDefaultValue = false)]
            public string? OwnerId { get; set; }

            [DataMember(
                Name = "savedAt",
                EmitDefaultValue = false)]
            public string? SavedAt { get; set; }

            [DataMember(
                Name = "slots",
                EmitDefaultValue = false)]
            public List<PreSchemaSlotShape>? Slots { get; set; }

            [DataMember(
                Name = "storageScope",
                EmitDefaultValue = false)]
            public string? StorageScope { get; set; }

            [DataMember(
                Name = "archiveIndex",
                EmitDefaultValue = false)]
            public int? ArchiveIndex { get; set; }

            [DataMember(
                Name = "playerName",
                EmitDefaultValue = false)]
            public string? PlayerName { get; set; }

            [DataMember(
                Name = "customPlayerName",
                EmitDefaultValue = false)]
            public string? CustomPlayerName { get; set; }

            [DataMember(
                Name = "currentScene",
                EmitDefaultValue = false)]
            public string? CurrentScene { get; set; }

            [DataMember(
                Name = "savedTotalGameSeconds",
                EmitDefaultValue = false)]
            public long? SavedTotalGameSeconds { get; set; }

            [DataMember(
                Name = "generation",
                EmitDefaultValue = false)]
            public long? Generation { get; set; }

            [DataMember(
                Name = "journal",
                EmitDefaultValue = false)]
            public ShapeMarker? Journal { get; set; }

            [DataMember(
                Name = "gameplayCandidate",
                EmitDefaultValue = false)]
            public ShapeMarker? GameplayCandidate { get; set; }

            [DataMember(
                Name = "legacyMigration",
                EmitDefaultValue = false)]
            public ShapeMarker? LegacyMigration { get; set; }
        }

        [DataContract]
        private sealed class ShapeMarker
        {
        }

        [DataContract]
        private sealed class ProductScopeShape
        {
            [DataMember(Name = "archiveIndex")]
            public int ArchiveIndex { get; set; }

            [DataMember(Name = "playerName")]
            public string PlayerName { get; set; } =
                string.Empty;

            [DataMember(Name = "customPlayerName")]
            public string CustomPlayerName { get; set; } =
                string.Empty;

            [DataMember(
                Name = "totalGameSeconds",
                EmitDefaultValue = false)]
            public long? TotalGameSeconds { get; set; }
        }

        [DataContract]
        private sealed class PreSchemaSlotShape
        {
            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(
                Name = "slotId",
                EmitDefaultValue = false)]
            public string? SlotId { get; set; }

            [DataMember(
                Name = "itemId",
                EmitDefaultValue = false)]
            public string? ItemId { get; set; }

            [DataMember(
                Name = "displayName",
                EmitDefaultValue = false)]
            public string? DisplayName { get; set; }

            [DataMember(
                Name = "skillId",
                EmitDefaultValue = false)]
            public string? SkillId { get; set; }

            [DataMember(
                Name = "lastMessage",
                EmitDefaultValue = false)]
            public string? LastMessage { get; set; }

            [DataMember(
                Name = "defenseBonus",
                EmitDefaultValue = false)]
            public int? DefenseBonus { get; set; }

            [DataMember(
                Name = "isShieldHat",
                EmitDefaultValue = false)]
            public bool? IsShieldHat { get; set; }

            [DataMember(
                Name = "shieldValue",
                EmitDefaultValue = false)]
            public int? ShieldValue { get; set; }

            [DataMember(
                Name = "shieldMaxValue",
                EmitDefaultValue = false)]
            public int? ShieldMaxValue { get; set; }

            [DataMember(
                Name = "shieldDefend",
                EmitDefaultValue = false)]
            public int? ShieldDefend { get; set; }
        }
    }
}
