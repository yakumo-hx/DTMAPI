using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using DTMAPI.MoreEquipmentSlots;

namespace MesAdoptionFixture
{
    // Private route experiment. No production entry calls this code; no file cleanup/replay.
    internal enum ReadKind { Missing, Supported, Unsupported, Invalid }
    internal enum LocalOption
    {
        Unavailable, AlreadyAdopted, ReuseCurrentGroup, ChooseEmptyGroup,
        AdoptExactLegacyCheckpoint, InsufficientEvidence
    }
    internal sealed record Qualification(LocalOption Option, string Reason)
    {
        internal bool CanStageLocal => Option == LocalOption.ChooseEmptyGroup ||
            Option == LocalOption.AdoptExactLegacyCheckpoint;
    }

    // Hypothetical single embedded payload, deliberately not a shipped schema.
    // The set records source bytes, not item identities, machines, paths or cleanup jobs.
    internal sealed class Payload
    {
        public int SchemaVersion { get; set; } = 1;
        public List<EquipmentSlotStorageEntry>? Slots { get; set; }
        public List<string>? AdoptedSources { get; set; }
    }
    internal sealed record Embedded(ReadKind Kind, object? Original, Payload? Value)
    {
        internal string? Raw => Original as string;
        internal static readonly JsonSerializerOptions JsonOptions = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        internal static Embedded Read(bool keyExists, object? value)
        {
            if (!keyExists) return new(ReadKind.Missing, null, null);
            if (value is not string raw) return new(ReadKind.Invalid, value, null);
            try
            {
                using var json = JsonDocument.Parse(raw);
                if (json.RootElement.ValueKind != JsonValueKind.Object ||
                    !json.RootElement.TryGetProperty("schemaVersion", out var schema) ||
                    schema.ValueKind != JsonValueKind.Number ||
                    !schema.TryGetInt32(out int version)) return new(ReadKind.Invalid, raw, null);
                if (version != 1) return new(ReadKind.Unsupported, raw, null);
                Payload? payload = JsonSerializer.Deserialize<Payload>(raw, JsonOptions);
                if (payload?.Slots == null || payload.Slots.Count != 3 ||
                    payload.AdoptedSources == null || payload.AdoptedSources.Any(s =>
                        s == null || s.Length != 64 || s.Any(c => !Uri.IsHexDigit(c))) ||
                    payload.AdoptedSources.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                        payload.AdoptedSources.Count)
                    return new(ReadKind.Invalid, raw, null);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < 3; i++)
                {
                    var slot = payload.Slots[i];
                    if (slot == null || slot.Index != i || slot.ItemId == null ||
                        slot.DefenseBonus < 0 || slot.ShieldValue < 0 || slot.ShieldMaxValue < slot.ShieldValue ||
                        slot.ShieldDefend < 0 ||
                        (slot.IsShield && slot.ShieldMaxValue <= 0) ||
                        (!slot.IsShield && (slot.ShieldValue != 0 || slot.ShieldMaxValue != 0 || slot.ShieldDefend != 0)) ||
                        (slot.IsOccupied && (slot.ItemState?.IsValid != true ||
                            slot.IsShield != (slot.ItemState.NativeType == "DolocTown.ItemHatShield") ||
                            !ids.Add(slot.ItemState.InstanceId))) ||
                        (!slot.IsOccupied && (slot.ItemState != null || slot.IsShield || slot.DefenseBonus != 0 ||
                            !string.IsNullOrEmpty(slot.DisplayName) || !string.IsNullOrEmpty(slot.SkillId))))
                        return new(ReadKind.Invalid, raw, null);
                }
                return new(ReadKind.Supported, raw, payload);
            }
            catch (JsonException) { return new(ReadKind.Invalid, raw, null); }
        }

