using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DTMAPI.DebugConsole
{
    internal static partial class DebugConsoleNativeAccess
    {
        private static readonly ConcurrentDictionary<string, Type> ResolvedTypes =
            new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<NativeMemberKey, MemberInfo>
            Members = new ConcurrentDictionary<NativeMemberKey, MemberInfo>();
        private static readonly ConcurrentDictionary<NativeMethodKey, MethodInfo>
            Methods = new ConcurrentDictionary<NativeMethodKey, MethodInfo>();

        internal static Type? Resolve(string qualifiedName)
        {
            if (ResolvedTypes.TryGetValue(qualifiedName, out Type? cached))
                return cached;
            Type? type = Type.GetType(qualifiedName, false);
            if (type != null)
            {
                ResolvedTypes.TryAdd(qualifiedName, type);
                return type;
            }
            string fullName = qualifiedName.Split(',')[0].Trim();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    ResolvedTypes.TryAdd(qualifiedName, type);
                    return type;
                }
            }
            return null;
        }

        internal static object? Read(object? instance, string name)
        {
            if (instance == null || string.IsNullOrWhiteSpace(name))
                return null;
            Type type = instance as Type ?? instance.GetType();
            object? target = instance is Type ? null : instance;
            var cacheKey = new NativeMemberKey(
                type,
                name,
                target == null,
                writable: false);
            if (Members.TryGetValue(cacheKey, out MemberInfo? cached))
                return cached is PropertyInfo cachedProperty
                    ? cachedProperty.GetValue(target, null)
                    : ((FieldInfo)cached).GetValue(target);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                (target == null ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, flags);
                if (property != null)
                {
                    Members.TryAdd(cacheKey, property);
                    return property.GetValue(target, null);
                }
                FieldInfo? field = current.GetField(name, flags);
                if (field != null)
                {
                    Members.TryAdd(cacheKey, field);
                    return field.GetValue(target);
                }
            }
            return null;
        }

        internal static bool Write(object? instance, string name, object? value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(name))
                return false;
            Type type = instance as Type ?? instance.GetType();
            object? target = instance is Type ? null : instance;
            var cacheKey = new NativeMemberKey(
                type,
                name,
                target == null,
                writable: true);
            if (Members.TryGetValue(cacheKey, out MemberInfo? cached))
            {
                if (cached is PropertyInfo cachedProperty)
                    cachedProperty.SetValue(target, ConvertValue(value, cachedProperty.PropertyType), null);
                else
                {
                    var cachedField = (FieldInfo)cached;
                    cachedField.SetValue(target, ConvertValue(value, cachedField.FieldType));
                }
                return true;
            }
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                (target == null ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, flags);
                if (property?.CanWrite == true)
                {
                    Members.TryAdd(cacheKey, property);
                    property.SetValue(target, ConvertValue(value, property.PropertyType), null);
                    return true;
                }
                FieldInfo? field = current.GetField(name, flags);
                if (field != null)
                {
                    Members.TryAdd(cacheKey, field);
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
            var cacheKey = new NativeMethodKey(
                type,
                name,
                parameterCount,
                isStatic);
            if (Methods.TryGetValue(cacheKey, out MethodInfo? cached))
                return cached;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static;
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? found = null;
                foreach (MethodInfo candidate in current.GetMethods(flags))
                {
                    if (candidate.Name.Equals(name, StringComparison.Ordinal) &&
                        candidate.GetParameters().Length == parameterCount &&
                        (!isStatic.HasValue ||
                            candidate.IsStatic == isStatic.Value))
                    {
                        found = candidate;
                        break;
                    }
                }
                if (found != null)
                {
                    Methods.TryAdd(cacheKey, found);
                    return found;
                }
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

        internal static string TextFirst(
            object? value,
            string name1,
            string name2)
        {
            string first = Text(value, name1);
            return string.IsNullOrWhiteSpace(first)
                ? Text(value, name2)
                : first;
        }

        internal static string TextFirst(
            object? value,
            string name1,
            string name2,
            string name3)
        {
            string first = Text(value, name1);
            if (!string.IsNullOrWhiteSpace(first))
                return first;
            string second = Text(value, name2);
            return string.IsNullOrWhiteSpace(second)
                ? Text(value, name3)
                : second;
        }

        internal static string First(params string[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ??
            string.Empty;

        internal static string First(string value1, string value2) =>
            FirstCore(value1) ?? FirstCore(value2) ?? string.Empty;

        internal static string First(
            string value1,
            string value2,
            string value3) =>
            FirstCore(value1) ?? FirstCore(value2) ?? FirstCore(value3) ??
            string.Empty;

        internal static string First(
            string value1,
            string value2,
            string value3,
            string value4) =>
            FirstCore(value1) ?? FirstCore(value2) ?? FirstCore(value3) ??
            FirstCore(value4) ?? string.Empty;

        internal static string First(
            string value1,
            string value2,
            string value3,
            string value4,
            string value5) =>
            FirstCore(value1) ?? FirstCore(value2) ?? FirstCore(value3) ??
            FirstCore(value4) ?? FirstCore(value5) ?? string.Empty;

        private static string? FirstCore(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

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

        internal static object? Vector2(object? source, Type? targetType)
        {
            if (source == null ||
                targetType == null ||
                !string.Equals(
                    targetType.FullName,
                    "UnityEngine.Vector2",
                    StringComparison.Ordinal))
            {
                return null;
            }

            try
            {
                object? rawX = Read(source, "x");
                object? rawY = Read(source, "y");
                if (rawX == null || rawY == null)
                    return null;
                float x = Convert.ToSingle(rawX, CultureInfo.InvariantCulture);
                float y = Convert.ToSingle(rawY, CultureInfo.InvariantCulture);
                return Activator.CreateInstance(targetType, x, y);
            }
            catch
            {
                return null;
            }
        }

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

        private readonly struct NativeMemberKey : IEquatable<NativeMemberKey>
        {
            internal NativeMemberKey(
                Type type,
                string name,
                bool isStatic,
                bool writable)
            {
                Type = type;
                Name = name;
                IsStatic = isStatic;
                Writable = writable;
            }

            private Type Type { get; }
            private string Name { get; }
            private bool IsStatic { get; }
            private bool Writable { get; }

            public bool Equals(NativeMemberKey other) =>
                ReferenceEquals(Type, other.Type) &&
                IsStatic == other.IsStatic &&
                Writable == other.Writable &&
                string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object? obj) =>
                obj is NativeMemberKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (Type.GetHashCode() * 397) ^
                        StringComparer.Ordinal.GetHashCode(Name);
                    hash = (hash * 397) ^ IsStatic.GetHashCode();
                    return (hash * 397) ^ Writable.GetHashCode();
                }
            }
        }

        private readonly struct NativeMethodKey : IEquatable<NativeMethodKey>
        {
            internal NativeMethodKey(
                Type type,
                string name,
                int parameterCount,
                bool? isStatic)
            {
                Type = type;
                Name = name;
                ParameterCount = parameterCount;
                StaticKind = isStatic.HasValue
                    ? (isStatic.Value ? 1 : 0)
                    : -1;
            }

            private Type Type { get; }
            private string Name { get; }
            private int ParameterCount { get; }
            private int StaticKind { get; }

            public bool Equals(NativeMethodKey other) =>
                ReferenceEquals(Type, other.Type) &&
                ParameterCount == other.ParameterCount &&
                StaticKind == other.StaticKind &&
                string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object? obj) =>
                obj is NativeMethodKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (Type.GetHashCode() * 397) ^
                        StringComparer.Ordinal.GetHashCode(Name);
                    hash = (hash * 397) ^ ParameterCount;
                    return (hash * 397) ^ StaticKind;
                }
            }
        }
    }
}
