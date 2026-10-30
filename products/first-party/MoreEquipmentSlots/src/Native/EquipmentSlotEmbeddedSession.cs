using System;
using System.Collections.Generic;
using System.IO;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentSlotEmbeddedSession : IDisposable
    {
        internal const string AdoptionWarning = "采用此电脑的旧装备会替换当前扩展槽装备，可能与背包中已有装备重复。";
        private readonly object archive;
        private readonly long epoch;
        private readonly string target;
        private bool expectedExists;
        private object? expectedRaw;
        private EquipmentSlotLegacySource? pendingSource;
        private string? staged;
        private string? preSaveFingerprint;
        private bool disposed;
        internal EquipmentSlotLegacySource? Source { get; }
        internal List<EquipmentSlotStorageEntry>? Current { get; }
        internal List<EquipmentSlotStorageEntry>? Working { get; private set; }
        internal EquipmentSlotEmbeddedState State { get; private set; }
        internal bool Deferred { get; private set; }
        internal string Message { get; private set; } = "";
        internal string SourceProblem { get; }

        internal EquipmentSlotEmbeddedSession(object archive, long epoch, string target, bool exists, object? raw,
            EquipmentSlotLegacySource? source, string sourceError = "")
        {
            this.archive = archive; this.epoch = epoch; this.target = target;
            expectedExists = exists; expectedRaw = raw; Source = source;
            var kind = EquipmentSlotEmbeddedPayload.Read(exists, raw, out var current, out string error);
            Current = current;
            SourceProblem = sourceError + source?.Error;
            if (source != null && source.Slots == null && SourceProblem.Length == 0)
                SourceProblem = "旧来源没有可确认的完整快照。";
            if (kind == EquipmentSlotEmbeddedReadKind.Invalid || kind == EquipmentSlotEmbeddedReadKind.Unsupported ||
                (SourceProblem.Length > 0 && current == null))
            { State = EquipmentSlotEmbeddedState.Unavailable; Message = "数据已保留，暂不能采用：" + error + SourceProblem; return; }
            if (source != null && SourceProblem.Length == 0)
            { State = EquipmentSlotEmbeddedState.NeedsChoice; Message = "打开背包装备栏，选择本次使用的整组装备。"; return; }
            State = EquipmentSlotEmbeddedState.Ready;
            Working = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(current ?? EquipmentSlotStorageDocument.CreateEmptySlots());
            // Unavailable legacy input grants no choice or cleanup authority.
            // Valid native Committed (including its empty group) remains usable.
            if (SourceProblem.Length > 0)
                Message = "已使用当前存档装备。此电脑的旧装备暂不能选择或清理，原文件保留；重新读档时可重试。";
        }

        internal void ValidateContext(object currentArchive, long currentEpoch, string currentTarget, bool exists, object? raw)
        {
            EnsureActive();
            if (!ReferenceEquals(archive, currentArchive) || epoch != currentEpoch || target != currentTarget ||
                exists != expectedExists || !Equals(raw, expectedRaw))
            {
                Message = "存档或已展示内容已改变，请重新打开旧装备选择。";
                Dispose();
                throw new InvalidOperationException(Message);
            }
        }
        internal void Choose(bool local)
        {
            EnsureActive();
            if (State != EquipmentSlotEmbeddedState.NeedsChoice || Deferred) throw new InvalidOperationException("请先打开旧装备选择。");
            ValidateSource();
            if (!local && Current == null) throw new InvalidOperationException("当前档还没有内嵌装备；可采用旧装备或暂不处理。");
            Working = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(local ? Source!.Slots! : Current!);
            pendingSource = Source;
            State = EquipmentSlotEmbeddedState.Ready;
            Message = local ? "已确认采用旧装备；正常保存后再清理准确旧文件。" : "已选择当前存档装备。";
        }
        internal void Defer()
        { EnsureActive(); if (State == EquipmentSlotEmbeddedState.NeedsChoice && !Deferred) { ValidateSource(); Source?.Dispose(); Deferred = true; Message = "暂不处理；原数据保留，可从装备栏重新打开选择。"; } }

        private void ValidateSource()
        {
            if (Source != null && Source.IsUnchanged()) return;
            Message = "旧文件或来源集合已改变，本次选择失效；请重新打开选择。原数据已保留，未安排清理。";
            Dispose();
            throw new IOException(Message);
        }

        internal string Stage(IReadOnlyList<EquipmentSlotStorageEntry> latest, string fingerprint)
        {
            EnsureActive();
            if (State != EquipmentSlotEmbeddedState.Ready) throw new InvalidOperationException("No selected Working state.");
            staged = EquipmentSlotEmbeddedPayload.Encode(latest);
            preSaveFingerprint = fingerprint;
            expectedExists = true; expectedRaw = staged;
            Working = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(latest);
            return staged;
        }
        internal bool FinishSaved(string fingerprint, bool diskExists, object? diskRaw)
        {
            EnsureActive();
            if (staged == null || preSaveFingerprint == fingerprint || !diskExists || !Equals(staged, diskRaw)) return false;
            staged = null; preSaveFingerprint = null;
            RemoveSelectedSource();
            return true;
        }
        internal void FinishKeepCurrent(bool diskExists, object? diskRaw)
        {
            EnsureActive();
            if (State != EquipmentSlotEmbeddedState.Ready || Current == null || !diskExists ||
                !Equals(expectedRaw, diskRaw)) return;
            RemoveSelectedSource();
        }
        private void RemoveSelectedSource()
        {
            var source = pendingSource; pendingSource = null; // One attempt; no automatic retry queue.
            if (source == null) return;
            try
            {
                Message = source.TryRemove(out string error) ? "当前装备已落盘，已提交所选旧文件的清理；文件占用可能延迟移除。" :
                    "装备提交已保留，本次未清理旧文件；下次仍需明确选择。原因：" + error;
            }
            finally { source.Dispose(); }
        }
        private void EnsureActive() { if (disposed) throw new ObjectDisposedException(nameof(EquipmentSlotEmbeddedSession)); }
        public void Dispose() { if (disposed) return; Source?.Dispose(); pendingSource = null; disposed = true; State = EquipmentSlotEmbeddedState.Unavailable; }
    }
}
