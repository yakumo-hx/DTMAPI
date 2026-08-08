using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    /// <summary>
    /// QA-only native save redirect. Production code never creates this owner.
    /// The runner independently validates the disposable fixture marker and
    /// proves that this path is outside the live Steam AutoCloud root.
    /// </summary>
    internal sealed class QaSaveFixtureIsolation
    {
        private const string HarmonyOwnerPrefix =
            "dtmapi.qa.save-fixture.";
        private static string redirectedSaveRoot =
            string.Empty;
        private readonly GameBridgeFixtureAccess access;
        private readonly string mode;
        private readonly string saveRoot;
        private readonly HarmonyReflectionPatcher patcher;
        private bool installed;

        internal QaSaveFixtureIsolation(
            GameBridgeFixtureAccess access,
            string mode,
            string saveRoot)
        {
            this.access = access ??
                throw new ArgumentNullException(nameof(access));
            this.mode = mode ??
                throw new ArgumentNullException(nameof(mode));
            this.saveRoot = Path.GetFullPath(
                saveRoot ??
                throw new ArgumentNullException(nameof(saveRoot)))
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            patcher = new HarmonyReflectionPatcher(
                access.Runtime,
                HarmonyOwnerPrefix + access.RunId);
        }

        internal void Install()
        {
            if (installed)
                throw new InvalidOperationException(
                    "The QA save fixture redirect may install only once.");
            string fixtureRoot =
                Path.GetDirectoryName(saveRoot) ??
                throw new InvalidDataException(
                    "The QA save fixture redirect could not resolve its disposable fixture root.");
            if (!Directory.Exists(saveRoot) ||
                !Path.GetFileName(saveRoot).Equals(
                    "SAVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The QA save fixture redirect requires an existing directory named SAVE.");
            }
            if (!Directory.Exists(
                    Path.Combine(
                        fixtureRoot,
                        "DTMAPI")))
            {
                throw new InvalidDataException(
                    "The QA save fixture redirect requires the sibling DTMAPI directory.");
            }
            EnsureOrdinaryFixtureTree(fixtureRoot);
            int conflictingOwnerPatches =
                Batch6AdvancedHarmonyOwnerObserver
                    .CountAllOwnerPatchesWithPrefix(
                        HarmonyOwnerPrefix);
            if (conflictingOwnerPatches != 0 ||
                !string.IsNullOrWhiteSpace(
                    Volatile.Read(
                        ref redirectedSaveRoot)))
            {
                throw new InvalidOperationException(
                    "A residual QA save-fixture Harmony owner or redirect root is already active; installation failed closed. patchCount=" +
                    conflictingOwnerPatches +
                    ".");
            }

            Volatile.Write(ref redirectedSaveRoot, saveRoot);
            try
            {
                MethodInfo prefix =
                    typeof(QaSaveFixtureIsolation).GetMethod(
                        nameof(GetCloudDirPathPrefix),
                        BindingFlags.NonPublic |
                        BindingFlags.Static) ??
                    throw new MissingMethodException(
                        typeof(QaSaveFixtureIsolation).FullName,
                        nameof(GetCloudDirPathPrefix));
                if (!patcher.TryPatchPrefix(
                        "DolocTown.GameData.LocalSave",
                        "get_cloudDirPath",
                        prefix,
                        parameterCount: 0))
                {
                    throw new InvalidOperationException(
                        "Could not install the exact QA LocalSave.cloudDirPath redirect.");
                }

                // Mark the owner live before the probe so a probe failure can use
                // the same retryable cleanup path as every later startup failure.
                installed = true;
                VerifyCurrentNativeSavePaths();
            }
            catch (Exception installFailure)
            {
                if (!installed)
                {
                    Volatile.Write(
                        ref redirectedSaveRoot,
                        string.Empty);
                    throw;
                }

                bool unpatched = patcher.TryUnpatchAllOwnedPatches();
                if (unpatched)
                {
                    Volatile.Write(
                        ref redirectedSaveRoot,
                        string.Empty);
                    installed = false;
                    throw;
                }

                throw new AggregateException(
                    "The QA save fixture path probe failed and its Harmony owner could not be released; the redirect remains fail-closed on the disposable root for Bootstrap cleanup retry.",
                    installFailure,
                    new InvalidOperationException(
                        "The QA save fixture Harmony owner could not be released after installation failure."));
            }

            string details =
                "mode=" +
                mode +
                ";root=" +
                saveRoot +
                ";harmonyOwner=" +
                patcher.OwnerId +
                ";preRuntime=true;nativePathProbe=true;owner=qa;fallback=false";
            access.PublishG3Status(
                "Smoke.SaveFixtureIsolation",
                "verified",
                "QA-only LocalSave.cloudDirPath Harmony prefix plus native path probe",
                details);
            access.Log(
                "Smoke save fixture isolation ready " +
                details +
                ".");
        }

        internal void Close(string reason)
        {
            if (!installed)
                return;
            bool unpatched =
                patcher.TryUnpatchAllOwnedPatches();
            if (unpatched)
            {
                Volatile.Write(
                    ref redirectedSaveRoot,
                    string.Empty);
                installed = false;
                access.Log(
                    "Smoke save fixture isolation released owner=" +
                    patcher.OwnerId +
                    ";reason=" +
                    (reason ?? string.Empty) +
                    ".");
                return;
            }

            throw new InvalidOperationException(
                "The QA save fixture Harmony owner could not be released.");
        }

        private static bool GetCloudDirPathPrefix(
            ref string __result)
        {
            string root =
                Volatile.Read(ref redirectedSaveRoot);
            if (string.IsNullOrWhiteSpace(root))
                return true;
            __result = root;
            return false;
        }

        private void VerifyCurrentNativeSavePaths()
        {
            Type dolocApi =
                patcher.ResolveType(
                    "DolocAPI, Assembly-CSharp") ??
                throw new TypeLoadException(
                    "The exact current DolocAPI type is unavailable before Runtime Mod loading.");
            FieldInfo persistenceField =
                dolocApi.GetField(
                    "dataPersistenceManager",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static) ??
                throw new MissingFieldException(
                    dolocApi.FullName,
                    "dataPersistenceManager");
            object persistenceManager =
                persistenceField.GetValue(null) ??
                throw new InvalidOperationException(
                    "DolocAPI.dataPersistenceManager is not initialized before the QA pre-Runtime save guard.");
            FieldInfo handlerField =
                persistenceManager.GetType().GetField(
                    "fileDataHandler",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance) ??
                throw new MissingFieldException(
                    persistenceManager.GetType().FullName,
                    "fileDataHandler");
            object handler =
                handlerField.GetValue(persistenceManager) ??
                throw new InvalidOperationException(
                    "DolocAPI.dataPersistenceManager.fileDataHandler is not initialized before Runtime Mod loading.");
            Type handlerType = handler.GetType();
            if (!string.Equals(
                    handlerType.FullName,
                    "DolocTown.GameData.LocalSave",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The current native save handler is not the exact DolocTown.GameData.LocalSave owner: " +
                    (handlerType.FullName ?? handlerType.Name) +
                    ".");
            }

            VerifyExactPath(
                "LocalSave.cloudDirPath",
                InvokeExactStringProperty(
                    handler,
                    "cloudDirPath"),
                saveRoot);
            VerifyExactPath(
                "LocalSave.GetDataFullPath(6)",
                InvokeExactStringMethod(
                    handler,
                    "GetDataFullPath",
                    new[] { typeof(int) },
                    new object[] { 6 }),
                Path.Combine(saveRoot, "doloc-save-6.data"));
            VerifyExactPath(
                "LocalSave.GetDataBackupPath(6)",
                InvokeExactStringMethod(
                    handler,
                    "GetDataBackupPath",
                    new[] { typeof(int) },
                    new object[] { 6 }),
                Path.Combine(saveRoot, "doloc-save-6.data.bak"));
            VerifyExactPath(
                "LocalSave.GetDataPrevPath(6,0)",
                InvokeExactStringMethod(
                    handler,
                    "GetDataPrevPath",
                    new[] { typeof(int), typeof(int) },
                    new object[] { 6, 0 }),
                Path.Combine(saveRoot, "doloc-save-6.data.prev0"));
            VerifyExactPath(
                "LocalSave.GetDataTempPath(6)",
                InvokeExactStringMethod(
                    handler,
                    "GetDataTempPath",
                    new[] { typeof(int) },
                    new object[] { 6 }),
                Path.Combine(saveRoot, "doloc-save-6-tmp.data"));
        }

        private static string InvokeExactStringProperty(
            object owner,
            string propertyName)
        {
            PropertyInfo property =
                owner.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance) ??
                throw new MissingMemberException(
                    owner.GetType().FullName,
                    propertyName);
            MethodInfo? getter = property.GetGetMethod(nonPublic: true);
            if (property.PropertyType != typeof(string) ||
                property.GetIndexParameters().Length != 0 ||
                getter == null ||
                getter.IsStatic)
            {
                throw new MissingMethodException(
                    "The exact instance string property " +
                    owner.GetType().FullName +
                    "." +
                    propertyName +
                    " is unavailable.");
            }
            return (string?)property.GetValue(owner, null) ??
                throw new InvalidDataException(
                    owner.GetType().FullName +
                    "." +
                    propertyName +
                    " returned null.");
        }

        private static string InvokeExactStringMethod(
            object owner,
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            MethodInfo method =
                owner.GetType().GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    binder: null,
                    types: parameterTypes,
                    modifiers: null) ??
                throw new MissingMethodException(
                    owner.GetType().FullName,
                    methodName);
            if (method.ReturnType != typeof(string) || method.IsStatic)
            {
                throw new MissingMethodException(
                    "The exact instance string method " +
                    owner.GetType().FullName +
                    "." +
                    methodName +
                    " is unavailable.");
            }
            return (string?)method.Invoke(owner, arguments) ??
                throw new InvalidDataException(
                    owner.GetType().FullName +
                    "." +
                    methodName +
                    " returned null.");
        }

        private static void VerifyExactPath(
            string label,
            string actualPath,
            string expectedPath)
        {
            string actual = Path.GetFullPath(actualPath);
            string expected = Path.GetFullPath(expectedPath);
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    label +
                    " did not resolve to the exact disposable fixture path. expected=" +
                    expected +
                    "; actual=" +
                    actual +
                    ".");
            }
        }

        private static void EnsureOrdinaryFixtureTree(
            string fixtureRoot)
        {
            var pending = new Stack<DirectoryInfo>();
            pending.Push(
                new DirectoryInfo(
                    Path.GetFullPath(fixtureRoot)));
            while (pending.Count > 0)
            {
                DirectoryInfo current = pending.Pop();
                RejectReparsePoint(current);
                foreach (FileSystemInfo child in
                    current.EnumerateFileSystemInfos())
                {
                    RejectReparsePoint(child);
                    if (child is DirectoryInfo directory)
                        pending.Push(directory);
                }
            }
        }

        private static void RejectReparsePoint(
            FileSystemInfo item)
        {
            item.Refresh();
            if ((item.Attributes &
                 FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    "The QA save fixture must not contain a junction, symbolic link, or other reparse point: " +
                    item.FullName);
            }
        }
    }
}
