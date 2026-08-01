using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{
    internal sealed class ShadowContentRegistry
    {
        private static readonly string[] OfficialJsonFiles =
        {
            "animal_tbanimal.json",
            "animal_tbanimaldocument.json",
            "item_tbitem.json",
            "item_tbitemspawn.json",
            "mod_tbmodstoreextension.json",
            "husbandry_tbhusbandry.json",
            "husbandry_tbhusbandryenergy.json"
        };

        public ShadowContentRegistrySnapshot Build(IReadOnlyList<DiscoveredMod> discoveredMods, IReadOnlyList<DiscoveredMod> loadedMods, string reason)
        {
            var loadedIds = new HashSet<string>(loadedMods.Select(m => m.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            var rows = new List<ShadowContentRegistryRow>();
            var diagnostics = new List<string>();

            foreach (DiscoveredMod mod in discoveredMods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                ShadowJsonSummary customAnimals = ReadDefinitions<ShadowCustomAnimalDefinition>(
                    mod.RootPath,
                    Path.Combine("Content", "DTMAPI", "custom-animals.json"),
                    d => (d.SpeciesId ?? string.Empty).Trim() + ":" + (d.AnimatorMode ?? string.Empty).Trim());
                ShadowJsonSummary audioReplacements = ReadDefinitions<ShadowAudioReplacementDefinition>(
                    mod.RootPath,
                    Path.Combine("Content", "DTMAPI", "audio-replacements.json"),
                    d => (d.Id ?? string.Empty).Trim() + ":" + (d.Category ?? string.Empty).Trim() + ":" + (d.NativeSoundEvent ?? string.Empty).Trim());
                OfficialJsonSummary officialJson = ReadOfficialJsonSummary(mod.RootPath);

                var row = new ShadowContentRegistryRow(
                    mod.Manifest.UniqueID,
                    mod.Manifest.Name,
                    mod.Source,
                    mod.OfficialEnabled,
                    loadedIds.Contains(mod.Manifest.UniqueID),
                    mod.Manifest.Type,
                    mod.Classification.IdentityName,
                    mod.Manifest.EntryDll,
                    mod.RootPath,
                    mod.ManifestPath,
                    customAnimals,
                    audioReplacements,
                    officialJson);
                rows.Add(row);

                AddDiagnostics(diagnostics, row);
            }

            return new ShadowContentRegistrySnapshot(reason ?? string.Empty, DateTimeOffset.Now, rows, diagnostics);
        }

        private static void AddDiagnostics(List<string> diagnostics, ShadowContentRegistryRow row)
        {
            if (row.CustomAnimals.Exists && !row.CustomAnimals.ParseStatus.Equals("ok", StringComparison.OrdinalIgnoreCase))
                diagnostics.Add(row.UniqueID + " custom-animals parseStatus=" + row.CustomAnimals.ParseStatus + " " + row.CustomAnimals.Message);
            if (row.AudioReplacements.Exists && !row.AudioReplacements.ParseStatus.Equals("ok", StringComparison.OrdinalIgnoreCase))
                diagnostics.Add(row.UniqueID + " audio-replacements parseStatus=" + row.AudioReplacements.ParseStatus + " " + row.AudioReplacements.Message);
            if (row.OfficialJson.ExistsCount > 0 && row.OfficialJson.InvalidCount > 0)
                diagnostics.Add(row.UniqueID + " official-json invalid=" + row.OfficialJson.InvalidCount + " " + row.OfficialJson.InvalidFiles);
            if (row.ManagedIdentity.Equals("ContentPack", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(row.EntryDll))
                diagnostics.Add(row.UniqueID + " non-code content declares EntryDll=" + row.EntryDll);
        }

        private static ShadowJsonSummary ReadDefinitions<T>(string rootPath, string relativePath, Func<T, string> summarize)
        {
            string path = Path.Combine(rootPath, relativePath);
            if (!File.Exists(path))
                return ShadowJsonSummary.Missing(relativePath);

            try
            {
                T[] definitions;
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T[]));
                    definitions = (T[])(serializer.ReadObject(stream) ?? new T[0]);
                }

                string[] summaries = definitions
                    .Where(d => d != null)
                    .Select(summarize)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(12)
                    .ToArray();
                return ShadowJsonSummary.Present(relativePath, definitions.Length, "ok", string.Join("|", summaries));
            }
            catch (Exception ex)
            {
                return ShadowJsonSummary.Present(relativePath, 0, "parse-error", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static OfficialJsonSummary ReadOfficialJsonSummary(string rootPath)
        {
            string contentRoot = Path.Combine(rootPath, "Content");
            int exists = 0;
            int jsonShaped = 0;
            List<string> invalid = new List<string>();
            foreach (string file in OfficialJsonFiles)
            {
                string path = Path.Combine(contentRoot, file);
                if (!File.Exists(path))
                    continue;

                exists++;
                try
                {
                    string text = File.ReadAllText(path).TrimStart();
                    if (text.StartsWith("{", StringComparison.Ordinal) || text.StartsWith("[", StringComparison.Ordinal))
                        jsonShaped++;
                    else
                        invalid.Add(file);
                }
                catch (Exception ex)
                {
                    invalid.Add(file + ":" + ex.GetType().Name);
                }
            }

            return new OfficialJsonSummary(exists, jsonShaped, invalid.Count, string.Join("|", invalid.Take(8).ToArray()));
        }

        [DataContract]
        private sealed class ShadowCustomAnimalDefinition
        {
            [DataMember(Name = "speciesId")] public string SpeciesId { get; set; } = string.Empty;
            [DataMember(Name = "animatorMode")] public string AnimatorMode { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class ShadowAudioReplacementDefinition
        {
            [DataMember(Name = "id")] public string Id { get; set; } = string.Empty;
            [DataMember(Name = "category")] public string Category { get; set; } = string.Empty;
            [DataMember(Name = "nativeSoundEvent")] public string NativeSoundEvent { get; set; } = string.Empty;
        }
    }

    internal sealed class ShadowContentRegistrySnapshot
    {
        public ShadowContentRegistrySnapshot(string reason, DateTimeOffset capturedAt, IReadOnlyList<ShadowContentRegistryRow> rows, IReadOnlyList<string> diagnostics)
        {
            Reason = reason;
            CapturedAt = capturedAt;
            Rows = rows;
            Diagnostics = diagnostics;
        }

        public string Reason { get; }
        public DateTimeOffset CapturedAt { get; }
        public IReadOnlyList<ShadowContentRegistryRow> Rows { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public int DiffCount => Diagnostics.Count;

        public string FormatSummary()
        {
            int customAnimalPacks = Rows.Count(r => r.CustomAnimals.Exists);
            int customAnimals = Rows.Sum(r => r.CustomAnimals.Count);
            int audioPacks = Rows.Count(r => r.AudioReplacements.Exists);
            int audioReplacements = Rows.Sum(r => r.AudioReplacements.Count);
            int officialJsonSources = Rows.Count(r => r.OfficialJson.ExistsCount > 0);
            string firstDiagnostics = Diagnostics.Count == 0 ? "none" : string.Join(" || ", Diagnostics.Take(5).ToArray());
            return "reason=" + Reason +
                "; rows=" + Rows.Count +
                "; loadedRows=" + Rows.Count(r => r.LoadedByOldSystem) +
                "; customAnimalPacks=" + customAnimalPacks +
                "; customAnimals=" + customAnimals +
                "; audioPacks=" + audioPacks +
                "; audioReplacements=" + audioReplacements +
                "; officialJsonSources=" + officialJsonSources +
                "; diffs=" + DiffCount +
                "; firstDiffs=" + firstDiagnostics;
        }
    }

    internal sealed class ShadowContentRegistryRow
    {
        public ShadowContentRegistryRow(
            string uniqueId,
            string name,
            string source,
            bool enabledByOldSystem,
            bool loadedByOldSystem,
            string type,
            string managedIdentity,
            string entryDll,
            string rootPath,
            string manifestPath,
            ShadowJsonSummary customAnimals,
            ShadowJsonSummary audioReplacements,
            OfficialJsonSummary officialJson)
        {
            UniqueID = uniqueId ?? string.Empty;
            Name = name ?? string.Empty;
            Source = source ?? string.Empty;
            EnabledByOldSystem = enabledByOldSystem;
            LoadedByOldSystem = loadedByOldSystem;
            Type = type ?? string.Empty;
            ManagedIdentity = managedIdentity ?? string.Empty;
            EntryDll = entryDll ?? string.Empty;
            RootPath = rootPath ?? string.Empty;
            ManifestPath = manifestPath ?? string.Empty;
            CustomAnimals = customAnimals;
            AudioReplacements = audioReplacements;
            OfficialJson = officialJson;
        }

        public string UniqueID { get; }
        public string Name { get; }
        public string Source { get; }
        public bool EnabledByOldSystem { get; }
        public bool LoadedByOldSystem { get; }
        public string Type { get; }
        public string ManagedIdentity { get; }
        public string EntryDll { get; }
        public string RootPath { get; }
        public string ManifestPath { get; }
        public ShadowJsonSummary CustomAnimals { get; }
        public ShadowJsonSummary AudioReplacements { get; }
        public OfficialJsonSummary OfficialJson { get; }
    }

    internal sealed class ShadowJsonSummary
    {
        private ShadowJsonSummary(string relativePath, bool exists, int count, string parseStatus, string message)
        {
            RelativePath = relativePath ?? string.Empty;
            Exists = exists;
            Count = count;
            ParseStatus = parseStatus ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string RelativePath { get; }
        public bool Exists { get; }
        public int Count { get; }
        public string ParseStatus { get; }
        public string Message { get; }

        public static ShadowJsonSummary Missing(string relativePath) => new ShadowJsonSummary(relativePath, false, 0, "missing", string.Empty);

        public static ShadowJsonSummary Present(string relativePath, int count, string parseStatus, string message) => new ShadowJsonSummary(relativePath, true, count, parseStatus, message);
    }

    internal sealed class OfficialJsonSummary
    {
        public OfficialJsonSummary(int existsCount, int jsonShapedCount, int invalidCount, string invalidFiles)
        {
            ExistsCount = existsCount;
            JsonShapedCount = jsonShapedCount;
            InvalidCount = invalidCount;
            InvalidFiles = invalidFiles ?? string.Empty;
        }

        public int ExistsCount { get; }
        public int JsonShapedCount { get; }
        public int InvalidCount { get; }
        public string InvalidFiles { get; }
    }
}
