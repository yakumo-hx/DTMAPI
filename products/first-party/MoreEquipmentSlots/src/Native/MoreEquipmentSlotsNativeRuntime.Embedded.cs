using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed partial class MoreEquipmentSlotsNativeRuntime
    {
        private EquipmentSlotEmbeddedSession? embeddedSession;
        private Func<long?>? readSaveEpoch;
        private IProtectedSourceFiles? protectedSources;
        internal void SetProtectedSources(IProtectedSourceFiles service) => protectedSources = service;
        private long EmbeddedEpoch => readSaveEpoch?.Invoke() ?? throw new InvalidOperationException("No active save session epoch.");
        internal void SetArchiveContext(Func<long?> readEpoch) => readSaveEpoch = readEpoch;

        private void LoadEmbedded(int slot, bool isNewGame = false)
        {
            bool exists = EquipmentSlotEmbeddedArchive.ReadCurrent(out object? raw);
            if (isNewGame) DolocTownItemPlacementGateway.RequireNativeCurrentSaveMissing(slot);
            EquipmentSlotLegacySource? source = null;
            string error = "";
            try
            {
                var service = protectedSources ?? throw new NotSupportedException("旧装备读取需要 Runtime 0.7.5 的受保护源服务。");
                var currentScope = DolocTownItemPlacementGateway.ReadCurrentScope(slot);
                EquipmentSlotEmbeddedEntry.TrySelect(exists, false, service, slot, configRoot, out source, out error, currentScope);
            }
            catch (Exception ex) { error = ex.Message; }
            choiceStatusDismissed = false;
            archiveIndex = slot;
            scope = DolocTownItemPlacementGateway.ReadCurrentScope(slot);
            embeddedSession?.Dispose();
            embeddedSession = new EquipmentSlotEmbeddedSession(EquipmentSlotEmbeddedArchive.CurrentArchive, EmbeddedEpoch,
                EquipmentSlotEmbeddedArchive.TargetPath(slot), exists, raw, source, error);
            newGamePending = isNewGame;
            if (embeddedSession.State == EquipmentSlotEmbeddedState.Ready) InstallEmbeddedWorking();
            lastMessage = embeddedSession.Message;
            monitor.Log("MES embedded load state=" + embeddedSession.State + "; " + lastMessage +
                (embeddedSession.SourceProblem.Length > 0 ? "; legacySource=" + embeddedSession.SourceProblem : ""));
            PublishLifecycle();
        }

        private void ValidateEmbeddedContext()
        {
            if (embeddedSession == null) throw new InvalidOperationException("当前档没有待处理的旧装备选择。");
            bool exists = EquipmentSlotEmbeddedArchive.ReadCurrent(out object? raw);
            try
            {
                if (DolocTownItemPlacementGateway.ReadCurrentArchiveIndex() != archiveIndex)
                    throw new InvalidDataException("MES embedded archive index changed.");
                embeddedSession.ValidateContext(EquipmentSlotEmbeddedArchive.CurrentArchive, EmbeddedEpoch,
                    EquipmentSlotEmbeddedArchive.TargetPath(archiveIndex), exists, raw);
            }
            catch { embeddedSession.Dispose(); throw; }
        }
        private void InstallEmbeddedWorking()
        {
            ClearNativeAndUi("embedded whole-group choice");
            nativeItems.Clear(); invalidTraitSlots.Clear();
            document = EquipmentSlotDocumentStore.CreateEmpty(scope!);
            workingSlots = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(embeddedSession!.Working!);
            document.Slots = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(workingSlots);
            workingDirty = false;
            RehydrateWorkingTraits("embedded whole-group choice");
            if (!deactivated) ApplyStoredFunctions("embedded whole-group choice");
        }

        internal string EmbeddedChoiceSummary
        {
            get
            {
                if (embeddedSession == null) return "当前档无需旧装备选择。";
                var text = new StringBuilder(embeddedSession.Message);
                text.Append("\n当前载入存档中的装备：").Append(DescribeGroup(embeddedSession.Current));
                text.Append("\n此电脑旧版装备：").Append(DescribeGroup(embeddedSession.Source?.Slots));
                if (embeddedSession.Source != null) text.Append("\n来源：").Append(embeddedSession.Source.Path);
                return text.ToString();
            }
        }
        private static string DescribeGroup(IReadOnlyList<EquipmentSlotStorageEntry>? slots)
        {
            if (slots == null) return "无可用快照";
            var text = new StringBuilder();
            foreach (var slot in slots)
            {
                if (text.Length > 0) text.Append("；");
                text.Append(slot.Index + 1).Append(": ").Append(slot.IsOccupied ? slot.DisplayName : "空");
                if (slot.IsShield) text.Append(" 盾 ").Append(slot.ShieldValue).Append('/').Append(slot.ShieldMaxValue);
                if (slot.ItemState?.Filled is bool filled) text.Append(filled ? " 药包已填充" : " 药包未填充");
                if (slot.ItemState?.Cooldown is int cooldown) text.Append(" 冷却 ").Append(cooldown);
            }
            return text.ToString();
        }
        internal void ReopenEmbeddedChoice()
        {
            if (embeddedSession == null || embeddedSession.State == EquipmentSlotEmbeddedState.Ready) return;
            int slot = archiveIndex;
            bool isNewGame = newGamePending;
            ResetTransientRoots("reopen embedded choice"); ClearLoadedSaveState();
            LoadEmbedded(slot, isNewGame);
        }
        internal void ChooseEmbedded(bool local)
        {
            ValidateEmbeddedContext();
            embeddedSession!.Choose(local);
            InstallEmbeddedWorking();
            monitor.Log("MES explicit choice=" + (local ? "local" : "current") + "; warning=" + EquipmentSlotEmbeddedSession.AdoptionWarning);
            if (!local)
            {
                string path = EquipmentSlotEmbeddedArchive.TargetPath(archiveIndex);
                bool exists = EquipmentSlotEmbeddedArchive.ReadDisk(path, out object? raw);
                embeddedSession.FinishKeepCurrent(exists, raw);
            }
            lastMessage = embeddedSession.Message;
            choiceStatusDismissed = false;
            PublishLifecycle();
        }
        internal void DeferEmbeddedChoice()
        {
            ValidateEmbeddedContext(); embeddedSession!.Defer();
            lastMessage = embeddedSession.Message;
            monitor.Log("MES explicit choice=defer; sources retained.");
        }

        private void StageEmbeddedWorking()
        {
            ValidateEmbeddedContext();
            if (embeddedSession!.State != EquipmentSlotEmbeddedState.Ready) return;
            if (newGamePending) RefreshNewGameScopeBeforeFirstSave();
            ReconcilePendingGameplayPlacement("embedded SaveSaving");
            ReconcilePendingGameplayIncomingWithdrawal("embedded SaveSaving");
            CaptureWorkingItems();
            string preimage = CapturePreSaveFingerprint();
            string payload = embeddedSession.Stage(RequireWorkingSlots(), preimage);
            EquipmentSlotEmbeddedArchive.Stage(payload);
        }
        private void CompleteEmbeddedSave()
        {
            ValidateEmbeddedContext();
            bool exists = EquipmentSlotEmbeddedArchive.ReadDisk(EquipmentSlotEmbeddedArchive.TargetPath(archiveIndex), out object? raw);
            if (embeddedSession!.FinishSaved(DolocTownItemPlacementGateway.GetNativeSaveFingerprint(archiveIndex), exists, raw))
            {
                document!.Slots = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(RequireWorkingSlots());
                workingDirty = false; stateUpgradePending = false;
                newGamePending = false;
                lastMessage = embeddedSession.Message;
                choiceStatusDismissed = false;
                if (uiSession != null) RefreshEmbeddedChoiceUi(uiSession);
                monitor.Log("MES embedded native file verified; " + lastMessage);
            }
        }
    }
}
