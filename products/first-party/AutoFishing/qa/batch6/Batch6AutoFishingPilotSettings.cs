using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingPilotSettings
    {
        internal const string CaseId = "Batch6AutoFishingPilot";
        internal const string ProductUniqueId = "Yuuka.DTMAPI.AutoFishing";
        internal const string ProductEntryType = "Yuuka.DTMAPI.AutoFishing.ModEntry";
        internal const string ProductEntryDll = "Yuuka.DTMAPI.AutoFishing.dll";
        internal const string ProductAssemblyName = "Yuuka.DTMAPI.AutoFishing";
        internal const string ProductHarmonyAssemblyName = "0Harmony";
        internal const int ExpectedProductPatchCount = 22;
        internal const int ObservationCadenceMilliseconds = 250;
        internal const int DirectNeutralStabilityMilliseconds = 1000;
        internal const int DirectNeutralReadyTimeoutSeconds = 30;
        internal const int NativeFishingContextReadyTimeoutSeconds = 30;
        internal const string OverallHookId = "Smoke.Batch6.AutoFishingPilot";
        internal const string HandshakeHookId = "Smoke.Batch6.AutoFishingPilot.Handshake";
        internal const string CleanupHookId = "Smoke.Batch6.AutoFishingPilot.Cleanup";
        internal const string LongRunContract = "LongRun";
        internal const string BehaviorContract = "Behavior";
        internal static readonly string[] ManualMovementTargetPhases = { "Ready", "Cast", "Wait", "BiteReady", "MiniGame" };

        private static readonly string[] Levels = { "L0", "L1", "L2", "L3", "L4", "L5" };
        private static readonly string[] Scenarios =
        {
            "DefaultLoop",
            "InstantBite",
            "SkipMiniGame",
            "InstantSkip",
            "FastAnimations",
            "CombinedInstantSkip",
            "CombinedInstantComplete"
        };
        private static readonly string[] FormalContracts = { LongRunContract, BehaviorContract };

        [DataMember(Name = "Enabled")]
        internal bool Enabled { get; set; }

        [DataMember(Name = "Level")]
        internal string Level { get; set; } = string.Empty;

        [DataMember(Name = "Scenario")]
        internal string Scenario { get; set; } = "CombinedInstantSkip";

        [DataMember(Name = "MeasureSeconds")]
        internal int MeasureSeconds { get; set; } = 600;

        [DataMember(Name = "SampleSeconds")]
        internal int SampleSeconds { get; set; } = 30;

        [DataMember(Name = "WarmupFish")]
        internal int WarmupFish { get; set; } = 5;

        [DataMember(Name = "TargetFish")]
        internal int TargetFish { get; set; } = 10;

        [DataMember(Name = "Multiplier")]
        internal double Multiplier { get; set; } = 3d;

        [DataMember(Name = "Formal")]
        internal bool Formal { get; set; }

        [DataMember(Name = "FormalContract")]
        internal string FormalContractKind { get; set; } = LongRunContract;

        [DataMember(Name = "CastChargeRatio")]
        internal double CastChargeRatio { get; set; }

        [DataMember(Name = "ManualMovementCancel")]
        internal bool ManualMovementCancel { get; set; }

        [DataMember(Name = "ToggleKey")]
        internal string ToggleKey { get; set; } = "F6";

        [DataMember(Name = "ExpectedPackageSha256")]
        internal string ExpectedPackageSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedEntryDllSha256")]
        internal string ExpectedEntryDllSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedManifestSha256")]
        internal string ExpectedManifestSha256 { get; set; } = string.Empty;

        [DataMember(Name = "ExpectedReferencePolicySha256")]
        internal string ExpectedReferencePolicySha256 { get; set; } = string.Empty;

        internal bool ProductMustBeAbsent => string.Equals(Level, "L0", StringComparison.Ordinal);

        internal void NormalizeAndValidate(bool caseSelected, int saveSlot)
        {
            Level = (Level ?? string.Empty).Trim().ToUpperInvariant();
            bool active = Enabled || caseSelected;
            Scenario = !active && string.IsNullOrWhiteSpace(Scenario)
                ? "CombinedInstantSkip"
                : Canonicalize(Scenario, Scenarios, "scenario");
            FormalContractKind = Canonicalize(FormalContractKind, FormalContracts, "formal contract");
            ToggleKey = string.IsNullOrWhiteSpace(ToggleKey)
                ? "F6"
                : ToggleKey.Trim().ToUpperInvariant();
            ExpectedPackageSha256 = NormalizeSha256(ExpectedPackageSha256, nameof(ExpectedPackageSha256), Enabled || caseSelected);
            ExpectedEntryDllSha256 = NormalizeSha256(ExpectedEntryDllSha256, nameof(ExpectedEntryDllSha256), Enabled || caseSelected);
            ExpectedManifestSha256 = NormalizeSha256(ExpectedManifestSha256, nameof(ExpectedManifestSha256), Enabled || caseSelected);
            ExpectedReferencePolicySha256 = NormalizeSha256(ExpectedReferencePolicySha256, nameof(ExpectedReferencePolicySha256), Enabled || caseSelected);

            if (!active)
                return;
            if (!Enabled || !caseSelected)
                throw new InvalidDataException("Batch6AutoFishingPilot settings and the single G6 case must be enabled together.");
            if (saveSlot != 5)
                throw new InvalidDataException("Batch6AutoFishingPilot requires the authoritative fifth save slot.");
            if (!Levels.Contains(Level, StringComparer.Ordinal))
                throw new InvalidDataException("Batch6AutoFishingPilot Level must be L0 through L5.");
            if (MeasureSeconds <= 0 || SampleSeconds <= 0 || SampleSeconds > MeasureSeconds)
                throw new InvalidDataException("Batch6AutoFishingPilot measurement and sample durations are invalid.");
            if (FormalContractKind == LongRunContract && (WarmupFish < 5 || TargetFish < 10))
                throw new InvalidDataException("Batch6AutoFishingPilot LongRun requires at least 5 warm-up fish and 10 measured fish.");
            if (FormalContractKind == BehaviorContract && (WarmupFish < 0 || TargetFish < 1))
                throw new InvalidDataException("Batch6AutoFishingPilot Behavior requires a non-negative warm-up and at least one measured fish target.");
            if (Formal && FormalContractKind == LongRunContract &&
                (MeasureSeconds != 600 || SampleSeconds != 30 || WarmupFish != 5 || TargetFish != 10))
                throw new InvalidDataException("Formal Batch6AutoFishingPilot LongRun requires exactly 600 measurement seconds, 30 sample seconds, 5 warm-up fish, and 10 measured fish.");
            if (Formal && FormalContractKind == BehaviorContract &&
                (MeasureSeconds != 1 || SampleSeconds != 1 || WarmupFish != 0 || TargetFish != 1))
                throw new InvalidDataException("Formal Batch6AutoFishingPilot Behavior requires exactly 1 measurement second, 1 sample second, 0 warm-up fish, and 1 measured fish.");
            if (double.IsNaN(Multiplier) || double.IsInfinity(Multiplier) || Multiplier < 1d || Multiplier > 4d)
                throw new InvalidDataException("Batch6AutoFishingPilot Multiplier must be between 1 and 4.");
            if (double.IsNaN(CastChargeRatio) || double.IsInfinity(CastChargeRatio) || CastChargeRatio < 0d || CastChargeRatio > 1d)
                throw new InvalidDataException("Batch6AutoFishingPilot CastChargeRatio must be between 0 and 1.");
            if (ManualMovementCancel && FormalContractKind != BehaviorContract)
                throw new InvalidDataException("Batch6AutoFishingPilot manual movement cancellation belongs only to the Behavior contract.");
            if (ManualMovementCancel && Level != "L1")
                throw new InvalidDataException("Batch6AutoFishingPilot manual movement cancellation requires the real Advanced product on L1.");
            if (ToggleKey != "F6" && ToggleKey != "F7")
                throw new InvalidDataException("Batch6AutoFishingPilot ToggleKey must be the exact physical F6 or F7 binding.");
            if (ManualMovementCancel && ToggleKey != "F7")
                throw new InvalidDataException("Batch6AutoFishingPilot manual movement cancellation requires the actual rebound F7 toggle path.");
            if (Formal && FormalContractKind == BehaviorContract && ProductMustBeAbsent)
                throw new InvalidDataException("Formal Batch6AutoFishingPilot Behavior requires the real Advanced product and cannot run as product-absent L0.");
        }

        internal bool IsFormalLongRunContract => Formal && FormalContractKind == LongRunContract &&
            MeasureSeconds == 600 && SampleSeconds == 30 && WarmupFish == 5 && TargetFish == 10;

        internal bool IsFormalBehaviorContract => Formal && FormalContractKind == BehaviorContract &&
            MeasureSeconds == 1 && SampleSeconds == 1 && WarmupFish == 0 && TargetFish == 1;

        internal bool IsFormalContract => IsFormalLongRunContract || IsFormalBehaviorContract;

        internal string AuthorityScope => IsFormalLongRunContract
            ? "formal-long-run"
            : IsFormalBehaviorContract
                ? "formal-behavior-profile"
                : "non-authoritative";

        internal static string NormalizeSha256(string? value, string label, bool required = true)
        {
            string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (!required && normalized.Length == 0)
                return string.Empty;
            if (normalized.Length != 64 || normalized.Any(character => !IsHex(character)))
                throw new InvalidDataException(label + " must be one exact 64-character SHA-256 value.");
            return normalized;
        }

        private static string Canonicalize(string? value, string[] allowed, string label)
        {
            string normalized = (value ?? string.Empty).Trim();
            string? exact = allowed.FirstOrDefault(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (exact == null)
                throw new InvalidDataException("Unsupported Batch6AutoFishingPilot " + label + " " + normalized + ".");
            return exact;
        }

        private static bool IsHex(char value) =>
            (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F');
    }
}
