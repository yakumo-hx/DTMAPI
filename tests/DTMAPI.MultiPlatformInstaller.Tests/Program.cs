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
        Run(nameof(CurrentRuntimeDoesNotInstallPowerShellTools), CurrentRuntimeDoesNotInstallPowerShellTools);
        Run(nameof(DiagnosticActionsDoNotRequireAnIntactPackage), DiagnosticActionsDoNotRequireAnIntactPackage);
        Run(nameof(CompleteUninstallRemovesOwnedContentAndIsRepeatable), CompleteUninstallRemovesOwnedContentAndIsRepeatable);
        Run(nameof(LegacyEquipmentSurvivesUninstall), LegacyEquipmentSurvivesUninstall);
        Run(nameof(UninstallRemovesInterruptedDirectoriesByName), UninstallRemovesInterruptedDirectoriesByName);
        Run(nameof(OtherPluginsKeepBepInEx), OtherPluginsKeepBepInEx);
        Run(nameof(CustomRuntimeIsNotAnUninstallTarget), CustomRuntimeIsNotAnUninstallTarget);
        Run(nameof(LockedFileCanBeRemovedOnRetry), LockedFileCanBeRemovedOnRetry);

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

    private static string CurrentRuntimeVersion() => XDocument.Load(Path.Combine(FindRepositoryRoot(),
        "build", "dtmapi-runtime-version.props")).Root!.Element("PropertyGroup")!
        .Element("DtmApiReleaseVersion")!.Value;

    private static void CurrentRuntimeVersionIsBoundToBuildAuthority()
    {
        MethodInfo validate = PackageLayoutType().GetMethod("ValidateRuntimeVersion",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        XElement authority = XDocument.Load(Path.Combine(FindRepositoryRoot(), "build",
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

    private static void CurrentRuntimeDoesNotInstallPowerShellTools()
    {
        Type layout = PackageLayoutType();
        Type installFile = layout.Assembly.GetType("DTMAPI.MultiPlatformInstaller.InstallFile", true)!;
        IList files = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(installFile))!;
        MethodInfo add = layout.GetMethod("AddInstalledTools", BindingFlags.NonPublic | BindingFlags.Static)!;
        add.Invoke(null, new object?[] { "unused-package-root", files, true, CurrentRuntimeVersion(), null });
        Assert(files.Count == 0, "Current Runtime still deploys PowerShell helpers.");
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
