using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Internal;

namespace DTMAPI.AudioContent
{
    public static class AudioContentReader
    {
        public static AudioRegistration Parse(string json) => ParseSection(StrictJson.Read(json, maximumBytes: int.MaxValue));
        public static AudioRegistration ParseSection(object? section)
        {
            Dictionary<string, object?> Object(object? value, params string[] allowed)
            {
                if (!(value is Dictionary<string, object?> obj) || obj.Keys.Any(k => !allowed.Contains(k))) throw new InvalidDataException("AudioSchema: unexpected object or field.");
                return obj;
            }
            object? Field(Dictionary<string, object?> obj, string name) => obj.TryGetValue(name, out var value) ? value : throw new InvalidDataException("AudioSchema: missing " + name);
            string Text(Dictionary<string, object?> obj, string name) => Field(obj, name) as string ?? throw new InvalidDataException("AudioSchema: expected string " + name);
            List<object?> Array(Dictionary<string, object?> obj, string name) => Field(obj, name) as List<object?> ?? throw new InvalidDataException("AudioSchema: expected array " + name);
            double Number(Dictionary<string, object?> obj, string name, double fallback) => !obj.TryGetValue(name, out var value) ? fallback : value is double number ? number : throw new InvalidDataException("AudioSchema: expected number " + name);
            var root = Object(section, "resources", "events");
            string[] Paths(Dictionary<string, object?> obj, string name) => !obj.ContainsKey(name) ? System.Array.Empty<string>() : Array(obj, name).Select(v => v as string ?? throw new InvalidDataException("AudioSchema: expected path")).ToArray();
            var resources = Array(root, "resources").Select(value => { var obj = Object(value, "key", "path", "sha256", "stream", "kind", "media", "dependencies");
                string kind = obj.ContainsKey("kind") ? Text(obj, "kind") : "source";
                return new AudioResource { Key = Text(obj, "key"), Path = Text(obj, "path"), Sha256 = obj.ContainsKey("sha256") ? Text(obj, "sha256") : "",
                    Kind = kind == "source" ? AudioResourceKind.Source : kind == "bank" ? AudioResourceKind.Bank : throw new InvalidDataException("AudioResourceKind"),
                    Media = Paths(obj, "media"), Dependencies = Paths(obj, "dependencies"),
                    Stream = obj.TryGetValue("stream", out var streaming) ? streaming is bool enabled ? enabled : throw new InvalidDataException("AudioStreamInvalid") : false }; }).ToArray();
            var events = Array(root, "events").Select(value =>
            {
                var obj = Object(value, "key", "resource", "use", "route", "bankEvent", "nativeEvent", "replaceNativeEvent", "volumeDb", "loop", "minimumIntervalMs");
                string use = Text(obj, "use");
                AudioUse purpose = use == "effect" ? AudioUse.Effect : use == "music" ? AudioUse.Music : throw new InvalidDataException("AudioUseUnsupported");
                string route = obj.ContainsKey("route") ? Text(obj, "route") : purpose == AudioUse.Music ? "Music" : "Actor";
                if (!Enum.TryParse(route, true, out AudioRoute routing) || !Enum.IsDefined(typeof(AudioRoute), routing)) throw new InvalidDataException("AudioRouteUnsupported");
                double interval = Number(obj, "minimumIntervalMs", 0);
                float volumeDb = obj.TryGetValue("volumeDb", out var volume) && volume is string text && text == "-inf" ? float.NegativeInfinity : (float)Number(obj, "volumeDb", 0);
                if (float.IsInfinity(volumeDb) && !(volume is string mute && mute == "-inf")) throw new InvalidDataException("AudioVolumeInvalid");
                if (interval < 0 || interval > int.MaxValue || interval != Math.Truncate(interval)) throw new InvalidDataException("AudioIntervalInvalid");
                return new AudioEvent { Key = Text(obj, "key"), Resource = Text(obj, "resource"), Use = purpose,
                    Route = routing,
                    BankEvent = obj.ContainsKey("bankEvent") ? Text(obj, "bankEvent") : "",
                    NativeEvent = obj.ContainsKey("nativeEvent") ? Text(obj, "nativeEvent") : "",
                    ReplaceNativeEvent = obj.ContainsKey("replaceNativeEvent") ? Text(obj, "replaceNativeEvent") : "", VolumeDb = volumeDb,
                    Loop = obj.TryGetValue("loop", out var loop) ? loop is bool enabled ? enabled : throw new InvalidDataException("AudioLoopInvalid") : false, MinimumIntervalMs = (int)interval };
            }).ToArray();
            return AudioValidation.CopyAndValidate(new AudioRegistration { Resources = resources, Events = events });
        }
    }
}
