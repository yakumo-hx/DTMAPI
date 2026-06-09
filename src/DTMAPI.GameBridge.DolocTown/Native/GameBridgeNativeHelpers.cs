using System;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class GameBridgeNativeHelpers
    {
        internal static object? TryGetDungeonResourceFromCollider(object collider)
        {
            Type? rendererType = ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (rendererType == null)
                return null;

            MethodInfo? getComponent = null;
            foreach (MethodInfo method in collider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == "GetComponent" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)
                {
                    getComponent = method.MakeGenericMethod(rendererType);
                    break;
                }
            }

            object? renderer = getComponent?.Invoke(collider, null);
            return renderer?.GetType().GetProperty("DungeonResource", BindingFlags.Public | BindingFlags.Instance)?.GetValue(renderer);
        }

        internal static bool IsResourceRemoved(object resource)
        {
            object? value = resource.GetType().GetProperty("IsRemoved", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value is bool removed && removed;
        }

        internal static string GetResourceName(object resource)
        {
            object? value = resource.GetType().GetProperty("ResourceName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value as string ?? resource.GetType().Name;
        }

        internal static string GetResourceClass(object resource)
        {
            object? proto = resource.GetType().GetProperty("Proto", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            object? resourceClass = proto?.GetType().GetProperty("ResourceClass", BindingFlags.Public | BindingFlags.Instance)?.GetValue(proto);
            return resourceClass?.ToString() ?? string.Empty;
        }

        internal static string FormatRatio(double value)
        {
            return value < 0 ? "unknown" : value.ToString("0.###");
        }

        internal static int ReadIntMember(object instance, string name, int fallback)
        {
            Type type = instance.GetType();
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value == null ? fallback : Convert.ToInt32(value);
        }

        internal static double ReadDoubleMember(object instance, string name, double fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToDouble(value);
        }

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
                catch
                {
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

        internal static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                try
                {
                    object? value = field.GetValue(null);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
            {
                try
                {
                    return property.GetValue(null);
                }
                catch
                {
                }
            }
            return null;
        }

        internal static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        internal static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        internal static string ReadStringMember(object instance, string name, string fallback)
        {
            string value = ReadStringMember(instance, name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        internal static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        internal static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object? value = field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value = property.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }

        internal static MethodInfo? FindMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.Name == name && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }
    }
}
