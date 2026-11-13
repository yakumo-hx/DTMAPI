using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;
using HarmonyLib;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HarmonyPatcher
    {
        private const string ProductionHarmonyOwnerId = "dtmapi.gamebridge.doloctown";
        private readonly DtmApiRuntime runtime;
        private readonly string ownerId;
        private Harmony? harmony;
        private static ArrayResultPostfixBinding? arrayResultPostfixBinding;
        private static readonly Dictionary<string, Type> TypeResolutionCache = new Dictionary<string, Type>(StringComparer.Ordinal);

        public HarmonyPatcher(DtmApiRuntime runtime)
            : this(runtime, ProductionHarmonyOwnerId)
        {
        }

        internal HarmonyPatcher(DtmApiRuntime runtime, string ownerId)
        {
            this.runtime = runtime;
            this.ownerId = ownerId;
        }

        internal static void ClearStaticCallbacks()
        {
            arrayResultPostfixBinding = null;
        }

        public bool TryPatchPrefix(string targetTypeName, string methodName, MethodInfo? prefix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: prefix, postfix: null, parameterCount: parameterCount);
        }

        public bool TryPatchPrefix(string targetTypeName, string methodName, MethodInfo? prefix, HarmonyTargetSignature signature)
        {
            return TryPatch(targetTypeName, methodName, prefix: prefix, postfix: null, signature: signature);
        }

        public bool TryPatchPostfix(string targetTypeName, string methodName, MethodInfo? postfix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: postfix, parameterCount: parameterCount);
        }

        public bool TryPatchPostfix(string targetTypeName, string methodName, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: postfix, signature: signature);
        }

        public bool TryPatchFinalizer(string targetTypeName, string methodName, MethodInfo? finalizer, HarmonyTargetSignature signature)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: null, signature: signature, finalizer: finalizer);
        }

        public bool TryPatchArrayResultPostfix(
            string targetTypeName,
            string methodName,
            MethodInfo? arrayResultCallback,
            int? parameterCount = null,
            Func<bool>? callbackGuard = null)
        {
            return TryPatchArrayResultPostfix(targetTypeName, methodName, arrayResultCallback, HarmonyTargetSignature.FromParameterCount(parameterCount), callbackGuard);
        }

        public bool TryPatchArrayResultPostfix(
            string targetTypeName,
            string methodName,
            MethodInfo? arrayResultCallback,
            HarmonyTargetSignature signature,
            Func<bool>? callbackGuard = null)
        {
            try
            {
                if (arrayResultCallback == null)
                    return false;

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, signature);
                if (target == null)
                    return false;

                MethodInfo? postfix = CreateArrayResultPostfix(target, arrayResultCallback, callbackGuard);
                return postfix != null && ApplyPatch(target, prefix: null, postfix: postfix);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch array-result postfix {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        public bool TryPatchConstructorPostfix(string targetTypeName, MethodInfo? postfix, int? parameterCount = null)
        {
            return TryPatchConstructorPostfix(targetTypeName, postfix, HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        public bool TryPatchConstructorPostfix(string targetTypeName, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            try
            {
                if (postfix == null)
                    return false;

                Type? targetType = FindType(targetTypeName);
                ConstructorInfo? target = FindConstructor(targetType, signature);
                if (target == null)
                    return false;

                return ApplyPatch(target, prefix: null, postfix: postfix);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch constructor {targetTypeName}.", ex.ToString());
                return false;
            }
        }

        public Type? ResolveType(string assemblyQualifiedName)
        {
            return FindType(assemblyQualifiedName);
        }

        public MethodInfo? ResolveMethod(string targetTypeName, string methodName, int? parameterCount = null)
        {
            return FindTarget(FindType(targetTypeName), methodName, parameterCount);
        }

        public MethodInfo? ResolveMethod(string targetTypeName, string methodName, HarmonyTargetSignature signature)
        {
            return FindTarget(FindType(targetTypeName), methodName, signature);
        }

        public string BuildTypeResolutionReport(params string[] expectedTypeNames)
        {
            var lines = new List<string>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .OrderBy(a => a.GetName().Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lines.Add("Loaded assemblies visible to DTMAPI:");
            foreach (Assembly assembly in assemblies)
                lines.Add("- " + DescribeAssembly(assembly));

            lines.Add("Expected type lookup:");
            foreach (string expected in expectedTypeNames)
            {
                string typeName = ExtractTypeName(expected);
                Type? resolved = FindType(expected);
                lines.Add("- " + typeName + " => " + (resolved == null ? "missing" : resolved.Assembly.GetName().Name + "::" + resolved.FullName));
            }

            string[] candidateNeedles = expectedTypeNames
                .Select(ExtractTypeName)
                .Select(t => t.Split('.').Last())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var candidates = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                string assemblyName = assembly.GetName().Name ?? string.Empty;
                if (!assemblyName.Contains("Assembly-CSharp") && !assemblyName.Contains("Doloc") && !assemblyName.Contains("Game"))
                    continue;

                foreach (Type type in SafeGetTypes(assembly))
                {
                    string fullName = type.FullName ?? type.Name;
                    if (candidateNeedles.Any(n => fullName.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                        candidates.Add(assemblyName + "::" + fullName);
                }
            }

            lines.Add("Candidate types:");
            if (candidates.Count == 0)
                lines.Add("- none");
            else
            {
                foreach (string candidate in candidates.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                    lines.Add("- " + candidate);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private bool TryPatch(string targetTypeName, string methodName, MethodInfo? prefix, MethodInfo? postfix, int? parameterCount)
        {
            return TryPatch(targetTypeName, methodName, prefix, postfix, HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        private bool TryPatch(string targetTypeName, string methodName, MethodInfo? prefix, MethodInfo? postfix, HarmonyTargetSignature signature, MethodInfo? finalizer = null)
        {
            try
            {
                if (prefix == null && postfix == null && finalizer == null)
                    return false;

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, signature);
                if (target == null)
                    return false;

                return ApplyPatch(target, prefix, postfix, finalizer);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        private bool ApplyPatch(MethodBase target, MethodInfo? prefix, MethodInfo? postfix, MethodInfo? finalizer = null)
        {
            Patches? info = Harmony.GetPatchInfo(target);
            bool Installed(IEnumerable<Patch>? patches, MethodInfo? method) =>
                method == null || (patches != null && patches.Any(patch => patch.owner == ownerId && patch.PatchMethod == method));

            // 平台补丁常驻；新的适配器复用已经安装的回调。
            if (Installed(info?.Prefixes, prefix)) prefix = null;
            if (Installed(info?.Postfixes, postfix)) postfix = null;
            if (Installed(info?.Finalizers, finalizer)) finalizer = null;
            if (prefix == null && postfix == null && finalizer == null) return true;

            HarmonyMethod? Wrap(MethodInfo? method) => method == null ? null : new HarmonyMethod(method);
            (harmony ??= new Harmony(ownerId)).Patch(target, prefix: Wrap(prefix), postfix: Wrap(postfix), finalizer: Wrap(finalizer));
            return true;
        }

        private static MethodInfo? CreateArrayResultPostfix(MethodInfo target, MethodInfo callback, Func<bool>? callbackGuard)
        {
            ParameterInfo[] targetParameters = target.GetParameters();
            if (!target.ReturnType.IsArray || targetParameters.Length != 3 || targetParameters[2].ParameterType != typeof(bool))
                return null;

            ParameterInfo[] callbackParameters = callback.GetParameters();
            if (!callback.IsStatic ||
                callback.ReturnType != typeof(Array) ||
                callbackParameters.Length != 5 ||
                callbackParameters[0].ParameterType != typeof(object) ||
                callbackParameters[1].ParameterType != typeof(object) ||
                callbackParameters[2].ParameterType != typeof(object) ||
                callbackParameters[3].ParameterType != typeof(bool) ||
                callbackParameters[4].ParameterType != typeof(Array))
                return null;

            var binding = new ArrayResultPostfixBinding(
                (Func<object, object, object, bool, Array, Array>)Delegate.CreateDelegate(typeof(Func<object, object, object, bool, Array, Array>), callback),
                callbackGuard);
            MethodInfo? generic = typeof(HarmonyPatcher).GetMethod(nameof(ArrayResultPostfixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            Type? elementType = target.ReturnType.GetElementType();
            if (generic == null || elementType == null)
                return null;

            arrayResultPostfixBinding = binding;
            return generic.MakeGenericMethod(targetParameters[0].ParameterType, targetParameters[1].ParameterType, elementType);
        }

        internal static void ArrayResultPostfixGeneric<TArg0, TArg1, TElement>(object __instance, TArg0 __0, TArg1 __1, bool __2, ref TElement[] __result)
        {
            ArrayResultPostfixBinding? binding = arrayResultPostfixBinding;
            if (binding == null || __result == null || (binding.CallbackGuard != null && !binding.CallbackGuard()))
                return;

            Array next = binding.Callback(__instance, __0!, __1!, __2, __result);
            if (next is TElement[] typed)
                __result = typed;
        }

        private sealed class ArrayResultPostfixBinding
        {
            internal ArrayResultPostfixBinding(
                Func<object, object, object, bool, Array, Array> callback,
                Func<bool>? callbackGuard)
            {
                Callback = callback;
                CallbackGuard = callbackGuard;
            }

            internal Func<object, object, object, bool, Array, Array> Callback { get; }
            internal Func<bool>? CallbackGuard { get; }
        }

        private static MethodInfo? FindTarget(Type? type, string name, int? parameterCount)
        {
            return FindTarget(type, name, HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        private static MethodInfo? FindTarget(Type? type, string name, HarmonyTargetSignature signature)
        {
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name != name)
                    continue;
                if (!signature.Matches(method))
                    continue;
                return method;
            }
            return null;
        }

        private static Type? FindType(string assemblyQualifiedName)
        {
            if (TypeResolutionCache.TryGetValue(assemblyQualifiedName, out Type cached))
                return cached;
            Type? resolved = FindTypeUncached(assemblyQualifiedName);
            if (resolved != null)
                TypeResolutionCache[assemblyQualifiedName] = resolved;
            return resolved;
        }

        private static Type? FindTypeUncached(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;

            string typeName = assemblyQualifiedName;
            int comma = assemblyQualifiedName.IndexOf(',');
            if (comma >= 0)
                typeName = assemblyQualifiedName.Substring(0, comma).Trim();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }

        private static ConstructorInfo? FindConstructor(Type? type, HarmonyTargetSignature signature)
        {
            if (type == null)
                return null;
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!signature.Matches(constructor))
                    continue;
                return constructor;
            }
            return null;
        }

        private static string ExtractTypeName(string assemblyQualifiedName)
        {
            int comma = assemblyQualifiedName.IndexOf(',');
            return comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName.Trim();
        }

        private static string DescribeAssembly(Assembly assembly)
        {
            string location = assembly.IsDynamic ? string.Empty : assembly.Location;

            string name = assembly.GetName().Name ?? assembly.FullName;
            return string.IsNullOrWhiteSpace(location) ? name : name + " @ " + location;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }
    }

    internal readonly struct HarmonyTargetSignature
    {
        private readonly int? parameterCount;
        private readonly Type? declaringType;
        private readonly Type? returnType;
        private readonly Type[]? parameterTypes;
        private readonly string? declaringTypeName;
        private readonly string? returnTypeName;
        private readonly string[]? parameterTypeNames;

        public HarmonyTargetSignature(
            int? parameterCount = null,
            Type? declaringType = null,
            Type? returnType = null,
            Type[]? parameterTypes = null,
            string? declaringTypeName = null,
            string? returnTypeName = null,
            string[]? parameterTypeNames = null)
        {
            this.parameterCount = parameterCount;
            this.declaringType = declaringType;
            this.returnType = returnType;
            this.parameterTypes = parameterTypes == null ? null : parameterTypes.ToArray();
            this.declaringTypeName = declaringTypeName;
            this.returnTypeName = returnTypeName;
            this.parameterTypeNames = parameterTypeNames == null ? null : parameterTypeNames.ToArray();
        }

        public static HarmonyTargetSignature FromParameterCount(int? parameterCount)
        {
            return new HarmonyTargetSignature(parameterCount: parameterCount);
        }

        public static HarmonyTargetSignature Exact(Type declaringType, Type returnType, params Type[] parameterTypes)
        {
            return new HarmonyTargetSignature(declaringType: declaringType, returnType: returnType, parameterTypes: parameterTypes);
        }

        public static HarmonyTargetSignature Exact(string declaringTypeName, string returnTypeName, params string[] parameterTypeNames)
        {
            return new HarmonyTargetSignature(declaringTypeName: declaringTypeName, returnTypeName: returnTypeName, parameterTypeNames: parameterTypeNames);
        }

        public bool Matches(MethodInfo method)
        {
            if (declaringType != null && method.DeclaringType != declaringType)
                return false;
            if (!MatchesTypeName(method.DeclaringType, declaringTypeName))
                return false;
            if (returnType != null && method.ReturnType != returnType)
                return false;
            if (!MatchesTypeName(method.ReturnType, returnTypeName))
                return false;

            ParameterInfo[] parameters = method.GetParameters();
            return MatchesParameters(parameters);
        }

        public bool Matches(ConstructorInfo constructor)
        {
            if (declaringType != null && constructor.DeclaringType != declaringType)
                return false;
            if (!MatchesTypeName(constructor.DeclaringType, declaringTypeName))
                return false;

            ParameterInfo[] parameters = constructor.GetParameters();
            return MatchesParameters(parameters);
        }

        private bool MatchesParameters(ParameterInfo[] parameters)
        {
            if (parameterTypes != null)
            {
                if (parameters.Length != parameterTypes.Length)
                    return false;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        return false;
                }
                return true;
            }

            if (parameterTypeNames != null)
            {
                if (parameters.Length != parameterTypeNames.Length)
                    return false;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!MatchesTypeName(parameters[i].ParameterType, parameterTypeNames[i]))
                        return false;
                }
                return true;
            }

            return !parameterCount.HasValue || parameters.Length == parameterCount.Value;
        }

        private static bool MatchesTypeName(Type? actual, string? expected)
        {
            string trimmed = expected?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                return true;
            if (actual == null)
                return false;

            return string.Equals(actual.FullName, trimmed, StringComparison.Ordinal) ||
                string.Equals(actual.AssemblyQualifiedName, trimmed, StringComparison.Ordinal) ||
                string.Equals(actual.Name, trimmed, StringComparison.Ordinal);
        }
    }
}
