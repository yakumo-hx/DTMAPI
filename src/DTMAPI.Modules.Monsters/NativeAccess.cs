using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DTMAPI.Modules.Monsters
{
    internal static class NativeAccess
    {
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        internal static FieldInfo Field(Type type, string name)
        {
            if (!fields.TryGetValue(type, out var named)) fields.Add(type, named = new Dictionary<string, FieldInfo>(StringComparer.Ordinal));
            if (named.TryGetValue(name, out var value)) return value;
            for (Type? current = type; current != null; current = current.BaseType)
            {
                value = current.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (value != null) { named.Add(name, value); return value; }
            }
            throw new MissingFieldException(type.FullName, name);
        }
        internal static object? Get(object value, string name) => Field(value.GetType(), name).GetValue(value);
        internal static T Get<T>(object value, string name) => (T)Get(value, name)!;
        internal static void Set(object value, string name, object? data) => Field(value.GetType(), name).SetValue(value, data);
        internal static void SetProperty(object value, string name, object? data) => Set(value, "<" + name + ">k__BackingField", data);
        internal static void ClearCollection(object value, string name) => Get(value, name)?.GetType().GetMethod("Clear", Type.EmptyTypes)!.Invoke(Get(value, name), null);
        internal static IDictionary Dictionary(object value, string name) => Get<IDictionary>(value, name);
    }
}
