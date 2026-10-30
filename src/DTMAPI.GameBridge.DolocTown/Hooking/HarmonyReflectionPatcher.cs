using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HarmonyReflectionPatcher
    {
        private const string ProductionHarmonyOwnerId = "dtmapi.gamebridge.doloctown";
        private readonly DtmApiRuntime runtime;
        private readonly string ownerId;
        private HarmonyBinding? harmony;
        private static ArrayResultPostfixBinding? arrayResultPostfixBinding;
        private static readonly object TypeResolutionGate = new object();
        private static readonly Dictionary<string, Type?> TypeResolutionCache = new Dictionary<string, Type?>(StringComparer.Ordinal);

        static HarmonyReflectionPatcher()
        {
            // Negative lookups are common while an optional native surface is unavailable.
            // Cache them too, then invalidate on AssemblyLoad so a later-loaded owner can be
            // discovered without rescanning every loaded assembly on every player frame.
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
        }

        public HarmonyReflectionPatcher(DtmApiRuntime runtime)
            : this(runtime, ProductionHarmonyOwnerId)
        {
        }

        internal HarmonyReflectionPatcher(DtmApiRuntime runtime, string ownerId)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Harmony owner id is required.", nameof(ownerId));
            this.ownerId = ownerId;
        }

        internal string OwnerId => ownerId;

        internal void BindHarmonyForTests(object instance, Type methodType) =>
            harmony = new HarmonyBinding(instance, instance.GetType(), methodType);

        internal static void ClearStaticCallbacks()
        {
            Volatile.Write(ref arrayResultPostfixBinding, null);
        }

        internal bool TryUnpatchAllOwnedPatches()
        {
            if (harmony == null)
                return true;

            try
            {
                MethodInfo? unpatchAll = harmony.UnpatchAll;
                if (unpatchAll == null)
                {
                    runtime.Diagnostics.RecordError(
                        "DTMAPI.GameBridge",
                        "Failed to unpatch Harmony owner because the exact UnpatchAll(string) API is unavailable.",
                        "owner=" + ownerId);
                    return false;
                }

                unpatchAll.Invoke(unpatchAll.IsStatic ? null : harmony.Instance, new object[] { ownerId });
                harmony = null;
                runtime.RuntimeMonitor.Log("Harmony owner cleanup completed owner=" + ownerId + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Failed to unpatch Harmony owner " + ownerId + ".",
                    ex.ToString());
                return false;
            }
        }

        internal bool TryUnpatchOwnedPatch(
            string targetTypeName,
            string methodName,
            int parameterCount)
        {
            try
            {
                if (!EnsureHarmony())
                    return false;
                MethodInfo? target = FindTarget(
                    FindType(targetTypeName),
                    methodName,
                    parameterCount);
                if (target == null)
                    return false;
                MethodInfo? unpatch = harmony!.Unpatch;
                if (unpatch == null)
                    return false;
                Type patchType =
                    unpatch.GetParameters()[1].ParameterType;
                object all = Enum.Parse(
                    patchType,
                    "All",
                    ignoreCase: false);
                unpatch.Invoke(
                    harmony.Instance,
                    new[] { target, all, ownerId });
                return CountOwnerPatches(
                    targetTypeName,
                    methodName,
                    parameterCount,
                    ownerId) == 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Failed to unpatch exact Harmony target " +
                    targetTypeName + "." + methodName + ".",
                    ex.ToString());
                return false;
            }
        }

        internal int CountOwnerPatches(
            string targetTypeName,
            string methodName,
            int parameterCount,
            string expectedOwnerId)
        {
            try
            {
                if (!EnsureHarmony())
                    return -1;
                MethodInfo? target = FindTarget(
                    FindType(targetTypeName),
                    methodName,
                    parameterCount);
                if (target == null)
                    return -1;
                MethodInfo? getPatchInfo = harmony!.GetPatchInfo;
                object? info =
                    getPatchInfo?.Invoke(null, new object[] { target });
                if (info == null)
                    return 0;
                int count = 0;
                foreach (string collectionName in new[]
                         {
                             "Prefixes",
                             "Postfixes",
                             "Transpilers",
                             "Finalizers"
                         })
                {
                    object? collection =
                        info.GetType()
                            .GetProperty(
                                collectionName,
                                BindingFlags.Public |
                                BindingFlags.Instance)
                            ?.GetValue(info, null) ??
                        info.GetType()
                            .GetField(
                                collectionName,
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.Instance)
                            ?.GetValue(info);
                    if (!(collection is System.Collections.IEnumerable
                          patches))
                        continue;
                    foreach (object? patch in patches)
                    {
                        if (patch == null)
                            continue;
                        Type patchType = patch.GetType();
                        object? patchOwner =
                            patchType.GetProperty(
                                    "owner",
                                    BindingFlags.Public |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Instance)
                                ?.GetValue(patch, null) ??
                            patchType.GetField(
                                    "owner",
                                    BindingFlags.Public |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Instance)
                                ?.GetValue(patch);
                        if (string.Equals(
                                patchOwner?.ToString(),
                                expectedOwnerId,
                                StringComparison.Ordinal))
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Failed to inspect Harmony target " +
                    targetTypeName + "." + methodName + ".",
                    ex.ToString());
                return -1;
            }
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
                if (arrayResultCallback == null || !EnsureHarmony())
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

        private bool TryPatch(string targetTypeName, string methodName, MethodInfo? prefix, MethodInfo? postfix, HarmonyTargetSignature signature, MethodInfo? finalizer = null)
        {
            try
            {
                if ((prefix == null && postfix == null && finalizer == null) || !EnsureHarmony())
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
            HarmonyBinding binding = harmony!;
            if (binding.Patch == null) return false;
            object? Wrap(MethodInfo? method) => method == null ? null : Activator.CreateInstance(binding.MethodType, method);
            binding.Patch.Invoke(binding.Instance, new object?[] { target, Wrap(prefix), Wrap(postfix), null, Wrap(finalizer) });
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
            MethodInfo? generic = typeof(HarmonyReflectionPatcher).GetMethod(nameof(ArrayResultPostfixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            Type? elementType = target.ReturnType.GetElementType();
            if (generic == null || elementType == null)
                return null;

            Volatile.Write(ref arrayResultPostfixBinding, binding);
            return generic.MakeGenericMethod(targetParameters[0].ParameterType, targetParameters[1].ParameterType, elementType);
        }

        internal static void ArrayResultPostfixGeneric<TArg0, TArg1, TElement>(object __instance, TArg0 __0, TArg1 __1, bool __2, ref TElement[] __result)
        {
            ArrayResultPostfixBinding? binding = Volatile.Read(ref arrayResultPostfixBinding);
            if (binding == null || __result == null || (binding.CallbackGuard != null && !binding.CallbackGuard()))
                return;

            Array next = binding.Callback(__instance, __0!, __1!, __2, __result);
            if (next is TElement[] typed)
                __result = typed;
        }

        internal static void ConfigureArrayResultPostfixForTests(
            Func<object, object, object, bool, Array, Array> callback,
            Func<bool>? callbackGuard)
        {
            Volatile.Write(ref arrayResultPostfixBinding, new ArrayResultPostfixBinding(
                callback ?? throw new ArgumentNullException(nameof(callback)),
                callbackGuard));
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

        private bool EnsureHarmony()
        {
            if (harmony != null)
                return true;

            Type? harmonyType = FindType("HarmonyLib.Harmony, 0Harmony") ?? FindType("HarmonyLib.Harmony, Harmony");
            Type? harmonyMethodType = FindType("HarmonyLib.HarmonyMethod, 0Harmony") ?? FindType("HarmonyLib.HarmonyMethod, Harmony");
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
            object? instance = Activator.CreateInstance(harmonyType, ownerId);
            if (instance == null) return false;
            harmony = new HarmonyBinding(instance, harmonyType, harmonyMethodType);
            return true;
        }

        private sealed class HarmonyBinding
        {
            public HarmonyBinding(object instance, Type type, Type methodType)
            {
                Instance = instance;
                MethodType = methodType;
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Patch = Find("Patch", parameters => parameters.Length == 5 && typeof(MethodBase).IsAssignableFrom(parameters[0].ParameterType));
                UnpatchAll = Find("UnpatchAll", parameters => parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
                Unpatch = methods.FirstOrDefault(method => !method.IsStatic && method.Name == "Unpatch" && MatchesUnpatch(method.GetParameters()));
                GetPatchInfo = methods.FirstOrDefault(method => method.IsStatic && method.Name == "GetPatchInfo" &&
                    method.GetParameters().Length == 1 && typeof(MethodBase).IsAssignableFrom(method.GetParameters()[0].ParameterType));
                MethodInfo? Find(string name, Func<ParameterInfo[], bool> accepts) =>
                    methods.FirstOrDefault(method => method.Name == name && accepts(method.GetParameters()));
            }

            private static bool MatchesUnpatch(ParameterInfo[] parameters) => parameters.Length == 3 &&
                typeof(MethodBase).IsAssignableFrom(parameters[0].ParameterType) && parameters[1].ParameterType.IsEnum && parameters[2].ParameterType == typeof(string);

            public object Instance { get; }
            public Type MethodType { get; }
            public MethodInfo? Patch { get; }
            public MethodInfo? UnpatchAll { get; }
            public MethodInfo? Unpatch { get; }
            public MethodInfo? GetPatchInfo { get; }
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
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                return null;

            lock (TypeResolutionGate)
            {
                if (TypeResolutionCache.TryGetValue(assemblyQualifiedName, out Type? cached))
                    return cached;
            }

            Type? resolved = FindTypeUncached(assemblyQualifiedName);
            lock (TypeResolutionGate)
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

        private static void OnAssemblyLoaded(object? sender, AssemblyLoadEventArgs args)
        {
            lock (TypeResolutionGate)
                TypeResolutionCache.Clear();
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
