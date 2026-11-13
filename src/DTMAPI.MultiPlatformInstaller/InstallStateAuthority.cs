using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal static class InstallStateAuthority
{
    public static bool TryValidateExactState(
        PackageLayout package,
        string gameDir,
        InstallStateV1? state,
        out string detail)
    {
        try
        {
            if (state is null || state.SchemaVersion != 1 || state.DryRun)
                throw new InvalidDataException("schema/dry-run identity is invalid");
            if (!string.Equals(state.DTMAPIVersion, package.ReleaseManifest.DTMAPIVersion, StringComparison.Ordinal) ||
                !string.Equals(state.BinaryVersion, package.ReleaseManifest.BinaryVersion, StringComparison.Ordinal) ||
                !string.Equals(state.SourceRepoCommit, package.ReleaseManifest.BuildCommit, StringComparison.Ordinal) ||
                !string.Equals(state.InstallerDistribution, "DTMAPI.MultiPlatform", StringComparison.Ordinal) ||
                !string.Equals(state.InstallScriptVersion, "multiplatform-1", StringComparison.Ordinal) ||
                state.QaFixturesInstalledCount != 0 || state.QaFixturesInstalled.Count != 0)
                throw new InvalidDataException("version, commit, or installer distribution differs");
            if (!string.Equals(FileSystemSafety.NormalizeRoot(state.GameDir), FileSystemSafety.NormalizeRoot(gameDir), FileSystemSafety.PathComparison) ||
                !string.Equals(FileSystemSafety.NormalizeRoot(state.PluginDir),
                    FileSystemSafety.CombineUnder(gameDir, "BepInEx/plugins/DTMAPI"), FileSystemSafety.PathComparison))
                throw new InvalidDataException("GameDir or PluginDir differs");

            Dictionary<string, List<InstalledFileReceipt>> receipts = state.FilesInstalled
                .Where(item => !string.IsNullOrWhiteSpace(item.RelativePath))
                .GroupBy(item => FileSystemSafety.NormalizeRelativePath(item.RelativePath), PathComparer())
                .ToDictionary(group => group.Key, group => group.ToList(), PathComparer());
            foreach (InstallFile expected in package.InstallFiles)
            {
                string relative = FileSystemSafety.NormalizeRelativePath(expected.TargetRelativePath);
                if (!receipts.TryGetValue(relative, out List<InstalledFileReceipt>? matches) || matches.Count != 1)
                    throw new InvalidDataException("missing or duplicate owned receipt: " + relative);
                InstalledFileReceipt actual = matches[0];
                if (!string.Equals(actual.Kind, expected.Kind, StringComparison.Ordinal) ||
                    !string.Equals(Path.GetFullPath(actual.Path), FileSystemSafety.CombineUnder(gameDir, relative), FileSystemSafety.PathComparison) ||
                    actual.Length != expected.Length ||
                    !string.Equals(actual.Sha256, expected.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(actual.FileVersion ?? string.Empty, expected.FileVersion ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(actual.ComponentId ?? string.Empty, expected.ComponentId ?? string.Empty, StringComparison.Ordinal))
                    throw new InvalidDataException("owned receipt differs: " + relative);
            }

            const string installStateRelative = "DTMAPI/install-state.json";
            if (!receipts.TryGetValue(installStateRelative, out List<InstalledFileReceipt>? stateReceipts) || stateReceipts.Count != 1 ||
                !string.Equals(stateReceipts[0].Kind, "install-state", StringComparison.Ordinal) ||
                !string.Equals(Path.GetFullPath(stateReceipts[0].Path),
                    FileSystemSafety.CombineUnder(gameDir, installStateRelative), FileSystemSafety.PathComparison))
                throw new InvalidDataException("install-state self receipt differs");

            if (state.OptionalComponents.Count != package.ReleaseManifest.OptionalComponents.Count)
                throw new InvalidDataException("optional component projection count differs");
            foreach (OptionalComponentReceipt expectedComponent in package.ReleaseManifest.OptionalComponents)
            {
                OptionalComponentReceipt? actualComponent = state.OptionalComponents.SingleOrDefault(item =>
                    string.Equals(item.ComponentId, expectedComponent.ComponentId, StringComparison.Ordinal));
                if (actualComponent is null || actualComponent.RelativePath != expectedComponent.RelativePath ||
                    actualComponent.Length != expectedComponent.Length ||
                    !string.Equals(actualComponent.Sha256, expectedComponent.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    actualComponent.FileVersion != expectedComponent.FileVersion ||
                    actualComponent.LoadPolicy != expectedComponent.LoadPolicy ||
                    actualComponent.DefaultLoadState != expectedComponent.DefaultLoadState)
                    throw new InvalidDataException("optional component projection differs: " + expectedComponent.ComponentId);
            }

            detail = "exact multi-platform install-state authority";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            return false;
        }
    }

    private static StringComparer PathComparer() => FileSystemSafety.PathComparison == StringComparison.OrdinalIgnoreCase
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
