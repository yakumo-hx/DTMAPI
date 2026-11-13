using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DTMAPI.MoreEquipmentSlots
{
    // 读 1.0.1 留下的旧装备文件，算出"上次原生保存时，三个额外槽里各有什么"。只读，不改文件，不碰游戏。
    //
    // 旧文件可能处在下面几种状态，规则都是"以上次原生保存为准"：
    // - 普通状态：slots 就是上次保存时的装备。
    // - MES 1.0.1 自己的换装还没保存（gameplayCandidate.phase = Prepared）：不算，仍以 slots 为准。
    // - MES 1.0.1 的换装已经随原生存档保存、还没整理（gameplayCandidate.phase = CommittedTombstone）：以 workingSlots 为准。
    // - 退回或替换做到一半，原生存档还没保存（journal.phase = Prepared）：
    //   escrow 里的装备仍算在原来的槽里；replacements（准备换进来的新装备）不算。
    //   0.7.0 Runtime 在 MES 没加载时把装备退回背包，也是先写成这个状态。
    //   "更新了 MES、还没更新 Runtime、进档后没保存就退出"是玩家最常走的路径，装备就在这里。
    // - 退回或替换已经随原生存档保存（journal.phase = CommittedTombstone）：
    //   已经放进背包或邮件的装备在存档里了，不算；放置失败的回到原来的槽；新装备已经写进 slots。
    // - 全部整理完：journal 和 gameplayCandidate 都为空，slots 就是结果。
    //   0.7.0 退回装备并保存之后就是这样，slots 是空的，所以不会再问玩家。
    //
    // 唯一可能多出一件的情况：原生保存刚完成，旧代码还没来得及把 phase 改成 CommittedTombstone 就崩溃了。
    // 按"宁可多一件，不能丢一件"处理，不再用存档指纹去推断。
    //
    // 1.0.1 写的是 schema 3，没有 itemState。导入时按 itemId 重新生成游戏物品，再套上护盾数值
    // 1.0.1 自己每次读档也是这样。
    internal static class LegacyEquipmentImport
    {
        internal const string FileName = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
        internal const string ImportedSuffix = ".imported";

        // configDirectory 是 Path.GetDirectoryName(helper.Config.GetConfigPath(manifest))，也就是 <游戏目录>/DTMAPI/config。
        // archiveIndex 是原生存档的 index：界面上的第 N 槽是 N-1。
        internal static string GetFilePath(string configDirectory, int archiveIndex) =>
            Path.Combine(configDirectory, "protected-items", "equipment-slots",
                "slot-" + archiveIndex.ToString(CultureInfo.InvariantCulture), FileName);

        internal static LegacyImportResult ReadFile(string path)
        {
            if (!File.Exists(path))
                return LegacyImportResult.NoFile();

            LegacyEquipmentDocument? document;
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    document = new DataContractJsonSerializer(typeof(LegacyEquipmentDocument))
                        .ReadObject(stream) as LegacyEquipmentDocument;
                }
            }
            catch (Exception ex)
            {
                // 读文件是边界：读不了就原样留着，把原因告诉玩家。
                return LegacyImportResult.Unreadable("旧装备文件读不了：" + ex.Message);
            }

            return document == null
                ? LegacyImportResult.Unreadable("旧装备文件是空的。")
                : Read(document);
        }

        // 会整理（Normalize）传入的 document。
        internal static LegacyImportResult Read(LegacyEquipmentDocument document)
        {
            if (document.SchemaVersion != 3)
            {
                return LegacyImportResult.Unreadable("不认识的旧装备文件版本：" +
                    document.SchemaVersion.ToString(CultureInfo.InvariantCulture) + "。");
            }
            document.Normalize();

            List<LegacyEquipmentSlot> slots = Copy(document.Slots);

            LegacyEquipmentCandidate? candidate = document.GameplayCandidate;
            if (candidate != null && candidate.State == LegacyEquipmentPhase.CommittedTombstone)
                slots = Copy(candidate.WorkingSlots);

            LegacyEquipmentJournal? journal = document.Journal;
            if (journal != null)
            {
                bool saved = journal.State == LegacyEquipmentPhase.CommittedTombstone;
                foreach (LegacyEquipmentEscrow? item in journal.Escrow)
                {
                    if (item == null || (saved && item.Placement != LegacyEquipmentPlacement.Failure))
                        continue;
                    string slotNumber = (item.SlotIndex + 1).ToString(CultureInfo.InvariantCulture);
                    if (item.SlotIndex < 0 || item.SlotIndex >= slots.Count)
                        return LegacyImportResult.Unreadable("旧装备文件里有不存在的槽位：第 " + slotNumber + " 槽。");
                    if (slots[item.SlotIndex].IsOccupied)
                        return LegacyImportResult.Unreadable("旧装备文件里第 " + slotNumber + " 槽同时记着两件装备。");
                    slots[item.SlotIndex] = item.ToSlot();
                }

                if (saved && journal.IncomingSucceeded)
                {
                    foreach (LegacyEquipmentSlot? replacement in journal.Replacements)
                    {
                        if (replacement == null || replacement.Index < 0 || replacement.Index >= slots.Count ||
                            HasFailedEscrow(journal, replacement.Index))
                        {
                            continue;
                        }
                        slots[replacement.Index] = replacement.Clone();
                    }
                }
            }

            int itemCount = 0;
            foreach (LegacyEquipmentSlot slot in slots)
            {
                if (slot.IsOccupied)
                    itemCount++;
            }

            string playerName = document.Scope.PlayerName;
            return itemCount == 0
                ? LegacyImportResult.Empty(playerName)
                : LegacyImportResult.Items(slots, playerName);
        }

        // 原生保存成功以后才调用：把旧文件改名为 .imported，不删除。
        internal static string MarkImported(string path)
        {
            string target = path + ImportedSuffix;
            if (File.Exists(target))
                target += "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            File.Move(path, target);
            return target;
        }

        private static List<LegacyEquipmentSlot> Copy(List<LegacyEquipmentSlot> source)
        {
            List<LegacyEquipmentSlot> slots = LegacyEquipmentDocument.CreateEmptySlots();
            foreach (LegacyEquipmentSlot? entry in source)
            {
                if (entry != null && entry.Index >= 0 && entry.Index < slots.Count)
                    slots[entry.Index] = entry.Clone();
            }
            return slots;
        }

        private static bool HasFailedEscrow(LegacyEquipmentJournal journal, int slotIndex)
        {
            foreach (LegacyEquipmentEscrow? item in journal.Escrow)
            {
                if (item != null && item.SlotIndex == slotIndex && item.Placement == LegacyEquipmentPlacement.Failure)
                    return true;
            }
            return false;
        }
    }

    internal enum LegacyImportStatus
    {
        // 没有旧文件。
        NoFile,
        // 有旧文件，但上次保存时额外槽是空的。不弹窗。
        Empty,
        // 有要导入的装备。弹窗让玩家确认。
        HasItems,
        // 读不懂。原样保留，把原因告诉玩家。
        Unreadable
    }

    internal sealed class LegacyImportResult
    {
        private LegacyImportResult(LegacyImportStatus status, List<LegacyEquipmentSlot> slots,
            string playerName, string problem)
        {
            Status = status;
            Slots = slots;
            PlayerName = playerName;
            Problem = problem;
        }

        internal LegacyImportStatus Status { get; }

        // 总是三个槽，按槽位顺序；空槽的 ItemId 为空。
        internal IReadOnlyList<LegacyEquipmentSlot> Slots { get; }

        // 旧存档的角色名，显示在确认窗口里，让玩家判断是不是自己的装备。
        internal string PlayerName { get; }

        // Unreadable 时的原因。
        internal string Problem { get; }

        internal int ItemCount
        {
            get
            {
                int count = 0;
                foreach (LegacyEquipmentSlot slot in Slots)
                {
                    if (slot.IsOccupied)
                        count++;
                }
                return count;
            }
        }

        internal static LegacyImportResult NoFile() =>
            new LegacyImportResult(LegacyImportStatus.NoFile, LegacyEquipmentDocument.CreateEmptySlots(),
                string.Empty, string.Empty);

        internal static LegacyImportResult Empty(string playerName) =>
            new LegacyImportResult(LegacyImportStatus.Empty, LegacyEquipmentDocument.CreateEmptySlots(),
                playerName, string.Empty);

        internal static LegacyImportResult Items(List<LegacyEquipmentSlot> slots, string playerName) =>
            new LegacyImportResult(LegacyImportStatus.HasItems, slots, playerName, string.Empty);

        internal static LegacyImportResult Unreadable(string problem) =>
            new LegacyImportResult(LegacyImportStatus.Unreadable, LegacyEquipmentDocument.CreateEmptySlots(),
                string.Empty, problem);
    }
}

