using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingManagerLifecycleSettings
    {
        internal const string CaseId = "Batch6AutoFishingManagerLifecycle";
        internal const string SameProcessDisableMode = "SameProcessDisable";
        internal const string ColdDisabledMode = "ColdDisabled";
        internal const string OverallHookId = "Smoke.Batch6.AutoFishingManagerLifecycle";
        internal const string HandshakeHookId = "Smoke.Batch6.AutoFishingManagerLifecycle.Handshake";
        internal const string CleanupHookId = "Smoke.Batch6.AutoFishingManagerLifecycle.Cleanup";

        private static readonly string[] Modes = { SameProcessDisableMode, ColdDisabledMode };

        [DataMember(Name = "Enabled")]
        internal bool Enabled { get; set; }

        [DataMember(Name = "Mode")]
        internal string Mode { get; set; } = SameProcessDisableMode;

        [DataMember(Name = "ExpectedProductRoot")]
        internal string ExpectedProductRoot { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedDisabledMarkerSha256")]
        internal string ExpectedDisabledMarkerSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedPackageSha256")]
        internal string ExpectedPackageSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedEntryDllSha256")]
        internal string ExpectedEntryDllSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedManifestSha256")]
        internal string ExpectedManifestSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedReferencePolicySha256")]
        internal string ExpectedReferencePolicySha256 { get; set; } = string.Empty;

        internal bool IsSameProcessDisable => string.Equals(Mode, SameProcessDisableMode, StringComparison.Ordinal);
        internal bool IsColdDisabled => string.Equals(Mode, ColdDisabledMode, StringComparison.Ordinal);
        internal string DisabledMarkerPath => Path.Combine(ExpectedProductRoot, "dtmapi.disabled");

        internal void NormalizeAndValidate(bool caseSelected, int saveSlot)
        {
            bool active = Enabled || caseSelected;
            string normalizedMode = (Mode ?? string.Empty).Trim();
            Mode = Modes.FirstOrDefault(value => value.Equals(normalizedMode, StringComparison.OrdinalIgnoreCase)) ?? normalizedMode;
            if (!active)
                return;
            if (!Enabled || !caseSelected)
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle settings and the single G6 case must be enabled together.");
            if (saveSlot != 5)
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle requires the authoritative fifth save slot.");
            if (!Modes.Contains(Mode, StringComparer.Ordinal))
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle Mode must be SameProcessDisable or ColdDisabled.");

            ExpectedProductRoot = Path.GetFullPath((ExpectedProductRoot ?? string.Empty).Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Path.IsPathRooted(ExpectedProductRoot) ||
                !string.Equals(Path.GetFileName(ExpectedProductRoot), Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle requires the exact absolute managed AutoFishing product root.");
            }
            ExpectedDisabledMarkerSha256 = Batch6AutoFishingPilotSettings.NormalizeSha256(ExpectedDisabledMarkerSha256, nameof(ExpectedDisabledMarkerSha256));
            ExpectedPackageSha256 = Batch6AutoFishingPilotSettings.NormalizeSha256(ExpectedPackageSha256, nameof(ExpectedPackageSha256));
            ExpectedEntryDllSha256 = Batch6AutoFishingPilotSettings.NormalizeSha256(ExpectedEntryDllSha256, nameof(ExpectedEntryDllSha256));
            ExpectedManifestSha256 = Batch6AutoFishingPilotSettings.NormalizeSha256(ExpectedManifestSha256, nameof(ExpectedManifestSha256));
            ExpectedReferencePolicySha256 = Batch6AutoFishingPilotSettings.NormalizeSha256(ExpectedReferencePolicySha256, nameof(ExpectedReferencePolicySha256));
        }

        internal Batch6AutoFishingPilotSettings CreatePackageVerificationSettings() => new Batch6AutoFishingPilotSettings
        {
            ExpectedPackageSha256 = ExpectedPackageSha256,
            ExpectedEntryDllSha256 = ExpectedEntryDllSha256,
            ExpectedManifestSha256 = ExpectedManifestSha256,
            ExpectedReferencePolicySha256 = ExpectedReferencePolicySha256
        };
    }
}
