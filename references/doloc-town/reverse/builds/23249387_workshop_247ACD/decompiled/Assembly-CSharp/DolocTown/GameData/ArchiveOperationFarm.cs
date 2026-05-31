using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Plant;
using DolocTown.Config.Player;
using DolocTown.Config.TechTree;
using DolocTown.Config.UI;
using DolocTown.Config.Weather;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

public static class ArchiveOperationFarm
{
	public static bool ShouldShowBuildingHealth(this ArchiveDataHandle handle)
	{
		return handle.farmData.hasAcidRainCamed;
	}

	public static int CountMainFarmEquipment(this ArchiveDataHandle handle, string name)
	{
		if (name.IsNullOrEmpty())
		{
			return 0;
		}
		int num = ((IEquipmentHost)handle.MainFarm).CountEquipment(name);
		foreach (TemplateRoomInHouse item in handle.MainFarm.DM_building.Buildings.Select((Building x) => x.room))
		{
			num += ((IEquipmentHost)item).CountEquipment(name);
		}
		return num;
	}

	public static bool QueryTemplateRoomFromGlobal(this ArchiveDataHandle handle, string guid, out Room room)
	{
		room = null;
		if (string.IsNullOrEmpty(guid))
		{
			room = handle.MainFarm;
			return true;
		}
		if (handle.MainFarm.QueryBuildingRoom(guid, out room))
		{
			return true;
		}
		foreach (CityRoom value in handle.cityData.cityRooms.Values)
		{
			if (value.QueryBuildingRoom(guid, out room))
			{
				return true;
			}
		}
		foreach (Dungeon totalDungeon in handle.dungeonData.dungeonManager.totalDungeons)
		{
			if (totalDungeon.QueryBuildingRoom(guid, out room))
			{
				return true;
			}
		}
		return false;
	}

	public static bool QueryTemplateRoom(this ArchiveDataHandle handle, string guid, out TemplateRoom room)
	{
		room = null;
		if (string.IsNullOrEmpty(guid))
		{
			room = handle.MainFarm;
			return true;
		}
		if (handle.currentRoom == null)
		{
			return handle.MainFarm.DM_building.QueryBuildingRoom(guid, out room);
		}
		return handle.currentRoom.DM_building.QueryBuildingRoom(guid, out room);
	}

