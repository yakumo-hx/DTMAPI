using System;
using System.IO;
using System.Text;
using DTMAPI.MapData;

try
{
    if (args.Length != 2 && args.Length != 4 || args[0] != "--project" || args.Length == 4 && args[2] != "--output")
    { Console.Error.WriteLine("Usage: MapData.Compiler --project author.dtmap.json [--output compiled.json]"); return 64; }
    string source = Path.GetFullPath(args[1]);
    string? output = args.Length == 4 ? Path.GetFullPath(args[3]) : null;
    if (output != null && string.Equals(source, output, StringComparison.OrdinalIgnoreCase)) { Console.Error.WriteLine("Output must not replace the author project."); return 64; }
    if (new FileInfo(source).Length > 16 * 1048576) { Console.Error.WriteLine("{\"ok\":false,\"diagnostics\":[{\"code\":\"invalid-shape\",\"field\":\"$\",\"message\":\"Input exceeds 16 MiB\"}]}"); return 2; }
    var result = MapCompiler.Compile(File.ReadAllText(source, new UTF8Encoding(false, true)));
    if (!result.Success) { Console.Error.Write(result.ToJson()); return 2; }
    if (output != null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string candidate = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(candidate, result.Map!.ToJson(), new UTF8Encoding(false)); File.Move(candidate, output, true); }
        finally { if (File.Exists(candidate)) File.Delete(candidate); }
    }
    Console.Write(result.ToJson()); return 0;
}
catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is DecoderFallbackException || error is ArgumentException)
{ Console.Error.WriteLine("Map compiler I/O error: " + error.Message); return 3; }
