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
        private object? harmony;
        private Type? harmonyType;
        private Type? harmonyMethodType;
        private static ArrayResultPostfixBinding? arrayResultPostfixBinding;
        private static Func<string, AnimatorAssetLoadPrefixResult>? animatorAssetTryLoadAssetCallback;
        private static Func<string, Type?, Type?>? animalAIDefaultAnyStateCallback;
        private static Func<object?, AnimalAITaskPrefixResult>? animalAIMakeDecisionFreeTimeCallback;
        private static Func<object?, string, object?, PngSpriteOverrideResult>? spriteOverrideTryGetModOverrideSpriteCallback;
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

        internal static void ClearStaticCallbacks()
        {
            Volatile.Write(ref arrayResultPostfixBinding, null);
            animatorAssetTryLoadAssetCallback = null;
            animalAIDefaultAnyStateCallback = null;
            animalAIMakeDecisionFreeTimeCallback = null;
            spriteOverrideTryGetModOverrideSpriteCallback = null;
        }

        internal bool TryUnpatchAllOwnedPatches()
        {
            if (harmony == null)
                return true;

            try
            {
                MethodInfo? unpatchAll = harmonyType?
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .FirstOrDefault(method =>
                    {
                        if (!method.Name.Equals("UnpatchAll", StringComparison.Ordinal))
                            return false;
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                    });
                if (unpatchAll == null)
                {
                    runtime.Diagnostics.RecordError(
                        "DTMAPI.GameBridge",
                        "Failed to unpatch Harmony owner because the exact UnpatchAll(string) API is unavailable.",
                        "owner=" + ownerId);
                    return false;
                }

                unpatchAll.Invoke(unpatchAll.IsStatic ? null : harmony, new object[] { ownerId });
                harmony = null;
                harmonyType = null;
                harmonyMethodType = null;
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
                MethodInfo? unpatch = harmonyType?
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    .FirstOrDefault(method =>
                    {
                        if (!method.Name.Equals(
                                "Unpatch",
                                StringComparison.Ordinal))
                            return false;
                        ParameterInfo[] parameters =
                            method.GetParameters();
                        return parameters.Length == 3 &&
                            typeof(MethodBase).IsAssignableFrom(
                                parameters[0].ParameterType) &&
                            parameters[1].ParameterType.IsEnum &&
                            parameters[2].ParameterType ==
                                typeof(string);
                    });
                if (unpatch == null)
                    return false;
                Type patchType =
                    unpatch.GetParameters()[1].ParameterType;
                object all = Enum.Parse(
                    patchType,
                    "All",
                    ignoreCase: false);
                unpatch.Invoke(
                    harmony,
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
                MethodInfo? getPatchInfo = harmonyType?
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .FirstOrDefault(method =>
                    {
                        if (!method.Name.Equals(
                                "GetPatchInfo",
                                StringComparison.Ordinal))
                            return false;
                        ParameterInfo[] parameters =
                            method.GetParameters();
                        return parameters.Length == 1 &&
                            typeof(MethodBase).IsAssignableFrom(
                                parameters[0].ParameterType);
                    });
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

        public bool TryPatchAnimatorAssetTryLoadAssetPrefix(
            string targetTypeName,
            string methodName,
            string runtimeAnimatorControllerTypeName,
            Func<string, AnimatorAssetLoadPrefixResult> callback)
        {
            try
            {
                string targetLabel = targetTypeName + "." + methodName + "(string,out " + runtimeAnimatorControllerTypeName + ")";
                if (callback == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimatorAsset.TryLoadAsset prefix skipped: callback missing for " + targetLabel + ".");
                    return false;
                }

                if (!EnsureHarmony())
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimatorAsset.TryLoadAsset prefix skipped: Harmony unavailable for " + targetLabel + ".");
                    return false;
                }

                Type? targetType = FindType(targetTypeName);
                Type? runtimeAnimatorControllerType = FindType(runtimeAnimatorControllerTypeName);
                if (targetType == null || runtimeAnimatorControllerType == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimatorAsset.TryLoadAsset prefix skipped: target or RuntimeAnimatorController type missing for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? target = FindAnimatorAssetTryLoadAssetTarget(targetType, methodName, runtimeAnimatorControllerType);
                if (target == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimatorAsset.TryLoadAsset prefix skipped: exact target signature not found for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? prefixDefinition = typeof(HarmonyReflectionPatcher).GetMethod(nameof(AnimatorAssetTryLoadAssetPrefixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
                if (prefixDefinition == null)
                    return false;

                animatorAssetTryLoadAssetCallback = callback;
                MethodInfo prefix = prefixDefinition.MakeGenericMethod(runtimeAnimatorControllerType);
                bool patched = ApplyPatch(target, prefix: prefix, postfix: null);
                runtime.RuntimeMonitor.Log("Harmony AnimatorAsset.TryLoadAsset prefix result: target=" + target + ", patched=" + patched + ".");
                return patched;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch AnimatorAsset.TryLoadAsset prefix {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        public bool TryPatchAnimalAIDefaultAnyStatePostfix(
            string targetTypeName,
            string methodName,
            Func<string, Type?, Type?> callback)
        {
            try
            {
                string targetLabel = targetTypeName + "." + methodName + "(string):Type";
                if (callback == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.GetDefaultAnyState postfix skipped: callback missing for " + targetLabel + ".");
                    return false;
                }

                if (!EnsureHarmony())
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.GetDefaultAnyState postfix skipped: Harmony unavailable for " + targetLabel + ".");
                    return false;
                }

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindAnimalAIDefaultAnyStateTarget(targetType, methodName);
                if (target == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.GetDefaultAnyState postfix skipped: exact target signature not found for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? postfix = typeof(HarmonyReflectionPatcher).GetMethod(nameof(AnimalAIDefaultAnyStatePostfix), BindingFlags.NonPublic | BindingFlags.Static);
                if (postfix == null)
                    return false;

                animalAIDefaultAnyStateCallback = callback;
                bool patched = ApplyPatch(target, prefix: null, postfix: postfix);
                runtime.RuntimeMonitor.Log("Harmony AnimalAI.GetDefaultAnyState postfix result: target=" + target + ", patched=" + patched + ".");
                return patched;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch AnimalAI.GetDefaultAnyState postfix {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        public bool TryPatchAnimalAIMakeDecisionFreeTimePrefix(
            string targetTypeName,
            string methodName,
            Func<object?, AnimalAITaskPrefixResult> callback)
        {
            try
            {
                string targetLabel = targetTypeName + "." + methodName + "():LinearTask";
                if (callback == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.MakeDecision_FreeTime prefix skipped: callback missing for " + targetLabel + ".");
                    return false;
                }

                if (!EnsureHarmony())
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.MakeDecision_FreeTime prefix skipped: Harmony unavailable for " + targetLabel + ".");
                    return false;
                }

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, HarmonyTargetSignature.FromParameterCount(0));
                if (target == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.MakeDecision_FreeTime prefix skipped: exact target signature not found for " + targetLabel + ".");
                    return false;
                }

                if (target.ReturnType.IsValueType)
                {
                    runtime.RuntimeMonitor.Log("Harmony AnimalAI.MakeDecision_FreeTime prefix skipped: target return type is not a class for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? prefixDefinition = typeof(HarmonyReflectionPatcher).GetMethod(nameof(AnimalAIMakeDecisionFreeTimePrefixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
                if (prefixDefinition == null)
                    return false;

                animalAIMakeDecisionFreeTimeCallback = callback;
                MethodInfo prefix = prefixDefinition.MakeGenericMethod(target.ReturnType);
                bool patched = ApplyPatch(target, prefix: prefix, postfix: null);
                runtime.RuntimeMonitor.Log("Harmony AnimalAI.MakeDecision_FreeTime prefix result: target=" + target + ", patched=" + patched + ".");
                return patched;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch AnimalAI.MakeDecision_FreeTime prefix {targetTypeName}.{methodName}.", ex.ToString());
                return false;
            }
        }

        public bool TryPatchSpriteOverrideTryGetModOverrideSpritePostfix(
            string targetTypeName,
            string methodName,
            string spriteTypeName,
            Func<object?, string, object?, PngSpriteOverrideResult> callback)
        {
            try
            {
                string targetLabel = targetTypeName + "." + methodName + "(string,out " + spriteTypeName + ")";
                if (callback == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony SpriteOverrideHandler.TryGetModOverrideSprite postfix skipped: callback missing for " + targetLabel + ".");
                    return false;
                }

                if (!EnsureHarmony())
                {
                    runtime.RuntimeMonitor.Log("Harmony SpriteOverrideHandler.TryGetModOverrideSprite postfix skipped: Harmony unavailable for " + targetLabel + ".");
                    return false;
                }

                Type? targetType = FindType(targetTypeName);
                Type? spriteType = FindType(spriteTypeName);
                if (targetType == null || spriteType == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony SpriteOverrideHandler.TryGetModOverrideSprite postfix skipped: target or Sprite type missing for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? target = FindSpriteOverrideTryGetModOverrideSpriteTarget(targetType, methodName, spriteType);
                if (target == null)
                {
                    runtime.RuntimeMonitor.Log("Harmony SpriteOverrideHandler.TryGetModOverrideSprite postfix skipped: exact target signature not found for " + targetLabel + ".");
                    return false;
                }

                MethodInfo? postfixDefinition = typeof(HarmonyReflectionPatcher).GetMethod(nameof(SpriteOverrideTryGetModOverrideSpritePostfixGeneric), BindingFlags.NonPublic | BindingFlags.Static);
                if (postfixDefinition == null)
                    return false;

                spriteOverrideTryGetModOverrideSpriteCallback = callback;
                MethodInfo postfix = postfixDefinition.MakeGenericMethod(spriteType);
                bool patched = ApplyPatch(target, prefix: null, postfix: postfix);
                runtime.RuntimeMonitor.Log("Harmony SpriteOverrideHandler.TryGetModOverrideSprite postfix result: target=" + target + ", patched=" + patched + ".");
                return patched;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", $"Failed to patch SpriteOverrideHandler.TryGetModOverrideSprite postfix {targetTypeName}.{methodName}.", ex.ToString());
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
            object? prefixMethod = prefix == null ? null : Activator.CreateInstance(harmonyMethodType!, prefix);
            object? postfixMethod = postfix == null ? null : Activator.CreateInstance(harmonyMethodType!, postfix);
            object? finalizerMethod = finalizer == null ? null : Activator.CreateInstance(harmonyMethodType!, finalizer);
            MethodInfo? patch = harmonyType!.GetMethods()
                .FirstOrDefault(m => m.Name == "Patch" &&
                    m.GetParameters().Length == 5 &&
                    typeof(MethodBase).IsAssignableFrom(m.GetParameters()[0].ParameterType));
            if (patch == null)
                return false;

            object?[] args = new object?[] { target, prefixMethod, postfixMethod, null, finalizerMethod };
            patch.Invoke(harmony, args);
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

        private static bool AnimatorAssetTryLoadAssetPrefixGeneric<TAsset>(string __0, ref TAsset __1, ref bool __result) where TAsset : class
        {
            Func<string, AnimatorAssetLoadPrefixResult>? callback = animatorAssetTryLoadAssetCallback;
            if (callback == null)
                return true;

            AnimatorAssetLoadPrefixResult result = callback(__0);
            if (!result.Handled)
                return true;

            TAsset? asset = result.Asset as TAsset;
            __1 = asset!;
            __result = asset != null;
            return false;
        }

        private static void AnimalAIDefaultAnyStatePostfix(string __0, ref Type __result)
        {
            Func<string, Type?, Type?>? callback = animalAIDefaultAnyStateCallback;
            if (callback == null)
                return;

            Type? next = callback(__0, __result);
            if (next != null)
                __result = next;
        }

        private static bool AnimalAIMakeDecisionFreeTimePrefixGeneric<TTask>(object __instance, ref TTask __result) where TTask : class
        {
            Func<object?, AnimalAITaskPrefixResult>? callback = animalAIMakeDecisionFreeTimeCallback;
            if (callback == null)
                return true;

            AnimalAITaskPrefixResult result = callback(__instance);
            if (!result.Handled)
                return true;

            if (result.Task is TTask task)
            {
                __result = task;
                return false;
            }

            return true;
        }

        private static void SpriteOverrideTryGetModOverrideSpritePostfixGeneric<TSprite>(object __instance, string __0, ref TSprite __1, ref bool __result) where TSprite : class
        {
            Func<object?, string, object?, PngSpriteOverrideResult>? callback = spriteOverrideTryGetModOverrideSpriteCallback;
            if (callback == null)
                return;

            PngSpriteOverrideResult result = callback(__instance, __0, __1);
            if (!result.Handled)
                return;

            if (result.UseOverride && result.Sprite is TSprite sprite)
            {
                __1 = sprite;
                __result = true;
                return;
            }

            __1 = null!;
            __result = false;
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
            harmony = Activator.CreateInstance(harmonyType, ownerId);
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

        private static MethodInfo? FindAnimatorAssetTryLoadAssetTarget(Type? type, string name, Type runtimeAnimatorControllerType)
        {
            if (type == null || runtimeAnimatorControllerType == null)
                return null;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != name || method.ReturnType != typeof(bool))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;
                if (parameters[0].ParameterType != typeof(string))
                    continue;
                if (!parameters[1].ParameterType.IsByRef || parameters[1].ParameterType.GetElementType() != runtimeAnimatorControllerType)
                    continue;

                return method;
            }

            return null;
        }

        private static MethodInfo? FindAnimalAIDefaultAnyStateTarget(Type? type, string name)
        {
            if (type == null)
                return null;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != name || method.ReturnType != typeof(Type))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    return method;
            }

            return null;
        }

        private static MethodInfo? FindSpriteOverrideTryGetModOverrideSpriteTarget(Type? type, string name, Type spriteType)
        {
            if (type == null || spriteType == null)
                return null;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != name || method.ReturnType != typeof(bool))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;
                if (parameters[0].ParameterType != typeof(string))
                    continue;
                if (!parameters[1].ParameterType.IsByRef || parameters[1].ParameterType.GetElementType() != spriteType)
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

    internal readonly struct AnimatorAssetLoadPrefixResult
    {
        private AnimatorAssetLoadPrefixResult(bool handled, object? asset)
        {
            Handled = handled;
            Asset = asset;
        }

        public bool Handled { get; }

        public object? Asset { get; }

        public static AnimatorAssetLoadPrefixResult Unhandled() => new AnimatorAssetLoadPrefixResult(false, null);

        public static AnimatorAssetLoadPrefixResult HandledAsset(object? asset) => new AnimatorAssetLoadPrefixResult(true, asset);
    }

    internal readonly struct PngSpriteOverrideResult
    {
        private PngSpriteOverrideResult(bool handled, bool useOverride, object? sprite)
        {
            Handled = handled;
            UseOverride = useOverride;
            Sprite = sprite;
        }

        public bool Handled { get; }

        public bool UseOverride { get; }

        public object? Sprite { get; }

        public static PngSpriteOverrideResult Unhandled() => new PngSpriteOverrideResult(false, false, null);

        public static PngSpriteOverrideResult OverrideSprite(object sprite) => new PngSpriteOverrideResult(true, true, sprite);

        public static PngSpriteOverrideResult NoOverride() => new PngSpriteOverrideResult(true, false, null);
    }

    internal readonly struct AnimalAITaskPrefixResult
    {
        private AnimalAITaskPrefixResult(bool handled, object? task)
        {
            Handled = handled;
            Task = task;
        }

        public bool Handled { get; }

        public object? Task { get; }

        public static AnimalAITaskPrefixResult Continue() => new AnimalAITaskPrefixResult(false, null);

        public static AnimalAITaskPrefixResult UseTask(object task) => new AnimalAITaskPrefixResult(true, task);
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
