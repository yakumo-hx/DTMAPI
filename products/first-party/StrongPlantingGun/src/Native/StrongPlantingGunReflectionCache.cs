using System;
using System.Collections.Generic;
using System.Reflection;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed class StrongPlantingGunReflectionCache
    {
        private readonly Dictionary<MemberKey, MemberInfo?>
            members =
                new Dictionary<MemberKey, MemberInfo?>();

        internal int Count => members.Count;

        internal int ResolutionCount { get; private set; }

        internal object? ReadMember(
            object instance,
            string name)
        {
            if (instance == null)
                return null;
            MemberInfo? member = GetMember(
                instance.GetType(),
                name,
                MemberKind.InstanceValue,
                0);
            try
            {
                if (member is FieldInfo field)
                    return field.GetValue(instance);
                if (member is PropertyInfo property)
                    return property.GetValue(instance);
            }
            catch
            {
            }
            return null;
        }

        internal object? ReadStaticMember(
            Type type,
            string name)
        {
            if (type == null)
                return null;
            MemberInfo? member = GetMember(
                type,
                name,
                MemberKind.StaticValue,
                0);
            try
            {
                if (member is FieldInfo field)
                    return field.GetValue(null);
                if (member is PropertyInfo property)
                    return property.GetValue(null);
            }
            catch
            {
            }
            return null;
        }

        internal int ReadInt(
            object instance,
            string name,
            int fallback)
        {
            object? value = ReadMember(instance, name);
            if (value is int result)
                return result;
            if (value is long longValue)
                return (int)longValue;
            return value is short shortValue
                ? shortValue
                : fallback;
        }

        internal bool ReadBool(
            object instance,
            string name,
            bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        internal string ReadString(
            object instance,
            string name) =>
            ReadMember(instance, name) as string ??
            string.Empty;

        internal MethodInfo? GetMethod(
            Type type,
            string name,
            int parameterCount) =>
            GetMember(
                type,
                name,
                MemberKind.InstanceMethod,
                parameterCount) as MethodInfo;

        internal MethodInfo? GetInteractionMethod(
            Type type,
            string name) =>
            GetMember(
                type,
                name,
                MemberKind.InteractionMethod,
                2) as MethodInfo;

        internal bool TrySetCapacity(
            object function,
            int capacity)
        {
            MemberInfo? member = GetMember(
                function.GetType(),
                "Capacity",
                MemberKind.CapacityWriter,
                0);
            if (member == null)
                return false;
            if (member is FieldInfo field)
                field.SetValue(function, capacity);
            else if (member is PropertyInfo property)
                property.SetValue(function, capacity);
            else
                return false;
            return true;
        }

        internal int ReadCapacity(object function) =>
            ReadInt(function, "Capacity", 0);

        internal bool TryWriteInt(
            object instance,
            string name,
            int value)
        {
            if (instance == null)
                return false;
            MemberInfo? member = GetMember(
                instance.GetType(),
                name,
                MemberKind.InstanceValue,
                0);
            if (member is FieldInfo field &&
                field.FieldType == typeof(int))
            {
                field.SetValue(instance, value);
                return true;
            }
            if (member is PropertyInfo property &&
                property.CanWrite &&
                property.PropertyType == typeof(int))
            {
                property.SetValue(instance, value);
                return true;
            }
            return false;
        }

        internal void Clear()
        {
            members.Clear();
            ResolutionCount = 0;
        }

        private MemberInfo? GetMember(
            Type type,
            string name,
            MemberKind kind,
            int parameterCount)
        {
            var key = new MemberKey(
                type,
                name,
                kind,
                parameterCount);
            if (members.TryGetValue(key, out MemberInfo? cached))
                return cached;

            ResolutionCount++;
            MemberInfo? resolved = Resolve(
                type,
                name,
                kind,
                parameterCount);
            members[key] = resolved;
            return resolved;
        }

        private static MemberInfo? Resolve(
            Type type,
            string name,
            MemberKind kind,
            int parameterCount)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                const BindingFlags instance =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance;
                const BindingFlags staticValue =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static;
                if (kind == MemberKind.StaticValue)
                {
                    FieldInfo? field = current.GetField(
                        name,
                        staticValue);
                    if (field != null)
                        return field;
                    PropertyInfo? property = current.GetProperty(
                        name,
                        staticValue);
                    if (property != null)
                        return property;
                    continue;
                }
                if (kind == MemberKind.InstanceValue)
                {
                    FieldInfo? field = current.GetField(
                        name,
                        instance);
                    if (field != null)
                        return field;
                    PropertyInfo? property = current.GetProperty(
                        name,
                        instance);
                    if (property != null)
                        return property;
                    continue;
                }
                if (kind == MemberKind.CapacityWriter)
                {
                    FieldInfo? field = current.GetField(
                        "<Capacity>k__BackingField",
                        instance);
                    if (field != null &&
                        field.FieldType == typeof(int))
                    {
                        return field;
                    }
                    PropertyInfo? property = current.GetProperty(
                        "Capacity",
                        instance);
                    if (property != null &&
                        property.CanWrite &&
                        property.PropertyType == typeof(int))
                    {
                        return property;
                    }
                    continue;
                }

                foreach (MethodInfo method in current.GetMethods(
                    instance))
                {
                    if (method.Name != name)
                        continue;
                    ParameterInfo[] parameters =
                        method.GetParameters();
                    if (parameters.Length != parameterCount)
                        continue;
                    if (kind == MemberKind.InteractionMethod &&
                        (parameters.Length != 2 ||
                         parameters[0].ParameterType.IsArray))
                    {
                        continue;
                    }
                    return method;
                }
            }
            return null;
        }

        private enum MemberKind : byte
        {
            InstanceValue,
            StaticValue,
            InstanceMethod,
            InteractionMethod,
            CapacityWriter
        }

        private readonly struct MemberKey :
            IEquatable<MemberKey>
        {
            private readonly Type type;
            private readonly string name;
            private readonly MemberKind kind;
            private readonly int parameterCount;

            internal MemberKey(
                Type type,
                string name,
                MemberKind kind,
                int parameterCount)
            {
                this.type = type;
                this.name = name;
                this.kind = kind;
                this.parameterCount = parameterCount;
            }

            public bool Equals(MemberKey other) =>
                ReferenceEquals(type, other.type) &&
                name == other.name &&
                kind == other.kind &&
                parameterCount == other.parameterCount;

            public override bool Equals(object? obj) =>
                obj is MemberKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = type.GetHashCode();
                    hash = (hash * 397) ^ name.GetHashCode();
                    hash = (hash * 397) ^ (int)kind;
                    hash = (hash * 397) ^ parameterCount;
                    return hash;
                }
            }
        }
    }
}
