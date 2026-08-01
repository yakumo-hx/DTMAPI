using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Loads the package-owned frozen-ABI executor bytes only after a frozen surface is called.
    /// GameBridge intentionally has no static assembly reference to the optional component.
    /// </summary>
    internal sealed class CompatibilityHostBroker
    {
        internal const string ComponentId = "gamebridge-compatibility-host";
        internal const string AssemblySimpleName = "DTMAPI.GameBridge.DolocTown.Compatibility";
        internal const string FactoryTypeName = "DTMAPI.GameBridge.DolocTown.CompatibilityHostFactory";
        internal const string FactoryMethodName = "Create";
        internal const string BackendMethodName = "GetService";
        internal const string ExpectedTargetFramework = ".NETStandard,Version=v2.0";
        internal const string ExpectedAssemblyVersion = "0.5.3.0";
        internal const string ExpectedRelativePath = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll";

        private static readonly ConditionalWeakTable<DtmApiRuntime, CompatibilityHostBroker> Brokers = new ConditionalWeakTable<DtmApiRuntime, CompatibilityHostBroker>();
        private static readonly object AssemblyLoadSync = new object();
        private static Assembly? brokerLoadedAssembly;
        private static string brokerLoadedSha256 = string.Empty;
        private readonly object sync = new object();
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, object> services = new Dictionary<string, object>(StringComparer.Ordinal);
        private object? backend;
        private Exception? loadFailure;

        private CompatibilityHostBroker(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            PublishInitialStatus();
        }

        private void PublishInitialStatus()
        {
            string status;
            string details;
            lock (AssemblyLoadSync)
            {
                if (brokerLoadedAssembly != null)
                {
                    status = "resident-dormant";
                    details = "brokerLoaded=true; services=0; demand=0; callbacks=0; hooks=0; process unload is not promised";
                }
                else
                {
                    bool reservedAssemblyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                        .Any(candidate => string.Equals(candidate.GetName().Name, AssemblySimpleName, StringComparison.OrdinalIgnoreCase));
                    status = reservedAssemblyLoaded ? "failed-closed-preloaded" : "dormant";
                    details = "loaded=" + reservedAssemblyLoaded.ToString().ToLowerInvariant() + "; services=0; demand=0; callbacks=0; hooks=0";
                }
            }

            runtime.SetHookStatus(
                "Compatibility.Host",
                status,
                "release-manifest.json OptionalComponents",
                details);
        }

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
                if (services.TryGetValue(serviceId, out object service))
                    return service;
                if (loadFailure != null)
                    throw new InvalidOperationException("The frozen compatibility host is unavailable; activation remains fail-closed.", loadFailure);

                try
                {
                    object preparedBackend = backend ?? LoadAndCreateBackend();
                    MethodInfo method = FindRequiredMethod(preparedBackend.GetType(), BackendMethodName, 2, isStatic: false);
                    object? created = InvokeUnwrapped(method, preparedBackend, new object[] { serviceId, constructionArguments ?? Array.Empty<object>() });
                    if (created == null)
                        throw new InvalidDataException("The frozen compatibility host returned no service for '" + serviceId + "'.");

                    backend = preparedBackend;
                    RegisterRelatedServices(
                        preparedBackend,
                        method,
                        serviceId);
                    services.Add(serviceId, created);
                    runtime.SetHookStatus(
                        "Compatibility.Host",
                        "resident",
                        FactoryTypeName + "." + FactoryMethodName,
                        "The dormant-shipped compatibility assembly is process-resident; service=" + serviceId + ". No unload is promised.");
                    return created;
                }
                catch (Exception ex)
                {
                    loadFailure = ex;
                    runtime.SetHookStatus(
                        "Compatibility.Host",
                        "failed-closed",
                        "release-manifest.json OptionalComponents",
                        ex.GetType().Name + ": " + ex.Message + "; demand=0; callbacks=0; hooks=0");
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge.Compatibility", "Frozen compatibility host activation failed closed.", ex.ToString());
                    throw new InvalidOperationException("The frozen compatibility host could not be activated; no compatibility Hook or demand was installed.", ex);
                }
            }
        }

        private void RegisterRelatedServices(
            object preparedBackend,
            MethodInfo getService,
            string requestedServiceId)
        {
            if (!string.Equals(
                    requestedServiceId,
                    "DebugConsole",
                    StringComparison.Ordinal) ||
                services.ContainsKey("DebugActions"))
            {
                return;
            }

            object? actionLifecycleOwner =
                InvokeUnwrapped(
                    getService,
                    preparedBackend,
                    new object[]
                    {
                        "DebugActions",
                        Array.Empty<object>()
                    });
            if (actionLifecycleOwner == null)
            {
                throw new InvalidDataException(
                    "The frozen DebugConsole service did not expose its required DebugActions lifecycle owner.");
            }

            services.Add(
                "DebugActions",
                actionLifecycleOwner);
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
            OptionalComponentReceipt receipt = ReadReceipt();
            ValidateReceipt(receipt);
            string path = ResolveComponentPath(receipt.RelativePath);
            byte[] bytes = ValidateFile(path, receipt.Length, receipt.Sha256);

            Assembly assembly;
            lock (AssemblyLoadSync)
            {
                if (brokerLoadedAssembly != null)
                {
                    if (!string.Equals(brokerLoadedSha256, receipt.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The process-resident compatibility assembly does not match the current package receipt.");
                    assembly = brokerLoadedAssembly;
                }
                else
                {
                    Assembly[] collisions = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(candidate => string.Equals(candidate.GetName().Name, AssemblySimpleName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (collisions.Length != 0)
                        throw new InvalidOperationException("A compatibility assembly with the reserved identity was already loaded outside the package broker.");

                    ValidatePreLoadIdentity(bytes, receipt);
                    Assembly loaded = Assembly.Load(bytes);
                    ValidateLoadedIdentity(loaded, receipt);
                    brokerLoadedAssembly = loaded;
                    brokerLoadedSha256 = receipt.Sha256;
                    assembly = loaded;
                }
            }
            ValidateLoadedIdentity(assembly, receipt);
            Type factoryType = assembly.GetType(FactoryTypeName, throwOnError: false, ignoreCase: false)
                ?? throw new InvalidDataException("The validated compatibility assembly does not expose its single factory type.");
            MethodInfo factory = FindRequiredMethod(factoryType, FactoryMethodName, 1, isStatic: true);
            object? created = InvokeUnwrapped(factory, null, new object[] { runtime });
            return created ?? throw new InvalidDataException("The validated compatibility factory returned no backend.");
        }

        private OptionalComponentReceipt ReadReceipt()
        {
            string path = Path.Combine(runtime.Paths.DtmApiPath, "release-manifest.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("The installed release manifest required by the compatibility broker is missing.", path);
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ReleaseManifest));
                    ReleaseManifest manifest = serializer.ReadObject(stream) as ReleaseManifest
                        ?? throw new InvalidDataException("The installed release manifest did not contain an object.");
                    OptionalComponentReceipt[] candidates = (manifest.OptionalComponents ?? Array.Empty<OptionalComponentReceipt>())
                        .Where(item => item != null && string.Equals(item.ComponentId, ComponentId, StringComparison.Ordinal))
                        .ToArray();
                    if (candidates.Length != 1)
                        throw new InvalidDataException("The installed release manifest must contain exactly one '" + ComponentId + "' optional component receipt.");
                    return candidates[0];
                }
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("The installed release manifest is unreadable or malformed.", ex);
            }
        }

        private static void ValidateReceipt(OptionalComponentReceipt receipt)
        {
            if (!string.Equals(receipt.ComponentId, ComponentId, StringComparison.Ordinal) ||
                !string.Equals(receipt.Distribution, "dormant-shipped", StringComparison.Ordinal) ||
                !string.Equals(receipt.LoadPolicy, "first-frozen-abi-call", StringComparison.Ordinal) ||
                !string.Equals(receipt.DefaultLoadState, "dormant", StringComparison.Ordinal) ||
                !receipt.IncludedInDownloadPackage)
            {
                throw new InvalidDataException("The compatibility optional-component policy does not match the dormant-shipped authority.");
            }
            if (!string.Equals(receipt.AssemblyName, AssemblySimpleName, StringComparison.Ordinal) ||
                !string.Equals(receipt.AssemblyVersion, ExpectedAssemblyVersion, StringComparison.Ordinal) ||
                !string.Equals(receipt.TargetFramework, "netstandard2.0", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The compatibility optional-component assembly identity or target framework is invalid.");
            }
            if (!string.Equals(receipt.RelativePath, ExpectedRelativePath, StringComparison.Ordinal))
                throw new InvalidDataException("The compatibility optional-component path does not match the frozen dormant-shipped topology.");
            if (string.IsNullOrWhiteSpace(receipt.AssemblyVersion) || receipt.Length <= 0 || !IsSha256(receipt.Sha256))
                throw new InvalidDataException("The compatibility optional-component receipt is incomplete.");
        }

        private string ResolveComponentPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new InvalidDataException("The compatibility optional-component path must be relative to the game root.");
            string root = Path.GetFullPath(runtime.Paths.GamePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string resolved = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            string requiredPrefix = root + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The compatibility optional-component path escaped the game root.");
            return resolved;
        }

        private static byte[] ValidateFile(string path, long expectedLength, string expectedSha256)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("The dormant-shipped compatibility assembly is missing.", path);
            var info = new FileInfo(path);
            if (info.Length != expectedLength)
                throw new InvalidDataException("The compatibility assembly length does not match its release-manifest receipt.");
            byte[] bytes = File.ReadAllBytes(path);
            string actual;
            using (SHA256 sha = SHA256.Create())
                actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
            if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The compatibility assembly SHA-256 does not match its release-manifest receipt.");
            return bytes;
        }

        private static void ValidateLoadedIdentity(Assembly assembly, OptionalComponentReceipt receipt)
        {
            AssemblyName identity = assembly.GetName();
            if (!string.Equals(identity.Name, AssemblySimpleName, StringComparison.Ordinal) ||
                !string.Equals(identity.Version?.ToString() ?? string.Empty, receipt.AssemblyVersion, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The loaded compatibility assembly identity does not match its receipt.");
            }
            TargetFrameworkAttribute? target = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            if (!string.Equals(target?.FrameworkName ?? string.Empty, ExpectedTargetFramework, StringComparison.Ordinal))
                throw new InvalidDataException("The loaded compatibility assembly is not netstandard2.0.");
        }

        private static void ValidatePreLoadIdentity(byte[] bytes, OptionalComponentReceipt receipt)
        {
            PortableAssemblyMetadata metadata;
            try
            {
                metadata = PortableAssemblyReferenceInspector.Inspect(bytes);
            }
            catch (ManagedModClassificationException ex)
            {
                throw new InvalidDataException("The compatibility assembly metadata could not be validated before load.", ex);
            }

            if (!string.Equals(metadata.AssemblyName, AssemblySimpleName, StringComparison.Ordinal) ||
                !string.Equals(metadata.AssemblyName, receipt.AssemblyName, StringComparison.Ordinal) ||
                !string.Equals(metadata.AssemblyVersion, ExpectedAssemblyVersion, StringComparison.Ordinal) ||
                !string.Equals(metadata.AssemblyVersion, receipt.AssemblyVersion, StringComparison.Ordinal) ||
                !string.Equals(metadata.TargetFramework, ExpectedTargetFramework, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The compatibility assembly name, version, or target framework failed pre-load validation.");
            }
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

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;
            return value.All(character => (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'));
        }

        [DataContract]
        private sealed class ReleaseManifest
        {
            [DataMember(Name = "OptionalComponents")]
            internal OptionalComponentReceipt[] OptionalComponents { get; set; } = Array.Empty<OptionalComponentReceipt>();
        }

        [DataContract]
        private sealed class OptionalComponentReceipt
        {
            [DataMember(Name = "ComponentId")] internal string ComponentId { get; set; } = string.Empty;
            [DataMember(Name = "Distribution")] internal string Distribution { get; set; } = string.Empty;
            [DataMember(Name = "LoadPolicy")] internal string LoadPolicy { get; set; } = string.Empty;
            [DataMember(Name = "RelativePath")] internal string RelativePath { get; set; } = string.Empty;
            [DataMember(Name = "Length")] internal long Length { get; set; }
            [DataMember(Name = "Sha256")] internal string Sha256 { get; set; } = string.Empty;
            [DataMember(Name = "AssemblyName")] internal string AssemblyName { get; set; } = string.Empty;
            [DataMember(Name = "AssemblyVersion")] internal string AssemblyVersion { get; set; } = string.Empty;
            [DataMember(Name = "FileVersion")] internal string FileVersion { get; set; } = string.Empty;
            [DataMember(Name = "TargetFramework")] internal string TargetFramework { get; set; } = string.Empty;
            [DataMember(Name = "DefaultLoadState")] internal string DefaultLoadState { get; set; } = string.Empty;
            [DataMember(Name = "IncludedInDownloadPackage")] internal bool IncludedInDownloadPackage { get; set; }
        }
    }
}
