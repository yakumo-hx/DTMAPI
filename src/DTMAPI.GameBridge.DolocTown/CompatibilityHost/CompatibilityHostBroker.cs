using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Loads the existing MES cold-recovery component only when saved data needs recovery.
    /// GameBridge intentionally has no static assembly reference to the optional component.
    /// </summary>
    internal sealed class CompatibilityHostBroker
    {
        internal const string ComponentId = "gamebridge-compatibility-host";
        internal const string AssemblySimpleName = "DTMAPI.GameBridge.DolocTown.Compatibility";
        internal const string FactoryTypeName = "DTMAPI.GameBridge.DolocTown.CompatibilityHostFactory";
        internal const string FactoryMethodName = "Create";
        internal const string BackendMethodName = "GetService";
        internal const string ExpectedRelativePath = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll";

        private static readonly ConditionalWeakTable<DtmApiRuntime, CompatibilityHostBroker> Brokers = new ConditionalWeakTable<DtmApiRuntime, CompatibilityHostBroker>();
        private static readonly object AssemblyLoadSync = new object();
        private static Assembly? brokerLoadedAssembly;
        private readonly object sync = new object();
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, object> services = new Dictionary<string, object>(StringComparer.Ordinal);
        private object? backend;

        private CompatibilityHostBroker(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            PublishInitialStatus();
        }

        private void PublishInitialStatus() =>
            runtime.SetHookStatus("Compatibility.Host", brokerLoadedAssembly == null ? "dormant" : "resident-dormant",
                "MES cold recovery", "The recovery component loads only when existing saved equipment needs it.");

        internal static CompatibilityHostBroker For(DtmApiRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            return Brokers.GetValue(runtime, value => new CompatibilityHostBroker(value));
        }

        internal bool IsLoaded
        {
            get
            {
                lock (sync)
                    return backend != null;
            }
        }

        internal object GetService(string serviceId, params object[] constructionArguments)
        {
            lock (sync)
            {
                if (services.TryGetValue(serviceId, out object service)) return service;
                object preparedBackend = backend ?? LoadAndCreateBackend();
                MethodInfo method = FindRequiredMethod(preparedBackend.GetType(), BackendMethodName, 2, isStatic: false);
                object created = InvokeUnwrapped(method, preparedBackend, new object[] { serviceId, constructionArguments ?? Array.Empty<object>() })
                    ?? throw new InvalidDataException("Recovery component returned no service for " + serviceId + ".");
                backend = preparedBackend;
                services.Add(serviceId, created);
                runtime.SetHookStatus("Compatibility.Host", "resident", FactoryTypeName + "." + FactoryMethodName,
                    "MES recovery service=" + serviceId + "; process-resident.");
                return created;
            }
        }

        internal bool TryGetService(string serviceId, out object? service)
        {
            lock (sync)
                return services.TryGetValue(serviceId, out service);
        }

        internal TDelegate? BindIfServiceLoaded<TDelegate>(string serviceId, string methodName)
            where TDelegate : class
        {
            return TryGetService(serviceId, out object? service) && service != null
                ? Bind<TDelegate>(service, methodName)
                : null;
        }

        private object LoadAndCreateBackend()
        {
            // Fixed package-local component; the CLR and the called factory establish
            // whether it can execute. Release receipts remain distribution evidence.
            string path = Path.Combine(runtime.Paths.GamePath, "DTMAPI", "components", "compatibility", AssemblySimpleName + ".dll");
            Assembly assembly;
            lock (AssemblyLoadSync)
            {
                assembly = brokerLoadedAssembly ?? Assembly.LoadFrom(path);
                brokerLoadedAssembly = assembly;
            }
            Type factoryType = assembly.GetType(FactoryTypeName, throwOnError: true, ignoreCase: false)!;
            MethodInfo factory = FindRequiredMethod(factoryType, FactoryMethodName, 1, isStatic: true);
            return InvokeUnwrapped(factory, null, new object[] { runtime })
                ?? throw new InvalidDataException("Recovery component factory returned no backend.");
        }

        internal static object? Invoke(object target, string methodName, params object?[] arguments)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            arguments ??= Array.Empty<object>();
            MethodInfo method = FindCompatibleMethod(target.GetType(), methodName, arguments);
            return InvokeUnwrapped(method, target, arguments);
        }

        internal static T Invoke<T>(object target, string methodName, params object?[] arguments)
        {
            object? result = Invoke(target, methodName, arguments);
            return result == null ? default! : (T)result;
        }

        internal static T ReadProperty<T>(object target, string propertyName, T fallback = default!)
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object? value = property?.GetValue(target);
            return value is T typed ? typed : fallback;
        }

        internal static TDelegate Bind<TDelegate>(object target, string methodName)
            where TDelegate : class
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            Type delegateType = typeof(TDelegate);
            MethodInfo invoke = delegateType.GetMethod("Invoke")
                ?? throw new InvalidOperationException(delegateType.FullName + " is not a delegate type.");
            ParameterInfo[] expected = invoke.GetParameters();
            MethodInfo[] matches = target.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal) || method.Name.EndsWith("." + methodName, StringComparison.Ordinal))
                .Where(method => method.ReturnType == invoke.ReturnType)
                .Where(method => ParametersMatch(method.GetParameters(), expected))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidDataException("Compatibility delegate method '" + target.GetType().FullName + "." + methodName + "' was missing or ambiguous.");
            return (TDelegate)(object)Delegate.CreateDelegate(delegateType, target, matches[0], throwOnBindFailure: true);
        }

        private static bool ParametersMatch(ParameterInfo[] actual, ParameterInfo[] expected)
        {
            if (actual.Length != expected.Length)
                return false;
            for (int index = 0; index < actual.Length; index++)
            {
                if (actual[index].ParameterType != expected[index].ParameterType)
                    return false;
            }
            return true;
        }

        private static MethodInfo FindRequiredMethod(Type type, string name, int parameterCount, bool isStatic)
        {
            MethodInfo[] matches = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance))
                .Where(method => method.IsStatic == isStatic && method.Name.Equals(name, StringComparison.Ordinal) && method.GetParameters().Length == parameterCount)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidDataException("Compatibility protocol method '" + type.FullName + "." + name + "' was missing or ambiguous.");
            return matches[0];
        }

        private static MethodInfo FindCompatibleMethod(Type type, string name, object?[] arguments)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(method => (method.Name.Equals(name, StringComparison.Ordinal) || method.Name.EndsWith("." + name, StringComparison.Ordinal)) && method.GetParameters().Length == arguments.Length)
                .Where(method => ParametersAccept(method.GetParameters(), arguments))
                .ToArray();
            if (methods.Length == 0)
                throw new MissingMethodException(type.FullName, name);
            return methods.OrderByDescending(method => method.Name.Equals(name, StringComparison.Ordinal)).First();
        }

        private static bool ParametersAccept(ParameterInfo[] parameters, object?[] arguments)
        {
            for (int index = 0; index < parameters.Length; index++)
            {
                Type parameterType = parameters[index].ParameterType;
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;
                object? argument = arguments[index];
                if (argument == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null && !parameters[index].IsOut)
                        return false;
                    continue;
                }
                if (!parameterType.IsInstanceOfType(argument))
                    return false;
            }
            return true;
        }

        private static object? InvokeUnwrapped(MethodInfo method, object? target, object?[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }
    }
}
