using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
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

            if (string.Equals(request.Operation, AuthorSessionProtocol.GetSourceSnapshotOperation, StringComparison.Ordinal))
                return CreateAuthorSourceSnapshotResult(decision, selected);
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

            return ReloadSelectedAuthorContent(active);
        }

        private AuthorSessionOperationResult CreateAuthorSourceSnapshotResult(
            AuthorSourceSelectionDecision decision,
            DiscoveredMod selected)
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
                Pair("shadowedTruncated", (reported.Length != shadowed.Length).ToString(CultureInfo.InvariantCulture))
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

        private AuthorSessionOperationResult ReloadSelectedAuthorContent(DiscoveredMod selected)
        {
            if (selected.Manifest.Type.Equals("CodeMod", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(selected.Manifest.EntryDll))
            {
                return AuthorSessionOperationResult.RestartRequired(
                    "codemod-restart-required",
                    "Unity Mono CodeMod assemblies are process-lifetime and are never hot reloaded.");
            }

            return AuthorSessionOperationResult.RestartRequired(
                "content-restart-required",
                "Content reload is not supported by the current Runtime; restart to load changes.");
        }

        private static KeyValuePair<string, string> Pair(string key, string value) =>
            new KeyValuePair<string, string>(key ?? string.Empty, value ?? string.Empty);
    }
}
