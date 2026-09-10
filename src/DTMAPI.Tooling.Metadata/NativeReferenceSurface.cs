using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DTMAPI.Tooling.Metadata;

public sealed record NativeSurfaceDependency(string Path, string AssemblyIdentity, long Length, string Sha256);
public sealed record NativeSurfaceResult(byte[] Bytes, IReadOnlyList<NativeSurfaceDependency> MetadataDependencies);

/// <summary>A local compiler view; never a redistributable or executable game replacement.</summary>
public static class NativeReferenceSurface
{
    public const string Generation = "metadata-surface-v1/cecil-0.11.6";

    public static byte[] Create(byte[] original, IEnumerable<string> resolverDirectories) => CreateDetailed(original, resolverDirectories).Bytes;

    public static NativeSurfaceResult CreateDetailed(byte[] original, IEnumerable<string> resolverDirectories)
    {
        using var resolver = new RecordingResolver();
        foreach (string directory in resolverDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
            resolver.AddSearchDirectory(directory);
        using var input = new MemoryStream(original, writable: false);
        using var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters
        { InMemory = true, ReadSymbols = false, AssemblyResolver = resolver });
        if (assembly.Modules.Count != 1) throw new InvalidDataException("native-surface-unsupported: multi-module assembly.");
        var module = assembly.MainModule;
        foreach (AssemblyNameReference reference in module.AssemblyReferences)
            if (reference.Name == "netstandard" && reference.Version > new Version(2, 0, 0, 0))
                reference.Version = new Version(2, 0, 0, 0);
        module.Resources.Clear();
        module.CustomDebugInformations.Clear();
        module.Attributes &= ~ModuleAttributes.StrongNameSigned;
        foreach (TypeDefinition type in Types(module.Types))
        {
            foreach (FieldDefinition field in type.Fields)
                if (field.InitialValue.Length != 0) field.InitialValue = Array.Empty<byte>();
            foreach (MethodDefinition method in type.Methods)
            {
                method.DebugInformation.SequencePoints.Clear();
                method.DebugInformation.Scope = null;
                method.CustomDebugInformations.Clear();
                if (!method.HasBody) continue;
                var body = new MethodBody(method);
                body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                method.Body = body;
            }
        }
        if (!assembly.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"))
        {
            var type = new TypeReference("System.Runtime.CompilerServices", "ReferenceAssemblyAttribute", module, module.TypeSystem.CoreLibrary);
            var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
            assembly.CustomAttributes.Add(new CustomAttribute(constructor));
        }
        using var output = new MemoryStream();
        assembly.Write(output, new WriterParameters { WriteSymbols = false, DeterministicMvid = true, Timestamp = 0 });
        byte[] bytes = output.ToArray();
        Verify(bytes);
        return new NativeSurfaceResult(bytes, resolver.Inputs.Values.OrderBy(input => input.Path, StringComparer.Ordinal).ToArray());
    }

    private sealed class RecordingResolver : DefaultAssemblyResolver
    {
        internal readonly Dictionary<string, NativeSurfaceDependency> Inputs = new(StringComparer.OrdinalIgnoreCase);
        public override AssemblyDefinition Resolve(AssemblyNameReference name) => Record(base.Resolve(name));
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Record(base.Resolve(name, parameters));
        private AssemblyDefinition Record(AssemblyDefinition assembly)
        {
            string path = Path.GetFullPath(assembly.MainModule.FileName);
            byte[] bytes = File.ReadAllBytes(path);
            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            using var stream = new MemoryStream(bytes, writable: false);
            using var observed = AssemblyDefinition.ReadAssembly(stream);
            if (observed.MainModule.Mvid != assembly.MainModule.Mvid || observed.Name.FullName != assembly.Name.FullName || (Inputs.TryGetValue(path, out var previous) && previous.Sha256 != hash))
                throw new InvalidDataException("native-metadata-input-changed: " + path);
            Inputs[path] = new NativeSurfaceDependency(path, assembly.Name.FullName, bytes.LongLength, hash);
            return assembly;
        }
    }

    public static void Verify(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var assembly = AssemblyDefinition.ReadAssembly(stream);
        if (assembly.MainModule.Resources.Count != 0 || !assembly.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"))
            throw new InvalidDataException("native-surface-invalid: reference marker/resources.");
        foreach (TypeDefinition type in Types(assembly.MainModule.Types))
        {
            if (type.Fields.Any(field => field.InitialValue.Length != 0)) throw new InvalidDataException("native-surface-invalid: field data.");
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
                if (method.Body.Instructions.Count != 2 || method.Body.Instructions[0].OpCode != OpCodes.Ldnull || method.Body.Instructions[1].OpCode != OpCodes.Throw)
                    throw new InvalidDataException("native-surface-invalid: original executable body.");
        }
    }

    internal static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (TypeDefinition type in roots)
        {
            yield return type;
            foreach (TypeDefinition nested in Types(type.NestedTypes)) yield return nested;
        }
    }
}
