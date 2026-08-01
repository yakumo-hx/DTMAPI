using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DTMAPI.DebugConsole
{
    internal static partial class DebugConsoleNativeAccess
    {
        internal static Type? Resolve(string qualifiedName)
        {
            Type? type = Type.GetType(qualifiedName, false);
            if (type != null)
                return type;
            string fullName = qualifiedName.Split(',')[0].Trim();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }
            return null;
        }

        internal static object? Read(object? instance, string name)
        {
            if (instance == null || string.IsNullOrWhiteSpace(name))
                return null;
            Type type = instance as Type ?? instance.GetType();
            object? target = instance is Type ? null : instance;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                (target == null ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, flags);
                if (property != null)
                    return property.GetValue(target, null);
                FieldInfo? field = current.GetField(name, flags);
                if (field != null)
                    return field.GetValue(target);
            }
            return null;
        }

        internal static bool Write(object? instance, string name, object? value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(name))
                return false;
            Type type = instance as Type ?? instance.GetType();
            object? target = instance is Type ? null : instance;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                (target == null ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, flags);
                if (property?.CanWrite == true)
                {
                    property.SetValue(target, ConvertValue(value, property.PropertyType), null);
                    return true;
                }
                FieldInfo? field = current.GetField(name, flags);
                if (field != null)
                {
                    field.SetValue(target, ConvertValue(value, field.FieldType));
                    return true;
                }
            }
            return false;
        }

        internal static MethodInfo? Method(
            Type? type,
            string name,
            int parameterCount,
            bool? isStatic = null)
        {
            if (type == null)
                return null;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static;
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? found = current.GetMethods(flags)
                    .FirstOrDefault(method =>
                        method.Name.Equals(name, StringComparison.Ordinal) &&
                        method.GetParameters().Length == parameterCount &&
                        (!isStatic.HasValue || method.IsStatic == isStatic.Value));
                if (found != null)
                    return found;
            }
            return null;
        }

        internal static object? Invoke(
            object? target,
            string name,
            params object?[] arguments)
        {
            Type? type = target as Type ?? target?.GetType();
            MethodInfo? method = Method(
                type,
                name,
                arguments?.Length ?? 0,
                target is Type ? true : (bool?)null);
            return method?.Invoke(target is Type ? null : target, arguments);
        }

        internal static IEnumerable<object> Enumerate(object? value)
        {
            if (value is string || !(value is IEnumerable enumerable))
                yield break;
            foreach (object? item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        internal static object? Table(string name)
        {
            Type? config = Resolve("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = Read(config, "Tables");
            return Read(tables, name);
        }

        internal static object? TableList(string name)
        {
            object? table = Table(name);
            return Read(table, "DataList") ?? Read(table, "TotalProtos");
        }

        internal static string Text(object? value, string name) =>
            Read(value, name)?.ToString() ?? string.Empty;

        internal static string First(params string[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ??
            string.Empty;

        internal static int Int(object? value, string name, int fallback = 0)
        {
            try
            {
                object? raw = Read(value, name);
                return raw == null ? fallback : Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        internal static double Double(object? value, string name, double fallback = 0)
        {
            try
            {
                object? raw = Read(value, name);
                return raw == null ? fallback : Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        internal static bool Bool(object? value, string name, bool fallback = false)
        {
            try
            {
                object? raw = Read(value, name);
                return raw == null ? fallback : Convert.ToBoolean(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        internal static object? Archive
        {
            get
            {
                Type? api = Resolve("DolocAPI, Assembly-CSharp");
                return Read(api, "archiveHandle");
            }
        }

        internal static object? CurrentRoom =>
            Read(Resolve("DolocAPI, Assembly-CSharp"), "CurrentRoom");

        internal static object? AgentPosition =>
            Read(Resolve("DolocAPI, Assembly-CSharp"), "AgentPosition");

        internal static double Vector(object? value, string component) =>
            Double(value, component, 0);

        internal static object? Vector2Int(int x, int y)
        {
            Type? type = Resolve("UnityEngine.Vector2Int, UnityEngine.CoreModule") ??
                Resolve("UnityEngine.Vector2Int, UnityEngine");
            return type == null ? null : Activator.CreateInstance(type, x, y);
        }

        internal static int Count(object? enumerable) =>
            Enumerate(enumerable).Count();

        internal static bool Add(object collection, object value)
        {
            MethodInfo? add = collection.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    method.Name == "Add" &&
                    method.GetParameters().Length == 1);
            if (add == null)
                return false;
            object? converted = ConvertValue(
                value,
                add.GetParameters()[0].ParameterType);
            object? result = add.Invoke(collection, new[] { converted });
            return !(result is bool added) || added;
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            Type nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (value == null)
                return nonNullable.IsValueType ? Activator.CreateInstance(nonNullable) : null;
            if (nonNullable.IsInstanceOfType(value))
                return value;
            if (nonNullable.IsEnum)
                return value is string text
                    ? Enum.Parse(nonNullable, text, true)
                    : Enum.ToObject(nonNullable, value);
            return Convert.ChangeType(value, nonNullable, CultureInfo.InvariantCulture);
        }
    }
}
