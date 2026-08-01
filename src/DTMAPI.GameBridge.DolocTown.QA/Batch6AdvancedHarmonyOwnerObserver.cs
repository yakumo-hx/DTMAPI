using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal readonly struct Batch6HarmonyPatchTarget
    {
        internal Batch6HarmonyPatchTarget(string typeName, string methodName, int parameterCount, int expectedOwnerPatchCount = 1)
        {
            TypeName = typeName ?? string.Empty;
            MethodName = methodName ?? string.Empty;
            ParameterCount = parameterCount;
            ExpectedOwnerPatchCount = expectedOwnerPatchCount;
        }

        internal string TypeName { get; }
        internal string MethodName { get; }
        internal int ParameterCount { get; }
        internal int ExpectedOwnerPatchCount { get; }
        internal string DisplayName => TypeName + "." + MethodName + "/" + ParameterCount.ToString(CultureInfo.InvariantCulture);
    }

    internal sealed class Batch6HarmonyOwnerInventory
    {
        internal bool ProductAssemblyLoaded { get; set; }
        internal int ExpectedTargetCount { get; set; }
        internal int ResolvedTargetCount { get; set; }
        internal int ExactOwnerTargetCount { get; set; }
        internal int ExactOwnerPatchCount { get; set; }
        internal int ExpectedPatchCount { get; set; }
        internal string Details { get; set; } = string.Empty;

        internal bool IsComplete => ProductAssemblyLoaded &&
            ExpectedTargetCount > 0 &&
            ResolvedTargetCount == ExpectedTargetCount &&
            ExactOwnerTargetCount == ExpectedTargetCount &&
            ExactOwnerPatchCount == ExpectedPatchCount;
    }

    /// <summary>QA-only exact Harmony owner inventory over a reviewed target list.</summary>
    internal static class Batch6AdvancedHarmonyOwnerObserver
    {
        internal static int CountAllOwnerPatches(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("Harmony owner is required.", nameof(owner));
            Assembly[] harmonyAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(assembly.GetName().Name, "0Harmony", StringComparison.Ordinal))
                .ToArray();
            if (harmonyAssemblies.Length == 0)
                return 0;
            if (harmonyAssemblies.Length != 1)
                throw new InvalidOperationException("Expected at most one loaded 0Harmony assembly; observed " + harmonyAssemblies.Length.ToString(CultureInfo.InvariantCulture) + ".");
            Type harmonyType = harmonyAssemblies[0].GetType("HarmonyLib.Harmony", throwOnError: true, ignoreCase: false)
                ?? throw new TypeLoadException("HarmonyLib.Harmony is unavailable.");
            MethodInfo getAllPatchedMethods = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(method => method.Name == "GetAllPatchedMethods" && method.GetParameters().Length == 0);
            MethodInfo getPatchInfo = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(method => method.Name == "GetPatchInfo" && method.GetParameters().Length == 1);
            int count = 0;
            if (getAllPatchedMethods.Invoke(null, null) is IEnumerable methods)
            {
                foreach (object? method in methods)
                    if (method is MethodBase methodBase)
                        count += CountOwnerPatches(getPatchInfo.Invoke(null, new object[] { methodBase }), owner);
            }
            return count;
        }

        internal static int CountAllOwnerPatchesWithPrefix(
            string ownerPrefix)
        {
            if (string.IsNullOrWhiteSpace(ownerPrefix))
            {
                throw new ArgumentException(
                    "Harmony owner prefix is required.",
                    nameof(ownerPrefix));
            }
            Assembly[] harmonyAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(
                        assembly =>
                            string.Equals(
                                assembly.GetName().Name,
                                "0Harmony",
                                StringComparison.Ordinal))
                    .ToArray();
            if (harmonyAssemblies.Length == 0)
                return 0;
            if (harmonyAssemblies.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected at most one loaded 0Harmony assembly; observed " +
                    harmonyAssemblies.Length.ToString(
                        CultureInfo.InvariantCulture) +
                    ".");
            }

            Type harmonyType =
                harmonyAssemblies[0].GetType(
                    "HarmonyLib.Harmony",
                    throwOnError: true,
                    ignoreCase: false)
                ?? throw new TypeLoadException(
                    "HarmonyLib.Harmony is unavailable.");
            MethodInfo getAllPatchedMethods =
                harmonyType.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                    .Single(
                        method =>
                            method.Name ==
                                "GetAllPatchedMethods" &&
                            method.GetParameters().Length ==
                                0);
            MethodInfo getPatchInfo =
                harmonyType.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                    .Single(
                        method =>
                            method.Name ==
                                "GetPatchInfo" &&
                            method.GetParameters().Length ==
                                1);
            int count = 0;
            if (getAllPatchedMethods.Invoke(null, null)
                is IEnumerable methods)
            {
                foreach (object? method in methods)
                {
                    if (method is MethodBase methodBase)
                    {
                        count += CountOwnerPatchesWithPrefix(
                            getPatchInfo.Invoke(
                                null,
                                new object[]
                                {
                                    methodBase
                                }),
                            ownerPrefix);
                    }
                }
            }
            return count;
        }

        internal static Batch6HarmonyOwnerInventory Observe(
            string productAssemblyName,
            string owner,
            IReadOnlyList<Batch6HarmonyPatchTarget> targets)
        {
            if (string.IsNullOrWhiteSpace(productAssemblyName))
                throw new ArgumentException("Product assembly name is required.", nameof(productAssemblyName));
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("Harmony owner is required.", nameof(owner));
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("At least one reviewed Harmony target is required.", nameof(targets));

            var inventory = new Batch6HarmonyOwnerInventory
            {
                ExpectedTargetCount = targets.Count,
                ExpectedPatchCount = targets.Sum(target => target.ExpectedOwnerPatchCount)
            };
            Assembly? productAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, productAssemblyName, StringComparison.Ordinal));
            if (productAssembly == null)
            {
                inventory.Details = "productAssemblyLoaded=false";
                return inventory;
            }
            inventory.ProductAssemblyLoaded = true;

            AssemblyName harmonyReference = productAssembly.GetReferencedAssemblies()
                .SingleOrDefault(reference => string.Equals(reference.Name, "0Harmony", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Loaded product does not reference the receipt-bound 0Harmony assembly.");
            Assembly[] harmonyAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(assembly.GetName().Name, harmonyReference.Name, StringComparison.Ordinal))
                .ToArray();
            if (harmonyAssemblies.Length != 1)
                throw new InvalidOperationException("Expected one loaded 0Harmony assembly; observed " + harmonyAssemblies.Length.ToString(CultureInfo.InvariantCulture) + ".");
            Type harmonyType = harmonyAssemblies[0].GetType("HarmonyLib.Harmony", throwOnError: true, ignoreCase: false)
                ?? throw new TypeLoadException("HarmonyLib.Harmony is unavailable.");
            MethodInfo getPatchInfo = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(method => method.Name == "GetPatchInfo" && method.GetParameters().Length == 1);
            Assembly? gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
            if (gameAssembly == null)
            {
                inventory.Details = "Assembly-CSharp not loaded";
                return inventory;
            }

            var samples = new List<string>();
            foreach (Batch6HarmonyPatchTarget target in targets)
            {
                Type? type = gameAssembly.GetType(target.TypeName, throwOnError: false, ignoreCase: false);
                MethodBase? method = FindMethod(type, target.MethodName, target.ParameterCount);
                if (method == null)
                {
                    samples.Add(target.DisplayName + "=missing");
                    continue;
                }
                inventory.ResolvedTargetCount++;
                object? patchInfo = getPatchInfo.Invoke(null, new object[] { method });
                int ownerPatchCount = CountOwnerPatches(patchInfo, owner);
                inventory.ExactOwnerPatchCount += ownerPatchCount;
                if (ownerPatchCount == target.ExpectedOwnerPatchCount)
                    inventory.ExactOwnerTargetCount++;
                samples.Add(target.DisplayName + "=" + ownerPatchCount.ToString(CultureInfo.InvariantCulture));
            }
            inventory.Details = string.Join(";", samples.ToArray());
            return inventory;
        }

        private static MethodBase? FindMethod(Type? type, string name, int parameterCount)
        {
            if (name == ".ctor")
                return type?.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(candidate => candidate.GetParameters().Length == parameterCount);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? method = current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == parameterCount);
                if (method != null)
                    return method;
            }
            return null;
        }

        internal static int CountOwnerPatches(object? patchInfo, string owner)
        {
            if (patchInfo == null)
                return 0;
            int count = 0;
            foreach (string collectionName in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
            {
                if (!(ReadMember(patchInfo, collectionName) is IEnumerable patches))
                    continue;
                foreach (object? patch in patches)
                {
                    string patchOwner = Convert.ToString(ReadMember(patch, "owner") ?? ReadMember(patch, "Owner"), CultureInfo.InvariantCulture) ?? string.Empty;
                    if (string.Equals(patchOwner, owner, StringComparison.Ordinal))
                        count++;
                }
            }
            return count;
        }

        private static int CountOwnerPatchesWithPrefix(
            object? patchInfo,
            string ownerPrefix)
        {
            if (patchInfo == null)
                return 0;
            int count = 0;
            foreach (string collectionName in
                new[]
                {
                    "Prefixes",
                    "Postfixes",
                    "Transpilers",
                    "Finalizers"
                })
            {
                if (!(ReadMember(
                    patchInfo,
                    collectionName) is IEnumerable patches))
                {
                    continue;
                }
                foreach (object? patch in patches)
                {
                    string patchOwner =
                        Convert.ToString(
                            ReadMember(patch, "owner") ??
                            ReadMember(patch, "Owner"),
                            CultureInfo.InvariantCulture) ??
                        string.Empty;
                    if (patchOwner.StartsWith(
                        ownerPrefix,
                        StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static object? ReadMember(object? value, string name)
        {
            if (value == null)
                return null;
            Type type = value.GetType();
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value) ??
                type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value);
        }
    }
}
