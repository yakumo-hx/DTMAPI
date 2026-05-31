using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Archives;
using DolocTown.Config.Mission;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public static class VersionCommand
{
	[Command("global_patch", Desc = "全局补丁")]
	private static void GlobalPatch()
	{
	}

	[Command("reputation_patch", Desc = "根据现有数据重置声望值")]
	private static void ReputationValuePatch()
	{
		Dictionary<FactionType, int> dictionary = new Dictionary<FactionType, int>();
		FactionMission[] totalMissions = DolocAPI.archiveHandle.cityData.factionMissionManager.TotalMissions;
		foreach (FactionMission factionMission in totalMissions)
		{
			dictionary.TryAdd(factionMission.MainSeries, 0);
			if (factionMission.IsComplete)
			{
				dictionary[factionMission.MainSeries] += factionMission.FamePoints;
			}
		}
		foreach (KeyValuePair<FactionType, int> item in dictionary)
		{
			DolocAPI.archiveHandle.cityData.treatyPortFactionManager.QueryTreatyPortFaction(item.Key, out var faction);
			faction.ReputationValue = item.Value;
		}
	}

	[Command("update_09200")]
	private static void UpdateTo09200()
	{
		DolocAPI.archiveHandle.cityData.factionMissionManager.__ClearAllFactionMissions();
		DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("claud_main_actsk");
		DolocAPI.archiveHandle.farmData.unlockedTechNodes.Clear();
		DolocAPI.archiveHandle.farmData.techLevelManager.ClampExp(TechPointType.NATURE, 349);
		DolocAPI.archiveHandle.farmData.techLevelManager.ClampExp(TechPointType.OPERATE, 349);
		DolocAPI.archiveHandle.farmData.techLevelManager.ClampExp(TechPointType.SCIENCE, 349);
		DolocAPI.archiveHandle.farmData.techLevelManager.ClampExp(TechPointType.BATTLE, 0);
		DolocAPI.archiveHandle.ResetPlayerValues();
		int num = DolocAPI.GlobalParameter.GameHours2Secs(18f);
		if (GetTrackBackTime09200() > num)
		{
			DolocAPI.StartDialogueNode("revert_time_09100");
		}
		else
		{
			RefreshAllResourcesAndVegetation();
		}
	}

	private static int GetTrackBackTime09200()
	{
		int num = DolocAPI.GlobalParameter.GameDays2Secs(14f) + DolocAPI.GlobalParameter.GameHours2Secs(6f);
		int totalSeconds = DolocAPI.archiveHandle.timeData.totalSeconds;
		return Mathf.Max(0, totalSeconds - num);
	}

	[Command("reset_time_09200", Desc = "0.92.00版本回溯到初始时间")]
	private static void TrackBackTime()
	{
		int trackBackTime = GetTrackBackTime09200();
		DolocAPI.DelayFrame(DolocAPI.uiSystem.basicTip.Hide, 2);
		DolocAPI.archiveHandle.TrackBackTime(trackBackTime, delegate
		{
			DolocAPI.OnWakeUp(saveData: false, sendEvent: true);
			DolocAPI.archiveHandle.TryRefreshEvent(isRender: false);
			DolocAPI.uiSystem.basicTip.RefreshAll();
			DolocAPI.uiSystem.basicTip.Show();
			RefreshAllResourcesAndVegetation();
			DolocAPI.archiveHandle.RefreshWeatherStatus();
		}, 0f, 1f, 3f);
	}

	private static void RefreshAllResourcesAndVegetation()
	{
		foreach (CityRoom value in DolocAPI.archiveHandle.cityData.cityRooms.Values)
		{
			IDungeonResourceHost current;
			IDungeonResourceHost dungeonResourceHost = (current = value);
			if (current != null)
			{
				current.ClearAllResources(useEffect: false);
				current.GenerateDungeonResource_DungeonMode(refreshPresets: true);
			}
			((IVegetationHost)dungeonResourceHost)?.ReGenRoomVegetation(isRender: false);
		}
		foreach (Dungeon item in DolocAPI.archiveHandle.dungeonData.dungeonManager.totalDungeons.ToList())
		{
			item.__ReGenDungeonDatas();
		}
	}

	[Command("update_09230")]
	private static void UpdateTo09230()
	{
		ForceRevertEdenMain();
		if (DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.SubmittedChipCount >= 13)
		{
			DolocAPI.AchievementSystem.SetSteamAchievement("achievements_000_080_000");
		}
	}

	private static void ForceRevertEdenMain()
	{
		if (DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(GameEventType.COMPLETE_DIALOGUE) is GameEventRecorderString gameEventRecorderString && DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("claud_main") && (from x in DolocAPI.archiveHandle.GetMissions()
			select x?.Id).ToList().Contains("eden_main_2") && !DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("drone_guide_anim"))
		{
			gameEventRecorderString.__ForceRemove("eden_main_valley_anim");
			DolocAPI.archiveHandle.cityData.dialogueManager.ClearVisitedCount("eden_main_entsk");
			DolocAPI.archiveHandle.cityData.dialogueManager.ClearVisitedCount("eden_main_valleylock_anim");
			DolocAPI.archiveHandle.cityData.dialogueManager.ClearVisitedCount("eden_main_valley_anim");
			DolocAPI.RemoveEventDecorator("MISSION_PROGRESS.OpenValley");
			DolocAPI.archiveHandle.farmData.missionChainManager.__ForceRemoveMissionChain("eden_main");
			DolocAPI.archiveHandle.farmData.missionManager.__ForceRemoveMission("eden_main_0");
			DolocAPI.archiveHandle.farmData.missionManager.__ForceRemoveMission("eden_main_1");
			DolocAPI.archiveHandle.farmData.missionManager.__ForceRemoveMission("eden_main_2");
			DolocAPI.archiveHandle.farmData.missionManager.__ForceRemoveMission("eden_main_1@0");
			DolocAPI.archiveHandle.farmData.missionManager.__ForceRemoveMission("eden_main_1@1");
			DolocAPI.archiveHandle.farmData.emailManager.RemoveEmail("dungeon_unlock");
			DolocAPI.archiveHandle.farmData.emailManager.RemoveEmail("copper_ore_guide");
			DolocAPI.StartMissionChain("eden_main");
			if (DolocAPI.archiveHandle.farmData.missionManager.IsMissionListening("eden_main_0"))
			{
				DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("eden_main_0");
				DolocAPI.EnableNpcSchedule("alchemy");
			}
		}
	}

	[Command("update_09231")]
	private static void UpdateTo09231()
	{
		if ((from x in DolocAPI.archiveHandle.GetMissions()
			select x?.Id).ToList().Contains("eden_main_5"))
		{
			DolocAPI.AddDialogueNode("eden_main_actsk");
		}
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("drone_guide_anim") && DolocAPI.QueryNpc("orlando", out var npc))
		{
			npc.EnableSchedule();
		}
		if (DolocAPI.archiveHandle.cityData.npcManager.QueryNpc("lightman", out var data) && DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("fish_anim"))
		{
			if (!data.hasKnownName)
			{
				data.SetAliasId("fisher");
			}
			data.VisitNpcName();
		}
	}

	[Command("update_08619", Desc = "升级到0.86.19版本")]
	private static void UpdateTo08619()
	{
		Dictionary<string, PlantDocumentInfo> unlockedDocuments = DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.unlockedDocuments;
		int num = 0;
		foreach (PlantDocumentInfo value in unlockedDocuments.Values)
		{
			num += value.Reward;
		}
		Debug.Log(num);
		if (num > 0)
		{
			RewardGold rewardGold = new RewardGold((ushort)num);
			DolocAPI.archiveHandle.farmData.emailManager.SendItemsAsEmail(new RewardGold[1] { rewardGold }, "plant_doc_reward_template");
		}
	}

	[Command("update_08621", Desc = "升级到0.86.21版本")]
	private static void UpdateTo08621()
	{
		if (!DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.LoadLockState("公车站-哨站") || !DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.LoadLockState("公车站-山间小路") || !DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.LoadLockState("公车站-湿地") || !DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.LoadLockState("公车站-码头"))
		{
			DolocAPI.SetObjectLockState("公车站-小镇", value: false);
		}
	}

	[Command("update_08712", Desc = "升级到0.87.12版本")]
	private static void UpdateTo08712()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>
		{
			{ "old_pickaxe", 1 },
			{ "copper_pickaxe", 1 },
			{ "old_axe", 1 },
			{ "copper_axe", 1 },
			{ "old_sickle", 1 },
			{ "copper_sickle", 1 },
			{ "old_watercan", 1 }
		};
		ArchiveDataHandle archiveHandle = DolocAPI.archiveHandle;
		List<LinearInventory> list = new List<LinearInventory>();
		Item[] array;
		foreach (Equipment allEquipment in archiveHandle.MainFarm.DM_equipment.AllEquipments)
		{
			if (allEquipment is Case @case)
			{
				list.Add(@case.inventory);
			}
			else
			{
				if (!DolocAPI.userSettings.autoUseBox || !(allEquipment is StorageShelf storageShelf))
				{
					continue;
				}
				array = storageShelf.inventory.ReadAll();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] is ItemBox itemBox)
					{
						list.Add(itemBox.inventory);
					}
				}
			}
		}
		array = archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemBox itemBox2)
			{
				list.Add(itemBox2.inventory);
			}
		}
		list.Add(archiveHandle.InventorySystem.inventory);
		LinearInventory[] inventories = list.ToArray();
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			string key = item.Key;
			int num = inventories.CountItem(key);
			if (num != 1)
			{
				inventories.MaxCostItem(key, num - 1);
			}
		}
	}

	[Command("update_08718", Desc = "升级到0.87.18版本")]
	private static void UpdateTo08718()
	{
		foreach (KeyValuePair<TechPointType, Dictionary<string, int>> item in new Dictionary<TechPointType, Dictionary<string, int>>
		{
			{
				TechPointType.OPERATE,
				new Dictionary<string, int>
				{
					{ "alloy_material", 4 },
					{ "farm_entrance", 1 },
					{ "tree_plant", 1 },
					{ "indoor_farm", 3 },
					{ "cooking2", 1 }
				}
			},
			{
				TechPointType.NATURE,
				new Dictionary<string, int>
				{
					{ "rechargeable_battery", 1 },
					{ "wind_power_generation", 3 },
					{ "cooking2", 1 }
				}
			},
			{
				TechPointType.SCIENCE,
				new Dictionary<string, int>
				{
					{ "refining_advanced", 1 },
					{ "simple_generator", 1 },
					{ "cooking2", 3 }
				}
			}
		})
		{
			TechPointType key = item.Key;
			foreach (KeyValuePair<string, int> item2 in item.Value)
			{
				string key2 = item2.Key;
				int value = item2.Value;
				if (DolocAPI.archiveHandle.farmData.unlockedTechNodes.Contains(key2))
				{
					DolocAPI.AddTechPoint(key, value);
				}
			}
		}
	}

	[Command("update_08720", Desc = "升级到0.87.20版本")]
	private static void UpdateTo08720()
	{
		if (DolocAPI.IsEventDecoratorComplete("MISSION_PROGRESS.MiraThrustor") && !DolocAPI.IsEventDecoratorComplete("MISSION_PROGRESS.GetThrustor") && !DolocAPI.IsMissionInProcess("thrustor_guide") && !DolocAPI.IsMissionInProcess("thrustor_guide1"))
		{
			DolocAPI.QueryRoom(DolocAPI.archiveHandle.farmData.ArchiveRoomId, out var room);
			((IDropItemHost)room).CreateDropItemNoRender("thrustor", (Vector2)DolocAPI.archiveHandle.farmData.agentData._agentPosition, shouldSendMsg: false);
			DolocAPI.StartMissionChain("thrustor_guide");
		}
	}

	[Command("update_08725", Desc = "升级到0.87.25版本")]
	private static void UpdateTo08725()
	{
		if (!DolocAPI.IsMissionComplete("drone_guide_2"))
		{
			DolocAPI.archiveHandle.cityData.dialogueManager.GetCandidateNodes("cod", out var nodeNames);
			if (nodeNames.Contains("shooting_range"))
			{
				DolocAPI.BroadcastString(GameEventType.COMPLETE_DIALOGUE, "shooting_first_complete");
				DolocAPI.RemoveDialogueNode("shooting_first_complete", "cod");
				DolocAPI.SetObjectLockState("靶场控制终端", value: false);
			}
		}
	}

	[Command("update_09110", Desc = "升级到0.91.10版本")]
	private static void UpdateTo09110()
	{
		if (DolocAPI.IsEventDecoratorComplete("MISSION_PROGRESS.CommercialPortRepair"))
		{
			DolocAPI.AddEventDecorator("COMMERCIAL_UNLOCK.kontiki");
			DolocAPI.UnlockStoreItem("paiea_shop", "seed_mint");
			DolocAPI.archiveHandle.cityData.treatyPortFactionManager.TreatyPortCompleted();
		}
	}

	[Command("update_09111", Desc = "升级到0.91.11版本")]
	private static void UpdateTo09111()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("paiea") && DolocAPI.QueryNpc("paiea", out var npc))
		{
			npc.VisitNpcName();
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Npc, "paiea");
			DolocAPI.BroadcastString(GameEventType.VISIT_NPC_NAME, "paiea");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "terraforming_main_actsk2") > 0)
		{
			DolocAPI.BroadcastString(GameEventType.COMPLETE_DIALOGUE, "terraforming_main_actsk1");
		}
	}

	[Command("update_09113", Desc = "升级到0.91.13版本")]
	private static void UpdateTo09113()
	{
	}

	[Command("update_09114", Desc = "升级到0.91.14版本")]
	private static void UpdateTo09114()
	{
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("advanced_baking"))
		{
			DolocAPI.archiveHandle.UnlockRecipe("large_oven");
		}
	}
}
