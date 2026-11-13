using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DTMAPI.Author.Build;

// Host-neutral serialization adapter. MSBuild evaluates every property/item; the SDK
// process reads the resulting XML. This task does not evaluate, resolve or compile.
public sealed class WriteBuildFacts : Task
{
    [Required] public string OutputFile { get; set; } = "";
    public ITaskItem[] Properties { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            var root = new XElement("BuildFacts",
                new XElement("Properties", Properties.Select(p => new XElement("Property",
                    new XAttribute("Name", p.ItemSpec), p.GetMetadata("Value")))),
                new XElement("Items", Items.Select(item => new XElement("Item",
                    new XAttribute("Kind", item.GetMetadata("DtmApiFactKind")),
                    new XAttribute("Path", item.GetMetadata("DtmApiFactKind") == "CompilerArgument" ? item.ItemSpec : item.GetMetadata("FullPath")),
                    item.MetadataNames.Cast<string>().OrderBy(name => name, StringComparer.Ordinal)
                        .Select(name => new XElement("Metadata", new XAttribute("Name", name), item.GetMetadata(name)))))));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputFile))!);
            new XDocument(root).Save(OutputFile);
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
        { Log.LogErrorFromException(ex); return false; }
    }
}
