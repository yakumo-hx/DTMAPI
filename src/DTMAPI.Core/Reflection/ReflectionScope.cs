using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Reflection
{
    // BCL-only foundation shared by short-lived native reads and the owner facade.
    internal sealed class ReflectionScope : IDisposable
    {
        private readonly object gate = new object();
        private readonly List<WeakReference<ReflectionBinding>> bindings = new List<WeakReference<ReflectionBinding>>();
        private Action? accessGuard;
        private bool closed;

        internal ReflectionScope(Action? accessGuard = null) { this.accessGuard = accessGuard; }

        internal ReflectionValue<T> Field<T>(object target, string name) => BindValue<T>(TargetType(target), target, name, false, ReflectionMemberKind.Field, true)!;
        internal ReflectionValue<T> StaticField<T>(Type type, string name) => BindValue<T>(type, null, name, true, ReflectionMemberKind.Field, true)!;
        internal ReflectionValue<T> Property<T>(object target, string name) => BindValue<T>(TargetType(target), target, name, false, ReflectionMemberKind.Property, true)!;
        internal ReflectionValue<T> StaticProperty<T>(Type type, string name) => BindValue<T>(type, null, name, true, ReflectionMemberKind.Property, true)!;
        internal bool TryField<T>(object target, string name, out ReflectionValue<T>? value) =>
            (value = BindValue<T>(TargetType(target), target, name, false, ReflectionMemberKind.Field, false)) != null;
        internal bool TryStaticField<T>(Type type, string name, out ReflectionValue<T>? value) =>
            (value = BindValue<T>(type, null, name, true, ReflectionMemberKind.Field, false)) != null;
        internal bool TryProperty<T>(object target, string name, out ReflectionValue<T>? value) =>
            (value = BindValue<T>(TargetType(target), target, name, false, ReflectionMemberKind.Property, false)) != null;
        internal bool TryStaticProperty<T>(Type type, string name, out ReflectionValue<T>? value) =>
            (value = BindValue<T>(type, null, name, true, ReflectionMemberKind.Property, false)) != null;
        internal ReflectionMethod Method(object target, string name, params Type[] parameters) =>
            BindMethod(TargetType(target), target, name, false, parameters, true)!;
        internal ReflectionMethod StaticMethod(Type type, string name, params Type[] parameters) =>
            BindMethod(type, null, name, true, parameters, true)!;
        internal bool TryMethod(object target, string name, Type[] parameters, out ReflectionMethod? method) =>
            (method = BindMethod(TargetType(target), target, name, false, parameters, false)) != null;
        internal bool TryStaticMethod(Type type, string name, Type[] parameters, out ReflectionMethod? method) =>
            (method = BindMethod(type, null, name, true, parameters, false)) != null;

        private ReflectionValue<T>? BindValue<T>(Type type, object? target, string name, bool isStatic, ReflectionMemberKind kind, bool required)
        {
            Demand();
            MemberInfo? member = ReflectionMetadata.Find(type, name, isStatic, kind, Array.Empty<Type>());
            if (member == null)
            {
                if (required)
                {
                    if (kind == ReflectionMemberKind.Field) throw new MissingFieldException(type.FullName, name);
                    throw new MissingMemberException(type.FullName, name);
                }
                return null;
            }
            Type valueType = member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;
            if (!typeof(T).IsAssignableFrom(valueType)) throw new InvalidCastException("Declared member type " + valueType + " is not assignable to " + typeof(T) + ".");
            return Add(new ReflectionValue<T>(this, target, member, valueType));
        }

        private ReflectionMethod? BindMethod(Type type, object? target, string name, bool isStatic, Type[] parameters, bool required)
        {
            Demand();
            var method = (MethodInfo?)ReflectionMetadata.Find(type, name, isStatic, ReflectionMemberKind.Method, parameters);
            if (method == null) { if (required) throw new MissingMethodException(type.FullName, name); return null; }
            return Add(new ReflectionMethod(this, target, method));
        }

        private T Add<T>(T binding) where T : ReflectionBinding
        {
            lock (gate)
            {
                if (closed) { binding.Dispose(); throw new ObjectDisposedException(nameof(ReflectionScope)); }
                bindings.RemoveAll(reference => !reference.TryGetTarget(out _));
                bindings.Add(new WeakReference<ReflectionBinding>(binding));
                return binding;
            }
        }
        internal void Demand()
        {
            Action? guard;
            lock (gate) { if (closed) throw new ObjectDisposedException(nameof(ReflectionScope)); guard = accessGuard; }
            guard?.Invoke();
            lock (gate) { if (closed) throw new ObjectDisposedException(nameof(ReflectionScope)); }
        }
        internal (MemberInfo Member, object? Target) Acquire(ReflectionBinding binding)
        {
            lock (gate)
            {
                Demand();
                // Admission and closure share this lock. Admitted calls retain local roots and finish outside locks.
                return binding.Capture(this);
            }
        }
        internal void Remove(ReflectionBinding binding)
        {
            lock (gate) bindings.RemoveAll(reference => !reference.TryGetTarget(out ReflectionBinding live) || ReferenceEquals(live, binding));
        }
        private static Type TargetType(object target) => (target ?? throw new ArgumentNullException(nameof(target))).GetType();
        public void Dispose()
        {
            ReflectionBinding[] release;
            lock (gate)
            {
                if (closed) return;
                closed = true;
                accessGuard = null;
                release = bindings.Select(reference => reference.TryGetTarget(out ReflectionBinding live) ? live : null)
                    .Where(binding => binding != null).Cast<ReflectionBinding>().ToArray();
                bindings.Clear();
            }
            foreach (ReflectionBinding binding in release) binding.Dispose();
        }
    }

    internal abstract class ReflectionBinding : IDisposable
    {
        protected readonly object Gate = new object();
        private ReflectionScope? scope;
        protected object? Target;
        protected MemberInfo? Member;
        protected ReflectionBinding(ReflectionScope scope, object? target, MemberInfo member)
        { this.scope = scope; Target = target; Member = member; }
        protected (MemberInfo Member, object? Target) Acquire()
        {
            ReflectionScope? current;
            lock (Gate) current = scope;
            if (current == null) throw new ObjectDisposedException(GetType().Name);
            return current.Acquire(this);
        }
        internal (MemberInfo Member, object? Target) Capture(ReflectionScope expected)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(scope, expected)) throw new ObjectDisposedException(GetType().Name);
                return (Member!, Target);
            }
        }
        protected static void CheckValue(Type declared, object? value)
        {
            if (value == null ? declared.IsValueType && Nullable.GetUnderlyingType(declared) == null : !declared.IsInstanceOfType(value))
                throw new ArgumentException("Value is not assignable to declared type " + declared + ". No implicit conversions are performed.");
        }
        public void Dispose()
        {
            ReflectionScope? previous;
            lock (Gate) { previous = scope; scope = null; Target = null; Member = null; }
            previous?.Remove(this);
        }
    }

    internal sealed class ReflectionValue<T> : ReflectionBinding, IReflectedField<T>, IReflectedProperty<T>
    {
        private readonly Type valueType;
        internal ReflectionValue(ReflectionScope scope, object? target, MemberInfo member, Type valueType) : base(scope, target, member)
        { this.valueType = valueType; }
        public bool CanWrite
        {
            get { var access = Acquire(); return access.Target?.GetType().IsValueType != true && Writable(access.Member); }
        }
        private static bool Writable(MemberInfo member) => member is FieldInfo field ? !field.IsInitOnly && !field.IsLiteral : ((PropertyInfo)member).GetSetMethod(true) != null;
        public T GetValue()
        {
            var access = Acquire();
            object? value = access.Member is FieldInfo field ? field.GetValue(access.Target) : ((PropertyInfo)access.Member).GetValue(access.Target, null);
            return (T)value!;
        }
        public void SetValue(T value)
        {
            var access = Acquire();
            if (!Writable(access.Member)) throw new NotSupportedException("Member is readonly or has no setter.");
            if (access.Target?.GetType().IsValueType == true) throw new NotSupportedException("Value-type instance mutation is unsupported.");
            CheckValue(valueType, value);
            if (access.Member is FieldInfo field) field.SetValue(access.Target, value);
            else ((PropertyInfo)access.Member).SetValue(access.Target, value, null);
        }
    }

    internal sealed class ReflectionMethod : ReflectionBinding, IReflectedMethod
    {
        internal ReflectionMethod(ReflectionScope scope, object? target, MethodInfo member) : base(scope, target, member) { }
        public Type ReturnType => ((MethodInfo)Acquire().Member).ReturnType;
        public T Invoke<T>(params object?[] arguments)
        {
            var access = Acquire();
            var method = (MethodInfo)access.Member;
            if (method.ReturnType == typeof(void) || !typeof(T).IsAssignableFrom(method.ReturnType))
                throw new InvalidCastException("Declared return type is not assignable to " + typeof(T) + ".");
            return (T)Call(method, access.Target, arguments)!;
        }
        public void Invoke(params object?[] arguments)
        {
            var access = Acquire();
            var method = (MethodInfo)access.Member;
            if (method.ReturnType != typeof(void)) throw new NotSupportedException("Void Invoke requires a declared void return type.");
            Call(method, access.Target, arguments);
        }
        private object? Call(MethodInfo method, object? target, object?[] arguments)
        {
            if (target?.GetType().IsValueType == true) throw new NotSupportedException("Value-type instance invocation may mutate a boxed copy and is unsupported.");
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            arguments = (object?[])arguments.Clone();
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != arguments.Length) throw new ArgumentException("Exact argument count required; optional/params expansion is not supported.", nameof(arguments));
            for (int index = 0; index < arguments.Length; index++) CheckValue(parameters[index].ParameterType, arguments[index]);
            // Reflection retains TargetInvocationException.InnerException and its original target stack.
            return method.Invoke(target, arguments);
        }
    }
}
