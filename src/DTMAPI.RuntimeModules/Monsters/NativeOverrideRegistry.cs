using System;
using System.Collections.Generic;
using System.Linq;


namespace DTMAPI.MonsterContent.Hosting
{
    // Only the paired facade/backend can pass a trusted index. Author DTOs contain no order.
    internal interface IMonsterOrderedDefinitionBackend
    {
        MonsterResult<MonsterDefinitionLease> Register(string owner, int loadIndex, MonsterDefinition definition);
    }
    internal interface IMonsterOverrideBackend
    {
        MonsterResult<MonsterOverrideLease> RegisterOverride(string owner, int loadIndex, MonsterNativeOverride value);
        IReadOnlyList<MonsterOverrideSnapshot> GetOverrides();
    }

    // Pure arbitration; the native adapter supplies the current raw baseline at every read.
    internal sealed class NativeOverrideRegistry
    {
        private sealed class Record
        {
            internal string Owner = "", Id = "";
            internal MonsterNativeOverride Value = null!;
            internal MonsterOverrideState State;
            internal MonsterDiagnostic? Diagnostic;
            internal int LoadIndex;
            internal long DeclarationOrder;
        }
        private sealed class Choice
        {
            internal Record? Winner;
        }
        private readonly Dictionary<string, Record> records = new Dictionary<string, Record>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> nextOrders = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, Choice>> choices = new Dictionary<string, Dictionary<string, Choice>>(StringComparer.Ordinal);
        private readonly Action thread;
        private readonly Func<string, string, double?> baseline;
        private readonly Func<string, string, MonsterNativeOverride, MonsterDiagnostic?> admission;
        private readonly Action? changed;
        private bool ready;
        private long generation;
        internal NativeOverrideRegistry(Action thread, Func<string, string, double?> baseline, Func<string, string, MonsterNativeOverride, MonsterDiagnostic?> admission, Action? changed = null)
        { this.thread = thread; this.baseline = baseline; this.admission = admission; this.changed = changed; }
        internal MonsterResult<MonsterOverrideLease> Register(string owner, int loadIndex, MonsterNativeOverride value, bool definitionPolicy = false)
        {
            thread(); var error = MonsterOverrideAdmission.Validate(owner, value);
            if (error != null) return MonsterResult<MonsterOverrideLease>.Fail(error);
            string id = owner + ":" + value.LocalId + (definitionPolicy ? "/drops" : "");
            if (records.ContainsKey(id)) return MonsterResult<MonsterOverrideLease>.Fail(new MonsterDiagnostic("DuplicateId", "An active native override already uses this owner/local ID.", owner, id));
            var record = new Record { Owner = owner, Id = id, Value = value.Copy(), LoadIndex = loadIndex };
            Validate(record);
            if (record.State == MonsterOverrideState.Rejected) return MonsterResult<MonsterOverrideLease>.Fail(record.Diagnostic!);
            nextOrders.TryGetValue(owner, out long declarationOrder);
            record.DeclarationOrder = declarationOrder;
            nextOrders[owner] = checked(declarationOrder + 1);
            records.Add(id, record);
            try { Rebuild(); }
            catch (Exception apply)
            {
                record.State = MonsterOverrideState.CleanupPending;
                Rebuild(notify: false);
                record.Diagnostic = new MonsterDiagnostic("NativeOverrideApplyFailed", apply.Message, owner, id, value.TargetId, "NativeOverride");
                return MonsterResult<MonsterOverrideLease>.FailedOwned(record.Diagnostic,
                    new MonsterOverrideLease(() => { thread(); return Snapshot(record); }, () => Withdraw(record)));
            }
            return MonsterResult<MonsterOverrideLease>.Ok(new MonsterOverrideLease(() => { thread(); return Snapshot(record); }, () => Withdraw(record)));
        }
        private void Validate(Record record)
        {
            record.Diagnostic = ready ? admission(record.Owner, record.Id, record.Value) : null;
            if (record.Diagnostic != null)
            {
                var d = record.Diagnostic;
                record.Diagnostic = new MonsterDiagnostic(d.Code, d.Message, record.Owner, record.Id, record.Value.TargetId, "NativeOverride", d.Field, d.Recovery);
            }
            record.State = !ready || record.Diagnostic?.Code == "WaitingNative" ? MonsterOverrideState.WaitingNative : record.Diagnostic == null ? MonsterOverrideState.Ready : MonsterOverrideState.Rejected;
        }
        internal void Refresh(bool isReady, long epoch)
        { thread(); ready = isReady; generation = epoch; foreach (var record in records.Values.Where(r => r.State != MonsterOverrideState.CleanupPending)) Validate(record); Rebuild(); }
        private static IEnumerable<string> Fields(Record r)
            => MonsterOverrideAdmission.Fields(r.Value);
        private static double Value(Record record, string field) => NativeAttackValidation.IsAttackField(field) ? NativeAttackValidation.Value(record.Value.NativeAttacks, field) : field == "defense" ? record.Value.Defense!.Value : record.Value.HurtDuration!.Value;
        private void Rebuild(bool notify = true)
        {
            choices.Clear();
            foreach (var target in records.Values.Where(r => r.State == MonsterOverrideState.Ready).GroupBy(r => r.Value.TargetId))
            {
                var fields = new Dictionary<string, Choice>(StringComparer.Ordinal); choices.Add(target.Key, fields);
                foreach (var group in target.SelectMany(r => Fields(r).Where(f => f != "drops" || r.Value.Drops?.Mode == MonsterDropMode.Replace).Select(f => new { r, f })).GroupBy(x => x.f))
                {
                    var winner = group.Select(x => x.r).OrderByDescending(r => r.LoadIndex).ThenByDescending(r => r.DeclarationOrder).First();
                    fields.Add(group.Key, new Choice { Winner = winner });
                }
            }
            if (notify) changed?.Invoke();
        }
        private Choice? Find(string target, string field) => choices.TryGetValue(target, out var fields) && fields.TryGetValue(field, out var choice) ? choice : null;
        internal IEnumerable<MonsterNativeOverride> ReadyValues => records.Values.Where(r => r.State == MonsterOverrideState.Ready).Select(r => r.Value);
        internal bool HasWaitingSounds => records.Values.Any(r => r.State == MonsterOverrideState.WaitingNative && (r.Value.Sounds != null || r.Value.NativeAttacks.Any(a => NativeAttackValidation.Sounds(a).Any())));
        internal IEnumerable<string> Targets(string field) => choices.Where(t => t.Value.ContainsKey(field)).Select(t => t.Key);
        internal string SoundOwner(string target, string field) => Find(target, field)?.Winner?.Owner ?? "";
        internal string SoundWinner(string target, string field) => Find(target, field)?.Winner?.Id ?? "";
        internal MonsterSound? ResolveSound(string target, string field) => IsAttackSoundField(field) ? AttackSoundInput(Find(target, field)?.Winner, field) :
            SoundValidation.Events(Find(target, field)?.Winner?.Value.Sounds).FirstOrDefault(s => "sounds." + s.Key == field).Value;
        internal IEnumerable<string> NativeAttackTargets => choices.Where(t => t.Value.Keys.Any(f => NativeAttackValidation.IsAttackField(f) && !IsAttackSoundField(f))).Select(t => t.Key);
        internal MonsterSkin? ResolveSkin(string target) => Find(target, "skin")?.Winner?.Value.Skin;
        internal MonsterDrops? ResolveDrops(string target) => Find(target, "drops")?.Winner?.Value.Drops;
        internal IEnumerable<MonsterDrops> AppendDrops(string target) => records.Values.Where(r => r.State == MonsterOverrideState.Ready && r.Value.TargetId == target && r.Value.Drops?.Mode == MonsterDropMode.Append)
            .OrderBy(r => r.LoadIndex).ThenBy(r => r.DeclarationOrder).Select(r => r.Value.Drops!);
        internal void RemoveDropPool(string id) { foreach (var record in records.Values.Where(r => r.Value.Drops?.ReferencesPool == true && r.Value.Drops.Source == MonsterPoolSource.Managed && r.Value.Drops.PoolId == id).ToArray()) Withdraw(record); }
        internal string? ResolveProjectileTint(string target) => Find(target, "projectileTint")?.Winner?.Value.ProjectileTint;
        internal MonsterSound? ResolveHurtSound(string target) => Find(target, "sounds.hurt")?.Winner?.Value.Sounds?.Hurt;
        internal MonsterSound? ResolveDeadSound(string target) => Find(target, "sounds.dead")?.Winner?.Value.Sounds?.Dead;
        internal MonsterSound? ResolveHummingSound(string target) => Find(target, "sounds.humming")?.Winner?.Value.Sounds?.Humming;
        internal MonsterSound? ResolveRecoverySound(string target) => Find(target, "sounds.recoveryWarning")?.Winner?.Value.Sounds?.RecoveryWarning;
        private static bool IsAttackSoundField(string field) => NativeAttackValidation.IsAttackField(field) && (field.EndsWith(".fireSound", StringComparison.Ordinal) || field.EndsWith(".dashSound", StringComparison.Ordinal) || field.EndsWith(".throwSound", StringComparison.Ordinal) || field.EndsWith(".volleySound", StringComparison.Ordinal) || field.EndsWith(".readySound", StringComparison.Ordinal) || field.EndsWith(".landSound", StringComparison.Ordinal) || field.EndsWith(".countdownSound", StringComparison.Ordinal) || field.EndsWith(".explosionSound", StringComparison.Ordinal));
        private static MonsterSound? AttackSoundInput(Record? record, string field) => record?.Value.NativeAttacks
            .SelectMany(a => NativeAttackValidation.Sounds(a).Where(s => NativeAttackValidation.Field(a.AttackId, s.Key) == field).Select(s => s.Value)).FirstOrDefault();
        internal MonsterSound? ResolveAttackSound(string target, string slot, string sound) => NativeAttackValidation.Sound(
            Find(target, NativeAttackValidation.Field(slot, sound))?.Winner?.Value.NativeAttacks.FirstOrDefault(a => a.AttackId == slot), sound);
        private static string Resource(Record record, string field)
        {
            using var hash = System.Security.Cryptography.SHA256.Create();
            var skin = record.Value.Skin;
            var drops = record.Value.Drops;
            var sound = IsAttackSoundField(field) ? AttackSoundInput(record, field) : SoundValidation.Events(record.Value.Sounds).FirstOrDefault(s => "sounds." + s.Key == field).Value;
            if (sound != null) return sound.ResourcePath + ":" + sound.VolumeDb.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + (sound.Loop?.ToString() ?? "inherit");
            string text =
                field == "skin" ? skin!.TemplateFingerprint + ":" + Convert.ToBase64String(skin.Png) + ":" + string.Join(";", skin.Frames.Select(f => f.FrameId + ":" + f.X + ":" + f.Y + ":" + f.Width + ":" + f.Height)) :
                drops!.Mode + ":" + drops.Source + ":" + drops.PoolId + ":" + drops.MinCount + ":" + drops.MaxCount + ":" + string.Join(";", (drops.Entries ?? new List<MonsterDropEntry>()).Select(e => e.ItemId + ":" + e.Weight.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + e.MinCount + ":" + e.MaxCount));
            return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
        internal string? ResolveTint(string target) => Find(target, "tint")?.Winner?.Value.Tint;
        internal IEnumerable<string> TintTargets => choices.Where(t => t.Value.ContainsKey("tint")).Select(t => t.Key);
        internal double Resolve(string target, string field, double nativeValue) { var choice = Find(target, field); return choice?.Winner != null ? Value(choice.Winner, field) : nativeValue; }
        private MonsterOverrideSnapshot Snapshot(Record record)
        {
            var fields = Fields(record).Select(field =>
            {
                var choice = field == "drops" && record.Value.Drops?.Mode == MonsterDropMode.Append && record.State == MonsterOverrideState.Ready ? new Choice { Winner = record } : Find(record.Value.TargetId, field);
                var state = record.State != MonsterOverrideState.Ready ? MonsterOverrideFieldState.Inactive : choice?.Winner == record ? MonsterOverrideFieldState.Effective :
                    MonsterOverrideFieldState.Shadowed;
                if (field == "tint" || field == "projectileTint") return new MonsterOverrideFieldSnapshot(field, field == "tint" ? record.Value.Tint! : record.Value.ProjectileTint!, field == "tint" ? ResolveTint(record.Value.TargetId) : ResolveProjectileTint(record.Value.TargetId), choice?.Winner?.Id ?? "", state);
                if (IsAttackSoundField(field) || field == "skin" || field == "drops" || field.StartsWith("sounds.", StringComparison.Ordinal)) return new MonsterOverrideFieldSnapshot(field, Resource(record, field), choice?.Winner == null ? null : Resource(choice.Winner, field), choice?.Winner?.Id ?? "", state);
                double? raw = baseline(record.Value.TargetId, field);
                return new MonsterOverrideFieldSnapshot(field, Value(record, field), raw, raw.HasValue ? Resolve(record.Value.TargetId, field, raw.Value) : (double?)null,
                    choice?.Winner?.Id ?? "", state);
            });
            return new MonsterOverrideSnapshot(record.Owner, record.Id, record.Value.TargetId, record.LoadIndex, record.DeclarationOrder, record.State, generation, record.Diagnostic, fields);
        }
        internal IReadOnlyList<MonsterOverrideSnapshot> Snapshots() { thread(); return records.Values.OrderBy(r => r.Id, StringComparer.Ordinal).Select(Snapshot).ToArray(); }
        private void Withdraw(Record record)
        {
            thread(); if (record.State == MonsterOverrideState.Withdrawn) return;
            record.State = MonsterOverrideState.CleanupPending;
            Rebuild();
            if (records.TryGetValue(record.Id, out var current) && ReferenceEquals(current, record)) records.Remove(record.Id);
            record.State = MonsterOverrideState.Withdrawn; record.Diagnostic = null;
        }
        internal int RemoveOwner(string owner) { thread(); var owned = records.Values.Where(r => r.Owner == owner).ToArray(); foreach (var r in owned) Withdraw(r); return owned.Length; }
        internal int CountOwner(string owner) => records.Values.Count(r => r.Owner == owner);
        internal void Clear() { thread(); foreach (var r in records.Values.ToArray()) Withdraw(r); }
    }
}
