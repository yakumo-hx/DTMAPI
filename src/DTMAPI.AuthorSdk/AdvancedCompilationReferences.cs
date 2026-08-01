using DTMAPI.Authoring.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace DTMAPI.AuthorSdk;

internal sealed class AdvancedCompilationReferenceSet
{
    public required IReadOnlyList<MetadataReference> References { get; init; }
    public required string SurfaceSha256 { get; init; }
}

internal static class AdvancedCompilationReferences
{
    private const string SurfaceResourcePrefix = "DTMAPI.AuthorSdk.advanced-reference-surface.";
    private const string SurfaceResourceSuffix = ".Assembly-CSharp.reference.cs.txt";

    public static AdvancedCompilationReferenceSet Create(
        CompatibilityAssets compatibility,
        ResolvedAdvancedReferenceSet resolved)
    {
        if (!resolved.Registration.PolicyId.Equals(resolved.Policy.PolicyId, StringComparison.Ordinal))
            throw new InvalidDataException("Advanced compiler reference registration/policy identity mismatch.");

        AdvancedReferencePolicyEntry gameAssembly = resolved.Policy.References.Single(reference =>
            reference.AssemblyName.Equals("Assembly-CSharp", StringComparison.Ordinal));
        string gameAssemblyPath = resolved.ReferencePaths.Single(path =>
            Path.GetFullPath(path).Equals(
                PathSafety.ResolveUnderRoot(resolved.GameRoot, gameAssembly.GameRelativePath, "Advanced game reference"),
                StringComparison.OrdinalIgnoreCase));
        VerifyAssemblyIdentity(gameAssemblyPath, gameAssembly.AssemblyName, new Version(0, 0, 0, 0));

        string? harmonyPath = resolved.ReferencePaths.SingleOrDefault(path =>
            AssemblyName.GetAssemblyName(path).Name?.Equals("0Harmony", StringComparison.Ordinal) == true);
        var references = compatibility.ReferencePaths
            .Append(compatibility.AbstractionsPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList<MetadataReference>();
        if (harmonyPath != null)
            references.Add(MetadataReference.CreateFromFile(harmonyPath));
        references.Add(CreateAssemblyCSharpSurface(compatibility, resolved.Registration));
        return new AdvancedCompilationReferenceSet
        {
            References = references,
            SurfaceSha256 = resolved.Registration.CompilerSurfaceSha256.ToLowerInvariant()
        };
    }

    private static MetadataReference CreateAssemblyCSharpSurface(
        CompatibilityAssets compatibility,
        AdvancedReferencePolicyRegistration registration)
    {
        byte[] sourceBytes;
        string resourceName = SurfaceResourcePrefix + registration.PolicyId + SurfaceResourceSuffix;
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException("The SDK-embedded Advanced compiler reference surface is missing for policy " + registration.PolicyId + "."))
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            sourceBytes = memory.ToArray();
        }
        string actualSha256 = PathSafety.Sha256Bytes(sourceBytes);
        if (!actualSha256.Equals(registration.CompilerSurfaceSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The SDK-embedded Advanced compiler reference surface hash is invalid for policy " + registration.PolicyId + ".");

        const string assemblyInfo = "using System.Reflection;\n" +
            "using System.Runtime.CompilerServices;\n" +
            "[assembly: AssemblyVersion(\"0.0.0.0\")]\n" +
            "[assembly: AssemblyFileVersion(\"0.0.0.0\")]\n" +
            "[assembly: ReferenceAssembly]\n";
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithDocumentationMode(DocumentationMode.Parse);
        SyntaxTree[] trees =
        {
            CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(sourceBytes), parseOptions, "DolocTown.Advanced.ReferenceSurface.cs", Encoding.UTF8),
            CSharpSyntaxTree.ParseText(assemblyInfo, parseOptions, "DTMAPI.Author.GeneratedAdvancedReferenceAssemblyInfo.cs", Encoding.UTF8)
        };
        IEnumerable<MetadataReference> references = compatibility.ReferencePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            checkOverflow: true,
            allowUnsafe: false,
            platform: Platform.AnyCpu,
            warningLevel: 4,
            deterministic: true,
            nullableContextOptions: NullableContextOptions.Enable,
            concurrentBuild: false);
        CSharpCompilation compilation = CSharpCompilation.Create("Assembly-CSharp", trees, references, options);
        using var output = new MemoryStream();
        var emit = compilation.Emit(output);
        if (!emit.Success)
        {
            string errors = string.Join("; ", emit.Diagnostics
                .Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture)));
            throw new InvalidDataException("Failed to construct the hash-fixed Advanced compiler reference surface for policy " + registration.PolicyId + ": " + errors);
        }
        ImmutableArray<byte> image = ImmutableArray.Create(output.ToArray());
        VerifyReferenceSurface(image);
        return MetadataReference.CreateFromImage(image);
    }

    private static void VerifyReferenceSurface(ImmutableArray<byte> image)
    {
        using var stream = new MemoryStream(image.ToArray(), writable: false);
        using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
        MetadataReader reader = pe.GetMetadataReader();
        AssemblyDefinition definition = reader.GetAssemblyDefinition();
        if (!reader.GetString(definition.Name).Equals("Assembly-CSharp", StringComparison.Ordinal)
            || definition.Version != new Version(0, 0, 0, 0))
            throw new InvalidDataException("The Advanced compiler reference surface has an invalid assembly identity.");

        TypeDefinition[] publicTypes = reader.TypeDefinitions
            .Select(reader.GetTypeDefinition)
            .Where(type => (type.Attributes & System.Reflection.TypeAttributes.Public) != 0)
            .ToArray();
        if (publicTypes.Length != 1 || !reader.GetString(publicTypes[0].Name).Equals("DolocAPI", StringComparison.Ordinal))
            throw new InvalidDataException("The Advanced compiler reference surface must expose only DolocAPI.");
        if (publicTypes[0].GetMethods()
            .Select(reader.GetMethodDefinition)
            .Any(method => (method.Attributes & System.Reflection.MethodAttributes.Public) != 0 &&
                (method.Attributes & System.Reflection.MethodAttributes.Static) == 0))
            throw new InvalidDataException("The Advanced compiler reference surface may expose only registry-hash-bound static DolocAPI methods.");
    }

    private static void VerifyAssemblyIdentity(string path, string expectedName, Version expectedVersion)
    {
        AssemblyName identity = AssemblyName.GetAssemblyName(path);
        if (!string.Equals(identity.Name, expectedName, StringComparison.Ordinal) || identity.Version != expectedVersion)
            throw new InvalidDataException("Tracked Advanced game reference identity/version mismatch: " + expectedName + ".");
    }
}
