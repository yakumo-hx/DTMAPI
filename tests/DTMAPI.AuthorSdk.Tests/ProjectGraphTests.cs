using System.Xml.Linq;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestProjectGraph(string temporary, string compatibility)
    {
        string root = Path.Combine(temporary, "standard graph boundaries"), mod = Path.Combine(root, "Mod"), library = Path.Combine(root, "Library");
        compatibility = Path.Combine(FindRepository(), ".tools/author-sdk-compatibility/0.7.0");
        Directory.CreateDirectory(library);
        string libraryPath = Path.Combine(library, "Maple.GraphLibrary.csproj");
        const string libraryProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>";
        File.WriteAllText(libraryPath, libraryProject);
        File.WriteAllText(Path.Combine(library, "Value.cs"), "namespace Maple { public static class GraphLibrary { public static int Value=>42; } }");
        await ExpectSuccess("standard graph Mod", "new", "codemod", mod, "--id", "Maple.GraphMod", "--name", "Graph", "--author", "Maple");
        string projectPath = Path.Combine(mod, "Maple.GraphMod.csproj");
        string project = File.ReadAllText(projectPath).Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../Library/Maple.GraphLibrary.csproj\" DtmApiDistribution=\"self-authored\" /></ItemGroup></Project>");
        File.WriteAllText(projectPath, project);
        File.WriteAllText(Path.Combine(mod, "src/ModEntry.cs"), "using DTMAPI.Abstractions; namespace Maple.GraphMod; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper helper){helper.Monitor.Log(Maple.GraphLibrary.Value.ToString());} }");
        await ExpectSuccess("standard graph restore", "restore", mod);
        var valid = await ExpectSuccess("standard graph pack", "pack", mod, "--compatibility-root", compatibility);
        File.WriteAllText(Path.Combine(mod, "one.txt"), "first"); File.WriteAllText(Path.Combine(mod, "two.txt"), "second");
        foreach (string fault in new[] { "cycle", "wrong-child-framework", "missing-generated", "resource-collision" })
        {
            if (fault == "cycle") File.WriteAllText(libraryPath, libraryProject.Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../Mod/Maple.GraphMod.csproj\" /></ItemGroup></Project>"));
            if (fault == "wrong-child-framework") File.WriteAllText(libraryPath, libraryProject.Replace("netstandard2.0", "net8.0"));
            if (fault == "missing-generated") File.WriteAllText(projectPath, project.Replace("</Project>", "<ItemGroup><Compile Include=\"generated-missing.cs\" /></ItemGroup></Project>"));
            if (fault == "resource-collision") File.WriteAllText(projectPath, project.Replace("</Project>", "<ItemGroup><EmbeddedResource Include=\"one.txt\" LogicalName=\"Duplicate\"/><EmbeddedResource Include=\"two.txt\" LogicalName=\"Duplicate\"/></ItemGroup></Project>"));
            // Restore diagnoses graph changes; package failures must still leave the
            // previous delivered package untouched and report the standard task ID.
            var failed = await ExpectFailure(fault, "pack", mod, "--compatibility-root", compatibility);
            True(failed.Diagnostics.Any(d => d.Code.StartsWith("MSB", StringComparison.Ordinal) || d.Code.StartsWith("NU", StringComparison.Ordinal) || d.Code.StartsWith("CS", StringComparison.Ordinal)), "standard graph diagnostic " + fault);
            Equal(valid.Sha256, Sha256(valid.OutputPath), "graph failure preserves package " + fault);
            File.WriteAllText(libraryPath, libraryProject); File.WriteAllText(projectPath, project);
        }
    }
}
