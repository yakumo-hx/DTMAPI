using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DTMAPI.Internal.Authoring
{
    // V2 uses a bounded JSON tree. Shared between disk metadata tooling and resident-host
    // reflection; it never constructs closed CLR types or loads an author assembly.
    internal static class NativeGenericSignature
    {
        internal static XElement S(string value) => new XElement("item", new XAttribute("type", "string"), value);
        internal static XElement N(int value) => new XElement("item", new XAttribute("type", "number"), value);
        internal static XElement B(bool value) => new XElement("item", new XAttribute("type", "boolean"), value ? "true" : "false");
        internal static XElement A(IEnumerable<XElement> values) => new XElement("item", new XAttribute("type", "array"), values.Select(v => new XElement(v) { Name = "item" }));
        internal static XElement O(params (string Name, XElement Value)[] fields) => new XElement("item", new XAttribute("type", "object"), fields.Select(f => new XElement(f.Value) { Name = f.Name }));
        internal static XElement Get(XElement row, string name) => row.Element(name) ?? throw Bad("missing field " + name);
        internal static string Text(XElement value) { RequireType(value, "string"); if (value.Value.Length > 2048 || value.Value.Any(c => char.IsControl(c))) throw Bad("text bound"); return value.Value; }
        internal static int Int(XElement value) { RequireType(value, "number"); if (!int.TryParse(value.Value, out int result) || result < 0 || result > 4096) throw Bad("number bound"); return result; }
        internal static bool Flag(XElement value) { RequireType(value, "boolean"); if (value.Value != "true" && value.Value != "false") throw Bad("boolean"); return value.Value == "true"; }
        internal static XElement[] Items(XElement value) { RequireType(value, "array"); var rows = value.Elements().ToArray(); if (rows.Length > 4096) throw Bad("array bound"); return rows; }
        internal static void Fields(XElement value, params string[] fields)
        {
            RequireType(value, "object"); var names = value.Elements().Select(e => e.Name.LocalName).ToArray();
            if (names.Length != fields.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length || names.Except(fields, StringComparer.Ordinal).Any()) throw Bad("duplicate, unknown or missing fields");
        }
        private static void RequireType(XElement value, string type) { if ((string?)value.Attribute("type") != type) throw Bad("expected " + type); }
        internal static InvalidDataException Bad(string reason) => new InvalidDataException("native-v2-signature-invalid: " + reason);
        internal static XElement Read(byte[] bytes)
        {
            if (bytes.Length > 1024 * 1024) throw Bad("document bound");
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, new XmlDictionaryReaderQuotas { MaxDepth = 64, MaxStringContentLength = 1048576, MaxArrayLength = 1048576 }))
                return XElement.Load(reader);
        }
        internal static byte[] Write(XElement element)
        {
            using (var output = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(output, Encoding.UTF8, false))
                { new XElement(element) { Name = "root" }.WriteTo(writer); writer.Flush(); }
                return output.ToArray();
            }
        }
        internal static string Key(XElement element)
        {
            XElement Canon(XElement node)
            {
                var copy = new XElement(node);
                if ((string?)node.Attribute("type") == "object") { copy.RemoveNodes(); copy.Add(node.Elements().OrderBy(e => e.Name.LocalName, StringComparer.Ordinal).Select(Canon)); }
                else if (node.HasElements) { copy.RemoveNodes(); copy.Add(node.Elements().Select(Canon)); }
                return copy;
            }
            return Encoding.UTF8.GetString(Write(Canon(element)));
        }

        // Exact verified framework identities only. The reference inventory is shared
        // with the package closure; Mono's mscorlib is one of those exact identities.
        private static readonly Lazy<HashSet<string>> Frameworks = new Lazy<HashSet<string>>(() =>
        {
            using (var stream = typeof(NativeGenericSignature).Assembly.GetManifestResourceStream("DTMAPI.Authoring.bcl-reference-identities.json") ?? throw Bad("BCL inventory missing"))
            using (var bytes = new MemoryStream())
            {
                stream.CopyTo(bytes);
                var identities = new HashSet<string>(Items(Read(bytes.ToArray())).Select(row =>
                    Text(Get(row, "name")) + ", Version=" + Text(Get(row, "assemblyVersion")) + ", Culture=" + (Text(Get(row, "culture")) == "" ? "neutral" : Text(Get(row, "culture")))
                    + ", PublicKeyToken=" + (Text(Get(row, "publicKeyToken")) == "" ? "null" : Text(Get(row, "publicKeyToken")))), StringComparer.Ordinal);
                identities.Add("System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
                // Unity CoreModule in build 25163613 encodes its BCL signatures against
                // mscorlib 2.0; the supported Mono host resolves them through mscorlib 4.0.
                identities.Add("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                return identities;
            }
        });
        internal static XElement Named(string name, string assembly, int arity = 0)
            => O(("kind", S("named")), ("name", S(name.Replace('/', '+'))), ("assemblyIdentity", S(Frameworks.Value.Contains(assembly) ? "bcl" : assembly)), ("arity", N(arity)));
        internal static XElement Parameter(bool method, int position) => O(("kind", S(method ? "methodParameter" : "typeParameter")), ("position", N(position)));
        internal static XElement Wrap(string kind, XElement element) => O(("kind", S(kind)), ("element", element));
        internal static XElement Construct(XElement definition, IEnumerable<XElement> arguments) => O(("kind", S("constructed")), ("definition", definition), ("arguments", A(arguments)));
        internal static XElement ArrayType(XElement element, int rank, bool vector) => O(("kind", S("array")), ("element", element), ("rank", N(rank)), ("vector", B(vector)));
        internal static XElement Constraint(int flags, IEnumerable<XElement> types) => O(("flags", N(flags)), ("types", A(types.OrderBy(Key, StringComparer.Ordinal))));
        internal static XElement Definition(XElement declaring, string kind, string name, bool isStatic, IEnumerable<XElement> typeParameters, IEnumerable<XElement> methodParameters, XElement result, IEnumerable<XElement> parameters)
            => O(("declaringType", declaring), ("kind", S(kind)), ("name", S(name)), ("isStatic", B(isStatic)), ("callingConvention", S("default")),
                ("typeParameters", A(typeParameters)), ("methodParameters", A(methodParameters)), ("returnType", result), ("parameterTypes", A(parameters)));
        internal static XElement Use(XElement definition, IEnumerable<XElement> typeArguments, IEnumerable<XElement> methodArguments, int typeArity, int methodArity)
            => O(("definition", definition), ("typeArguments", A(typeArguments)), ("methodArguments", A(methodArguments)), ("contextTypeArity", N(typeArity)), ("contextMethodArity", N(methodArity)));

        internal static void Validate(XElement use)
        {
            Fields(use, "definition", "typeArguments", "methodArguments", "contextTypeArity", "contextMethodArity");
            var def = Get(use, "definition");
            Fields(def, "declaringType", "kind", "name", "isStatic", "callingConvention", "typeParameters", "methodParameters", "returnType", "parameterTypes");
            var declaring = Get(def, "declaringType");
            var tp = Items(Get(def, "typeParameters")); var mp = Items(Get(def, "methodParameters"));
            if (tp.Length > 64 || mp.Length > 64 || Text(Get(declaring, "kind")) != "named" || Int(Get(declaring, "arity")) != tp.Length) throw Bad("declaring arity");
            CheckType(declaring, tp.Length, mp.Length, 0);
            string kind = Text(Get(def, "kind")), name = Text(Get(def, "name")); bool isStatic = Flag(Get(def, "isStatic"));
            if (Text(Get(def, "callingConvention")) != "default" || (kind != "type" && kind != "method" && kind != "field")) throw Bad("member kind/convention");
            if (kind == "type" ? name != "" || isStatic : name.Length == 0) throw Bad("member name/staticness");
            if (kind != "method" && (mp.Length != 0 || Items(Get(def, "parameterTypes")).Length != 0)) throw Bad("non-method parameters");
            foreach (var constraint in tp.Concat(mp))
            {
                Fields(constraint, "flags", "types"); int flags = Int(Get(constraint, "flags"));
                if ((flags & ~31) != 0 || (flags & 3) == 3 || (flags & 12) == 12) throw Bad("generic constraint flags");
                var types = Items(Get(constraint, "types"));
                if (types.Select(Key).Distinct().Count() != types.Length || !types.Select(Key).SequenceEqual(types.Select(Key).OrderBy(k => k, StringComparer.Ordinal))) throw Bad("duplicate/unsorted constraints");
                foreach (var type in types) CheckType(type, tp.Length, mp.Length, 0);
            }
            CheckType(Get(def, "returnType"), tp.Length, mp.Length, 0);
            foreach (var parameter in Items(Get(def, "parameterTypes"))) CheckType(parameter, tp.Length, mp.Length, 0);
            var ta = Items(Get(use, "typeArguments")); var ma = Items(Get(use, "methodArguments"));
            if ((ta.Length != 0 && ta.Length != tp.Length) || (ma.Length != 0 && ma.Length != mp.Length)) throw Bad("use arity");
            int ct = Int(Get(use, "contextTypeArity")), cm = Int(Get(use, "contextMethodArity"));
            if (ct > 64 || cm > 64) throw Bad("use context bound");
            foreach (var argument in ta.Concat(ma)) CheckType(argument, ct, cm, 0);
        }
        private static void CheckType(XElement node, int typeArity, int methodArity, int depth)
        {
            if (depth > 24) throw Bad("type depth");
            string kind = Text(Get(node, "kind"));
            switch (kind)
            {
                case "named":
                    Fields(node, "kind", "name", "assemblyIdentity", "arity");
                    string name = Text(Get(node, "name")), identity = Text(Get(node, "assemblyIdentity"));
                    if (name.Length == 0 || name.IndexOfAny(new[] { '[', ']', '&', '*', '/', '\\', '|', ';' }) >= 0 || Int(Get(node, "arity")) > 64) throw Bad("named type");
                    int declaredArity = 0;
                    foreach (var part in name.Split('+'))
                    {
                        int tick = part.LastIndexOf('`');
                        if (tick >= 0) { if (!int.TryParse(part.Substring(tick + 1), out int count) || count < 1 || count > 64) throw Bad("named arity spelling"); declaredArity += count; }
                    }
                    if (declaredArity != Int(Get(node, "arity"))) throw Bad("named arity mismatch");
                    if (identity != "bcl")
                    {
                        try { var parsed = new AssemblyName(identity); if (parsed.FullName != identity || parsed.Version == null) throw Bad("assembly identity"); }
                        catch (Exception ex) when (ex is ArgumentException || ex is FileLoadException) { throw Bad("assembly identity: " + identity); }
                    }
                    break;
                case "typeParameter": case "methodParameter":
                    Fields(node, "kind", "position"); if (Int(Get(node, "position")) >= (kind == "typeParameter" ? typeArity : methodArity)) throw Bad("parameter position"); break;
                case "constructed":
                    Fields(node, "kind", "definition", "arguments"); var def = Get(node, "definition");
                    CheckType(def, typeArity, methodArity, depth + 1); var args = Items(Get(node, "arguments"));
                    if (Text(Get(def, "kind")) != "named" || args.Length == 0 || args.Length != Int(Get(def, "arity"))) throw Bad("constructed arity");
                    foreach (var argument in args) CheckType(argument, typeArity, methodArity, depth + 1); break;
                case "array":
                    Fields(node, "kind", "element", "rank", "vector"); int rank = Int(Get(node, "rank")); bool vector = Flag(Get(node, "vector"));
                    if (rank < 1 || rank > 32 || (vector && rank != 1) || (!vector && rank == 1)) throw Bad("array shape unsupported");
                    CheckType(Get(node, "element"), typeArity, methodArity, depth + 1); break;
                case "byRef": case "pointer":
                    Fields(node, "kind", "element"); CheckType(Get(node, "element"), typeArity, methodArity, depth + 1); break;
                default: throw Bad("unknown type node " + kind);
            }
        }
        internal static IEnumerable<XElement> NamedNodes(XElement use) => use.DescendantsAndSelf().Where(e => (string?)e.Attribute("type") == "object" && e.Element("kind")?.Value == "named");

        internal static XElement ReflectType(Type type) => ReflectType(type, false);
        private static XElement ReflectType(Type type, bool definition)
        {
            if (type.IsGenericParameter) return Parameter(type.DeclaringMethod != null, type.GenericParameterPosition);
            if (type.IsByRef) return Wrap("byRef", ReflectType(type.GetElementType()!));
            if (type.IsPointer) return Wrap("pointer", ReflectType(type.GetElementType()!));
            if (type.IsArray) return ArrayType(ReflectType(type.GetElementType()!), type.GetArrayRank(), type.GetArrayRank() == 1 && type == type.GetElementType()!.MakeArrayType());
            if (type.IsGenericType && !definition) return Construct(ReflectType(type.GetGenericTypeDefinition(), true), type.GetGenericArguments().Select(ReflectType));
            return Named(type.FullName ?? throw Bad(type.Name), type.Assembly.FullName!, type.IsGenericTypeDefinition ? type.GetGenericArguments().Length : 0);
        }
        private static XElement ReflectConstraint(Type parameter) => Constraint((int)parameter.GenericParameterAttributes, parameter.GetGenericParameterConstraints().Select(ReflectType));
        internal static XElement ReflectDefinition(Type type, MemberInfo? member)
        {
            var tp = type.IsGenericTypeDefinition ? type.GetGenericArguments().Select(ReflectConstraint).ToArray() : new XElement[0];
            if (member == null) return Definition(ReflectType(type, true), "type", "", false, tp, new XElement[0], Named("System.Void", "bcl"), new XElement[0]);
            if (member is FieldInfo field)
            {
                if (field.GetRequiredCustomModifiers().Length + field.GetOptionalCustomModifiers().Length != 0) throw NativeSignature.Unsupported(field.ToString() ?? field.Name);
                return Definition(ReflectType(type, true), "field", field.Name, field.IsStatic, tp, new XElement[0], ReflectType(field.FieldType), new XElement[0]);
            }
            var method = (MethodBase)member;
            if ((method.CallingConvention & (CallingConventions.VarArgs | CallingConventions.ExplicitThis)) != 0) throw NativeSignature.Unsupported(method.ToString() ?? method.Name);
            var parameters = method.GetParameters();
            if (parameters.Any(p => p.GetRequiredCustomModifiers().Length + p.GetOptionalCustomModifiers().Length != 0)
                || (method is MethodInfo m && m.ReturnParameter.GetRequiredCustomModifiers().Length + m.ReturnParameter.GetOptionalCustomModifiers().Length != 0)) throw NativeSignature.Unsupported(method.ToString() ?? method.Name);
            return Definition(ReflectType(type, true), "method", method.Name, method.IsStatic, tp,
                method.IsGenericMethodDefinition ? method.GetGenericArguments().Select(ReflectConstraint) : new XElement[0],
                method is MethodInfo info ? ReflectType(info.ReturnType) : Named("System.Void", "bcl"), parameters.Select(p => ReflectType(p.ParameterType)));
        }
        internal static bool Contains(Assembly host, XElement use)
        {
            Validate(use); var definition = Get(use, "definition"); var declaring = Get(definition, "declaringType");
            if (Text(Get(declaring, "assemblyIdentity")) != host.FullName) return false;
            var type = host.GetType(Text(Get(declaring, "name")), false, false); if (type == null) return false;
            string key = Key(definition), kind = Text(Get(definition, "kind"));
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (kind == "type") return Key(ReflectDefinition(type, null)) == key;
            IEnumerable<MemberInfo> members = kind == "field" ? type.GetFields(flags).Cast<MemberInfo>() : type.GetMethods(flags).Cast<MemberInfo>().Concat(type.GetConstructors(flags));
            foreach (var member in members.Where(m => m.Name == Text(Get(definition, "name"))))
            { try { if (Key(ReflectDefinition(type, member)) == key) return true; } catch (InvalidDataException) { } }
            return false;
        }
    }
}
