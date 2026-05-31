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

        public HarmonyReflectionPatcher(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool TryPatchPrefix(string targetTypeName, string methodName, MethodInfo? prefix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: prefix, postfix: null, parameterCount: parameterCount);
        }

        public bool TryPatchPostfix(string targetTypeName, string methodName, MethodInfo? postfix, int? parameterCount = null)
        {
            return TryPatch(targetTypeName, methodName, prefix: null, postfix: postfix, parameterCount: parameterCount);
        }

        public bool TryPatchConstructorPostfix(string targetTypeName, MethodInfo? postfix, int? parameterCount = null)
        {
            try
            {
                if (postfix == null || !EnsureHarmony())
                    return false;

                Type? targetType = FindType(targetTypeName);
                ConstructorInfo? target = FindConstructor(targetType, parameterCount);
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
            try
            {
                if ((prefix == null && postfix == null) || !EnsureHarmony())
                    return false;

                Type? targetType = FindType(targetTypeName);
                MethodInfo? target = FindTarget(targetType, methodName, parameterCount);
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
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name != name)
                    continue;
                if (parameterCount.HasValue && method.GetParameters().Length != parameterCount.Value)
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
            if (type == null)
                return null;
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (parameterCount.HasValue && constructor.GetParameters().Length != parameterCount.Value)
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
}
