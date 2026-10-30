using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DTMAPI.Internal.Authoring
{
    // Shared spelling for metadata-only tooling and reflection over the installed host.
    // V1 deliberately rejects signatures which cannot be represented exactly.
    internal static class NativeSignature
    {
        internal static string NamedType(string name, string assemblyName, string token, string identity)
        {
            bool framework = (assemblyName == "mscorlib" || assemblyName == "netstandard" || assemblyName == "System.Private.CoreLib" || assemblyName == "System" || assemblyName.StartsWith("System.", StringComparison.Ordinal))
                && (token == "b77a5c561934e089" || token == "b03f5f7f11d50a3a" || token == "7cec85d7bea7798e" || token == "cc7b13ffcd2ddd51");
            return "[" + (framework ? "bcl" : identity) + "]" + name.Replace('/', '+');
        }

        internal static string TypeName(Type type)
        {
            if (type.IsGenericType || type.IsGenericParameter) throw Unsupported(type.ToString());
            if (type.IsByRef) return TypeName(type.GetElementType()!) + "&";
            if (type.IsPointer) return TypeName(type.GetElementType()!) + "*";
            if (type.IsArray)
            {
                Type element = type.GetElementType()!;
                if (type.GetArrayRank() == 1 && type != element.MakeArrayType()) throw Unsupported("non-vector rank-one array");
                return TypeName(element) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }
            AssemblyName assembly = type.Assembly.GetName();
            string token = BitConverter.ToString(assembly.GetPublicKeyToken() ?? new byte[0]).Replace("-", "").ToLowerInvariant();
            return NamedType(type.FullName ?? throw Unsupported(type.Name), assembly.Name!, token, assembly.FullName);
        }

        internal static string MemberKey(string assembly, string declaringType, string kind, string name, bool isStatic, string result, string[] parameters)
            => string.Join("|", assembly, declaringType, kind, name, isStatic ? "static" : "instance", result, string.Join(";", parameters));

        internal static InvalidDataException Unsupported(string detail) => new InvalidDataException("native-signature-unsupported: " + detail);
    }
}
