using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.Internal;
using DTMAPI.RuntimeModules.Animals;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;

namespace DTMAPI.RuntimeModules.Content
{
    internal static class RuntimeContent
    {
        internal static IRuntimeContentSource Prepare(IRuntimeModuleOwnerContext owner) => new Activation(owner, Read(owner));
        private static Dictionary<string, object?> Read(IRuntimeModuleOwnerContext owner)
        {
            var input = owner.Files.ReadText("PackData/content.json");
            if (input.Status != OwnerDataStatus.Found || input.Value == null) throw new InvalidDataException("Runtime content: " + input.Status);
            var root = Object(StrictJson.Read(input.Value, maximumBytes: 1024 * 1024));
            if (!root.TryGetValue("schemaVersion", out var version) || !(version is double number) || number != 1 || root.Keys.Any(k => k != "schemaVersion" && k != "audio" && k != "animals" && k != "monsters"))
                throw new InvalidDataException("Runtime content requires schemaVersion 1 and only audio/animals/monsters sections.");
            return root;
        }
        private static Dictionary<string, object?> Object(object? value) => value as Dictionary<string, object?> ?? throw new InvalidDataException("Expected content object.");
        private static string Text(Dictionary<string, object?> value, string key) => value.TryGetValue(key, out var item) && item is string text ? text : throw new InvalidDataException("Expected content string: " + key);
        private sealed class Activation : IRuntimeContentSource
        {
            private readonly IRuntimeModuleOwnerContext owner;
            private Dictionary<string, object?> root;
            private readonly List<IDisposable> retired = new List<IDisposable>();
            private IAudioRegistration? audio;
            private IDisposable? voices;
            private IAnimalRegistration? definitions;
            private string definitionsKey = "";
            private string audioKey = "", voicesKey = "", monstersKey = "";
            private IAudioRegistration? voicesAudio, monstersAudio;
            private ContentRegistration? monsters;
            private bool closed;
            internal Activation(IRuntimeModuleOwnerContext owner, Dictionary<string, object?> root) { this.owner = owner; this.root = root; }
            public void Activate() => Apply(root);
            public void Refresh() { owner.Guard(); var candidate = Read(owner); Apply(candidate); root = candidate; }
            private void Apply(Dictionary<string, object?> candidate)
            {
                if (closed) throw new ObjectDisposedException("Runtime content source");
                ApplyDefinitions(candidate);
                try { ApplyAudio(candidate); ApplyVoices(candidate); ApplyMonsters(candidate); }
                finally { DrainRetired(); }
            }
            private void ApplyAudio(Dictionary<string, object?> candidate)
            {
                try
                {
                    candidate.TryGetValue("audio", out var section);
                    string key = section == null ? "" : Json.Write(section);
                    if (audioKey == key) return;
                    var declaration = section == null ? null : AudioContentReader.ParseSection(section);
                    var next = declaration == null ? null : (owner.GetApi<IAudioContentApi>(AudioModule.ProviderId) ?? throw new InvalidOperationException("Audio provider unavailable.")).Register(declaration);
                    if (audio != null) retired.Add(audio);
                    audio = next; audioKey = key;
                }
                catch (Exception ex) { Report("audio", ex); }
            }
            private void ApplyVoices(Dictionary<string, object?> candidate)
            {
                try
                {
                    object? values = null;
                    if (candidate.TryGetValue("animals", out var section)) Object(section).TryGetValue("voices", out values);
                    string key = values == null ? "" : Json.Write(values);
                    if (voicesKey == key && ReferenceEquals(voicesAudio, audio)) return;
                    IDisposable? next = null;
                    if (values != null && audio != null)
                    {
                        var bindings = (values as List<object?> ?? throw new InvalidDataException("Expected animals.voices array.")).Select(value =>
                        {
                            var row = Object(value); string stage = Text(row, "stage");
                            return new AnimalVoiceBinding { SpeciesId = Text(row, "speciesId"), Stage = stage == "child" ? AnimalStage.Child : stage == "adult" ? AnimalStage.Adult : throw new InvalidDataException("Animal stage"),
                                NativeEvent = Text(row, "nativeEvent"), Sound = audio.GetSound(Text(row, "sound")) };
                        }).ToArray();
                        if (bindings.Length != 0) next = (owner.GetApi<IAnimalContentApi>(AnimalModule.ProviderId) ?? throw new InvalidOperationException("Animals provider unavailable.")).RegisterVoices(bindings);
                    }
                    if (voices != null) retired.Add(voices);
                    voices = next; voicesKey = key; voicesAudio = audio;
                }
                catch (Exception ex) { Report("animals.voices", ex); }
            }
            private void ApplyMonsters(Dictionary<string, object?> candidate)
            {
                try
                {
                    candidate.TryGetValue("monsters", out var section);
                    string key = section == null ? "" : Json.Write(section);
                    if (monstersKey == key && ReferenceEquals(monstersAudio, audio)) return;
                    // Parsing/resources precede withdrawal. Registration uses the same
                    // scoped API transaction as advanced authors; failed leases remain retryable.
                    byte[] ReadResource(string path)
                    {
                        var bytes = owner.Resources.ReadBytes(path);
                        if (bytes.Status != OwnerDataStatus.Found || bytes.Value == null) throw new InvalidDataException(path + ": " + bytes.Status);
                        return bytes.Value;
                    }
                    var input = section == null ? null : ContentCodec.ReadSection(section, owner.Manifest.UniqueID, ReadResource);
                    if (input != null)
                    {
                        foreach (var row in input.Definitions.SelectMany(SoundValidation.CommonEvents).Concat(input.NativeOverrides.SelectMany(SoundValidation.CommonEvents)))
                        {
                            try { row.Value.Sound = audio?.GetSound(row.Value.ContentSoundId); }
                            catch (Exception ex) { Report("monsters.sounds", ex); }
                        }
                    }
                    monsters?.Dispose(); monsters = null;
                    if (input != null && input.Definitions.Count + input.NativeOverrides.Count + input.DropPools.Count + input.SpawnRules.Count != 0)
                    {
                        var api = owner.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Monster provider unavailable.");
                        monsters = ContentRegistration.Register(api, owner.Manifest.UniqueID, input);
                        if (!monsters.Accepted) throw new InvalidDataException(monsters.Diagnostic!.Code + ": " + monsters.Diagnostic.Message);
                    }
                    monstersKey = key; monstersAudio = audio;
                }
                catch (Exception ex) { Report("monsters", ex); }
            }
            private void ApplyDefinitions(Dictionary<string, object?> candidate)
            {
                try
                {
                    AnimalDefinitionBatch? batch = null;
                    if (candidate.TryGetValue("animals", out var section))
                    {
                        var animals = Object(section);
                        if (animals.Keys.Any(k => k != "definitions" && k != "voices")) throw new InvalidDataException("Unknown animals section field.");
                        if (animals.TryGetValue("definitions", out var values))
                        {
                            var rows = values as List<object?> ?? throw new InvalidDataException("Expected animals.definitions array.");
                            if (rows.Count != 0) batch = new AnimalDefinitionBatch { Definitions = rows.Select(ParseAnimal).ToArray() };
                        }
                    }
                    string key = batch == null ? "" : string.Join("|", batch.Definitions.Select(row => string.Join("|", new[] { row.SpeciesId, row.TemplateSpeciesId, row.AiTemplate, row.AnimatorMode, row.AdultAnimatorKey, row.ChildAnimatorKey, row.AnimatorBundle, row.AdultAnimatorAsset, row.ChildAnimatorAsset, row.FrameManifest, row.TemplateSpritePrefix, row.CustomSpritePrefix }.Select(s => s.Length + ":" + s))));
                    if (key == definitionsKey) return;
                    var next = batch == null ? null : ((IAnimalFileApi)(owner.GetApi<IAnimalContentApi>(AnimalModule.ProviderId) ?? throw new InvalidOperationException("Animals provider unavailable."))).Replace(definitions, batch);
                    if (definitions != null) retired.Add(definitions);
                    definitions = next; definitionsKey = key;
                }
                catch (Exception ex) { Report("animals.definitions", ex); }
            }
            private static AnimalDefinition ParseAnimal(object? value)
            {
                var row = Object(value);
                string Optional(string field) => row.ContainsKey(field) ? Text(row, field) : "";
                if (row.Keys.Any(k => !new[] { "speciesId", "templateSpeciesId", "aiTemplate", "animatorMode", "adultAnimatorKey", "childAnimatorKey", "animatorBundle", "adultAnimatorAsset", "childAnimatorAsset", "frameManifest", "templateSpritePrefix", "customSpritePrefix" }.Contains(k))) throw new InvalidDataException("Unknown animal definition field.");
                return new AnimalDefinition { SpeciesId = Text(row, "speciesId"), TemplateSpeciesId = Optional("templateSpeciesId"), AiTemplate = Optional("aiTemplate"), AnimatorMode = Optional("animatorMode"),
                    AdultAnimatorKey = Optional("adultAnimatorKey"), ChildAnimatorKey = Optional("childAnimatorKey"), AnimatorBundle = Optional("animatorBundle"), AdultAnimatorAsset = Optional("adultAnimatorAsset"), ChildAnimatorAsset = Optional("childAnimatorAsset"),
                    FrameManifest = Optional("frameManifest"), TemplateSpritePrefix = Optional("templateSpritePrefix"), CustomSpritePrefix = Optional("customSpritePrefix") };
            }
            private void Report(string section, Exception error) => owner.Report(section, error.GetBaseException().Message);
            private void DrainRetired(bool requireComplete = false)
            {
                while (retired.Count != 0)
                {
                    int last = retired.Count - 1;
                    try { retired[last].Dispose(); retired.RemoveAt(last); }
                    catch (Exception ex) { if (requireComplete) throw; Report("cleanup-pending", ex); return; }
                }
            }
            public void Dispose()
            {
                if (!closed)
                {
                    closed = true;
                    if (audio != null) retired.Add(audio);
                    if (voices != null) retired.Add(voices);
                    if (definitions != null) retired.Add(definitions);
                    if (monsters != null) retired.Add(monsters);
                    monsters = null;
                    definitions = null;
                    audio = null; voices = null;
                }
                DrainRetired(requireComplete: true);
            }
        }
    }
}
