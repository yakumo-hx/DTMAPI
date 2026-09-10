using DTMAPI.Authoring.Contracts;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DTMAPI.AuthorSdk;

internal static class SymbolInspector
{
    internal static CommandReport Execute(ParsedCommand command)
    {
        command.RequireOnlyOptions("pdb");
        if (command.Positionals.Count != 1) throw new CommandLineException("symbols requires one DLL and optional --pdb path.");
        string dll = Path.GetFullPath(command.Positionals[0]);
        string pdb = Path.GetFullPath(command.Option("pdb", Path.ChangeExtension(dll, ".pdb")));
        var report = new CommandReport { Command = "symbols", RootPath = dll, OutputPath = pdb };
        try
        {
            using var peStream = File.OpenRead(dll);
            using var pe = new PEReader(peStream);
            using var pdbStream = File.OpenRead(pdb);
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            MetadataReader symbols = provider.GetMetadataReader();
            var id = new BlobContentId(symbols.DebugMetadataHeader!.Id);
            bool matched = pe.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.CodeView &&
                entry.Stamp == id.Stamp && pe.ReadCodeViewDebugDirectoryData(entry).Guid == id.Guid &&
                pe.ReadCodeViewDebugDirectoryData(entry).Age == 1);
            if (!matched) throw new InvalidDataException("Portable PDB identity does not match this DLL. Use the symbols from the same build.");
            report.Values["dllSha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))).ToLowerInvariant();
            report.Values["pdbSha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pdb))).ToLowerInvariant();
            report.Values["moduleMvid"] = pe.GetMetadataReader().GetGuid(pe.GetMetadataReader().GetModuleDefinition().Mvid).ToString("D");
            report.Values["symbolIdentity"] = id.Guid.ToString("D") + ":" + id.Stamp;
            report.Values["documents"] = string.Join(";", symbols.Documents.Take(50).Select(handle => symbols.GetString(symbols.GetDocument(handle).Name)));
            report.Values["documentCount"] = symbols.Documents.Count.ToString();
            report.Success = true;
            report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK190", Severity = DiagnosticSeverity.Info, Message = "Matching portable symbols. Shipping Mono line support must be verified in the actual host; this check does not prove debugger attach." });
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
        {
            report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK191", Severity = DiagnosticSeverity.Error, Message = ex.Message, Path = pdb, Guidance = "Keep DLL and PDB from the same build; restore matching symbols and restart the game. DLL: " + dll });
        }
        return report;
    }
}
