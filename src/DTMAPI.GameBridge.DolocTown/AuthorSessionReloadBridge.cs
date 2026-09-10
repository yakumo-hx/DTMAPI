using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private long optionalAuthorFileStatusCallCount;
        private long optionalAuthorDirectoryEnumerationCount;

        internal long OptionalAuthorFileStatusCallCount => optionalAuthorFileStatusCallCount;

        internal long OptionalAuthorDirectoryEnumerationCount => optionalAuthorDirectoryEnumerationCount;

        private AuthorSessionOperationResult HandleAuthorSessionRequest(AuthorSessionRequest request)
        {
            if (!runtime.IsRuntimeThread)
                return AuthorSessionOperationResult.Error("runtime-thread-required", "Author content operations may only run on the Runtime thread.");

            AuthorSourceSelectionDecision? decision = runtime.AuthorSourceSelectionDecisions
                .SingleOrDefault(value => string.Equals(value.UniqueId, request.UniqueId, StringComparison.OrdinalIgnoreCase));
            DiscoveredMod? selected = decision?.Selected;
            if (decision == null || selected == null)
                return AuthorSessionOperationResult.Rejected("source-not-selected", "No authorized selected source exists for this UniqueID.");
            if (!AuthorSessionProtocol.PathsEqual(selected.RootPath, request.SelectedRoot))
                return AuthorSessionOperationResult.Rejected("selected-root-mismatch", "The request selectedRoot no longer matches Runtime source authority.");

            string currentTreeSha256;
            try
            {
                currentTreeSha256 = AuthorFileTreeDigest.Compute(selected.RootPath);
            }
            catch (Exception ex)
            {
                return AuthorSessionOperationResult.Rejected(
                    "source-tree-unreadable",
                    "The selected source tree could not be hashed safely: " + ex.GetType().Name + ".");
            }

            if (!string.Equals(currentTreeSha256, request.ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorSessionOperationResult.Rejected(
                    "source-tree-hash-mismatch",
                    "The selected source changed after the SDK request was prepared.",
                    Pair("actualTreeSha256", currentTreeSha256),
                    Pair("treeHashAlgorithm", AuthorFileTreeDigest.Algorithm));
            }

            if (string.Equals(request.Operation, AuthorSessionProtocol.GetSourceSnapshotOperation, StringComparison.Ordinal))
                return CreateAuthorSourceSnapshotResult(decision, selected, currentTreeSha256);
            if (string.Equals(request.Operation, AuthorSessionProtocol.ExecuteCommandOperation, StringComparison.Ordinal))
            {
                if (!selected.OfficialEnabled || !runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID.Equals(request.UniqueId, StringComparison.OrdinalIgnoreCase)))
                    return AuthorSessionOperationResult.Rejected("source-not-active", "Commands require the selected source's active Mod owner.");
                return runtime.ExecuteAuthorPlatformCommand(request);
            }
            if (!string.Equals(request.Operation, AuthorSessionProtocol.ReloadContentOperation, StringComparison.Ordinal))
                return AuthorSessionOperationResult.Rejected("operation-unsupported", "The requested author operation is unsupported.");

            if (!selected.OfficialEnabled)
                return AuthorSessionOperationResult.Rejected("source-disabled", "The selected source is disabled by the current Runtime/native owner state.");

            DiscoveredMod? active = runtime.LoadedMods.SingleOrDefault(candidate =>
                string.Equals(candidate.Manifest.UniqueID, selected.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase) &&
                AuthorSessionProtocol.PathsEqual(candidate.RootPath, selected.RootPath) &&
                string.Equals(candidate.Source, selected.Source, StringComparison.OrdinalIgnoreCase) &&
                candidate.WorkshopId == selected.WorkshopId &&
                string.Equals(candidate.OfficialId, selected.OfficialId, StringComparison.OrdinalIgnoreCase));
            if (active == null)
            {
                return AuthorSessionOperationResult.Rejected(
                    "source-not-active",
                    "The selected source did not pass Runtime load, minimum-version and dependency gates; no content was reloaded.");
            }

            AuthorSessionOperationResult? manifestValidation = ValidateReloadManifest(active);
            if (manifestValidation != null)
                return manifestValidation;

            return ReloadSelectedAuthorContent(active, currentTreeSha256);
        }

        private static AuthorSessionOperationResult? ValidateReloadManifest(DiscoveredMod active)
        {
            ManifestModel current;
            try
            {
                current = new ManifestReader().Read(active.ManifestPath);
            }
            catch (Exception ex)
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "manifest-unreadable-restart-required",
                    "The active manifest could not be revalidated safely: " + ex.GetType().Name + ": " + ex.Message);
            }

            if (!string.Equals(ManifestAuthorityFingerprint(active.Manifest), ManifestAuthorityFingerprint(current), StringComparison.Ordinal))
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "manifest-changed-restart-required",
                    "Runtime-authoritative manifest identity, version, minimum, dependency or entry fields changed after load; restart is required.");
            }

            return null;
        }

        private static string ManifestAuthorityFingerprint(ManifestModel manifest)
        {
            string[] dependencies = (manifest.DependencyModels ?? new List<ManifestDependencyModel>())
                .Select(dependency =>
                    (dependency.UniqueID ?? string.Empty) + "\u001f" +
                    (dependency.MinimumVersion ?? string.Empty) + "\u001f" +
                    dependency.Required.ToString(CultureInfo.InvariantCulture))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] updateKeys = (manifest.UpdateKeyModels ?? new List<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return string.Join("\u001e", new[]
            {
                manifest.Name ?? string.Empty,
                manifest.Author ?? string.Empty,
                manifest.Description ?? string.Empty,
                manifest.UniqueID ?? string.Empty,
                manifest.Version ?? string.Empty,
                manifest.Type ?? string.Empty,
                manifest.EntryDll ?? string.Empty,
                manifest.EntryType ?? string.Empty,
                manifest.MinimumDTMApiVersion ?? string.Empty,
                manifest.MinimumGameVersion ?? string.Empty,
                string.Join("\u001d", dependencies),
                string.Join("\u001d", updateKeys)
            });
        }

        private AuthorSessionOperationResult CreateAuthorSourceSnapshotResult(
            AuthorSourceSelectionDecision decision,
            DiscoveredMod selected,
            string currentTreeSha256)
        {
            DiscoveredMod[] shadowed = decision.Candidates
                .Where(candidate => !IsSelectedSourceCandidate(candidate, selected))
                .ToArray();
            const int maximumReportedShadowedCandidates = 2;
            DiscoveredMod[] reported = shadowed.Take(maximumReportedShadowedCandidates).ToArray();
            var values = new List<KeyValuePair<string, string>>
            {
                Pair("mode", decision.Mode.ToString()),
                Pair("source", selected.Source),
                Pair("selectedRoot", selected.RootPath),
                Pair("version", selected.Manifest.Version),
                Pair("nativeSubscriptionVerified", selected.NativeSubscriptionVerified.ToString(CultureInfo.InvariantCulture)),
                Pair("selectionReason", selected.SelectionReason),
                Pair("shadowedCount", shadowed.Length.ToString(CultureInfo.InvariantCulture)),
                Pair("shadowedReturned", reported.Length.ToString(CultureInfo.InvariantCulture)),
                Pair("shadowedTruncated", (reported.Length != shadowed.Length).ToString(CultureInfo.InvariantCulture)),
                Pair("treeHashAlgorithm", AuthorFileTreeDigest.Algorithm),
                Pair("treeSha256", currentTreeSha256)
            };
            values.Add(Pair("officialEnabled", selected.OfficialEnabled.ToString(CultureInfo.InvariantCulture)));
            values.Add(Pair("ownerActive", runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID.Equals(selected.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture)));
            values.AddRange(AuthorAssemblyObservation.Read(selected));

            for (int index = 0; index < reported.Length; index++)
            {
                DiscoveredMod candidate = reported[index];
                string prefix = "shadowed" + index.ToString(CultureInfo.InvariantCulture);
                values.Add(Pair(prefix + "Source", candidate.Source));
                values.Add(Pair(prefix + "Root", candidate.RootPath));
                values.Add(Pair(prefix + "OfficialEnabled", candidate.OfficialEnabled.ToString(CultureInfo.InvariantCulture)));
                values.Add(Pair(prefix + "NativeVerified", candidate.NativeSubscriptionVerified.ToString(CultureInfo.InvariantCulture)));
                values.Add(Pair(prefix + "EnablementReason", candidate.EnablementReason));
                values.Add(Pair(prefix + "ShadowReason", "candidate-not-selected; " + decision.Outcome));
            }

            return AuthorSessionOperationResult.Success(
                "source-snapshot",
                "The Runtime returned its current native-owner-backed source decision.",
                values.ToArray());
        }

        private static bool IsSelectedSourceCandidate(DiscoveredMod candidate, DiscoveredMod selected)
        {
            return AuthorSessionProtocol.PathsEqual(candidate.RootPath, selected.RootPath) &&
                string.Equals(candidate.Source, selected.Source, StringComparison.OrdinalIgnoreCase) &&
                candidate.WorkshopId == selected.WorkshopId &&
                string.Equals(candidate.OfficialId, selected.OfficialId, StringComparison.OrdinalIgnoreCase);
        }

        private AuthorSessionOperationResult ReloadSelectedAuthorContent(DiscoveredMod selected, string currentTreeSha256)
        {
            if (selected.Manifest.Type.Equals("CodeMod", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(selected.Manifest.EntryDll))
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "codemod-restart-required",
                    "Unity Mono CodeMod assemblies are process-lifetime and are never hot reloaded.");
            }

            string dtmapiContentRoot = Path.Combine(selected.RootPath, "Content", "DTMAPI");
            optionalAuthorFileStatusCallCount += 2;
            bool hasAudio = File.Exists(Path.Combine(dtmapiContentRoot, "audio-replacements.json"));
            bool hasCustomAnimals = File.Exists(Path.Combine(dtmapiContentRoot, "custom-animals.json"));
            if (hasCustomAnimals)
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "custom-animals-restart-required",
                    "Custom Animals retain native sprite, Unity asset and live/save-lifetime object references; SDK 0.1.0 does not reload them.");
            }

            if (!hasAudio)
            {
                optionalAuthorFileStatusCallCount++;
                bool nativeOfficialContent = File.Exists(Path.Combine(selected.RootPath, "info.json"));
                if (!nativeOfficialContent)
                {
                    optionalAuthorFileStatusCallCount++;
                    nativeOfficialContent = Directory.Exists(Path.Combine(selected.RootPath, "Content"));
                }
                return AuthorSessionOperationResult.RestartRequired(
                    nativeOfficialContent ? "native-content-restart-required" : "content-format-restart-required",
                    nativeOfficialContent
                        ? "Native official JSON/table content has no per-package transactional reload owner and requires restart."
                        : "This content format has no reviewed reload owner and requires restart.");
            }

            AuthorSessionOperationResult? unownedContent = FindUnownedReloadContentObserved(selected, dtmapiContentRoot);
            if (unownedContent != null)
                return unownedContent;

            AudioReplacementService? service = audioReplacementFeature?.Service;
            if (service == null)
                return AuthorSessionOperationResult.Error("audio-service-unavailable", "The Audio replacement Runtime owner is unavailable.");

            AudioReplacementService.AudioReplacementOwnerReloadResult result = service.ReloadContentPackOwner(
                selected.Manifest.UniqueID,
                selected.RootPath,
                "AuthorSession.reload-content",
                currentTreeSha256);
            KeyValuePair<string, string>[] values =
            {
                Pair("audioStatus", result.Status),
                Pair("generation", result.Generation.ToString(CultureInfo.InvariantCulture)),
                Pair("activeEntryCount", result.ActiveEntryCount.ToString(CultureInfo.InvariantCulture)),
                Pair("retainedPreviousGeneration", result.RetainedPreviousGeneration.ToString(CultureInfo.InvariantCulture)),
                Pair("treeHashAlgorithm", AuthorFileTreeDigest.Algorithm),
                Pair("treeSha256", currentTreeSha256)
            };
            return result.Success
                ? AuthorSessionOperationResult.Success("audio-generation-committed", result.Message, values)
                : AuthorSessionOperationResult.Rejected("audio-generation-" + result.Status, result.Message, values);
        }

        private static AuthorSessionOperationResult? FindUnownedReloadContent(DiscoveredMod selected, string dtmapiContentRoot) =>
            FindUnownedReloadContentCore(selected, dtmapiContentRoot, null, null);

        private AuthorSessionOperationResult? FindUnownedReloadContentObserved(DiscoveredMod selected, string dtmapiContentRoot) =>
            FindUnownedReloadContentCore(
                selected,
                dtmapiContentRoot,
                () => optionalAuthorFileStatusCallCount++,
                () => optionalAuthorDirectoryEnumerationCount++);

        private static AuthorSessionOperationResult? FindUnownedReloadContentCore(
            DiscoveredMod selected,
            string dtmapiContentRoot,
            Action? beforeFileStatus,
            Action? beforeDirectoryEnumeration)
        {
            AuthorSessionOperationResult? metadataValidation = ValidateSdkPackageMetadata(selected, dtmapiContentRoot, beforeFileStatus);
            if (metadataValidation != null)
                return metadataValidation;

            string contentRoot = Path.Combine(selected.RootPath, "Content");
            beforeFileStatus?.Invoke();
            if (!Directory.Exists(contentRoot))
                return null;

            string audioSchema = Path.Combine(dtmapiContentRoot, "audio-replacements.json");
            string packageMarker = Path.Combine(dtmapiContentRoot, "dtmapi-package.json");
            try
            {
                beforeDirectoryEnumeration?.Invoke();
                foreach (string file in Directory.EnumerateFiles(contentRoot, "*", SearchOption.AllDirectories))
                {
                    if (AuthorSessionProtocol.PathsEqual(file, audioSchema) ||
                        AuthorSessionProtocol.PathsEqual(file, packageMarker) ||
                        AuthorSessionProtocol.PathsEqual(file, selected.ManifestPath) ||
                        file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                        (IsPathUnder(file, dtmapiContentRoot) &&
                            (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))))
                    {
                        continue;
                    }

                    bool underDtmapi = IsPathUnder(file, dtmapiContentRoot);
                    return AuthorSessionOperationResult.RestartRequired(
                        underDtmapi ? "content-format-restart-required" : "native-content-restart-required",
                        underDtmapi
                            ? "The package contains a DTMAPI content format outside the reviewed Audio-only reload owner: " + file + "."
                            : "The package contains native/other Content files outside the reviewed Audio-only reload owner: " + file + ".");
                }
            }
            catch (Exception ex)
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "content-inventory-unreadable-restart-required",
                    "The package content inventory could not be proven Audio-only: " + ex.GetType().Name + ": " + ex.Message);
            }

            return null;
        }

        private static AuthorSessionOperationResult? ValidateSdkPackageMetadata(
            DiscoveredMod selected,
            string dtmapiContentRoot,
            Action? beforeFileStatus)
        {
            string infoPath = Path.Combine(selected.RootPath, "info.json");
            beforeFileStatus?.Invoke();
            if (File.Exists(infoPath))
            {
                try
                {
                    OfficialInfoProjection info = ReadJson<OfficialInfoProjection>(infoPath);
                    if (!string.Equals(info.Version, selected.Manifest.Version, StringComparison.Ordinal) ||
                        !string.Equals(info.Name, selected.Manifest.Name, StringComparison.Ordinal) ||
                        !string.Equals(info.Author, selected.Manifest.Author, StringComparison.Ordinal))
                    {
                        return AuthorSessionOperationResult.RestartRequired(
                            "official-info-changed-restart-required",
                            "SDK-projected info.json no longer matches the active Runtime manifest.");
                    }
                }
                catch (Exception ex)
                {
                    return AuthorSessionOperationResult.RestartRequired(
                        "official-info-unreadable-restart-required",
                        "SDK-projected info.json could not be revalidated: " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            string markerPath = Path.Combine(dtmapiContentRoot, "dtmapi-package.json");
            beforeFileStatus?.Invoke();
            if (File.Exists(markerPath))
            {
                try
                {
                    AuthorPackageMarker.ValidateIfPresent(selected.RootPath, selected.ManifestPath,
                        selected.Manifest.UniqueID, selected.Manifest.Version, "ContentPack", string.Empty, string.Empty,
                        selected.Manifest.MinimumDTMApiVersion);
                }
                catch (Exception ex)
                {
                    return AuthorSessionOperationResult.RestartRequired(
                        "package-metadata-unreadable-restart-required",
                        "dtmapi-package.json could not be revalidated: " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            return null;
        }

        private static T ReadJson<T>(string path) where T : class
        {
            using FileStream stream = File.OpenRead(path);
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)(serializer.ReadObject(stream) ?? throw new InvalidDataException("JSON object was empty."));
        }

        private static bool IsPathUnder(string path, string root)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        [DataContract]
        private sealed class OfficialInfoProjection
        {
            [DataMember(Name = "name")] public string Name { get; set; } = string.Empty;
            [DataMember(Name = "author")] public string Author { get; set; } = string.Empty;
            [DataMember(Name = "version")] public string Version { get; set; } = string.Empty;
        }

        private static KeyValuePair<string, string> Pair(string key, string value) =>
            new KeyValuePair<string, string>(key ?? string.Empty, value ?? string.Empty);
    }
}
