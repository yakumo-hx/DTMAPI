using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.RuntimeModules.Content;

static partial class Program
{
    static void RuntimeContentChecks()
    {
        string root = Path.Combine(Path.GetTempPath(), "dtmapi-monster-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "PackData"));
        try
        {
            var backend = new ScopedBackend();
            using var host = new MonsterModuleFixture(() => backend);
            host.Root = _ => root;
            var owner = new Manifest("File");
            var api = host.Api(owner);
            var input = new ContentInput { OwnerId = owner.UniqueID };
            input.Definitions.Add(Definition("file"));
            string path = Path.Combine(root, "PackData", "content.json");
            File.WriteAllText(path, ContentCodec.WriteContent(input));
            using var source = RuntimeContent.Prepare(host.Owner(owner)); source.Activate();
            var record = backend.Definitions.Find(owner.UniqueID, "File:file")!;
            Check(record.Definition.Health == 60 && record.State == MonsterDefinitionState.Creatable, "The fixed file entry reaches the same scoped definition registry as the API without Audio demand.");
            Check(api.RegisterDefinition(Definition("file")).Diagnostic!.Code == "DuplicateId", "File and API share the Owner ID index.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"monsters\":{\"invalid\":true}}"); source.Refresh();
            Check(record.State == MonsterDefinitionState.Creatable, "A malformed file section is rejected before its valid generation is withdrawn.");
            input.Definitions[0].Sounds = new MonsterSounds { Hurt = new MonsterSound { ResourcePath = "missing.wav" } };
            using var parsed = JsonDocument.Parse(ContentCodec.WriteContent(input));
            File.WriteAllText(path, "{\"schemaVersion\":1,\"audio\":{\"invalid\":true},\"monsters\":" + parsed.RootElement.GetProperty("monsters").GetRawText() + "}");
            source.Refresh();
            var withFailedSound = backend.Definitions.Find(owner.UniqueID, "File:file")!;
            Check(withFailedSound.State == MonsterDefinitionState.Creatable && withFailedSound.Definition.Sounds!.Hurt!.RegisteredSound!() == null && host.Reports.Any(r => r.StartsWith("audio:")), "Audio failure leaves the file body accepted and selects native sound fallback.");
            source.Dispose();
            using var direct = api.RegisterDefinition(Definition("file")).Value!;
            Check(direct.Snapshot.State == MonsterDefinitionState.Creatable && record.State == MonsterDefinitionState.Withdrawn, "File withdrawal frees the identity for direct API registration and isolates the old generation.");
        }
        finally { Directory.Delete(root, true); }
    }
}
