using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal static class InstallStateAuthority
{
    public static void AssertCompatibleForInstall(PackageLayout package, string gameDir)
    {
        string statePath = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/install-state.json");
        string releasePath = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/release-manifest.json");
        InstallStateV1? state = null;
        ReleaseManifestV1? installedRelease = null;

        if (File.Exists(statePath))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, statePath);
                state = JsonStore.Read(statePath, InstallerJsonContext.Default.InstallStateV1)
                    ?? throw new InvalidDataException("install-state.json is empty.");
            }
            catch (Exception ex)
            {
                throw Conflict("The existing install-state.json cannot prove safe Runtime provenance: " + ex.Message, ex);
            }
        }

        if (File.Exists(releasePath))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, releasePath);
                installedRelease = JsonStore.Read(releasePath, InstallerJsonContext.Default.ReleaseManifestV1)
                    ?? throw new InvalidDataException("release-manifest.json is empty.");
            }
            catch (Exception ex)
            {
                throw Conflict("The existing release-manifest.json cannot prove safe Runtime provenance: " + ex.Message, ex);
            }
        }

        Version packageVersion = ParseVersion(package.ReleaseManifest.DTMAPIVersion, "package Runtime version");
        Version? installedReleaseVersion = null;
        if (installedRelease is not null)
        {
            ValidateBasicRelease(installedRelease);
            installedReleaseVersion = ParseVersion(installedRelease.DTMAPIVersion, "installed release-manifest Runtime version");
            if (installedReleaseVersion > packageVersion)
                throw Conflict($"Downgrade refused: installed release manifest {installedReleaseVersion}, package Runtime {packageVersion}.");
            if (installedReleaseVersion == packageVersion &&
                !TryValidateExactRelease(package.ReleaseManifest, installedRelease, out string installedReleaseDetail))
                throw Conflict("Same-version installed release manifest differs from this package: " + installedReleaseDetail);
        }

        if (state is not null)
        {
            ValidateBasicState(state, gameDir);
            bool claimsMultiPlatformAuthority = string.Equals(
                state.InstallerDistribution, "DTMAPI.MultiPlatform", StringComparison.Ordinal);
            bool claimsRecognizedLegacyAuthority = string.IsNullOrWhiteSpace(state.InstallerDistribution);
            if (!claimsMultiPlatformAuthority && !claimsRecognizedLegacyAuthority)
                throw Conflict("Existing install-state names an unknown installer distribution: " + state.InstallerDistribution);
            if (claimsRecognizedLegacyAuthority && installedRelease is null)
                throw Conflict("Legacy install-state has no corresponding release-manifest authority.");

            if (installedRelease is not null &&
                (!string.Equals(state.DTMAPIVersion, installedRelease.DTMAPIVersion, StringComparison.Ordinal) ||
                 !string.Equals(state.BinaryVersion, installedRelease.BinaryVersion, StringComparison.Ordinal) ||
                 !string.Equals(state.SourceRepoCommit, installedRelease.BuildCommit, StringComparison.Ordinal)))
                throw Conflict("Existing install-state and release-manifest authorities disagree on version, binary version, or source commit.");
            Version installedVersion = ParseVersion(state.DTMAPIVersion, "installed Runtime version");
            if (installedVersion > packageVersion)
                throw Conflict($"Downgrade refused: installed Runtime {installedVersion}, package Runtime {packageVersion}.");
            if (installedVersion == packageVersion)
            {
                bool strongState = TryValidateExactState(package, gameDir, state, out string stateDetail);
                string releaseDetail = "missing";
                bool exactRelease = installedRelease is not null &&
                    TryValidateExactRelease(package.ReleaseManifest, installedRelease, out releaseDetail);
                if ((claimsMultiPlatformAuthority && !strongState) ||
                    (claimsRecognizedLegacyAuthority && !exactRelease))
                    throw Conflict("Same-version Runtime provenance differs or is incomplete. State=" + stateDetail +
                                   "; Release=" + releaseDetail);

                if (!string.Equals(state.SourceRepoCommit, package.ReleaseManifest.BuildCommit, StringComparison.Ordinal))
                    throw Conflict("Same-version Runtime SourceRepoCommit differs from this package.");
                if (!string.Equals(state.BinaryVersion, package.ReleaseManifest.BinaryVersion, StringComparison.Ordinal))
                    throw Conflict("Same-version Runtime BinaryVersion differs from this package.");
            }
        }

        if (state is null && installedRelease is null)
        {
            List<string> conflicting = new();
            foreach (InstallFile expected in package.InstallFiles)
            {
                string target = FileSystemSafety.CombineUnder(gameDir, expected.TargetRelativePath);
                if (!File.Exists(target))
                    continue;
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, target);
                if (!FileSystemSafety.Matches(target, expected.Length, expected.Sha256))
                    conflicting.Add(expected.TargetRelativePath);
            }
            if (conflicting.Count > 0)
                throw Conflict("Existing owned files differ, but no provenance receipt can authorize repair: " + string.Join(", ", conflicting));
        }
    }

    private static void ValidateBasicState(InstallStateV1 state, string gameDir)
    {
        if (state.SchemaVersion != 1 || state.DryRun || string.IsNullOrWhiteSpace(state.DTMAPIVersion) ||
            string.IsNullOrWhiteSpace(state.BinaryVersion) || string.IsNullOrWhiteSpace(state.SourceRepoCommit) ||
            string.IsNullOrWhiteSpace(state.GameDir) || string.IsNullOrWhiteSpace(state.PluginDir))
            throw Conflict("The existing install-state basic identity is incomplete or invalid.");
        try
        {
            if (!string.Equals(FileSystemSafety.NormalizeRoot(state.GameDir), FileSystemSafety.NormalizeRoot(gameDir), FileSystemSafety.PathComparison) ||
                !string.Equals(FileSystemSafety.NormalizeRoot(state.PluginDir),
                    FileSystemSafety.CombineUnder(gameDir, "BepInEx/plugins/DTMAPI"), FileSystemSafety.PathComparison))
                throw Conflict("The existing install-state belongs to a different game/plugin directory.");
        }
        catch (InstallerException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Conflict("The existing install-state path identity is invalid: " + ex.Message, ex);
        }
    }

    private static void ValidateBasicRelease(ReleaseManifestV1 release)
    {
        if (release.SchemaVersion != 1 || string.IsNullOrWhiteSpace(release.DTMAPIVersion) ||
            string.IsNullOrWhiteSpace(release.BinaryVersion) || string.IsNullOrWhiteSpace(release.BuildCommit))
            throw Conflict("The existing release-manifest basic identity is incomplete or invalid.");
        _ = ParseVersion(release.DTMAPIVersion, "installed release-manifest Runtime version");
        _ = ParseVersion(release.BinaryVersion, "installed release-manifest binary version");
    }

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

    private static bool TryValidateExactRelease(
        ReleaseManifestV1 expected,
        ReleaseManifestV1 actual,
        out string detail)
    {
        try
        {
            if (actual.SchemaVersion != expected.SchemaVersion ||
                actual.DTMAPIVersion != expected.DTMAPIVersion ||
                actual.BinaryVersion != expected.BinaryVersion ||
                actual.BuildCommit != expected.BuildCommit ||
                actual.PackageKind != expected.PackageKind)
                throw new InvalidDataException("release identity differs");

            Dictionary<string, RuntimeAssemblyReceipt> expectedAssemblies = expected.IncludedAssemblies
                .ToDictionary(item => item.FileName, StringComparer.Ordinal);
            Dictionary<string, RuntimeAssemblyReceipt> actualAssemblies = actual.IncludedAssemblies
                .ToDictionary(item => item.FileName, StringComparer.Ordinal);
            if (!expectedAssemblies.Keys.OrderBy(item => item, StringComparer.Ordinal)
                .SequenceEqual(actualAssemblies.Keys.OrderBy(item => item, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidDataException("assembly set differs");
            foreach ((string name, RuntimeAssemblyReceipt expectedReceipt) in expectedAssemblies)
            {
                RuntimeAssemblyReceipt actualReceipt = actualAssemblies[name];
                if (actualReceipt.Length != expectedReceipt.Length ||
                    !string.Equals(actualReceipt.Sha256, expectedReceipt.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    actualReceipt.FileVersion != expectedReceipt.FileVersion)
                    throw new InvalidDataException("assembly receipt differs: " + name);
            }

            if (actual.OptionalComponents.Count != expected.OptionalComponents.Count)
                throw new InvalidDataException("optional component count differs");
            foreach (OptionalComponentReceipt expectedComponent in expected.OptionalComponents)
            {
                OptionalComponentReceipt? actualComponent = actual.OptionalComponents.SingleOrDefault(item =>
                    string.Equals(item.ComponentId, expectedComponent.ComponentId, StringComparison.Ordinal));
                if (actualComponent is null || actualComponent.RelativePath != expectedComponent.RelativePath ||
                    actualComponent.Length != expectedComponent.Length ||
                    !string.Equals(actualComponent.Sha256, expectedComponent.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    actualComponent.FileVersion != expectedComponent.FileVersion)
                    throw new InvalidDataException("optional component receipt differs: " + expectedComponent.ComponentId);
            }

            detail = "exact release-manifest authority";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            return false;
        }
    }

    private static Version ParseVersion(string value, string label)
    {
        if (!Version.TryParse(value, out Version? version))
            throw Conflict(label + " is invalid: " + value);
        return version;
    }

    private static InstallerException Conflict(string detail, Exception? inner = null) =>
        new(InstallerCodes.RuntimeConflict, 3,
            "检测到无法证明与本包同源的现有 Runtime；为避免同版本不同字节互相覆盖，安装已停止。",
            "Existing Runtime provenance cannot be proven identical to this package. Installation stopped to prevent a same-version/different-payload overwrite.",
            detail, inner);

    private static StringComparer PathComparer() => FileSystemSafety.PathComparison == StringComparison.OrdinalIgnoreCase
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
