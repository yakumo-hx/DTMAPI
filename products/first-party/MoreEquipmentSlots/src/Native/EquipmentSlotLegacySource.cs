using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    // Core freezes identities/bytes. MES interprets scope and source authority.
    internal sealed class EquipmentSlotLegacySource : IDisposable
    {
        private readonly IProtectedSourceSnapshot snapshot;
        private readonly List<IProtectedSourceSnapshot> captured = new List<IProtectedSourceSnapshot>();
        private readonly List<string> missing = new List<string>();
        private IProtectedSourceFiles? sourceFiles;
        private Func<bool>? normalEntryStillEligible;
        private bool cleanupBlocked;
        private bool previousEligible;
        internal string Path { get; }
        internal List<EquipmentSlotStorageEntry>? Slots { get; private set; }
        internal string Error { get; private set; } = "";

        private EquipmentSlotLegacySource(IProtectedSourceSnapshot snapshot, string displayRoot,
            EquipmentSlotSaveScope? expectedScope, bool global)
        {
            this.snapshot = snapshot;
            captured.Add(snapshot);
            Path = System.IO.Path.Combine(displayRoot, snapshot.RelativeKey.Replace('/', System.IO.Path.DirectorySeparatorChar));
            try { Decode(snapshot.CopyBytes(), expectedScope, global); }
            catch (Exception ex) { Error = "旧来源读取无法核对，原文已保留：" + ex.Message; }
        }

        private void Decode(byte[] bytes, EquipmentSlotSaveScope? expectedScope, bool global)
        {
            var probe = EquipmentSlotStorageFormatClassifier.Probe(bytes);
            EquipmentSlotStorageDocument? doc;
            if (probe.Format == EquipmentSlotStorageFormat.LegacyFlat || probe.Format == EquipmentSlotStorageFormat.PreSchemaGlobal)
            {
                if (expectedScope == null) { Error = "旧 flat 来源需要当前存档身份，原文已保留。"; return; }
                EquipmentSlotSnapshotShape.ValidateLegacy(bytes, probe.Format == EquipmentSlotStorageFormat.PreSchemaGlobal);
                if (!EquipmentSlotLegacyMigration.TryConvert(bytes, expectedScope, global, out doc, out _, out string failure))
                { Error = "旧来源不可解释，原文已保留：" + failure; return; }
            }
            else if (probe.Format == EquipmentSlotStorageFormat.ProductV3)
            {
                // Unknown/missing fields and unresolved transactions confer no fallback authority.
                EquipmentSlotSnapshotShape.Validate(bytes, embedded: false);
                using (var stream = new MemoryStream(bytes, false))
                    doc = new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).ReadObject(stream) as EquipmentSlotStorageDocument;
                if (doc == null) { Error = "旧 Product 快照为空，原文已保留。"; return; }
                if ((doc.Journal != null && doc.Journal.State != EquipmentSlotJournalState.CommittedTombstone) ||
                    (doc.GameplayCandidate != null && doc.GameplayCandidate.State != EquipmentSlotGameplayCandidateState.CommittedTombstone))
                { Error = "旧来源含未决 journal/GameplayCandidate；原文及旧恢复责任保留，本产品未重放。"; return; }
                if (!EquipmentSlotDocumentStore.TryValidateRawDocument(doc, expectedScope ?? doc.Scope, out string failure))
                {
                    previousEligible = doc.Journal == null && doc.GameplayCandidate == null && IsCapturedCorruption(failure);
                    Error = "旧 Product 快照不可解释，原文已保留：" + failure;
                    return;
                }
            }
            else
            {
                // Parsing errors here refer exclusively to already captured bytes, never IO.
                previousEligible = probe.Format == EquipmentSlotStorageFormat.Invalid &&
                    (probe.Failure.StartsWith(nameof(SerializationException) + ":", StringComparison.Ordinal) ||
                     probe.Failure.StartsWith(nameof(XmlException) + ":", StringComparison.Ordinal)) && HasMalformedJson(bytes);
                Error = "旧来源格式不可解释，原文已保留：" + probe.Format + ":" + probe.Failure;
                return;
            }
            if ((doc!.Journal != null && doc.Journal.State != EquipmentSlotJournalState.CommittedTombstone) ||
                (doc.GameplayCandidate != null && doc.GameplayCandidate.State != EquipmentSlotGameplayCandidateState.CommittedTombstone))
            { Error = "旧来源含未决 journal/GameplayCandidate；原文及旧恢复责任保留，本产品未重放。"; return; }
            if (!EquipmentSlotDocumentStore.TryValidateRawDocument(doc, expectedScope ?? doc.Scope, out string committedFailure))
            { Error = "旧 Committed 组不可解释，原文已保留：" + committedFailure; return; }
            // A validated tombstone already carries Committed slots. Only that group
            // may be explicitly adopted; importing it never finalizes/replays the old
            // transaction or deletes the file which still owns its recovery record.
            cleanupBlocked = doc.Journal != null || doc.GameplayCandidate != null;
            if (!EquipmentSlotEmbeddedPayload.Validate(doc.Slots, out string error))
            { Error = "旧装备缺少或具有无效的历史实例状态；未猜造 filled/cooldown，原文已保留：" + error; return; }
            Slots = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(doc.Slots);
        }

        private static bool IsCapturedCorruption(string failure) =>
            failure == "generation-invalid" || failure == "slot-count-invalid" || failure == "slot-order-invalid" ||
            failure == "slot-index-missing" || failure == "slot-identity-duplicate" || failure == "slot-value-invalid" ||
            failure == "slot-item-missing" || failure == "shield-capacity-invalid";

        private static bool HasMalformedJson(byte[] bytes)
        {
            // Serializer conversion errors (for example an out-of-range future
            // schema number) are not evidence of damaged JSON or old authority.
            try
            {
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max))
                    XElement.Load(reader);
                return false;
            }
            catch (XmlException) { return true; }
        }

        internal static EquipmentSlotLegacySource? CaptureForSlot(IProtectedSourceFiles service, int slot, string displayRoot,
            out string error, EquipmentSlotSaveScope? expectedScope = null)
        {
            const string fileName = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
            string scopedKey = "protected-items/equipment-slots/slot-" + slot.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + fileName;
            string[] keys = { scopedKey, fileName, scopedKey + ".previous" };
            var results = new ProtectedSourceResult[3];
            EquipmentSlotLegacySource? selected = null;
            error = "";
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    results[i] = service.Capture(keys[i]);
                    if (results[i].Status != ProtectedSourceStatus.Missing &&
                        (results[i].Status != ProtectedSourceStatus.Captured || results[i].Snapshot == null))
                    { error = keys[i] + ": " + results[i].Status + ": " + results[i].Detail; return null; }
                }
                if (results[1].Snapshot != null && (results[0].Snapshot != null || results[2].Snapshot != null))
                { error = "发现多个旧来源，请先分别核对，未自动选择或清理。"; return null; }
                if (results[0].Snapshot != null)
                {
                    var primary = new EquipmentSlotLegacySource(results[0].Snapshot!, displayRoot, expectedScope, false);
                    selected = primary;
                    if (primary.Slots == null && primary.previousEligible && results[2].Snapshot != null)
                        selected = new EquipmentSlotLegacySource(results[2].Snapshot!, displayRoot, expectedScope, false);
                }
                else if (results[1].Snapshot != null)
                    selected = new EquipmentSlotLegacySource(results[1].Snapshot!, displayRoot, expectedScope, true);
                else if (results[2].Snapshot != null)
                    selected = new EquipmentSlotLegacySource(results[2].Snapshot!, displayRoot, expectedScope, false);
                if (selected == null) return null; // Every result is explicitly Missing.
                selected.sourceFiles = service;
                foreach (var result in results)
                    if (result.Snapshot != null && !selected.captured.Contains(result.Snapshot)) selected.captured.Add(result.Snapshot);
                for (int i = 0; i < keys.Length; i++) if (results[i].Status == ProtectedSourceStatus.Missing) selected.missing.Add(keys[i]);
                return selected;
            }
            finally
            {
                // Ownership of all eligibility snapshots moves to the selected group.
                if (selected == null) foreach (var result in results) result?.Snapshot?.Dispose();
            }
        }

        internal static EquipmentSlotLegacySource? Capture(IProtectedSourceFiles service, string key, string displayRoot, out string error)
        {
            var result = service.Capture(key); error = "";
            if (result.Status == ProtectedSourceStatus.Missing) return null;
            if (result.Status != ProtectedSourceStatus.Captured || result.Snapshot == null)
            { error = result.Status + ": " + result.Detail; return null; }
            return new EquipmentSlotLegacySource(result.Snapshot, displayRoot, null, key.IndexOf('/') < 0);
        }

        internal void RequireNormalEntry(Func<bool> stillEligible, bool retainRecoveryFiles = false)
        { normalEntryStillEligible = stillEligible; cleanupBlocked |= retainRecoveryFiles; }

        internal bool IsUnchanged()
        {
            try
            {
                if (normalEntryStillEligible != null && !normalEntryStillEligible()) return false;
                foreach (var source in captured) if (source.Revalidate().Status != ProtectedSourceStatus.Unchanged) return false;
                if (sourceFiles != null) foreach (string key in missing)
                {
                    var result = sourceFiles.Capture(key);
                    try { if (result.Status != ProtectedSourceStatus.Missing) return false; }
                    finally { result.Snapshot?.Dispose(); }
                }
                return true;
            }
            catch { return false; }
        }
        internal bool TryRemove(out string error)
        {
            if (Slots == null || Error.Length > 0) { error = "旧来源没有可确认的完整装备组；原文件已保留。"; return false; }
            if (cleanupBlocked) { error = "仍有旧恢复文件或事务记录；仅采用已确认装备，旧来源与恢复责任全部保留。"; return false; }
            if (!IsUnchanged()) { error = "旧来源或可选资格已改变或无法核对；原文件已保留。"; return false; }
            var result = snapshot.TryDelete();
            error = result.Status + ": " + result.Detail;
            return result.Status == ProtectedSourceStatus.DeleteCommitted;
        }
        public void Dispose() { foreach (var source in captured) source.Dispose(); }
    }
}
