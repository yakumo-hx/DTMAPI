using DTMAPI.Authoring.Contracts;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DTMAPI.AuthorSdk;

// One snapshot supplies both the compiler and its input identity. Output paths are
// deliberately excluded so identical projects in different directories agree.
internal sealed class CodeModBuildInput
{
    public sealed record Source(string Path, string Text);

    private CodeModBuildInput(Source[] sources, string sourceTreeSha256)
    {
        Sources = sources;
        SourceTreeSha256 = sourceTreeSha256;
    }

    public Source[] Sources { get; }
    public string SourceTreeSha256 { get; }

    public static CodeModBuildInput Read(string sourceRoot, IEnumerable<string> files, Func<string, string>? logicalPath = null)
    {
        var sources = new List<Source>();
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in files)
        {
            byte[] bytes = File.ReadAllBytes(file);
            string relative = logicalPath == null ? PathSafety.RelativePath(sourceRoot, file) : logicalPath(file);
            digest.AppendData(Encoding.UTF8.GetBytes(relative + "\0" + bytes.Length.ToString(CultureInfo.InvariantCulture) + "\0"));
            digest.AppendData(bytes);
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            sources.Add(new Source(relative, reader.ReadToEnd()));
        }
        return new CodeModBuildInput(sources.ToArray(), Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant());
    }

    public void AddIdentity(CommandReport report, string generatedAssemblyInfo)
    {
        var inputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = "1",
            ["sourceTreeSha256"] = SourceTreeSha256,
            ["assemblyInfoSha256"] = PathSafety.Sha256Bytes(Encoding.UTF8.GetBytes(generatedAssemblyInfo)),
            ["sdkCompilerSha256"] = PathSafety.Sha256File(typeof(CodeModBuildInput).Assembly.Location),
            ["roslynSha256"] = PathSafety.Sha256File(typeof(CSharpCompilation).Assembly.Location),
            ["compilerOptions"] = report.Values["compilerOptions"]
        };
        foreach (string key in new[] { "apiTarget", "assemblyName", "codeModKind", "configuration", "referenceIdentities", "abstractionsSha256", "compatibilityManifestSha256", "referencePolicySha256", "compilerReferenceSurfaceSha256", "gameAssemblySha256", "projectGraphInputs", "projectResourceInputsSha256", "restoreLockSha256" })
            if (report.Values.TryGetValue(key, out string? value)) inputs[key] = value;
        string json = JsonSerializer.Serialize(inputs);
        report.Values["buildInputIdentity"] = json;
        report.Values["buildInputSha256"] = PathSafety.Sha256Bytes(Encoding.UTF8.GetBytes(json));
    }
}
