using System;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;

if ((args.Length != 6 && args.Length != 7) || args[0] != "--project" || args[2] != "--catalog" || args[4] != "--output" || (args.Length == 7 && args[6] != "--validate-only"))
{ Console.Error.WriteLine("Usage: --project file.dtmonster.json --catalog catalog.json --output NEW_DIRECTORY [--validate-only]"); return 2; }
bool validateOnly = args.Length == 7;
string output = Path.GetFullPath(args[5]);
if (Directory.Exists(output) || File.Exists(output)) { Console.Error.WriteLine("OutputExists: choose a new output directory; existing results are preserved."); return 2; }
Directory.CreateDirectory(output);
try
{
    byte[] projectBytes = File.ReadAllBytes(args[1]), catalogBytes = File.ReadAllBytes(args[3]);
    var input = ContentCodec.ReadProject(Encoding.UTF8.GetString(projectBytes));
    var catalog = CatalogCodec.Read(Encoding.UTF8.GetString(catalogBytes));
    var errors = ContentCompiler.Validate(input, catalog);
    if (errors.Count != 0)
    {
        File.WriteAllText(Path.Combine(output, "validation.json"), Json.Write(Json.Map("success", false, "diagnostics", errors.Select(CatalogCodec.DiagnosticData).ToArray())), new UTF8Encoding(false));
        foreach (var e in errors) Console.Error.WriteLine(e.Code + ":" + e.Field + ":" + e.Message);
        return 1;
    }
    if (!validateOnly)
    {
        string payload = ContentCodec.WriteContent(input, catalog);
        foreach (var resource in input.Images.Concat(input.Audio)) { string path = Path.Combine(output, resource.Key); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, resource.Value); }
        Directory.CreateDirectory(Path.Combine(output, "PackData"));
        File.WriteAllText(Path.Combine(output, "PackData", "content.json"), payload, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output, "catalog.json"), Json.Write(Json.Map("schemaVersion", 1, "binding", "experimental-unbound", "compilerVersion", "0.1.0", "gameVersion", catalog.GameVersion, "moduleVersion", catalog.ModuleVersion,
            "catalogFingerprint", catalog.Fingerprint, "catalogSha256", ContentCodec.Hash(catalogBytes), "projectSha256", ContentCodec.Hash(projectBytes), "payloadSha256", ContentCodec.Hash(Encoding.UTF8.GetBytes(payload)),
            "images", input.Images.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => Json.Map("path", p.Key, "length", p.Value.Length, "sha256", ContentCodec.Hash(p.Value))).ToArray(),
            "audio", input.Audio.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => Json.Map("path", p.Key, "length", p.Value.Length, "sha256", ContentCodec.Hash(p.Value))).ToArray())), new UTF8Encoding(false));
    }
    File.WriteAllText(Path.Combine(output, "validation.json"), Json.Write(Json.Map("success", true, "scope", "offline-reference-validation", "diagnostics", Array.Empty<object>(), "runtimeRevalidationRequired", true)), new UTF8Encoding(false));
    Console.WriteLine("PASS " + (validateOnly ? "validation" : "candidate") + " definitions=" + input.Definitions.Count + " pools=" + input.DropPools.Count + " rules=" + input.SpawnRules.Count);
    return 0;
}
catch (Exception ex)
{
    var diagnostic = ex is AuthoringException authoring ? authoring.Diagnostic : new MonsterDiagnostic("CompilerFailure", ex.Message, stage: "Compile", field: "input/output");
    // A failed run has no success marker. Any partially written files are explicitly
    // invalidated; existing user directories were never opened for output.
    // This directory was created by this invocation after refusing existing paths.
    // Remove every partial artifact, leaving diagnostics only on any failed compile.
    foreach (string file in Directory.GetFiles(output, "*", SearchOption.AllDirectories)) File.Delete(file);
    foreach (string directory in Directory.GetDirectories(output)) Directory.Delete(directory, true);
    File.WriteAllText(Path.Combine(output, "validation.json"), Json.Write(Json.Map("success", false, "diagnostics", new[] { CatalogCodec.DiagnosticData(diagnostic) })), new UTF8Encoding(false));
    Console.Error.WriteLine(diagnostic.Code + ":" + diagnostic.Field + ":" + diagnostic.Message);
    return ex is AuthoringException ? 1 : 3;
}
