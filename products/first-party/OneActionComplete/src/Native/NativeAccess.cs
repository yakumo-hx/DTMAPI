using System;
using System.Reflection;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal static class NativeAccess
    {
        internal static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch { }
            }
            return null;
        }

        internal static object? TryGetDungeonResourceFromCollider(object collider)
        {
            Type? rendererType = ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (rendererType == null)
                return null;
            foreach (MethodInfo method in collider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != "GetComponent" || !method.IsGenericMethodDefinition || method.GetParameters().Length != 0)
                    continue;
                object? renderer = method.MakeGenericMethod(rendererType).Invoke(collider, null);
                return renderer == null ? null : ReadMember(renderer, "DungeonResource");
            }
            return null;
        }

        internal static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                try
                {
                    FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (field != null)
                        return field.GetValue(instance);
                    PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (property != null)
                        return property.GetValue(instance);
                }
                catch { return null; }
            }
            return null;
        }

        internal static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            try
            {
                return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                    ?? type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            }
            catch { return null; }
        }

        internal static int ReadInt(object instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            try { return value == null ? fallback : Convert.ToInt32(value); }
            catch { return fallback; }
        }

        internal static double ReadDouble(object instance, string name, double fallback)
        {
            object? value = ReadMember(instance, name);
            try { return value == null ? fallback : Convert.ToDouble(value); }
            catch { return fallback; }
        }

        internal static bool ReadBool(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        internal static string ReadString(object instance, string name) => ReadMember(instance, name) as string ?? string.Empty;

        internal static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (method.Name == name && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }

        internal static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        internal static bool IsRemoved(object resource) => ReadBool(resource, "IsRemoved", false);
        internal static string ResourceName(object resource) => ReadString(resource, "ResourceName") is string value && value.Length > 0 ? value : resource.GetType().Name;

        internal static string ResourceClass(object resource)
        {
            object? proto = ReadMember(resource, "Proto");
            return proto == null ? string.Empty : ReadMember(proto, "ResourceClass")?.ToString() ?? string.Empty;
        }

        internal static string FormatRatio(double value) => value < 0 ? "unknown" : value.ToString("0.###");
    }
}
