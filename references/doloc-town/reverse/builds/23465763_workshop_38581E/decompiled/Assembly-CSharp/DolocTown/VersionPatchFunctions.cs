using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Fishing;
using DolocTown.Config.Mission;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Plant;
using DolocTown.Config.Resource;
using DolocTown.Config.TechTree;
using DolocTown.Config.UI;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using UnityEngine;

namespace DolocTown;

public class VersionPatchFunctions
{
	[VersionPatch("0.92.01")]
	public static void Patch09201()
	{
		DolocAPI.archiveHandle.farmData.unlockedSeedNodes.Remove("seed_chinese_cabbage");
		DolocAPI.RefreshStore("villain_shop");
	}

	[VersionPatch("0.92.03")]
	public static void Patch09203()
	{
		if (DolocAPI.IsMissionComplete("plant_guide_1"))
		{
			DolocAPI.SendItemAsEmail("cake_kasia", 1);
		}
	}

	[VersionPatch("0.92.06")]
	public static void Patch09206()
	{
		if (DolocAPI.IsMissionComplete("terraforming_main_2"))
		{
			DolocAPI.SendItemAsEmail("terraforming_valley", 1);
		}
	}

	[VersionPatch("0.92.07")]
	public static void Patch09207()
	{
		if (DolocAPI.IsMissionListening("zenis_cup_0") && DolocAPI.GetEventTriggerCount(GameEventType.OBTAIN_ITEM, "zenis_cup") > 0)
		{
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("zenis_cup_0");
		}
	}

