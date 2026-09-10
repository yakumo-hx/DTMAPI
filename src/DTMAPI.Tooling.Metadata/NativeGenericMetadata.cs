using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;
using DTMAPI.Internal.Authoring;
using static DTMAPI.Internal.Authoring.NativeGenericSignature;

namespace DTMAPI.Tooling.Metadata;

/// <summary>Full native V2 definition/use extraction from disk PE; no target code loads.</summary>
public static class NativeGenericMetadata
{
    public static IReadOnlyList<byte[]> Extract(byte[] authorBytes, IReadOnlyList<string> hostFiles, IEnumerable<string> resolverDirectories, byte[]? pdb = null)
    {
        using var resolver = new DefaultAssemblyResolver();
        foreach (var directory in resolverDirectories.Concat(hostFiles.Select(Path.GetDirectoryName)).Distinct()) resolver.AddSearchDirectory(directory);
        var hosts = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
        try
        {
            foreach (string file in hostFiles)
            {
                var host = AssemblyDefinition.ReadAssembly(file, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
                if (!hosts.TryAdd(host.Name.FullName, host)) { host.Dispose(); throw new InvalidDataException("native-reference-duplicate: " + file); }
            }
            using var bytes = new MemoryStream(authorBytes, false);
            using var symbols = pdb == null ? null : new MemoryStream(pdb, false);
            using var author = AssemblyDefinition.ReadAssembly(bytes, new ReaderParameters { AssemblyResolver = resolver, InMemory = true,
                ReadSymbols = symbols != null, SymbolStream = symbols, SymbolReaderProvider = symbols == null ? null : new Mono.Cecil.Cil.PortablePdbReaderProvider() });
            var uses = new SortedDictionary<string, XElement>(StringComparer.Ordinal);
            void Add(XElement definition, IEnumerable<XElement> ta, IEnumerable<XElement> ma, int ct, int cm)
            { var use = Use(definition, ta, ma, ct, cm); Validate(use); uses[Key(use)] = use; }
            void TypeUse(TypeReference reference, int ct, int cm)
            {
                _ = TypeNode(reference); // Reject unsupported syntax even when nested around a host reference.
                if (reference is GenericParameter) return;
                if (reference is GenericInstanceType constructed)
                    foreach (var argument in constructed.GenericArguments) TypeUse(argument, ct, cm);
                else if (reference is TypeSpecification specification) { TypeUse(specification.ElementType, ct, cm); return; }
                if (!hosts.TryGetValue(Scope(reference), out var host)) return;
                var definition = FindType(host, reference.GetElementType().FullName);
                Add(Describe(definition), reference is GenericInstanceType instance ? instance.GenericArguments.Select(TypeNode) : Array.Empty<XElement>(), Array.Empty<XElement>(), ct, cm);
            }
            void MemberUse(MemberReference reference, int ct, int cm)
            {
                TypeUse(reference.DeclaringType, ct, cm);
                if (reference is GenericInstanceMethod spec)
                    foreach (var argument in spec.GenericArguments) TypeUse(argument, ct, cm);
                var method = reference is GenericInstanceMethod generic ? generic.ElementMethod : reference as MethodReference;
                if (method != null)
                {
                    if (method.CallingConvention == MethodCallingConvention.VarArg || method.ExplicitThis) throw NativeSignature.Unsupported(method.FullName);
                    TypeUse(method.ReturnType, Math.Max(ct, method.DeclaringType.GenericParameters.Count), Math.Max(cm, method.GenericParameters.Count));
                    foreach (var parameter in method.Parameters) TypeUse(parameter.ParameterType, Math.Max(ct, method.DeclaringType.GenericParameters.Count), Math.Max(cm, method.GenericParameters.Count));
                }
                else if (reference is FieldReference fieldRef) TypeUse(fieldRef.FieldType, ct, cm);
                if (!hosts.TryGetValue(Scope(reference.DeclaringType), out var host)) return;
                var type = FindType(host, reference.DeclaringType.GetElementType().FullName);
                XElement definition;
                if (method != null)
                {
                    string returns = Key(TypeNode(method.ReturnType)); string[] parameters = method.Parameters.Select(p => Key(TypeNode(p.ParameterType))).ToArray();
                    var matches = type.Methods.Where(m => m.Name == method.Name && m.IsStatic == !method.HasThis && m.GenericParameters.Count == method.GenericParameters.Count
                        && Safe(() => Key(TypeNode(m.ReturnType)) == returns && m.Parameters.Select(p => Key(TypeNode(p.ParameterType))).SequenceEqual(parameters))).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("native-member-unresolved: " + method.FullName + "; requested return " + returns
                        + "; host candidates " + string.Join("; ", type.Methods.Where(m => m.Name == method.Name).Select(m => m.FullName + " return " + Key(TypeNode(m.ReturnType)))));
                    definition = Describe(matches[0]);
                }
                else if (reference is FieldReference field)
                {
                    var matches = type.Fields.Where(f => f.Name == field.Name && Safe(() => Key(TypeNode(f.FieldType)) == Key(TypeNode(field.FieldType)))).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("native-member-unresolved: " + field.FullName);
                    definition = Describe(matches[0]);
                }
                else throw NativeSignature.Unsupported(reference.FullName);
                Add(definition, reference.DeclaringType is GenericInstanceType declaring ? declaring.GenericArguments.Select(TypeNode) : Array.Empty<XElement>(),
                    reference is GenericInstanceMethod called ? called.GenericArguments.Select(TypeNode) : Array.Empty<XElement>(), ct, cm);
            }
            foreach (var type in NativeReferenceSurface.Types(author.MainModule.Types))
            {
                int ct = type.GenericParameters.Count;
                if (type.BaseType != null) TypeUse(type.BaseType, ct, 0);
                foreach (var contract in type.Interfaces) TypeUse(contract.InterfaceType, ct, 0);
                foreach (var parameter in type.GenericParameters) foreach (var constraint in parameter.Constraints) TypeUse(constraint.ConstraintType, ct, 0);
                foreach (var field in type.Fields) TypeUse(field.FieldType, ct, 0);
                foreach (var method in type.Methods)
                {
                    int cm = method.GenericParameters.Count;
                    try
                    {
                        TypeUse(method.ReturnType, ct, cm);
                        foreach (var parameter in method.Parameters) TypeUse(parameter.ParameterType, ct, cm);
                        foreach (var parameter in method.GenericParameters) foreach (var constraint in parameter.Constraints) TypeUse(constraint.ConstraintType, ct, cm);
                        foreach (var overridden in method.Overrides) MemberUse(overridden, ct, cm);
                        if (!method.HasBody) continue;
                        foreach (var variable in method.Body.Variables) TypeUse(variable.VariableType, ct, cm);
                        foreach (var handler in method.Body.ExceptionHandlers) if (handler.CatchType != null) TypeUse(handler.CatchType, ct, cm);
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is TypeReference usedType) TypeUse(usedType, ct, cm);
                            else if (instruction.Operand is MemberReference member) MemberUse(member, ct, cm);
                            else if (instruction.Operand is CallSite) throw NativeSignature.Unsupported("function pointer/calli in " + method.FullName);
                        }
                    }
                    catch (InvalidDataException ex)
                    {
                        var point = method.DebugInformation.SequencePoints.FirstOrDefault(p => !p.IsHidden);
                        throw new InvalidDataException(ex.Message + "; author " + author.Name.Name + ".dll / " + method.FullName
                            + (point == null ? "; member only (no PDB source line)" : "; " + point.Document.Url + ":" + point.StartLine), ex);
                    }
                }
            }
            // Include metadata references not surfaced by an instruction (attributes, signatures,
            // ldtoken and open definitions). Actual MethodSpec uses above retain their caller context.
            foreach (var reference in author.MainModule.GetTypeReferences()) TypeUse(reference, 64, 64);
            foreach (var reference in author.MainModule.GetMemberReferences()) MemberUse(reference, 64, 64);
            return uses.Values.Select(Write).ToArray();
        }
        finally { foreach (var host in hosts.Values) host.Dispose(); }
    }

    public static bool Contains(byte[] hostBytes, byte[] required)
    {
        var use = Read(required); Validate(use); var definition = Get(use, "definition"); var declaring = Get(definition, "declaringType");
        using var bytes = new MemoryStream(hostBytes, false); using var host = AssemblyDefinition.ReadAssembly(bytes);
        if (host.Name.FullName != Text(Get(declaring, "assemblyIdentity"))) return false;
        var type = NativeReferenceSurface.Types(host.MainModule.Types).SingleOrDefault(t => t.FullName.Replace('/', '+') == Text(Get(declaring, "name")));
        if (type == null) return false; string key = Key(definition);
        return Text(Get(definition, "kind")) switch
        {
            "type" => Safe(() => Key(Describe(type)) == key),
            "field" => type.Fields.Any(f => Safe(() => Key(Describe(f)) == key)),
            "method" => type.Methods.Any(m => Safe(() => Key(Describe(m)) == key)),
            _ => false
        };
    }
    public static byte[] Promote(byte[] hostBytes, NativeMemberDescription required)
    {
        if (!NativeMemberMetadata.Contains(hostBytes, required)) throw new InvalidDataException("native-required-member-missing: " + required.Key);
        using var bytes = new MemoryStream(hostBytes, false); using var host = AssemblyDefinition.ReadAssembly(bytes);
        var type = FindType(host, required.DeclaringType.Replace('+', '/'));
        string Legacy(TypeReference t)
        {
            var node = TypeNode(t); string kind = Text(Get(node, "kind"));
            if (kind == "named") return "[" + Text(Get(node, "assemblyIdentity")) + "]" + Text(Get(node, "name"));
            if (t is ByReferenceType br) return Legacy(br.ElementType) + "&";
            if (t is PointerType p) return Legacy(p.ElementType) + "*";
            if (t is ArrayType a) return Legacy(a.ElementType) + "[" + new string(',', a.Rank - 1) + "]";
            throw NativeSignature.Unsupported(t.FullName);
        }
        XElement definition = required.Kind == "type" ? Describe(type) : required.Kind == "field" ? Describe(type.Fields.Single(f => f.Name == required.Name))
            : Describe(type.Methods.Single(m => m.Name == required.Name && m.IsStatic == required.IsStatic && !m.HasGenericParameters
                && Safe(() => Legacy(m.ReturnType) == required.ReturnType && m.Parameters.Select(p => Legacy(p.ParameterType)).SequenceEqual(required.ParameterTypes))));
        return Write(Use(definition, Array.Empty<XElement>(), Array.Empty<XElement>(), 0, 0));
    }
    private static bool Safe(Func<bool> test) { try { return test(); } catch (InvalidDataException) { return false; } }
    private static TypeDefinition FindType(AssemblyDefinition host, string name) => NativeReferenceSurface.Types(host.MainModule.Types).SingleOrDefault(t => t.FullName == name) ?? throw new InvalidDataException("native-type-unresolved: " + host.Name.FullName + "/" + name);
    private static string Scope(TypeReference type)
    {
        type = type.GetElementType(); while (type.DeclaringType != null) type = type.DeclaringType;
        return type.Scope is AssemblyNameReference reference ? reference.FullName : type.Module.Assembly.Name.FullName;
    }
    private static int Arity(TypeReference type) => type.FullName.Split('/', '+').Sum(part => { int tick = part.LastIndexOf('`'); return tick < 0 ? 0 : int.Parse(part.Substring(tick + 1), System.Globalization.CultureInfo.InvariantCulture); });
    private static XElement TypeNode(TypeReference type)
    {
        if (type is GenericParameter parameter) return Parameter(parameter.Type == GenericParameterType.Method, parameter.Position);
        if (type is GenericInstanceType constructed) return Construct(TypeNode(constructed.ElementType), constructed.GenericArguments.Select(TypeNode));
        if (type is ByReferenceType byref) return Wrap("byRef", TypeNode(byref.ElementType));
        if (type is PointerType pointer) return Wrap("pointer", TypeNode(pointer.ElementType));
        if (type is ArrayType array)
        {
            if (!array.IsVector && (array.Rank == 1 || array.Dimensions.Any(d => d.UpperBound.HasValue || (d.LowerBound.HasValue && d.LowerBound.Value != 0)))) throw NativeSignature.Unsupported(type.FullName);
            return ArrayType(TypeNode(array.ElementType), array.Rank, array.IsVector);
        }
        if (type is TypeSpecification) throw NativeSignature.Unsupported(type.FullName);
        return Named(type.FullName, Scope(type), Arity(type));
    }
    private static XElement Constraints(GenericParameter parameter) => Constraint((int)parameter.Attributes, parameter.Constraints.Select(c => TypeNode(c.ConstraintType)));
    private static XElement Describe(TypeDefinition type) => Definition(TypeNode(type), "type", "", false, type.GenericParameters.Select(Constraints), Array.Empty<XElement>(), Named("System.Void", "bcl"), Array.Empty<XElement>());
    private static XElement Describe(FieldDefinition field) => Definition(TypeNode(field.DeclaringType), "field", field.Name, field.IsStatic, field.DeclaringType.GenericParameters.Select(Constraints), Array.Empty<XElement>(), TypeNode(field.FieldType), Array.Empty<XElement>());
    private static XElement Describe(MethodDefinition method)
    {
        if (method.CallingConvention == MethodCallingConvention.VarArg || method.ExplicitThis) throw NativeSignature.Unsupported(method.FullName);
        return Definition(TypeNode(method.DeclaringType), "method", method.Name, method.IsStatic, method.DeclaringType.GenericParameters.Select(Constraints), method.GenericParameters.Select(Constraints), TypeNode(method.ReturnType), method.Parameters.Select(p => TypeNode(p.ParameterType)));
    }
}
