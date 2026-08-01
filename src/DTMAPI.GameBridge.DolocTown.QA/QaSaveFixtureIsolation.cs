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
            MethodInfo prefix =
                typeof(QaSaveFixtureIsolation).GetMethod(
                    nameof(GetDataDirPathPrefix),
                    BindingFlags.NonPublic |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(QaSaveFixtureIsolation).FullName,
                    nameof(GetDataDirPathPrefix));
            if (!patcher.TryPatchPrefix(
                    "DolocTown.GameData.LocalSave",
                    "get_dataDirPath",
                    prefix,
                    parameterCount: 0))
            {
                Volatile.Write(
                    ref redirectedSaveRoot,
                    string.Empty);
                throw new InvalidOperationException(
                    "Could not install the exact QA LocalSave.dataDirPath redirect.");
            }

            installed = true;
            string details =
                "mode=" +
                mode +
                ";root=" +
                saveRoot +
                ";harmonyOwner=" +
                patcher.OwnerId +
                ";owner=qa;fallback=false";
            access.PublishG3Status(
                "Smoke.SaveFixtureIsolation",
                "verified",
                "QA-only LocalSave.dataDirPath Harmony prefix",
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

        private static bool GetDataDirPathPrefix(
            ref string __result)
        {
            string root =
                Volatile.Read(ref redirectedSaveRoot);
            if (string.IsNullOrWhiteSpace(root))
                return true;
            __result = root;
            return false;
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
