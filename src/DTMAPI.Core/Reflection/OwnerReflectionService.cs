using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Reflection
{
    internal sealed class OwnerReflectionService : IReflectionHelper, IDisposable
    {
        private readonly ReflectionScope scope;
        internal OwnerReflectionService(Action ensureOwnerAlive) { scope = new ReflectionScope(ensureOwnerAlive); }
        public IReflectedField<T> GetField<T>(object target, string name) => scope.Field<T>(target, name);
        public IReflectedField<T> GetStaticField<T>(Type type, string name) => scope.StaticField<T>(type, name);
        public bool TryGetField<T>(object target, string name, out IReflectedField<T>? field)
        { bool found = scope.TryField<T>(target, name, out var value); field = value; return found; }
        public bool TryGetStaticField<T>(Type type, string name, out IReflectedField<T>? field)
        { bool found = scope.TryStaticField<T>(type, name, out var value); field = value; return found; }
        public IReflectedProperty<T> GetProperty<T>(object target, string name) => scope.Property<T>(target, name);
        public IReflectedProperty<T> GetStaticProperty<T>(Type type, string name) => scope.StaticProperty<T>(type, name);
        public bool TryGetProperty<T>(object target, string name, out IReflectedProperty<T>? property)
        { bool found = scope.TryProperty<T>(target, name, out var value); property = value; return found; }
        public bool TryGetStaticProperty<T>(Type type, string name, out IReflectedProperty<T>? property)
        { bool found = scope.TryStaticProperty<T>(type, name, out var value); property = value; return found; }
        public IReflectedMethod GetMethod(object target, string name, Type[] parameterTypes) => scope.Method(target, name, parameterTypes);
        public IReflectedMethod GetStaticMethod(Type type, string name, Type[] parameterTypes) => scope.StaticMethod(type, name, parameterTypes);
        public bool TryGetMethod(object target, string name, Type[] parameterTypes, out IReflectedMethod? method)
        { bool found = scope.TryMethod(target, name, parameterTypes, out var value); method = value; return found; }
        public bool TryGetStaticMethod(Type type, string name, Type[] parameterTypes, out IReflectedMethod? method)
        { bool found = scope.TryStaticMethod(type, name, parameterTypes, out var value); method = value; return found; }
        public void Dispose() => scope.Dispose();
    }
}
