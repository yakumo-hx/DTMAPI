using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Runs in the selected SDK's MSBuild process. No selected project targets execute.
public sealed class DtmApiBuildInputs : Task
{
    [Required] public ITaskItem[] Projects { get; set; }
    [Required] public string Configuration { get; set; }
    [Required] public string OutputFile { get; set; }
    public string PathsFile { get; set; }
    public string ChangedPathsFile { get; set; }
    public bool AllowMissing { get; set; }

    public override bool Execute()
    {
        var rows = new SortedSet<string>(StringComparer.Ordinal);
        var files = new SortedSet<string>(StringComparer.Ordinal);
        var projectFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var fileItems = new HashSet<string>(new[] { "Compile", "EmbeddedResource", "AdditionalFiles", "Content", "None", "Analyzer" });
        var changedPaths = string.IsNullOrEmpty(ChangedPathsFile) ? Array.Empty<string>() : File.ReadAllLines(ChangedPathsFile);
        try
        {
            using (var collection = new ProjectCollection())
            {
                var entries = Projects.Select(item => new ProjectGraphEntryPoint(item.ItemSpec,
                    new Dictionary<string, string> { ["Configuration"] = Configuration }));
                var graph = new ProjectGraph(entries, collection, (path, properties, projectCollection) =>
                {
                    var project = new Project(path, properties, null, projectCollection);
                    // Graph evaluation can run concurrently. Serialize collection of the receipt.
                    lock (rows)
                    {
                        files.Add(project.FullPath);
                        var localFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project.FullPath };
                        if (!projectFiles.TryGetValue(project.FullPath, out var knownFiles)) projectFiles[project.FullPath] = knownFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var import in project.Imports) { files.Add(import.ImportedProject.FullPath); localFiles.Add(import.ImportedProject.FullPath); }
                        foreach (var property in project.AllEvaluatedProperties)
                            if (!property.IsEnvironmentProperty && !property.IsReservedProperty)
                                rows.Add(path + "|property|" + property.Name + "|" + property.EvaluatedValue);
                        foreach (var item in project.Items.Where(item => fileItems.Contains(item.ItemType) || item.ItemType == "ProjectReference" || item.ItemType == "Reference" || item.ItemType == "PackageReference"))
                        {
                            rows.Add(path + "|item|" + item.ItemType + "|" + item.EvaluatedInclude + "|" + string.Join("|",
                                item.Metadata.OrderBy(m => m.Name, StringComparer.Ordinal).Select(m => m.Name + "=" + m.EvaluatedValue)));
                            if (item.GetMetadataValue("DtmApiGeneratedBuildAsset") == "true") continue;
                            string input = item.ItemType == "Reference" ? item.GetMetadataValue("HintPath") :
                                fileItems.Contains(item.ItemType) ? item.GetMetadataValue("FullPath") : "";
                            if (input.Length == 0) continue;
                            input = Path.GetFullPath(Path.IsPathRooted(input) ? input : Path.Combine(project.DirectoryPath, input));
                            localFiles.Add(input);
                            // None includes may describe optional non-files; required compile/resource/reference inputs fail closed.
                            if (File.Exists(input)) files.Add(input);
                            else if (item.ItemType != "None" && !AllowMissing) throw new FileNotFoundException("Evaluated " + item.ItemType + " input is missing in " + project.FullPath + ": " + input, input);
                            else rows.Add(input + "|missing");
                        }
                        // Use MSBuild's evaluated include/exclude/remove semantics, including
                        // globs whose final file no longer exists in the working tree.
                        if (changedPaths.Length > 0)
                            foreach (var glob in project.GetAllGlobs().Where(g => fileItems.Contains(g.ItemElement.ItemType)))
                                foreach (string changed in changedPaths)
                                    if (glob.MsBuildGlob.IsMatch(Path.GetRelativePath(project.DirectoryPath, changed))) localFiles.Add(changed);
                        knownFiles.UnionWith(localFiles);
                    }
                    return project.CreateProjectInstance();
                });
                if (!string.IsNullOrEmpty(PathsFile))
                {
                    var mappings = graph.EntryPointNodes.Select(root =>
                    {
                        var seen = new HashSet<ProjectGraphNode>();
                        void Visit(ProjectGraphNode node) { if (seen.Add(node)) foreach (var child in node.ProjectReferences) Visit(child); }
                        Visit(root);
                        return new {
                            Project = root.ProjectInstance.FullPath,
                            Files = seen.SelectMany(node => projectFiles[node.ProjectInstance.FullPath]).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToArray(),
                            Directories = seen.Select(node => Path.GetDirectoryName(node.ProjectInstance.FullPath)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                            OutputDirectories = seen.Select(node => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(node.ProjectInstance.FullPath), node.ProjectInstance.GetPropertyValue("OutputPath")))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                        };
                    });
                    File.WriteAllText(PathsFile, JsonSerializer.Serialize(mappings));
                }
            }
            using (var hash = SHA256.Create())
            {
                foreach (string path in files)
                    using (var stream = File.OpenRead(path))
                        rows.Add(path + "|sha256|" + BitConverter.ToString(hash.ComputeHash(stream)));
                File.WriteAllText(OutputFile, BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", rows)))).Replace("-", "").ToLowerInvariant());
            }
            return true;
        }
        catch (Exception ex) { Log.LogErrorFromException(ex, true); return false; }
    }
}
