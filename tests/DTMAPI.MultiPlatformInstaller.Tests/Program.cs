using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DTMAPI.MultiPlatformInstaller.Tests;

internal static partial class Program
{
    private static int _failures;

    private static int Main()
    {
        Run(nameof(AtomicReceiptValidationFailureRestoresPreviousBytes),
            AtomicReceiptValidationFailureRestoresPreviousBytes);
        Run(nameof(CurrentRuntimeVersionIsBoundToBuildAuthority), CurrentRuntimeVersionIsBoundToBuildAuthority);
        Run(nameof(CurrentModuleInventoryMatchesInstalledScripts), CurrentModuleInventoryMatchesInstalledScripts);
        Run(nameof(CurrentInstalledHelpersRequireTheirCommonModules), CurrentInstalledHelpersRequireTheirCommonModules);
        Run(nameof(DiagnosticActionsDoNotRequireAnIntactPackage), DiagnosticActionsDoNotRequireAnIntactPackage);
        Run(nameof(CompleteUninstallRemovesOwnedContentAndIsRepeatable), CompleteUninstallRemovesOwnedContentAndIsRepeatable);
        Run(nameof(LegacyEquipmentRequiresExplicitConsent), LegacyEquipmentRequiresExplicitConsent);
        Run(nameof(UninstallTerminatesBothTransactionFamilies), UninstallTerminatesBothTransactionFamilies);
        Run(nameof(UninstallRestoresForeignOriginalAndPreservesAmbiguousBackup), UninstallRestoresForeignOriginalAndPreservesAmbiguousBackup);
        Run(nameof(CustomRuntimeRequiresOwnership), CustomRuntimeRequiresOwnership);

        if (_failures > 0)
        {
            Console.Error.WriteLine("DTMAPI multi-platform installer tests failed: " + _failures);
            return 1;
        }

        Console.WriteLine("DTMAPI multi-platform installer tests passed.");
        return 0;
    }

    private static Type PackageLayoutType() => Assembly.LoadFrom(
        Path.Combine(AppContext.BaseDirectory, "DTMAPI-MultiPlatform-Installer.dll"))
        .GetType("DTMAPI.MultiPlatformInstaller.PackageLayout", throwOnError: true)!;

