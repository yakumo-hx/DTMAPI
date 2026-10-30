using System;

namespace DTMAPI.Abstractions
{
    /// <summary>Exact BCL reflection over caller-owned objects. Calls execute synchronously on the caller's thread.
    /// Dispose wrappers promptly. Owner closure invalidates retained wrappers. This is not a security sandbox or a Unity lifetime check.</summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Internal candidate; public release requires the complete M3 gate.")]
    public interface IReflectionHelper
    {
        IReflectedField<T> GetField<T>(object target, string name);
        IReflectedField<T> GetStaticField<T>(Type type, string name);
        bool TryGetField<T>(object target, string name, out IReflectedField<T>? field);
        bool TryGetStaticField<T>(Type type, string name, out IReflectedField<T>? field);
        IReflectedProperty<T> GetProperty<T>(object target, string name);
        IReflectedProperty<T> GetStaticProperty<T>(Type type, string name);
        bool TryGetProperty<T>(object target, string name, out IReflectedProperty<T>? property);
        bool TryGetStaticProperty<T>(Type type, string name, out IReflectedProperty<T>? property);
        IReflectedMethod GetMethod(object target, string name, Type[] parameterTypes);
        IReflectedMethod GetStaticMethod(Type type, string name, Type[] parameterTypes);
        bool TryGetMethod(object target, string name, Type[] parameterTypes, out IReflectedMethod? method);
        bool TryGetStaticMethod(Type type, string name, Type[] parameterTypes, out IReflectedMethod? method);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IReflectedField<T> : IDisposable
    {
        bool CanWrite { get; }
        T GetValue();
        void SetValue(T value);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IReflectedProperty<T> : IDisposable
    {
        bool CanWrite { get; }
        T GetValue();
        void SetValue(T value);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IReflectedMethod : IDisposable
    {
        Type ReturnType { get; }
        T Invoke<T>(params object?[] arguments);
        void Invoke(params object?[] arguments);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public static class DtmHelperExtensions
    {
        public static IReflectionHelper GetReflection(this IDtmHelper helper) =>
            helper.GetRequiredService<IReflectionHelper>("0.6.2", "0.6.2");
    }
}
