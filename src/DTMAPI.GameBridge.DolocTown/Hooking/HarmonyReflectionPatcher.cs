using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HarmonyReflectionPatcher
    {
        private readonly DtmApiRuntime runtime;
        private object? harmony;
        private Type? harmonyType;
        private Type? harmonyMethodType;
        private static Func<object, object, object, bool, Array, Array>? arrayResultPostfixCallback;

        public HarmonyReflectionPatcher(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool TryPatchPrefix(string targetTypeName, string methodName, MethodInfo? prefix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: prefix, postfix: null, parameterCount: parameterCount);
        }

        public bool TryPatchPrefix(string targetTypeName, string methodName, MethodInfo? prefix, HarmonyTargetSignature signature)
        {
            return TryPatch(targetTypeName, methodName, prefix: prefix, postfix: null, signature: signature);
        }

        public bool TryPatchClosedGenericPrefix(string targetGenericTypeName, string[] genericArgumentTypeNames, string methodName, MethodInfo? prefix, int? parameterCount = null)
        {
            return TryPatchClosedGeneric(targetGenericTypeName, genericArgumentTypeNames, methodName, prefix: prefix, postfix: null, signature: HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        public bool TryPatchClosedGenericPrefix(string targetGenericTypeName, string[] genericArgumentTypeNames, string methodName, MethodInfo? prefix, HarmonyTargetSignature signature)
        {
            return TryPatchClosedGeneric(targetGenericTypeName, genericArgumentTypeNames, methodName, prefix: prefix, postfix: null, signature: signature);
        }

        public bool TryPatchPostfix(string targetTypeName, string methodName, MethodInfo? postfix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: postfix, parameterCount: parameterCount);
        }

        public bool TryPatchPostfix(string targetTypeName, string methodName, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: postfix, signature: signature);
        }

        public bool TryPatchClosedGenericPostfix(string targetGenericTypeName, string[] genericArgumentTypeNames, string methodName, MethodInfo? postfix, int? parameterCount = null)
        {
            return TryPatchClosedGeneric(targetGenericTypeName, genericArgumentTypeNames, methodName, prefix: null, postfix: postfix, signature: HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        public bool TryPatchClosedGenericPostfix(string targetGenericTypeName, string[] genericArgumentTypeNames, string methodName, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            return TryPatchClosedGeneric(targetGenericTypeName, genericArgumentTypeNames, methodName, prefix: null, postfix: postfix, signature: signature);
        }

        public bool TryPatchArrayResultPostfix(string targetTypeName, string methodName, MethodInfo? arrayResultCallback, int? parameterCount = null)
        {
            return TryPatchArrayResultPostfix(targetTypeName, methodName, arrayResultCallback, HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        public bool TryPatchArrayResultPostfix(string targetTypeName, string methodName, MethodInfo? arrayResultCallback, HarmonyTargetSignature signature)
        {
            try
            {
                if (arrayResultCallback == null || !EnsureHarmony())
                    return false;

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, signature);
                if (target == null)
                    return false;

                MethodInfo? postfix = CreateArrayResultPostfix(target, arrayResultCallback);
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
                if (postfix == null || !EnsureHarmony())
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
                foreach (string candidate in candidates.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).Take(80))
                    lines.Add("- " + candidate);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private bool TryPatch(string targetTypeName, string methodName, MethodInfo? prefix, MethodInfo? postfix, int? parameterCount)
        {
            return TryPatch(targetTypeName, methodName, prefix, postfix, HarmonyTargetSignature.FromParameterCount(parameterCount));
        }

        private bool TryPatch(string targetTypeName, string methodName, MethodInfo? prefix, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            try
            {
                if ((prefix == null && postfix == null) || !EnsureHarmony())
                    return false;

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, signature);
                if (target == null)
                    return false;

                return ApplyPatch(target, prefix, postfix);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        private bool TryPatchClosedGeneric(string targetGenericTypeName, string[] genericArgumentTypeNames, string methodName, MethodInfo? prefix, MethodInfo? postfix, HarmonyTargetSignature signature)
        {
            try
            {
                string targetLabel = targetGenericTypeName + "<" + string.Join(", ", genericArgumentTypeNames) + ">." + methodName;
                if (prefix == null && postfix == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: callback missing for " + targetLabel + ".");
                    return false;
                }

                if (!EnsureHarmony())
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: Harmony unavailable for " + targetLabel + ".");
                    return false;
                }

                Type? targetGenericType = FindType(targetGenericTypeName);
                Type[] genericArguments = genericArgumentTypeNames
                    .Select(FindType)
                    .Where(t => t != null)
                    .Cast<Type>()
                    .ToArray();
                if (targetGenericType == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: target generic type not found " + targetGenericTypeName + ".");
                    return false;
                }

                if (!targetGenericType.IsGenericTypeDefinition)
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: target type is not a generic definition " + targetGenericType.FullName + ".");
                    return false;
                }

                if (genericArguments.Length != genericArgumentTypeNames.Length)
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: generic arguments not found for " + targetGenericTypeName + "<" + string.Join(", ", genericArgumentTypeNames) + ">.");
                    return false;
                }

                Type targetType = targetGenericType.MakeGenericType(genericArguments);
                MethodInfo? target = FindTarget(targetType, methodName, signature);
                if (target == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony closed generic patch skipped: method not found " + targetType.FullName + "." + methodName + ".");
                    return false;
                }

                bool patched = ApplyPatch(target, prefix, postfix);
                runtime.RuntimeMonitor.Log("Harmony closed generic patch result: target=" + target + ", prefix=" + (prefix != null) + ", postfix=" + (postfix != null) + ", patched=" + patched + ".");
                return patched;
            }
            catch (Exception ex)
            {
                string args = string.Join(", ", genericArgumentTypeNames);
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch closed generic {targetGenericTypeName}<{args}>.{methodName}.", ex.ToString());
                return false;
            }
        }

        private bool ApplyPatch(MethodBase target, MethodInfo? prefix, MethodInfo? postfix)
        {
            object? prefixMethod = prefix == null ? null : Activator.CreateInstance(harmonyMethodType!, prefix);
            object? postfixMethod = postfix == null ? null : Activator.CreateInstance(harmonyMethodType!, postfix);
            MethodInfo? patch = harmonyType!.GetMethods()
                .FirstOrDefault(m => m.Name == "Patch" &&
                    m.GetParameters().Length == 5 &&
                    typeof(MethodBase).IsAssignableFrom(m.GetParameters()[0].ParameterType));
            if (patch == null)
                return false;

            object?[] args = new object?[] { target, prefixMethod, postfixMethod, null, null };
            patch.Invoke(harmony, args);
            return true;
        }

        private static MethodInfo? CreateArrayResultPostfix(MethodInfo target, MethodInfo callback)
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

            arrayResultPostfixCallback = (Func<object, object, object, bool, Array, Array>)Delegate.CreateDelegate(typeof(Func<object, object, object, bool, Array, Array>), callback);
            MethodInfo? generic = typeof(HarmonyReflectionPatcher).GetMethod(nameof(ArrayResultPostfixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            Type? elementType = target.ReturnType.GetElementType();
            return generic == null || elementType == null
                ? null
                : generic.MakeGenericMethod(targetParameters[0].ParameterType, targetParameters[1].ParameterType, elementType);
        }

        private static void ArrayResultPostfixGeneric<TArg0, TArg1, TElement>(object __instance, TArg0 __0, TArg1 __1, bool __2, ref TElement[] __result)
        {
            Func<object, object, object, bool, Array, Array>? callback = arrayResultPostfixCallback;
            if (callback == null || __result == null)
                return;

            Array next = callback(__instance, __0!, __1!, __2, __result);
            if (next is TElement[] typed)
                __result = typed;
        }

        private bool EnsureHarmony()
        {
            if (harmony != null)
                return true;

            harmonyType = FindType("HarmonyLib.Harmony, 0Harmony") ?? FindType("HarmonyLib.Harmony, Harmony");
            harmonyMethodType = FindType("HarmonyLib.HarmonyMethod, 0Harmony") ?? FindType("HarmonyLib.HarmonyMethod, Harmony");
            if (harmonyType == null || harmonyMethodType == null)
            {
                try
                {
                    Assembly.Load("0Harmony");
                }
                catch
                {
                }
                try
                {
                    string harmonyPath = Path.Combine(runtime.Paths.GamePath, "BepInEx", "core", "0Harmony.dll");
                    if (File.Exists(harmonyPath))
                        Assembly.LoadFrom(harmonyPath);
                }
                catch
                {
                }
                harmonyType = FindType("HarmonyLib.Harmony, 0Harmony") ?? FindType("HarmonyLib.Harmony, Harmony");
                harmonyMethodType = FindType("HarmonyLib.HarmonyMethod, 0Harmony") ?? FindType("HarmonyLib.HarmonyMethod, Harmony");
            }
            if (harmonyType == null || harmonyMethodType == null)
                return false;
            harmony = Activator.CreateInstance(harmonyType, "dtmapi.gamebridge.doloctown");
            return harmony != null;
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
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;

            string typeName = assemblyQualifiedName;
            int comma = assemblyQualifiedName.IndexOf(',');
            if (comma >= 0)
                typeName = assemblyQualifiedName.Substring(0, comma).Trim();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static ConstructorInfo? FindConstructor(Type? type, int? parameterCount)
        {
            return FindConstructor(type, HarmonyTargetSignature.FromParameterCount(parameterCount));
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
            string location;
            try
            {
                location = assembly.Location;
            }
            catch
            {
                location = string.Empty;
            }

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
            catch
            {
                return Array.Empty<Type>();
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
            return new HarmonyTargetSignature(declaringType: declaringType, returnType: returnType, parameterTypes: parameterTypes ?? Type.EmptyTypes);
        }

        public static HarmonyTargetSignature Exact(string declaringTypeName, string returnTypeName, params string[] parameterTypeNames)
        {
            return new HarmonyTargetSignature(declaringTypeName: declaringTypeName, returnTypeName: returnTypeName, parameterTypeNames: parameterTypeNames ?? Array.Empty<string>());
        }

        public bool Matches(MethodInfo method)
        {
            if (method == null)
                return false;
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
            if (constructor == null)
                return false;
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
