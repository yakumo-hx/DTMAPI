using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace DTMAPI.Tooling.Metadata;

/// <summary>Metadata-only classification; Runtime is a code type, not a Native flag.</summary>
public static class ManagedMethodMetadata
{
    public static void RequireManagedMethods(byte[] bytes, string asset)
    {
        using var stream = new MemoryStream(bytes, false);
        using var assembly = AssemblyDefinition.ReadAssembly(stream);
        foreach (var type in NativeReferenceSurface.Types(assembly.MainModule.Types))
        foreach (var method in type.Methods)
        {
            var code = method.ImplAttributes & MethodImplAttributes.CodeTypeMask;
            string? reason = method.IsPInvokeImpl ? "pinvoke" : method.IsUnmanaged ? "unmanaged" : method.IsInternalCall ? "internal-call"
                : code == MethodImplAttributes.Native ? "native-code-type"
                : code == MethodImplAttributes.OPTIL ? "optil-code-type"
                : code == MethodImplAttributes.Runtime && !LegalDelegateMethod(type, method) ? "invalid-runtime-delegate" : null;
            if (reason != null)
                throw new InvalidDataException("restore-native-method: " + asset + "; " + method.FullName + "; " + reason
                    + " (attributes=" + method.Attributes + ", implementation=" + method.ImplAttributes + ").");
        }
    }

    private static bool FrameworkType(TypeReference? type, string name)
    {
        if (type == null || type.FullName != name || type.Scope is not AssemblyNameReference identity) return false;
        string token = BitConverter.ToString(identity.PublicKeyToken ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant();
        return (identity.Name == "netstandard" && token == "cc7b13ffcd2ddd51")
            || (identity.Name == "mscorlib" && token == "b77a5c561934e089")
            || (identity.Name == "System.Runtime" && token == "b03f5f7f11d50a3a");
    }

    private static bool LegalDelegateMethod(TypeDefinition type, MethodDefinition method)
    {
        if (!type.IsSealed || !FrameworkType(type.BaseType, "System.MulticastDelegate") || method.IsStatic || !method.IsPublic
            || !method.IsHideBySig || method.HasGenericParameters || method.RVA != 0
            || method.ImplAttributes != MethodImplAttributes.Runtime || method.CallingConvention != MethodCallingConvention.Default)
            return false;
        var invokes = type.Methods.Where(m => m.Name == "Invoke").ToArray();
        if (invokes.Length != 1) return false;
        var invoke = invokes[0];
        if (!invoke.IsVirtual || !invoke.IsNewSlot || invoke.IsAbstract || invoke.IsStatic || invoke.HasGenericParameters) return false;
        string[] parameters = method.Parameters.Select(p => p.ParameterType.FullName).ToArray();
        string[] arguments = invoke.Parameters.Select(p => p.ParameterType.FullName).ToArray();
        if (method.Name == ".ctor")
            return method.IsRuntimeSpecialName && method.IsSpecialName && !method.IsVirtual
                && FrameworkType(method.ReturnType, "System.Void") && parameters.Length == 2
                && FrameworkType(method.Parameters[0].ParameterType, "System.Object") && FrameworkType(method.Parameters[1].ParameterType, "System.IntPtr");
        if (!method.IsVirtual || !method.IsNewSlot || method.IsAbstract || method.IsSpecialName || method.IsRuntimeSpecialName) return false;
        if (method.Name == "Invoke") return true;
        if (method.Name == "BeginInvoke")
            return FrameworkType(method.ReturnType, "System.IAsyncResult") && parameters.Length == arguments.Length + 2
                && parameters.Take(arguments.Length).SequenceEqual(arguments)
                && FrameworkType(method.Parameters[arguments.Length].ParameterType, "System.AsyncCallback")
                && FrameworkType(method.Parameters[arguments.Length + 1].ParameterType, "System.Object");
        if (method.Name == "EndInvoke")
        {
            string[] byrefs = invoke.Parameters.Where(p => p.ParameterType.IsByReference).Select(p => p.ParameterType.FullName).ToArray();
            return method.ReturnType.FullName == invoke.ReturnType.FullName && parameters.Length == byrefs.Length + 1
                && parameters.Take(byrefs.Length).SequenceEqual(byrefs)
                && FrameworkType(method.Parameters[byrefs.Length].ParameterType, "System.IAsyncResult");
        }
        return false;
    }
}
