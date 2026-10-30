using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;

namespace DTMAPI.BepInExBootstrap
{
    internal static class QaHostActivationLoader
    {
        internal const string ActivationFileName = "qa-host-activation.json";
        internal const string HostDirectoryName = "qa-host";
        internal const string SettingsFileName = "qa-settings.json";
        internal const string AssemblyFileName = "DTMAPI.GameBridge.DolocTown.QA.dll";

        internal static PreparedQaHost? LoadIfRequested(DtmApiRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            string activationPath = Path.Combine(runtime.Paths.DtmApiPath, ActivationFileName);
            if (!File.Exists(activationPath))
                return null;

            QaHostActivationReceipt receipt = ReadReceipt(activationPath);
            ValidateReceiptHeader(receipt, runtime);

            string hostRoot = Path.GetFullPath(Path.Combine(runtime.Paths.DtmApiPath, HostDirectoryName));
            string runRoot = Path.GetFullPath(Path.Combine(hostRoot, receipt.RunId));
            string requiredPrefix = hostRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!runRoot.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("QA host run directory escaped its fixed root.");

            string dllPath = Path.Combine(runRoot, AssemblyFileName);
            string settingsPath = Path.Combine(runRoot, SettingsFileName);
            byte[] settingsBytes = ValidateFile(settingsPath, receipt.SettingsLength, receipt.SettingsSha256, "settings");
            byte[] dllBytes = ValidateFile(dllPath, receipt.DllLength, receipt.DllSha256, "assembly");

            Assembly[] existingAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(item => string.Equals(item.GetName().Name, QaHostProtocol.AssemblySimpleName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (existingAssemblies.Length != 0)
                throw new InvalidOperationException("A QA host assembly with the reserved identity is already loaded; refusing ambiguous activation.");

            // Load the exact bytes that were covered by the activation receipt. Loading
            // from the path here would reopen a hash-check/load time-of-check gap.
            Assembly assembly = Assembly.Load(dllBytes);
            ValidateLoadedAssemblyIdentity(assembly, receipt);
            Type? factoryType = assembly.GetType(QaHostProtocol.FactoryTypeName, throwOnError: false, ignoreCase: false);
            if (factoryType == null || !typeof(IQaHostFactory).IsAssignableFrom(factoryType))
                throw new InvalidDataException("The validated QA assembly does not expose the required factory contract.");

            var factory = Activator.CreateInstance(factoryType, nonPublic: true) as IQaHostFactory;
            if (factory == null)
                throw new InvalidDataException("The validated QA factory could not be created.");
            if (factory.ProtocolVersion != QaHostProtocol.ProtocolVersion)
                throw new InvalidDataException("The QA factory protocol version does not match the Runtime contract.");
            if (!string.Equals(factory.SupportedRuntimeReleaseVersion, DtmApiRuntime.ApiVersion, StringComparison.Ordinal))
                throw new InvalidDataException("The QA factory Runtime release version does not match the active Runtime.");

            var context = new QaHostPreparationContext(runtime, receipt.RunId, runRoot, settingsBytes);
            GameBridgeFixtureStartupOptions options = factory.PrepareStartupOptions(context)
                ?? throw new InvalidDataException("The QA factory returned no startup options.");
            if (!string.Equals(options.RunId, receipt.RunId, StringComparison.Ordinal))
                throw new InvalidDataException("The QA startup options runId does not match the activation receipt.");

            return new PreparedQaHost(factory, context, options);
        }

        private static QaHostActivationReceipt ReadReceipt(string path)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(QaHostActivationReceipt));
                    return serializer.ReadObject(stream) as QaHostActivationReceipt
                        ?? throw new InvalidDataException("QA activation receipt did not contain an object.");
                }
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("QA activation receipt is unreadable or malformed.", ex);
            }
        }

        private static void ValidateReceiptHeader(QaHostActivationReceipt receipt, DtmApiRuntime runtime)
        {
            if (receipt.SchemaVersion != QaHostProtocol.SchemaVersion)
                throw new InvalidDataException("Unsupported QA activation schemaVersion " + receipt.SchemaVersion + ".");
            if (receipt.ProtocolVersion != QaHostProtocol.ProtocolVersion)
                throw new InvalidDataException("Unsupported QA activation protocolVersion " + receipt.ProtocolVersion + ".");
            if (!IsRunId(receipt.RunId))
                throw new InvalidDataException("QA activation runId must contain exactly 32 hexadecimal characters.");
            if (!string.Equals(receipt.RuntimeReleaseVersion, DtmApiRuntime.ApiVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA activation Runtime release version mismatch.");
            if (!string.Equals(receipt.RuntimeBinaryVersion, DtmApiRuntime.BinaryVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA activation Runtime binary version mismatch.");

            string runtimeAssemblyVersion = typeof(DtmApiRuntime).Assembly.GetName().Version?.ToString() ?? string.Empty;
            if (!string.Equals(receipt.RuntimeAssemblyVersion, runtimeAssemblyVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA activation Runtime assembly compatibility version mismatch.");
            if (!string.Equals(receipt.AssemblyName, QaHostProtocol.AssemblySimpleName, StringComparison.Ordinal) ||
                !string.Equals(receipt.FactoryTypeName, QaHostProtocol.FactoryTypeName, StringComparison.Ordinal))
                throw new InvalidDataException("QA activation assembly/factory identity mismatch.");
        }

        private static byte[] ValidateFile(string path, long expectedLength, string expectedSha256, string kind)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("QA host " + kind + " file is missing.", path);
            var info = new FileInfo(path);
            if (expectedLength <= 0 || info.Length != expectedLength)
                throw new InvalidDataException("QA host " + kind + " length does not match its activation receipt.");
            if (!IsSha256(expectedSha256))
                throw new InvalidDataException("QA host " + kind + " receipt SHA-256 is invalid.");

            byte[] bytes = File.ReadAllBytes(path);
            string actualHash;
            using (SHA256 sha = SHA256.Create())
                actualHash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
            if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("QA host " + kind + " SHA-256 does not match its activation receipt.");
            return bytes;
        }

        private static void ValidateLoadedAssemblyIdentity(Assembly assembly, QaHostActivationReceipt receipt)
        {
            AssemblyName identity = assembly.GetName();
            if (!string.Equals(identity.Name, QaHostProtocol.AssemblySimpleName, StringComparison.Ordinal))
                throw new InvalidDataException("QA host AssemblyName is not the reserved identity.");
            string assemblyVersion = identity.Version?.ToString() ?? string.Empty;
            if (!string.Equals(assemblyVersion, receipt.DllAssemblyVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA host AssemblyVersion does not match its activation receipt.");

            string fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty;
            string productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
            if (!string.Equals(fileVersion, receipt.DllFileVersion, StringComparison.Ordinal) ||
                !string.Equals(productVersion, receipt.DllProductVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA host FileVersion/ProductVersion does not match its activation receipt.");
            if (!string.Equals(receipt.DllFileVersion, DtmApiRuntime.BinaryVersion, StringComparison.Ordinal) ||
                !string.Equals(receipt.DllProductVersion, DtmApiRuntime.ApiVersion, StringComparison.Ordinal))
                throw new InvalidDataException("QA host binary/release version does not match the active Runtime.");
        }

        private static bool IsRunId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f') || (current >= 'A' && current <= 'F')))
                    return false;
            }
            return true;
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f') || (current >= 'A' && current <= 'F')))
                    return false;
            }
            return true;
        }

        [DataContract]
        private sealed class QaHostActivationReceipt
        {
            [DataMember(Name = "schemaVersion")]
            internal int SchemaVersion { get; set; }

            [DataMember(Name = "protocolVersion")]
            internal int ProtocolVersion { get; set; }

            [DataMember(Name = "runId")]
            internal string RunId { get; set; } = string.Empty;

            [DataMember(Name = "runtimeReleaseVersion")]
            internal string RuntimeReleaseVersion { get; set; } = string.Empty;

            [DataMember(Name = "runtimeBinaryVersion")]
            internal string RuntimeBinaryVersion { get; set; } = string.Empty;

            [DataMember(Name = "runtimeAssemblyVersion")]
            internal string RuntimeAssemblyVersion { get; set; } = string.Empty;

            [DataMember(Name = "assemblyName")]
            internal string AssemblyName { get; set; } = string.Empty;

            [DataMember(Name = "factoryTypeName")]
            internal string FactoryTypeName { get; set; } = string.Empty;

            [DataMember(Name = "dllLength")]
            internal long DllLength { get; set; }

            [DataMember(Name = "dllSha256")]
            internal string DllSha256 { get; set; } = string.Empty;

            [DataMember(Name = "dllAssemblyVersion")]
            internal string DllAssemblyVersion { get; set; } = string.Empty;

            [DataMember(Name = "dllFileVersion")]
            internal string DllFileVersion { get; set; } = string.Empty;

            [DataMember(Name = "dllProductVersion")]
            internal string DllProductVersion { get; set; } = string.Empty;

            [DataMember(Name = "settingsLength")]
            internal long SettingsLength { get; set; }

            [DataMember(Name = "settingsSha256")]
            internal string SettingsSha256 { get; set; } = string.Empty;
        }
    }
}