	[VersionPatch("0.92.14")]
	public static void Patch09214()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("seedunlock_guide_entsk") && DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "seedunlock_guide_entsk") == 0)
		{
			DolocAPI.AddDialogueNode("seedunlock_guide_entsk");
		}
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("force_guide_actsk") && DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "force_guide_actsk") == 0)
		{
			DolocAPI.AddDialogueNode("force_guide_actsk");
		}
	}

	[VersionPatch("0.92.16")]
	public static void Patch09216()
	{
		foreach (SeedInfo data in DolocConfig.Tables.TbSeed.DataList)
		{
			if (!DolocAPI.archiveHandle.IsSeedNodeUnlocked(data.Id))
			{
				DolocAPI.archiveHandle.farmData.recipeManager.LockRecipe(data.Id, includeDish: false);
			}
		}
		DolocAPI.archiveHandle.UnlockRecipe("seed_endyam");
		DolocAPI.archiveHandle.UnlockRecipe("seed_succulent");
		DolocAPI.archiveHandle.UnlockRecipe("seed_thunder_grass");
		DolocAPI.archiveHandle.UnlockRecipe("seed_paddy");
		DolocAPI.archiveHandle.UnlockRecipe("seed_wheat");
		DolocAPI.archiveHandle.UnlockRecipe("seed_crimson_ascomyceter");
	}

	[VersionPatch("0.92.23")]
	public static void Patch09223()
	{
		if (DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.SubmittedChipCount > 0 && DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "archive_chip_guide_adtsk_1") == 0)
		{
			DolocAPI.BroadcastString(GameEventType.COMPLETE_DIALOGUE, "archive_chip_guide_adtsk_1");
			DolocAPI.AddDialogueNode("archive_chip_guide_adtsk_2");
		}
	}

	private static void ResetAchievements()
	{
		DolocAPI.AchievementSystem.Initialize(force: true);
		int submittedChipCount = DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.SubmittedChipCount;
		int num = submittedChipCount switch
		{
			0 => 0, 
			1 => 1, 
			_ => Mathf.Min(5, 1 + (submittedChipCount - 1) / 3), 
		};
		GameEventRecorder recorder = DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(GameEventType.ANALYZE_CHIP);
		if (recorder != null && recorder.totalCount == 0)
		{
			for (int i = 0; i < num; i++)
			{
				DolocAPI.BroadcastInt(GameEventType.ANALYZE_CHIP, 1);
			}
		}
		if (DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(GameEventType.MAKE_ITEM) is GameEventRecorderString gameEventRecorderString)
		{
			foreach (string key in gameEventRecorderString.Datas.Keys)
			{
				if (DolocAPI.QueryItemProto(key, out var proto) && DolocAPI.GlobalParameter.FoodItemSubTypes.Contains(proto.SubType) && DolocAPI.GetEventTriggerCount(GameEventType.MAKE_ITEM, proto.Id) > 0 && DolocAPI.GetEventTriggerCount(GameEventType.MAKE_NEW_FOOD, proto.Id) <= 0)
				{
					DolocAPI.BroadcastString(GameEventType.MAKE_NEW_FOOD, proto.Id);
				}
			}
		}
		if (!SteamManager.Initialized)
		{
			Debug.LogError("恢复Steam成就: 成就重建失败,Steam未初始化");
			return;
		}
		AchievementSystem achievementSystem = DolocAPI.archiveHandle.extraData.achievementSystem;
		Queue<string> queue = new Queue<string>();
		Debug.Log($"<color=#ff4f4f>对{achievementSystem.LockedAchievements.Count()}个成就任务进行重建</color>");
		foreach (IMission lockedAchievement in achievementSystem.LockedAchievements)
		{
			try
			{
				if (lockedAchievement.TryInvokeHistoryInSandBox(out var reason))
				{
					Debug.Log("成就任务\"" + lockedAchievement.Id + "\"重建成功");
					queue.Enqueue(lockedAchievement.Id);
				}
				else
				{
					Debug.LogWarning("成就任务\"" + lockedAchievement.Id + "\"重建失败:" + reason);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"重建成就任务\"{lockedAchievement.Id}\"时遇到异常:{ex}");
				Debug.LogException(ex);
			}
		}
		while (queue.Count > 0)
		{
			string missionId = queue.Dequeue();
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission(missionId);
		}
		Debug.Log($"一共{queue.Count}个成就恢复完成");
		Debug.Log("<color=#ff4f4f>特别成就手动检查</color>");
		ResetSpecialAchievements();
	}

	private static void ResetSpecialAchievements()
	{
		if (DolocAPI.archiveHandle.IsDoubleJumpUnlocked())
		{
			CompleteAchievement("achievements_000_000_000");
			Debug.Log("<color=#ff4f4f>特别成就手动检查:\"achievements_000_000_000\"已经解锁</color>");
		}
		if (DolocAPI.archiveHandle.IsDashUnlocked())
		{
			CompleteAchievement("achievements_000_000_010");
			Debug.Log("<color=#ff4f4f>特别成就手动检查:\"achievements_000_000_010\"已经解锁</color>");
		}
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("trash_can_junk_mail"))
		{
			CompleteAchievement("achievements_000_500_000");
			Debug.Log("<color=#ff4f4f>特别成就手动检查:\"achievements_000_500_000\"已经解锁</color>");
		}
		if (DolocAPI.archiveHandle.farmData.agentData.farmLevel >= 2)
		{
			CompleteAchievement("achievements_000_020_000");
			Debug.Log("<color=#ff4f4f>特别成就手动检查:\"achievements_000_020_000\"已经解锁</color>");
		}
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("kasia_favorability5"))
		{
			CompleteAchievement("achievements_000_110_000");
			Debug.Log("<color=#ff4f4f>特别成就手动检查:\"achievements_000_110_000\"已经解锁</color>");
		}
	}

	private static void CompleteAchievement(string missionId)
	{
		DolocAPI.archiveHandle.farmData.missionManager.CompleteMission(missionId);
		DolocAPI.AchievementSystem.SetSteamAchievement(missionId);
	}

	[VersionPatch("0.92.32")]
	public static void Patch09232()
	{
		if (!DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("force_main") && DolocAPI.GetEventTriggerCount(GameEventType.CUSTOM, "huge_stone") > 0 && !DolocAPI.IsMissionComplete("force_main_1") && DolocAPI.GetEventTriggerCount(GameEventType.SUBMIT_ITEM, "torn_page") == 0 && !(from x in DolocAPI.archiveHandle.GetMissions()
			select x?.Id).ToList().Contains("force_main_2"))
		{
			DolocAPI.archiveHandle.StopMissionChain("force_main");
			DolocAPI.StartMissionChain("force_main");
		}
	}

	[VersionPatch("0.92.33")]
	public static void Patch09233()
	{
		ResetAchievements();
	}

	[VersionPatch("0.92.35")]
	public static void Patch09235()
	{
		if (DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("force_main"))
		{
			DolocAPI.archiveHandle.StopMissionChain("force_main");
			DolocAPI.archiveHandle.RemoveMission("force_main_0");
			DolocAPI.archiveHandle.RemoveMission("force_main_1");
			DolocAPI.archiveHandle.RemoveMission("force_main_2");
		}
	}

	[VersionPatch("0.92.39")]
	public static void Patch09239()
	{
		if (DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("force_main") && !DolocAPI.IsMissionInProcess("force_main") && DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(GameEventType.SUBMIT_ITEM, "torn_page") == 0 && !DolocAPI.IsMissionInProcess("terraforming_main") && !DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("terraforming_main"))
		{
			DolocAPI.archiveHandle.farmData.missionChainManager.__ForceRemoveMissionChain("force_main");
			DolocAPI.archiveHandle.StopMissionChain("force_main");
			DolocAPI.archiveHandle.RemoveMission("force_main_0");
			DolocAPI.archiveHandle.RemoveMission("force_main_1");
			DolocAPI.archiveHandle.RemoveMission("force_main_2");
			DolocAPI.StartMissionChain("force_main");
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("force_main_0");
		}
	}

	[VersionPatch("0.92.40")]
	public static void Patch09240()
	{
		if ((DolocAPI.IsMissionInProcess("terraforming_main") || DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("terraforming_main")) && DolocAPI.IsMissionInProcess("force_main"))
		{
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("force_main_0");
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("force_main_1");
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("force_main_2");
			DolocAPI.archiveHandle.farmData.missionChainManager.__ForceSetChainComplete("force_main");
		}
	}

	[VersionPatch("0.93.00")]
	private static void UpdateTo09300()
	{
		DolocAPI.archiveHandle.farmData.techLevelManager.ClampExp(TechPointType.ANIMAL, 0);
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("claud_main_actsk"))
		{
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Item);
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Monster);
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Npc);
		}
		if (DolocAPI.IsEventDecoratorComplete("MISSION_PROGRESS.VisitLightman"))
		{
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Creature);
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.MAKE_ITEM, "construct_controller") > 0)
		{
			DolocAPI.SendEmail("construct_controller_update");
		}
		if (DolocAPI.archiveHandle.farmLevel == 2)
		{
			DolocAPI.SendEmail("farm_extensionplus");
		}
	}

	[VersionPatch("0.93.02")]
	private static void UpdateTo09302()
	{
		if (DolocAPI.archiveHandle.cityData.factionMissionManager.QueryFactionMission("skychild_wreckage", out var mission) && mission.IsComplete)
		{
			mission.CashMissionReward();
		}
	}

	[VersionPatch("0.93.03")]
	private static void UpdateTo09303()
	{
		DolocAPI.RemoveDialogueNode("unlock_all_docs");
		DolocAPI.AddDialogueNode("archive_plant_guide_actsk");
		DolocAPI.AddDialogueNode("farm_extension");
	}

	[VersionPatch("0.93.05")]
	private static void UpdateTo09305()
	{
		DolocAPI.archiveHandle.UnlockSeedItemInStore();
		DolocAPI.RefreshStore("seasonseed_shop");
	}

	[VersionPatch("0.93.07")]
	private static void UpdateTo09307()
	{
		if (DolocAPI.archiveHandle.QueryStore("animal_shop", out var store) && store.GetSoldCount("recipe_honey_comb") >= 1)
		{
			DolocAPI.SendItemAsEmail("recipe_honey_comb", 1);
		}
		if (DolocAPI.archiveHandle.farmLevel >= 2 && !DolocAPI.archiveHandle.ContainsEmailName("construct_controller_guide"))
		{
			DolocAPI.SendEmail("construct_controller_guide", allowRepeat: false);
			DolocAPI.UnlockStoreItem("mody_exchange_shop", "construct_controller");
			DolocAPI.RefreshStore("mody_exchange_shop");
		}
		string[] array = new string[4] { "player_behavior@22", "player_behavior@25", "player_behavior@27", "player_behavior@28" };
		foreach (string text in array)
		{
			if (!DolocAPI.IsMissionComplete(text))
			{
				DolocAPI.archiveHandle.RemoveMission(text);
				DolocAPI.Command_RestartMission("player_behavior", text);
			}
		}
	}

	[VersionPatch("0.93.09")]
	private static void UpdateTo09309()
	{
		DolocAPI.assets.techTrees.QueryTechTree("animal_techtree", out var tree);
		TreeGraphNode<TechNodeProto>[] nodes = tree.nodes;
		foreach (TreeGraphNode<TechNodeProto> treeGraphNode in nodes)
		{
			if (DolocAPI.archiveHandle.GetTechNodeUnlockState(treeGraphNode.id))
			{
				TechNodeCost[] costs = treeGraphNode.data.costs;
				for (int j = 0; j < costs.Length; j++)
				{
					TechNodeCost techNodeCost = costs[j];
					DolocAPI.AddTechPoint(techNodeCost.type, techNodeCost.count);
				}
				string[] equipments = treeGraphNode.data.equipments;
				foreach (string item in equipments)
				{
					DolocAPI.archiveHandle.farmData.recipeManager.UnlockedRecipes.Remove(item);
				}
				DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove(treeGraphNode.id);
			}
		}
		TechLevelData levelData = DolocAPI.archiveHandle.farmData.techLevelManager.GetLevelData(TechPointType.ANIMAL);
		int totalExp = levelData.TotalExp;
		levelData.__Clear();
		levelData.AddTechExp(totalExp);
		if (DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("force_main") || DolocAPI.GetEventTriggerCount(GameEventType.SUBMIT_ITEM, "torn_page") > 0)
		{
			DolocAPI.RemoveDialogueNode("force_guide_actsk");
		}
	}

	[VersionPatch("0.93.10")]
	private static void UpdateTo09310()
	{
		if (DolocAPI.IsFactionMissionComplete("vulture_building"))
		{
			DolocAPI.SendItemAsEmail("seed_tree", 50);
		}
		if (DolocAPI.IsEventDecoratorComplete("COMMERCIAL_UNLOCK.vulture") && !DolocAPI.QueryFactionJoinState(FactionType.Vulture))
		{
			DolocAPI.RemoveEventDecorator("COMMERCIAL_UNLOCK.vulture");
			DolocAPI.AddDialogueNode("force_shylock_invite_face");
			DolocAPI.QueryNpc("shylock", out var npc);
			npc.InvokeSchedule();
		}
		if (!DolocAPI.IsEventDecoratorComplete("COMMERCIAL_UNLOCK.skychild") && DolocAPI.QueryFactionJoinState(FactionType.Skychild))
		{
			DolocAPI.QueryTreatyPortFaction("skychild", out var faction);
			faction.ClearSettleInfo();
		}
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("animal_guide_actsk2"))
		{
			DolocAPI.AddDialogueNode("kenenimuu_intro");
		}
	}

	[VersionPatch("0.93.11")]
	private static void UpdateTo09311()
	{
		foreach (var (nodeName, pt) in new Dictionary<string, int>
		{
			{ "animal_care", 1 },
			{ "auto_feed", 1 },
			{ "fish_hatchery", 1 },
			{ "advanced_animal", 2 },
			{ "advanced_fishing", 2 },
			{ "manure_boost", 2 }
		})
		{
			if (DolocAPI.archiveHandle.GetTechNodeUnlockState(nodeName))
			{
				DolocAPI.AddTechPoint(TechPointType.ANIMAL, pt);
			}
		}
	}

	[VersionPatch("0.93.12")]
	private static void UpdateTo09312()
	{
		if (DolocAPI.QueryNpc("mody", out var npc))
		{
			npc.ManualSetToMarkPoint("机库休息室-墨翟");
			npc.InvokeSchedule();
		}
		DolocAPI.archiveHandle.farmData.emailManager.__Unique("seasonseed_unlock");
	}

	[VersionPatch("0.93.15")]
	private static void UpdateTo09315()
	{
		if (DolocAPI.QueryNpc("cod", out var npc))
		{
			npc.ManualSetToMarkPoint("工程机库-科奥德");
			npc.EnableSchedule();
		}
	}

	[VersionPatch("0.93.16")]
	private static void UpdateTo09316()
	{
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("loom") && !DolocAPI.archiveHandle.GetTechNodeUnlockState("pickle1"))
		{
			DolocAPI.archiveHandle.farmData.recipeManager.UnlockedRecipes.Remove("loom");
			DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove("loom");
			DolocAPI.archiveHandle.UnlockTechNode("food_techtree", "pickle1");
		}
	}

	[VersionPatch("0.93.19")]
	private static void UpdateTo09319()
	{
		if (!DolocAPI.IsEventDecoratorComplete("COMMERCIAL_UNLOCK.vulture") || !DolocAPI.IsEventDecoratorComplete("COMMERCIAL_UNLOCK.skychild") || DolocAPI.archiveHandle.cityData.treatyPortFactionManager.MaintainState)
		{
			return;
		}
		DolocAPI.QueryTreatyPortFaction("vulture", out var faction);
		DolocAPI.QueryTreatyPortFaction("skychild", out var faction2);
		if (faction.IsJoin)
		{
			if (!faction2.IsJoin)
			{
				faction2.Settle(faction.JoinTime);
			}
		}
		else if (faction2.IsJoin)
		{
			faction.Settle(faction2.JoinTime);
		}
	}

	[VersionPatch("0.94.03")]
	private static void UpdateTo09403()
	{
		if (DolocAPI.archiveHandle.farmData.unlockedBuildings.Remove("cellar") && DolocAPI.GetEventTriggerCount(GameEventType.MAKE_ITEM, "cellar") == 0)
		{
			DolocAPI.SendItemAsEmail("cellar", 1);
		}
	}

	[VersionPatch("0.94.05")]
	private static void UpdateTo09405()
	{
		DolocAPI.RemoveDialogueNode("unlock_all_docs");
		DolocAPI.AddDialogueNode("archive_plant_guide_actsk");
	}

	[VersionPatch("0.94.06")]
	private static void UpdateTo09406()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("alchemy_favorability4"))
		{
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy3");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy3_1");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy6");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy6_1");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy6_2");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy8");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy8_1");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy8_2");
			DolocAPI.archiveHandle.RemoveMission("npc_favorability@alchemy10");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "alchemy_favorability2_actsk") > 0)
		{
			DolocAPI.RemoveDialogueNode("alchemy_favorability2_actsk");
		}
	}

	[VersionPatch("0.94.08")]
	private static void UpdateTo09408()
	{
		if (DolocAPI.archiveHandle.cityData.treatyPortFactionManager.MaintainState)
		{
			string[] treatyPortGatesName = DolocAPI.GlobalParameter.TreatyPortGatesName;
			for (int i = 0; i < treatyPortGatesName.Length; i++)
			{
				DolocAPI.DisableGate(treatyPortGatesName[i]);
			}
		}
	}

	[VersionPatch("0.94.09")]
	private static void UpdateTo09409()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("alchemy_favorability4_select"))
		{
			DolocAPI.archiveHandle.cityData.dialogueManager.GetCandidateNodes("alchemy", out var nodeNames);
			if (!nodeNames.IsNullOrEmpty() && !nodeNames.Contains("alchemy_herbal_pouch"))
			{
				DolocAPI.AddDialogueNode("alchemy_herbal_pouch");
				if (!DolocAPI.Command_HasHerbPouch())
				{
					DolocAPI.SendItemAsEmail("herbal_pouch", 1);
				}
			}
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "lank_favorability1_actsk") > 0)
		{
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("lank_favorability1_mission_0");
			DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("lank_favorability1_mission_1");
		}
		foreach (CityRoom value in DolocAPI.archiveHandle.cityData.cityRooms.Values)
		{
			if (!value.IsInHouse)
			{
				DolocAPI.RefreshResourceByType(value, DungeonResourceType.ORE);
			}
		}
		foreach (Dungeon item in DolocAPI.archiveHandle.dungeonData.dungeonManager.totalDungeons.ToList())
		{
			item.__ReGenDungeonDatas();
		}
	}

	[VersionPatch("0.94.10")]
	private static void UpdateTo09410()
	{
		if (DolocAPI.IsMissionComplete("dada_favorability3_mission@dada8_4"))
		{
			DolocAPI.SendEmail("dada_favorability3", allowRepeat: false);
		}
	}

	[VersionPatch("0.94.11")]
	private static void UpdateTo09411()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("chip_after_analyze"))
		{
			DolocAPI.archiveHandle.cityData.dialogueManager.Visit("archive_chip_guide.first_chip");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, "archive_plant_guide_actsk") > 0)
		{
			DolocAPI.archiveHandle.cityData.dialogueManager.Visit("archive_plant_guide.first_plant");
		}
	}

	[VersionPatch("0.94.12")]
	private static void UpdateTo09412()
	{
		DolocAPI.StartFactionMission("cerro_rico_display_cup");
	}

	private static bool CheckEventComplete(string id)
	{
		if (!DolocAPI.IsEventDecoratorComplete(id))
		{
			return DolocAPI.GetEventTriggerCount(GameEventType.COMPLETE_DIALOGUE, id) > 0;
		}
		return true;
	}

	private static bool VisitedInDialogue(string id)
	{
		return DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited(id);
	}

	[VersionPatch("0.95.01")]
	private static void UpdateTo09501()
	{
		if (DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(GameEventType.ARRIVE_ROOM_CITY) is GameEventRecorderString gameEventRecorderString)
		{
			foreach (string key in gameEventRecorderString.Datas.Keys)
			{
				DolocAPI.archiveHandle.farmData.mapManager.SetRoomVisited("city_full", "city_" + key);
			}
		}
		if (!DolocAPI.archiveHandle.CanFarmUpgrade())
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetRoomVisited("city_full", "farm_type1-平地");
		}
		if (DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("city_郊区-后山山麓1") && DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("city_郊区-小林地"))
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("后山山麓1-隐藏");
		}
		if (DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.红树林_DOWN") && DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.洞穴_红树林"))
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-红树林洞穴入口");
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-红树林洞穴出口");
		}
		if (DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.泥炭地R") && DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.洞穴_泥炭地"))
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-泥炭地洞穴入口");
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-泥炭地洞穴出口");
		}
		if (DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.湿地入口_R3") && DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.洞穴_污染区"))
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-污染区洞穴入口");
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-污染区洞穴出口");
		}
		if (DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.睡莲地R") && DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited("dungeon_湿地.洞穴_睡莲地"))
		{
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-睡莲地洞穴入口");
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited("湿地-睡莲地洞穴出口");
		}
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("tree_plant"))
		{
			DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove("tree_plant");
		}
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("botany") && !DolocAPI.archiveHandle.GetTechNodeUnlockState("fertilizer_research"))
		{
			DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove("botany");
		}
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("material_science3"))
		{
			DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove("material_science3");
		}
		if (DolocAPI.archiveHandle.GetTechNodeUnlockState("material_science4"))
		{
			DolocAPI.archiveHandle.farmData.unlockedTechNodes.Remove("material_science4");
		}
		DolocAPI.archiveHandle.farmData.techLevelManager.AfterLoadData();
		if (VisitedInDialogue("lank_pickaxe_ex_copy_merlin_2"))
		{
			DolocAPI.AddEventDecorator("LANK_PICKAXE_EX.Merlin_Finished");
		}
		DolocAPI.Command_RestartMission("achievements", "achievements_000_500_030");
	}

	[VersionPatch("0.95.03")]
	private static void UpdateTo09503()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		if ((dateNow.Month == 4 && dateNow.Day == 22 && dateNow.Hour >= 9) || (dateNow.Month == 4 && dateNow.Day > 22 && dateNow.Day <= 26))
		{
			DolocAPI.SendEmail("evernight_festival", allowRepeat: false);
			DolocAPI.AddDialogueNode("evernight_festival_lantern_entsk");
		}
		if ((dateNow.Month == 4 && dateNow.Day == 23 && dateNow.Hour >= 7) || (dateNow.Month == 4 && dateNow.Day > 23))
		{
			DolocAPI.SendEmail("evernight_festival_firecracker", allowRepeat: false);
			DolocAPI.AddDialogueNode("evernight_festival_firecracker");
		}
		if (dateNow.Month == 4 && dateNow.Day >= 26 && dateNow.Day <= 28)
		{
			DolocAPI.StartDialogueNode("evernight_gift_function");
		}
		if (dateNow.Month == 4 && dateNow.Day == 28 && dateNow.Hour >= 7)
		{
			DolocAPI.SendEmail("evernight_festival_newyear", allowRepeat: false);
		}
	}

	[VersionPatch("0.95.04")]
	private static void UpdateTo09504()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("claud_main_actsk"))
		{
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Resource);
		}
		DolocAPI.RemoveEventDecorator("FESTIVAL.EvernightNewyear");
		if (DolocAPI.IsEventDecoratorComplete("MISSION_PROGRESS.TerraVulture"))
		{
			DolocAPI.archiveHandle.UnlockGene("aerial_root");
			DolocAPI.archiveHandle.UnlockGene("oxygen");
		}
	}

	[VersionPatch("0.95.05")]
	private static void UpdateTo09505()
	{
		if (DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited("claud_main_actsk"))
		{
			DolocAPI.archiveHandle.farmData.collectionManager.UnlockCollectionFunc(CompendiumLabel.Resource);
		}
		CollectionManager collectionManager = DolocAPI.archiveHandle.farmData.collectionManager;
		string title;
		foreach (AnimalDocumentInfo data in DolocConfig.Tables.TbAnimalDocument.DataList)
		{
			if (!collectionManager.creatureCollections.ContainsKey(data.Id))
			{
				collectionManager.creatureCollections.Add(data.Id, new CollectionRecord());
				collectionManager.creatureCollections[data.Id].isUnlock = collectionManager.collections.ContainsKey(data.Id);
			}
			collectionManager.RefreshAnimalRecord(data.Id, out title);
		}
		foreach (FishDocumentInfo data2 in DolocConfig.Tables.TbFishDocument.DataList)
		{
			collectionManager.creatureCollections.TryAdd(data2.Id, new CollectionRecord());
			collectionManager.creatureCollections[data2.Id].isUnlock = DolocAPI.archiveHandle.cityData.documentManager.fishDocMgr.CheckFishDocumentUnlocked(data2.Id);
			collectionManager.RefreshFishRecord(data2.Id, out title);
		}
		foreach (MonsterDocumentInfo data3 in DolocConfig.Tables.TbMonsterDocument.DataList)
		{
			collectionManager.monsterCollections.TryAdd(data3.Id, new CollectionRecord());
			collectionManager.monsterCollections[data3.Id].isUnlock = DolocAPI.GetEventTriggerCount(GameEventType.SLAIN_MONSTER, data3.Id) > 0;
			collectionManager.RefreshMonsterRecord(data3.Id, out title);
		}
		foreach (NpcDocumentInfo data4 in DolocConfig.Tables.TbNpcDocument.DataList)
		{
			if (!collectionManager.npcCollections.ContainsKey(data4.Id) && collectionManager.collections.ContainsKey(data4.Id))
			{
				collectionManager.npcCollections.Add(data4.Id, collectionManager.collections[data4.Id]);
				collectionManager.RefreshNpcRecord(data4.Id, out title);
			}
			else
			{
				collectionManager.npcCollections.TryAdd(data4.Id, new CollectionRecord());
			}
		}
		int num = DolocAPI.GetEventTriggerCount(GameEventType.OBTAIN_ITEM, "berry") / 3;
		for (int i = 0; i < num; i++)
		{
			DolocAPI.BroadcastString(GameEventType.GATHERING_VEGETATION_FRUIT, "berry_thicket");
		}
		int num2 = DolocAPI.GetEventTriggerCount(GameEventType.OBTAIN_ITEM, "psinensis") / 3;
		for (int j = 0; j < num2; j++)
		{
			DolocAPI.BroadcastString(GameEventType.GATHERING_VEGETATION_FRUIT, "psinensis");
		}
		foreach (ResourceDocumentInfo data5 in DolocConfig.Tables.TbResourceDocument.DataList)
		{
			collectionManager.resourceCollections.TryAdd(data5.Id, new CollectionRecord());
			CollectionRecord collectionRecord = collectionManager.resourceCollections[data5.Id];
			collectionRecord.isUnlock = data5.ResourceType switch
			{
				ResourceDocumentType.Resource => DolocAPI.GetEventTriggerCount(GameEventType.FELL_DUNGEON_RESOURCE, data5.Id) > 0, 
				ResourceDocumentType.Vegetation => DolocAPI.GetEventTriggerCount(GameEventType.GATHERING_VEGETATION_FRUIT, data5.Id) > 0, 
				_ => false, 
			};
			collectionManager.RefreshResourceRecord(data5.Id, out title);
		}
		DolocAPI.archiveHandle.farmData.envOptimizerSystem.data.Debug_ClearPoint();
		DolocAPI.archiveHandle.farmData.envOptimizerSystem.ResetEnvOptimizerPoint();
		foreach (Npc allNpc in DolocAPI.archiveHandle.cityData.npcManager.AllNpcs)
		{
			if (allNpc.proto.ScheduleInitialState)
			{
				allNpc.EnableSchedule();
			}
			else
			{
				allNpc.DisableSchedule(allNpc.proto.InitialMarkPoint);
			}
		}
		if (CheckEventComplete("eden_main_entsk_select_c") || CheckEventComplete("MISSION_PROGRESS.TerraformingStart"))
		{
			DolocAPI.EnableNpcSchedule("alchemy");
		}
		if (CheckEventComplete("claud_main_actsk") || CheckEventComplete("MISSION_PROGRESS.ForceOpen"))
		{
			DolocAPI.EnableNpcSchedule("claud");
		}
		if (CheckEventComplete("MISSION_PROGRESS.DroneGet") || VisitedInDialogue("shooting_first"))
		{
			DolocAPI.EnableNpcSchedule("cod");
		}
		if (CheckEventComplete("MISSION_PROGRESS.OpenValley") || CheckEventComplete("eden_main_valley_anim"))
		{
			DolocAPI.EnableNpcSchedule("kel");
		}
		if (CheckEventComplete("MISSION_PROGRESS.AnimalAppear"))
		{
			DolocAPI.EnableNpcSchedule("kenenimuu");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.VISIT_NPC_NAME, "lank") > 0)
		{
			DolocAPI.EnableNpcSchedule("lank");
		}
		if (CheckEventComplete("MISSION_PROGRESS.VisitLightman") || VisitedInDialogue("fish_anim"))
		{
			DolocAPI.EnableNpcSchedule("lightman");
		}
		if (CheckEventComplete("MISSION_PROGRESS.OpenValley") || CheckEventComplete("eden_main_valley_anim"))
		{
			DolocAPI.EnableNpcSchedule("loveyer");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.VISIT_NPC_NAME, "merlin") > 0)
		{
			DolocAPI.EnableNpcSchedule("merlin");
		}
		if (CheckEventComplete("MISSION_PROGRESS.BeAdventurer") || CheckEventComplete("drone_guide_entsk"))
		{
			DolocAPI.EnableNpcSchedule("orlando");
		}
		else if (CheckEventComplete("drone_guide_anim"))
		{
			DolocAPI.DisableNpcSchedule("orlando", "冒险家公会-奥兰多1");
		}
		if (DolocAPI.IsMissionComplete("trading_port_main_9@1"))
		{
			DolocAPI.EnableNpcSchedule("paiea");
		}
		if (DolocAPI.IsMissionComplete("riverway_main_2"))
		{
			DolocAPI.EnableNpcSchedule("pike");
		}
		if (DolocAPI.IsMissionComplete("trading_port_main_9@1"))
		{
			DolocAPI.EnableNpcSchedule("quipu");
		}
		if (CheckEventComplete("MISSION_PROGRESS.OpenValley") || CheckEventComplete("eden_main_valley_anim"))
		{
			DolocAPI.EnableNpcSchedule("sacco");
		}
		if (DolocAPI.IsMissionComplete("trading_port_main_9@1"))
		{
			DolocAPI.EnableNpcSchedule("shylock");
		}
		if (DolocAPI.GetEventTriggerCount(GameEventType.VISIT_NPC_NAME, "zenis") > 0)
		{
			DolocAPI.EnableNpcSchedule("zenis");
		}
		if (DolocAPI.IsMissionComplete("drone_guide_6"))
		{
			DolocAPI.AddDialogueNode("orlando_exchange");
			DolocAPI.SendEmail("orlando_exchange_guide", allowRepeat: false);
		}
		DolocAPI.archiveHandle.RefreshAllStores();
	}

	[VersionPatch("0.95.07")]
	private static void UpdateTo09507()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		if (dateNow.Month == 4 && dateNow.Day >= 23 && dateNow.Day <= 28)
		{
			DolocAPI.AddEventDecorator("FESTIVAL.EvernightNian");
		}
		if (DolocAPI.IsMissionComplete("gene_guide_0"))
		{
			DolocAPI.archiveHandle.cityData.documentManager.characterDocMgr.UnLockNpcDocument("gene_1");
		}
		if (DolocAPI.IsMissionComplete("gene_guide_1"))
		{
			DolocAPI.archiveHandle.cityData.documentManager.characterDocMgr.UnLockNpcDocument("gene_2");
		}
		if (DolocAPI.IsMissionComplete("gene_guide_3"))
		{
			DolocAPI.archiveHandle.cityData.documentManager.characterDocMgr.UnLockNpcDocument("gene_3");
		}
	}

	[VersionPatch("0.95.09")]
	private static void UpdateTo09509()
	{
		if (VisitedInDialogue("trading_port_start_anim"))
		{
			DolocAPI.DisableNpcSchedule("paiea", "码头-帕伊雅");
		}
		if (DolocAPI.IsMissionComplete("trading_port_main_9@1"))
		{
			DolocAPI.EnableNpcSchedule("paiea");
		}
	}

	[VersionPatch("0.95.16")]
	private static void UpdateTo09516()
	{
		DolocAPI.StartDialogueNode("version_patch9501");
		DolocAPI.StartDialogueNode("version_patch9510");
		DolocAPI.StartDialogueNode("version_patch9513");
	}

	[VersionPatch("0.96.06")]
	private static void UpdateTo09606()
	{
		if (DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted("eden_main") && !DolocAPI.IsMissionComplete("eden_main_2"))
		{
			DolocAPI.Command_RestartMission("eden_main", "eden_main_2");
			if (CheckEventComplete("eden_main_valley_anim"))
			{
				DolocAPI.archiveHandle.farmData.missionManager.CompleteMission("eden_main_2");
			}
		}
	}
}
