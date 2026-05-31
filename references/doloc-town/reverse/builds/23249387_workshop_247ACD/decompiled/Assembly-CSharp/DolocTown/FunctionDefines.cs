using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Building;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Equipment;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.Config.Mission;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Plant;
using DolocTown.Config.Player;
using DolocTown.Config.Resource;
using DolocTown.Config.Room;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class FunctionDefines
{
	private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.Auto,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
	};

	[Command("count_item_in_backpack", Desc = "检查背包里指定道具数量")]
	private static int CountItemInBackpack(string itemName)
	{
		return DolocAPI.CountItem(itemName, checkBox: true);
	}

	[Command("try_place_in_backpack", Desc = "尝试放置指定数量道具到玩家背包，不能放下则返回false，(可选)溢出以邮件发送")]
	private static bool TryPlaceInBackpack(string itemName, int count = 1, bool sendEmailOnOverflow = false)
	{
		return DolocAPI.TryPlaceInBackpack(itemName, count, sendEmailOnOverflow);
	}

	[Command("place_in_backpack", Desc = "放置指定数量道具到玩家背包，溢出以邮件发送")]
	private static void PlaceInBackpack(string itemName, int count)
	{
		if (!DolocAPI.TryPlaceInBackpack(itemName, count, sendEmailOnOverflow: true) && DolocAPI.QueryItemProto(itemName, out var proto))
		{
			DolocAPI.ShowMessageBoxNode(proto.UiSpriteAsset.Asset, DolocUtils.Format(DolocConfig.StaticTexts.ItemSendEmailOnOverflow, (proto.Title ?? "").Colored(DolocUiColor.EYECATCHCOLOR_CYAN)));
		}
	}

	[Command("can_place_in_backpack", Desc = "检查背包是否能放下指定数量的道具")]
	private static bool CanPlaceInBackpack(string itemName, int count = 1)
	{
		return DolocAPI.CanPlaceItem(itemName, count);
	}

	[Command("get_backpack_empty_cell_count", Desc = "查询背包空位数")]
	private static int GetBackpackEmptyCellCount()
	{
		return DolocAPI.archiveHandle.InventorySystem.inventory.emptyCount;
	}

	[Command("backup_and_clear_backpack_except_type", Desc = "备份并清除当前背包指定类型外的道具")]
	private static void BackupBackpack(string itemType)
	{
		DolocAPI.archiveHandle.farmData.agentData.BackupEquipment();
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		DolocAPI.archiveHandle.farmData.agentData.BackupBackpack();
		for (int i = 0; i < inventory.capacity; i++)
		{
			inventory.ClearSlotLockStatus(i);
			Item item = inventory.Read(i);
			if (item != null && item.type.Id != itemType)
			{
				inventory.Take(i);
			}
		}
		inventory.Sort();
		DolocAPI.ResetQuickInventorySelection();
	}

	[Command("revert_backpack", Desc = "恢复备份的背包")]
	private static void RevertBackpack()
	{
		DolocAPI.archiveHandle.farmData.agentData.RevertEquipment();
		DolocAPI.archiveHandle.farmData.agentData.RevertBackpack();
	}

	[Command("get_current_backpack_level", Desc = "查询当前背包等级")]
	private static int GetBackpackLevel()
	{
		return DolocAPI.archiveHandle.backpackLevel;
	}

	[Command("can_backpack_upgrade", Desc = "查询背包是否可以继续升级")]
	private static bool CanBackpackUpgrade()
	{
		return DolocAPI.archiveHandle.CanBackpackUpgrade();
	}

	[Command("open_backpack_upgrade_panel", Desc = "购买背包容量")]
	private static UniTask OpenBackpackUpgradePanel()
	{
		DolocAPI.EnterUI((BackpackUpgradeUiState state) => state.HandleStartUpArgs());
		return DolocAPI.WaitWhileInUiSateTask();
	}

	[Command("cost_item", Desc = "消耗玩家身上指定道具")]
	private static void CostItem(string itemName, int count, bool useEffect = true)
	{
		DolocAPI.CostItem(itemName, count, checkBox: true);
		if (useEffect && DolocAPI.QueryItemProto(itemName, out var proto))
		{
			DolocAPI.RaiseSpriteFadeUp(DolocAPI.agent.PositionCenter, proto.UiSpriteAsset.Asset);
		}
	}

	[Command("clear_backpack", Desc = "删除玩家身上所有道具")]
	private static void ClearBackpack()
	{
		DolocAPI.archiveHandle.InventorySystem.inventory.Clear();
	}

	[Command("is_npc_at_scene", Desc = "查询npc是否在指定场景(不指定场景则检查是否在当前场景)")]
	private static bool IsNpcAtScene(string npcName, string sceneName = null)
	{
		if (sceneName == null)
		{
			sceneName = DolocAPI.archiveHandle.currentRoom.SceneRawName;
		}
		return DolocAPI.IsNpcAtScene(npcName, sceneName);
	}

	[Command("is_npc_at_current_scene", Desc = "查询npc是否在当前场景")]
	private static bool IsNpcAtCurrentScene(string npcName)
	{
		return DolocAPI.IsNpcAtScene(npcName, DolocAPI.archiveHandle.currentRoom.SceneRawName);
	}

	[Command("get_current_scene", Desc = "获取当前场景名")]
	private static string GetCurrentScene()
	{
		return DolocAPI.archiveHandle.currentRoom.SceneRawName;
	}

	[Command("is_malignant_weather_now", Desc = "查询现在是否是恶性天气")]
	private static bool IsMalignantWeatherNow()
	{
		return DolocAPI.archiveHandle.WeatherSystem.IsInMalignantWeather;
	}

	[Command("get_current_weather", Desc = "获取当前天气类型")]
	private static string GetCurrentWeather()
	{
		return DolocAPI.archiveHandle.CurrentWeatherType.ToString().ToLower();
	}

	[Command("get_total_days", Desc = "获取当前度过的总天数")]
	private static int GetTotalDays()
	{
		return DolocAPI.archiveHandle.timeData.TotalDays;
	}

	[Command("get_current_week_day", Desc = "获取今天的星期数")]
	private static int GetCurrentWeekDay()
	{
		return (int)DolocAPI.archiveHandle.CurrentWeekDay;
	}

	[Command("get_current_year", Desc = "获取当前年份")]
	private static int GetCurrentYear()
	{
		return DolocAPI.archiveHandle.DateNow.Year;
	}

	[Command("get_current_month", Desc = "获取当前月份")]
	private static int GetCurrentMonth()
	{
		return DolocAPI.archiveHandle.DateNow.Month;
	}

	[Command("get_current_date", Desc = "获取当月日期")]
	private static int GetCurrentDate()
	{
		return DolocAPI.archiveHandle.DateNow.Day;
	}

	[Command("get_current_hour", Desc = "获取当前小时数")]
	private static int GetCurrentHour()
	{
		return DolocAPI.archiveHandle.DateNow.Hour;
	}

	[Command("get_current_npc_liking_lv", Desc = "获取当前npc的好感度等级")]
	private static int GetCurrentNpcLikingLv(string npcName)
	{
		return DolocAPI.QueryNpcLikingLv(npcName);
	}

	[Command("add_target_npc_liking_value", Desc = "增加目标npc的好感值")]
	private static void AddTargetNpcLikingValue(string npcName, float value)
	{
		DolocAPI.AddTargetNpcLikingValue(npcName, value);
	}

	[Command("query_gift_level", Desc = "查询npc该礼物的喜爱等级")]
	private static int QueryGiftLevel(string npcName, string itemName)
	{
		return DolocAPI.QueryGiftLevel(npcName, itemName);
	}

	[Command("gift_item_to_npc", Desc = "将道具赠送给目标Npc")]
	private static void GiftItemToNpc(string npcName, string itemName)
	{
		DolocAPI.archiveHandle.cityData.likingManager.ReceiveGift(npcName, itemName, DolocAPI.IsNpcBirthday(npcName, DolocAPI.archiveHandle.DateNow));
	}

	[Command("is_gift_limit", Desc = "查询是否到达本周赠礼上限")]
	private static bool IsGiftLimit(string npcName)
	{
		if (DolocAPI.Command_IsNpcBirthday(npcName))
		{
			return false;
		}
		DolocAPI.QueryNpcLikingInfo(npcName, out var liking);
		if (liking.extraGiftCount > 0)
		{
			return false;
		}
		return liking.giftCount >= DolocAPI.GlobalParameter.WeeklyGiftLimit;
	}

	[Command("add_extra_gift_count_to_all_npcs", Desc = "为所有NPC添加额外赠礼次数")]
	private static void AddExtraGiftCountToAllNpcs(int count = 1)
	{
		DolocAPI.archiveHandle.cityData.likingManager.AddExtraGiftCountToAllNpcs(count);
	}

	[Command("clear_extra_gift_count", Desc = "清除额外赠礼次数")]
	private static void ClearExtraGiftCount()
	{
		DolocAPI.archiveHandle.cityData.likingManager.ClearExtraGiftCount();
	}

	[Command("add_liking_by_greeting", Desc = "每天第一次打招呼会增加好感度")]
	private static void AddLikingByGreeting(string npcName)
	{
		DolocAPI.archiveHandle.cityData.likingManager.AddLikingByGreeting(npcName);
	}

	[Command("add_liking_by_hold_birthday_party", Desc = "举办生日派对增加的好感度")]
	private static void AddLikingByHoldBirthdayParty(string npcName)
	{
		DolocAPI.AddTargetNpcLikingValue(npcName, DolocAPI.GlobalParameter.LikingBirthdayParty);
	}

	[Command("add_liking_by_organize_activity", Desc = "和npc一起做活动增加好感度")]
	private static void AddLikingByOrganizeActivity(string npcName)
	{
		DolocAPI.QueryNpcLikingInfo(npcName, out var liking);
		if (!liking.isParticipateActivity)
		{
			liking.isParticipateActivity = true;
			DolocAPI.AddTargetNpcLikingValue(npcName, DolocAPI.GlobalParameter.LikingActivity);
		}
	}

	[Command("dialogue_target_drop_item", Desc = "在对话对象周围生成掉落物")]
	private static void DialogueTargetDropItem(string entityName, string itemName, int count = 1)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		string title = DolocAPI.archiveHandle.currentRoom.Title;
		DolocAPI.ThrowItemWithCount(new CountItem(itemName, count), dialogueTargetViewOrDefault.WorldPosition, 0f, title);
	}

	[Command("dialogue_toward_target_drop_item", Desc = "使对话对象朝目标对象方向扔出一个掉落物")]
	private static void DialogueTowardTargetDropItem(string entityName, string targetEntityName, string itemName, float distance, int count = 1)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		float offset = ((DolocAPI.GetDialogueTargetViewOrDefault(targetEntityName).WorldPosition.x < dialogueTargetViewOrDefault.WorldPosition.x) ? (0f - distance) : distance);
		DolocAPI.ThrowItemWithCount(new CountItem(itemName, count), dialogueTargetViewOrDefault.WorldPosition, offset, DolocAPI.archiveHandle.currentRoom.Title);
	}

	[Command("generate_drop_item", Desc = "在玩家周围生成一个掉落物 (但避免被立即拾取)")]
	private static void GenerateDropItemAroundPlayer(string itemName, float offset = 5f, int count = 1)
	{
		string title = DolocAPI.archiveHandle.currentRoom.Title;
		offset = Mathf.Abs(offset) * (float)(DolocAPI.AgentFaceRight ? 1 : (-1));
		DolocAPI.ThrowItemWithCount(new CountItem(itemName, count), DolocAPI.AgentPosition, offset, title, shouldSendMsg: true);
	}

	[Command("get_current_interactable_id", Desc = "获取当前可交互物的id(房间id.交互物guid)")]
	private static string GetCurrentInteractableObjectId()
	{
		InteractableObject currentInteractableObject = DolocAPI.CurrentInteractableObject;
		if (currentInteractableObject == null || currentInteractableObject.roomId.IsNullOrEmpty() || currentInteractableObject.guid.IsNullOrEmpty())
		{
			return string.Empty;
		}
		return currentInteractableObject.roomId + "." + currentInteractableObject.guid;
	}

	[Command("roll_drop_lib", Desc = "随机获取指定掉落库中的一个道具")]
	private static string GetItemFromSpawnLut(string libId)
	{
		ItemSpawnInfo orDefault = DolocConfig.Tables.TbItemSpawn.GetOrDefault(libId);
		if (orDefault == null)
		{
			return "";
		}
		CountItem[] array = orDefault.SpawnItems(1);
		if (array.IsNullOrEmpty())
		{
			return "";
		}
		return array[0].itemName;
	}

	[Command("interactable_roll_drop_lib", Desc = "在当前接触的交互物周围根据掉落库生成掉落物")]
	private static void GenerateDropItemAroundInteractableObject(string libId, int minCount, int maxCount)
	{
		if (DolocAPI.CurrentInteractableObject == null)
		{
			return;
		}
		ItemSpawnInfo orDefault = DolocConfig.Tables.TbItemSpawn.GetOrDefault(libId);
		if (orDefault == null)
		{
			return;
		}
		CountItem[] array = orDefault.SpawnItems(minCount, maxCount);
		for (int i = 0; i < array.Length; i++)
		{
			CountItem countItem = array[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				DolocAPI.ThrowItem(countItem.itemName, DolocAPI.CurrentInteractableObject.position, 0f, null, shouldSendMsg: true);
			}
		}
	}

	[Command("npc_roll_drop_lib", Desc = "在指定npc周围根据掉落库生成掉落物")]
	private static void GenerateDropItemAroundTarget(string npc, string libId)
	{
		string itemFromSpawnLut = GetItemFromSpawnLut(libId);
		if (!itemFromSpawnLut.IsNullOrEmpty())
		{
			IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(npc);
			DolocAPI.ThrowItem(itemFromSpawnLut, dialogueTargetViewOrDefault.WorldPosition, 0f, null, shouldSendMsg: true);
		}
	}

	[Command("debug_generate_random_drop_items", Desc = "随机生成一个掉落物(debug)")]
	private static void GenerateRandomDropItems(int count = 1, float offset = 5f)
	{
		System.Random random = new System.Random();
		string[] array = DolocConfig.Tables.TbItem.DataList.Select((ItemInfo x) => x.Id).ToArray();
		for (int i = 0; i < count; i++)
		{
			GenerateDropItemAroundPlayer(array[random.Next(array.Length)], offset + (float)i);
		}
	}

	[Command("debug_generate_steel_tools", Desc = "生成所有钢工具")]
	private static void GenerateSteelTools()
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		DolocAPI.PlaceItem("steel_pickaxe");
		DolocAPI.PlaceItem("steel_axe");
		DolocAPI.PlaceItem("steel_sickle");
	}

	[Command("debug_generate_all_hats", Desc = "生成所有帽子")]
	private static void GenerateAllHats()
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		foreach (HatInfo data in DolocConfig.Tables.TbHat.DataList)
		{
			DolocAPI.PlaceItem(data.Id);
		}
	}

	[Command("debug_generate_all_capsules", Desc = "生成所有胶囊")]
	private static void GenerateAllCapsules(int count = 1)
	{
		foreach (CropGeneInfo data in DolocConfig.Tables.TbCropGene.DataList)
		{
			DolocAPI.PlaceItem(data.CapsuleItem, count);
		}
	}

	[Command("debug_generate_all_gene_items", Desc = "生成基因系统相关所有道具")]
	private static void GenerateAllGeneItems()
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		DolocAPI.PlaceItem("gene_incubator", 10);
		DolocAPI.PlaceItem("gene_extractor", 10);
		DolocAPI.PlaceItem("gene_replicator", 10);
		DolocAPI.PlaceItem("gene_synthesizer", 10);
		DolocAPI.PlaceItem("seed_compacting_machine", 10);
		DolocAPI.PlaceItem("seed_endyam", 999);
		DolocAPI.PlaceItem("seed_thunder_grass", 999);
		DolocAPI.PlaceItem("seed_chinese_cabbage", 999);
		DolocAPI.PlaceItem("endyam", 999);
		GenerateAllCapsules(999);
	}

	[Command("debug_generate_all_seeds", Desc = "生成所有种子道具(及支架和种植盆)")]
	private static void GenerateAllSeeds()
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		DolocAPI.PlaceItem("pt_bamboo", 999);
		DolocAPI.PlaceItem("plantbasin_foam", 999);
		DolocAPI.PlaceItem("plantbasin_shrub", 999);
		DolocAPI.PlaceItem("plantbasin_vine", 999);
		DolocAPI.PlaceItem("plantbasin_fungus", 999);
		foreach (ItemInfo data in DolocConfig.Tables.TbItem.DataList)
		{
			if (data.Function is ItemFunctionSeed)
			{
				DolocAPI.PlaceItem(data.Id, 999);
			}
		}
	}

	[Command("add_gene_to_selected_item", Desc = "给选中的道具添加基因(基因数量不会超出数量上限)")]
	private static void AddGeneToSelectedItem(string geneId)
	{
		if (DolocAPI.SelectedItem is IHasGeneGroup hasGeneGroup)
		{
			hasGeneGroup.AddGene(geneId);
			DolocAPI.RefreshQuickInventory();
		}
	}

	[Command("add_gene_to_all_items", Desc = "给背包中的所有道具添加基因(基因数量不会超出数量上限)")]
	private static void AddGeneToAllItems(string geneId)
	{
		Item[] array = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is IHasGeneGroup hasGeneGroup)
			{
				hasGeneGroup.AddGene(geneId);
			}
		}
		DolocAPI.RefreshQuickInventory();
	}

	[Command("debug_generate_all_buildings", Desc = "生成所有建筑图纸")]
	private static void GenerateAllBuildings(int count = 1)
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		foreach (BuildingInfo data in DolocConfig.Tables.TbBuilding.DataList)
		{
			for (int i = 0; i < count; i++)
			{
				DolocAPI.PlaceItem(data.Id);
			}
		}
	}

	[Command("debug_generate_all_lights", Desc = "生成所有灯具")]
	private static void GenerateAllLights(int count = 1)
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		foreach (EquipmentInfo data in DolocConfig.Tables.TbEquipment.DataList)
		{
			if (data.Function is EquipmentFuncLamp)
			{
				for (int i = 0; i < count; i++)
				{
					DolocAPI.PlaceItem(data.Id);
				}
			}
		}
	}

	[Command("debug_generate_faction_items", Desc = "生成势力任务所需的道具")]
	private static void GenerateFactionItems(string type = "")
	{
		DolocAPI.Command_SetBackpackCapacity(40);
		foreach (FactionMissionInfo data in DolocConfig.Tables.TbFactionMission.DataList)
		{
			if ((!type.IsNullOrEmpty() && data.MainSeries.ToString().ToLower() != type) || DolocAPI.IsFactionMissionComplete(data.Id))
			{
				continue;
			}
			foreach (CountItem requiredItem in data.RequiredItems)
			{
				DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, DolocAPI.GenerateItem(requiredItem.itemName, requiredItem.itemCount), DolocAPI.AgentPosition);
			}
		}
	}

	[Command("query_gate_is_open", Desc = "获取目标传送门是否开启")]
	private static bool QueryGateIsOpen(string portalId)
	{
		PortalInfo orDefault = DolocConfig.Tables.TbPortal.GetOrDefault(portalId);
		if (orDefault.TargetId_Ref == null)
		{
			return false;
		}
		if (!orDefault.UseTimeRange)
		{
			return true;
		}
		int hour = DolocAPI.archiveHandle.DateNow.Hour;
		return orDefault.TimeRange.InRange(hour);
	}

	[Command("get_npc_scene_name", Desc = "获取npc当前所在场景名")]
	private static string GetNpcAtSceneName(string npcName)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			return npc.sceneName;
		}
		return string.Empty;
	}

	[Command("get_npc_x", Desc = "获取npc当前位置的x坐标")]
	private static float GetNpcX(string npcName)
	{
		if (npcName == "player")
		{
			return DolocAPI.AgentPosition.x;
		}
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			return npc.positionWS.x;
		}
		return 0f;
	}

	[Command("get_npc_y", Desc = "获取npc当前位置的y坐标")]
	private static float GetNpY(string npcName)
	{
		if (npcName == "player")
		{
			return DolocAPI.AgentPosition.y;
		}
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			return npc.positionWS.y;
		}
		return 0f;
	}

	[Command("set_disable_dispose_item_state", Desc = "是否禁止丢弃道具")]
	private static void SetDisposeItemState(bool value)
	{
		DolocAPI.archiveHandle.farmData.agentData.disableDisposeItem = value;
	}

	[Command("unlock_all_collection", Desc = "解锁全图鉴(不包括档案)")]
	private static void UnlockCompleteIllustratedCollection()
	{
		DolocConfig.Tables.TbItem.DataList.ForEach(delegate(ItemInfo item)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Item, item.Id, popTip: false);
		});
		DolocConfig.Tables.TbMonsterDocument.DataList.ForEach(delegate(MonsterDocumentInfo monster)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Monster, monster.Id, popTip: false);
		});
		DolocConfig.Tables.TbNpcDocument.DataList.ForEach(delegate(NpcDocumentInfo npc)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Npc, npc.Id, popTip: false);
		});
		DolocConfig.Tables.TbAnimalDocument.DataList.ForEach(delegate(AnimalDocumentInfo animal)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Animal, animal.Id, popTip: false);
		});
		DolocConfig.Tables.TbResourceDocument.DataList.ForEach(delegate(ResourceDocumentInfo resource)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Resource, resource.Id, popTip: false);
		});
		DolocConfig.Tables.TbFarmFish.DataList.ForEach(delegate(FarmFishInfo fish)
		{
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Fish, fish.Id, popTip: false);
		});
	}

	[Command("unlock_a_npc_document", Desc = "解锁一条npc的图鉴档案信息")]
	private static void UnlockNpcPartDocument(string npcName, string docName)
	{
		DolocAPI.archiveHandle.farmData.collectionManager.UnlockNpcPartDocument(npcName, docName);
	}

	[Command("unlock_a_monster_document", Desc = "解锁一条怪物的图鉴档案信息")]
	private static void UnlockMonsterPartDocument(string name, string docName)
	{
		DolocAPI.archiveHandle.farmData.collectionManager.UnlockMonsterPartDocument(name, docName);
	}

	[Command("get_reputation_value", Desc = "获取该势力的声望值")]
	private static int GetReputationValue(string factionName)
	{
		return DolocAPI.archiveHandle.cityData.treatyPortFactionManager.GetReputationValue(factionName);
	}

	[Command("view_reputation_values", Desc = "查看所有势力的声望值")]
	private static void ViewReputationValues()
	{
		foreach (TreatyPortFactionInfo data in DolocConfig.Tables.TbTreatyPortFaction.DataList)
		{
			string id = data.Id;
			DolocAPI.output($"{id}: {GetReputationValue(id)}");
		}
	}

	[Command("add_reputation_value", Desc = "增加该势力的声望值")]
	private static void AddReputationValue(string factionName, int value)
	{
		DolocAPI.archiveHandle.cityData.treatyPortFactionManager.AddReputationValue(factionName, value);
	}

	[Command("settled_treaty_port", Desc = "邀请入驻")]
	private static bool SettledTreatyPort(string factionName)
	{
		return DolocAPI.archiveHandle.cityData.treatyPortFactionManager.SettledTreatyPort(factionName);
	}

	[Command("check_faction_invite_cd", Desc = "检查该势力是否在邀请cd中")]
	private static bool CheckFactionInviteCd(string factionName)
	{
		DolocAPI.QueryTreatyPortFaction(factionName, out var faction);
		return faction.DisableContact;
	}

	[Command("faction_contact_closed", Desc = "暂时关闭该势力的无线电联络方式")]
	private static void FactionContactClosed(string factionName)
	{
		DolocAPI.QueryTreatyPortFaction(factionName, out var faction);
		faction.DisableContact = true;
	}

	[Command("treaty_port_completed", Desc = "商埠维修完成")]
	private static void TreatyPortCompleted()
	{
		DolocAPI.archiveHandle.cityData.treatyPortFactionManager.TreatyPortCompleted();
	}

	[Command("get_random_trash_talk", Desc = "获取一条随机垃圾文学")]
	private static string GetRandomTrashTalk()
	{
		return DolocConfig.Tables.TbTrashTalk.GetRandomTrashTalk();
	}

	[Command("get_random_trash_talk_in_group", Desc = "从指定的组里获取一条随机垃圾文学")]
	private static string GetRandomTrashTalkInGroup(string group)
	{
		return DolocConfig.Tables.TbTrashTalk.GetRandomTrashTalk(group);
	}

	[Command("refresh_current_city_room_render", Desc = "重新渲染当前城镇房间")]
	private static void RefreshCurrentCityRoomRender()
	{
		if (DolocAPI.CurrentRoom.Type == RoomType.City)
		{
			DolocAPI.CurrentRoom.RefreshRender();
		}
	}

	[Command("get_health_percent", Desc = "获取玩家血量百分比")]
	private static float GetCurrentHealthPercent()
	{
		return DolocAPI.archiveHandle.CurrentHealthPercent;
	}

	[Command("get_energy_percent", Desc = "获取玩家体力值百分比")]
	private static float GetCurrentEnergyPercent()
	{
		return DolocAPI.archiveHandle.CurrentEnergyPercent;
	}

	[Command("get_spirit_percent", Desc = "获取玩家精力值百分比")]
	private static float GetCurrentSpiritPercent()
	{
		return DolocAPI.archiveHandle.CurrentSpiritPercent;
	}

	[Command("get_current_money", Desc = "获取玩家当前的金币数")]
	private static int GetCurrentMoney()
	{
		return DolocAPI.archiveHandle.CurrentMoney;
	}

	[Command("get_game_event_record_count", Desc = "查询游戏开始至今完成指定游戏事件的次数，不指定参数则返回总次数")]
	private static int GetGameEventRecordCount(string gameEvent, string arg = null)
	{
		if (!Enum.TryParse<GameEventType>(gameEvent, ignoreCase: true, out var result))
		{
			return 0;
		}
		return DolocAPI.GetEventTriggerCount(result, arg);
	}

	[Command("complete_event_decorator", Desc = "完成指定的事件装饰器")]
	private static void CompleteEventDecorator(string decoratorId)
	{
		DolocAPI.AddEventDecorator(decoratorId);
	}

	[Command("remove_event_decorator", Desc = "移除指定的事件装饰器")]
	private static void RemoveEventDecorator(string decoratorId)
	{
		DolocAPI.RemoveEventDecorator(decoratorId);
	}

	[Command("is_event_decorator_complete", Desc = "指定一个事件装饰器Id并检查该装饰器是否已经完成")]
	private static bool IsEventDecoratorComplete(string decorator)
	{
		return DolocAPI.IsEventDecoratorComplete(decorator);
	}

	[Command("is_mission_complete", Desc = "检查指定任务是否完成")]
	private static bool IsMissionComplete(string missionId)
	{
		if (string.IsNullOrEmpty(missionId))
		{
			return false;
		}
		return DolocAPI.IsMissionComplete(missionId);
	}

	[Command("is_mission_in_process", Desc = "检查指定任务或节点是否正在进行中")]
	private static bool IsMissionInProcess(string missionId)
	{
		if (string.IsNullOrEmpty(missionId))
		{
			return false;
		}
		return DolocAPI.IsMissionInProcess(missionId);
	}

	[Command("is_tech_node_unlocked", Desc = "检查指定科技节点是否解锁")]
	private static bool IsTechNodeUnlocked(string techNodeName)
	{
		return DolocAPI.archiveHandle.GetTechNodeUnlockState(techNodeName);
	}

	[Command("is_first_meet_npc", Desc = "是否第一次遇见目标npc")]
	private static bool IsFirstMeetNpc(string npcName)
	{
		return !DolocAPI.IsVisitedNpcName(npcName);
	}

	[Command("archive_backpack_contains_plant", Desc = "查询背包内是否包含可向档案馆提交的植物")]
	private static bool CheckBackpackHasPlantForArchive()
	{
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		for (int i = 0; i < inventory.capacity; i++)
		{
			Item item = inventory.Read(i);
			if (documentManager.plantDocMgr.CanSubmitItemAsPlant(item))
			{
				return true;
			}
		}
		return false;
	}

	[Command("archive_backpack_contains_chip", Desc = "查询背包内是否包含可向档案馆提交的芯片")]
	private static bool CheckBackpackHasChipForArchive()
	{
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		for (int i = 0; i < inventory.capacity; i++)
		{
			Item item = inventory.Read(i);
			if (documentManager.chipDocMgr.CanSubmitItemAsChip(item))
			{
				return true;
			}
		}
		return false;
	}

	[Command("archive_all_plant_docs_unlocked", Desc = "解锁了档案馆内全部的植物档案")]
	private static bool AllPlantDocUnlocked()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.allPlantDocUnlocked;
	}

	[Command("archive_all_chip_docs_unlocked", Desc = "解锁了档案馆内全部的植物档案")]
	private static bool AllChipDocUnlocked()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.allChipDocUnlocked;
	}

	[Command("archive_has_normal_chip_to_analyze", Desc = "判断待解析的芯片是否包括普通芯片，在【解析芯片】前调用")]
	private static bool HasNormalChipToAnalyze()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.HasNormalChipToAnalyze();
	}

	[Command("archive_has_special_chip_to_analyze", Desc = "判断待解析的芯片是否包括特殊芯片，在【解析芯片】前调用")]
	private static bool HasSpecialChipToAnalyze()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.HasSpecialChipToAnalyze();
	}

	[Command("is_farm_resource_empty")]
	private static bool IsFarmResourceEmpty()
	{
		string roomId = DolocAPI.archiveHandle.MainFarm.RoomId;
		if (GetResourceCount(DungeonResourceType.TREE.ToString(), roomId) == 0)
		{
			return GetResourceCount(DungeonResourceType.WEEDS.ToString(), roomId) == 0;
		}
		return false;
	}

	[Command("get_resource_count_of_type")]
	private static int GetResourceCount(string resourceType = null, string roomName = null)
	{
		IDungeonResourceHost dungeonResourceHost = (roomName.IsNullOrEmpty() ? DolocAPI.CurrentRoom : DolocAPI.GetRoom(roomName));
		if (dungeonResourceHost == null)
		{
			return 0;
		}
		IEnumerable<DungeonResource> allDungeonResources = dungeonResourceHost.DM_dungeonResource.AllDungeonResources;
		if (resourceType.IsNullOrEmpty())
		{
			return allDungeonResources.Count();
		}
		DungeonResourceType dungeonResourceType = resourceType.ConvertToEnumOrDefault<DungeonResourceType>();
		int num = 0;
		foreach (DungeonResource item in allDungeonResources)
		{
			if (item.ResourceType == dungeonResourceType)
			{
				num++;
			}
		}
		return num;
	}

	[Command("fall_asleep", Desc = "入睡指定秒数")]
	private static void Command_FallAsleep(int totalSeconds, bool saveData, bool fullSleepBuff)
	{
		if (totalSeconds <= 0)
		{
			return;
		}
		DolocAPI.archiveHandle.farmData.agentData.OnSpiritReset();
		DolocAPI.agent.MotionAbility.ClearEnvModerate();
		DolocAPI.archiveHandle.PassTimeNoControl(totalSeconds, delegate
		{
			if (DolocAPI.CurrentRoom.Type == RoomType.Farm)
			{
				((TemplateRoom)DolocAPI.CurrentRoom).RefreshRender();
			}
			DolocAPI.RecoverPlayerValue(totalSeconds, fullSleepBuff, isNap: false);
			DolocAPI.OnWakeUp(saveData, sendEvent: true, clearRecoveryDecayBuffs: true);
		});
	}

	[Command("get_game_hour2_secs", Desc = "获取游戏世界该小时对应的秒数")]
	private static int GetGameHour2Secs(int hour)
	{
		return DolocAPI.GlobalParameter.GameHours2Secs(hour);
	}

	[Command("sleep_to_target_hour", Desc = "睡到指定时间")]
	private static void Command_PassTimeToTargetHour(int hour, bool saveData = false, bool fullSleepBuff = true)
	{
		DateInfo dateNow = DolocAPI.archiveHandle.timeData.dateNow;
		int num = ((hour > dateNow.Hour) ? (hour - dateNow.Hour) : (hour + DolocAPI.GlobalParameter.Day2Hour - dateNow.Hour)) * DolocAPI.GlobalParameter.Hour2Min - dateNow.Minute;
		Command_FallAsleep(DolocAPI.GlobalParameter.GameMinutes2Secs(num), saveData, fullSleepBuff);
	}

	[Command("open_terraforming_panel", Desc = "打开环境改造器页面")]
	private static UniTask OpenEnvOptimizerPanel()
	{
		DolocAPI.EnterUI<EnvOptimizerUiState>();
		return DolocAPI.WaitWhileInUiSateTask();
	}

	[Command("get_latest_activated_terraforming_part", Desc = "获取刚刚解锁的环境改造器零件")]
	private static string GetLatestActivatedEnvOptimizerComponent()
	{
		return EnvOptimizerUiState.latestActivatedComponent ?? "";
	}

	[Command("put_item_into_terraforming", Desc = "把指定零件道具放进环境改造器")]
	private static void PutItemIntoEnvOptimizer(int index, string itemName)
	{
		EnvOptimizerSystem envOptimizerSystem = DolocAPI.archiveHandle.farmData.envOptimizerSystem;
		if (envOptimizerSystem.IfItemIsComponent(itemName))
		{
			envOptimizerSystem.TryPlaceItemAtIndex(index, DolocAPI.GenerateItem(itemName));
		}
	}

	[Command("add_env_optimizer_point", Desc = "给环境改造器分支增加指定分数 (不指定分支则处理每个分支)")]
	private static void AddEnvPoint(int point, string type = "")
	{
		EnvOptimizerBranchType result;
		if (type.IsNullOrEmpty())
		{
			DolocAPI.archiveHandle.farmData.envOptimizerSystem.Debug_ForceAddPointForAllBranches(point);
		}
		else if (Enum.TryParse<EnvOptimizerBranchType>(type, ignoreCase: true, out result))
		{
			DolocAPI.archiveHandle.farmData.envOptimizerSystem.Debug_ForceAddPoint(result, point);
		}
	}

	[Command("get_env_optimizer_point", Desc = "给环境改造器分支增加指定分数 (不指定分支则获取总分数)")]
	private static string GetEnvPoint(string type = "")
	{
		EnvOptimizerSystem envOptimizerSystem = DolocAPI.archiveHandle.farmData.envOptimizerSystem;
		if (type.IsNullOrEmpty())
		{
			return $"{envOptimizerSystem.TotalPower} / {envOptimizerSystem.TotalLimitation}";
		}
		if (Enum.TryParse<EnvOptimizerBranchType>(type, ignoreCase: true, out var result) && envOptimizerSystem.Branches.TryGetValue(result, out var value))
		{
			return $"{value.Count}/{value.Limitation}";
		}
		return "0";
	}

	[Command("get_env_optimizer_level", Desc = "获取当前改造等级")]
	private static int GetEnvOptimizerLevel()
	{
		return DolocAPI.archiveHandle.farmData.envOptimizerSystem.GetActiveSlotCount();
	}

	[Command("get_unlocked_monster_doc_count", Desc = "获取已解锁的怪物档案数")]
	private static int GetUnlockedMonsterDocCount()
	{
		return DolocAPI.archiveHandle.farmData.collectionManager.GetMonsterDocumentCount();
	}

	[Command("get_unlocked_resource_doc_count", Desc = "获取已解锁的资源档案数")]
	private static int GetUnlockedResourceDocCount()
	{
		return DolocAPI.archiveHandle.farmData.collectionManager.GetResourceDocumentCount();
	}
}
