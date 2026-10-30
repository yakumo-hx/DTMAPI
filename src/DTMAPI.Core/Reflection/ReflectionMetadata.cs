using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.Core.Reflection
{
    internal enum ReflectionMemberKind { Field, Property, Method }

    internal static class ReflectionMetadata
    {
        internal const int Capacity = 512;
        private static readonly object Gate = new object();
        private static readonly Dictionary<Key, MemberInfo> Cache = new Dictionary<Key, MemberInfo>();
        private static readonly Queue<Key> Order = new Queue<Key>();
        internal static int Count { get { lock (Gate) return Cache.Count; } }

        internal static MemberInfo? Find(Type type, string name, bool isStatic, ReflectionMemberKind kind, Type[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A member name is required.", nameof(name));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (type.ContainsGenericParameters || type.IsByRef || type.IsPointer) throw new NotSupportedException("Open or indirect target types are unsupported.");
            Type[] signature = (Type[])parameters.Clone();
            foreach (Type parameter in signature)
            {
                if (parameter == null) throw new ArgumentException("A parameter type is null.", nameof(parameters));
                if (parameter.IsByRef || parameter.IsPointer || parameter.ContainsGenericParameters)
                    throw new NotSupportedException("ref/out, pointer and open generic signatures are unsupported.");
            }
            var key = new Key(type, name, isStatic, kind, signature);
            lock (Gate)
            {
                if (Cache.TryGetValue(key, out MemberInfo member)) return member;
                MemberInfo? found = Resolve(key);
                // Types are immutable once created. Do not cache Missing or perform string-based assembly scans.
                if (found == null) return null;
                if (Cache.Count == Capacity) Cache.Remove(Order.Dequeue());
                Cache.Add(key, found);
                Order.Enqueue(key);
                return found;
            }
        }

        private static MemberInfo? Resolve(Key key)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                (key.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? level = key.Type; level != null; level = level.BaseType)
            {
                if (key.Kind == ReflectionMemberKind.Field)
                {
                    FieldInfo? field = level.GetField(key.Name, flags);
                    if (field != null) { ValidateValueType(field.FieldType); return field; }
                }
                else if (key.Kind == ReflectionMemberKind.Property)
                {
                    PropertyInfo[] matches = level.GetProperties(flags).Where(property => property.Name == key.Name).ToArray();
                    if (matches.Length > 1) throw new AmbiguousMatchException("Multiple property declarations match: " + string.Join("; ", matches.Select(member => member.ToString())));
                    if (matches.Length == 1)
                    {
                        PropertyInfo property = matches[0];
                        if (property.GetIndexParameters().Length != 0 || property.GetGetMethod(true) == null)
                            throw new NotSupportedException("Indexers and setter-only properties are unsupported.");
                        ValidateValueType(property.PropertyType);
                        return property;
                    }
                }
                else
                {
                    MethodInfo[] matches = level.GetMethods(flags).Where(method => method.Name == key.Name &&
                        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(key.Parameters)).ToArray();
                    if (matches.Length > 1) throw new AmbiguousMatchException("Multiple exact method signatures match: " + string.Join("; ", matches.Select(member => member.ToString())));
                    if (matches.Length == 1)
                    {
                        MethodInfo method = matches[0];
                        if (method.IsGenericMethod || method.ContainsGenericParameters || (method.CallingConvention & CallingConventions.VarArgs) != 0)
                            throw new NotSupportedException("Generic and varargs methods are unsupported.");
                        ValidateValueType(method.ReturnType);
                        return method;
                    }
                }
            }
            return null;
        }
        private static void ValidateValueType(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.ContainsGenericParameters)
                throw new NotSupportedException("Indirect or open generic member types are unsupported.");
        }

        private sealed class Key : IEquatable<Key>
        {
            internal readonly Type Type;
            internal readonly string Name;
            internal readonly bool IsStatic;
            internal readonly ReflectionMemberKind Kind;
            internal readonly Type[] Parameters;
            internal Key(Type type, string name, bool isStatic, ReflectionMemberKind kind, Type[] parameters)
            { Type = type; Name = name; IsStatic = isStatic; Kind = kind; Parameters = parameters; }
            public bool Equals(Key? other) => other != null && Type == other.Type && Name == other.Name &&
                IsStatic == other.IsStatic && Kind == other.Kind && Parameters.SequenceEqual(other.Parameters);
            public override bool Equals(object? obj) => obj is Key other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ((Type.GetHashCode() * 397) ^ Name.GetHashCode()) * 397 ^ (int)Kind;
                    hash = hash * 397 ^ IsStatic.GetHashCode();
                    foreach (Type parameter in Parameters) hash = hash * 397 ^ parameter.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
