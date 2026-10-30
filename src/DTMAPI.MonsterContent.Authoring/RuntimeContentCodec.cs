using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTMAPI.MonsterContent.Authoring
{
    public static partial class ContentCodec
    {
        /// <summary>Writes the shared Runtime entry. Dependencies belong to the package manifest.</summary>
        public static string WriteContent(ContentInput input, MonsterContentCatalog? catalog = null)
        {
            var resources = new SortedDictionary<string, object>(StringComparer.Ordinal);
            var events = new SortedDictionary<string, object>(StringComparer.Ordinal);
            void Sounds(IEnumerable<KeyValuePair<string, MonsterSound>> rows, string templateId)
            {
                foreach (var row in rows)
                {
                    var sound = row.Value;
                    string use = SoundValidation.CommonUseKey(row.Key);
                    string route = use == "dead" ? "NativeMonsterDeath" : use.StartsWith("explosion.", StringComparison.Ordinal) ? "NativeMonsterExplosion" : "NativeMonster";
                    bool loop = sound.Loop ?? (use == "humming" ? catalog?.Templates.FirstOrDefault(t => t.Id == templateId)?.HummingLoop ?? false : false);
                    if (sound.ResourcePath.Length == 0) throw new AuthoringException("ResourceMissing", row.Key, "Offline output requires an audio resource, not a live handle.");
                    string resource = "m_" + Hash(Encoding.UTF8.GetBytes(sound.ResourcePath));
                    string key = resource + "_" + route + (loop ? "_loop" : "");
                    resources[resource] = Json.Map("key", resource, "path", sound.ResourcePath);
                    events[key] = Json.Map("key", key, "resource", resource, "use", "effect", "route", route, "loop", loop);
                    sound.ContentSoundId = key;
                }
            }
            foreach (var definition in input.Definitions) Sounds(SoundValidation.CommonEvents(definition), definition.TemplateId);
            foreach (var value in input.NativeOverrides) Sounds(SoundValidation.CommonEvents(value), value.TargetId);
            var root = Json.Map("schemaVersion", 1);
            if (events.Count != 0) root["audio"] = Json.Map("resources", resources.Values.ToArray(), "events", events.Values.ToArray());
            root["monsters"] = SectionData(input);
            return Json.Write(root);
        }

        /// <summary>Offline reader; media preparation is deferred to Audio. Identity comes from the package.</summary>
        public static ContentInput ReadContent(string text, string owner, Func<string, byte[]> readResource)
        {
            var root = Json.Object(Json.Read(text), "$", "schemaVersion monsters", "audio animals");
            if (Json.Int(root["schemaVersion"], "schemaVersion") != 1) throw new AuthoringException("UnsupportedVersion", "schemaVersion", "Expected Runtime content schemaVersion 1.");
            var input = ReadSection(root["monsters"], owner, readResource);
            if (root.TryGetValue("audio", out var audio))
            {
                DTMAPI.AudioContent.AudioRegistration declaration;
                try { declaration = DTMAPI.AudioContent.Authoring.AudioContentReader.ParseSection(audio); }
                catch (System.IO.InvalidDataException ex) { throw new AuthoringException("InvalidAudio", "audio", ex.Message); }
                foreach (var row in input.Definitions.SelectMany(SoundValidation.CommonEvents).Concat(input.NativeOverrides.SelectMany(SoundValidation.CommonEvents)))
                {
                    var sound = row.Value;
                    var entry = declaration.Events.FirstOrDefault(e => e.Key == sound.ContentSoundId);
                    if (entry == null) continue;
                    sound.ResourcePath = declaration.Resources.Single(r => r.Key == entry.Resource).Path;
                    sound.Loop = entry.Loop;
                }
            }
            return input;
        }
    }
}
