using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Reflection;
using System.Runtime.Loader;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestProjectGraph(string temporary, string compatibility)
    {
        compatibility = Path.Combine(FindRepository(), ".tools", "author-sdk-compatibility", "0.6.4");
        string workspace = Path.Combine(temporary, "project graph"), leaf = Path.Combine(workspace, "Leaf"), middle = Path.Combine(workspace, "Middle"), mod = Path.Combine(workspace, "Mod");
        foreach (var pair in new[] { (leaf, "Maple.Leaf"), (middle, "Maple.Middle") })
            True((await Run("new", "library", pair.Item1, "--id", pair.Item2, "--api-target", "0.6.4", "--json")).Report.Success, "new library " + pair.Item2);
        True((await Run("new", "codemod", mod, "--id", "Maple.GraphMod", "--name", "Graph", "--author", "Maple", "--api-target", "0.6.4", "--json")).Report.Success, "new graph Mod");
        void Edit(string root, bool library, Action<JsonObject> change)
        {
            string path = Path.Combine(root, library ? "dtmapi.library.json" : "dtmapi.author.json");
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject(); change(json); File.WriteAllText(path, json.ToJsonString());
        }
        Edit(middle, true, json => json["build"] = new JsonObject { ["workspaceRoot"] = "..", ["projectReferences"] = new JsonArray("../Leaf/Maple.Leaf.csproj") });
        File.WriteAllText(Path.Combine(middle, "src", "Library.cs"), "namespace Maple.Middle; public static class Library { public static int Value => Maple.Leaf.Library.Value + 1; }");
        Directory.CreateDirectory(Path.Combine(mod, "assets")); Directory.CreateDirectory(Path.Combine(mod, "generated"));
        File.WriteAllText(Path.Combine(mod, "assets", "text.txt"), "embedded-value");
        File.WriteAllText(Path.Combine(mod, "assets", "copy.txt"), "copied-value");
        File.WriteAllText(Path.Combine(mod, "generated", "Value.cs"), "namespace Maple.GraphMod; public static class Generated { public const int Value=7; }");
        Edit(mod, false, json => json["build"] = new JsonObject { ["workspaceRoot"] = "..", ["projectReferences"] = new JsonArray("../Middle/Maple.Middle.csproj"),
            ["embeddedResources"] = new JsonArray(new JsonObject { ["path"] = "assets/text.txt", ["logicalName"] = "Maple.Graph.Text" }),
            ["contentFiles"] = new JsonArray(new JsonObject { ["path"] = "assets/copy.txt", ["targetPath"] = "Content/Graph/copy.txt" }),
            ["generatedSourceFiles"] = new JsonArray("generated/Value.cs") });
        File.WriteAllText(Path.Combine(mod, "src", "ModEntry.cs"), "using DTMAPI.Abstractions; namespace Maple.GraphMod; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper h){} public static int Value => Maple.Middle.Library.Value+Generated.Value; }");
        var first = (await Run("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(first.Success, "graph pack: " + string.Join(";", first.Diagnostics.Select(d => d.Message)));
        var second = (await Run("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(second.Success && second.Sha256 == first.Sha256, "graph deterministic pack");
        string extracted = Path.Combine(workspace, "extracted"); ZipFile.ExtractToDirectory(first.OutputPath, extracted);
        True(File.Exists(Path.Combine(extracted, "lib/private/Maple.Leaf.dll")) && File.Exists(Path.Combine(extracted, "lib/private/Maple.Middle.dll")), "transitive library payloads");
        True(File.ReadAllText(Path.Combine(extracted, "Content/Graph/copy.txt")) == "copied-value", "explicit content payload");
        var loader = new AssemblyLoadContext("project-graph-test", isCollectible: true);
        loader.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly :
            loader.LoadFromAssemblyPath(Path.Combine(extracted, "lib", "private", name.Name + ".dll"));
        Assembly assembly = loader.LoadFromAssemblyPath(Path.Combine(extracted, "Content/DTMAPI/Maple.GraphMod.dll"));
        True((int)assembly.GetType("Maple.GraphMod.ModEntry")!.GetProperty("Value")!.GetValue(null)! == 50, "actual transitive CLR call");
        using (var stream = assembly.GetManifestResourceStream("Maple.Graph.Text"))
            True(stream != null && new StreamReader(stream).ReadToEnd() == "embedded-value", "actual embedded resource lookup");
        loader.Unload();
        string authorFile = Path.Combine(mod, "dtmapi.author.json"), original = File.ReadAllText(authorFile);
        string libraryFile = Path.Combine(leaf, "dtmapi.library.json"), originalLibrary = File.ReadAllText(libraryFile);
        foreach (string fault in new[] { "cycle", "escape", "missing-generated", "resource-collision", "unsupported-xml", "bad-library-tfm" })
        {
            if (fault == "cycle") Edit(leaf, true, j => j["build"] = new JsonObject { ["workspaceRoot"] = "..", ["projectReferences"] = new JsonArray("../Middle/Maple.Middle.csproj") });
            if (fault == "escape") Edit(mod, false, j => j["build"]!["workspaceRoot"] = ".");
            if (fault == "missing-generated") Edit(mod, false, j => j["build"]!["generatedSourceFiles"] = new JsonArray("generated/missing.cs"));
            if (fault == "resource-collision") Edit(mod, false, j => j["build"]!["embeddedResources"]!.AsArray().Add(new JsonObject { ["path"] = "assets/text.txt", ["logicalName"] = "maple.graph.text" }));
            if (fault == "bad-library-tfm") Edit(leaf, true, j => j["targetFramework"] = "net8.0");
            string projectFile = Path.Combine(mod, "Maple.GraphMod.csproj"), projectOriginal = File.ReadAllText(projectFile);
            if (fault == "unsupported-xml") File.WriteAllText(projectFile, projectOriginal.Replace("</Project>", "<ItemGroup><Analyzer Include=\"unknown.dll\" /></ItemGroup></Project>"));
            var rejected = (await Run("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
            True(!rejected.Success, "graph rejects " + fault);
            True(DTMAPI.AuthorSdk.PathSafety.Sha256File(first.OutputPath) == first.Sha256, "failed graph preserves old package " + fault);
            File.WriteAllText(authorFile, original); File.WriteAllText(libraryFile, originalLibrary); File.WriteAllText(projectFile, projectOriginal);
        }
    }
}