	public static bool UnlockSeedNode(this ArchiveDataHandle handle, string id)
	{
		if (DolocConfig.Tables.TbSeedUnlock.GetOrDefault(id) == null)
		{
			return false;
		}
		if (handle.farmData.unlockedSeedNodes.Add(id))
		{
			DolocAPI.BroadcastString(GameEventType.UNLOCK_SEED, id);
			DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_SEED);
			DolocAPI.TryPlaceInBackpack(id, 5, sendEmailOnOverflow: true);
			Debug.Log("解锁种子：" + id);
			return true;
		}
		return false;
	}

	public static bool IsSeedNodeUnlocked(this ArchiveDataHandle handle, string id)
	{
		return handle.farmData.unlockedSeedNodes.Contains(id);
	}

	public static int GetUnlockedSeedCount(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockedSeedNodes.Count;
	}

	public static bool IsSeedNodeAvailableToUnlock(this ArchiveDataHandle handle, string id)
	{
		if (handle.farmData.unlockedSeedNodes.Contains(id))
		{
			return false;
		}
		if (handle.farmData.seedsCanUnlock.Contains(id))
		{
			return true;
		}
		return DolocConfig.Tables.TbSeedUnlock.GetOrDefault(id)?.DefaultUnlock ?? false;
	}

	public static bool UnlockGene(this ArchiveDataHandle handle, string geneName)
	{
		if (handle.farmData.unlockedGeneNames.Contains(geneName))
		{
			return true;
		}
		if (DolocConfig.Tables.TbCropGene.DataMap.ContainsKey(geneName))
		{
			handle.farmData.unlockedGeneNames.Add(geneName);
			return true;
		}
		return false;
	}

	public static void UnlockAllGenes(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockedGeneNames.Clear();
		foreach (CropGeneInfo data in DolocConfig.Tables.TbCropGene.DataList)
		{
			handle.farmData.unlockedGeneNames.Add(data.Id);
		}
	}

	public static bool IsGeneUnlocked(this ArchiveDataHandle handle, string geneName)
	{
		if (!geneName.IsNullOrEmpty())
		{
			return handle.farmData.unlockedGeneNames.Contains(geneName);
		}
		return false;
	}

	public static bool CheckTechTreeUnlocked(this ArchiveDataHandle handle, string treeName)
	{
		TechTreeInfo byId = DolocConfig.Tables.TbTechTree.GetById(treeName);
		if (byId == null)
		{
			return false;
		}
		if (!byId.DefaultUnlock)
		{
			return handle.farmData.unlockedTechTree.Contains(treeName);
		}
		return true;
	}

	public static bool UnlockTechNode(this ArchiveDataHandle handle, string treeName, string nodeName)
	{
		if (!DolocAPI.assets.techTrees.QueryTechTreeNode(treeName, nodeName, out var proto))
		{
			return false;
		}
		if (!handle.farmData.unlockedTechNodes.Add(nodeName))
		{
			return false;
		}
		string[] equipments = proto.data.equipments;
		foreach (string recipeName in equipments)
		{
			DolocAPI.archiveHandle.UnlockRecipe(recipeName);
		}
		equipments = proto.data.buildings;
		foreach (string buildingName in equipments)
		{
			DolocAPI.archiveHandle.UnlockBuilding(buildingName);
		}
		equipments = proto.data.recipes;
		foreach (string recipeName2 in equipments)
		{
			DolocAPI.archiveHandle.UnlockRecipe(recipeName2);
		}
		handle.farmData.lockedTechNodes.Remove(nodeName);
		DolocAPI.BroadcastString(GameEventType.UNLOCK_TECH_NODE, nodeName);
		return true;
	}

	public static bool GetTechNodeUnlockState(this ArchiveDataHandle handle, string nodeName)
	{
		return handle.farmData.unlockedTechNodes.Contains(nodeName);
	}

	public static void LockTechTreeNode(this ArchiveDataHandle handle, string nodeName)
	{
		if (!handle.GetTechNodeUnlockState(nodeName) && !handle.farmData.lockedTechNodes.Contains(nodeName))
		{
			handle.farmData.lockedTechNodes.Add(nodeName);
		}
	}

	public static bool GetFirstLockNode(this ArchiveDataHandle handle, out string nodeName)
	{
		nodeName = string.Empty;
		if (handle.farmData.lockedTechNodes.Count == 0)
		{
			return false;
		}
		nodeName = handle.farmData.lockedTechNodes[0];
		return true;
	}

	public static bool IsEquipmentUnlocked(this ArchiveDataHandle handle, string equipmentName)
	{
		return handle.farmData.recipeManager.CheckRecipeUnlocked(equipmentName);
	}

	public static bool UnlockPlatform(this ArchiveDataHandle handle, string name)
	{
		if (!DolocAPI.QueryPlatformProto(name, out var _))
		{
			return false;
		}
		return handle.UnlockRecipe(name);
	}

	public static bool UnlockBuilding(this ArchiveDataHandle handle, string buildingName)
	{
		if (!DolocAPI.QueryBuilding(buildingName, out var _))
		{
			return false;
		}
		if (handle.farmData.unlockedBuildings.Contains(buildingName))
		{
			return false;
		}
		handle.farmData.unlockedBuildings.Add(buildingName);
		return true;
	}

	public static bool IsBuildingUnlocked(this ArchiveDataHandle handle, string buildingName)
	{
		if (buildingName.IsNullOrEmpty())
		{
			return false;
		}
		return handle.farmData.unlockedBuildings.Contains(buildingName);
	}

	public static bool UnlockRecipe(this ArchiveDataHandle handle, string recipeName, bool includeDish = false)
	{
		if (string.IsNullOrEmpty(recipeName))
		{
			return false;
		}
		return handle.farmData.recipeManager.UnlockRecipe(recipeName, includeDish);
	}

	public static bool IsRecipeUnlocked(this ArchiveDataHandle handle, string recipeName)
	{
		return handle.farmData.recipeManager.CheckRecipeUnlocked(recipeName);
	}

	public static bool UnlockRecipeGroup(this ArchiveDataHandle handle, string groupName)
	{
		IRecipeGroup recipeGroup = new RecipeGroup(groupName);
		if (!recipeGroup.isValid)
		{
			return false;
		}
		IRecipe[] allRecipes = recipeGroup.GetAllRecipes(includeLocked: true);
		foreach (IRecipe recipe in allRecipes)
		{
			handle.UnlockRecipe(recipe.RecipeId);
		}
		return true;
	}

	public static bool UnlockTutorial(this ArchiveDataHandle handle, string tutorialName)
	{
		if (handle.farmData.unlockedTutorials.Contains(tutorialName))
		{
			return false;
		}
		handle.farmData.unlockedTutorials.Add(tutorialName);
		return true;
	}

	public static bool CanBackpackUpgrade(this ArchiveDataHandle handle)
	{
		return handle.backpackLevel < DolocConfig.Tables.TbBackpackLevel.DataList.Last().Level;
	}

	public static bool UpgradeBackpack(this ArchiveDataHandle handle)
	{
		int key = handle.farmData.agentData.backpackLevel + 1;
		BackpackLevelInfo byLevel = DolocConfig.Tables.TbBackpackLevel.GetByLevel(key);
		if (byLevel == null)
		{
			return false;
		}
		handle.farmData.agentData.backpackLevel++;
		handle.InventorySystem.SetBackpackCapacity(byLevel.Capacity);
		return true;
	}

	public static void ValidateBackpackCapacity(this ArchiveDataHandle handle)
	{
		int backpackLevel = handle.farmData.agentData.backpackLevel;
		BackpackLevelInfo byLevel = DolocConfig.Tables.TbBackpackLevel.GetByLevel(backpackLevel);
		if (byLevel != null)
		{
			int capacity = byLevel.Capacity;
			if (handle.InventorySystem.inventory.capacity != capacity)
			{
				handle.InventorySystem.SetBackpackCapacity(capacity);
			}
		}
	}

	public static void RecordCollection(this ArchiveDataHandle handle, CollectionType type, string id, bool popTip = true)
	{
		handle.farmData.collectionManager.RecordCollectionInfo(type, id, popTip);
	}

	public static bool GetRecord(this ArchiveDataHandle handle, CompendiumLabel label, string id, out CollectionRecord record)
	{
		return handle.farmData.collectionManager.GetRecord(label, id, out record);
	}

	public static float GetWeatherRegulatorCooldownProgress(this ArchiveDataHandle handle)
	{
		return handle.timeData.WeatherRegulatorCdProgress;
	}

	public static void UseWeatherRegulator(this ArchiveDataHandle handle)
	{
		WeatherType WeatherType = handle.CurrentWeatherType;
		WeatherType type = new List<WeatherType>(from WeatherType t in Enum.GetValues(typeof(WeatherType))
			where t != WeatherType && !t.IsMalignantWeather() && t != WeatherType.NONE
			select t).Choice();
		DolocAPI.archiveHandle.SetWeather(type, shouldRender: true);
		DolocAPI.archiveHandle.PatchWeather(type);
		handle.timeData.CoolingWeatherRegulator();
	}

	public static bool SetFarmData(this ArchiveDataHandle handle, RoomProto proto)
	{
		FarmLevelInfo byId = DolocConfig.Tables.TbFarmLevel.GetById(proto.name);
		if (byId == null)
		{
			Debug.LogError("<" + proto.name + ">没有在配置表中找到对应的农场等级！");
			return false;
		}
		if (handle.MainFarm == null || !handle.MainFarm.TerrainExtend(proto))
		{
			return false;
		}
		handle.farmData.agentData.farmLevel = byId.Level;
		return true;
	}

	public static bool CanFarmUpgrade(this ArchiveDataHandle handle)
	{
		return handle.farmLevel < DolocConfig.Tables.TbFarmLevel.DataList.Last().Level;
	}

	public static bool GetFarmNextLevelProto(this ArchiveDataHandle handle, out FarmLevelInfo levelProto)
	{
		int key = handle.farmData.agentData.farmLevel + 1;
		levelProto = DolocConfig.Tables.TbFarmLevel.GetByLevel(key);
		return levelProto != null;
	}

	public static FarmLevelInfo GetFarmCurrentLevelProto(this ArchiveDataHandle handle)
	{
		return DolocConfig.Tables.TbFarmLevel.GetByLevel(handle.farmLevel);
	}

	public static bool GetMaxLevelFarmProto(this ArchiveDataHandle handle, out RoomProto proto)
	{
		FarmLevelInfo farmLevelInfo = DolocConfig.Tables.TbFarmLevel.DataList.Last();
		return DolocAPI.assets.rooms.QueryData(farmLevelInfo.Id, out proto);
	}
}
