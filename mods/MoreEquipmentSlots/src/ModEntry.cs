using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;

namespace DTMAPI.MoreEquipmentSlots
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private EquipmentGame game = null!;
        private EquipmentSave? save;
        private Action<string>? writePayload;
        private LegacyImportResult? legacy;
        private string? legacyPath;
        private string? adoptedPath;
        private bool savedPayload;
        private bool askToImport;
        private string notice = "";

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu") ??
                throw new InvalidOperationException("无法取得 DTMAPI 配置菜单。");
            game = new EquipmentGame(helper.Monitor, () => save?.CanWrite == true, Notify, RequestImport);
            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.SetDisplayName(helper.ModManifest, () => "更多装备槽");
            menu.AddParagraph(helper.ModManifest, () =>
                "三个额外槽只增加装备效果。装备随游戏存档保存，不保存就退出会回到上次保存的状态。");
            helper.Events.Save.SaveLoaded += OnLoaded;
            helper.Events.Save.SaveSaving += OnSaving;
            helper.Events.Save.SaveSaved += OnSaved;
            helper.Events.GameLoop.ReturnedToTitle += OnTitle;
            helper.Events.GameLoop.UpdateTicked += OnUpdate;
            game.Install();
        }

        private void OnLoaded(object sender, SaveLoadedEventArgs e)
        {
            Reset();
            // 只取游戏自己的变量容器；读写和原生保存使用同一个对象。
            var archive = DolocAPI.archiveHandle;
            IReflectionHelper reflection = helper.GetReflection();
            DialogueVariableStorage storage;
            using (var field = reflection.GetField<DialogueVariableStorage>(archive.cityData.dialogueManager, "variableStorage"))
                storage = field.GetValue();
            using (var field = reflection.GetField<IDictionary>(storage, "variables"))
            {
                IDictionary values = field.GetValue();
                bool exists = values.Contains(EquipmentPayload.Key);
                save = new EquipmentSave(exists, exists ? values[EquipmentPayload.Key] : null);
            }
            writePayload = payload => storage.SetValue(EquipmentPayload.Key, payload);
            if (save.Kind == EquipmentPayloadReadKind.Unavailable)
            {
                game.AccessPrompt = "查看装备提示";
                Notify(save.Error);
                return;
            }
            if (save.Kind == EquipmentPayloadReadKind.Ready)
            {
                LoadEquipment(save.Slots!);
                return;
            }

            int slot = e.SaveSlot ?? archive.archiveIndex;
            string configDirectory = Path.GetDirectoryName(helper.Config.GetConfigPath(helper.ModManifest))!;
            legacyPath = LegacyEquipmentImport.GetFilePath(configDirectory, slot);
            legacy = LegacyEquipmentImport.ReadFile(legacyPath);
            if (legacy.Status == LegacyImportStatus.Unreadable)
            {
                save.Block(legacy.Problem);
                game.AccessPrompt = "查看装备提示";
                Notify(legacy.Problem);
            }
            else if (legacy.Status == LegacyImportStatus.HasItems)
            {
                game.AccessPrompt = "导入旧装备";
                askToImport = true;
            }
            else
            {
                LoadEquipment(EquipmentPayload.EmptySlots());
            }
        }

        private bool LoadEquipment(IReadOnlyList<EquipmentSlotData> items)
        {
            try
            {
                game.Load(items);
                save!.Enable();
                game.RefreshView();
                return true;
            }
            catch (Exception ex)
            {
                // 恢复物品会调用原生工厂。失败时不能把原数据写成空槽。
                string message = "装备暂时无法加载，原数据已保留：" + ex.GetBaseException().Message;
                save!.Block(message);
                game.Clear();
                game.AccessPrompt = "查看装备提示";
                Notify(message);
                return false;
            }
        }

        private void OnSaving(object sender, SaveSavingEventArgs e)
        {
            savedPayload = false;
            if (save?.CanWrite != true) return;
            // 写入失败向平台抛出，平台取消这次原生保存。
            savedPayload = save.Write(game.Capture(), writePayload!);
        }

        private void OnSaved(object sender, SaveSavedEventArgs e)
        {
            if (!savedPayload || adoptedPath == null) return;
            savedPayload = false;
            try
            {
                LegacyEquipmentImport.MarkImported(adoptedPath);
                adoptedPath = null;
            }
            catch (Exception ex)
            {
                Notify("装备已随游戏保存；旧文件改名失败，仍保留在原处：" + ex.Message);
            }
        }

        private void RequestImport()
        {
            if (save == null)
            {
                Notify("请先进入一个存档。");
                return;
            }
            if (save.Kind == EquipmentPayloadReadKind.Unavailable)
                Notify(save.Error);
            else if (legacy?.Status == LegacyImportStatus.HasItems && !save.CanWrite)
                askToImport = true;
        }

        private void OnUpdate(object sender, UpdateTickedEventArgs e)
        {
            // 原生确认框自己暂停游戏并处理键鼠；等读档和其他对话结束再打开。
            if (!(DolocAPI.userInput.CurrentState is NormalGameState) &&
                !(DolocAPI.userInput.CurrentState is EquipmentBarUiState) &&
                !(DolocAPI.userInput.CurrentState is HomePageUiState)) return;
            if (notice.Length > 0)
            {
                DolocAPI.ShowConfirmBox(notice);
                notice = "";
                return;
            }
            if (!askToImport || legacy == null) return;
            string owner = legacy.PlayerName.Length > 0 ? "角色：" + legacy.PlayerName + "\n" : "";
            DolocAPI.ShowQuestionBox(owner +
                "发现旧版留下的 " + legacy.ItemCount + " 件装备，要放回三个额外槽吗？\n\n" +
                "采用后可能与背包已有装备重复。正常保存成功后，旧文件才会改名保留。" +
                "不保存就退出，下次进入仍会询问。", () => game.HandleGameCall(Adopt), null, true);
            askToImport = false;
        }

        private void Adopt()
        {
            var items = new List<EquipmentSlotData>(3);
            foreach (LegacyEquipmentSlot item in legacy!.Slots)
                items.Add(new EquipmentSlotData
                {
                    Index = item.Index, ItemId = item.ItemId,
                    IsShield = item.IsShield, ShieldValue = item.ShieldValue
                });
            if (LoadEquipment(items))
            {
                adoptedPath = legacyPath;
                legacy = null;
            }
        }

        private void Notify(string message)
        {
            helper.Monitor.Log(message, LogLevel.Warn);
            notice = message;
        }

        private void OnTitle(object sender, ReturnedToTitleEventArgs e) => Reset();

        private void Reset()
        {
            save = null;
            writePayload = null;
            legacy = null;
            legacyPath = null;
            adoptedPath = null;
            savedPayload = false;
            askToImport = false;
            notice = "";
            game.Clear();
        }

        public void Dispose()
        {
            // 平台在 Entry 失败时收回事件和配置页；这里释放 Mod 自己创建的界面和补丁。
            game?.Dispose();
        }
    }
}

