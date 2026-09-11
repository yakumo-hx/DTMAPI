using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;

namespace DTMAPI.AuthorSdk;

internal static class ManagedRuntimeSurface
{
    // Resolve actual TypeRef/MemberRef uses against the selected implementations.
    // Cecil reads metadata and follows generic signatures, base types and forwarders;
    // it never loads or invokes the package's executable code.
    internal static void Verify(IEnumerable<string> runtimePaths, IEnumerable<string> hostPaths)
    {
        using var resolver = new RuntimeResolver();
        foreach (string path in hostPaths) resolver.Add(path, runtime: false);
        var runtimes = runtimePaths.Select(path =>
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes, false));
            if (!pe.HasMetadata || pe.PEHeaders.CorHeader == null || (pe.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) == 0)
                throw new InvalidDataException("runtime-native-assembly: " + Path.GetFileName(path));
            try { DTMAPI.Tooling.Metadata.ManagedMethodMetadata.RequireManagedMethods(bytes, Path.GetFileName(path)); }
            catch (InvalidDataException ex) { throw new InvalidDataException(ex.Message.Replace("restore-native-method:", "runtime-native-method:", StringComparison.Ordinal), ex); }
            return resolver.Add(path, runtime: true);
        }).ToArray();
        foreach (var assembly in runtimes)
        {
            if (assembly.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"))
                throw new InvalidDataException("runtime-reference-assembly: " + assembly.Name.FullName);
            foreach (var type in assembly.MainModule.GetTypeReferences())
                if (resolver.IsRuntime(type))
                {
                    var definition = type.Resolve();
                    if (definition == null) throw new InvalidDataException("runtime-type-missing: " + assembly.Name.Name + " -> " + type.FullName);
                    for (var declaring = definition; declaring != null; declaring = declaring.DeclaringType)
                        if ((declaring.IsNestedPrivate && declaring.Module.Assembly != assembly) ||
                            ((!declaring.IsNested && !declaring.IsPublic || declaring.IsNestedAssembly || declaring.IsNestedFamilyAndAssembly) && !HasAssemblyAccess(declaring.Module.Assembly, assembly)))
                            throw new InvalidDataException("runtime-type-inaccessible: " + assembly.Name.Name + " -> " + type.FullName);
                }
            foreach (var member in assembly.MainModule.GetMemberReferences())
            {
                if (!resolver.IsRuntime(member.DeclaringType)) continue;
                IMemberDefinition? resolved = member switch { MethodReference method => method.Resolve(), FieldReference field => field.Resolve(), _ => null };
                if (resolved == null) throw new InvalidDataException("runtime-member-missing: " + assembly.Name.Name + " -> " + member.FullName);
                bool privateMember = resolved is MethodDefinition pm ? pm.IsPrivate : ((FieldDefinition)resolved).IsPrivate;
                bool assemblyMember = resolved is MethodDefinition am ? am.IsAssembly || am.IsFamilyAndAssembly : ((FieldDefinition)resolved).IsAssembly || ((FieldDefinition)resolved).IsFamilyAndAssembly;
                if (privateMember && resolved.DeclaringType.Module.Assembly != assembly || assemblyMember && !HasAssemblyAccess(resolved.DeclaringType.Module.Assembly, assembly))
                    throw new InvalidDataException("runtime-member-inaccessible: " + assembly.Name.Name + " -> " + member.FullName);
                if (resolved is MethodDefinition methodDefinition && methodDefinition.IsStatic == ((MethodReference)member).HasThis)
                    throw new InvalidDataException("runtime-member-static-mismatch: " + member.FullName);
            }
            foreach (var type in assembly.MainModule.Types) VerifyFieldUses(type, resolver);
        }
    }

    private static void VerifyFieldUses(TypeDefinition type, RuntimeResolver resolver)
    {
        foreach (var method in type.Methods.Where(method => method.HasBody))
            foreach (var instruction in method.Body.Instructions)
            {
                bool? requiresStatic = instruction.OpCode.Code switch
                {
                    Code.Ldsfld or Code.Stsfld or Code.Ldsflda => true,
                    Code.Ldfld or Code.Stfld or Code.Ldflda => false,
                    _ => null // ldtoken does not require a particular field storage kind.
                };
                if (requiresStatic == null || instruction.Operand is not FieldReference field || !resolver.IsRuntime(field.DeclaringType)) continue;
                var definition = field.Resolve();
                if (definition == null)
                    throw new InvalidDataException("runtime-member-missing: " + method.FullName + " -> " + field.FullName);
                if (definition.IsStatic != requiresStatic.Value)
                    throw new InvalidDataException("runtime-field-static-mismatch: " + type.Module.Assembly.Name.Name + " / " + method.FullName
                        + " -> " + field.FullName + "; " + instruction.OpCode.Name + " requires " + (requiresStatic.Value ? "static" : "instance")
                        + ", selected implementation is " + (definition.IsStatic ? "static" : "instance"));
            }
        foreach (var nested in type.NestedTypes) VerifyFieldUses(nested, resolver);
    }

    private static bool HasAssemblyAccess(AssemblyDefinition provider, AssemblyDefinition consumer)
    {
        if (provider == consumer) return true;
        foreach (var attribute in provider.CustomAttributes.Where(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute"))
        {
            if (attribute.ConstructorArguments.Count != 1 || attribute.ConstructorArguments[0].Value is not string value) continue;
            System.Reflection.AssemblyName friend;
            try { friend = new System.Reflection.AssemblyName(value); }
            catch (ArgumentException) { continue; }
            byte[] key = friend.GetPublicKey() ?? Array.Empty<byte>();
            if (friend.Name == consumer.Name.Name && (key.Length == 0 ? !provider.Name.HasPublicKey : key.SequenceEqual(consumer.Name.PublicKey))) return true;
        }
        return false;
    }

    private sealed class RuntimeResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> assemblies = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> runtime = new(StringComparer.OrdinalIgnoreCase);

        internal AssemblyDefinition Add(string path, bool runtime)
        {
            var assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(path), false), new ReaderParameters { AssemblyResolver = this, InMemory = true });
            if (!assemblies.TryAdd(assembly.Name.Name, assembly))
            { assembly.Dispose(); throw new InvalidDataException("runtime-surface-identity-collision: " + path); }
            if (runtime) this.runtime.Add(assembly.Name.Name);
            return assembly;
        }

        internal bool IsRuntime(TypeReference type)
        {
            while (type is TypeSpecification specification) type = specification.ElementType;
            while (type.DeclaringType != null) type = type.DeclaringType;
            return type.Scope is AssemblyNameReference reference ? runtime.Contains(reference.Name)
                : type.Scope is ModuleDefinition module && runtime.Contains(module.Assembly.Name.Name);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name) => Resolve(name, new ReaderParameters());
        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (!assemblies.TryGetValue(name.Name, out var assembly) || runtime.Contains(name.Name) && assembly.Name.FullName != name.FullName)
                throw new InvalidDataException("runtime-surface-reference-missing: " + name.FullName);
            return assembly;
        }
        public void Dispose() { foreach (var assembly in assemblies.Values) assembly.Dispose(); }
    }
}