        internal static Embedded FromSlots(IEnumerable<EquipmentSlotStorageEntry> slots,
            IEnumerable<string>? adopted = null) => Read(true, JsonSerializer.Serialize(new Payload
            {
                Slots = slots.Select(s => s.Clone()).ToList(),
                AdoptedSources = (adopted ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.Ordinal).ToList()
            }, JsonOptions));
    }

    internal sealed record LegacyInput(string Path, byte[] Bytes, string Digest,
        EquipmentSlotStorageFormat Format, EquipmentSlotStorageDocument? Document, string Failure)
    {
        // Exact path only. In particular, do not call LoadOrMigrate or silently pick .previous.
        internal static LegacyInput Read(string path, EquipmentSlotSaveScope scope)
        {
            byte[] before = File.ReadAllBytes(path);
            var probe = EquipmentSlotStorageFormatClassifier.Probe(path);
            EquipmentSlotStorageDocument? doc = null;
            string failure = probe.Failure;
            if (probe.Format == EquipmentSlotStorageFormat.ProductV3)
                EquipmentSlotDocumentStore.TryReadValidated(path, scope, out doc, out failure);
            else if (probe.Format == EquipmentSlotStorageFormat.LegacyFlat ||
                     probe.Format == EquipmentSlotStorageFormat.PreSchemaGlobal)
                EquipmentSlotLegacyMigration.TryConvert(path, scope,
                    probe.Format == EquipmentSlotStorageFormat.PreSchemaGlobal,
                    out doc, out _, out failure);
            if (!File.ReadAllBytes(path).SequenceEqual(before))
                throw new IOException("Fixture changed during read; interpretation invalidated.");
            return new(path, before, Convert.ToHexString(SHA256.HashData(before)),
                probe.Format, doc, failure);
        }
    }

    internal sealed record CheckpointObservation(string Fingerprint,
        Func<string, NativeRecoveryObservation> Observe);

    internal static class QualificationPrototype
    {
        internal static Qualification Evaluate(Embedded current, IReadOnlyList<LegacyInput> sources,
            CheckpointObservation? native = null)
        {
            if (current.Kind == ReadKind.Invalid || current.Kind == ReadKind.Unsupported)
                return new(LocalOption.Unavailable, "embedded-preserve-raw");
            if (sources.Count != 1)
                return new(LocalOption.Unavailable, "one-explained-legacy-authority-required");
            LegacyInput source = sources[0];
            if (source.Document == null)
                return new(LocalOption.Unavailable, "legacy-unreadable:" + source.Failure);
            if (current.Value?.AdoptedSources?.Contains(source.Digest, StringComparer.OrdinalIgnoreCase) == true)
                return new(LocalOption.AlreadyAdopted, "known-source-cannot-restore-items");
            if (source.Format == EquipmentSlotStorageFormat.PreSchemaGlobal)
                return new(LocalOption.InsufficientEvidence, "unscoped-global-has-no-archive-attribution");

            var projection = ProjectWithoutReplay(source.Document, native);
            if (projection.Slots == null)
                return new(LocalOption.Unavailable, projection.Reason);
            if (projection.Slots.Any(s => s.IsOccupied && s.ItemState?.IsValid != true))
                return new(LocalOption.InsufficientEvidence, "legacy-instance-state-incomplete");
            if (current.Value?.Slots != null && SameSlots(current.Value.Slots, projection.Slots))
                return new(LocalOption.ReuseCurrentGroup, "same-complete-group-reuse-current-no-restore");
            if (projection.Slots.All(s => !s.IsOccupied))
                return new(LocalOption.ChooseEmptyGroup, "explicit-empty-choice-mints-no-item");
            if (current.Kind == ReadKind.Missing && projection.ExactCheckpoint)
                return new(LocalOption.AdoptExactLegacyCheckpoint, "exact-existing-transaction-binds-first-import");
            return new(LocalOption.InsufficientEvidence, "unknown-history-cannot-prove-exclusive-custody");
        }

        // A minimal in-memory Working transition. Persistence below is tested by a model
        // roundtrip only; this method does not accept SaveSaved as native commit evidence.
        internal static Embedded StageLocal(Embedded current, LegacyInput source,
            CheckpointObservation? native = null)
        {
            Qualification decision = Evaluate(current, new[] { source }, native);
            if (!decision.CanStageLocal) throw new InvalidOperationException(decision.Reason);
            var projection = ProjectWithoutReplay(source.Document!, native);
            return Embedded.FromSlots(projection.Slots!,
                (current.Value?.AdoptedSources ?? new List<string>()).Append(source.Digest));
        }

        internal static bool SameSlots(IReadOnlyList<EquipmentSlotStorageEntry> a,
            IReadOnlyList<EquipmentSlotStorageEntry> b) => a.Count == b.Count &&
            a.Zip(b, (x, y) => x.Index == y.Index && x.ItemId == y.ItemId &&
                x.DisplayName == y.DisplayName && x.SkillId == y.SkillId &&
                x.DefenseBonus == y.DefenseBonus && x.IsShield == y.IsShield &&
                x.ShieldValue == y.ShieldValue && x.ShieldMaxValue == y.ShieldMaxValue &&
                x.ShieldDefend == y.ShieldDefend && EquipmentSlotItemState.Same(x.ItemState, y.ItemState)).All(x => x);

        // Only an exact recorded preimage or committed postimage is a checkpoint witness.
        // A changed fingerprint plus matching counts can explain an old transaction under
        // its old policy, but is not permission to import it into an arbitrary later save.
        private static (List<EquipmentSlotStorageEntry>? Slots, bool ExactCheckpoint, string Reason)
            ProjectWithoutReplay(EquipmentSlotStorageDocument source, CheckpointObservation? native)
        {
            var slots = source.Slots.Select(s => s.Clone()).ToList();
            if (source.Journal == null && source.GameplayCandidate == null)
                return (slots, false, "settled-without-checkpoint-witness");
            if (native == null) return (null, false, "transaction-requires-native-evidence");
            if (source.GameplayCandidate is EquipmentSlotGameplayCandidate candidate)
            {
                var decision = EquipmentSlotGameplayCandidateCoordinator.DecideRecovery(
                    source, native.Fingerprint, native.Observe);
                if (candidate.State == EquipmentSlotGameplayCandidateState.Prepared &&
                    decision.Action == EquipmentSlotGameplayRecoveryAction.DiscardUncommitted)
                    return (slots, true, "exact-preimage-discards-abandoned-working");
                if (candidate.State == EquipmentSlotGameplayCandidateState.CommittedTombstone &&
                    decision.Action == EquipmentSlotGameplayRecoveryAction.FinalizeCommitted)
                    return (slots, true, "exact-committed-gameplay-postimage");
                return (null, false, "gameplay-transaction-not-an-exact-checkpoint");
            }
            EquipmentSlotTransactionJournal journal = source.Journal!;
            var recovery = EquipmentSlotTransactionCoordinator.DecideRecovery(source,
                native.Fingerprint, native.Observe);
            if (journal.State != EquipmentSlotJournalState.CommittedTombstone ||
                recovery.Action != EquipmentSlotRecoveryAction.FinalizeCommitted)
                return (null, false, "owner-or-orphan-transaction-requires-resolution");
            // Use the existing finalizer on a private copy; never replay placement or mutate raw input.
            var copy = new EquipmentSlotStorageDocument
            { Scope = source.Scope.Clone(), Generation = source.Generation, Slots = slots, Journal = journal };
            EquipmentSlotTransactionCoordinator.FinalizeCommitted(copy);
            return (copy.Slots, true, "exact-recovery-postimage-excludes-returned-escrow");
        }

        // Shared requirement for all three existing MES recovery dispatch paths.
        // Exercised as a model gate, NOT wired to the production Compatibility Host.
        internal static bool MayEnterOldCompatibilityRecovery(string owner, Embedded current) =>
            owner != MoreEquipmentSlotsProductContract.UniqueId || current.Kind == ReadKind.Missing;
    }
}
