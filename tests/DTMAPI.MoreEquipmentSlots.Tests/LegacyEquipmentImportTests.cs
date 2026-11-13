using System;
using System.IO;
using System.Runtime.Serialization.Json;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.UnitTests
{
    // 1.0.1 旧装备文件的各种状态，按 1.0.1 和 0.7.0 实际会写出的样子构造（规则见 LegacyEquipmentImport 顶部的说明）。
    // 这些用例只依赖文件格式。MES 2.0 删掉旧的事务代码以后，它们照样有效。
    internal static class LegacyEquipmentImportTests
    {
        internal static void RunAll()
        {
            SavedSlotsAreImported();
            EmptyFileImportsNothing();
            UnsavedSwapInOldMesIsIgnored();
            SavedSwapInOldMesIsImported();
            OldRuntimeStartedReturningButPlayerDidNotSave();
            OldRuntimeReturnedAndSaved();
            OldRuntimeSavedButStoppedBeforeCleanup();
            FailedPlacementGoesBackToItsSlot();
            UnsavedReplacementKeepsTheOldItem();
            SavedReplacementKeepsTheNewItem();
            UnknownVersionIsUnreadable();
            ConflictingRecordIsUnreadable();
            FileIsReadAndMarkedImported();
            RealOldRuntimeFileKeepsUnsavedEquipment();
        }

        private static void RealOldRuntimeFileKeepsUnsavedEquipment()
        {
            // 由已发布 Runtime 读取真实存档副本生成；退出前没有原生保存。
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "legacy-runtime-unsaved.json");
            LegacyImportResult result = LegacyEquipmentImport.ReadFile(path);
            Check(result.Status == LegacyImportStatus.HasItems && result.ItemCount == 2,
                "实际旧文件里待退回的两件装备都要导入。");
            Check(!result.Slots[0].IsOccupied && result.Slots[1].ItemId == "straw_hat" &&
                result.Slots[2].ItemId == "grandmas_button", "装备必须回到原来的两个槽。");
            Check(result.PlayerName == "fixture", "公开样本的角色名已匿名化。");
        }

        private static void SavedSlotsAreImported()
        {
            LegacyImportResult result = LegacyEquipmentImport.Read(Saved("hat", "", "passive"));
            Check(result.Status == LegacyImportStatus.HasItems && result.ItemCount == 2, "上次保存时的两件装备都要导入。");
            Check(result.Slots[0].ItemId == "hat" && !result.Slots[1].IsOccupied && result.Slots[2].ItemId == "passive",
                "装备要回到原来的槽位。");
            Check(result.PlayerName == "fixture", "要带上旧存档的角色名，给确认窗口用。");
        }

        private static void EmptyFileImportsNothing()
        {
            Check(LegacyEquipmentImport.Read(Saved()).Status == LegacyImportStatus.Empty, "额外槽是空的，就不弹窗。");
        }

        private static void UnsavedSwapInOldMesIsIgnored()
        {
            LegacyEquipmentDocument document = Saved("hat");
            document.GameplayCandidate = Candidate(LegacyEquipmentPhase.Prepared, "other");
            Check(LegacyEquipmentImport.Read(document).Slots[0].ItemId == "hat", "1.0.1 里没保存的换装不算。");
        }

        private static void SavedSwapInOldMesIsImported()
        {
            LegacyEquipmentDocument document = Saved("hat");
            document.GameplayCandidate = Candidate(LegacyEquipmentPhase.CommittedTombstone, "other");
            Check(LegacyEquipmentImport.Read(document).Slots[0].ItemId == "other", "1.0.1 里已经随原生存档保存的换装要算。");
        }

        // 玩家最常走的路径：更新了 MES，还没更新 Runtime。
        // 0.7.0 进档时把装备挪进待退回记录、放进背包，玩家没保存就退出了。
        private static void OldRuntimeStartedReturningButPlayerDidNotSave()
        {
            LegacyEquipmentDocument document = Saved("hat", "shield");
            document.Slots[1].IsShield = true;
            document.Slots[1].ShieldValue = 30;
            document.Slots[1].ShieldMaxValue = 50;
            document.Slots[1].ShieldDefend = 5;
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0, 1);

            LegacyImportResult result = LegacyEquipmentImport.Read(document);
            Check(result.Status == LegacyImportStatus.HasItems && result.ItemCount == 2, "没保存，待退回的装备必须照样导入。");
            Check(result.Slots[0].ItemId == "hat" && result.Slots[1].ItemId == "shield", "待退回的装备回到原来的槽位。");
            Check(result.Slots[1].IsShield && result.Slots[1].ShieldValue == 30 && result.Slots[1].ShieldMaxValue == 50 &&
                result.Slots[1].ShieldDefend == 5, "护盾数值要保留。");
        }

        private static void OldRuntimeReturnedAndSaved()
        {
            LegacyEquipmentDocument document = Saved("hat");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0);
            document.Journal = null; // 保存后 0.7.0 整理完毕：journal 清掉，slots 是空的。
            Check(LegacyEquipmentImport.Read(document).Status == LegacyImportStatus.Empty, "已经退回并保存过，装备在背包里，不再导入。");
        }

        private static void OldRuntimeSavedButStoppedBeforeCleanup()
        {
            LegacyEquipmentDocument document = Saved("hat", "passive");
            MoveToEscrow(document, LegacyEquipmentPlacement.Mail, 0, 1);
            document.Journal!.State = LegacyEquipmentPhase.CommittedTombstone;
            Check(LegacyEquipmentImport.Read(document).Status == LegacyImportStatus.Empty,
                "已经随原生存档进了背包或邮件的装备，不再导入。");
        }

        private static void FailedPlacementGoesBackToItsSlot()
        {
            LegacyEquipmentDocument document = Saved("hat", "passive");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0, 1);
            document.Journal!.Escrow[1].Placement = LegacyEquipmentPlacement.Failure;
            document.Journal.State = LegacyEquipmentPhase.CommittedTombstone;

            LegacyImportResult result = LegacyEquipmentImport.Read(document);
            Check(result.ItemCount == 1 && result.Slots[1].ItemId == "passive" && !result.Slots[0].IsOccupied,
                "没放进背包的那一件回到原来的槽位，放进去的那一件不再导入。");
        }

        // 1.0.1 换装到一半：旧装备在待退回记录里，新装备在 replacements 里，还没保存。
        private static void UnsavedReplacementKeepsTheOldItem()
        {
            LegacyEquipmentDocument document = Saved("old");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0);
            document.Journal!.Replacements.Add(new LegacyEquipmentSlot { Index = 0, ItemId = "new" });
            document.Journal.IncomingSucceeded = true;
            Check(LegacyEquipmentImport.Read(document).Slots[0].ItemId == "old", "没保存的换装，按旧装备导入。");
        }

        private static void SavedReplacementKeepsTheNewItem()
        {
            LegacyEquipmentDocument document = Saved("old");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0);
            document.Journal!.Replacements.Add(new LegacyEquipmentSlot { Index = 0, ItemId = "new" });
            document.Journal.IncomingSucceeded = true;
            document.Journal.State = LegacyEquipmentPhase.CommittedTombstone;
            document.Slots[0] = document.Journal.Replacements[0].Clone(); // 保存时，旧代码已经把新装备写进 slots。

            LegacyImportResult result = LegacyEquipmentImport.Read(document);
            Check(result.ItemCount == 1 && result.Slots[0].ItemId == "new", "已保存的换装按新装备导入，旧装备已经在背包里了。");
        }

        private static void UnknownVersionIsUnreadable()
        {
            LegacyEquipmentDocument document = Saved("hat");
            document.SchemaVersion = 99;
            Check(LegacyEquipmentImport.Read(document).Status == LegacyImportStatus.Unreadable, "不认识的版本不导入。");
            document.SchemaVersion = 4;
            Check(LegacyEquipmentImport.Read(document).Status == LegacyImportStatus.Unreadable, "没有发布过的候选格式不导入。");
        }

        private static void ConflictingRecordIsUnreadable()
        {
            LegacyEquipmentDocument document = Saved("hat");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0);
            document.Slots[0].ItemId = "other"; // 槽里和待退回记录里各有一件。
            Check(LegacyEquipmentImport.Read(document).Status == LegacyImportStatus.Unreadable, "自相矛盾的旧文件不导入，原样保留。");
        }

        private static void FileIsReadAndMarkedImported()
        {
            string root = Path.Combine(Environment.GetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT") ?? Path.GetTempPath(),
                "legacy-import-" + Guid.NewGuid().ToString("N"));
            string path = LegacyEquipmentImport.GetFilePath(root, 2);
            Check(LegacyEquipmentImport.ReadFile(path).Status == LegacyImportStatus.NoFile, "没有旧文件时什么都不做。");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            LegacyEquipmentDocument document = Saved("hat", "", "passive");
            MoveToEscrow(document, LegacyEquipmentPlacement.Backpack, 0);
            using (FileStream stream = File.Create(path))
                new DataContractJsonSerializer(typeof(LegacyEquipmentDocument)).WriteObject(stream, document);

            LegacyImportResult result = LegacyEquipmentImport.ReadFile(path);
            Check(result.Status == LegacyImportStatus.HasItems && result.ItemCount == 2, "从文件读出的结果要和直接读一致。");

            string imported = LegacyEquipmentImport.MarkImported(path);
            Check(!File.Exists(path) && File.Exists(imported) && imported == path + LegacyEquipmentImport.ImportedSuffix,
                "导入后改名，不删除。");
            Check(LegacyEquipmentImport.ReadFile(path).Status == LegacyImportStatus.NoFile, "改名以后不会再问。");

            File.WriteAllText(path, "{ 坏掉的文件");
            Check(LegacyEquipmentImport.ReadFile(path).Status == LegacyImportStatus.Unreadable, "坏文件不导入。");
            Check(File.Exists(path), "坏文件原样保留。");
            Directory.Delete(root, true);
        }

        private static LegacyEquipmentDocument Saved(params string[] itemIds)
        {
            var document = new LegacyEquipmentDocument { SchemaVersion = 3 };
            document.Scope.PlayerName = "fixture";
            for (int index = 0; index < itemIds.Length; index++)
                document.Slots[index].ItemId = itemIds[index];
            return document;
        }

        private static LegacyEquipmentCandidate Candidate(LegacyEquipmentPhase state, string firstSlotItem)
        {
            var candidate = new LegacyEquipmentCandidate { State = state };
            candidate.WorkingSlots[0].ItemId = firstSlotItem;
            return candidate;
        }

        // 和旧代码的 PrepareRecovery 一样：装备从槽里挪进 escrow，槽清空，phase 仍是 Prepared。
        private static void MoveToEscrow(LegacyEquipmentDocument document, LegacyEquipmentPlacement placement,
            params int[] slotIndexes)
        {
            var journal = new LegacyEquipmentJournal
            {
                State = LegacyEquipmentPhase.Prepared
            };
            foreach (int index in slotIndexes)
            {
                LegacyEquipmentSlot slot = document.Slots[index];
                journal.Escrow.Add(new LegacyEquipmentEscrow
                {
                    SlotIndex = index,
                    ItemId = slot.ItemId,
                    DisplayName = slot.DisplayName,
                    SkillId = slot.SkillId,
                    DefenseBonus = slot.DefenseBonus,
                    IsShield = slot.IsShield,
                    ShieldValue = slot.ShieldValue,
                    ShieldMaxValue = slot.ShieldMaxValue,
                    ShieldDefend = slot.ShieldDefend,
                    Placement = placement
                });
                document.Slots[index] = new LegacyEquipmentSlot { Index = index };
            }
            document.Journal = journal;
        }

        private static void Check(bool ok, string message)
        {
            if (!ok)
                throw new InvalidOperationException(message);
        }
    }
}

