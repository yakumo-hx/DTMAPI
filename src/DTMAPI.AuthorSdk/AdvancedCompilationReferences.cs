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

internal static class AdvancedCompilationReferences
{
    private const string SurfaceResourcePrefix = "DTMAPI.AuthorSdk.advanced-reference-surface.";
    private const string SurfaceResourceSuffix = ".Assembly-CSharp.reference.cs.txt";
    private const string MoreEquipmentPolicyId = "doloctown-24456188-moreequipmentslots-v1";
    private const string MoreEquipmentUniqueId = "DTMAPI.MoreEquipmentSlotsMod";
    private const string MoreEquipmentSurfaceSha256 = "70D65F0BCB013C16E7232B91D3F232E2663072D6C8C661550519F1E8A806AEB0";

    private static MetadataReference CreateAssemblyCSharpSurface(
        CompatibilityAssets compatibility,
        AdvancedReferencePolicyRegistration registration, out string imageSha256, string? destination = null)
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
        imageSha256 = PathSafety.Sha256Bytes(image.AsSpan());
        VerifyReferenceSurface(image, registration);
        if (destination != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination) && PathSafety.Sha256File(destination) != imageSha256)
                throw new InvalidDataException("Advanced reference cache changed: " + destination);
            if (!File.Exists(destination)) File.WriteAllBytes(destination, image.ToArray());
        }
        return MetadataReference.CreateFromImage(image);
    }

    internal static string[] PreparePaths(CompatibilityAssets compatibility, ResolvedAdvancedReferenceSet resolved, string root)
    {
        string destination = Path.Combine(root, "obj", "dtmapi-advanced", resolved.Registration.CompilerSurfaceSha256, "Assembly-CSharp.dll");
        CreateAssemblyCSharpSurface(compatibility, resolved.Registration, out _, destination);
        return compatibility.ReferencePaths.Append(compatibility.AbstractionsPath).Append(destination)
            .Concat(resolved.ReferencePaths.Where(path => AssemblyName.GetAssemblyName(path).Name == "0Harmony")).ToArray();
    }

    private static void VerifyReferenceSurface(
        ImmutableArray<byte> image,
        AdvancedReferencePolicyRegistration registration)
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
            .Where(IsPublicSurfaceType)
            .ToArray();

        TypeDefinition? dolocApi = publicTypes.SingleOrDefault(type =>
            reader.GetString(type.Namespace).Length == 0 &&
            reader.GetString(type.Name).Equals("DolocAPI", StringComparison.Ordinal));
        if (dolocApi == null)
            throw new InvalidDataException("The Advanced compiler reference surface must expose DolocAPI.");

        if (IsMoreEquipmentShieldSurface(registration))
        {
            VerifyMoreEquipmentShieldSurface(reader, publicTypes, dolocApi.Value);
            return;
        }

        if (publicTypes.Length != 1)
            throw new InvalidDataException("The Advanced compiler reference surface must expose only DolocAPI.");
        if (dolocApi.Value.GetMethods()
            .Select(reader.GetMethodDefinition)
            .Any(method => (method.Attributes & System.Reflection.MethodAttributes.Public) != 0 &&
                (method.Attributes & System.Reflection.MethodAttributes.Static) == 0))
            throw new InvalidDataException("The Advanced compiler reference surface may expose only registry-hash-bound static DolocAPI methods.");
    }

    private static bool IsPublicSurfaceType(TypeDefinition type)
    {
        System.Reflection.TypeAttributes visibility = type.Attributes & System.Reflection.TypeAttributes.VisibilityMask;
        return visibility is System.Reflection.TypeAttributes.Public or System.Reflection.TypeAttributes.NestedPublic;
    }

    private static bool IsMoreEquipmentShieldSurface(AdvancedReferencePolicyRegistration registration) =>
        registration.PolicyId.Equals(MoreEquipmentPolicyId, StringComparison.Ordinal) &&
        registration.RequiredUniqueId.Equals(MoreEquipmentUniqueId, StringComparison.Ordinal) &&
        registration.CompilerSurfaceSha256.Equals(MoreEquipmentSurfaceSha256, StringComparison.OrdinalIgnoreCase);

    private static void VerifyMoreEquipmentShieldSurface(
        MetadataReader reader,
        IReadOnlyList<TypeDefinition> publicTypes,
        TypeDefinition dolocApi)
    {
        if (publicTypes.Count != 2 ||
            dolocApi.GetMethods().Count != 0 ||
            dolocApi.GetFields().Count != 0 ||
            dolocApi.GetProperties().Count != 0 ||
            dolocApi.GetEvents().Count != 0 ||
            dolocApi.GetNestedTypes().Count() != 0)
        {
            throw new InvalidDataException(
                "The MoreEquipmentSlots compiler reference surface must expose only an empty DolocAPI and the admitted native shield interface.");
        }

        TypeDefinition shield = publicTypes.SingleOrDefault(type =>
            reader.GetString(type.Namespace).Equals("DolocTown", StringComparison.Ordinal) &&
            reader.GetString(type.Name).Equals("IAgentEquipmentShieldItem", StringComparison.Ordinal));
        if (shield.Equals(default(TypeDefinition)) ||
            (shield.Attributes & System.Reflection.TypeAttributes.Interface) == 0 ||
            shield.GetFields().Count != 0 ||
            shield.GetEvents().Count != 0 ||
            shield.GetNestedTypes().Count() != 0)
        {
            throw new InvalidDataException(
                "The MoreEquipmentSlots compiler reference surface has an invalid native shield interface.");
        }

        Dictionary<string, byte[]> expectedMethods = new(StringComparer.Ordinal)
        {
            ["get_ShieldPercent"] = new byte[] { 0x20, 0x00, 0x0c },
            ["get_ShieldValue"] = new byte[] { 0x20, 0x00, 0x0c },
            ["TryBlockAttack"] = new byte[] { 0x20, 0x02, 0x02, 0x08, 0x10, 0x08 }
        };
        MethodDefinition[] methods = shield.GetMethods().Select(reader.GetMethodDefinition).ToArray();
        if (methods.Length != expectedMethods.Count || methods.Any(method =>
                !expectedMethods.TryGetValue(reader.GetString(method.Name), out byte[]? signature) ||
                !reader.GetBlobBytes(method.Signature).SequenceEqual(signature) ||
                (method.Attributes & System.Reflection.MethodAttributes.Public) == 0 ||
                (method.Attributes & System.Reflection.MethodAttributes.Abstract) == 0 ||
                (method.Attributes & System.Reflection.MethodAttributes.Static) != 0))
        {
            throw new InvalidDataException(
                "The MoreEquipmentSlots compiler reference surface native shield methods do not match the admitted contract.");
        }

        Dictionary<string, byte[]> expectedProperties = new(StringComparer.Ordinal)
        {
            ["ShieldPercent"] = new byte[] { 0x28, 0x00, 0x0c },
            ["ShieldValue"] = new byte[] { 0x28, 0x00, 0x0c }
        };
        PropertyDefinition[] properties = shield.GetProperties().Select(reader.GetPropertyDefinition).ToArray();
        if (properties.Length != expectedProperties.Count || properties.Any(property =>
                !expectedProperties.TryGetValue(reader.GetString(property.Name), out byte[]? signature) ||
                !reader.GetBlobBytes(property.Signature).SequenceEqual(signature)))
        {
            throw new InvalidDataException(
                "The MoreEquipmentSlots compiler reference surface native shield properties do not match the admitted contract.");
        }
    }

    private static void VerifyAssemblyIdentity(string path, string expectedName, Version expectedVersion)
    {
        AssemblyName identity = AssemblyName.GetAssemblyName(path);
        if (!string.Equals(identity.Name, expectedName, StringComparison.Ordinal) || identity.Version != expectedVersion)
            throw new InvalidDataException("Tracked Advanced game reference identity/version mismatch: " + expectedName + ".");
    }
}
