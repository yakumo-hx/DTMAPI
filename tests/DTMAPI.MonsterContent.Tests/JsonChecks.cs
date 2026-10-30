using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static object? ReferenceJson(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => ReferenceJson(p.Value), StringComparer.Ordinal),
        JsonValueKind.Array => e.EnumerateArray().Select(ReferenceJson).ToList(),
        JsonValueKind.String => e.GetString(), JsonValueKind.Number => e.GetDouble(),
        JsonValueKind.True => true, JsonValueKind.False => false, _ => null
    };
    static void JsonChecks()
    {
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "strict-json-cases.json")));
        foreach (var row in corpus.RootElement.GetProperty("valid").EnumerateArray())
        {
            string text = row.GetProperty("text").GetString()!;
            using var reference = JsonDocument.Parse(text.StartsWith("\uFEFF", StringComparison.Ordinal) ? text.Substring(1) : text);
            Check(Json.Write(Json.Read(text)) == Json.Write(ReferenceJson(reference.RootElement)), "Shared valid JSON corpus: " + row.GetProperty("name").GetString());
        }
        void Reject(Action read, string code, string name)
        {
            try { read(); throw new Exception("Accepted invalid JSON: " + name); }
            catch (AuthoringException e)
            {
                Check(e.Diagnostic.Code == code, name + ": expected " + code + ", got " + e.Diagnostic.Code);
                if (code == "InvalidJson") Check(e.Message.Contains("character"), "Syntax failure includes a character position: " + name);
            }
        }
        foreach (var row in corpus.RootElement.GetProperty("invalid").EnumerateArray())
            Reject(() => Json.Read(row.GetProperty("text").GetString()!), row.GetProperty("code").GetString()!, row.GetProperty("name").GetString()!);
        Check(Json.Read(new string('[', 63) + "0" + new string(']', 63)) is List<object?>, "64 value levels are accepted.");
        Reject(() => Json.Read(new string('[', 64) + "0" + new string(']', 64)), "InvalidJson", "65 value levels");
        Check(Json.Read("{}" + new string(' ', 12 * 1024 * 1024 + 1)) is Dictionary<string, object?>, "Large author projects have no platform byte budget.");

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !(File.Exists(Path.Combine(directory.FullName, "DTMAPI.sln")) && File.Exists(Path.Combine(directory.FullName, "global.json")))) directory = directory.Parent;
        string sample = Path.Combine((directory ?? throw new DirectoryNotFoundException("Repository root was not found.")).FullName, "examples/monster-authoring/mc08");
        var sampleImages = ContentCodec.ReadProject(File.ReadAllText(Path.Combine(sample, "browser-saved.dtmonster.json"))).Images;
        var documents = new[]
        {
            (Name: "project", Text: File.ReadAllText(Path.Combine(sample, "browser-saved.dtmonster.json")), Read: (Action<string>)(s => ContentCodec.ReadProject(s))),
            (Name: "payload", Text: File.ReadAllText(Path.Combine(sample, "compiled/PackData/content.json")), Read: (Action<string>)(s => ContentCodec.ReadContent(s, "A", p => sampleImages[p]))),
            (Name: "catalog", Text: File.ReadAllText(Path.Combine(sample, "catalog-snapshot.json")), Read: (Action<string>)(s => CatalogCodec.Read(s)))
        };
        foreach (var document in documents)
        {
            document.Read(document.Text); document.Read("\uFEFF" + document.Text);
            Check(true, document.Name + " accepts valid input and one BOM.");
            string text = document.Text.TrimEnd();
            var malformed = new[] { text.Substring(0, text.Length - 1) + ",}", new Regex("\"(version|schemaVersion)\":\\s*([0-9])").Replace(text, "\"$1\":0$2", 1), text + " {}", "\uFEFF\uFEFF" + text };
            foreach (string bad in malformed) Reject(() => document.Read(bad), "InvalidJson", document.Name + " grammar boundary");
        }
    }
}
