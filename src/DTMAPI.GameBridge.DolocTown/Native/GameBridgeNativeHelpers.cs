using System;
using System.Collections;
using System.Collections.Generic;
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

        internal static void ShowNativeSmallMessage(string message, bool error)
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                string methodName = error ? "ShowMessageBoxSmallErr" : "ShowMessageBoxSmall";
                MethodInfo? method = null;
                foreach (MethodInfo candidate in dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static) ?? Array.Empty<MethodInfo>())
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (candidate.Name == methodName && parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
                    {
                        method = candidate;
                        break;
                    }
                }

                if (method == null)
                    return;

                ParameterInfo[] methodParameters = method.GetParameters();
                object?[] args = methodParameters.Length == 1
                    ? new object?[] { message }
                    : methodParameters.Length == 2
                        ? new object?[] { message, 1.2f }
                        : new object?[] { message, 1.2f, false };
                method.Invoke(null, args);
            }
            catch
            {
            }
        }

        internal static double ReadAnimatorSpeed(object animator, double fallback)
        {
            object? value = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animator);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        internal static bool TryWriteAnimatorSpeed(object animator, double value)
        {
            try
            {
                PropertyInfo? speed = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance);
                if (speed == null || !speed.CanWrite)
                    return false;
                speed.SetValue(animator, Convert.ChangeType(value, speed.PropertyType));
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(1, value));
        }

        internal static double ClampSeconds(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return min;
            return Math.Min(max, Math.Max(min, value));
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

        internal static bool ReadStaticBoolMember(Type? type, string name, bool fallback)
        {
            object? value = ReadStaticMember(type, name);
            return value is bool result ? result : fallback;
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

        internal static bool WriteBoolMember(object instance, string name, bool value)
        {
            return WriteMember(instance, name, typeof(bool), value);
        }

        internal static bool WriteFloatMember(object instance, string name, float value)
        {
            return WriteMember(instance, name, typeof(float), value);
        }

        private static bool WriteMember(object instance, string name, Type expectedType, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == expectedType)
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == expectedType)
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        internal static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
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

        internal static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value == null)
                yield break;
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }
    }
}