    private static void CurrentModuleInventoryMatchesInstalledScripts()
    {
        var files = (IReadOnlyDictionary<string, string>)PackageLayoutType().GetField("RuntimeModuleFiles",
            BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        string script = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "common", "files.ps1");
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
            Convert.ToBase64String(Encoding.Unicode.GetBytes(". '" + script.Replace("'", "''") +
                "'; @(Get-DtmApiRuntimeModuleFile) | ConvertTo-Json -Compress")) })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Installed script inventory exceeded 30 seconds.");
        }
        if (process.ExitCode != 0) throw new InvalidDataException("Installed script inventory failed: " + error.GetAwaiter().GetResult());
        using var inventory = JsonDocument.Parse(output.GetAwaiter().GetResult());
        var expected = inventory.RootElement.EnumerateArray()
            .ToDictionary(row => row.GetProperty("RelativePath").GetString()!, row => row.GetProperty("Id").GetString()!);
        if (files.Count != expected.Count || files.Any(pair => !expected.TryGetValue(pair.Key, out var id) || id != pair.Value))
            throw new InvalidDataException("Installer component inventory disagrees with the installed status and installation scripts.");
    }

    private static string CurrentRuntimeVersion() => XDocument.Load(Path.Combine(FindRepositoryRoot(),
        "tools", "release", "dtmapi-runtime-version.props")).Root!.Element("PropertyGroup")!
        .Element("DtmApiReleaseVersion")!.Value;

    private static void CurrentRuntimeVersionIsBoundToBuildAuthority()
    {
        MethodInfo validate = PackageLayoutType().GetMethod("ValidateRuntimeVersion",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        XElement authority = XDocument.Load(Path.Combine(FindRepositoryRoot(), "tools", "release",
            "dtmapi-runtime-version.props")).Root!.Element("PropertyGroup")!;
        string version = authority.Element("DtmApiReleaseVersion")!.Value;
        string binary = authority.Element("DtmApiBinaryFileVersion")!.Value;
        foreach (object[] accepted in new[]
        {
            new object[] { 1, "0.6.1", "0.6.1.0" },
            new object[] { 2, "0.7.0", "0.7.0.0" },
            new object[] { 2, version, binary }
        })
            validate.Invoke(null, accepted);
        foreach (object[] rejected in new[]
        {
            new object[] { 1, version, binary },
            new object[] { 2, "0.6.1", "0.6.1.0" },
            new object[] { 2, version, "0.0.0.0" },
            new object[] { 2, "9.9.9", "9.9.9.0" },
            new object[] { 3, version, binary }
        })
        {
            bool refused = false;
            try { validate.Invoke(null, rejected); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException) { refused = true; }
            Assert(refused, "A version outside the schema/build authority was accepted.");
        }
    }

    private static void CurrentInstalledHelpersRequireTheirCommonModules()
    {
        string root = CreateManagedTestDirectory();
        try
        {
            string tools = Path.Combine(root, "Content", "DTMAPIInstaller", "tools");
            Directory.CreateDirectory(Path.Combine(tools, "common"));
            foreach (string file in new[] { "common.ps1", "release-common.ps1", "uninstall-dtmapi.ps1",
                "check-dtmapi-status.ps1", "collect-logs.ps1", "analyze-startup-evidence.ps1", "uninstall-common.ps1", "uninstall-targets.json",
                "dtmapi-runtime-version.props", "dtmapi-product-definitions.json" })
                File.WriteAllText(Path.Combine(tools, file), "fixture");
            string[] modules = Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "tools", "scripts", "common"), "*.ps1");
            foreach (string module in modules)
                File.Copy(module, Path.Combine(tools, "common", Path.GetFileName(module)));

            Type layout = PackageLayoutType();
            Type installFile = layout.Assembly.GetType("DTMAPI.MultiPlatformInstaller.InstallFile", throwOnError: true)!;
            Type listType = typeof(List<>).MakeGenericType(installFile);
            IList files = (IList)Activator.CreateInstance(listType)!;
            MethodInfo add = layout.GetMethod("AddInstalledTools", BindingFlags.NonPublic | BindingFlags.Static)!;
            Type sourceType = layout.Assembly.GetType("DTMAPI.MultiPlatformInstaller.CandidateRuntimeSource", true)!;
            object source = JsonSerializer.Deserialize(JsonSerializer.Serialize(new
            {
                SharedFiles = Directory.GetFiles(tools, "*", SearchOption.AllDirectories).Select(path => new
                {
                    RelativePath = Path.GetRelativePath(root, path).Replace('\\', '/'),
                    Length = new FileInfo(path).Length,
                    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
                })
            }), sourceType)!;
            add.Invoke(null, new object[] { root, files, true, CurrentRuntimeVersion(), source });
            string[] installed = files.Cast<object>().Select(file => (string)installFile
                .GetProperty("TargetRelativePath")!.GetValue(file)!).ToArray();
            foreach (string module in modules)
                Assert(installed.Contains("DTMAPI/tools/common/" + Path.GetFileName(module)),
                    "A dependency of the installed common entry point was omitted.");

            // Schema 2 reads its declared receipts; only installation consumes source bytes.
            object package = layout.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single()
                .Invoke(new object?[] { root, null, null, "", "", "", files });
            MethodInfo validatePayload = layout.GetMethod("ValidateInstallPayload")!;
            validatePayload.Invoke(package, null);
            File.Delete(Path.Combine(tools, "common", Path.GetFileName(modules[0])));
            add.Invoke(null, new object[] { root, Activator.CreateInstance(listType)!, true, CurrentRuntimeVersion(), source });
            bool refused = false;
            try { validatePayload.Invoke(package, null); }
            catch (TargetInvocationException ex) when (ex.InnerException?.GetType().Name == "InstallerException")
            {
                refused = (string?)ex.InnerException.GetType().GetProperty("Code")!.GetValue(ex.InnerException) == "DTM-E1201";
            }
            Assert(refused, "Missing common dependency did not fail before installation.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private static void AtomicReceiptValidationFailureRestoresPreviousBytes()
    {
        string root = CreateManagedTestDirectory();
        try
        {
            string receipt = Path.Combine(root, "transaction.json");
            byte[] previous = Encoding.UTF8.GetBytes("{\"generation\":\"accepted-old\"}\n");
            File.WriteAllBytes(receipt, previous);

            string installerPath = Path.Combine(AppContext.BaseDirectory, "DTMAPI-MultiPlatform-Installer.dll");
            Assert(File.Exists(installerPath), "Installer project dependency was not built/copied: " + installerPath);
            Assembly installer = Assembly.LoadFrom(installerPath);
            Type safety = installer.GetType("DTMAPI.MultiPlatformInstaller.FileSystemSafety", throwOnError: true)!;
            MethodInfo writeAtomic = safety.GetMethod(
                "WriteAllTextAtomicWithRetry",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string), typeof(Func<string, bool>) },
                modifiers: null) ?? throw new InvalidOperationException("Atomic writer method was not found.");

            bool rejected = false;
            try
            {
                writeAtomic.Invoke(null, new object?[]
                {
                    receipt,
                    "{\"generation\":\"rejected-new\"}\n",
                    new Func<string, bool>(_ => false)
                });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException)
            {
                rejected = true;
            }

            Assert(rejected, "A failed read-back validator must reject the new receipt.");
            Assert(File.Exists(receipt), "The accepted old receipt must still exist after rejection.");
            Assert(File.ReadAllBytes(receipt).SequenceEqual(previous),
                "The accepted old receipt bytes must be restored exactly after rejection.");
            Assert(Directory.EnumerateFileSystemEntries(root, "transaction.json.tmp-*", SearchOption.TopDirectoryOnly).Any() == false,
                "Atomic receipt rejection must not leave a temporary file.");
            Assert(Directory.EnumerateFileSystemEntries(root, "transaction.json.bak-*", SearchOption.TopDirectoryOnly).Any() == false,
                "Atomic receipt rejection must not leave a backup file.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateManagedTestDirectory()
    {
        string baseRoot = Path.Combine(FindRepositoryRoot(), "tmp", "test-runs");
        Directory.CreateDirectory(baseRoot);
        string root = Path.Combine(baseRoot, "multiplatform-unit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !(File.Exists(Path.Combine(current.FullName, "DTMAPI.sln")) && File.Exists(Path.Combine(current.FullName, "global.json"))))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException(
            "Repository root could not be found from the test host.");
    }

    private static void Run(string name, Action action)
    {
        try
        {
            action();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception ex)
        {
            _failures++;
            Console.Error.WriteLine("FAIL " + name + ": " + ex);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
