using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DTMAPI.MoreSaves
{
    internal sealed class MoreSavesArchiveMigrationResult
    {
        internal MoreSavesArchiveMigrationResult(
            int legacySourceCount,
            int movedFileCount,
            int preservedDestinationCount)
        {
            LegacySourceCount = legacySourceCount;
            MovedFileCount = movedFileCount;
            PreservedDestinationCount = preservedDestinationCount;
        }

        internal int LegacySourceCount { get; }
        internal int MovedFileCount { get; }
        internal int PreservedDestinationCount { get; }
        internal bool IsNoOp => LegacySourceCount == 0;
        internal string Summary =>
            "legacySources=" + LegacySourceCount.ToString(CultureInfo.InvariantCulture) +
            ", moved=" + MovedFileCount.ToString(CultureInfo.InvariantCulture) +
            ", destinationsPreserved=" + PreservedDestinationCount.ToString(CultureInfo.InvariantCulture) + ".";

        internal static MoreSavesArchiveMigrationResult NoLegacyFiles() =>
            new MoreSavesArchiveMigrationResult(0, 0, 0);
    }

    internal sealed class MoreSavesArchiveMigrationException : InvalidOperationException
    {
        internal MoreSavesArchiveMigrationException(string message)
            : base(message)
        {
        }

        internal MoreSavesArchiveMigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    internal static class MoreSavesArchiveMigration
    {
        internal const string CurrentArchiveFileNameFormat = "doloc-save-{0}.data";
        internal const int FirstExpandedArchiveIndex = MoreSavesNativeRuntime.VanillaSlotCount;
        internal const int LastExpandedArchiveIndex = MoreSavesNativeRuntime.ExpandedSlotCount - 1;
        private const string LegacyArchiveFileNameFormat = "ea-playtest-doloc-archive-{0}.data";
        private const string LegacyPrevFileNameFormat = "ea-playtest-doloc-archive-{0}-prev.data";
        private const string LegacyBackupFileNameFormat = "ea-playtest-doloc-archive-{0}-bak.data";
        private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

        internal static MoreSavesArchiveMigrationResult ExecuteNative(object manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            string nativeFormat = ReadRequiredStringMember(manager, "archiveFileNameFormat");
            if (!string.Equals(nativeFormat, CurrentArchiveFileNameFormat, StringComparison.Ordinal))
            {
                throw new MoreSavesArchiveMigrationException(
                    "MoreSaves legacy migration supports the Doloc Town 1.00 archive format '" +
                    CurrentArchiveFileNameFormat + "', but the native manager reported '" + nativeFormat + "'.");
            }

            object localSave = ResolveLocalSave();
            MethodInfo getDataFullPath = GetRequiredMethod(
                localSave.GetType(),
                "GetDataFullPath",
                new[] { typeof(int) },
                typeof(string));

            return Execute(index => InvokeCurrentPath(localSave, getDataFullPath, index));
        }

        internal static MoreSavesArchiveMigrationResult Execute(
            Func<int, string> currentPathProvider,
            Action<string, string>? moveFile = null)
        {
            if (currentPathProvider == null)
                throw new ArgumentNullException(nameof(currentPathProvider));
            moveFile = moveFile ?? File.Move;

            // Resolve every native current path before moving anything. This is
            // path authority validation only; archive contents are never read.
            List<ArchivePair> pairs = BuildPairs(currentPathProvider);
            int legacySourceCount = 0;
            int movedFileCount = 0;
            int preservedDestinationCount = 0;

            foreach (ArchivePair pair in pairs)
            {
                if (Directory.Exists(pair.SourcePath))
                {
                    throw new MoreSavesArchiveMigrationException(
                        "A legacy archive source is a directory rather than a file: " +
                        Path.GetFileName(pair.SourcePath) + ".");
                }
                if (!File.Exists(pair.SourcePath))
                    continue;

                legacySourceCount++;
                if (File.Exists(pair.DestinationPath) || Directory.Exists(pair.DestinationPath))
                {
                    preservedDestinationCount++;
                    continue;
                }

                try
                {
                    moveFile(pair.SourcePath, pair.DestinationPath);
                }
                catch (Exception ex)
                {
                    throw new MoreSavesArchiveMigrationException(
                        "MoreSaves could not move one legacy archive role. No overwrite was attempted: " +
                        Path.GetFileName(pair.SourcePath) + " -> " + Path.GetFileName(pair.DestinationPath) + ".",
                        ex);
                }

                bool sourceStillExists = File.Exists(pair.SourcePath) || Directory.Exists(pair.SourcePath);
                bool destinationExists = File.Exists(pair.DestinationPath);
                if (sourceStillExists || !destinationExists)
                {
                    throw new MoreSavesArchiveMigrationException(
                        "A legacy archive move did not produce the required source-absent/destination-present state: " +
                        Path.GetFileName(pair.SourcePath) + " -> " + Path.GetFileName(pair.DestinationPath) + ".");
                }
                movedFileCount++;
            }

            return new MoreSavesArchiveMigrationResult(
                legacySourceCount,
                movedFileCount,
                preservedDestinationCount);
        }

        private static List<ArchivePair> BuildPairs(Func<int, string> currentPathProvider)
        {
            var pairs = new List<ArchivePair>();
            string? migrationRoot = null;
            for (int index = FirstExpandedArchiveIndex; index <= LastExpandedArchiveIndex; index++)
            {
                string targetPath;
                try
                {
                    string providedPath = currentPathProvider(index);
                    if (string.IsNullOrWhiteSpace(providedPath) || !Path.IsPathRooted(providedPath))
                        throw new InvalidOperationException("Native GetDataFullPath did not return an absolute path.");
                    targetPath = Path.GetFullPath(providedPath);
                }
                catch (Exception ex)
                {
                    throw new MoreSavesArchiveMigrationException(
                        "The native current archive path could not be resolved for index " +
                        index.ToString(CultureInfo.InvariantCulture) + ".",
                        ex);
                }

                string expectedName = Format(CurrentArchiveFileNameFormat, index);
                if (!string.Equals(Path.GetFileName(targetPath), expectedName, StringComparison.Ordinal))
                {
                    throw new MoreSavesArchiveMigrationException(
                        "The native current archive path does not match the exact Doloc Town 1.00 filename for index " +
                        index.ToString(CultureInfo.InvariantCulture) + ": " + Path.GetFileName(targetPath) + ".");
                }

                string root = Path.GetDirectoryName(targetPath) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(root))
                    throw new MoreSavesArchiveMigrationException("The native SAVE directory path is missing for MoreSaves migration.");
                root = Path.GetFullPath(root);
                if (File.Exists(root))
                    throw new MoreSavesArchiveMigrationException("The native SAVE root is a file rather than a directory.");
                if (migrationRoot == null)
                    migrationRoot = root;
                else if (!PathComparer.Equals(migrationRoot, root))
                    throw new MoreSavesArchiveMigrationException("Native expanded archive paths do not share one exact SAVE directory.");

                pairs.Add(new ArchivePair(
                    Path.Combine(root, Format(LegacyArchiveFileNameFormat, index)),
                    targetPath));
                pairs.Add(new ArchivePair(
                    Path.Combine(root, Format(LegacyPrevFileNameFormat, index)),
                    targetPath + ".prev0"));
                pairs.Add(new ArchivePair(
                    Path.Combine(root, Format(LegacyBackupFileNameFormat, index)),
                    targetPath + ".bak"));
            }
            return pairs;
        }

        private static string InvokeCurrentPath(object localSave, MethodInfo method, int index)
        {
            try
            {
                return method.Invoke(localSave, new object[] { index }) as string ??
                    throw new MoreSavesArchiveMigrationException("Native GetDataFullPath returned no path.");
            }
            catch (TargetInvocationException ex)
            {
                throw new MoreSavesArchiveMigrationException(
                    "Native GetDataFullPath failed for expanded archive index " +
                    index.ToString(CultureInfo.InvariantCulture) + ".",
                    ex.InnerException ?? ex);
            }
        }

        private static object ResolveLocalSave()
        {
            object manager = ReadRequiredStaticMember(typeof(global::DolocAPI), "dataPersistenceManager");
            Type managerType = manager.GetType();
            object? localSave = managerType.GetField("fileDataHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager) ??
                managerType.GetProperty("fileDataHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager);
            return localSave ?? throw new MoreSavesArchiveMigrationException("DolocAPI.dataPersistenceManager has no initialized LocalSave fileDataHandler.");
        }

        private static object ReadRequiredStaticMember(Type type, string name)
        {
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) ??
                type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            return value ?? throw new MoreSavesArchiveMigrationException("Native static member is unavailable: DolocAPI." + name + ".");
        }

        private static string ReadRequiredStringMember(object instance, string name)
        {
            Type type = instance.GetType();
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value as string ?? throw new MoreSavesArchiveMigrationException("Native string member is unavailable: " + type.FullName + "." + name + ".");
        }

        private static MethodInfo GetRequiredMethod(Type type, string name, Type[] parameterTypes, Type returnType)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null) ??
                throw new MoreSavesArchiveMigrationException("Native method is unavailable: " + type.FullName + "." + name + ".");
            if (method.ReturnType != returnType)
                throw new MoreSavesArchiveMigrationException("Native method has an unexpected return type: " + type.FullName + "." + name + ".");
            return method;
        }

        private static string Format(string value, int index) =>
            string.Format(CultureInfo.InvariantCulture, value, index);

        private sealed class ArchivePair
        {
            internal ArchivePair(string sourcePath, string destinationPath)
            {
                SourcePath = Path.GetFullPath(sourcePath);
                DestinationPath = Path.GetFullPath(destinationPath);
            }

            internal string SourcePath { get; }
            internal string DestinationPath { get; }
        }
    }
}
